using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// 문화재 에셋 머티리얼 텍스처 일괄 연결.
///
/// prefix 매핑: M_/MI_/Mat_ (머티리얼) ↔ T_ (텍스처)
/// 예) M_ClothYellow_Top → 베이스명 ClothYellow_Top → T_ClothYellow_Top_BC 찾음
public static class CultureMaterialLinker
{
    static readonly string[] CULTURE_FOLDERS = {
        "Assets/HanokBuilder/Resources/HanokAssets/건축물완성형",
        "Assets/HanokBuilder/Resources/HanokAssets/건축물부품형",
        "Assets/HanokBuilder/Resources/HanokAssets/공간소품",
        "Assets/HanokBuilder/Resources/HanokAssets/디지털휴먼",
    };

    static readonly string[] MAT_PREFIXES   = { "MI_", "M_", "Mat_", "mat_" };
    static readonly string[] TEX_PREFIXES   = { "", "T_", "t_" };
    static readonly string[] BASE_SUFFIXES  = {
        "_BaseColor", "_Basecolor", "_basecolor", "_BC",
        "_Albedo", "_Diffuse", "_Diffuse_Color", "_D", "_C"
    };

    static readonly string[] ALL_TEX_SLOTS = {
        "_BaseMap", "_BumpMap", "_MetallicGlossMap", "_OcclusionMap",
        "_EmissionMap", "_DetailMask", "_DetailAlbedoMap", "_DetailNormalMap"
    };

    // ─── 메뉴 1: 폴더별 초기화 ────────────────────────────────────────────
    [MenuItem("HanokBuilder/Tools/Reset ► 공간소품 Only")]
    public static void Reset_GongganSopin() => ResetFolders(new[] {
        "Assets/HanokBuilder/Resources/HanokAssets/공간소품" });

    [MenuItem("HanokBuilder/Tools/Reset ► 건축물완성형 Only")]
    public static void Reset_Wanseonghyeong() => ResetFolders(new[] {
        "Assets/HanokBuilder/Resources/HanokAssets/건축물완성형" });

    [MenuItem("HanokBuilder/Tools/Reset ► 건축물부품형 Only")]
    public static void Reset_Bupumhyeong() => ResetFolders(new[] {
        "Assets/HanokBuilder/Resources/HanokAssets/건축물부품형" });

    [MenuItem("HanokBuilder/Tools/Reset ► 디지털휴먼 Only")]
    public static void Reset_DigitalHuman() => ResetFolders(new[] {
        "Assets/HanokBuilder/Resources/HanokAssets/디지털휴먼" });

    [MenuItem("HanokBuilder/Tools/Reset All Material Textures (Start Fresh)")]
    public static void ResetAllTextures() => ResetFolders(CULTURE_FOLDERS);

    static void ResetFolders(string[] folders)
    {
        int cleared = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material", folders))
        {
            var matPath = AssetDatabase.GUIDToAssetPath(guid);
            var mat     = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) continue;

            bool dirty = false;
            foreach (var slot in ALL_TEX_SLOTS)
            {
                if (!mat.HasProperty(slot)) continue;
                // SetTexture null 은 런타임 값만 초기화 — YAML 참조까지 지우려면 직접 파일 수정 필요
                mat.SetTexture(slot, null);
                dirty = true;
            }
            // 글로우 키워드 끄기
            mat.DisableKeyword("_NORMALMAP");
            mat.DisableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.black);

            if (dirty) { EditorUtility.SetDirty(mat); cleared++; }
        }
        AssetDatabase.SaveAssets();

        // SaveAssets 이후 .mat 파일의 YAML 잔여 guid 도 null 로 덮어쓰기
        OverwriteTexGuidsInFiles(folders);

        AssetDatabase.Refresh();
        Debug.Log($"[MaterialLinker] 초기화 완료 — {cleared}개 ({string.Join(", ", folders)})");
        EditorUtility.DisplayDialog("완료", $"{cleared}개 머티리얼 초기화 완료\n이제 Link Culture Material Textures 실행하세요.", "확인");
    }

    // .mat 파일 YAML에서 fileID!=0 인 잔여 텍스처 참조를 fileID:0 으로 직접 교체
    static void OverwriteTexGuidsInFiles(string[] folders)
    {
        var matGuids = AssetDatabase.FindAssets("t:Material", folders);
        foreach (var g in matGuids)
        {
            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", AssetDatabase.GUIDToAssetPath(g)));
            if (!File.Exists(path)) continue;
            var text  = File.ReadAllText(path);
            // m_Texture: {fileID: 2800000, guid: ..., type: 3} → m_Texture: {fileID: 0}
            var fixed_ = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"m_Texture: \{fileID: \d+, guid: [0-9a-f]+, type: \d+\}",
                "m_Texture: {fileID: 0}");
            if (fixed_ != text)
                File.WriteAllText(path, fixed_);
        }
    }

    // ─── 메뉴: ConvexHull 렌더러 숨기기 ──────────────────────────────────────
    // FBX 내부 _ConvexHulls 메쉬는 콜리전 전용 — MeshRenderer 비활성화
    [MenuItem("HanokBuilder/Tools/Hide ConvexHull Renderers in CM_ Prefabs")]
    public static void HideConvexHullRenderers()
    {
        int prefabCount = 0, rendererCount = 0;

        foreach (var folder in CULTURE_FOLDERS)
        {
            var prefabFolder = folder + "/Prefabs";
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                using var scope = new PrefabUtility.EditPrefabContentsScope(path);
                var root = scope.prefabContentsRoot;
                bool modified = false;

                foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (!mr.gameObject.name.Contains("ConvexHull", System.StringComparison.OrdinalIgnoreCase))
                        continue;
                    mr.enabled = false;
                    rendererCount++;
                    modified = true;
                }
                if (modified) prefabCount++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[ConvexHull] {prefabCount}개 프리팹 / {rendererCount}개 렌더러 숨김");
        EditorUtility.DisplayDialog("완료",
            $"{prefabCount}개 프리팹에서 ConvexHull 렌더러 {rendererCount}개 숨김\n(메쉬 삭제 아님 — 콜리전은 유지됨)",
            "확인");
    }

    // ─── 메뉴 2: 텍스처 연결 ───────────────────────────────────────────────
    [MenuItem("HanokBuilder/Tools/Link Culture Material Textures")]
    public static void Run()
    {
        var texMap = BuildTexMap();
        Debug.Log($"[MaterialLinker] 텍스처 캐시: {texMap.Count}개");

        int linked = 0, skipped = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material", CULTURE_FOLDERS))
        {
            var matPath = AssetDatabase.GUIDToAssetPath(guid);
            var mat     = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) continue;

            var matName  = Path.GetFileNameWithoutExtension(matPath);
            var rawBase  = StripBaseSuffix(matName);          // Door_04_BC → Door_04
            var cleanBase= StripMatPrefix(rawBase);            // M_Door_04  → Door_04

            // 텍스처 prefix 조합으로 후보 생성
            var bc = SlotCandidates(cleanBase, "_BC", "_BaseColor", "_Albedo", "_Diffuse", "_D", "_C");
            var nm = SlotCandidates(cleanBase, "_N", "_Normal", "_NRM", "_Normal_DirectX");
            var mt = SlotCandidates(cleanBase, "_M", "_Metallic", "_ORM");
            var ao = SlotCandidates(cleanBase, "_AO", "_Occlusion", "_O");
            var em = SlotCandidates(cleanBase, "_E", "_Emission", "_EM");

            // matName 자체도 BaseMap 후보 (ex: Door_04_BC.mat → 텍스처도 Door_04_BC.jpg)
            var baseMapCandidates = new[] { matName, rawBase }.Concat(bc).ToArray();

            bool dirty = false;
            dirty |= AssignSlot(mat, texMap, "_BaseMap",          baseMapCandidates);
            dirty |= AssignBumpSlot(mat, texMap,                   nm);
            dirty |= AssignSlot(mat, texMap, "_MetallicGlossMap",  mt);
            dirty |= AssignSlot(mat, texMap, "_OcclusionMap",      ao);
            dirty |= AssignSlot(mat, texMap, "_EmissionMap",       em);

            if (dirty) { EditorUtility.SetDirty(mat); linked++; }
            else skipped++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[MaterialLinker] 완료 — 연결 {linked}개 / 스킵 {skipped}개");
        EditorUtility.DisplayDialog("완료", $"{linked}개 머티리얼 텍스처 연결 완료\n({skipped}개 스킵)", "확인");
    }

    // ─── 헬퍼 ─────────────────────────────────────────────────────────────

    static Dictionary<string, string> BuildTexMap()
    {
        return AssetDatabase.FindAssets("t:Texture2D", CULTURE_FOLDERS
                .Append("Assets/HanokBuilder/Resources/HanokAssets/SharedTextures")
                .ToArray())
            .Select(g => AssetDatabase.GUIDToAssetPath(g))
            .GroupBy(p => Path.GetFileNameWithoutExtension(p), System.StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key.ToLowerInvariant(), g => g.First(), System.StringComparer.OrdinalIgnoreCase);
    }

    // tex prefix × suffix 조합 후보 목록
    static IEnumerable<string> SlotCandidates(string baseName, params string[] suffixes)
    {
        foreach (var sfx in suffixes)
            foreach (var pre in TEX_PREFIXES)
                yield return pre + baseName + sfx;
    }

    static string StripMatPrefix(string name)
    {
        foreach (var p in MAT_PREFIXES)
            if (name.StartsWith(p, System.StringComparison.OrdinalIgnoreCase))
                return name.Substring(p.Length);
        return name;
    }

    static string StripBaseSuffix(string name)
    {
        foreach (var s in BASE_SUFFIXES)
            if (name.EndsWith(s, System.StringComparison.OrdinalIgnoreCase))
                return name[..^s.Length];
        return name;
    }

    static bool AssignSlot(Material mat, Dictionary<string, string> texMap, string slot, IEnumerable<string> candidates)
    {
        if (!mat.HasProperty(slot) || mat.GetTexture(slot) != null) return false;
        foreach (var name in candidates)
        {
            if (!texMap.TryGetValue(name.ToLowerInvariant(), out var path)) continue;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) continue;
            mat.SetTexture(slot, tex);
            return true;
        }
        return false;
    }

    // BumpMap 전용: 이미 NormalMap 타입인 텍스처만 적용 (타입 강제 변환 안 함 → 보호막 방지)
    static bool AssignBumpSlot(Material mat, Dictionary<string, string> texMap, IEnumerable<string> candidates)
    {
        if (!mat.HasProperty("_BumpMap") || mat.GetTexture("_BumpMap") != null) return false;
        foreach (var name in candidates)
        {
            if (!texMap.TryGetValue(name.ToLowerInvariant(), out var path)) continue;
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;

            // 이미 NormalMap으로 설정된 텍스처만 연결
            if (ti.textureType != TextureImporterType.NormalMap) continue;

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) continue;
            mat.SetTexture("_BumpMap", tex);
            mat.EnableKeyword("_NORMALMAP");
            return true;
        }
        return false;
    }
}
