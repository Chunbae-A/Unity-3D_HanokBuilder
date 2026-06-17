using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// SunaedongHouse FBX들을 prefab화하고, HanokAssetTags(카테고리)와
/// HanokAssetInfo(한글 표시명/검색태그)를 일괄 생성한다.
/// 이미 존재하는 prefab/AssetInfo는 건드리지 않는다 (재실행 안전).
public static class SunaedongHouseImportTool
{
    const string ROOT            = "Assets/HanokBuilder/Resources/HanokAssets/SunaedongHouse";
    const string PREFAB_ROOT     = ROOT + "/Prefabs";
    const string CATEGORIES_ROOT = "Assets/HanokBuilder/Resources/HanokCategories";
    const string ASSETINFO_ROOT  = "Assets/HanokBuilder/Resources/HanokAssetInfo";

    static readonly (string fbx, string name, string kor, string[] cats, string[] tags)[] ENTRIES =
    {
        // ── 완성형 ───────────────────────────────────────────
        ("House_1/house.fbx",   "SH1_Complete", "수내동가옥 1호", new[]{"Complete"}, new[]{"수내동","한옥","가옥","문화재"}),
        ("House_2/house2 .fbx", "SH2_Complete", "수내동가옥 2호", new[]{"Complete"}, new[]{"수내동","한옥","가옥","문화재"}),

        // ── 공통 부품 (Part/H) ────────────────────────────────
        ("Part/H/H_ArchBeam.fbx",  "SH_ArchBeam",  "홍예보", new[]{"Parts","Beam"}, Array.Empty<string>()),
        ("Part/H/H_MainDoor.fbx",  "SH_MainDoor",  "정문",   new[]{"Parts","Door"}, new[]{"대문"}),
        ("Part/H/H_Stone_01.fbx",  "SH_Stone_01",  "기단석 1", new[]{"Parts","Decoration"}, new[]{"주춧돌","초석"}),
        ("Part/H/H_Stone_02.fbx",  "SH_Stone_02",  "기단석 2", new[]{"Parts","Decoration"}, new[]{"주춧돌","초석"}),
        ("Part/H/H_Stone_03.fbx",  "SH_Stone_03",  "기단석 3", new[]{"Parts","Decoration"}, new[]{"주춧돌","초석"}),
        ("Part/H/H_Stone_04.fbx",  "SH_Stone_04",  "기단석 4", new[]{"Parts","Decoration"}, new[]{"주춧돌","초석"}),
        ("Part/H/H_Stone_05.fbx",  "SH_Stone_05",  "기단석 5", new[]{"Parts","Decoration"}, new[]{"주춧돌","초석"}),
        ("Part/H/H_Stone_06.fbx",  "SH_Stone_06",  "기단석 6", new[]{"Parts","Decoration"}, new[]{"주춧돌","초석"}),
        ("Part/H/H_Stone_07.fbx",  "SH_Stone_07",  "기단석 7", new[]{"Parts","Decoration"}, new[]{"주춧돌","초석"}),
        ("Part/H/H_Stone_08.fbx",  "SH_Stone_08",  "기단석 8", new[]{"Parts","Decoration"}, new[]{"주춧돌","초석"}),
        ("Part/H/H_Stone_09.fbx",  "SH_Stone_09",  "기단석 9", new[]{"Parts","Decoration"}, new[]{"주춧돌","초석"}),
        ("Part/H/H_Stone_10.fbx",  "SH_Stone_10",  "기단석 10", new[]{"Parts","Decoration"}, new[]{"주춧돌","초석"}),
        ("Part/H/H_Stone_11.fbx",  "SH_Stone_11",  "기단석 11", new[]{"Parts","Decoration"}, new[]{"주춧돌","초석"}),
        ("Part/H/H_Stone_12.fbx",  "SH_Stone_12",  "기단석 12", new[]{"Parts","Decoration"}, new[]{"주춧돌","초석"}),
        ("Part/H/H_Tie.fbx",        "SH_Tie",        "결구 부재", new[]{"Parts","Beam"}, Array.Empty<string>()),
        ("Part/H/H_WindowWall.fbx", "SH_WindowWall", "창벽",     new[]{"Parts","Wall"}, Array.Empty<string>()),
        ("Part/H/H_wall_01.fbx",    "SH_Wall_01",    "벽체 1",   new[]{"Parts","Wall"}, Array.Empty<string>()),
        ("Part/H/H_wall_02.fbx",    "SH_Wall_02",    "벽체 2",   new[]{"Parts","Wall"}, Array.Empty<string>()),

        // ── 1호 전용 (Part/H1) ────────────────────────────────
        ("Part/H1/H1_Chimney.fbx",        "SH1_Chimney",      "굴뚝",      new[]{"Parts","Decoration"}, new[]{"연통"}),
        ("Part/H1/H1_Door.fbx",           "SH1_Door",         "방문",      new[]{"Parts","Door"}, Array.Empty<string>()),
        ("Part/H1/H1_Door_wall.fbx",      "SH1_DoorWall",     "문벽",      new[]{"Parts","Wall"}, Array.Empty<string>()),
        ("Part/H1/H1_Floor.fbx",          "SH1_Floor",        "바닥",      new[]{"Parts","Floor"}, Array.Empty<string>()),
        ("Part/H1/H1_Floor2.fbx",         "SH1_Floor_02",     "바닥 2",    new[]{"Parts","Floor"}, Array.Empty<string>()),
        ("Part/H1/H1_LongBeam.fbx",       "SH1_LongBeam",     "긴 보",     new[]{"Parts","Beam"}, Array.Empty<string>()),
        ("Part/H1/H1_Long_curveBeam.fbx", "SH1_LongCurveBeam","곡선 장보", new[]{"Parts","Beam"}, Array.Empty<string>()),
        ("Part/H1/H1_MiddleBeam.fbx",     "SH1_MiddleBeam",   "중간보",    new[]{"Parts","Beam"}, Array.Empty<string>()),
        ("Part/H1/H1_Pillar.fbx",         "SH1_Pillar",       "기둥",      new[]{"Parts","Decoration"}, Array.Empty<string>()),
        ("Part/H1/H1_Roof.fbx",           "SH1_Roof",         "지붕",      new[]{"Parts","Roof"}, Array.Empty<string>()),
        ("Part/H1/H1_Room.fbx",           "SH1_Room",         "방",        new[]{"Parts","Floor"}, new[]{"실내","내부"}),
        ("Part/H1/H1_Seokkarae.fbx",      "SH1_Seokkarae",    "서까래",    new[]{"Parts","Roof"}, new[]{"연목"}),
        ("Part/H1/H1_ShortBeam.fbx",      "SH1_ShortBeam",    "짧은 보",   new[]{"Parts","Beam"}, Array.Empty<string>()),
        ("Part/H1/H1_ShortPillar.fbx",    "SH1_ShortPillar",  "짧은 기둥", new[]{"Parts","Decoration"}, Array.Empty<string>()),
        ("Part/H1/H1_Subroof.fbx",        "SH1_Subroof",      "부속 지붕", new[]{"Parts","Roof"}, Array.Empty<string>()),
        ("Part/H1/H1_TieStick1.fbx",      "SH1_TieStick_01",  "동자주 1",  new[]{"Parts","Beam"}, new[]{"동자기둥"}),
        ("Part/H1/H1_TieStick2.fbx",      "SH1_TieStick_02",  "동자주 2",  new[]{"Parts","Beam"}, new[]{"동자기둥"}),
        ("Part/H1/H1_TieStick3.fbx",      "SH1_TieStick_03",  "동자주 3",  new[]{"Parts","Beam"}, new[]{"동자기둥"}),
        ("Part/H1/H1_TieStick4.fbx",      "SH1_TieStick_04",  "동자주 4",  new[]{"Parts","Beam"}, new[]{"동자기둥"}),
        ("Part/H1/H1_TieStick5.fbx",      "SH1_TieStick_05",  "동자주 5",  new[]{"Parts","Beam"}, new[]{"동자기둥"}),
        ("Part/H1/H1_TieStick6.fbx",      "SH1_TieStick_06",  "동자주 6",  new[]{"Parts","Beam"}, new[]{"동자기둥"}),
        ("Part/H1/H1_base.fbx",           "SH1_Base",         "기단",      new[]{"Parts","Decoration"}, new[]{"주춧돌","초석"}),
        ("Part/H1/H1_prop.fbx",           "SH1_Prop",         "소품",      new[]{"Parts","Decoration"}, Array.Empty<string>()),
        ("Part/H1/H1_wall1.fbx",          "SH1_Wall_01",      "벽체 1",    new[]{"Parts","Wall"}, Array.Empty<string>()),
        ("Part/H1/H1_wall2.fbx",          "SH1_Wall_02",      "벽체 2",    new[]{"Parts","Wall"}, Array.Empty<string>()),
        ("Part/H1/H1_wall3.fbx",          "SH1_Wall_03",      "벽체 3",    new[]{"Parts","Wall"}, Array.Empty<string>()),
        ("Part/H1/H1_wall4.fbx",          "SH1_Wall_04",      "벽체 4",    new[]{"Parts","Wall"}, Array.Empty<string>()),

        // ── 2호 전용 (Part/H2) ────────────────────────────────
        ("Part/H2/H2_Door1.fbx",           "SH2_Door_01",       "방문 1",      new[]{"Parts","Door"}, Array.Empty<string>()),
        ("Part/H2/H2_Door2.fbx",           "SH2_Door_02",       "방문 2",      new[]{"Parts","Door"}, Array.Empty<string>()),
        ("Part/H2/H2_Floor.fbx",           "SH2_Floor",         "바닥",        new[]{"Parts","Floor"}, Array.Empty<string>()),
        ("Part/H2/H2_LongBeam.fbx",        "SH2_LongBeam",      "긴 보",       new[]{"Parts","Beam"}, Array.Empty<string>()),
        ("Part/H2/H2_LongTieStick.fbx",    "SH2_LongTieStick",  "긴 동자주",   new[]{"Parts","Beam"}, new[]{"동자기둥"}),
        ("Part/H2/H2_MainDoor1_Left.fbx",  "SH2_MainDoor_Left", "정문 좌측",   new[]{"Parts","Door"}, new[]{"대문"}),
        ("Part/H2/H2_MainDoor1_Right.fbx", "SH2_MainDoor_Right","정문 우측",   new[]{"Parts","Door"}, new[]{"대문"}),
        ("Part/H2/H2_Pillar.fbx",          "SH2_Pillar",        "기둥",        new[]{"Parts","Decoration"}, Array.Empty<string>()),
        ("Part/H2/H2_Roof.fbx",            "SH2_Roof",          "지붕",        new[]{"Parts","Roof"}, Array.Empty<string>()),
        ("Part/H2/H2_Seokkarae.fbx",       "SH2_Seokkarae",     "서까래",      new[]{"Parts","Roof"}, new[]{"연목"}),
        ("Part/H2/H2_ShortBeam.fbx",       "SH2_ShortBeam",     "짧은 보",     new[]{"Parts","Beam"}, Array.Empty<string>()),
        ("Part/H2/H2_ShortPillar.fbx",     "SH2_ShortPillar",   "짧은 기둥",   new[]{"Parts","Decoration"}, Array.Empty<string>()),
        ("Part/H2/H2_ShortTieStick.fbx",   "SH2_ShortTieStick", "짧은 동자주", new[]{"Parts","Beam"}, new[]{"동자기둥"}),
        ("Part/H2/H2_SubRoof.fbx",         "SH2_Subroof",       "부속 지붕",   new[]{"Parts","Roof"}, Array.Empty<string>()),
        ("Part/H2/H2_Wooden_Floor.fbx",    "SH2_WoodenFloor",   "마루",        new[]{"Parts","Maru"}, new[]{"툇마루"}),
    };

    [MenuItem("HanokBuilder/Tools/Import Sunaedong House Assets")]
    public static void Run()
    {
        if (!AssetDatabase.IsValidFolder(PREFAB_ROOT))
            AssetDatabase.CreateFolder(ROOT, "Prefabs");

        var catCache = new Dictionary<string, HanokAssetCategory>();
        HanokAssetCategory GetCat(string key)
        {
            if (catCache.TryGetValue(key, out var cached)) return cached;
            var cat = AssetDatabase.LoadAssetAtPath<HanokAssetCategory>($"{CATEGORIES_ROOT}/Category_{key}.asset");
            if (cat == null) Debug.LogWarning($"[Sunaedong] 카테고리를 찾을 수 없음: {key}");
            catCache[key] = cat;
            return cat;
        }

        int createdPrefab = 0, createdInfo = 0, skippedPrefab = 0, skippedInfo = 0;

        foreach (var e in ENTRIES)
        {
            var prefabPath = $"{PREFAB_ROOT}/{e.name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                var fbxPath = $"{ROOT}/{e.fbx}";
                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                if (fbx == null)
                {
                    Debug.LogError($"[Sunaedong] FBX를 찾을 수 없음: {fbxPath}");
                    continue;
                }

                var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                inst.name = e.name;

                var tags = inst.GetComponent<HanokAssetTags>();
                if (tags == null) tags = inst.AddComponent<HanokAssetTags>();
                tags.categories = e.cats.Select(GetCat).Where(c => c != null).ToArray();

                PrefabUtility.SaveAsPrefabAsset(inst, prefabPath);
                UnityEngine.Object.DestroyImmediate(inst);
                createdPrefab++;
            }
            else
            {
                skippedPrefab++;
            }

            var infoPath = $"{ASSETINFO_ROOT}/AssetInfo_{e.name}.asset";
            if (AssetDatabase.LoadAssetAtPath<HanokAssetInfo>(infoPath) == null)
            {
                var info = ScriptableObject.CreateInstance<HanokAssetInfo>();
                info.assetKey = e.name;
                info.displayName = e.kor;
                info.tags = e.tags;
                AssetDatabase.CreateAsset(info, infoPath);
                createdInfo++;
            }
            else
            {
                skippedInfo++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Sunaedong] prefab 생성 {createdPrefab}개 (스킵 {skippedPrefab}) / AssetInfo 생성 {createdInfo}개 (스킵 {skippedInfo}) / 총 {ENTRIES.Length}개 항목");
    }
}
