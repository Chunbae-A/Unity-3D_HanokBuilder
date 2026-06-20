using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;

public class HanokPartsMeasure : EditorWindow
{
    [MenuItem("Hanok/Measure Assembly Parts")]
    static void MeasureParts()
    {
        var targets = new[]
        {
            // 기단
            "HanokAssets/Parts/Decoration/SM_W_Foundation_1",
            "HanokAssets/Parts/Decoration/SM_W_Foundation_2",
            "HanokAssets/Parts/Decoration/SM_W_Foundation_3",
            "HanokAssets/Parts/Decoration/SM_W_Foundation_4",
            // 기둥 전체
            "HanokAssets/Parts/Wood/SM_P_Wood_1",
            "HanokAssets/Parts/Wood/SM_P_Wood_2",
            "HanokAssets/Parts/Wood/SM_P_Wood_3",
            "HanokAssets/Parts/Wood/SM_P_Wood_4",
            // 벽 전체
            "HanokAssets/Parts/Wall/SM_W_Wood_1",
            "HanokAssets/Parts/Wall/SM_W_Wood_2",
            "HanokAssets/Parts/Wall/SM_W_Stone_1",
            "HanokAssets/Parts/Wall/SM_W_White",
            "HanokAssets/Parts/Wall/SM_W_Pink01",
            // 마루 전체
            "HanokAssets/Parts/Maru/SM_F_Wood_1",
            "HanokAssets/Parts/Maru/SM_F_Wood_2",
            "HanokAssets/Parts/Maru/SM_F_Wood_3",
            "HanokAssets/Parts/Maru/SM_F_Wood_4",
            // 보
            "HanokAssets/Parts/Beam/SM_R_Beam_34",
            // 단청
            "HanokAssets/Parts/Dancheong/SM_R_Dancheong_1",
            "HanokAssets/Parts/Dancheong/SM_R_Dancheong_2",
            "HanokAssets/Parts/Dancheong/SM_R_DancheongSupport_1",
            "HanokAssets/Parts/Dancheong/SM_R_DancheongCorner_1",
            // 문 전체
            "HanokAssets/Parts/Door/SM_D_Wood_1",
            "HanokAssets/Parts/Door/SM_D_Wood_2",
            "HanokAssets/Parts/Door/SM_D_Wood_3",
            "HanokAssets/Parts/Door/SM_D_Wood_4",
            "HanokAssets/Parts/Door/SM_D_Wood_5_1",
            "HanokAssets/Parts/Door/SM_D_Wood_6",
            // 지붕 샘플
            "HanokAssets/Parts/Roof/SM_Roof_1",
            "HanokAssets/Parts/Roof/SM_Roof_10",
            "HanokAssets/Parts/Roof/SM_Roof_20",
            "HanokAssets/Parts/Roof/SM_Roof_30",
            "HanokAssets/Parts/Roof/SM_Roof_40",
            "HanokAssets/Parts/Roof/SM_Roof_48",
        };

        var sb = new StringBuilder();
        sb.AppendLine("name\twidth(X)\theight(Y)\tdepth(Z)\tpivot_offset_Y");

        foreach (var path in targets)
        {
            var go = Resources.Load<GameObject>(path);
            if (go == null) { sb.AppendLine($"{System.IO.Path.GetFileName(path)}\t[NOT FOUND]"); continue; }

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(go);
            inst.transform.position = Vector3.zero;

            Bounds b = new Bounds(inst.transform.position, Vector3.zero);
            foreach (var r in inst.GetComponentsInChildren<Renderer>())
                b.Encapsulate(r.bounds);

            float pivotOffsetY = inst.transform.position.y - b.min.y;

            sb.AppendLine($"{go.name}\t{b.size.x:F3}\t{b.size.y:F3}\t{b.size.z:F3}\t{pivotOffsetY:F3}");
            Object.DestroyImmediate(inst);
        }

        string outPath = "Assets/Editor/HanokPartsData.txt";
        File.WriteAllText(outPath, sb.ToString());
        AssetDatabase.Refresh();
        Debug.Log($"[HanokMeasure] 저장 완료: {outPath}\n\n" + sb.ToString());
        EditorUtility.DisplayDialog("측정 완료", $"결과:\n\n{sb}", "OK");
    }
}
