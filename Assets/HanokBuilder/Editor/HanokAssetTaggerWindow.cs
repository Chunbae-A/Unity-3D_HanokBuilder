using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// Self-service tool: pick a folder and a set of HanokAssetCategory assets,
/// then bulk-apply HanokAssetTags to every prefab found inside that folder (and subfolders).
/// Also exposes the one-time initial migration (folder structure -> category assets + tags)
/// as a menu command built on the same TagFolder logic, so there is a single place that
/// knows how to tag a folder.
public class HanokAssetTaggerWindow : EditorWindow
{
    DefaultAsset _targetFolder;
    readonly List<HanokAssetCategory> _categories = new List<HanokAssetCategory>();
    Vector2 _scroll;

    [MenuItem("HanokBuilder/Tools/Asset Tagger")]
    public static void Open()
    {
        var window = GetWindow<HanokAssetTaggerWindow>();
        window.titleContent = new GUIContent("Hanok Asset Tagger");
        window.minSize = new Vector2(320, 280);
        window.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "선택한 폴더(하위 폴더 포함) 안의 모든 prefab에 HanokAssetTags를 부착하고\n" +
            "아래 카테고리 목록으로 categories를 채워 넣습니다 (기존 태그는 덮어씁니다).",
            MessageType.Info);

        EditorGUILayout.Space();
        _targetFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "대상 폴더", _targetFolder, typeof(DefaultAsset), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("적용할 카테고리", EditorStyles.boldLabel);

        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MinHeight(100));
        for (int i = 0; i < _categories.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            _categories[i] = (HanokAssetCategory)EditorGUILayout.ObjectField(
                _categories[i], typeof(HanokAssetCategory), false);
            if (GUILayout.Button("✕", GUILayout.Width(24)))
            {
                _categories.RemoveAt(i);
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("+ 카테고리 슬롯 추가"))
            _categories.Add(null);

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(!CanApply()))
        {
            if (GUILayout.Button("선택한 폴더에 태깅 적용", GUILayout.Height(32)))
                Apply();
        }
    }

    bool CanApply()
    {
        return _targetFolder != null
            && _categories.Count > 0
            && _categories.All(c => c != null);
    }

    void Apply()
    {
        var folderPath = AssetDatabase.GetAssetPath(_targetFolder);
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            EditorUtility.DisplayDialog("Hanok Asset Tagger", "폴더를 선택해주세요.", "확인");
            return;
        }

        var categories = _categories.ToArray();
        int tagged = TagFolder(folderPath, categories);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var labels = string.Join(", ", categories.Select(c => c.label));
        Debug.Log($"[HanokBuilder] '{folderPath}' — prefab {tagged}개를 [{labels}] 카테고리로 태깅했습니다");
        EditorUtility.DisplayDialog("Hanok Asset Tagger", $"prefab {tagged}개를 태깅했습니다.\n카테고리: {labels}", "확인");
    }

    // ── 폴더 단위 태깅 (창과 마이그레이션이 공유하는 핵심 로직) ──────────────

    static int TagFolder(string folderPath, HanokAssetCategory[] categories)
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogWarning($"[HanokBuilder] 폴더 없음: {folderPath}");
            return 0;
        }

        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
        int count = 0;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var tags = contents.GetComponent<HanokAssetTags>();
                if (tags == null)
                    tags = contents.AddComponent<HanokAssetTags>();

                tags.categories = categories;
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                count++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        return count;
    }

    // ── 1회성 초기 마이그레이션 (기존 폴더 구조 -> 카테고리 SO 생성 + 일괄 태깅) ──

    const string ASSETS_ROOT     = "Assets/HanokBuilder/Resources/HanokAssets";
    const string CATEGORIES_ROOT = "Assets/HanokBuilder/Resources/HanokCategories";

    const string CAT_COMPLETE = "Complete";
    const string CAT_PARTS    = "Parts";

    static readonly (string key, string label, int order)[] PART_SUBS =
    {
        ("Beam",       "보",   0),
        ("Dancheong",  "단청", 1),
        ("Decoration", "장식", 2),
        ("Door",       "문",   3),
        ("Floor",      "바닥", 4),
        ("Handrail",   "난간", 5),
        ("Maru",       "마루", 6),
        ("Natural",    "자연", 7),
        ("Roof",       "지붕", 8),
        ("Wall",       "벽체", 9),
        ("Wood",       "목재", 10),
    };

    [MenuItem("HanokBuilder/Tools/Migrate Asset Categories")]
    public static void Migrate()
    {
        var complete = GetOrCreateCategory(CAT_COMPLETE, "완성형", null, 0);
        var parts    = GetOrCreateCategory(CAT_PARTS, "부품형", null, 1);

        var subs = new Dictionary<string, HanokAssetCategory>();
        foreach (var s in PART_SUBS)
            subs[s.key] = GetOrCreateCategory(s.key, s.label, parts, s.order);

        AssetDatabase.SaveAssets();

        int tagged = 0;
        tagged += TagFolder($"{ASSETS_ROOT}/{CAT_COMPLETE}", new[] { complete });

        foreach (var s in PART_SUBS)
            tagged += TagFolder($"{ASSETS_ROOT}/Parts/{s.key}", new[] { parts, subs[s.key] });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[HanokBuilder] 카테고리 마이그레이션 완료 — prefab {tagged}개 태깅됨");
    }

    static HanokAssetCategory GetOrCreateCategory(string key, string label, HanokAssetCategory parent, int order)
    {
        if (!AssetDatabase.IsValidFolder(CATEGORIES_ROOT))
            CreateFolderRecursive(CATEGORIES_ROOT);

        var path = $"{CATEGORIES_ROOT}/Category_{key}.asset";
        var cat = AssetDatabase.LoadAssetAtPath<HanokAssetCategory>(path);
        if (cat == null)
        {
            cat = ScriptableObject.CreateInstance<HanokAssetCategory>();
            AssetDatabase.CreateAsset(cat, path);
        }

        cat.key = key;
        cat.label = label;
        cat.parent = parent;
        cat.order = order;
        EditorUtility.SetDirty(cat);

        return cat;
    }

    static void CreateFolderRecursive(string path)
    {
        var parts = path.Split('/');
        var current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
