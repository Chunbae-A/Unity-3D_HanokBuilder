using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class AssetTransformEditor
{
    private static float uniformScale = 1f;

    public static void Draw(
        ref GameObject selectedAsset,
        ref bool snapEnabled,
        ref float snapGridSize,
        List<GameObject> placedAssets)
    {
        if (selectedAsset == null)
        {
            EditorGUILayout.HelpBox("씬에서 오브젝트를 클릭하거나\n에셋 탭 목록에서 선택하세요.", MessageType.None);
            return;
        }

        GUILayout.Label("선택됨:", EditorStyles.miniLabel);
        EditorGUILayout.LabelField(selectedAsset.name, EditorStyles.boldLabel);
        GUILayout.Space(8);

        Undo.RecordObject(selectedAsset.transform, "Transform Edit");
        Transform t = selectedAsset.transform;

        // ── Position ─────────────────────────────────────
        GUILayout.Label("Position", EditorStyles.boldLabel);
        t.position = EditorGUILayout.Vector3Field("", t.position);

        GUILayout.BeginHorizontal();
        GUILayout.Label("X", GUILayout.Width(12));
        float px = EditorGUILayout.Slider(t.position.x, -20f, 20f);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Y", GUILayout.Width(12));
        float py = EditorGUILayout.Slider(t.position.y, 0f, 10f);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Z", GUILayout.Width(12));
        float pz = EditorGUILayout.Slider(t.position.z, -20f, 20f);
        GUILayout.EndHorizontal();

        t.position = new Vector3(px, py, pz);
        GUILayout.Space(8);

        // ── Rotation ──────────────────────────────────────
        GUILayout.Label("Rotation (Y축)", EditorStyles.boldLabel);
        Vector3 euler = t.eulerAngles;
        euler.y = EditorGUILayout.Slider(euler.y, 0f, 360f);
        t.eulerAngles = euler;

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("-90°"))   t.Rotate(0, -90, 0);
        if (GUILayout.Button("+90°"))   t.Rotate(0,  90, 0);
        if (GUILayout.Button("정면"))   t.eulerAngles = Vector3.zero;
        GUILayout.EndHorizontal();
        GUILayout.Space(8);

        // ── Scale ─────────────────────────────────────────
        GUILayout.Label("Scale (균등)", EditorStyles.boldLabel);
        uniformScale = EditorGUILayout.Slider(uniformScale, 0.1f, 5f);
        t.localScale = Vector3.one * uniformScale;

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("0.5×")) { uniformScale = 0.5f; t.localScale = Vector3.one * 0.5f; }
        if (GUILayout.Button("1×"))   { uniformScale = 1.0f; t.localScale = Vector3.one;         }
        if (GUILayout.Button("2×"))   { uniformScale = 2.0f; t.localScale = Vector3.one * 2.0f; }
        GUILayout.EndHorizontal();
        GUILayout.Space(12);

        // ── 스냅 설정 ─────────────────────────────────────
        GUILayout.Label("스냅 설정", EditorStyles.boldLabel);
        snapEnabled = EditorGUILayout.Toggle("그리드 스냅", snapEnabled);
        if (snapEnabled)
        {
            snapGridSize = EditorGUILayout.Slider("그리드 단위(m)", snapGridSize, 0.1f, 2f);
            if (GUILayout.Button("현재 위치를 그리드에 맞추기"))
            {
                Vector3 p = t.position;
                p.x = Mathf.Round(p.x / snapGridSize) * snapGridSize;
                p.z = Mathf.Round(p.z / snapGridSize) * snapGridSize;
                t.position = p;
            }
        }
        GUILayout.Space(12);

        // ── 삭제 / 복제 ───────────────────────────────────
        GUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f);
        if (GUILayout.Button("삭제", GUILayout.Height(28)))
        {
            if (EditorUtility.DisplayDialog("삭제 확인",
                $"'{selectedAsset.name}'을(를) 삭제하시겠습니까?", "삭제", "취소"))
            {
                placedAssets.Remove(selectedAsset);
                Undo.DestroyObjectImmediate(selectedAsset);
                selectedAsset = null;
                return;
            }
        }

        GUI.backgroundColor = new Color(0.15f, 0.4f, 0.7f);
        if (GUILayout.Button("복제", GUILayout.Height(28)))
        {
            GameObject clone = Object.Instantiate(selectedAsset);
            clone.name = selectedAsset.name + "_복사";
            clone.transform.position = selectedAsset.transform.position + new Vector3(0.5f, 0, 0.5f);
            Undo.RegisterCreatedObjectUndo(clone, "Duplicate Asset");
            placedAssets.Add(clone);
            Selection.activeGameObject = clone;
            selectedAsset = clone;
        }

        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        GUILayout.Space(8);

        // ── Prefab 저장 ───────────────────────────────────
        GUI.backgroundColor = new Color(0.1f, 0.5f, 0.4f);
        if (GUILayout.Button("Prefab으로 저장", GUILayout.Height(32)))
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Prefab 저장",
                selectedAsset.name + "_HanokSet",
                "prefab",
                "저장할 위치를 선택하세요",
                "Assets/HanokBuilder/Prefabs");

            if (!string.IsNullOrEmpty(path))
            {
                PrefabUtility.SaveAsPrefabAsset(selectedAsset, path);
                Debug.Log($"[HanokBuilder] Prefab 저장 완료: {path}");
            }
        }
        GUI.backgroundColor = Color.white;
    }
}