using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HanokUIManager — 왼쪽 에셋 목록 (partial)
/// 심플 구조: 탭 → 아이템을 assetContent에 직접 추가 (중첩 레이아웃 없음)
/// </summary>
public partial class HanokUIManager
{
    Camera _thumbCam;

    static readonly string[] CATEGORIES = { "구조 모듈", "전통 소품", "전통문양" };

    int              _curCat  = 0;
    Button[]         _catBtns;
    GameObject       _tabsGO;              // 탭 행 참조 (삭제 보호)
    List<GameObject> _allPrefabs = new();

    const float CELL_W = 76f;
    const float CELL_H = 88f;
    const int   COLS   = 3;

    // ── 에셋 로드 ─────────────────────────────────────────
    void LoadAssets()
    {
        if (assetContent == null)
        {
            Debug.LogError("[HanokBuilder] assetContent null");
            return;
        }

        _tabsGO = BuildCategoryTabs(assetContent);

        var raw = Resources.LoadAll(ASSET_PATH);
        foreach (var o in raw)
            if (o is GameObject g) _allPrefabs.Add(g);
        _allPrefabs.Sort((a, b) =>
            string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));

        Debug.Log($"[HanokBuilder] {_allPrefabs.Count}개 에셋 로드");
        RefreshAssetList();
    }

    // ── 카테고리 탭 ───────────────────────────────────────
    // 반환: 탭 행 GameObject (삭제 시 보호용)
    GameObject BuildCategoryTabs(Transform parent)
    {
        var tabRow = new GameObject("Tabs");
        tabRow.transform.SetParent(parent, false);
        var le = tabRow.AddComponent<LayoutElement>();
        le.preferredHeight = 36; le.flexibleWidth = 1;
        tabRow.AddComponent<Image>().color = Hex("#E8E4DC");
        var hlg = tabRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 0;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        _catBtns = new Button[CATEGORIES.Length];
        for (int i = 0; i < CATEGORIES.Length; i++)
        {
            int idx = i;
            var go = new GameObject("Cat_" + i);
            go.transform.SetParent(tabRow.transform, false);
            go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = (i == 0) ? NAVY : Hex("#E8E4DC");
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var cs = btn.colors;
            cs.highlightedColor = Hex("#D0CCC4");
            btn.colors = cs;
            btn.onClick.AddListener(() => ShowCategory(idx));
            _catBtns[i] = btn;

            var tgo = new GameObject("T");
            tgo.transform.SetParent(go.transform, false);
            var tRT = tgo.AddComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
            tRT.offsetMin = tRT.offsetMax = Vector2.zero;
            var t = tgo.AddComponent<TextMeshProUGUI>();
            t.text = CATEGORIES[i]; t.fontSize = 10;
            t.color = (i == 0) ? Color.white : TEXT_SUB;
            t.alignment = TextAlignmentOptions.Center;
            KorFont(t);
        }
        return tabRow;
    }

    // ── 카테고리 전환 ─────────────────────────────────────
    void ShowCategory(int idx)
    {
        _curCat = idx;
        UpdateTabColors();
        RefreshAssetList();
    }

    void UpdateTabColors()
    {
        if (_catBtns == null) return;
        for (int i = 0; i < _catBtns.Length; i++)
        {
            _catBtns[i].GetComponent<Image>().color =
                (i == _curCat) ? NAVY : Hex("#E8E4DC");
            var txt = _catBtns[i].GetComponentInChildren<TMP_Text>();
            if (txt) txt.color = (i == _curCat) ? Color.white : TEXT_SUB;
        }
    }

    // ── 에셋 목록 갱신 ────────────────────────────────────
    // 탭 이외의 자식을 즉시(DestroyImmediate) 삭제 후 재생성
    void RefreshAssetList()
    {
        if (assetContent == null) return;

        // 탭 제외한 기존 아이템 즉시 삭제 (Destroy는 프레임 끝 처리라 레이아웃 혼선)
        var children = new List<GameObject>();
        foreach (Transform ch in assetContent)
            if (ch.gameObject != _tabsGO) children.Add(ch.gameObject);
        // 삭제 전 RenderTexture GPU 메모리 해제
        foreach (var ch in children)
            foreach (var ri in ch.GetComponentsInChildren<RawImage>())
                if (ri.texture is RenderTexture oldRt) { oldRt.Release(); Destroy(oldRt); }
        foreach (var ch in children) DestroyImmediate(ch);

        if (_allPrefabs.Count == 0) { AddEmptyMsg(); return; }

        // 현재 카테고리명 (서브카테고리 필터링은 향후 구현)
        AddSectionLabel(CATEGORIES[_curCat]);

        // 3열 그리드 행 구성
        for (int i = 0; i < _allPrefabs.Count; i += COLS)
        {
            var row = new GameObject("Row");
            row.transform.SetParent(assetContent, false);

            // RectTransform 먼저 추가 → LayoutElement 추가
            row.AddComponent<RectTransform>();
            var rle = row.AddComponent<LayoutElement>();
            rle.preferredHeight = CELL_H + 4f;
            rle.flexibleWidth   = 1;
            row.AddComponent<Image>().color = Color.clear;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4;
            hlg.padding = new RectOffset(8, 8, 2, 2);
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth  = false;

            for (int j = 0; j < COLS; j++)
            {
                int pi = i + j;
                if (pi < _allPrefabs.Count)
                {
                    var cap    = _allPrefabs[pi];
                    var rawImg = MakeGridCell(row.transform, cap.name, () => Spawn(cap));
                    StartCoroutine(CaptureThumbnail(cap, rawImg));
                }
                else
                {
                    // 빈 칸 (3열 정렬 맞춤)
                    var blank = new GameObject("Blank");
                    blank.transform.SetParent(row.transform, false);
                    blank.AddComponent<RectTransform>();
                    blank.AddComponent<LayoutElement>().preferredWidth = CELL_W;
                    blank.AddComponent<Image>().color = Color.clear;
                }
            }
        }

        // 레이아웃 강제 재계산
        StartCoroutine(RebuildNext());
    }

    IEnumerator RebuildNext()
    {
        yield return null;
        if (assetContent != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                assetContent.GetComponent<RectTransform>());
    }

    void AddSectionLabel(string text)
    {
        var go = new GameObject("SecLbl");
        go.transform.SetParent(assetContent, false);
        go.AddComponent<RectTransform>();
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 28; le.flexibleWidth = 1;
        go.AddComponent<Image>().color = Hex("#E4E0D8");
        var tgo = new GameObject("T"); tgo.transform.SetParent(go.transform, false);
        var tRT = tgo.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(12, 0); tRT.offsetMax = Vector2.zero;
        var t = tgo.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = 9; t.fontStyle = FontStyles.Bold;
        t.color = TEXT_SUB; t.alignment = TextAlignmentOptions.Left;
        KorFont(t);
    }

    void AddEmptyMsg()
    {
        var go = new GameObject("Empty");
        go.transform.SetParent(assetContent, false);
        go.AddComponent<RectTransform>();
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 100; le.flexibleWidth = 1;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = "Resources/HanokAssets\n폴더에 Prefab을 넣으세요";
        t.fontSize = 10; t.color = TEXT_SUB;
        t.alignment = TextAlignmentOptions.Center;
        KorFont(t);
    }

    // ── 그리드 셀 ─────────────────────────────────────────
    RawImage MakeGridCell(Transform parent, string label, System.Action onClick)
    {
        var go = new GameObject("Cell");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = CELL_W; le.preferredHeight = CELL_H;
        var img = go.AddComponent<Image>();
        img.color = BG_CARD;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = BORDER; outline.effectDistance = new Vector2(1, -1);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cs = btn.colors;
        cs.normalColor      = BG_CARD;
        cs.highlightedColor = Hex("#E4E0D8");
        cs.pressedColor     = Hex("#D4D0C8");
        btn.colors = cs;
        btn.onClick.AddListener(() => onClick?.Invoke());

        // 썸네일 (상단 72%)
        var thumb = new GameObject("Thumb");
        thumb.transform.SetParent(go.transform, false);
        var tRT = thumb.AddComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0, 0.26f); tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(3, 0); tRT.offsetMax = new Vector2(-3, -3);
        var raw = thumb.AddComponent<RawImage>();
        raw.color = Hex("#D8D4CC");

        // 이름 라벨 (하단 26%)
        var ngo = new GameObject("Name");
        ngo.transform.SetParent(go.transform, false);
        var nRT = ngo.AddComponent<RectTransform>();
        nRT.anchorMin = Vector2.zero; nRT.anchorMax = new Vector2(1, 0.26f);
        nRT.offsetMin = new Vector2(2, 2); nRT.offsetMax = new Vector2(-2, 0);
        var t = ngo.AddComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = 8; t.color = TEXT_MAIN;
        t.alignment = TextAlignmentOptions.Center;
        t.overflowMode = TextOverflowModes.Ellipsis;
        t.enableWordWrapping = false;
        LatFont(t);

        return raw;
    }

    // ── RenderTexture 썸네일 ──────────────────────────────
    IEnumerator CaptureThumbnail(GameObject prefab, RawImage target)
    {
        yield return null;
        if (target == null) yield break;
        EnsureThumbCam();

        const float FAR = 8000f;
        var inst = Instantiate(prefab, new Vector3(FAR, 0f, FAR), Quaternion.identity);
        inst.hideFlags = HideFlags.HideAndDontSave;
        SetLayerAll(inst, THUMB_LAYER);

        // FBX 재질 색상 보존: 깨진 셰이더를 URP/Lit으로 교체하면서 원본 색상 유지
        FixMaterialColors(inst);

        var rends = inst.GetComponentsInChildren<Renderer>();
        Bounds bounds;
        if (rends.Length > 0)
        {
            bounds = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) bounds.Encapsulate(rends[i].bounds);
        }
        else bounds = new Bounds(inst.transform.position, Vector3.one * 2f);

        // 아이소메트릭 스타일 각도 (약간 옆+위)
        float dist = Mathf.Max(bounds.size.magnitude * 1.35f, 0.5f);
        _thumbCam.transform.position =
            bounds.center + new Vector3(1.1f, 0.85f, -1.05f).normalized * dist;
        _thumbCam.transform.LookAt(bounds.center);

        // 해상도 128×128 + 4x MSAA (선명도 개선)
        var rt = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4;
        _thumbCam.targetTexture = rt;
        _thumbCam.Render();
        _thumbCam.targetTexture = null;

        if (target != null) { target.texture = rt; target.color = Color.white; }
        else { rt.Release(); Destroy(rt); } // target 소멸 시 GPU 메모리 즉시 해제
        Destroy(inst);
    }

    // FBX 재질 색상 보존 — Standard → URP 변환 시 색상·텍스처 유지
    static void FixMaterialColors(GameObject obj)
    {
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) return; // URP 없으면 스킵

        foreach (var r in obj.GetComponentsInChildren<Renderer>())
        {
            // sharedMaterial 로 깨진 셰이더 체크 (인스턴스 생성 최소화)
            bool needFix = false;
            foreach (var sm in r.sharedMaterials)
            {
                if (sm == null) continue;
                var sn = sm.shader?.name ?? "";
                if (sn == "Hidden/InternalErrorShader" || sn == "Standard" || sn == "")
                    needFix = true;
            }
            if (!needFix) continue;

            // 인스턴스 생성 후 수정 (원본 프리팹 재질 건드리지 않음)
            var mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m == null) continue;
                var sn = m.shader?.name ?? "";
                if (sn != "Hidden/InternalErrorShader" && sn != "Standard" && sn != "") continue;

                // 기존 색상·텍스처 추출
                Color col = m.HasProperty("_Color")   ? m.GetColor("_Color")     : Color.white;
                Texture tx = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;

                m.shader = urpLit;
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
                if (m.HasProperty("_Color"))     m.SetColor("_Color",     col);
                if (tx != null)
                {
                    if (m.HasProperty("_BaseMap"))  m.SetTexture("_BaseMap",  tx);
                    if (m.HasProperty("_MainTex"))  m.SetTexture("_MainTex",  tx);
                }
            }
        }
    }

    void EnsureThumbCam()
    {
        if (_thumbCam != null) return;
        var go = new GameObject("_HanokThumbCam");
        go.hideFlags  = HideFlags.HideAndDontSave;
        _thumbCam     = go.AddComponent<Camera>();
        _thumbCam.enabled          = false;
        _thumbCam.clearFlags       = CameraClearFlags.SolidColor;
        _thumbCam.backgroundColor  = Hex("#EEE8DC"); // 따뜻한 한지 계열 배경
        _thumbCam.fieldOfView      = 28f;            // 좁은 FOV (왜곡 최소화)
        _thumbCam.nearClipPlane    = 0.05f;
        _thumbCam.farClipPlane     = 20000f;
        _thumbCam.cullingMask      = 1 << THUMB_LAYER;
        _thumbCam.allowMSAA        = true;
    }

    static void SetLayerAll(GameObject root, int layer)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = layer;
    }
}
