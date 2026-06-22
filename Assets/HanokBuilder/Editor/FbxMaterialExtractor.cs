using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class FbxMaterialExtractor
{
    // .fbm 폴더가 없어서 SharedTextures에서 텍스처를 찾아야 하는 폴더만 처리
    static readonly string[] TARGET_FOLDERS = {
        "Assets/HanokBuilder/Resources/HanokAssets/건축물완성형",
        "Assets/HanokBuilder/Resources/HanokAssets/건축물부품형",
        "Assets/HanokBuilder/Resources/HanokAssets/공간소품",
        "Assets/HanokBuilder/Resources/HanokAssets/디지털휴먼",
    };
    const int BATCH_SIZE = 1;

    static Queue<string> _queue;
    static int _total, _processed;

    [MenuItem("HanokBuilder/Tools/Extract & Upgrade FBX Materials")]
    public static void Run()
    {
        var guids = AssetDatabase.FindAssets("t:Model", TARGET_FOLDERS);
        _queue = new Queue<string>(
            guids.Where(g => {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (p.Contains("/Prefabs/")) return false;
                var imp = AssetImporter.GetAtPath(p) as ModelImporter;
                return imp != null && imp.materialSearch != ModelImporterMaterialSearch.Everywhere;
            })
        );
        _total     = _queue.Count;
        _processed = 0;

        if (_total == 0)
        {
            EditorUtility.DisplayDialog("완료", "모든 FBX가 이미 External로 설정되어 있습니다.", "확인");
            return;
        }

        Debug.Log($"[FbxMaterialExtractor] {_total}개 FBX 처리 시작 (배치 {BATCH_SIZE}개씩)");
        EditorApplication.delayCall += ProcessBatch;
    }

    static void ProcessBatch()
    {
        for (int i = 0; i < BATCH_SIZE && _queue.Count > 0; i++)
        {
            var guid    = _queue.Dequeue();
            var fbxPath = AssetDatabase.GUIDToAssetPath(guid);
            var imp     = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (imp == null) continue;

            imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            imp.materialLocation   = ModelImporterMaterialLocation.External;
            imp.materialSearch     = ModelImporterMaterialSearch.Everywhere;
            imp.SaveAndReimport();
            _processed++;

            EditorUtility.DisplayProgressBar(
                "FBX 머티리얼 추출 중",
                $"({_processed}/{_total}) {System.IO.Path.GetFileName(fbxPath)}",
                (float)_processed / _total);
        }

        System.GC.Collect();

        if (_queue.Count > 0)
        {
            EditorApplication.delayCall += ProcessBatch;
        }
        else
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[FbxMaterialExtractor] 완료 — {_processed}개 처리");
            EditorUtility.DisplayDialog("완료",
                $"{_processed}개 FBX 머티리얼 추출 완료.\n\nWindow → Rendering → Render Pipeline Converter\n에서 Material Upgrade 실행하세요.",
                "확인");
        }
    }
}
