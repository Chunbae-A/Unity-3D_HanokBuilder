using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using TMPro;

/// <summary>
/// Left asset library panel for HanokUIManager.
/// Loads prefab entries from Resources/HanokAssets and filters them by tagged or runtime categories.
/// </summary>
public partial class HanokUIManager
{
    // ── 데이터 모델 ──────────────────────────────────────
    // prefab 한 개 + 그 prefab에 태깅된 카테고리 목록을 묶어 보관
    class HanokAssetEntry
    {
        // 즉시 로드된 프리팹 (일반 에셋) 또는 null (지연 로딩 에셋)
        GameObject _prefab;
        public string prefabPath;   // 지연 로딩용 Resources 경로

        public GameObject prefab
        {
            get
            {
                if (_prefab == null && !string.IsNullOrEmpty(prefabPath))
                    _prefab = Resources.Load<GameObject>(prefabPath);
                return _prefab;
            }
            set => _prefab = value;
        }

        public string assetKey;
        public HanokAssetCategory[] categories;
        public string displayName;
        public string[] searchTags;
        public bool isCultureAsset;

        public HanokAssetEntry(
            GameObject prefab,
            string assetKey,
            HanokAssetCategory[] categories,
            string displayName,
            string[] searchTags,
            bool isCultureAsset)
        {
            _prefab = prefab;
            this.assetKey = assetKey;
            this.categories = categories;
            this.displayName = displayName;
            this.searchTags = searchTags;
            this.isCultureAsset = isCultureAsset;
        }
    }

    // ── 상태 필드 ────────────────────────────────────────
    Camera _thumbCam;   // 썸네일 촬영용 카메라 (최초 사용 시 지연 생성)
    Dictionary<string, GameObject> _culturePrefabLookup;

    // 썸네일 순차 캡처 큐 — 한 번에 1개씩 처리해 프레임 스파이크 방지
    readonly Queue<(GameObject prefab, RawImage target)> _thumbQueue =
        new Queue<(GameObject, RawImage)>();
    bool _thumbQueueRunning = false;

    const string LABEL_ALL = "전체";
    const string CULTURE_INDEX_PATH = "HanokAssets/CultureMetaverse/culture_metaverse_index";

    // 카테고리 정의 (Resources/HanokCategories에서 로드한 SO들을 분류해 보관)
    readonly List<HanokAssetCategory> _mainCategories = new List<HanokAssetCategory>();
    readonly Dictionary<HanokAssetCategory, List<HanokAssetCategory>> _childCategories =
        new Dictionary<HanokAssetCategory, List<HanokAssetCategory>>();
    readonly Dictionary<string, HanokAssetCategory> _cultureCategoriesByKey =
        new Dictionary<string, HanokAssetCategory>();
    HanokAssetCategory _cultureCategory;
    HanokAssetCategory _cultureCharactersCategory;
    HanokAssetCategory _cultureMerchantCategory;
    HanokAssetCategory _cultureFestivalCategory;
    HanokAssetCategory _cultureObjectCategory;

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
    // Resources/HanokAssets를 스캔해 라이브러리로 채택:
    //   HanokAssetTags(카테고리)가 지정된 정식 프리팹만 채택.
    //   Meshes/Part 등 원본 FBX는 정식 프리팹이 참조하는 소스 메시이므로 제외
    //   (포함 시 같은 부재가 라이브러리에 중복 표시됨)
    void LoadAssets()
    {
        if (assetContent == null)
        {
            Debug.LogError("[HanokBuilder] assetContent null");
            return;
        }

        // 카테고리 SO 먼저 초기화 (에셋 분류에 필요)
        LoadCategoryDefinitions();

        // 에셋별 한글 표시명 + 검색 태그 (HanokAssetInfo SO, assetKey == prefab 이름으로 매칭)
        var assetInfoByKey = new Dictionary<string, HanokAssetInfo>();
        foreach (var info in Resources.LoadAll<HanokAssetInfo>(ASSETINFO_PATH))
            if (!string.IsNullOrEmpty(info.assetKey))
                assetInfoByKey[info.assetKey] = info;

        _assetEntries.Clear();
        _aiCatalog = null;
        _culturePrefabLookup = null;
        var addedPrefabs = new HashSet<GameObject>();
        var raw = Resources.LoadAll<GameObject>(ASSET_PATH);
        foreach (var prefab in raw)
        {
            // CM_ 프리팹은 JSON 매니페스트로 지연 로딩 — bulk load에서 제외
            if (prefab.name.StartsWith("CM_")) continue;

            assetInfoByKey.TryGetValue(prefab.name, out var info);
            string displayName = (info != null && !string.IsNullOrEmpty(info.displayName))
                ? info.displayName : prefab.name;
            string[] searchTags = (info?.tags) ?? System.Array.Empty<string>();

            var assetTags = prefab.GetComponent<HanokAssetTags>();
            if (assetTags == null || assetTags.categories == null || assetTags.categories.Length == 0)
                continue;

            _assetEntries.Add(new HanokAssetEntry(prefab, prefab.name, assetTags.categories, displayName, searchTags, false));
        }

        LoadCultureFolderManifests();
        LoadCultureMetaverseAssets(assetInfoByKey, addedPrefabs);

        _assetEntries.Sort((a, b) =>
            string.Compare(a.displayName, b.displayName, System.StringComparison.OrdinalIgnoreCase));

        // 에셋 없는 카테고리 제거 후 탭 UI 구성 (버튼만 생성 — 메모리 무관)
        PruneEmptyCategories();
        BuildCategoryTabs(assetContent);

        Debug.Log($"[HanokBuilder] {_assetEntries.Count} assets loaded (HanokAssets 폴더 내 전체)");
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

        EnsureCultureCategories();
    }

    void EnsureCultureCategories()
    {
        _cultureCategoriesByKey.Clear();

        _cultureCategory = RuntimeCategory(_cultureCategory, "culture_metaverse", "문화포털", null, 9000);

        AddRuntimeCategory(_cultureCategory);

        _cultureCharactersCategory = RegisterCultureSubCategory(
            _cultureCharactersCategory, "characters", "캐릭터", 9010);
        _cultureMerchantCategory = RegisterCultureSubCategory(
            _cultureMerchantCategory, "merchant", "상인/공간", 9020);
        _cultureFestivalCategory = RegisterCultureSubCategory(
            _cultureFestivalCategory, "festival", "전통축제", 9030);
        _cultureObjectCategory = RegisterCultureSubCategory(
            _cultureObjectCategory, "object", "문화재/소품", 9040);

        RegisterCultureSubCategory(null, "object_weapon", "무기류", 9050);
        RegisterCultureSubCategory(null, "object_equipment", "시설/장비류", 9060);
        RegisterCultureSubCategory(null, "object_nature", "자연물", 9070);
        RegisterCultureSubCategory(null, "object_clothing", "의복류", 9080);
        RegisterCultureSubCategory(null, "object_changwon", "창원의집", 9090);
        RegisterCultureSubCategory(null, "object_gugak_clothing", "국악원 의복", 9100);
        RegisterCultureSubCategory(null, "object_gugak_instrument", "국악원 악기", 9110);
        RegisterCultureSubCategory(null, "object_army_weapon", "육군박물관 무기", 9120);
        RegisterCultureSubCategory(null, "object_stone_pagoda", "석탑", 9130);
        RegisterCultureSubCategory(null, "object_interactive", "상호작용", 9140);

        _mainCategories.Sort((a, b) => a.order.CompareTo(b.order));
        foreach (var children in _childCategories.Values)
            children.Sort((a, b) => a.order.CompareTo(b.order));
    }

    HanokAssetCategory RegisterCultureSubCategory(
        HanokAssetCategory category,
        string key,
        string label,
        int order)
    {
        category = RuntimeCategory(category, key, label, _cultureCategory, order);
        _cultureCategoriesByKey[key] = category;
        AddRuntimeCategory(category);
        return category;
    }

    HanokAssetCategory RuntimeCategory(
        HanokAssetCategory category,
        string key,
        string label,
        HanokAssetCategory parent,
        int order)
    {
        if (category == null)
        {
            category = ScriptableObject.CreateInstance<HanokAssetCategory>();
            category.hideFlags = HideFlags.HideAndDontSave;
        }

        category.key = key;
        category.label = label;
        category.parent = parent;
        category.order = order;
        category.name = "Runtime_" + key;
        return category;
    }

    void AddRuntimeCategory(HanokAssetCategory category)
    {
        if (category.parent == null)
        {
            if (!_mainCategories.Contains(category))
                _mainCategories.Add(category);
            return;
        }

        if (!_childCategories.TryGetValue(category.parent, out var children))
        {
            children = new List<HanokAssetCategory>();
            _childCategories[category.parent] = children;
        }

        if (!children.Contains(category))
            children.Add(category);
    }

    // 에셋이 한 개도 없는 카테고리/서브카테고리를 패널에서 제거
    void PruneEmptyCategories()
    {
        // 카테고리별 에셋 수 집계
        var counts = new Dictionary<HanokAssetCategory, int>();
        foreach (var entry in _assetEntries)
            foreach (var cat in entry.categories)
            {
                if (cat == null) continue;
                counts[cat] = (counts.TryGetValue(cat, out var n) ? n : 0) + 1;
                if (cat.parent != null)
                    counts[cat.parent] = (counts.TryGetValue(cat.parent, out var p) ? p : 0) + 1;
            }

        // 빈 서브카테고리 제거
        foreach (var key in new List<HanokAssetCategory>(_childCategories.Keys))
        {
            var subs = _childCategories[key];
            subs.RemoveAll(s => !counts.ContainsKey(s));
            if (subs.Count == 0)
                _childCategories.Remove(key);
        }

        // 에셋도 서브도 없는 메인 카테고리 제거
        _mainCategories.RemoveAll(m => !counts.ContainsKey(m) && !_childCategories.ContainsKey(m));
    }

    // ── JSON 매니페스트 기반 지연 로딩 (건축물완성형/부품형/공간소품/디지털휴먼) ──
    void LoadCultureFolderManifests()
    {
        string[] manifests = { "건축물완성형", "건축물부품형", "공간소품", "디지털휴먼" };
        foreach (var name in manifests)
        {
            var ta = Resources.Load<TextAsset>($"HanokManifest/{name}");
            if (ta == null) { Debug.LogWarning($"[HanokBuilder] 매니페스트 없음: HanokManifest/{name}.json — python Tools/generate_culture_manifests.py 실행하세요"); continue; }

            ManifestJson root;
            try { root = JsonUtility.FromJson<ManifestJson>(ta.text); }
            catch (System.Exception e) { Debug.LogError($"[HanokBuilder] 매니페스트 파싱 오류 {name}: {e.Message}"); continue; }

            // 메인 카테고리 SO (CultureAssetImporter가 만든 .asset)
            var mainCatSO = Resources.Load<HanokAssetCategory>($"HanokCategories/Category_{GetCatKey(name)}");
            if (mainCatSO == null) mainCatSO = MakeRuntimeCat(name, name, null, 8000 + System.Array.IndexOf(manifests, name) * 10);

            // 서브카테고리 런타임 캐시
            var subCatCache = new Dictionary<string, HanokAssetCategory>();

            foreach (var asset in root.assets)
            {
                var cats = new List<HanokAssetCategory> { mainCatSO };

                if (!string.IsNullOrEmpty(asset.sub))
                {
                    if (!subCatCache.TryGetValue(asset.sub, out var subCat))
                    {
                        subCat = MakeRuntimeCat(asset.sub, asset.sub, mainCatSO, mainCatSO.order + subCatCache.Count + 1);
                        subCatCache[asset.sub] = subCat;
                    }
                    cats.Add(subCat);
                }

                var entry = new HanokAssetEntry(null, asset.key, cats.ToArray(), asset.display, System.Array.Empty<string>(), true)
                {
                    prefabPath = asset.path
                };
                _assetEntries.Add(entry);
            }

            Debug.Log($"[HanokBuilder] 매니페스트 로드: {name} {root.assets.Length}개");
        }
    }

    static string GetCatKey(string folderName) => folderName switch
    {
        "건축물완성형" => "Complete",
        "건축물부품형" => "Parts",
        "공간소품"     => "Props",
        "디지털휴먼"   => "DigitalHuman",
        _              => folderName
    };

    HanokAssetCategory MakeRuntimeCat(string key, string label, HanokAssetCategory parent, int order)
    {
        var cat = ScriptableObject.CreateInstance<HanokAssetCategory>();
        cat.hideFlags = HideFlags.HideAndDontSave;
        cat.key = key; cat.label = label; cat.parent = parent; cat.order = order;
        cat.name = "Runtime_" + key;
        AddRuntimeCategory(cat);
        return cat;
    }

    [System.Serializable] class ManifestJson  { public string category; public ManifestAsset[] assets; }
    [System.Serializable] class ManifestAsset { public string key, display, sub, path; }

    void LoadCultureMetaverseAssets(
        Dictionary<string, HanokAssetInfo> assetInfoByKey,
        HashSet<GameObject> addedPrefabs)
    {
        var objectTitles = LoadObjectDirectTitles();
        if (LoadCultureMetaverseAssetIndex(assetInfoByKey, addedPrefabs, objectTitles))
            return;

        AddCultureAssetGroup(
            "HanokAssets/CultureMetaverse/Characters",
            _cultureCharactersCategory,
            "문화포털 캐릭터 한국적 캐릭터 상인 남자 여자 인물",
            assetInfoByKey,
            addedPrefabs,
            objectTitles);
        AddCultureAssetGroup(
            "HanokAssets/CultureMetaverse/Props/MerchantAndEnvironment",
            _cultureMerchantCategory,
            "문화포털 상인 장터 의상 저고리 치마 바지 신발 나무 공간 소품",
            assetInfoByKey,
            addedPrefabs,
            objectTitles);
        AddCultureAssetGroup(
            "HanokAssets/CultureMetaverse/Props/KoreanTraditionalFestival/Prefabs",
            _cultureFestivalCategory,
            "문화포털 김포 통진 두레놀이 농경 전통 축제 농기구 소품",
            assetInfoByKey,
            addedPrefabs,
            objectTitles);
        AddCultureAssetGroup(
            "HanokAssets/CultureMetaverse/Props/ObjectDirectFBX",
            _cultureObjectCategory,
            "문화포털 문화재 오브젝트 소품 창원의집 수원화성 홍천 육군박물관 석탑 좌상 깃발 총통",
            assetInfoByKey,
            addedPrefabs,
            objectTitles);
    }

    bool LoadCultureMetaverseAssetIndex(
        Dictionary<string, HanokAssetInfo> assetInfoByKey,
        HashSet<GameObject> addedPrefabs,
        Dictionary<string, string> objectTitles)
    {
        var index = Resources.Load<TextAsset>(CULTURE_INDEX_PATH);
        if (index == null) return false;

        int loaded = 0;
        var lines = index.text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("category\t"))
                continue;

            var parts = line.Split('\t');
            if (parts.Length < 3) continue;

            string categoryKey = parts[0].Trim();
            string resourcePath = parts[1].Trim();
            string broadTags = parts[2].Trim();
            string displayNameOverride = parts.Length >= 4 ? parts[3].Trim() : null;
            var subCategory = GetCultureSubCategory(categoryKey);
            if (subCategory == null) continue;

            var prefab = LoadCulturePrefab(resourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"[HanokBuilder] CultureMetaverse index asset not found: {resourcePath}");
                continue;
            }

            if (AddCultureAsset(prefab, resourcePath, subCategory, broadTags, assetInfoByKey,
                    addedPrefabs, objectTitles, displayNameOverride))
                loaded++;
        }

        Debug.Log($"[HanokBuilder] {loaded} CultureMetaverse indexed assets loaded");
        return loaded > 0;
    }

    GameObject LoadCulturePrefab(string resourcePath)
    {
        var prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab != null) return prefab;

        EnsureCulturePrefabLookup();
        string fileName = resourcePath;
        int slash = fileName.LastIndexOf('/');
        if (slash >= 0) fileName = fileName[(slash + 1)..];
        string key = CultureLookupKey(fileName);
        return _culturePrefabLookup.TryGetValue(key, out prefab) ? prefab : null;
    }

    void EnsureCulturePrefabLookup()
    {
        if (_culturePrefabLookup != null) return;

        _culturePrefabLookup = new Dictionary<string, GameObject>();
        foreach (var prefab in Resources.LoadAll<GameObject>("HanokAssets/CultureMetaverse"))
        {
            if (prefab == null) continue;

            string key = CultureLookupKey(prefab.name);
            if (!_culturePrefabLookup.ContainsKey(key))
                _culturePrefabLookup[key] = prefab;
        }
    }

    string CultureLookupKey(string value)
    {
        return StripExtension(value)
            .Normalize(NormalizationForm.FormC)
            .ToLowerInvariant();
    }

    HanokAssetCategory GetCultureSubCategory(string key)
    {
        if (_cultureCategoriesByKey.TryGetValue(key, out var category))
            return category;

        return key switch
        {
            "characters" => _cultureCharactersCategory,
            "merchant" => _cultureMerchantCategory,
            "festival" => _cultureFestivalCategory,
            "object" => _cultureObjectCategory,
            _ => null
        };
    }

    void AddCultureAssetGroup(
        string resourcePath,
        HanokAssetCategory subCategory,
        string broadTags,
        Dictionary<string, HanokAssetInfo> assetInfoByKey,
        HashSet<GameObject> addedPrefabs,
        Dictionary<string, string> objectTitles)
    {
        foreach (var prefab in Resources.LoadAll<GameObject>(resourcePath))
        {
            AddCultureAsset(prefab, resourcePath + "/" + prefab.name, subCategory, broadTags,
                assetInfoByKey, addedPrefabs, objectTitles, null);
        }
    }

    bool AddCultureAsset(
        GameObject prefab,
        string assetKey,
        HanokAssetCategory subCategory,
        string broadTags,
        Dictionary<string, HanokAssetInfo> assetInfoByKey,
        HashSet<GameObject> addedPrefabs,
        Dictionary<string, string> objectTitles,
        string displayNameOverride)
    {
        if (prefab == null || addedPrefabs.Contains(prefab)) return false;

        assetInfoByKey.TryGetValue(prefab.name, out var info);
        string displayName = !string.IsNullOrEmpty(displayNameOverride)
            ? displayNameOverride
            : GetCultureDisplayName(prefab.name, info, objectTitles);
        string[] searchTags = BuildCultureSearchTags(prefab.name, displayName, broadTags, info);
        _assetEntries.Add(new HanokAssetEntry(
            prefab,
            assetKey,
            new[] { _cultureCategory, subCategory },
            displayName,
            searchTags,
            true));
        addedPrefabs.Add(prefab);
        return true;
    }

    Dictionary<string, string> LoadObjectDirectTitles()
    {
        var map = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
        var text = Resources.Load<TextAsset>(
            "HanokAssets/CultureMetaverse/Props/ObjectDirectFBX/object_direct_fbx_sources");
        if (text == null) return map;

        var lines = text.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var parts = line.Split('\t');
            if (parts.Length < 4) continue;

            string title = parts[1].Trim();
            string file = parts[2].Trim();
            string baseName = StripExtension(file);
            if (!string.IsNullOrEmpty(title))
            {
                map[file] = title;
                map[baseName] = title;
            }
        }
        return map;
    }

    string GetCultureDisplayName(string prefabName, HanokAssetInfo info, Dictionary<string, string> objectTitles)
    {
        if (info != null && !string.IsNullOrEmpty(info.displayName))
            return info.displayName;

        if (objectTitles != null && objectTitles.TryGetValue(prefabName, out var title))
            return title;

        string overrideName = prefabName switch
        {
            "DH_Man_01" => "한국적 캐릭터 01",
            "DH_Man_02" => "한국적 캐릭터 02",
            "DH_Man_03" => "한국적 캐릭터 03",
            "DH_Man_04" => "한국적 캐릭터 04",
            "DH_Woman_01" => "한국적 캐릭터 05",
            "SM_Merchant_Male" => "상인 남자",
            "SM_Merchant_FeMale" => "상인 여자",
            "SM_FineTree" => "소나무",
            "SM_WIllowTree" => "버드나무",
            _ => null
        };
        if (!string.IsNullOrEmpty(overrideName)) return overrideName;

        return HumanizeAssetName(prefabName);
    }

    string[] BuildCultureSearchTags(string prefabName, string displayName, string broadTags, HanokAssetInfo info)
    {
        var tags = new List<string>
        {
            "CultureMetaverse",
            "문화포털",
            "추가에셋",
            displayName,
            prefabName,
            HumanizeAssetName(prefabName),
            broadTags
        };

        if (info != null && info.tags != null)
            tags.AddRange(info.tags);

        return tags.ToArray();
    }

    string HumanizeAssetName(string name)
    {
        string text = StripExtension(name);
        string[] prefixes = { "SM_", "DH_", "M_", "T_" };
        foreach (var prefix in prefixes)
            if (text.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                text = text[prefix.Length..];

        var sb = new StringBuilder(text.Length);
        foreach (char c in text)
            sb.Append(c == '_' || c == '-' ? ' ' : c);
        return sb.ToString().Trim();
    }

    string StripExtension(string name)
    {
        int dot = name.LastIndexOf('.');
        return dot > 0 ? name[..dot] : name;
    }

    // ── 카테고리 탭 UI 구성 ──────────────────────────────
    // 로드된 카테고리 SO들로 "전체" + 메인 필터 행을 동적 생성하고, 서브 필터 그리드 자리를 마련
    void BuildCategoryTabs(Transform parent)
    {
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
        row.AddComponent<Image>().color = BG_CARD;

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6;
        hlg.padding = new RectOffset(10, 10, 6, 6);
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

        grid.AddComponent<Image>().color = BG_CARD;

        var glg = grid.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(60f, 24f);
        glg.spacing = new Vector2(6f, 6f);
        glg.padding = new RectOffset(10, 10, 8, 8);
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
        img.sprite = RoundedRectSprite(8f);
        img.type = Image.Type.Sliced;
        img.color = BTN_GHOST;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = Color.clear;
        outline.effectDistance = new Vector2(1, -1);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cs = btn.colors;
        cs.highlightedColor = BTN_HOVER;
        cs.pressedColor = BTN_PRESS;
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
        btn.GetComponent<Image>().color = active ? BTN_ACTIVE : BTN_GHOST;

        var outline = btn.GetComponent<Outline>();
        if (outline != null)
            outline.effectColor = active ? GLOW : Color.clear;

        var txt = btn.GetComponentInChildren<TMP_Text>();
        if (txt != null)
            txt.color = active ? TEXT_ON_ACCENT : TEXT_SUB;
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
                    // 지연 로딩 — 이 시점에 Resources.Load 실행 (그리드에 보이는 것만 로드)
                    var prefab = entry.prefab;
                    if (prefab == null) continue;
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
        }

        return result;
    }

    // 검색어가 prefab 이름, 한글 표시명, 검색 태그(동의어) 중 하나에라도 포함되면 매치
    bool MatchesSearch(HanokAssetEntry asset)
    {
        if (asset.assetKey.IndexOf(_searchQuery, System.StringComparison.OrdinalIgnoreCase) >= 0)
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

        go.AddComponent<Image>().color = Color.clear;

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
        img.sprite = RoundedRectSprite(8f);
        img.type = Image.Type.Sliced;
        img.color = BG_CARD_SOLID;
        img.material = GlassMaterial();
        AddInnerGlow(go, 8f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cs = btn.colors;
        cs.normalColor = BG_CARD_SOLID;
        cs.highlightedColor = HexA("#FFFFFF", 0.92f);
        cs.pressedColor = HexA("#FFFFFF", 0.98f);
        btn.colors = cs;
        btn.onClick.AddListener(() => onClick?.Invoke());

        // 썸네일 모서리를 둥글게 — RoundedRectSprite를 마스크로 사용해 RawImage를 클리핑
        var thumbMask = new GameObject("ThumbMask");
        thumbMask.transform.SetParent(go.transform, false);
        var tmRT = thumbMask.AddComponent<RectTransform>();
        tmRT.anchorMin = new Vector2(0, 0.26f);
        tmRT.anchorMax = Vector2.one;
        tmRT.offsetMin = new Vector2(3, 0);
        tmRT.offsetMax = new Vector2(-3, -3);

        var tmImg = thumbMask.AddComponent<Image>();
        tmImg.sprite = RoundedRectSprite(6f);
        tmImg.type = Image.Type.Sliced;
        tmImg.color = Color.white;
        tmImg.raycastTarget = false;
        var mask = thumbMask.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        var thumb = new GameObject("Thumb");
        thumb.transform.SetParent(thumbMask.transform, false);
        var tRT = thumb.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = tRT.offsetMax = Vector2.zero;

        var raw = thumb.AddComponent<RawImage>();
        raw.color = Hex("#E4DED4");

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
        AddTextHalo(t);

        return raw;
    }

    // ── 썸네일 순차 큐 ───────────────────────────────────
    // RefreshAssetList·AI 패널 모두 이 메서드로 요청 → 프레임당 1개씩 순차 처리
    void EnqueueThumbnail(GameObject prefab, RawImage target)
    {
        _thumbQueue.Enqueue((prefab, target));
        if (!_thumbQueueRunning)
        {
            // 동일 프레임에 다수 호출 시 코루틴이 중복 시작되지 않도록
            // StartCoroutine 전에 미리 플래그를 세운다
            _thumbQueueRunning = true;
            StartCoroutine(ProcessThumbnailQueue());
        }
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
    // orthographic 카메라로 prefab을 먼 곳에 임시 인스턴스화해 찍은 뒤 RenderTexture로 표시
    IEnumerator CaptureThumbnail(GameObject prefab, RawImage target)
    {
        yield return null;
        if (target == null) yield break;

        EnsureThumbCam();

        // 메인 카메라 clipping 범위(~1000) 밖에 배치 + layer 30 → 메인 카메라에 보이지 않음
        const float FAR = 8000f;
        var previewOrigin = new Vector3(FAR, 0f, FAR);
        var inst = Instantiate(prefab, previewOrigin, Quaternion.identity);
        // HideInHierarchy: Hierarchy 창에서 숨기되 씬 그래프에는 포함 → URP Render Graph가 정상 인식
        inst.hideFlags = HideFlags.HideInHierarchy;
        SetLayerAll(inst, THUMB_LAYER);
        FixMaterialColors(inst);
        ImproveThumbnailMaterialContrast(inst);
        NormalizeThumbnailInstance(inst, previewOrigin);

        var rends = inst.GetComponentsInChildren<Renderer>(true);
        var bounds = GetRendererBounds(rends, inst.transform.position);
        FitThumbnailCamera(bounds);

        var rt = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGB32);
        rt.Create();
        _thumbCam.targetTexture = rt;
        _thumbCam.enabled = true;
        // Unity 6 URP Render Graph는 Camera.Render()를 지원하지 않음.
        // 카메라를 enabled=true로 두고 yield null로 URP가 다음 프레임에 카메라 목록을 갱신한 뒤,
        // WaitForEndOfFrame으로 그 프레임의 모든 카메라 렌더링이 끝난 시점에 RT를 읽음.
        yield return null;
        yield return new WaitForEndOfFrame();
        _thumbCam.enabled = false;
        _thumbCam.targetTexture = null;

        if (target != null) AssignThumbnail(target, rt);
        else { rt.Release(); Destroy(rt); }
        Destroy(inst);
    }

    void NormalizeThumbnailInstance(GameObject inst, Vector3 origin)
    {
        if (inst.transform.localScale.magnitude > 50f)
            inst.transform.localScale = Vector3.one;

        var rends = inst.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return;

        var bounds = GetRendererBounds(rends, inst.transform.position);
        float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (maxSize > 0.001f)
        {
            const float TARGET_THUMB_SIZE = 2.2f;
            float scale = TARGET_THUMB_SIZE / maxSize;
            inst.transform.localScale *= Mathf.Clamp(scale, 0.001f, 1000f);
        }

        rends = inst.GetComponentsInChildren<Renderer>();
        bounds = GetRendererBounds(rends, inst.transform.position);
        Vector3 centeredBottom = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        inst.transform.position += origin - centeredBottom;
    }

    void AssignThumbnail(RawImage target, Texture texture)
    {
        if (target == null || texture == null) return;
        target.texture = texture;
        target.color = Color.white;
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

    static void ImproveThumbnailMaterialContrast(GameObject obj)
    {
        foreach (var r in obj.GetComponentsInChildren<Renderer>())
        {
            var mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m == null) continue;

                bool hasTexture =
                    (m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null) ||
                    (m.HasProperty("_MainTex") && m.GetTexture("_MainTex") != null);
                if (hasTexture) continue;

                Color color = Color.white;
                if (m.HasProperty("_BaseColor")) color = m.GetColor("_BaseColor");
                else if (m.HasProperty("_Color")) color = m.GetColor("_Color");

                bool nearlyWhite = color.r > 0.92f && color.g > 0.92f && color.b > 0.92f;
                if (!nearlyWhite) continue;

                Color thumbnailTint = Hex("#D4C8B8");
                thumbnailTint.a = color.a;
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", thumbnailTint);
                if (m.HasProperty("_Color")) m.SetColor("_Color", thumbnailTint);
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

        float padding = 1.18f;
        _thumbCam.orthographicSize = Mathf.Max(maxY, maxX, 0.5f) * padding;
        _thumbCam.transform.rotation = viewRot;
        _thumbCam.transform.position = cen + viewDir * Mathf.Max(maxZ + 10f, 10f);
        _thumbCam.nearClipPlane = 0.01f;
        _thumbCam.farClipPlane  = Mathf.Max(maxZ + 30f, 50f);
    }

    void EnsureThumbCam()
    {
        if (_thumbCam != null) return;

        var go = new GameObject("_HanokThumbCam");
        // HideInHierarchy만 설정: 하이어라키에서 숨기되 Camera.allCameras에는 포함됨
        go.hideFlags  = HideFlags.HideInHierarchy;
        _thumbCam     = go.AddComponent<Camera>();
        _thumbCam.enabled          = false;  // 캡처 직전에만 활성화
        _thumbCam.clearFlags       = CameraClearFlags.SolidColor;
        _thumbCam.backgroundColor  = Hex("#EEE8DC");
        _thumbCam.orthographic     = true;
        _thumbCam.nearClipPlane    = 0.01f;
        _thumbCam.farClipPlane     = 100f;
        _thumbCam.cullingMask      = 1 << THUMB_LAYER;  // layer 30 (31은 int overflow)
        _thumbCam.allowMSAA        = false;
        // URP가 이 카메라를 Base Camera로 정식 인식하도록 필수 컴포넌트 추가
        var urpData = go.AddComponent<UniversalAdditionalCameraData>();
        urpData.renderType = CameraRenderType.Base;
        urpData.renderShadows = false;  // 썸네일용 — 그림자 불필요, 성능 최적화

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
