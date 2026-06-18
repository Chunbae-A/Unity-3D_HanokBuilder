#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class HanokAssetVerifier : EditorWindow
{
    static readonly string ASSETS_ROOT = "Assets/HanokBuilder/Resources/HanokAssets";
    static readonly string[] EXPECTED_FOLDERS = { "Complete", "Meshes", "Parts", "SunaedongHouse", "Textures", "Material", "CultureMetaverse" };
    static readonly string[] BINARY_EXTS = { ".fbx", ".FBX", ".obj", ".png", ".jpg", ".tga", ".tiff", ".exr" };

    Vector2 _scroll;

    [MenuItem("HanokBuilder/에셋 설정 확인 (Drive)")]
    static void ShowWindow()
    {
        var w = GetWindow<HanokAssetVerifier>("에셋 설정 확인");
        w.minSize = new Vector2(440, 340);
        w.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("HanokBuilder 에셋 설정 확인", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        EditorGUILayout.HelpBox(
            "FBX / 텍스처 바이너리는 Google Drive에서 별도 관리됩니다.\n\n" +
            "[셋업 방법]\n" +
            "1. Google Drive > HanokAssets_Binaries 폴더 다운로드\n" +
            "2. 폴더 내용을 Unity 프로젝트의 아래 경로에 복사:\n" +
            "   " + ASSETS_ROOT + "/\n" +
            "3. Unity 에디터 재실행 (또는 Assets > Refresh)",
            MessageType.Info);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("폴더별 바이너리 상태", EditorStyles.boldLabel);

        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));

        string projectRoot = Application.dataPath.Replace("/Assets", "").Replace("\\Assets", "");

        bool allOk = true;
        foreach (var folder in EXPECTED_FOLDERS)
        {
            string fullPath = Path.Combine(projectRoot, ASSETS_ROOT.Replace("/", Path.DirectorySeparatorChar.ToString()), folder);
            bool folderExists = Directory.Exists(fullPath);
            bool hasBinaries = folderExists && BINARY_EXTS.Any(ext =>
                Directory.GetFiles(fullPath, "*" + ext, SearchOption.AllDirectories).Length > 0);

            if (!hasBinaries) allOk = false;

            var style = new GUIStyle(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal(style);

            string statusIcon = hasBinaries ? "[OK]" : "[없음]";
            var labelColor = hasBinaries ? Color.green : new Color(1f, 0.6f, 0f);

            var prevColor = GUI.contentColor;
            GUI.contentColor = labelColor;
            EditorGUILayout.LabelField(statusIcon, GUILayout.Width(52));
            GUI.contentColor = prevColor;

            EditorGUILayout.LabelField(folder, GUILayout.Width(160));

            string desc = hasBinaries ? "바이너리 있음" :
                          (folderExists ? "폴더는 있으나 바이너리 없음 — 드라이브에서 복사 필요" : "폴더 없음 — 드라이브에서 복사 필요");
            EditorGUILayout.LabelField(desc, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(6);
        if (allOk)
        {
            EditorGUILayout.HelpBox("모든 폴더에 바이너리가 있습니다.", MessageType.None);
        }
        else
        {
            EditorGUILayout.HelpBox("일부 폴더에 바이너리가 없습니다. 드라이브에서 복사해주세요.", MessageType.Warning);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("에셋 폴더 열기"))
        {
            string path = Path.GetFullPath(Path.Combine(projectRoot, ASSETS_ROOT.Replace("/", Path.DirectorySeparatorChar.ToString())));
            EditorUtility.RevealInFinder(path);
        }
        if (GUILayout.Button("새로고침"))
        {
            Repaint();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(8);
    }
}
#endif
