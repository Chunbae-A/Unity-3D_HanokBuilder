using UnityEngine;
using UnityEngine.Rendering;


public static class HanokSceneSetup
{
    // ── Architectural constants (Joseon proportions) ──────────────────────────
    const float KAN   = 3.3f;   // 칸: bay module
    const float COL_H = 3.6f;   // 기둥 height
    const float EAVE  = 1.2f;   // 처마 overhang
    const float TILE  = 0.22f;  // 기와 tile width

    // ── Material smoothness presets ───────────────────────────────────────────
    const float SM_ROUGH_STONE   = 0.08f;
    const float SM_POLISHED_GRAN = 0.35f;
    const float SM_DARK_WOOD     = 0.04f;
    const float SM_PAPER         = 0.02f;
    const float SM_GLAZED_TILE   = 0.45f;
    const float SM_WATER         = 0.92f;
    const float SM_CLAY          = 0.06f;
    const float SM_LACQUER       = 0.55f;
    const float SM_IRON          = 0.62f;

    // ── Named root objects (destroyed & rebuilt each SetPreset call) ──────────
    const string ROOT_PLATFORM   = "_HanokPlatform";
    const string ROOT_WALLS      = "_HanokWalls";
    const string ROOT_PINES      = "_HanokPines";
    const string ROOT_COURTYARD  = "_HanokCourtyard";
    const string ROOT_BG         = "_HanokBgBuildings";
    const string ROOT_MAIN       = "_HanokMainBuilding";
    const string ROOT_SARANG     = "_HanokSarang";
    const string ROOT_MARKET     = "_HanokMarket";
    const string ROOT_GARDEN     = "_HanokGarden";

    // ═════════════════════════════════════════════════════════════════════════
    //  PUBLIC API
    // ═════════════════════════════════════════════════════════════════════════

    public static void Setup()
    {
        CreateOuterGround();
        CreateStonePlatform();
        CreateFloor();
        CreateCourtyard();
        CreateBoundaryWalls();
        CreateBroadGroves();
        CreateMainHanok();
        CreateDistantBuildings();
        CreateMountainSilhouettes();
        SetupSkybox();
        SetupLighting();
        SetupCamera();
    }

    public static void SetPreset(int idx)
    {
        DestroyNamed(ROOT_PLATFORM, ROOT_WALLS, ROOT_PINES, ROOT_COURTYARD,
                     ROOT_BG, ROOT_MAIN, ROOT_SARANG, ROOT_MARKET, ROOT_GARDEN);
        switch (idx)
        {
            case 0: Preset_HanokMadang();   break;
            case 1: Preset_Sarangchae();    break;
            case 2: Preset_Jangter();       break;
            case 3: Preset_Garden();        break;
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  PRESETS
    // ═════════════════════════════════════════════════════════════════════════

    static void Preset_HanokMadang()   { }
    static void Preset_Sarangchae()    { }
    static void Preset_Jangter()       { }
    static void Preset_Garden()        { }

    // ═════════════════════════════════════════════════════════════════════════
    //  ENVIRONMENT — shared
    // ═════════════════════════════════════════════════════════════════════════

    static void CreateOuterGround()
    {
        // Multi-layer ground: far dirt → mid gravel → near soil
        Color farDirt   = Hex("8B7355");
        Color midGravel = Hex("9E8E72");
        Color nearSoil  = Hex("6B5A3E");

        var g = Mk("_HanokGround");
        MkBox(g, V(0,-0.15f,0),   V(80,0.1f,80),  farDirt,   SM_ROUGH_STONE);
        MkBox(g, V(0,-0.08f,0),   V(40,0.1f,40),  midGravel, SM_ROUGH_STONE);
        MkBox(g, V(0, 0.00f,0),   V(20,0.1f,20),  nearSoil,  SM_CLAY);
    }

    static void CreateStonePlatform()
    {
        var p = Mk(ROOT_PLATFORM);
        Color granite   = Hex("B8AFA0");
        Color graniteD  = Hex("9E9488");
        Color graniteDD = Hex("7A7068");

        // 3-layer platform with chamfered edges
        MkBox(p, V(0, 0.15f, 0),  V(14f, 0.30f, 10f),  graniteDD, SM_POLISHED_GRAN);  // base
        MkBox(p, V(0, 0.40f, 0),  V(13f, 0.25f,  9f),  graniteD,  SM_POLISHED_GRAN);  // mid
        MkBox(p, V(0, 0.60f, 0),  V(12f, 0.20f,  8f),  granite,   SM_POLISHED_GRAN);  // top

        // Corner 초석 (foundation stones)
        float[] cx = {-5.5f, 5.5f}; float[] cz = {-3.5f, 3.5f};
        foreach (float x in cx) foreach (float z in cz)
            MkBox(p, V(x, 0.78f, z), V(0.55f, 0.22f, 0.55f), granite, SM_POLISHED_GRAN);

        // Approach steps — 3 risers, centered on south face
        for (int i = 0; i < 3; i++)
            MkBox(p, V(0, 0.10f + i*0.18f, -4.6f - i*0.35f),
                  V(3.0f, 0.18f, 0.35f), granite, SM_POLISHED_GRAN);

        // 노둣돌 (mounting stones) flanking steps
        MkBox(p, V(-1.8f, 0.12f, -5.4f), V(0.6f, 0.24f, 0.6f), graniteD, SM_POLISHED_GRAN);
        MkBox(p, V( 1.8f, 0.12f, -5.4f), V(0.6f, 0.24f, 0.6f), graniteD, SM_POLISHED_GRAN);
    }

    static void CreateFloor()
    {
        var f = Mk("_HanokFloor");
        Color dark = Hex("3D2B1F");
        // 우물마루 (lattice floor) — alternating planks
        for (int row = -5; row <= 5; row++)
        for (int col = -3; col <= 3; col++)
        {
            float bright = (row + col) % 2 == 0 ? 1f : 0.78f;
            MkBox(f, V(col * 0.72f, 0.72f, row * 0.72f),
                  V(0.68f, 0.06f, 0.68f),
                  dark * bright, SM_DARK_WOOD);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  PRESET 0 — 한옥 마당
    // ═════════════════════════════════════════════════════════════════════════

    static void CreateCourtyard()
    {
        var c = Mk(ROOT_COURTYARD);
        Color sand   = Hex("C8B89A");
        Color sand2  = Hex("BCA882");
        Color clay   = Hex("8B6914");
        Color stone  = Hex("9E9488");
        Color dark   = Hex("3D2B1F");
        Color bronze = Hex("7C6035");
        Color moss   = Hex("4A6741");
        Color water  = Hex("4A7A9B");

        // Compacted earth courtyard
        MkBox(c, V(0,-0.02f,0), V(11f,0.04f,7.5f), sand, SM_CLAY);
        // Sand variation strips
        for (int i = -4; i <= 4; i++)
            MkBox(c, V(i*1.1f, 0.01f, 0), V(0.9f, 0.02f, 7f), sand2*(0.9f+i*0.01f), SM_CLAY);

        // 물확 (stone water basin)
        MkBox(c, V(3.2f,0.06f,2.2f), V(0.9f,0.12f,0.9f), stone, SM_POLISHED_GRAN);
        MkBox(c, V(3.2f,0.14f,2.2f), V(0.7f,0.04f,0.7f), water, SM_WATER);  // water surface
        // Basin rim detail
        MkBox(c, V(3.2f,0.13f, 2.65f), V(0.9f,0.05f,0.05f), stone, SM_POLISHED_GRAN);
        MkBox(c, V(3.2f,0.13f, 1.75f), V(0.9f,0.05f,0.05f), stone, SM_POLISHED_GRAN);

        // 장독대 (jar stand) — stone slab + 5 onggi jars
        MkBox(c, V(-3.5f,0.02f, 2.5f), V(1.8f,0.04f,1.2f), stone, SM_ROUGH_STONE);
        for (int j = 0; j < 5; j++)
        {
            float jx = -4.1f + j*0.44f;
            Color jc = j % 2 == 0 ? Hex("3D2010") : Hex("5A3218");
            MkCyl(c, V(jx,0.20f,2.5f), 0.16f,0.38f, jc, SM_CLAY);
            // Jar shoulder (wider middle)
            MkCyl(c, V(jx,0.30f,2.5f), 0.18f,0.14f, jc, SM_CLAY);
            // Lid
            MkCyl(c, V(jx,0.42f,2.5f), 0.13f,0.08f, jc*(0.7f), SM_CLAY);
        }

        // 괴석 (scholar's rock arrangement)
        GardenRock(c.transform, V(-2.0f,0.12f,-2.0f), 0.42f, Hex("827668"), Hex("6A5E54"));
        GardenRock(c.transform, V(-1.5f,0.08f,-1.6f), 0.28f, Hex("706258"), Hex("5C504A"));
        GardenRock(c.transform, V(-2.3f,0.06f,-1.5f), 0.22f, Hex("8A7C70"), Hex("706458"));

        // 화분 (flower pots) near main building
        float[] potX = {-1.4f, 1.4f};
        foreach (float px in potX)
        {
            MkCyl(c, V(px,0.12f,-3.2f), 0.18f,0.22f, clay, SM_CLAY);
            MkCyl(c, V(px,0.26f,-3.2f), 0.14f,0.08f, clay*(0.8f), SM_CLAY);
            // Soil
            MkCyl(c, V(px,0.30f,-3.2f), 0.12f,0.02f, Hex("3D2B1F"), SM_CLAY);
            // Plant (small sphere bunch)
            MkBox(c, V(px,0.38f,-3.2f), V(0.16f,0.16f,0.16f), moss, SM_ROUGH_STONE);
        }

        // Stepping stones across courtyard
        float[] ssz = {-2.5f, -1.2f, 0.2f, 1.5f, 2.8f};
        for (int s = 0; s < ssz.Length; s++)
            MkBox(c, V(0.2f*Mathf.Sin(s*1.3f), 0.03f, ssz[s]),
                  V(0.55f+s%2*0.12f, 0.06f, 0.40f+s%3*0.08f),
                  stone*(0.85f+s%2*0.12f), SM_ROUGH_STONE);

        // 굴뚝 (chimney) behind main building — visible above roofline
        MkBox(c, V(4.2f,1.0f,-3.5f), V(0.35f,2.2f,0.35f), Hex("6B5A4E"), SM_ROUGH_STONE);
        // Chimney cap
        MkBox(c, V(4.2f,2.18f,-3.5f), V(0.5f,0.12f,0.5f), Hex("5A4A40"), SM_ROUGH_STONE);
        // Chimney smoke holes (dark inset)
        MkBox(c, V(4.2f,1.6f,-3.48f), V(0.12f,0.22f,0.04f), Hex("1A1210"), 0f);
        MkBox(c, V(4.2f,1.6f,-3.52f), V(0.12f,0.22f,0.04f), Hex("1A1210"), 0f);
    }

    static void CreateBoundaryWalls()
    {
        var w = Mk(ROOT_WALLS);
        Color daubed = Hex("D4C9B0");  // 흰 회벽
        Color stone  = Hex("7A6E5A");  // 돌
        Color dark   = Hex("2A1E14");  // dark wood
        Color tile   = Hex("4A3828");  // 기와

        // --- 솟을대문 (main gate) north side ---
        // Stone base
        MkBox(w, V(0, 0.30f,-6.8f), V(5.5f,0.60f,0.50f), stone, SM_ROUGH_STONE);
        // 5 gate pillars
        float[] gpx = {-2.2f,-1.1f,0f,1.1f,2.2f};
        foreach (float gx in gpx)
        {
            MkBox(w, V(gx,1.50f,-6.8f), V(0.22f,2.4f,0.22f), dark, SM_DARK_WOOD);
            // Column 주초석
            MkBox(w, V(gx,0.64f,-6.8f), V(0.30f,0.12f,0.30f), stone, SM_POLISHED_GRAN);
        }
        // Lintel
        MkBox(w, V(0,2.76f,-6.8f), V(5.5f,0.16f,0.22f), dark, SM_DARK_WOOD);
        // Door panels (lattice, 2 leaves)
        BuildShojiGrid(w.transform, V(-0.55f,1.38f,-6.79f), 0.88f, 2.2f, 4, 8);
        BuildShojiGrid(w.transform, V( 0.55f,1.38f,-6.79f), 0.88f, 2.2f, 4, 8);
        // Gate roof — curved eave tiles
        BuildCurvedRoof(w.transform, V(0,2.9f,-6.8f), 5.5f, 0.7f, tile, 2.0f);

        // --- East wall ---
        BuildWallSection(w.transform, V( 7.0f,1.0f, 0f), 0f,   14f, daubed, stone, tile);
        // --- West wall ---
        BuildWallSection(w.transform, V(-7.0f,1.0f, 0f), 180f, 14f, daubed, stone, tile);
        // --- North wall (flanking gate) ---
        BuildWallSection(w.transform, V(-4.5f,1.0f,-6.8f), 90f, 4.5f, daubed, stone, tile);
        BuildWallSection(w.transform, V( 4.5f,1.0f,-6.8f), 90f, 4.5f, daubed, stone, tile);
    }

    static void BuildWallSection(Transform parent, Vector3 center, float rotY, float length,
                                  Color render, Color stone, Color tile)
    {
        var go = Mk("WallSec", parent);
        go.transform.position = center;
        go.transform.eulerAngles = V(0, rotY, 0);

        // Stone foundation
        MkBox(go, V(0,-0.5f,0), V(length,0.40f,0.44f), stone, SM_ROUGH_STONE);
        // Rendered wall body
        MkBox(go, V(0, 0.3f,0), V(length,1.20f,0.36f), render, SM_CLAY);
        // Mortar lines
        int seg = Mathf.RoundToInt(length / 0.55f);
        for (int m = 0; m < seg; m++)
            MkBox(go, V(-length/2f+0.28f+m*0.55f, -0.10f, 0.01f), V(0.02f,1.0f,0.02f),
                  stone*(0.6f), SM_ROUGH_STONE);
        // Coping tiles
        BuildCurvedRoof(go.transform, V(0,0.96f,0), length, 0.42f, tile, 1.4f);
    }

    static void CreateMainHanok()
    {
        var h = Mk(ROOT_MAIN);
        BuildHanok(h.transform, V(0, 0.72f, -2.0f), 4, 2, COL_H, true);
    }

    // ---- Universal hanok builder ----
    // kanX = bays wide, kanZ = bays deep, full = add interior detail
    static void BuildHanok(Transform parent, Vector3 origin, int kanX, int kanZ,
                            float colH, bool full)
    {
        var go = Mk("Hanok", parent);
        go.transform.position = origin;

        Color dark   = Hex("2A1E14");
        Color darkM  = Hex("3C2A1E");
        Color tile   = Hex("3C3028");
        Color stone  = Hex("9E9488");
        Color white  = Hex("E8DFD0");
        Color red    = Hex("8B2800");

        float W = kanX * KAN;
        float D = kanZ * KAN;
        float hw = W * 0.5f;
        float hd = D * 0.5f;

        // ── 초석 (foundation stones) + 기둥 (columns) ────────────────────────
        int numX = kanX + 1;
        int numZ = kanZ + 1;
        for (int xi = 0; xi < numX; xi++)
        for (int zi = 0; zi < numZ; zi++)
        {
            float cx = -hw + xi * KAN;
            float cz = -hd + zi * KAN;
            // 초석
            MkBox(go, V(cx,0.02f,cz), V(0.40f,0.10f,0.40f), stone, SM_POLISHED_GRAN);
            // 기둥
            float cHeight = colH + (xi==numX/2&&zi==numZ/2 ? 0.1f : 0);
            MkCyl(go, V(cx, cHeight*0.5f+0.07f, cz), 0.18f, cHeight, red, SM_LACQUER);
            // AO shadow at base
            MkBox(go, V(cx,-0.0f,cz), V(0.22f,0.04f,0.22f), dark*(0.4f), 0f);
        }

        // ── 공포 (bracket sets) on top of each column ─────────────────────────
        for (int xi = 0; xi < numX; xi++)
        for (int zi = 0; zi < numZ; zi++)
        {
            float cx = -hw + xi * KAN;
            float cz = -hd + zi * KAN;
            float by  = colH + 0.08f;
            // Main bracket arm
            MkBox(go, V(cx,by,cz), V(0.55f,0.15f,0.22f), dark, SM_DARK_WOOD);
            MkBox(go, V(cx,by,cz), V(0.22f,0.15f,0.55f), dark, SM_DARK_WOOD);
            // Upper naso (stepped up)
            MkBox(go, V(cx,by+0.14f,cz), V(0.70f,0.12f,0.18f), darkM, SM_DARK_WOOD);
            MkBox(go, V(cx,by+0.14f,cz), V(0.18f,0.12f,0.70f), darkM, SM_DARK_WOOD);
        }

        // ── 도리 (purlins) along eave line ────────────────────────────────────
        float pY = colH + 0.22f;
        // Front and back purlins
        MkBox(go, V(0,pY,-hd), V(W+0.3f,0.18f,0.18f), dark, SM_DARK_WOOD);
        MkBox(go, V(0,pY, hd), V(W+0.3f,0.18f,0.18f), dark, SM_DARK_WOOD);
        // Side purlins
        MkBox(go, V(-hw,pY,0), V(0.18f,0.18f,D+0.3f), dark, SM_DARK_WOOD);
        MkBox(go, V( hw,pY,0), V(0.18f,0.18f,D+0.3f), dark, SM_DARK_WOOD);
        // Ridge purlin
        MkBox(go, V(0,pY+colH*0.45f,0), V(W+0.3f,0.20f,0.20f), dark, SM_DARK_WOOD);

        // ── 창방 (wall plates) at mid-height ──────────────────────────────────
        float wY = colH * 0.62f;
        MkBox(go, V(0,wY,-hd), V(W,0.14f,0.12f), dark, SM_DARK_WOOD);
        MkBox(go, V(0,wY, hd), V(W,0.14f,0.12f), dark, SM_DARK_WOOD);
        MkBox(go, V(-hw,wY,0), V(0.12f,0.14f,D), dark, SM_DARK_WOOD);
        MkBox(go, V( hw,wY,0), V(0.12f,0.14f,D), dark, SM_DARK_WOOD);

        // ── 벽 (wall infill between columns) — daub panels ──────────────────
        // Front face (shoji panels for sarangchae-style look)
        if (full)
        {
            for (int xi = 0; xi < kanX; xi++)
            {
                float px = -hw + KAN*0.5f + xi*KAN;
                // Front shoji grid
                BuildShojiGrid(go.transform, V(px, colH*0.45f, -hd), KAN-0.22f, colH*0.7f, 3, 6);
                // Side walls (daub)
                if (xi == 0 || xi == kanX-1) continue;
            }
            // Side daub walls
            for (int zi = 0; zi < kanZ; zi++)
            {
                float pz = -hd + KAN*0.5f + zi*KAN;
                MkBox(go, V(-hw, colH*0.45f, pz), V(0.10f,colH*0.7f,KAN-0.22f), white, SM_PAPER);
                MkBox(go, V( hw, colH*0.45f, pz), V(0.10f,colH*0.7f,KAN-0.22f), white, SM_PAPER);
            }
        }

        // ── 지붕 (roof) — authentic curved hip/gable ──────────────────────────
        float roofBase = pY + 0.1f;
        float roofH    = (W + D) * 0.14f;   // roof height proportional to plan
        float eaveW    = W + EAVE*2;
        float eaveD    = D + EAVE*2;

        // Main roof body (hip)
        MkBox(go, V(0,roofBase+roofH*0.5f,0), V(eaveW+0.1f,roofH,eaveD+0.1f), tile, SM_GLAZED_TILE);
        // Hip rafter planes (4 triangular wedges)
        MkRoofWedge(go, V(0, roofBase, -eaveD*0.5f),  eaveW, roofH, tile,  0f);
        MkRoofWedge(go, V(0, roofBase,  eaveD*0.5f),  eaveW, roofH, tile, 180f);
        MkRoofWedge(go, V(-eaveW*0.5f,roofBase,0),    eaveD, roofH, tile, 270f);
        MkRoofWedge(go, V( eaveW*0.5f,roofBase,0),    eaveD, roofH, tile,  90f);

        // Curved roof tiles (기와) — two rows of tiles along eave perimeter
        BuildCurvedRoof(go.transform, V(0, roofBase+roofH*0.82f, 0), eaveW, eaveD, tile, 2.2f);

        // 용마루 (ridge beam)
        MkBox(go, V(0,roofBase+roofH+0.10f,0), V(W*0.5f,0.22f,0.28f), tile*(0.7f), SM_GLAZED_TILE);
        MkBox(go, V(0,roofBase+roofH+0.22f,0), V(W*0.5f,0.12f,0.16f), dark,        SM_DARK_WOOD);

        // 취두 (owl-tail ridge ends)
        float[] rtails = {-W*0.25f, W*0.25f};
        foreach (float rx in rtails)
        {
            MkBox(go, V(rx, roofBase+roofH+0.18f, 0), V(0.25f,0.35f,0.30f), Hex("5C4A3C"), SM_GLAZED_TILE);
            FlatSph(go, V(rx, roofBase+roofH+0.42f, 0), 0.14f, Hex("4A3828"), SM_GLAZED_TILE);
        }

        // 처마 eave undersides — darker for simulated AO
        MkBox(go, V(0,roofBase+0.05f,-eaveD*0.5f+0.15f), V(eaveW,0.08f,0.50f), dark*(0.5f), 0f);
        MkBox(go, V(0,roofBase+0.05f, eaveD*0.5f-0.15f), V(eaveW,0.08f,0.50f), dark*(0.5f), 0f);
        MkBox(go, V(-eaveW*0.5f+0.15f,roofBase+0.05f,0), V(0.50f,0.08f,eaveD), dark*(0.5f), 0f);
        MkBox(go, V( eaveW*0.5f-0.15f,roofBase+0.05f,0), V(0.50f,0.08f,eaveD), dark*(0.5f), 0f);
    }

    static void MkRoofWedge(GameObject parent, Vector3 pos, float baseW, float h, Color c, float rotY)
    {
        var go = new GameObject("RoofWedge");
        go.transform.SetParent(parent.transform);
        go.transform.position = parent.transform.position + pos;
        go.transform.eulerAngles = V(0, rotY, 0);
        // Layered thin boxes tapering upward
        int layers = 8;
        for (int i = 0; i < layers; i++)
        {
            float t = (float)i / layers;
            float w = baseW * (1f - t * 0.6f);
            float ly = pos.y + h * t + h / layers * 0.5f;
            MkBox(go, V(0, h * t + h/layers*0.5f, -baseW*0.25f*(1f-t)),
                  V(w, h/layers+0.01f, baseW*0.5f*(1f-t)+0.02f),
                  c * (0.85f + t*0.15f), SM_GLAZED_TILE);
        }
    }

    // Procedural curved roof tile rows along perimeter
    static void BuildCurvedRoof(Transform parent, Vector3 center, float w, float d, Color tileC, float ridgeH)
    {
        // Front and back
        BuildTileRow(parent, center + V(0,0,-d*0.5f), w, tileC, 0f);
        BuildTileRow(parent, center + V(0,0, d*0.5f), w, tileC, 180f);
        // Left and right
        BuildTileRow(parent, center + V(-w*0.5f,0,0), d, tileC, 90f);
        BuildTileRow(parent, center + V( w*0.5f,0,0), d, tileC, 270f);
    }

    static void BuildCurvedRoof(Transform parent, Vector3 center, float w, float d, Color tileC)
    {
        BuildCurvedRoof(parent, center, w, d, tileC, 0f);
    }

    static void BuildTileRow(Transform parent, Vector3 start, float length, Color c, float rotY)
    {
        var go = new GameObject("TileRow");
        go.transform.SetParent(parent);
        go.transform.position = start;
        go.transform.eulerAngles = V(0, rotY, 0);

        int count = Mathf.RoundToInt(length / TILE);
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            float x = -length*0.5f + i*TILE + TILE*0.5f;
            // Sinusoidal curve: tiles lift at center, droop at ends (처마 곡선)
            float curveY = Mathf.Sin(t * Mathf.PI) * 0.18f;
            float curveZ = Mathf.Cos(t * Mathf.PI) * 0.04f;

            // Barrel tile (암키와)
            MkBox(go, V(x, curveY, curveZ), V(TILE-0.01f, 0.08f, 0.30f), c, SM_GLAZED_TILE);
            // Cap tile (수키와)
            if (i % 2 == 0)
                MkCyl(go, V(x, curveY+0.06f, curveZ), 0.055f, 0.28f,
                      c * 0.75f, SM_GLAZED_TILE, true); // horizontal
            // Drip tile (막새)
            MkBox(go, V(x, curveY, curveZ-0.18f), V(TILE-0.01f,0.12f,0.08f), c*(0.7f), SM_GLAZED_TILE);
        }
    }

    static void CreateDistantBuildings()
    {
        var bg = Mk(ROOT_BG);
        Color tile  = Hex("3A3028");
        Color dark  = Hex("2E2018");
        Color wall  = Hex("BFB5A2");

        // Distant background hanok silhouettes (3 buildings at varied depths)
        float[] depths = {-14f, -18f, -22f};
        float[] widths = { 9f,  14f,   6f};
        float[] offs   = { 3f,  -2f,   5f};
        for (int i = 0; i < depths.Length; i++)
        {
            float alpha = 1f - i * 0.22f; // fade with distance
            // Wall
            MkBox(bg, V(offs[i],1.1f,depths[i]), V(widths[i],2.2f,0.3f), wall*alpha, SM_CLAY);
            // Simple hip roof
            MkBox(bg, V(offs[i],2.4f,depths[i]), V(widths[i]+1.4f,0.8f,0.5f), tile*alpha, SM_GLAZED_TILE);
            MkBox(bg, V(offs[i],3.0f,depths[i]), V(widths[i]*0.55f,0.3f,0.3f), tile*alpha, SM_GLAZED_TILE);
        }
    }

    static void CreateMountainSilhouettes()
    {
        var m = Mk("_HanokMountains");
        Color mtnFar  = Hex("6B7FA0");
        Color mtnMid  = Hex("4E6080");
        Color mtnNear = Hex("3A4E68");

        // Far mountains
        float[] mtnX = {-28f,-12f, 4f,18f,32f};
        float[] mtnH = { 14f, 18f,22f,16f,12f};
        float[] mtnW = { 20f, 28f,24f,22f,18f};
        for (int i = 0; i < mtnX.Length; i++)
        {
            // Body
            FlatSph(m, V(mtnX[i], mtnH[i]*0.5f, -55f), mtnW[i]*0.5f,  mtnFar,  0f);
            FlatSph(m, V(mtnX[i]+4, mtnH[i]*0.4f, -48f), mtnW[i]*0.4f, mtnMid, 0f);
        }
        // Near ridge
        for (int i = -3; i <= 3; i++)
            FlatSph(m, V(i*12f, 5f, -32f), 9f, mtnNear, 0f);
    }

    static void CreateBroadGroves()
    {
        var g = Mk(ROOT_PINES);
        // Flanking pine trees
        float[][] positions = {
            new[]{-9f, 0f, -4f}, new[]{-10f,0f,  2f},
            new[]{ 9f, 0f, -3f}, new[]{ 10f,0f,  3f},
            new[]{-8f, 0f,  6f}, new[]{ 8f, 0f,  5f},
        };
        float[] scales = {1.2f, 0.9f, 1.1f, 1.0f, 0.85f, 1.15f};
        for (int i = 0; i < positions.Length; i++)
            Pine(g.transform, V(positions[i][0], positions[i][1], positions[i][2]),
                 scales[i], Hex("1E3A1A"), Hex("3D2B18"));
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  PRESET 1 — 사랑채 (Scholar's Outer Quarters)
    // ═════════════════════════════════════════════════════════════════════════

    static void CreateSarangchaeRoom()
    {
        var s = Mk(ROOT_SARANG);
        Color dark   = Hex("2A1E14");
        Color darkM  = Hex("3A2818");
        Color red    = Hex("8B2800");
        Color paper  = Hex("EDE0CC");
        Color stone  = Hex("9E9488");
        Color tile   = Hex("3C3028");
        Color floor  = Hex("4A3020");
        Color lacq   = Hex("1A0E08");

        // Platform (툇돌)
        MkBox(s, V(0,0.04f,0), V(16f,0.08f,12f), stone, SM_POLISHED_GRAN);

        // Main 사랑채 structure — 4 bays × 2 bays
        BuildHanok(s.transform, V(0,0.08f,0), 4, 2, COL_H, true);

        // 툇마루 (wooden veranda) in front
        float verandaZ = -KAN;
        MkBox(s, V(0, 0.40f, verandaZ-KAN*0.3f), V(4*KAN, 0.08f, KAN*0.6f), floor, SM_DARK_WOOD);
        // Veranda plank lines
        for (int pl = 0; pl < 6; pl++)
            MkBox(s, V(0, 0.45f, verandaZ-KAN*0.3f+pl*0.36f-0.9f),
                  V(4*KAN, 0.02f, 0.02f), lacq, SM_LACQUER);

        // 난간 (veranda railing)
        float railY = 0.75f;
        MkBox(s, V( 0,         railY, verandaZ-KAN*0.55f), V(4*KAN+0.1f,0.08f,0.08f), dark, SM_DARK_WOOD);
        // Rail posts
        for (int rp = 0; rp <= 8; rp++)
        {
            float rpx = -2*KAN + rp*(4*KAN/8f);
            MkBox(s, V(rpx, 0.55f, verandaZ-KAN*0.55f), V(0.08f,0.55f,0.08f), dark, SM_DARK_WOOD);
        }
        // Rail lattice fill
        BuildShojiGrid(s.transform, V(0, 0.58f, verandaZ-KAN*0.55f+0.02f), 4*KAN, 0.30f, 16, 2);

        // 분합문 (folding door panels) — 3 bays each with 4 panels
        for (int bay = 0; bay < 4; bay++)
        {
            float bx = -2*KAN + KAN*0.5f + bay*KAN;
            BuildShojiGrid(s.transform, V(bx, COL_H*0.45f, -KAN+0.01f), KAN*0.23f, COL_H*0.7f, 2, 7);
            BuildShojiGrid(s.transform, V(bx+KAN*0.26f, COL_H*0.45f, -KAN+0.01f), KAN*0.23f, COL_H*0.7f, 2, 7);
        }

        // 화분 on veranda
        float[] potX2 = {-5.5f, -3.5f, 3.5f, 5.5f};
        foreach (float px in potX2)
        {
            MkCyl(s, V(px, 0.50f, verandaZ-KAN*0.3f), 0.15f, 0.22f, Hex("6B3C1E"), SM_CLAY);
            FlatSph(s, V(px, 0.72f, verandaZ-KAN*0.3f), 0.16f, Hex("2A5020"), SM_ROUGH_STONE);
        }

        // 서책/문방 props on veranda
        // Low desk
        MkBox(s, V( 1.5f, 0.50f, verandaZ), V(0.8f,0.06f,0.5f), Hex("2A1808"), SM_LACQUER);
        MkBox(s, V( 1.5f, 0.47f, verandaZ), V(0.7f,0.10f,0.04f), Hex("1E1008"), SM_LACQUER);
        // Book stack
        MkBox(s, V( 1.5f, 0.58f, verandaZ), V(0.22f,0.12f,0.18f), Hex("4A3828"), SM_DARK_WOOD);
        MkBox(s, V( 1.5f, 0.68f, verandaZ), V(0.18f,0.06f,0.16f), Hex("6E5A44"), SM_DARK_WOOD);

        // 석등 (stone lantern)
        StoneLantern(s.transform, V(-6.5f, 0.08f, 0f));

        // 연지 (small decorative pond)
        MkBox(s, V( 6.5f,0.03f,-0.5f), V(2.2f,0.06f,1.6f), stone, SM_ROUGH_STONE);
        MkBox(s, V( 6.5f,0.08f,-0.5f), V(1.9f,0.04f,1.3f), Hex("3A6A8A"), SM_WATER);
        // Lotus in pond
        FlatSph(s, V(6.3f,0.14f,-0.3f), 0.18f, Hex("E8A0A0"), SM_PAPER);  // flower
        MkBox(s, V(6.7f,0.10f,-0.6f), V(0.24f,0.04f,0.24f), Hex("2A6020"), SM_ROUGH_STONE); // leaf
    }

    static void CreateLightTrees()
    {
        var t = Mk(ROOT_PINES);
        PlumTree(t.transform, V(-8f,0f,-3f));
        PlumTree(t.transform, V( 8f,0f, 2f));
        PlumTree(t.transform, V(-7f,0f, 4f));
        Pine(t.transform, V(10f,0f,-2f),  1.0f, Hex("1E3A1A"), Hex("3D2B18"));
        Pine(t.transform, V(-10f,0f,3f),  0.9f, Hex("1E3A1A"), Hex("3D2B18"));
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  PRESET 2 — 조선 장터 (Market)
    // ═════════════════════════════════════════════════════════════════════════

    static void CreateMarketStalls()
    {
        var mk = Mk(ROOT_MARKET);
        Color dark   = Hex("2A1E14");
        Color straw  = Hex("C8A840");
        Color earth  = Hex("8B6914");
        Color stone  = Hex("9E9488");
        Color clay   = Hex("6B3C1E");

        // Market ground — flattened, trampled earth
        MkBox(mk, V(0,-0.01f,0), V(28f,0.02f,18f), Hex("A8946C"), SM_CLAY);

        // Main stall row (rear, 6 stalls)
        string[] awningHex = {"8B2800","4A6B2A","2A4A8B","8B7800","6B2A6B","2A6B6B"};
        for (int i = 0; i < 6; i++)
        {
            float sx = -7.5f + i * 2.8f;
            BuildStall(mk.transform, V(sx, 0f, -5f), 2.4f, 2.0f, awningHex[i % awningHex.Length]);
        }

        // Side stalls (left)
        for (int i = 0; i < 3; i++)
            BuildStall(mk.transform, V(-9.5f, 0f, -1f + i*3.2f), 2.2f, 2.0f, awningHex[i+2]);
        // Side stalls (right)
        for (int i = 0; i < 3; i++)
            BuildStall(mk.transform, V( 9.5f, 0f, -1f + i*3.2f), 2.2f, 2.0f, awningHex[i]);

        // 우물 (well) at market center
        MarketWell(mk.transform, V(0f, 0f, 2f));

        // 저울 (beam scale) at a prominent stall
        MarketScale(mk.transform, V(3.2f, 0f, -3.8f));

        // 멱통 (straw bundles) scatter
        float[][] bpos = new float[][] { new[]{-3f,-2.5f}, new[]{3f,-2f}, new[]{-4f,1f}, new[]{5f,1.5f}, new[]{-2f,3f} };
        foreach (float[] bp in bpos)
            StrawBundle(mk.transform, V(bp[0], 0f, bp[1]));

        // 항아리 (onggi jars) cluster
        float[][] jpos = new float[][] { new[]{-6f,-1f}, new[]{-5.5f,-0.4f}, new[]{-5f,-1.2f}, new[]{4.5f,0.8f}, new[]{5f,0.2f} };
        foreach (float[] jp in jpos)
        {
            MkCyl(mk, V(jp[0], 0.20f, jp[1]), 0.17f, 0.38f, clay, SM_CLAY);
            MkCyl(mk, V(jp[0], 0.30f, jp[1]), 0.19f, 0.12f, clay*(0.8f), SM_CLAY);
        }

        // 포목 (cloth bolts) hanging at 2 stalls
        Color[] clothC = {Hex("E8D0B0"), Hex("B08040"), Hex("8B3020"), Hex("405888")};
        for (int ci = 0; ci < clothC.Length; ci++)
            MkBox(mk, V(-7.5f + ci*0.6f, 1.8f, -4.85f), V(0.44f,1.4f,0.06f), clothC[ci], SM_PAPER);

        // 등롱 (market lanterns)
        CreateLantern(mk.transform, V(-2.5f, 0f, -4.0f));
        CreateLantern(mk.transform, V( 2.5f, 0f, -4.0f));
        CreateLantern(mk.transform, V( 0.0f, 0f,  1.0f));

        // Silhouette figures (simple shapes suggesting people/movement)
        float[][] figPos = new float[][] { new[]{-1f,0f}, new[]{2f,-1f}, new[]{-3f,1f}, new[]{1f,2f}, new[]{-2f,-2f} };
        foreach (float[] fp in figPos)
            MarketFigure(mk.transform, V(fp[0], 0f, fp[1]));
    }

    static void BuildStall(Transform parent, Vector3 pos, float w, float h, string colorHex)
    {
        var go = Mk("Stall", parent);
        go.transform.position = pos;
        Color awning = Hex(colorHex);
        Color dark   = Hex("2A1E14");
        Color stone  = Hex("9E9488");

        // 4 posts
        float hw = w*0.5f;
        float[] px2 = {-hw, hw};
        foreach (float px in px2)
        {
            MkBox(go, V(px,h*0.5f,-0.4f), V(0.10f,h,0.10f), dark, SM_DARK_WOOD);
            MkBox(go, V(px,h*0.5f, 0.4f), V(0.10f,h,0.10f), dark, SM_DARK_WOOD);
        }

        // Awning — front sloped canopy
        MkBox(go, V(0,h+0.05f,0),    V(w+0.2f,0.06f,1.0f), awning, SM_CLAY);
        // Front overhang drape (angled)
        MkBox(go, V(0,h-0.22f,-0.5f), V(w+0.1f,0.06f,0.1f), awning*(0.8f), SM_CLAY);

        // Goods shelf
        MkBox(go, V(0, h*0.45f, 0.02f), V(w-0.1f,0.06f,0.6f), dark*(0.7f), SM_DARK_WOOD);
        MkBox(go, V(0, h*0.24f, 0.02f), V(w-0.1f,0.06f,0.6f), dark*(0.7f), SM_DARK_WOOD);

        // Base cloth drape at front
        MkBox(go, V(0,h*0.22f,-0.62f), V(w-0.12f,h*0.44f,0.04f), awning*(0.6f), SM_PAPER);
    }

    static void MarketWell(Transform parent, Vector3 pos)
    {
        var go = Mk("Well", parent);
        go.transform.position = pos;
        Color stone = Hex("9E9488");
        Color dark  = Hex("2A1E14");
        Color rope  = Hex("C8A840");
        Color water = Hex("3A6A8A");

        MkCyl(go, V(0,0.30f,0),  0.60f, 0.60f, stone, SM_ROUGH_STONE); // outer
        MkCyl(go, V(0,0.34f,0),  0.48f, 0.56f, water, SM_WATER);       // water
        MkCyl(go, V(0,0.62f,0),  0.52f, 0.06f, stone*(0.8f), SM_ROUGH_STONE); // rim
        // Frame posts
        MkBox(go, V(-0.5f,1.0f,0),  V(0.10f,0.8f,0.10f), dark, SM_DARK_WOOD);
        MkBox(go, V( 0.5f,1.0f,0),  V(0.10f,0.8f,0.10f), dark, SM_DARK_WOOD);
        // Cross beam
        MkBox(go, V(0,1.42f,0), V(1.2f,0.10f,0.10f), dark, SM_DARK_WOOD);
        // Rope / bucket
        MkBox(go, V(0.1f,1.0f,0), V(0.02f,0.8f,0.02f), rope, SM_DARK_WOOD);
        MkCyl(go, V(0.1f,0.62f,0), 0.12f,0.18f, stone, SM_ROUGH_STONE);
    }

    static void MarketScale(Transform parent, Vector3 pos)
    {
        var go = Mk("Scale", parent);
        go.transform.position = pos;
        Color wood  = Hex("5A3C1E");
        Color iron  = Hex("5A5A5A");
        Color rope  = Hex("C8A840");

        // Stand
        MkBox(go, V(0,0.5f,0), V(0.06f,1.0f,0.06f), wood, SM_DARK_WOOD);
        MkBox(go, V(0,0.06f,0), V(0.4f,0.12f,0.24f), wood, SM_DARK_WOOD);
        // Beam
        MkBox(go, V(0,1.06f,0), V(0.90f,0.06f,0.06f), iron, SM_IRON);
        // Pans
        MkBox(go, V(-0.38f,0.82f,0), V(0.28f,0.04f,0.28f), iron, SM_IRON);
        MkBox(go, V( 0.38f,0.82f,0), V(0.28f,0.04f,0.28f), iron, SM_IRON);
        // Strings
        MkBox(go, V(-0.38f,0.94f,0), V(0.02f,0.24f,0.02f), rope, SM_DARK_WOOD);
        MkBox(go, V( 0.38f,0.94f,0), V(0.02f,0.24f,0.02f), rope, SM_DARK_WOOD);
        // Weight
        FlatSph(go, V(0.38f,0.72f,0), 0.10f, iron, SM_IRON);
    }

    static void StrawBundle(Transform parent, Vector3 pos)
    {
        var go = Mk("Straw", parent);
        go.transform.position = pos;
        Color straw = Hex("C8A840");
        MkCyl(go, V(0,0.28f,0), 0.22f, 0.55f, straw, SM_CLAY);
        MkBox(go, V(0,0.44f,0), V(0.5f,0.06f,0.5f), straw*(0.7f), SM_CLAY); // bind
        // Straw top
        for (int st = 0; st < 6; st++)
        {
            float sa = st * Mathf.PI / 3f;
            MkBox(go, V(Mathf.Cos(sa)*0.12f,0.58f,Mathf.Sin(sa)*0.12f),
                  V(0.06f,0.18f,0.06f), straw*(0.9f), SM_CLAY);
        }
    }

    static void MarketFigure(Transform parent, Vector3 pos)
    {
        var go = Mk("Figure", parent);
        go.transform.position = pos;
        Color body = Hex("E8E0D0");
        Color hat  = Hex("1A1A1A");
        // Simple silhouette: body + head + gat (hat)
        MkBox(go,   V(0,0.7f,0),  V(0.28f,1.0f,0.18f), body, SM_PAPER); // body
        FlatSph(go, V(0,1.24f,0), 0.14f, body, SM_PAPER);                // head
        MkBox(go,   V(0,1.44f,0), V(0.32f,0.06f,0.32f), hat, SM_DARK_WOOD); // gat brim
        MkCyl(go,   V(0,1.54f,0), 0.10f, 0.14f, hat, SM_DARK_WOOD);         // gat top
    }

    static void CreatePineGroves()
    {
        var g = Mk(ROOT_PINES);
        float[] px = {-12f,-13f, 11f,12f,-10f,10f};
        float[] pz = {  -2f,  3f,  -1f, 4f,  6f, 6f};
        float[] sc = {1.1f,0.9f,1.0f,0.85f,1.2f,0.95f};
        for (int i = 0; i < px.Length; i++)
            Pine(g.transform, V(px[i],0,pz[i]), sc[i], Hex("1E3A1A"), Hex("3D2B18"));
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  PRESET 3 — 전통 정원 (Traditional Garden)
    // ═════════════════════════════════════════════════════════════════════════

    static void CreateGardenElements()
    {
        var g = Mk(ROOT_GARDEN);
        Color stone  = Hex("9E9488");
        Color moss   = Hex("3A5C2A");
        Color water  = Hex("3A6A8A");
        Color sand   = Hex("C8B89A");
        Color dark   = Hex("2A1E14");
        Color tile   = Hex("3C3028");

        // ── 연지 (lotus pond) — multi-layer ────────────────────────────────
        MkBox(g, V(0, 0.00f, 0.5f), V(8.5f,0.08f,5.5f), stone,  SM_ROUGH_STONE);  // base
        MkBox(g, V(0, 0.06f, 0.5f), V(7.8f,0.05f,4.8f), Hex("2A5A72"), SM_WATER); // deep
        MkBox(g, V(0, 0.09f, 0.5f), V(7.4f,0.04f,4.4f), water,  SM_WATER);        // surface

        // Pond edge stones (16 mossy stones)
        for (int i = 0; i < 16; i++)
        {
            float angle  = i * Mathf.PI * 2f / 16f;
            float radius = i % 2 == 0 ? 4.3f : 3.9f;
            float ex = Mathf.Cos(angle) * radius;
            float ez = 0.5f + Mathf.Sin(angle) * 2.6f;
            float sz = 0.3f + (i % 3)*0.12f;
            MkBox(g, V(ex, 0.07f, ez), V(sz+0.1f, sz*0.4f, sz), moss, SM_ROUGH_STONE);
        }

        // 연꽃 (lotus flowers) × 6
        float[][] lotusPos = new float[][] {
            new[]{-1.5f,0.5f}, new[]{1.0f,1.2f}, new[]{-0.5f,-0.5f},
            new[]{ 2.0f,-0.3f}, new[]{-2.2f,1.0f}, new[]{0.5f,2.0f}
        };
        foreach (float[] lp in lotusPos)
        {
            Vector3 lpos = V(lp[0], 0.15f, 0.5f+lp[1]);
            // Stem
            MkCyl(g, lpos + V(0,-0.1f,0), 0.025f, 0.22f, Hex("2A5020"), SM_ROUGH_STONE);
            // Leaf
            MkBox(g, lpos + V(0.12f, 0.02f, 0.08f), V(0.30f,0.03f,0.28f), Hex("2E5C20"), SM_ROUGH_STONE);
            // Flower (layered petals)
            FlatSph(g, lpos + V(0,0.05f,0), 0.15f, Hex("F0C8C8"), SM_PAPER);
            FlatSph(g, lpos + V(0,0.12f,0), 0.10f, Hex("E8A0A0"), SM_PAPER);
            FlatSph(g, lpos + V(0,0.17f,0), 0.06f, Hex("F8E090"), SM_PAPER); // center
        }

        // ── 계류 (garden stream) flowing from left ──────────────────────────
        for (int seg = 0; seg < 8; seg++)
        {
            float t   = seg / 8f;
            float sx  = -8f + seg * 1.2f;
            float sz2 = Mathf.Sin(t * Mathf.PI * 1.5f) * 1.0f - 3.5f;
            float sw  = 0.6f + Mathf.Sin(t * Mathf.PI) * 0.3f;
            MkBox(g, V(sx, 0.02f, sz2), V(sw, 0.06f, 1.0f), water, SM_WATER);
            // Stream bank stones
            MkBox(g, V(sx, 0.01f, sz2+0.55f), V(sw+0.2f,0.04f,0.12f), stone, SM_ROUGH_STONE);
            MkBox(g, V(sx, 0.01f, sz2-0.55f), V(sw+0.2f,0.04f,0.12f), stone, SM_ROUGH_STONE);
        }

        // ── 정자 (pavilion) — 6-post hexagonal ──────────────────────────────
        Vector3 pavilionCenter = V(5.5f, 0f, -2f);
        Build6PostPavilion(g.transform, pavilionCenter);

        // ── 괴석 (scholar's rock) arrangement ──────────────────────────────
        GardenRock(g.transform, V(-5.5f,0.28f,-2.5f), 0.55f, Hex("706258"), Hex("5C5048"));
        GardenRock(g.transform, V(-4.8f,0.18f,-2.0f), 0.35f, Hex("7A6C60"), Hex("645854"));
        GardenRock(g.transform, V(-6.0f,0.14f,-1.8f), 0.28f, Hex("686058"), Hex("58504A"));
        GardenRock(g.transform, V(-5.3f,0.10f,-3.2f), 0.22f, Hex("787068"), Hex("60585C"));

        // 이끼 패치 (moss patches)
        float[][] mossPos = new float[][] { new[]{-3f,-4f}, new[]{2f,-3.5f}, new[]{-1f,3f}, new[]{4f,2.5f}, new[]{-5f,1f}, new[]{6f,-1f}, new[]{3f,-5f}, new[]{-2f,-2f} };
        foreach (float[] mp in mossPos)
            MkBox(g, V(mp[0], 0.02f, mp[1]), V(0.7f+Mathf.Abs(mp[0])%0.4f, 0.04f, 0.5f+Mathf.Abs(mp[1])%0.3f),
                  moss, SM_ROUGH_STONE);

        // 석등 (stone lantern near pavilion)
        StoneLantern(g.transform, V(4.2f, 0f, -0.5f));
        StoneLantern(g.transform, V(-3.5f, 0f, 3.5f));
    }

    static void Build6PostPavilion(Transform parent, Vector3 center)
    {
        var go = Mk("Pavilion", parent);
        go.transform.position = center;
        Color dark  = Hex("2A1E14");
        Color red   = Hex("8B2800");
        Color tile  = Hex("3C3028");
        Color stone = Hex("9E9488");
        Color floor = Hex("4A3020");
        const float R = 1.8f; // radius

        // Stone base platform
        MkBox(go, V(0,-0.04f,0), V(R*2.2f,0.08f,R*2.2f), stone, SM_POLISHED_GRAN);

        // 6 posts in hexagon
        for (int i = 0; i < 6; i++)
        {
            float angle = i * Mathf.PI / 3f;
            float px = Mathf.Cos(angle) * R;
            float pz = Mathf.Sin(angle) * R;
            MkBox(go, V(px,-0.02f,pz), V(0.30f,0.06f,0.30f), stone, SM_POLISHED_GRAN); // 주초
            MkCyl(go, V(px,COL_H*0.5f+0.04f,pz), 0.14f, COL_H, red, SM_LACQUER);
        }

        // Floor decking
        MkBox(go, V(0,0.08f,0), V(R*1.85f,0.06f,R*1.85f), floor, SM_DARK_WOOD);
        // Floor planks
        for (int pl = -3; pl <= 3; pl++)
            MkBox(go, V(0,0.12f,pl*0.5f), V(R*1.8f,0.02f,0.02f), dark, SM_DARK_WOOD);

        // 난간 (railing) between posts
        for (int i = 0; i < 6; i++)
        {
            float a1 = i * Mathf.PI / 3f;
            float a2 = (i+1) * Mathf.PI / 3f;
            Vector3 p1 = V(Mathf.Cos(a1)*R, COL_H*0.3f, Mathf.Sin(a1)*R);
            Vector3 p2 = V(Mathf.Cos(a2)*R, COL_H*0.3f, Mathf.Sin(a2)*R);
            Vector3 mid = (p1+p2)*0.5f;
            float dist = Vector3.Distance(p1,p2);
            float ang  = Mathf.Atan2(p2.z-p1.z, p2.x-p1.x) * Mathf.Rad2Deg;
            // Rail bar
            var rail = MkBox(go, mid, V(dist,0.06f,0.06f), dark, SM_DARK_WOOD);
            rail.transform.eulerAngles = V(0,-ang,0);
        }

        // Top ring beam
        MkBox(go, V(0,COL_H+0.1f,0), V(R*2.1f,0.16f,0.16f), dark, SM_DARK_WOOD);
        MkBox(go, V(0,COL_H+0.1f,0), V(0.16f,0.16f,R*2.1f), dark, SM_DARK_WOOD);

        // Conical roof
        int roofLayers = 10;
        for (int rl = 0; rl < roofLayers; rl++)
        {
            float t = (float)rl / roofLayers;
            float rr = R * 1.35f * (1f-t*0.88f);
            float ry = COL_H + 0.2f + rl * 0.3f;
            MkCyl(go, V(0,ry,0), rr, 0.22f+t*0.06f, tile*(0.85f+t*0.15f), SM_GLAZED_TILE);
        }
        // Finial
        FlatSph(go, V(0,COL_H+3.4f,0), 0.20f, tile*(0.6f), SM_GLAZED_TILE);
    }

    static void CreateGardenTrees()
    {
        var t = Mk(ROOT_PINES);
        // Red maple
        RedMapleTree(t.transform, V(-7f, 0f, -1f));
        RedMapleTree(t.transform, V( 7f, 0f,  3f));
        // 산수유 (cornelian cherry)
        SansuyuTree(t.transform, V(-5f, 0f, -4f));
        SansuyuTree(t.transform, V( 7f, 0f, -3f));
        // Bamboo groves
        CreateBambooGrove(t.transform, V(-8f, 0f, 3f));
        CreateBambooGrove(t.transform, V( 9f, 0f,-1f));
        // Pine
        Pine(t.transform, V(-9f,0f,0f), 1.2f, Hex("1E3A1A"), Hex("3D2B18"));
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  TREE / PLANT BUILDERS
    // ═════════════════════════════════════════════════════════════════════════

    static void Pine(Transform parent, Vector3 pos, float scale, Color needle, Color bark)
    {
        var go = Mk("Pine", parent);
        go.transform.position = pos;

        // Trunk — tapered via 2 cylinders
        MkCyl(go, V(0, scale*1.8f, 0),      scale*0.14f, scale*3.6f, bark, SM_DARK_WOOD);
        MkCyl(go, V(0, scale*3.6f, 0),      scale*0.08f, scale*1.8f, bark*(0.85f), SM_DARK_WOOD);

        // Branches (8, alternating)
        for (int b = 0; b < 8; b++)
        {
            float ba  = b * Mathf.PI / 4f;
            float bh  = scale*(1.2f + b*0.5f);
            float blen= scale*(1.0f - b*0.08f);
            MkBox(go, V(Mathf.Cos(ba)*blen*0.5f, bh, Mathf.Sin(ba)*blen*0.5f),
                  V(blen,scale*0.06f,scale*0.06f), bark*(0.8f), SM_DARK_WOOD);
        }

        // 8-layer canopy with irregular offsets
        float[] offX = {0.04f,-0.06f,0.08f,-0.03f,0.05f,-0.08f,0.02f,-0.05f};
        float[] offZ = {0.06f, 0.04f,-0.05f,0.08f,-0.06f,0.03f,-0.04f,0.07f};
        for (int i = 0; i < 8; i++)
        {
            float t   = (float)i/8f;
            float cy  = scale*(2.2f + i*0.65f);
            float cr  = scale*(1.4f - i*0.12f);
            float bright = 0.7f + t*0.3f;
            FlatSph(go, V(offX[i]*scale*2, cy, offZ[i]*scale*2), cr, needle*bright, SM_ROUGH_STONE);
        }
    }

    static void PlumTree(Transform parent, Vector3 pos)
    {
        var go = Mk("PlumTree", parent);
        go.transform.position = pos;
        Color bark   = Hex("3C2818");
        Color blossom= Hex("F0C0C8");
        Color center = Hex("FFEA80");

        MkCyl(go, V(0,1.2f,0), 0.12f, 2.4f, bark, SM_DARK_WOOD);

        // 6 branches in various directions with natural angles
        float[] bAngles = {15f, 75f, 135f, 195f, 255f, 315f};
        float[] bElev   = {35f, 20f,  40f,  25f,  45f,  30f};
        float[] bLen    = {1.6f,1.4f, 1.8f, 1.3f, 1.7f, 1.5f};
        for (int b = 0; b < 6; b++)
        {
            float rad = bAngles[b] * Mathf.Deg2Rad;
            float elv = bElev[b]   * Mathf.Deg2Rad;
            Vector3 dir = V(Mathf.Cos(rad)*Mathf.Cos(elv),
                            Mathf.Sin(elv),
                            Mathf.Sin(rad)*Mathf.Cos(elv));
            Vector3 btip = dir * bLen[b];
            btip.y += 1.8f;

            var branch = MkBox(go, btip*0.5f + V(0,1.8f,0),
                               V(bLen[b],0.06f,0.06f), bark, SM_DARK_WOOD);
            branch.transform.LookAt(go.transform.position + btip + V(0,1.8f,0));

            // 4 blossoms per branch tip
            for (int fl = 0; fl < 4; fl++)
            {
                Vector3 fpos = btip + dir*(-0.2f+fl*0.18f) + V(0,1.8f,0)
                             + V(Random.Range(-0.12f,0.12f),
                                 Random.Range(-0.08f,0.12f),
                                 Random.Range(-0.12f,0.12f));
                FlatSph(go, fpos, 0.12f, blossom, SM_PAPER);
                FlatSph(go, fpos+V(0,0.06f,0), 0.06f, center, SM_PAPER);
            }
        }
    }

    static void RedMapleTree(Transform parent, Vector3 pos)
    {
        var go = Mk("RedMaple", parent);
        go.transform.position = pos;
        Color bark  = Hex("3A2010");
        Color leaf1 = Hex("C83020");
        Color leaf2 = Hex("E04818");
        Color leaf3 = Hex("8B2010");

        MkCyl(go, V(0,1.5f,0), 0.14f, 3.0f, bark, SM_DARK_WOOD);
        // 4-layer canopy — irregular, offset, red hues
        float[] ly = {2.4f, 3.1f, 3.7f, 4.1f};
        float[] lr = {1.6f, 1.9f, 1.5f, 1.0f};
        Color[] lc = {leaf3, leaf1, leaf2, leaf1};
        Vector3[] loff = {V(0.15f,0,-0.12f), V(-0.10f,0,0.18f), V(0.08f,0,-0.08f), V(-0.05f,0,0.05f)};
        for (int li = 0; li < 4; li++)
            FlatSph(go, V(0,ly[li],0)+loff[li], lr[li], lc[li], SM_ROUGH_STONE);
        // Fallen leaf suggestions at base
        for (int fl = 0; fl < 8; fl++)
        {
            float fa = fl * Mathf.PI/4f;
            MkBox(go, V(Mathf.Cos(fa)*0.7f, 0.03f, Mathf.Sin(fa)*0.7f),
                  V(0.18f,0.02f,0.14f), leaf1*(0.7f+fl%3*0.1f), SM_ROUGH_STONE);
        }
    }

    static void SansuyuTree(Transform parent, Vector3 pos)
    {
        var go = Mk("Sansuyu", parent);
        go.transform.position = pos;
        Color bark   = Hex("4A3018");
        Color flower = Hex("F0D030");
        Color leaf   = Hex("2A4A18");

        MkCyl(go, V(0,1.4f,0), 0.10f, 2.8f, bark, SM_DARK_WOOD);
        // Multi-branch structure
        for (int b = 0; b < 5; b++)
        {
            float ba  = b * Mathf.PI*2f/5f;
            float bh  = 1.4f + b*0.4f;
            float blen= 0.9f + b%2*0.3f;
            MkBox(go, V(Mathf.Cos(ba)*blen*0.5f, bh, Mathf.Sin(ba)*blen*0.5f),
                  V(blen, 0.05f, 0.05f), bark, SM_DARK_WOOD);
        }
        // Yellow flower clusters
        float[] fy = {2.0f,2.5f,3.0f,2.8f,2.2f};
        float[] fx = {0.3f,-0.5f,0.1f,-0.3f,0.5f};
        float[] fz = {0.4f, 0.2f,-0.4f,0.5f,-0.3f};
        for (int fi = 0; fi < 5; fi++)
            FlatSph(go, V(fx[fi],fy[fi],fz[fi]), 0.35f, flower, SM_ROUGH_STONE);
        // Sparse leaf layer above flowers
        FlatSph(go, V(0, 3.2f, 0), 0.9f, leaf, SM_ROUGH_STONE);
    }

    static void CreateBambooGrove(Transform parent, Vector3 center)
    {
        var go = Mk("BambooGrove", parent);
        go.transform.position = center;
        Color green = Hex("3A6B2A");
        Color darkG = Hex("2A5018");
        Color tan   = Hex("C8A840");

        // 10 culms, varied positions
        float[] bx = {0f,-0.6f,0.5f,-0.3f,0.7f,-0.8f,0.2f,-0.4f,0.6f,-0.1f};
        float[] bz = {0f, 0.4f,-0.3f,0.7f, 0.2f, 0.6f,-0.5f,-0.2f,0.8f, 0.5f};
        float[] bh = {5.5f,4.8f,6.0f,5.2f,4.6f,5.8f,5.0f,5.4f,4.9f,5.7f};
        for (int ci = 0; ci < 10; ci++)
        {
            // Culm with internodes
            int nodes = Mathf.RoundToInt(bh[ci] / 0.48f);
            MkCyl(go, V(bx[ci],bh[ci]*0.5f,bz[ci]), 0.055f, bh[ci], green*(0.9f+ci%3*0.05f), SM_ROUGH_STONE);
            // Node rings
            for (int n = 0; n < nodes; n++)
                MkBox(go, V(bx[ci],n*0.48f+0.1f,bz[ci]), V(0.09f,0.04f,0.09f), tan, SM_ROUGH_STONE);
            // Leaf clusters at top third
            int leaves = 4;
            for (int lv = 0; lv < leaves; lv++)
            {
                float la   = lv * Mathf.PI / 2f + ci*0.3f;
                float ly   = bh[ci] * 0.7f + lv * bh[ci]*0.07f;
                float llen = 0.7f - lv*0.08f;
                MkBox(go, V(bx[ci]+Mathf.Cos(la)*llen*0.5f, ly, bz[ci]+Mathf.Sin(la)*llen*0.5f),
                      V(llen,0.03f,0.08f), darkG, SM_ROUGH_STONE);
            }
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  PROPS
    // ═════════════════════════════════════════════════════════════════════════

    static void StoneLantern(Transform parent, Vector3 pos)
    {
        var go = Mk("StoneLantern", parent);
        go.transform.position = pos;
        Color gran  = Hex("B0A898");
        Color granD = Hex("9A9082");
        Color light = Hex("FFD090");

        MkBox(go, V(0,0.08f,0), V(0.50f,0.16f,0.50f), granD, SM_ROUGH_STONE); // 지대석
        MkBox(go, V(0,0.20f,0), V(0.42f,0.10f,0.42f), gran,  SM_POLISHED_GRAN);
        MkCyl(go, V(0,0.52f,0), 0.10f, 0.50f, granD, SM_ROUGH_STONE);          // 간주석
        MkCyl(go, V(0,0.82f,0), 0.12f, 0.08f, gran,  SM_POLISHED_GRAN);
        MkBox(go, V(0,0.92f,0), V(0.38f,0.42f,0.38f), gran,  SM_POLISHED_GRAN);// 화사석
        // Light inside
        MkBox(go, V(0,0.92f,0), V(0.22f,0.34f,0.22f), light, SM_PAPER);
        // 4 window openings (dark frame)
        float[] wdx = {0.19f,-0.19f,0f,0f};
        float[] wdz = {0f,0f,0.19f,-0.19f};
        for (int wi = 0; wi < 4; wi++)
            MkBox(go, V(wdx[wi],0.92f,wdz[wi]), V(0.04f,0.22f,0.04f), Hex("1A1008"), 0f);
        // 옥개석 (roof cap) — 3 layers
        MkBox(go, V(0,1.18f,0), V(0.52f,0.10f,0.52f), granD, SM_POLISHED_GRAN);
        MkBox(go, V(0,1.30f,0), V(0.44f,0.10f,0.44f), gran,  SM_POLISHED_GRAN);
        MkBox(go, V(0,1.42f,0), V(0.36f,0.10f,0.36f), granD, SM_POLISHED_GRAN);
        // 상륜부 (finial)
        MkCyl(go, V(0,1.58f,0), 0.08f, 0.22f, gran, SM_POLISHED_GRAN);
        FlatSph(go, V(0,1.72f,0), 0.08f, granD, SM_POLISHED_GRAN);
    }

    static void CreateLantern(Transform parent, Vector3 pos)
    {
        var go = Mk("Lantern", parent);
        go.transform.position = pos;
        Color dark  = Hex("2A1E14");
        Color paper = Hex("F0E0B0");
        Color light = Hex("FFD060");
        Color rope  = Hex("C8A040");

        // Pole
        MkBox(go, V(0,1.5f,0), V(0.06f,3.0f,0.06f), dark, SM_DARK_WOOD);
        // Horizontal arm
        MkBox(go, V(0,3.0f,0), V(0.8f,0.06f,0.06f), dark, SM_DARK_WOOD);
        // Rope from arm
        MkBox(go, V(0.35f,2.7f,0), V(0.02f,0.6f,0.02f), rope, SM_DARK_WOOD);
        // Lantern body
        MkBox(go, V(0.35f,2.32f,0), V(0.28f,0.38f,0.28f), paper, SM_PAPER);
        MkBox(go, V(0.35f,2.32f,0), V(0.20f,0.30f,0.20f), light, SM_PAPER);
        // Lantern ends
        MkBox(go, V(0.35f,2.54f,0), V(0.30f,0.06f,0.30f), dark, SM_DARK_WOOD);
        MkBox(go, V(0.35f,2.10f,0), V(0.30f,0.06f,0.30f), dark, SM_DARK_WOOD);
        // Tassel
        for (int ts = 0; ts < 3; ts++)
            MkBox(go, V(0.35f+ts*0.04f-0.04f, 1.98f, 0), V(0.02f,0.22f,0.02f), Hex("C03020"), SM_DARK_WOOD);
    }

    static void GardenRock(Transform parent, Vector3 pos, float s, Color c, Color cd)
    {
        var go = Mk("GardenRock", parent);
        go.transform.position = pos;
        FlatSph(go, V(0,     0,    0),    s,       c,  SM_ROUGH_STONE);
        FlatSph(go, V(s*0.4f,s*0.3f, s*0.2f), s*0.65f, cd, SM_ROUGH_STONE);
        FlatSph(go, V(-s*0.3f,s*0.1f,-s*0.2f),s*0.50f, c*(0.9f), SM_ROUGH_STONE);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  ENVIRONMENT — SHARED
    // ═════════════════════════════════════════════════════════════════════════

    static void SetupSkybox()
    {
        var mat = new Material(Shader.Find("Skybox/Procedural"));
        mat.SetFloat("_SunSize",         0.03f);
        mat.SetFloat("_SunSizeConvergence", 8f);
        mat.SetFloat("_AtmosphereThickness", 1.15f);
        mat.SetColor("_SkyTint",   Hex("B8C8D8"));
        mat.SetColor("_GroundColor",Hex("A09080"));
        mat.SetFloat("_Exposure",   1.35f);
        RenderSettings.skybox = mat;
        RenderSettings.fogColor   = Hex("C8D4DC");
        RenderSettings.fog        = true;
        RenderSettings.fogMode    = FogMode.Linear;
        RenderSettings.fogStartDistance = 28f;
        RenderSettings.fogEndDistance   = 85f;
        DynamicGI.UpdateEnvironment();
    }

    static void SetupLighting()
    {
        // Remove auto-generated directional lights
        foreach (var dl in Object.FindObjectsOfType<Light>())
            if (dl.name.StartsWith("_Hanok") || dl.name == "Directional Light")
                Object.DestroyImmediate(dl.gameObject);

        // Main sun (afternoon, golden hour)
        var sun = new GameObject("_HanokSun").AddComponent<Light>();
        sun.type      = LightType.Directional;
        sun.color     = Hex("FFE8C0");
        sun.intensity = 1.45f;
        sun.shadows   = LightShadows.Soft;
        sun.shadowStrength = 0.75f;
        sun.shadowBias     = 0.015f;
        sun.shadowNormalBias = 0.4f;
        QualitySettings.shadowDistance = 120f; // covers full scene
        sun.transform.eulerAngles = V(42f, -38f, 0f);

        // Sky fill (blue-ish, soft, from above)
        var sky = new GameObject("_HanokSky").AddComponent<Light>();
        sky.type      = LightType.Directional;
        sky.color     = Hex("C8DCF0");
        sky.intensity = 0.32f;
        sky.shadows   = LightShadows.None;
        sky.transform.eulerAngles = V(75f, 140f, 0f);

        // Ground bounce (warm, from below)
        var bounce = new GameObject("_HanokBounce").AddComponent<Light>();
        bounce.type      = LightType.Directional;
        bounce.color     = Hex("F0D8A0");
        bounce.intensity = 0.18f;
        bounce.shadows   = LightShadows.None;
        bounce.transform.eulerAngles = V(-75f, 0f, 0f);

        // Ambient trilight
        RenderSettings.ambientMode    = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor     = Hex("A8C0D8");
        RenderSettings.ambientEquatorColor = Hex("D8C8A8");
        RenderSettings.ambientGroundColor  = Hex("584C38");
    }

    static void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.backgroundColor = Hex("B8CAD6");
        cam.clearFlags      = CameraClearFlags.Skybox;
        cam.nearClipPlane   = 0.15f;
        cam.farClipPlane    = 350f;
        cam.fieldOfView     = 58f;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  SHOJI (lattice panel) builder — uses MkBox only, no LineRenderer
    // ═════════════════════════════════════════════════════════════════════════

    static void BuildShojiGrid(Transform parent, Vector3 center, float w, float h, int cols, int rows)
    {
        var go = Mk("Shoji", parent);
        go.transform.position = center;
        Color paper = Hex("EDE0CC");
        Color dark  = Hex("2A1E14");

        // Paper fill
        MkBox(go, V(0,0,0), V(w, h, 0.02f), paper, SM_PAPER);
        // Vertical battens
        float cellW = w / cols;
        for (int c2 = 0; c2 <= cols; c2++)
            MkBox(go, V(-w*0.5f+c2*cellW, 0, 0.012f), V(0.018f,h,0.018f), dark, SM_DARK_WOOD);
        // Horizontal rails
        float cellH = h / rows;
        for (int r = 0; r <= rows; r++)
            MkBox(go, V(0,-h*0.5f+r*cellH,0.012f), V(w,0.018f,0.018f), dark, SM_DARK_WOOD);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  PRIMITIVE HELPERS
    // ═════════════════════════════════════════════════════════════════════════

    static GameObject MkBox(GameObject parent, Vector3 lpos, Vector3 size, Color c, float smooth)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = lpos;
        go.transform.localScale    = size;
        Rend(go, c, smooth);
        return go;
    }

    // Overload accepting Transform parent
    static GameObject MkBox(Transform parent, Vector3 lpos, Vector3 size, Color c, float smooth)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.SetParent(parent);
        go.transform.localPosition = lpos;
        go.transform.localScale    = size;
        Rend(go, c, smooth);
        return go;
    }

    static void MkCyl(GameObject parent, Vector3 lpos, float radius, float height, Color c, float smooth,
                       bool horizontal = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = lpos;
        go.transform.localScale    = V(radius*2, height*0.5f, radius*2);
        if (horizontal) go.transform.localEulerAngles = V(0,0,90f);
        Rend(go, c, smooth);
    }

    static void MkCyl(Transform parent, Vector3 lpos, float radius, float height, Color c, float smooth)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.transform.SetParent(parent);
        go.transform.localPosition = lpos;
        go.transform.localScale    = V(radius*2, height*0.5f, radius*2);
        Rend(go, c, smooth);
    }

    static void FlatSph(GameObject parent, Vector3 lpos, float r, Color c, float smooth)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = lpos;
        go.transform.localScale    = V(r*2, r*1.35f, r*2);
        Rend(go, c, smooth);
    }

    static void FlatSph(Transform parent, Vector3 lpos, float r, Color c, float smooth)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.transform.SetParent(parent);
        go.transform.localPosition = lpos;
        go.transform.localScale    = V(r*2, r*1.35f, r*2);
        Rend(go, c, smooth);
    }

    static void Rend(GameObject go, Color c, float smooth)
    {
        var mr = go.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", c);
        mat.SetFloat("_Smoothness", smooth);
        mat.SetFloat("_Metallic",   0f);
        mr.sharedMaterial = mat;
        mr.shadowCastingMode  = smooth < 0.01f ? ShadowCastingMode.Off : ShadowCastingMode.On;
        mr.receiveShadows     = true;
        if (go.TryGetComponent<Collider>(out var col)) Object.DestroyImmediate(col);
    }

    static GameObject Mk(string name, Transform parent = null)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent);
        return go;
    }

    static GameObject Mk(string name, GameObject parent)
    {
        return Mk(name, parent.transform);
    }

    static void DestroyNamed(params string[] names)
    {
        foreach (string n in names)
        {
            var go = GameObject.Find(n);
            if (go != null) Object.DestroyImmediate(go);
        }
    }

    static Color Hex(string h)
    {
        ColorUtility.TryParseHtmlString("#" + h, out Color c);
        return c;
    }

    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
}
