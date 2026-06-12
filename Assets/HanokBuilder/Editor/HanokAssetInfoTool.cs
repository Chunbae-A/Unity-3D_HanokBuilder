using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// Complete/Parts 안의 모든 prefab 목록을 콘솔에 출력하고,
/// 각 prefab마다 HanokAssetInfo(한글 표시명) SO가 없으면 새로 만들어준다.
/// 이미 있는 SO는 건드리지 않으므로, displayName은 인스펙터에서 자유롭게 수정해도 안전하다.
public static class HanokAssetInfoTool
{
    const string ASSETS_ROOT     = "Assets/HanokBuilder/Resources/HanokAssets";
    const string ASSETINFO_ROOT  = "Assets/HanokBuilder/Resources/HanokAssetInfo";

    [MenuItem("HanokBuilder/Tools/List Assets and Generate Display Names")]
    public static void ListAndGenerate()
    {
        var guids = AssetDatabase.FindAssets("t:Prefab", new[]
        {
            $"{ASSETS_ROOT}/Complete",
            $"{ASSETS_ROOT}/Parts",
        });

        var paths = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(p => p, System.StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!AssetDatabase.IsValidFolder(ASSETINFO_ROOT))
            CreateFolderRecursive(ASSETINFO_ROOT);

        int created = 0;
        foreach (var path in paths)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            Debug.Log($"[HanokBuilder] {name}  ({path})");

            var infoPath = $"{ASSETINFO_ROOT}/AssetInfo_{name}.asset";
            if (AssetDatabase.LoadAssetAtPath<HanokAssetInfo>(infoPath) != null)
                continue;

            var info = ScriptableObject.CreateInstance<HanokAssetInfo>();
            info.assetKey = name;
            info.displayName = name;
            AssetDatabase.CreateAsset(info, infoPath);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[HanokBuilder] prefab 총 {paths.Count}개 / 새로 생성된 AssetInfo {created}개 ({ASSETINFO_ROOT})");
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
