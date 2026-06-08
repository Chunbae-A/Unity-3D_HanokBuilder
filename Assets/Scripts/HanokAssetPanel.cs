using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Left asset library panel for HanokUIManager.
/// Loads prefab entries from Resources/HanokAssets and filters them by HanokAssetTags categories.
/// </summary>
public partial class HanokUIManager
{
    class HanokAssetEntry
    {
        public GameObject prefab;
        public HanokAssetCategory[] categories;

        public HanokAssetEntry(GameObject prefab, HanokAssetCategory[] categories)
        {
            this.prefab = prefab;
            this.categories = categories;
        }
    }

    Camera _thumbCam;

    const string LABEL_ALL = "전체";

    readonly List<HanokAssetCategory> _mainCategories = new List<HanokAssetCategory>();
    readonly Dictionary<HanokAssetCategory, List<HanokAssetCategory>> _childCategories =
        new Dictionary<HanokAssetCategory, List<HanokAssetCategory>>();

    const float SEARCH_DEBOUNCE = 0.25f;

    TMP_InputField searchInput;
    string _searchQuery = "";
    string _pendingSearchQuery = "";
    Coroutine _searchDebounce;

    HanokAssetCategory _selectedMain;
    HanokAssetCategory _selectedSub;
    HanokAssetCategory[] _mainFilterCats;
    HanokAssetCategory[] _subFilterCats = System.Array.Empty<HanokAssetCategory>();
    Button[] _mainFilterBtns;
    Button[] _subFilterBtns = System.Array.Empty<Button>();
    GameObject _mainFilterGO;
    GameObject _subFilterGO;
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

        _assetEntries.Clear();
        var raw = Resources.LoadAll<GameObject>(ASSET_PATH);
        foreach (var prefab in raw)
        {
            var tags = prefab.GetComponent<HanokAssetTags>();
            if (tags == null || tags.categories == null || tags.categories.Length == 0)
                continue;

            _assetEntries.Add(new HanokAssetEntry(prefab, tags.categories));
        }

        _assetEntries.Sort((a, b) =>
            string.Compare(a.prefab.name, b.prefab.name, System.StringComparison.OrdinalIgnoreCase));

        Debug.Log($"[HanokBuilder] {_assetEntries.Count} assets loaded");
        RefreshAssetList();
    }

    void LoadCategoryDefinitions()
    {
        _mainCategories.Clear();
        _childCategories.Clear();

        var raw = Resources.LoadAll<HanokAssetCategory>(CATEGORY_PATH);
        foreach (var cat in raw)
        {
            if (cat.parent == null)
            {
                _mainCategories.Add(cat);
                continue;
            }

            if (!_childCategories.TryGetValue(cat.parent, out var children))
            {
                children = new List<HanokAssetCategory>();
                _childCategories[cat.parent] = children;
            }
            children.Add(cat);
        }

        _mainCategories.Sort((a, b) => a.order.CompareTo(b.order));
        foreach (var children in _childCategories.Values)
            children.Sort((a, b) => a.order.CompareTo(b.order));
    }

    void BuildCategoryTabs(Transform parent)
    {
        LoadCategoryDefinitions();

        _mainFilterGO = BuildFilterRow(parent, "MainFilters", 36f);
        _mainFilterCats = new HanokAssetCategory[_mainCategories.Count + 1];
        _mainCategories.CopyTo(_mainFilterCats, 1);

        _mainFilterBtns = new Button[_mainFilterCats.Length];
        for (int i = 0; i < _mainFilterCats.Length; i++)
        {
            var cat = _mainFilterCats[i];
            _mainFilterBtns[i] = MakeFilterButton(_mainFilterGO.transform, cat == null ? LABEL_ALL : cat.label,
                () => SelectMainCategory(cat));
        }

        _subFilterGO = BuildFilterGrid(parent, "SubFilters", 88f);
        _subFilterGO.SetActive(false);

        UpdateTabColors();
    }

    void RebuildSubFilters(HanokAssetCategory main)
    {
        foreach (Transform child in _subFilterGO.transform)
            DestroyImmediate(child.gameObject);

        if (main == null || !_childCategories.TryGetValue(main, out var children) || children.Count == 0)
        {
            _subFilterCats = System.Array.Empty<HanokAssetCategory>();
            _subFilterBtns = System.Array.Empty<Button>();
            _subFilterGO.SetActive(false);
            return;
        }

        _subFilterCats = new HanokAssetCategory[children.Count + 1];
        children.CopyTo(_subFilterCats, 1);

        _subFilterBtns = new Button[_subFilterCats.Length];
        for (int i = 0; i < _subFilterCats.Length; i++)
        {
            var cat = _subFilterCats[i];
            _subFilterBtns[i] = MakeFilterButton(_subFilterGO.transform, cat == null ? LABEL_ALL : cat.label,
                () => SelectSubCategory(cat));
        }

        _subFilterGO.SetActive(true);
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

    void OnSearchChanged(string value)
    {
        string trimmed = value.Trim();
        if (trimmed == _pendingSearchQuery) return;
        _pendingSearchQuery = trimmed;

        if (_searchDebounce != null) StopCoroutine(_searchDebounce);
        _searchDebounce = StartCoroutine(ApplySearchAfterDelay(trimmed));
    }

    IEnumerator ApplySearchAfterDelay(string query)
    {
        yield return new WaitForSeconds(SEARCH_DEBOUNCE);
        _searchDebounce = null;

        if (_searchQuery == query) yield break;
        _searchQuery = query;
        RefreshAssetList();
    }

    void SelectMainCategory(HanokAssetCategory cat)
    {
        _selectedMain = cat;
        _selectedSub = null;
        RebuildSubFilters(cat);
        UpdateTabColors();
        RefreshAssetList();
    }

    void SelectSubCategory(HanokAssetCategory cat)
    {
        _selectedSub = cat;
        UpdateTabColors();
        RefreshAssetList();
    }

    void UpdateTabColors()
    {
        for (int i = 0; i < _mainFilterBtns.Length; i++)
            SetFilterButtonState(_mainFilterBtns[i], _mainFilterCats[i] == _selectedMain);

        for (int i = 0; i < _subFilterBtns.Length; i++)
            SetFilterButtonState(_subFilterBtns[i], _subFilterCats[i] == _selectedSub);
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
            if (ch.gameObject != _mainFilterGO && ch.gameObject != _subFilterGO)
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
            if (_selectedMain != null && System.Array.IndexOf(asset.categories, _selectedMain) < 0)
                continue;

            if (_selectedSub != null && System.Array.IndexOf(asset.categories, _selectedSub) < 0)
                continue;

            if (_searchQuery.Length > 0 &&
                asset.prefab.name.IndexOf(_searchQuery, System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            result.Add(asset);
        }

        return result;
    }

    string GetCurrentCategoryLabel()
    {
        string label;
        if (_selectedMain == null)
            label = LABEL_ALL;
        else if (_selectedSub == null)
            label = _selectedMain.label;
        else
            label = $"{_selectedMain.label} / {_selectedSub.label}";

        if (_searchQuery.Length > 0)
            label += $" · '{_searchQuery}' 검색결과";

        return label;
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
        t.text = _searchQuery.Length > 0
            ? $"'{_searchQuery}'에 대한 검색 결과가 없습니다"
            : "Resources/HanokAssets\n폴더에 Prefab을 넣으세요";
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
