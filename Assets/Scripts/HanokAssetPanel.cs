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
    // ── 데이터 모델 ──────────────────────────────────────
    // prefab 한 개 + 그 prefab에 태깅된 카테고리 목록을 묶어 보관
    class HanokAssetEntry
    {
        public GameObject prefab;
        public HanokAssetCategory[] categories;
        public string displayName;   // HanokAssetInfo에서 가져온 한글 표시명 (없으면 prefab 이름)
        public string[] searchTags;  // HanokAssetInfo의 추가 검색어/동의어

        public HanokAssetEntry(GameObject prefab, HanokAssetCategory[] categories, string displayName, string[] searchTags)
        {
            this.prefab = prefab;
            this.categories = categories;
            this.displayName = displayName;
            this.searchTags = searchTags;
        }
    }

    // ── 상태 필드 ────────────────────────────────────────
    Camera _thumbCam;   // 썸네일 촬영용 카메라 (최초 사용 시 지연 생성)

    // 썸네일 순차 캡처 큐 — 한 번에 1개씩 처리해 프레임 스파이크 방지
    readonly Queue<(GameObject prefab, RawImage target)> _thumbQueue =
        new Queue<(GameObject, RawImage)>();
    bool _thumbQueueRunning = false;

    const string LABEL_ALL = "전체";

    // 카테고리 정의 (Resources/HanokCategories에서 로드한 SO들을 분류해 보관)
    readonly List<HanokAssetCategory> _mainCategories = new List<HanokAssetCategory>();
    readonly Dictionary<HanokAssetCategory, List<HanokAssetCategory>> _childCategories =
        new Dictionary<HanokAssetCategory, List<HanokAssetCategory>>();

    // 검색
    const float SEARCH_DEBOUNCE = 0.25f;
    TMP_InputField searchInput;
    string _searchQuery = "";          // 실제로 필터링에 적용된 검색어
    string _pendingSearchQuery = "";   // 입력창에 마지막으로 들어온 값 (디바운스 비교용)
    Coroutine _searchDebounce;

    // 카테고리 필터 선택 상태 + 탭 버튼 UI 참조
    HanokAssetCategory _selectedMain;
    HanokAssetCategory _selectedSub;
    HanokAssetCategory[] _mainFilterCats;
    HanokAssetCategory[] _subFilterCats = System.Array.Empty<HanokAssetCategory>();
    Button[] _mainFilterBtns;
    Button[] _subFilterBtns = System.Array.Empty<Button>();
    GameObject _mainFilterGO;
    GameObject _subFilterGO;

    // 로드된 에셋 목록 + 그리드 레이아웃 상수
    readonly List<HanokAssetEntry> _assetEntries = new List<HanokAssetEntry>();
    const float CELL_W = 76f;
    const float CELL_H = 88f;
    const int COLS = 3;

    // ── 에셋 로딩 ────────────────────────────────────────
    // Resources/HanokAssets를 한 번에 스캔해 HanokAssetTags가 붙은 prefab만 라이브러리로 채택
    void LoadAssets()
    {
        if (assetContent == null)
        {
            Debug.LogError("[HanokBuilder] assetContent null");
            return;
        }

        BuildCategoryTabs(assetContent);

        // 에셋별 한글 표시명 + 검색 태그 (HanokAssetInfo SO, assetKey == prefab 이름으로 매칭)
        var assetInfoByKey = new Dictionary<string, HanokAssetInfo>();
        foreach (var info in Resources.LoadAll<HanokAssetInfo>(ASSETINFO_PATH))
            if (!string.IsNullOrEmpty(info.assetKey))
                assetInfoByKey[info.assetKey] = info;

        _assetEntries.Clear();
        var raw = Resources.LoadAll<GameObject>(ASSET_PATH);
        foreach (var prefab in raw)
        {
            var assetTags = prefab.GetComponent<HanokAssetTags>();
            if (assetTags == null || assetTags.categories == null || assetTags.categories.Length == 0)
                continue;

            assetInfoByKey.TryGetValue(prefab.name, out var info);
            string displayName = prefab.name;
            string[] searchTags = System.Array.Empty<string>();
            if (info != null)
            {
                if (!string.IsNullOrEmpty(info.displayName)) displayName = info.displayName;
                if (info.tags != null) searchTags = info.tags;
            }
            _assetEntries.Add(new HanokAssetEntry(prefab, assetTags.categories, displayName, searchTags));
        }

        _assetEntries.Sort((a, b) =>
            string.Compare(a.prefab.name, b.prefab.name, System.StringComparison.OrdinalIgnoreCase));

        Debug.Log($"[HanokBuilder] {_assetEntries.Count} assets loaded");
        RefreshAssetList();
    }

    // ── 카테고리 정의 로딩 ───────────────────────────────
    // HanokAssetCategory SO들을 읽어 parent==null은 메인, 나머지는 부모별 서브 목록으로 분류·정렬
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

    // ── 카테고리 탭 UI 구성 ──────────────────────────────
    // 로드된 카테고리 SO들로 "전체" + 메인 필터 행을 동적 생성하고, 서브 필터 그리드 자리를 마련
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

    // 선택된 메인 카테고리의 자식 목록으로 서브 필터 버튼들을 다시 그림 (자식이 없으면 그리드 숨김)
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

    // ── 검색 ─────────────────────────────────────────────
    // 입력값이 실제로 바뀐 경우에만 디바운스 타이머를 (재)시작 — 중복 입력 이벤트로 인한 새로고침 폭주 방지
    void OnSearchChanged(string value)
    {
        string trimmed = value.Trim();
        if (trimmed == _pendingSearchQuery) return;
        _pendingSearchQuery = trimmed;

        if (_searchDebounce != null) StopCoroutine(_searchDebounce);
        _searchDebounce = StartCoroutine(ApplySearchAfterDelay(trimmed));
    }

    // 타이핑이 멈추고 SEARCH_DEBOUNCE초가 지나야 실제로 검색어를 적용하고 목록을 한 번만 새로고침
    IEnumerator ApplySearchAfterDelay(string query)
    {
        yield return new WaitForSeconds(SEARCH_DEBOUNCE);
        _searchDebounce = null;

        if (_searchQuery == query) yield break;
        _searchQuery = query;
        RefreshAssetList();
    }

    // ── 필터 선택 ────────────────────────────────────────
    // 메인 카테고리를 바꾸면 서브 선택은 초기화하고 서브 필터 그리드를 재구성
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

    // ── 목록 렌더링 ──────────────────────────────────────
    // 필터 행을 제외한 기존 그리드를 비우고, 현재 필터·검색 조건에 맞는 에셋들로 다시 채움
    void RefreshAssetList()
    {
        if (assetContent == null) return;

        // 이전 캡처 요청 취소 — 카테고리/검색 변경 시 불필요한 작업 방지
        _thumbQueue.Clear();

        var children = new List<GameObject>();
        foreach (Transform ch in assetContent)
        {
            if (ch.gameObject != _mainFilterGO && ch.gameObject != _subFilterGO)
                children.Add(ch.gameObject);
        }
        // 삭제 전 RenderTexture GPU 메모리 해제
        foreach (var ch in children)
            foreach (var ri in ch.GetComponentsInChildren<RawImage>())
                if (ri.texture is RenderTexture oldRt) { oldRt.Release(); Destroy(oldRt); }
        foreach (var ch in children) DestroyImmediate(ch);

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
                    var entry = filtered[pi];
                    var prefab = entry.prefab;
                    var rawImg = MakeGridCell(row.transform, entry.displayName, () => Spawn(prefab));
                    EnqueueThumbnail(prefab, rawImg);
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

    // 선택된 메인/서브 카테고리를 모두 포함하고, 검색어가 이름에 포함되는 에셋만 추려냄 (AND 조건)
    // 성능 제한: 최대 1개만 반환 (컴퓨터 사양 대응)
    List<HanokAssetEntry> GetFilteredAssets()
    {
        var result = new List<HanokAssetEntry>();
        foreach (var asset in _assetEntries)
        {
            if (_selectedMain != null && System.Array.IndexOf(asset.categories, _selectedMain) < 0)
                continue;

            if (_selectedSub != null && System.Array.IndexOf(asset.categories, _selectedSub) < 0)
                continue;

            if (_searchQuery.Length > 0 && !MatchesSearch(asset))
                continue;

            result.Add(asset);
            break;  // 에셋 1개만 표시
        }

        return result;
    }

    // 검색어가 prefab 이름, 한글 표시명, 검색 태그(동의어) 중 하나에라도 포함되면 매치
    bool MatchesSearch(HanokAssetEntry asset)
    {
        if (asset.prefab.name.IndexOf(_searchQuery, System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        if (asset.displayName.IndexOf(_searchQuery, System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        foreach (var tag in asset.searchTags)
            if (tag.IndexOf(_searchQuery, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

        return false;
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
        t.textWrappingMode = TextWrappingModes.NoWrap;
        if (HasKorean(label)) KorFont(t); else LatFont(t);

        return raw;
    }

    // ── 썸네일 순차 큐 ───────────────────────────────────
    // RefreshAssetList·AI 패널 모두 이 메서드로 요청 → 프레임당 1개씩 순차 처리
    void EnqueueThumbnail(GameObject prefab, RawImage target)
    {
        _thumbQueue.Enqueue((prefab, target));
        if (!_thumbQueueRunning)
            StartCoroutine(ProcessThumbnailQueue());
    }

    IEnumerator ProcessThumbnailQueue()
    {
        _thumbQueueRunning = true;
        while (_thumbQueue.Count > 0)
        {
            var (prefab, target) = _thumbQueue.Dequeue();
            yield return StartCoroutine(CaptureThumbnail(prefab, target));
            yield return null; // 캡처 사이 1프레임 여유
        }
        _thumbQueueRunning = false;
    }

    // ── 썸네일 캡처 ──────────────────────────────────────
    // 카메라 시야 밖 먼 곳에 prefab을 임시 인스턴스화해 전용 카메라로 찍은 뒤 RenderTexture로 표시
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

        // 비활성 자식도 포함해 bounds 계산 (LOD/비활성 메시 누락 방지)
        var rends = inst.GetComponentsInChildren<Renderer>(true);
        var bounds = GetRendererBounds(rends, inst.transform.position);

        float dist = Mathf.Max(bounds.size.magnitude * 1.35f, 0.5f);
        _thumbCam.transform.position =
            bounds.center + new Vector3(1.1f, 0.85f, -1.05f).normalized * dist;
        _thumbCam.transform.LookAt(bounds.center);

        // 클리핑 평면: 카메라-오브젝트 거리를 고려해 올바르게 설정
        _thumbCam.nearClipPlane = 0.1f;
        _thumbCam.farClipPlane  = dist + bounds.size.magnitude + 10f;

        var rt = new RenderTexture(128, 128, 16, RenderTextureFormat.ARGB32);
        rt.Create();
        _thumbCam.targetTexture = rt;

        // URP에서 disabled 카메라의 Render() 미동작 문제 방지:
        // 한 프레임 동안 활성화해 파이프라인이 정상 렌더하도록 함
        _thumbCam.enabled = true;
        yield return new WaitForEndOfFrame();
        _thumbCam.enabled = false;
        _thumbCam.targetTexture = null;

        if (target != null) { target.texture = rt; target.color = Color.white; }
        else { rt.Release(); Destroy(rt); }
        Destroy(inst);
    }

    // FBX 재질 색상 보존 — Standard → URP 변환 시 색상·텍스처 유지
    static void FixMaterialColors(GameObject obj)
    {
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) return;

        foreach (var r in obj.GetComponentsInChildren<Renderer>())
        {
            bool needFix = false;
            foreach (var sm in r.sharedMaterials)
            {
                if (sm == null) continue;
                var sn = sm.shader?.name ?? "";
                if (sn == "Hidden/InternalErrorShader" || sn == "Standard" || sn == "")
                    needFix = true;
            }
            if (!needFix) continue;

            var mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m == null) continue;
                var sn = m.shader?.name ?? "";
                if (sn != "Hidden/InternalErrorShader" && sn != "Standard" && sn != "") continue;

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
        Quaternion invRot  = Quaternion.Inverse(viewRot);

        Vector3 ext = bounds.extents;
        Vector3 cen = bounds.center;
        float maxX = 0f, maxY = 0f, maxZ = 0f;

        for (int x = -1; x <= 1; x += 2)
        for (int y = -1; y <= 1; y += 2)
        for (int z = -1; z <= 1; z += 2)
        {
            Vector3 corner = cen + Vector3.Scale(ext, new Vector3(x, y, z));
            Vector3 local  = invRot * (corner - cen);
            maxX = Mathf.Max(maxX, Mathf.Abs(local.x));
            maxY = Mathf.Max(maxY, Mathf.Abs(local.y));
            maxZ = Mathf.Max(maxZ, Mathf.Abs(local.z));
        }

        _thumbCam.nearClipPlane = 0.01f;
        _thumbCam.farClipPlane  = Mathf.Max(maxZ + 30f, 50f);
    }

    void EnsureThumbCam()
    {
        if (_thumbCam != null) return;

        var go = new GameObject("_HanokThumbCam");
        // HideAndDontSave 대신 HideInHierarchy만 설정: URP는 Camera.allCameras에서 HideAndDontSave 카메라를 제외하므로 렌더링이 안 됨
        go.hideFlags  = HideFlags.HideInHierarchy;
        _thumbCam     = go.AddComponent<Camera>();
        _thumbCam.enabled          = false;
        _thumbCam.clearFlags       = CameraClearFlags.SolidColor;
        _thumbCam.backgroundColor  = Hex("#EEE8DC");
        _thumbCam.fieldOfView      = 28f;
        _thumbCam.nearClipPlane    = 0.05f;
        _thumbCam.farClipPlane     = 20000f;
        _thumbCam.cullingMask      = 1 << THUMB_LAYER;
        _thumbCam.allowMSAA        = true;

        var lightGO = new GameObject("ThumbnailLight");
        lightGO.transform.SetParent(go.transform, false);
        lightGO.transform.localRotation = Quaternion.Euler(45f, -35f, 0f);
        var light = lightGO.AddComponent<Light>();
        light.type      = LightType.Directional;
        light.intensity = 1.8f;
        light.cullingMask = 1 << THUMB_LAYER;
    }

    static void SetLayerAll(GameObject root, int layer)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = layer;
    }
}
