using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class HanokBuilderWindow : EditorWindow
{
    private GameObject selectedAsset;
    private Vector2 assetListScrollPos;
    private List<GameObject> placedAssets = new List<GameObject>();
    private string aiPrompt = "";
    private bool snapEnabled = true;
    private float snapGridSize = 0.25f;
    private int currentTab = 0;
    private readonly string[] tabNames = { "에셋", "편집", "AI 생성" };

    [MenuItem("HanokBuilder/에디터 열기")]
    public static void OpenWindow()
    {
        HanokBuilderWindow window = GetWindow<HanokBuilderWindow>();
        window.titleContent = new GUIContent("HanokBuilder");
        window.minSize = new Vector2(320, 500);
        window.Show();
    }

    private void OnEnable()
    {
        Selection.selectionChanged += OnSelectionChange;
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= OnSelectionChange;
    }

    private void OnSelectionChange()
    {
        if (Selection.activeGameObject != null)
        {
            selectedAsset = Selection.activeGameObject;
        }
        Repaint();
    }

    private void OnGUI()
    {
        DrawHeader();
        currentTab = GUILayout.Toolbar(currentTab, tabNames);
        GUILayout.Space(4);

        switch (currentTab)
        {
            case 0: DrawAssetTab();  break;
            case 1: DrawEditTab();   break;
            case 2: DrawAITab();     break;
        }
    }

    private void DrawHeader()
    {
        Rect headerRect = GUILayoutUtility.GetRect(0, 36, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(headerRect, new Color(0.23f, 0.20f, 0.54f));
        GUI.Label(headerRect, "  HanokBuilder", new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            normal = { textColor = Color.white },
            alignment = TextAnchor.MiddleLeft
        });
    }

    private void DrawAssetTab()
    {
        GUILayout.Label("배치된 에셋 목록", EditorStyles.boldLabel);

        assetListScrollPos = EditorGUILayout.BeginScrollView(assetListScrollPos, GUILayout.Height(200));
        for (int i = 0; i < placedAssets.Count; i++)
        {
            if (placedAssets[i] == null) continue;
            bool isSelected = (placedAssets[i] == selectedAsset);
            GUI.backgroundColor = isSelected ? new Color(0.6f, 0.5f, 1f) : Color.white;
            if (GUILayout.Button(placedAssets[i].name, GUILayout.Height(28)))
            {
                selectedAsset = placedAssets[i];
                Selection.activeGameObject = placedAssets[i];
                SceneView.lastActiveSceneView?.FrameSelected();
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndScrollView();

        GUILayout.Space(8);
        DrawDropArea();
        GUILayout.Space(8);

        if (GUILayout.Button("씬의 에셋 목록 갱신", GUILayout.Height(30)))
        {
            RefreshSceneAssets();
        }
    }

    private void DrawDropArea()
    {
        Rect dropRect = GUILayoutUtility.GetRect(0, 60, GUILayout.ExpandWidth(true));
        GUI.Box(dropRect, "여기에 Prefab을 드래그해서 씬에 배치");

        Event evt = Event.current;
        if (evt.type == EventType.DragUpdated && dropRect.Contains(evt.mousePosition))
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            evt.Use();
        }
        else if (evt.type == EventType.DragPerform && dropRect.Contains(evt.mousePosition))
        {
            DragAndDrop.AcceptDrag();
            foreach (Object obj in DragAndDrop.objectReferences)
            {
                if (obj is GameObject prefab)
                    PlaceAssetInScene(prefab);
            }
            evt.Use();
        }
    }

    private void PlaceAssetInScene(GameObject prefab)
    {
        Vector3 spawnPos = Vector3.zero;
        if (SceneView.lastActiveSceneView != null)
        {
            Camera cam = SceneView.lastActiveSceneView.camera;
            spawnPos = cam.transform.position + cam.transform.forward * 5f;
        }

        if (snapEnabled)
        {
            spawnPos.x = Mathf.Round(spawnPos.x / snapGridSize) * snapGridSize;
            spawnPos.z = Mathf.Round(spawnPos.z / snapGridSize) * snapGridSize;
            spawnPos.y = 0f;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        instance.transform.position = spawnPos;
        Undo.RegisterCreatedObjectUndo(instance, "Place Hanok Asset");

        placedAssets.Add(instance);
        Selection.activeGameObject = instance;
        selectedAsset = instance;
        Repaint();
    }

    private void RefreshSceneAssets()
    {
        placedAssets.Clear();
        foreach (GameObject obj in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (obj.transform.parent == null &&
                obj.GetComponent<Camera>() == null &&
                obj.GetComponent<Light>() == null)
            {
                placedAssets.Add(obj);
            }
        }
        Repaint();
    }

    private void DrawEditTab()
    {
        AssetTransformEditor.Draw(ref selectedAsset, ref snapEnabled, ref snapGridSize, placedAssets);
    }

    private void DrawAITab()
    {
        GUILayout.Label("AI 공간 생성", EditorStyles.boldLabel);
        GUILayout.Space(4);
        EditorGUILayout.HelpBox("원하는 공간을 텍스트로 입력하면\nAI가 에셋 배치 좌표를 생성합니다.", MessageType.Info);
        GUILayout.Space(8);
        GUILayout.Label("공간 설명:");
        aiPrompt = EditorGUILayout.TextArea(aiPrompt, GUILayout.Height(80));
        GUILayout.Space(8);
        GUI.backgroundColor = new Color(0.3f, 0.25f, 0.7f);
        if (GUILayout.Button("AI로 공간 생성 →", GUILayout.Height(36)))
        {
            Debug.Log($"[HanokBuilder] AI 요청: {aiPrompt}");
        }
        GUI.backgroundColor = Color.white;
    }
}