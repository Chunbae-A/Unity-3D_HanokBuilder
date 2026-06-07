using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Left asset library panel for HanokUIManager.
/// Loads prefab entries from Resources/HanokAssets and filters them by folder category.
/// </summary>
public partial class HanokUIManager
{
    class HanokAssetEntry
    {
        public GameObject prefab;
        public string mainCategory;
        public string subCategory;

        public HanokAssetEntry(GameObject prefab, string mainCategory, string subCategory)
        {
            this.prefab = prefab;
            this.mainCategory = mainCategory;
            this.subCategory = subCategory;
        }
    }

    struct HanokSubCategory
    {
        public string key;
        public string label;
        public string path;

        public HanokSubCategory(string key, string label, string path)
        {
            this.key = key;
            this.label = label;
            this.path = path;
        }
    }

    Camera _thumbCam;

    const string CAT_ALL = "All";
    const string CAT_COMPLETE = "Complete";
    const string CAT_PARTS = "Parts";

    static readonly (string key, string label)[] MAIN_FILTERS =
    {
        (CAT_ALL, "전체"),
        (CAT_COMPLETE, "완성형"),
        (CAT_PARTS, "부품형"),
    };

    static readonly HanokSubCategory[] PART_FILTERS =
    {
        new HanokSubCategory(CAT_ALL, "전체", ""),
        new HanokSubCategory("Beam", "보", "Beam"),
        new HanokSubCategory("Dancheong", "단청", "Dancheong"),
        new HanokSubCategory("Decoration", "장식", "Decoration"),
        new HanokSubCategory("Door", "문", "Door"),
        new HanokSubCategory("Floor", "바닥", "Floor"),
        new HanokSubCategory("Handrail", "난간", "Handrail"),
        new HanokSubCategory("Maru", "마루", "Maru"),
        new HanokSubCategory("Natural", "자연", "Natural"),
        new HanokSubCategory("Roof", "지붕", "Roof"),
        new HanokSubCategory("Wall", "벽체", "Wall"),
        new HanokSubCategory("Wood", "목재", "Wood"),
    };

    string _selectedMain = CAT_ALL;
    string _selectedSub = CAT_ALL;
    Button[] _mainFilterBtns;
    Button[] _partFilterBtns;
    GameObject _mainFilterGO;
    GameObject _partFilterGO;
    readonly List<HanokAssetEntry> _assetEntries = new List<HanokAssetEntry>();

    const float CELL_W = 76f;
    const float CELL_H = 88f;
    const int COLS = 3;

    void LoadAssets()
    {
        if (assetContent == null)
        {
            Debug.LogError("[HanokBuilder] assetContent null");
            return;
        }

        BuildCategoryTabs(assetContent);
        LoadCategory($"{ASSET_PATH}/Complete", CAT_COMPLETE, CAT_ALL);

        foreach (var cat in PART_FILTERS)
        {
            if (cat.key == CAT_ALL) continue;
            LoadCategory($"{ASSET_PATH}/Parts/{cat.path}", CAT_PARTS, cat.key);
        }

        _assetEntries.Sort((a, b) =>
            string.Compare(a.prefab.name, b.prefab.name, System.StringComparison.OrdinalIgnoreCase));

        Debug.Log($"[HanokBuilder] {_assetEntries.Count} assets loaded");
        RefreshAssetList();
    }

    void LoadCategory(string resourcePath, string mainCategory, string subCategory)
    {
        var raw = Resources.LoadAll(resourcePath);
        foreach (var o in raw)
        {
            if (o is GameObject g)
                _assetEntries.Add(new HanokAssetEntry(g, mainCategory, subCategory));
        }
    }

    void BuildCategoryTabs(Transform parent)
    {
        _mainFilterGO = BuildFilterRow(parent, "MainFilters", 36f);
        _mainFilterBtns = new Button[MAIN_FILTERS.Length];
        for (int i = 0; i < MAIN_FILTERS.Length; i++)
        {
            var filter = MAIN_FILTERS[i];
            var key = filter.key;
            _mainFilterBtns[i] = MakeFilterButton(_mainFilterGO.transform, filter.label,
                () => ShowMainCategory(key));
        }

        _partFilterGO = BuildFilterGrid(parent, "PartFilters", 88f);
        _partFilterBtns = new Button[PART_FILTERS.Length];
        for (int i = 0; i < PART_FILTERS.Length; i++)
        {
            var filter = PART_FILTERS[i];
            var key = filter.key;
            _partFilterBtns[i] = MakeFilterButton(_partFilterGO.transform, filter.label,
                () => ShowPartCategory(key));
        }

        UpdateTabColors();
    }

    GameObject BuildFilterRow(Transform parent, string name, float height)
    {
        var row = new GameObject(name);
        row.transform.SetParent(parent, false);
        row.AddComponent<RectTransform>();
        var le = row.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.flexibleWidth = 1;
        row.AddComponent<Image>().color = Hex("#E8E4DC");

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4;
        hlg.padding = new RectOffset(8, 8, 4, 4);
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        return row;
    }

    GameObject BuildFilterGrid(Transform parent, string name, float height)
    {
        var grid = new GameObject(name);
        grid.transform.SetParent(parent, false);
        grid.AddComponent<RectTransform>();

        var le = grid.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.flexibleWidth = 1;

        grid.AddComponent<Image>().color = Hex("#E8E4DC");

        var glg = grid.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(60f, 24f);
        glg.spacing = new Vector2(4f, 4f);
        glg.padding = new RectOffset(8, 8, 6, 6);
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 4;

        return grid;
    }

    Button MakeFilterButton(Transform parent, string label, System.Action onClick)
    {
        var go = new GameObject("Filter_" + label);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();

        var img = go.AddComponent<Image>();
        img.color = BTN_GHOST;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cs = btn.colors;
        cs.highlightedColor = Hex("#D0CCC4");
        cs.pressedColor = NAVY_LIGHT;
        btn.colors = cs;
        btn.onClick.AddListener(() => onClick?.Invoke());

        var tgo = new GameObject("T");
        tgo.transform.SetParent(go.transform, false);
        var tRT = tgo.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = Vector2.zero;
        tRT.offsetMax = Vector2.zero;

        var t = tgo.AddComponent<TextMeshProUGUI>();
        t.text = label;
        t.fontSize = 9;
        t.color = TEXT_SUB;
        t.alignment = TextAlignmentOptions.Center;
        KorFont(t);

        return btn;
    }

    void ShowMainCategory(string key)
    {
        _selectedMain = key;
        _selectedSub = CAT_ALL;
        UpdateTabColors();
        RefreshAssetList();
    }

    void ShowPartCategory(string key)
    {
        _selectedMain = CAT_PARTS;
        _selectedSub = key;
        UpdateTabColors();
        RefreshAssetList();
    }

    void UpdateTabColors()
    {
        if (_mainFilterBtns != null)
        {
            for (int i = 0; i < _mainFilterBtns.Length; i++)
                SetFilterButtonState(_mainFilterBtns[i], MAIN_FILTERS[i].key == _selectedMain);
        }

        bool showParts = _selectedMain == CAT_PARTS;
        if (_partFilterGO != null)
            _partFilterGO.SetActive(showParts);

        if (_partFilterBtns != null)
        {
            for (int i = 0; i < _partFilterBtns.Length; i++)
                SetFilterButtonState(_partFilterBtns[i], PART_FILTERS[i].key == _selectedSub);
        }
    }

    void SetFilterButtonState(Button btn, bool active)
    {
        if (btn == null) return;
        btn.GetComponent<Image>().color = active ? NAVY : BTN_GHOST;

        var txt = btn.GetComponentInChildren<TMP_Text>();
        if (txt != null)
            txt.color = active ? Color.white : TEXT_SUB;
    }

    void RefreshAssetList()
    {
        if (assetContent == null) return;

        var children = new List<GameObject>();
        foreach (Transform ch in assetContent)
        {
            if (ch.gameObject != _mainFilterGO && ch.gameObject != _partFilterGO)
                children.Add(ch.gameObject);
        }
        foreach (var ch in children)
            DestroyImmediate(ch);

        var filtered = GetFilteredAssets();
        if (filtered.Count == 0)
        {
            AddEmptyMsg();
            return;
        }

        AddSectionLabel(GetCurrentCategoryLabel());

        for (int i = 0; i < filtered.Count; i += COLS)
        {
            var row = new GameObject("Row");
            row.transform.SetParent(assetContent, false);
            row.AddComponent<RectTransform>();

            var rle = row.AddComponent<LayoutElement>();
            rle.preferredHeight = CELL_H + 4f;
            rle.flexibleWidth = 1;

            row.AddComponent<Image>().color = Color.clear;

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4;
            hlg.padding = new RectOffset(8, 8, 2, 2);
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth = true;

            for (int j = 0; j < COLS; j++)
            {
                int pi = i + j;
                if (pi < filtered.Count)
                {
                    var prefab = filtered[pi].prefab;
                    var rawImg = MakeGridCell(row.transform, prefab.name, () => Spawn(prefab));
                    StartCoroutine(CaptureThumbnail(prefab, rawImg));
                }
                else
                {
                    var blank = new GameObject("Blank");
                    blank.transform.SetParent(row.transform, false);
                    blank.AddComponent<RectTransform>();
                    var blankLE = blank.AddComponent<LayoutElement>();
                    blankLE.preferredWidth = CELL_W;
                    blankLE.flexibleWidth = 1;
                    blank.AddComponent<Image>().color = Color.clear;
                }
            }
        }

        StartCoroutine(RebuildNext());
    }

    List<HanokAssetEntry> GetFilteredAssets()
    {
        var result = new List<HanokAssetEntry>();
        foreach (var asset in _assetEntries)
        {
            if (_selectedMain != CAT_ALL && asset.mainCategory != _selectedMain)
                continue;

            if (_selectedMain == CAT_PARTS &&
                _selectedSub != CAT_ALL &&
                asset.subCategory != _selectedSub)
                continue;

            result.Add(asset);
        }

        return result;
    }

    string GetCurrentCategoryLabel()
    {
        if (_selectedMain == CAT_ALL)
            return "전체";

        if (_selectedMain == CAT_COMPLETE)
            return "완성형";

        foreach (var filter in PART_FILTERS)
            if (filter.key == _selectedSub)
                return "부품형 / " + filter.label;

        return "부품형";
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
        le.preferredHeight = 28;
        le.flexibleWidth = 1;

        go.AddComponent<Image>().color = Hex("#E4E0D8");

        var tgo = new GameObject("T");
        tgo.transform.SetParent(go.transform, false);
        var tRT = tgo.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(12, 0);
        tRT.offsetMax = Vector2.zero;

        var t = tgo.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = 9;
        t.fontStyle = FontStyles.Bold;
        t.color = TEXT_SUB;
        t.alignment = TextAlignmentOptions.Left;
        KorFont(t);
    }

    void AddEmptyMsg()
    {
        var go = new GameObject("Empty");
        go.transform.SetParent(assetContent, false);
        go.AddComponent<RectTransform>();

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 100;
        le.flexibleWidth = 1;

        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = "Resources/HanokAssets\n폴더에 Prefab을 넣으세요";
        t.fontSize = 10;
        t.color = TEXT_SUB;
        t.alignment = TextAlignmentOptions.Center;
        KorFont(t);
    }

    RawImage MakeGridCell(Transform parent, string label, System.Action onClick)
    {
        var go = new GameObject("Cell");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = CELL_W;
        le.preferredHeight = CELL_H;
        le.flexibleWidth = 1;

        var img = go.AddComponent<Image>();
        img.color = BG_CARD;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = BORDER;
        outline.effectDistance = new Vector2(1, -1);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cs = btn.colors;
        cs.normalColor = BG_CARD;
        cs.highlightedColor = Hex("#E4E0D8");
        cs.pressedColor = Hex("#D4D0C8");
        btn.colors = cs;
        btn.onClick.AddListener(() => onClick?.Invoke());

        var thumb = new GameObject("Thumb");
        thumb.transform.SetParent(go.transform, false);
        var tRT = thumb.AddComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0, 0.26f);
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(3, 0);
        tRT.offsetMax = new Vector2(-3, -3);

        var raw = thumb.AddComponent<RawImage>();
        raw.color = Hex("#D8D4CC");

        var ngo = new GameObject("Name");
        ngo.transform.SetParent(go.transform, false);
        var nRT = ngo.AddComponent<RectTransform>();
        nRT.anchorMin = Vector2.zero;
        nRT.anchorMax = new Vector2(1, 0.26f);
        nRT.offsetMin = new Vector2(2, 2);
        nRT.offsetMax = new Vector2(-2, 0);

        var t = ngo.AddComponent<TextMeshProUGUI>();
        t.text = label;
        t.fontSize = 8;
        t.color = TEXT_MAIN;
        t.alignment = TextAlignmentOptions.Center;
        t.overflowMode = TextOverflowModes.Ellipsis;
        t.enableWordWrapping = false;
        LatFont(t);

        return raw;
    }

    IEnumerator CaptureThumbnail(GameObject prefab, RawImage target)
    {
        yield return null;
        if (target == null) yield break;
        EnsureThumbCam();

        const float FAR = 8000f;
        var inst = Instantiate(prefab, new Vector3(FAR, 0f, FAR), Quaternion.identity);
        inst.hideFlags = HideFlags.HideAndDontSave;
        SetLayerAll(inst, THUMB_LAYER);

        var rends = inst.GetComponentsInChildren<Renderer>();
        var bounds = GetRendererBounds(rends, inst.transform.position);
        FitThumbnailCamera(bounds);

        var rt = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGB32);
        _thumbCam.targetTexture = rt;
        _thumbCam.Render();
        _thumbCam.targetTexture = null;

        if (target != null)
        {
            target.texture = rt;
            target.color = Color.white;
        }

        inst.SetActive(false);
        Destroy(inst);
    }

    Bounds GetRendererBounds(Renderer[] rends, Vector3 fallbackCenter)
    {
        if (rends.Length == 0)
            return new Bounds(fallbackCenter, Vector3.one * 2f);

        var bounds = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
            bounds.Encapsulate(rends[i].bounds);

        return bounds;
    }

    void FitThumbnailCamera(Bounds bounds)
    {
        Vector3 viewDir = new Vector3(1f, 0.75f, -1f).normalized;
        Quaternion viewRot = Quaternion.LookRotation(-viewDir, Vector3.up);
        Quaternion invRot = Quaternion.Inverse(viewRot);

        Vector3 ext = bounds.extents;
        Vector3 cen = bounds.center;
        float maxX = 0f;
        float maxY = 0f;
        float maxZ = 0f;

        for (int x = -1; x <= 1; x += 2)
        for (int y = -1; y <= 1; y += 2)
        for (int z = -1; z <= 1; z += 2)
        {
            Vector3 corner = cen + Vector3.Scale(ext, new Vector3(x, y, z));
            Vector3 local = invRot * (corner - cen);
            maxX = Mathf.Max(maxX, Mathf.Abs(local.x));
            maxY = Mathf.Max(maxY, Mathf.Abs(local.y));
            maxZ = Mathf.Max(maxZ, Mathf.Abs(local.z));
        }

        float padding = 1.18f;
        float aspect = 1f;
        _thumbCam.orthographicSize = Mathf.Max(maxY, maxX / aspect, 0.5f) * padding;
        _thumbCam.transform.rotation = viewRot;
        _thumbCam.transform.position = cen + viewDir * Mathf.Max(maxZ + 10f, 10f);
        _thumbCam.nearClipPlane = 0.01f;
        _thumbCam.farClipPlane = Mathf.Max(maxZ + 30f, 50f);
    }

    void EnsureThumbCam()
    {
        if (_thumbCam != null) return;

        var go = new GameObject("_HanokThumbCam");
        go.hideFlags = HideFlags.HideAndDontSave;

        _thumbCam = go.AddComponent<Camera>();
        _thumbCam.enabled = false;
        _thumbCam.clearFlags = CameraClearFlags.SolidColor;
        _thumbCam.backgroundColor = Hex("#E8E4DC");
        _thumbCam.orthographic = true;
        _thumbCam.nearClipPlane = 0.01f;
        _thumbCam.farClipPlane = 100f;
        _thumbCam.cullingMask = 1 << THUMB_LAYER;

        var lightGO = new GameObject("ThumbnailLight");
        lightGO.transform.SetParent(go.transform, false);
        lightGO.transform.localRotation = Quaternion.Euler(45f, -35f, 0f);
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.8f;
        light.cullingMask = 1 << THUMB_LAYER;
    }

    static void SetLayerAll(GameObject root, int layer)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = layer;
    }
}
