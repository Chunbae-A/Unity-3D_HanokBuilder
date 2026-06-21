using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// drive_asset_sync.py 로 내려받은 4개 카테고리 폴더의 FBX를 스캔해
/// Prefab 및 HanokAssetInfo SO를 일괄 생성한다.
/// 이미 존재하는 항목은 건드리지 않으므로 재실행 안전.
public static class CultureAssetImporter
{
    const string HANOK_ASSETS_ROOT = "Assets/HanokBuilder/Resources/HanokAssets";
    const string CATEGORIES_ROOT   = "Assets/HanokBuilder/Resources/HanokCategories";
    const string ASSETINFO_ROOT    = "Assets/HanokBuilder/Resources/HanokAssetInfo";

    // (Drive 폴더명, 카테고리 키, 한글 라벨, 기본 검색태그)
    static readonly (string folder, string catKey, string label, string[] tags)[] CATEGORY_DEFS =
    {
        ("건축물완성형", "Complete",     "건축물 완성형", new[] { "건축물", "완성형", "한옥", "전통건축" }),
        ("건축물부품형", "Parts",        "건축물 부품형", new[] { "건축물", "부품", "부품형", "한옥" }),
        ("디지털휴먼",  "DigitalHuman", "디지털 휴먼",  new[] { "디지털휴먼", "캐릭터", "인물", "사람" }),
        ("공간소품",   "Props",        "공간 소품",   new[] { "공간소품", "소품", "인테리어", "오브젝트" }),
    };

    [MenuItem("HanokBuilder/Tools/Import Culture Assets")]
    public static void Run()
    {
        EnsureFolder(ASSETINFO_ROOT);

        int totalPrefab = 0, totalInfo = 0, skippedPrefab = 0, skippedInfo = 0;

        foreach (var def in CATEGORY_DEFS)
        {
            var catFolder = $"{HANOK_ASSETS_ROOT}/{def.folder}";
            if (!AssetDatabase.IsValidFolder(catFolder))
            {
                Debug.LogWarning($"[CultureImport] 폴더 없음 — drive_asset_sync.py 먼저 실행: {catFolder}");
                continue;
            }

            var cat         = GetOrCreateCategory(def.catKey, def.label);
            var prefabsDir  = $"{catFolder}/Prefabs";
            var infoDir     = $"{ASSETINFO_ROOT}/{def.folder}";
            EnsureFolder(prefabsDir);
            EnsureFolder(infoDir);

            // Prefabs 서브폴더를 제외한 FBX 탐색 (재귀)
            var guids = AssetDatabase.FindAssets("t:Model", new[] { catFolder });
            foreach (var guid in guids)
            {
                var fbxPath = AssetDatabase.GUIDToAssetPath(guid);
                if (fbxPath.Replace("\\", "/").Contains("/Prefabs/")) continue;

                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                if (fbx == null) continue;

                var assetKey  = "CM_" + Path.GetFileNameWithoutExtension(fbxPath);
                var prefabPath = $"{prefabsDir}/{assetKey}.prefab";

                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
                {
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                    inst.name = assetKey;

                    var tags = inst.GetComponent<HanokAssetTags>()
                               ?? inst.AddComponent<HanokAssetTags>();
                    tags.categories = cat != null
                        ? new[] { cat }
                        : System.Array.Empty<HanokAssetCategory>();

                    PrefabUtility.SaveAsPrefabAsset(inst, prefabPath);
                    Object.DestroyImmediate(inst);
                    totalPrefab++;
                }
                else
                {
                    skippedPrefab++;
                }

                var infoPath = $"{infoDir}/AssetInfo_{assetKey}.asset";
                if (AssetDatabase.LoadAssetAtPath<HanokAssetInfo>(infoPath) == null)
                {
                    var info = ScriptableObject.CreateInstance<HanokAssetInfo>();
                    info.assetKey    = assetKey;
                    info.displayName = Path.GetFileNameWithoutExtension(fbxPath);
                    info.tags        = def.tags;
                    AssetDatabase.CreateAsset(info, infoPath);
                    totalInfo++;
                }
                else
                {
                    skippedInfo++;
                }
            }

            Debug.Log($"[CultureImport] {def.folder} 처리 완료");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"[CultureImport] 완료 — " +
            $"Prefab 생성 {totalPrefab}개 (스킵 {skippedPrefab}) / " +
            $"AssetInfo 생성 {totalInfo}개 (스킵 {skippedInfo})"
        );
    }

    static HanokAssetCategory GetOrCreateCategory(string key, string label)
    {
        var path = $"{CATEGORIES_ROOT}/Category_{key}.asset";
        var cat  = AssetDatabase.LoadAssetAtPath<HanokAssetCategory>(path);
        if (cat != null) return cat;

        cat       = ScriptableObject.CreateInstance<HanokAssetCategory>();
        cat.key   = key;
        cat.label = label;
        EnsureFolder(CATEGORIES_ROOT);
        AssetDatabase.CreateAsset(cat, path);
        Debug.Log($"[CultureImport] 신규 카테고리 생성: {key}");
        return cat;
    }

    static void EnsureFolder(string assetPath)
    {
        var parts   = assetPath.Split('/');
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
