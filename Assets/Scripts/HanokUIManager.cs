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

    // ── 드래그 상태 ──────────────────────────────────────
    bool    _isDragging;
    bool    _pendingDrag;
    Vector2 _dragStartMouse;
    Vector3 _dragOffset;
    Plane   _dragPlane;

    // ── 회전 기즈모 ──────────────────────────────────────
    HanokRotationGizmo _rotGizmo;

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

    const string ASSET_PATH     = "HanokAssets";
    const string CATEGORY_PATH  = "HanokCategories";
    const string ASSETINFO_PATH = "HanokAssetInfo";
    const int    THUMB_LAYER = 31;

    // ── 생명주기 ──────────────────────────────────────────
    void Start()
    {
        if (koreanFont == null)
            koreanFont = Resources.Load<TMP_FontAsset>("NotoSansKR-Regular SDF")
                      ?? Resources.Load<TMP_FontAsset>("MalgunGothic SDF");

        // 씬 환경 초기화 (바닥·조명·카메라 배경)
        HanokSceneSetup.Setup();

        // 카메라 컨트롤러 자동 추가
        if (Camera.main != null &&
            Camera.main.GetComponent<HanokCameraController>() == null)
            Camera.main.gameObject.AddComponent<HanokCameraController>();

        // 회전 기즈모 생성
        _rotGizmo = gameObject.AddComponent<HanokRotationGizmo>();

        BuildUI();
        LoadAssets();
        StartCoroutine(ForceLayout());
    }

    void Update()
    {
        SyncTransformInputs();
        HandleViewportClick();
        HandleKeyboardShortcuts();
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
        SyncGizmo();
    }

    void SyncGizmo()
    {
        if (_rotGizmo == null) return;
        if (currentTool == EditTool.Rotate && selectedObject != null)
            _rotGizmo.Attach(selectedObject);
        else
            _rotGizmo.Detach();
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
        var obj = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        obj.name = prefab.name;

        // FBX Scale Factor 100 자동 보정 (단위: cm → m)
        if (obj.transform.localScale.magnitude > 50f)
            obj.transform.localScale = Vector3.one;

        EnsureCollider(obj);

        // 바닥 위에 올바르게 배치 (피벗이 중심인 모델 대응)
        obj.transform.position = GetSpawnPos();
        PlaceOnFloor(obj);

        AttachSelectable(obj);
        SelectObject(obj);

        // 배치 즉시 스무스 카메라 포커스
        var camCtrl = Camera.main?.GetComponent<HanokCameraController>();
        camCtrl?.FocusObject(obj);
    }

    // 모델 바닥면이 Y=0(바닥 평면) 위에 오도록 위치 보정
    void PlaceOnFloor(GameObject obj)
    {
        var rends = obj.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return;
        var b = rends[0].bounds;
        foreach (var r in rends) b.Encapsulate(r.bounds);
        float offset = obj.transform.position.y - b.min.y;
        obj.transform.position += Vector3.up * offset;
    }

    Vector3 GetSpawnPos()
    {
        // 카메라가 현재 바라보는 피벗 XZ 위치에 배치 → 항상 화면 중앙에 나타남
        var camCtrl = Camera.main?.GetComponent<HanokCameraController>();
        if (camCtrl != null)
        {
            var pivot = camCtrl.Pivot;
            // 피벗 위에서 아래로 레이캐스트해 바닥면 정확히 찍기
            var ray = new Ray(pivot + Vector3.up * 50f, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit h, 200f))
                return h.point;
            return new Vector3(pivot.x, 0f, pivot.z);
        }
        if (Camera.main == null) return Vector3.zero;
        var r2 = Camera.main.ScreenPointToRay(
            new Vector3(Screen.width * .5f, Screen.height * .5f));
        if (Physics.Raycast(r2, out RaycastHit hit2, 500f)) return hit2.point;
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
        SyncGizmo();

        // 선택 시 카메라 피벗을 오브젝트 방향으로 부드럽게 이동
        // (오비트가 선택한 오브젝트 주변으로 자연스럽게 전환됨)
        if (obj != null)
            Camera.main?.GetComponent<HanokCameraController>()
                        ?.ShiftPivotToward(obj.transform.position);
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

    // ── 뷰포트 클릭 / 드래그 ─────────────────────────────
    void HandleViewportClick()
    {
        var mouse = Mouse.current;
        if (mouse == null || Camera.main == null) return;

        bool overUI = UnityEngine.EventSystems.EventSystem.current != null &&
                      UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

        Vector2 mp    = mouse.position.ReadValue();
        Vector2 mDelta = mouse.delta.ReadValue();

        // ═══════════════════════════════════════════════════
        // SELECT 모드: 클릭=선택, 클릭+드래그=자유 이동
        // ═══════════════════════════════════════════════════
        if (currentTool == EditTool.Select)
        {
            if (mouse.leftButton.wasPressedThisFrame && !overUI)
            {
                _isDragging  = false;
                _pendingDrag = false;
                var ray = Camera.main.ScreenPointToRay((Vector3)mp);
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
                {
                    var sa = hit.collider.GetComponent<SelectableAsset>();
                    if (sa != null)
                    {
                        SelectObject(sa.Root);
                        // 드래그 대기 상태로 전환 (5px 이상 움직이면 드래그 시작)
                        _pendingDrag    = true;
                        _dragStartMouse = mp;
                        _dragPlane      = MakeDragPlane(sa.Root.transform.position);
                        if (_dragPlane.Raycast(ray, out float e))
                            _dragOffset = sa.Root.transform.position - ray.GetPoint(e);
                        else _dragOffset = Vector3.zero;
                    }
                    else ClearSelection();
                }
                else ClearSelection();
            }

            // 드래그 시작 판정 (5px 임계값)
            if (_pendingDrag && !_isDragging && mouse.leftButton.isPressed &&
                Vector2.Distance(mp, _dragStartMouse) > 5f)
            {
                _isDragging  = true;
                _pendingDrag = false;
            }

            // 드래그 적용
            if (_isDragging && selectedObject != null && mouse.leftButton.isPressed)
            {
                var ray = Camera.main.ScreenPointToRay((Vector3)mp);
                if (_dragPlane.Raycast(ray, out float e))
                    selectedObject.transform.position = ray.GetPoint(e) + _dragOffset;
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            { _isDragging = false; _pendingDrag = false; }
            return;
        }

        // ═══════════════════════════════════════════════════
        // MOVE 모드: 오브젝트 클릭→드래그 이동 (뷰 인식 평면)
        // ═══════════════════════════════════════════════════
        if (currentTool == EditTool.Move)
        {
            if (mouse.leftButton.wasPressedThisFrame && !overUI)
            {
                _isDragging = false;
                var ray = Camera.main.ScreenPointToRay((Vector3)mp);
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
                {
                    var sa = hit.collider.GetComponent<SelectableAsset>();
                    if (sa != null)
                    {
                        SelectObject(sa.Root);
                        _isDragging = true;
                        _dragPlane  = MakeDragPlane(sa.Root.transform.position);
                        if (_dragPlane.Raycast(ray, out float e))
                            _dragOffset = sa.Root.transform.position - ray.GetPoint(e);
                        else _dragOffset = Vector3.zero;
                    }
                }
            }
            if (mouse.leftButton.isPressed && _isDragging && selectedObject != null)
            {
                var ray = Camera.main.ScreenPointToRay((Vector3)mp);
                if (_dragPlane.Raycast(ray, out float e))
                    selectedObject.transform.position = ray.GetPoint(e) + _dragOffset;
            }
            if (mouse.leftButton.wasReleasedThisFrame) _isDragging = false;
            return;
        }

        // ═══════════════════════════════════════════════════
        // ROTATE 모드: 기즈모(X/Y/Z 링) 드래그 또는 오브젝트 클릭→선택
        // ═══════════════════════════════════════════════════
        if (currentTool == EditTool.Rotate)
        {
            // 기즈모가 마우스를 소비 중이면 UIManager는 처리 안 함
            if (_rotGizmo != null && _rotGizmo.IsConsuming)
                return;

            // 오브젝트 클릭으로 선택 (빈 공간 클릭은 선택 유지)
            if (mouse.leftButton.wasPressedThisFrame && !overUI)
            {
                var ray = Camera.main.ScreenPointToRay((Vector3)mp);
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
                {
                    var sa = hit.collider.GetComponent<SelectableAsset>();
                    if (sa != null) SelectObject(sa.Root);
                }
            }
            return;
        }

        // ═══════════════════════════════════════════════════
        // DELETE 모드: 좌클릭 즉시 삭제
        // ═══════════════════════════════════════════════════
        if (currentTool == EditTool.Delete)
        {
            if (!mouse.leftButton.wasPressedThisFrame || overUI) return;
            var ray = Camera.main.ScreenPointToRay((Vector3)mp);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                var sa = hit.collider.GetComponent<SelectableAsset>();
                if (sa != null) { SelectObject(sa.Root); DeleteSelected(); }
            }
        }
    }

    // 현재 카메라 뷰에 맞는 드래그 평면을 반환
    // Top/3D → XZ 수평면 | Front/Back → XY 수직면 | Right/Left → ZY 수직면
    Plane MakeDragPlane(Vector3 objPos)
    {
        var cam = Camera.main?.GetComponent<HanokCameraController>();
        if (cam == null) return new Plane(Vector3.up, objPos);

        switch (cam.CurrentPreset)
        {
            case HanokCameraController.ViewPreset.Front:
            case HanokCameraController.ViewPreset.Back:
                return new Plane(Vector3.forward, objPos); // XY 평면

            case HanokCameraController.ViewPreset.Right:
            case HanokCameraController.ViewPreset.Left:
                return new Plane(Vector3.right, objPos);   // ZY 평면

            default: // Perspective, Top
                return new Plane(Vector3.up, objPos);      // XZ 평면
        }
    }

    // ── 키보드 단축키 ─────────────────────────────────────
    void HandleKeyboardShortcuts()
    {
        var kb = Keyboard.current;
        if (kb == null || AnyInputFocused()) return;

        if (kb.digit1Key.wasPressedThisFrame) SetTool(EditTool.Select);
        if (kb.digit2Key.wasPressedThisFrame) SetTool(EditTool.Move);
        if (kb.digit3Key.wasPressedThisFrame) SetTool(EditTool.Rotate);
        if (kb.digit4Key.wasPressedThisFrame) SetTool(EditTool.Delete);
        if (kb.deleteKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame)
            DeleteSelected();
        if (kb.escapeKey.wasPressedThisFrame)
        { ClearSelection(); SetTool(EditTool.Select); }
        if (kb.homeKey.wasPressedThisFrame)
            Camera.main?.GetComponent<HanokCameraController>()?.ResetView();
    }

    bool AnyInputFocused()
    {
        var sel = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
        return sel != null && sel.GetComponent<TMPro.TMP_InputField>() != null;
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
