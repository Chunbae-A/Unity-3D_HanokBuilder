using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// HanokBuilder — 메인 관리자 (상태·로직)
/// 파일 구성:
///   HanokUIBuilder.cs  — Canvas/패널 생성
///   HanokAssetPanel.cs — 왼쪽 에셋 목록
///   HanokEditPanel.cs  — 오른쪽 정보+편집 패널
/// </summary>
public partial class HanokUIManager : MonoBehaviour
{
    [Header("폰트")]
    public TMP_FontAsset koreanFont;

    // ── 뷰포트 툴 모드 ────────────────────────────────────
    public enum EditTool { Select, Move, Rotate, Delete }
    EditTool currentTool = EditTool.Select;

    // ── 내부 상태 ─────────────────────────────────────────
    GameObject     selectedObject;
    TMP_Text       infoNameText;
    TMP_InputField posX, posY, posZ;
    TMP_InputField rotX, rotY, rotZ;
    TMP_InputField scaleF;
    Transform      assetContent;
    Button[]       toolBtns;

    // ── 라이트 테마 색상 팔레트 ───────────────────────────
    static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out Color c); return c; }

    // 기반
    static readonly Color BG_ROOT    = Hex("#EAE6DF");
    static readonly Color BG_PANEL   = Hex("#FFFFFF");
    static readonly Color BG_CARD    = Hex("#F7F4EF");
    static readonly Color BG_INPUT   = Hex("#EDEAE4");
    static readonly Color BORDER     = Hex("#D4CFC8");

    // 브랜드
    static readonly Color NAVY       = Hex("#1B3A6B");
    static readonly Color NAVY_LIGHT = Hex("#2C5282");
    static readonly Color FOREST     = Hex("#3D6B4F");
    static readonly Color GOLD       = Hex("#9A7228");

    // 텍스트
    static readonly Color TEXT_H     = Hex("#1A1A1A");
    static readonly Color TEXT_MAIN  = Hex("#333333");
    static readonly Color TEXT_SUB   = Hex("#888888");
    static readonly Color TEXT_HINT  = Hex("#BBBBBB");

    // 버튼
    static readonly Color BTN_PRI    = Hex("#1B3A6B");
    static readonly Color BTN_SEC    = Hex("#3D6B4F");
    static readonly Color BTN_DANGER = Hex("#B03030");
    static readonly Color BTN_GHOST  = Hex("#E8E4DC");

    // 축
    static readonly Color COL_X = Hex("#C0392B");
    static readonly Color COL_Y = Hex("#27AE60");
    static readonly Color COL_Z = Hex("#2980B9");

    const string ASSET_PATH  = "HanokAssets";
    const int    THUMB_LAYER = 31;

    // ── 생명주기 ──────────────────────────────────────────
    void Start()
    {
        if (koreanFont == null)
            koreanFont = Resources.Load<TMP_FontAsset>("NotoSansKR-Regular SDF")
                      ?? Resources.Load<TMP_FontAsset>("MalgunGothic SDF");

        // 카메라 컨트롤러 자동 추가
        if (Camera.main != null &&
            Camera.main.GetComponent<HanokCameraController>() == null)
            Camera.main.gameObject.AddComponent<HanokCameraController>();

        BuildUI();
        LoadAssets();
        StartCoroutine(ForceLayout());
    }

    void Update()
    {
        SyncTransformInputs();
        HandleViewportClick();
    }

    void OnDestroy()
    {
        if (_thumbCam != null) Destroy(_thumbCam.gameObject);
    }

    IEnumerator ForceLayout()
    {
        yield return null; yield return null;
        foreach (var f in FindObjectsByType<ContentSizeFitter>(FindObjectsSortMode.None))
            LayoutRebuilder.ForceRebuildLayoutImmediate(f.GetComponent<RectTransform>());
    }

    // ── 툴 전환 ───────────────────────────────────────────
    public void SetTool(EditTool tool)
    {
        currentTool = tool;
        RefreshToolBtns();
    }

    void RefreshToolBtns()
    {
        if (toolBtns == null) return;
        for (int i = 0; i < toolBtns.Length; i++)
        {
            var img = toolBtns[i].GetComponent<Image>();
            img.color = (i == (int)currentTool) ? NAVY : BTN_GHOST;
            var txt = toolBtns[i].GetComponentInChildren<TMP_Text>();
            if (txt) txt.color = (i == (int)currentTool) ? Color.white : TEXT_SUB;
        }
    }

    // ── 에셋 배치 ─────────────────────────────────────────
    public void Spawn(GameObject prefab)
    {
        var obj = Instantiate(prefab, GetSpawnPos(), Quaternion.identity);
        obj.name = prefab.name;

        // FBX Scale Factor 100 자동 보정 (단위: cm → m)
        if (obj.transform.localScale.magnitude > 50f)
            obj.transform.localScale = Vector3.one;

        EnsureCollider(obj);
        AttachSelectable(obj);
        SelectObject(obj);

        // 배치 즉시 카메라 포커스
        var camCtrl = Camera.main?.GetComponent<HanokCameraController>();
        camCtrl?.FocusSelected();
    }

    Vector3 GetSpawnPos()
    {
        if (Camera.main == null) return Vector3.zero;
        var ray = Camera.main.ScreenPointToRay(
            new Vector3(Screen.width * .5f, Screen.height * .5f));
        if (Physics.Raycast(ray, out RaycastHit hit, 500f)) return hit.point;
        var p = Camera.main.transform.position + Camera.main.transform.forward * 10f;
        p.y = 0f; return p;
    }

    void EnsureCollider(GameObject obj)
    {
        if (obj.GetComponentInChildren<Collider>() != null) return;
        var col = obj.AddComponent<BoxCollider>();
        var rs  = obj.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return;
        var b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        col.center = obj.transform.InverseTransformPoint(b.center);
        col.size   = obj.transform.InverseTransformVector(b.size);
    }

    void AttachSelectable(GameObject root)
    {
        if (!root.GetComponent<SelectableAsset>())
            root.AddComponent<SelectableAsset>().Init(this, root);
        foreach (var col in root.GetComponentsInChildren<Collider>())
            if (!col.gameObject.GetComponent<SelectableAsset>())
                col.gameObject.AddComponent<SelectableAsset>().Init(this, root);
    }

    // ── 선택 ─────────────────────────────────────────────
    public GameObject GetSelectedObject() => selectedObject;

    public void SelectObject(GameObject obj)
    {
        // 이전 선택 하이라이트 제거
        if (selectedObject != null)
        {
            var prev = selectedObject.GetComponent<HanokSelectionHighlight>();
            if (prev != null) prev.Hide();
        }

        selectedObject = obj;

        // 새 선택 하이라이트 적용
        if (obj != null)
        {
            var hl = obj.GetComponent<HanokSelectionHighlight>();
            if (hl == null) hl = obj.AddComponent<HanokSelectionHighlight>();
            hl.Show();
        }

        RefreshInfoPanel();
        if (obj != null) ForceSyncTransform();
    }

    void RefreshInfoPanel()
    {
        if (infoNameText == null) return;
        bool has = selectedObject != null;
        infoNameText.text  = has ? selectedObject.name : "부재를 선택하세요";
        infoNameText.color = has ? TEXT_H : TEXT_HINT;
    }

    // ── Transform 동기화 ──────────────────────────────────
    void SyncTransformInputs()
    {
        if (selectedObject == null || posX == null) return;
        var t = selectedObject.transform;
        if (!posX.isFocused)  posX.SetTextWithoutNotify(t.position.x.ToString("F2"));
        if (!posY.isFocused)  posY.SetTextWithoutNotify(t.position.y.ToString("F2"));
        if (!posZ.isFocused)  posZ.SetTextWithoutNotify(t.position.z.ToString("F2"));
        if (!rotX.isFocused)  rotX.SetTextWithoutNotify(t.eulerAngles.x.ToString("F1"));
        if (!rotY.isFocused)  rotY.SetTextWithoutNotify(t.eulerAngles.y.ToString("F1"));
        if (!rotZ.isFocused)  rotZ.SetTextWithoutNotify(t.eulerAngles.z.ToString("F1"));
        if (!scaleF.isFocused) scaleF.SetTextWithoutNotify(t.localScale.x.ToString("F2"));
    }

    void ForceSyncTransform()
    {
        if (selectedObject == null || posX == null) return;
        var t = selectedObject.transform;
        posX.SetTextWithoutNotify(t.position.x.ToString("F2"));
        posY.SetTextWithoutNotify(t.position.y.ToString("F2"));
        posZ.SetTextWithoutNotify(t.position.z.ToString("F2"));
        rotX.SetTextWithoutNotify(t.eulerAngles.x.ToString("F1"));
        rotY.SetTextWithoutNotify(t.eulerAngles.y.ToString("F1"));
        rotZ.SetTextWithoutNotify(t.eulerAngles.z.ToString("F1"));
        scaleF.SetTextWithoutNotify(t.localScale.x.ToString("F2"));
    }

    // ── Transform 적용 ───────────────────────────────────
    public void ApplyPos()
    {
        if (!selectedObject) return;
        selectedObject.transform.position =
            new Vector3(Pf(posX.text), Pf(posY.text), Pf(posZ.text));
    }

    public void ApplyRot()
    {
        if (!selectedObject) return;
        selectedObject.transform.eulerAngles =
            new Vector3(Pf(rotX.text), Pf(rotY.text), Pf(rotZ.text));
    }

    public void ApplyScale()
    {
        if (!selectedObject) return;
        float s = Mathf.Max(0.001f, Pf(scaleF.text));
        selectedObject.transform.localScale = Vector3.one * s;
    }

    public void QuickRot(float deg)
    {
        if (selectedObject) selectedObject.transform.Rotate(0, deg, 0, Space.World);
    }

    public void ResetRot()
    {
        if (selectedObject) selectedObject.transform.eulerAngles = Vector3.zero;
    }

    public void SetScale(float s)
    {
        if (!selectedObject) return;
        selectedObject.transform.localScale = Vector3.one * s;
        scaleF?.SetTextWithoutNotify(s.ToString("F2"));
    }

    public void Duplicate()
    {
        if (!selectedObject) return;
        var c = Instantiate(selectedObject);
        c.name = selectedObject.name + "_복사";
        c.transform.position += Vector3.right * 2f;
        AttachSelectable(c);
        SelectObject(c);
    }

    public void DeleteSelected()
    {
        if (!selectedObject) return;
        Destroy(selectedObject);
        selectedObject = null;
        RefreshInfoPanel();
    }

    public void ClearSelection()
    {
        if (selectedObject != null)
        {
            var hl = selectedObject.GetComponent<HanokSelectionHighlight>();
            if (hl != null) hl.Hide();
        }
        selectedObject = null;
        RefreshInfoPanel();
    }

    // ── 뷰포트 클릭 ──────────────────────────────────────
    void HandleViewportClick()
    {
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es != null && es.IsPointerOverGameObject()) return;
        if (Camera.main == null) return;

        if (currentTool == EditTool.Delete)
        {
            var ray2 = Camera.main.ScreenPointToRay((Vector3)mouse.position.ReadValue());
            if (Physics.Raycast(ray2, out RaycastHit h2, 1000f))
            {
                var sa2 = h2.collider.GetComponent<SelectableAsset>();
                if (sa2 != null) { SelectObject(sa2.Root); DeleteSelected(); }
            }
            return;
        }

        var ray = Camera.main.ScreenPointToRay((Vector3)mouse.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            var sa = hit.collider.GetComponent<SelectableAsset>();
            if (sa != null) SelectObject(sa.Root);
        }
    }

    // ── 유틸 ─────────────────────────────────────────────
    static float Pf(string s) =>
        float.TryParse(s,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out float v) ? v : 0f;

    void KorFont(TMP_Text t)  { if (koreanFont) t.font = koreanFont; }

    TMP_FontAsset _lat;
    TMP_FontAsset LatinFont =>
        _lat ??= Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF")
              ?? TMP_Settings.defaultFontAsset;
    void LatFont(TMP_Text t)  { var f = LatinFont; if (f) t.font = f; }

    static bool HasKorean(string s)
    {
        foreach (char c in s)
            if (c >= '가' && c <= '힣') return true;
        return false;
    }
}
