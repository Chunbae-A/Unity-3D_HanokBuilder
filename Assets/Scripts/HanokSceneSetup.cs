using UnityEngine;
using UnityEngine.Rendering;

public static class HanokSceneSetup
{
    static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out Color c); return c; }
    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

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

    static void CreateOuterGround()
    {
        if (GameObject.Find("_HanokOuterGround") != null) return;
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        go.name = "_HanokOuterGround";
        go.transform.position   = V(0f, -0.01f, 0f);
        go.transform.localScale = V(300f, 1f, 300f);
        Rend(go, Hex("#6B5F4E"), 0f, ShadowCastingMode.Off);
        Object.Destroy(go.GetComponent<Collider>());
    }

    // ── 기단 (3단 구성) ─────────────────────────────────────────
    static void CreateStonePlatform()
    {
        if (GameObject.Find("_HanokPlatform") != null) return;
        var root = new GameObject("_HanokPlatform");

        MkBox(root.transform, Hex("#908880"), V(0f, -0.30f, 0f), V(23.5f, 0.18f, 23.5f), ShadowCastingMode.Off);
        MkBox(root.transform, Hex("#888078"), V(0f, -0.16f, 0f), V(22.8f, 0.20f, 22.8f), ShadowCastingMode.Off);
        MkBox(root.transform, Hex("#7E786E"), V(0f, -0.02f, 0f), V(22.0f, 0.24f, 22.0f), ShadowCastingMode.Off);
        MkBox(root.transform, Hex("#A09890"), V(0f,  0.10f, 0f), V(22.0f, 0.04f, 22.0f), ShadowCastingMode.Off);

        Color corner = Hex("#5E5850");
        float d = 10.8f;
        foreach (var cp in new[]{ V(-d,0.10f,d), V(d,0.10f,d), V(-d,0.10f,-d), V(d,0.10f,-d) })
        {
            MkBox(root.transform, corner, cp, V(1.3f, 0.14f, 1.3f), ShadowCastingMode.Off);
            MkBox(root.transform, Hex("#4E4840"), cp + V(0,0.08f,0), V(0.70f, 0.06f, 0.70f), ShadowCastingMode.Off);
        }

        Color[] stepC = { Hex("#706A62"), Hex("#686258"), Hex("#504C48") };
        for (int i = -9; i <= 9; i++)
        {
            float xi = i * 1.15f;
            for (int s = 0; s < 3; s++)
            {
                float fw  = 1.08f - s * 0.08f;
                float fh  = 0.06f;
                float fzo = 10.75f + s * 0.12f;
                float fy  = 0.04f - s * 0.06f;
                MkBox(root.transform, stepC[s], V(xi, fy,  fzo), V(fw, fh, 0.30f - s*0.04f), ShadowCastingMode.Off);
                MkBox(root.transform, stepC[s], V(xi, fy, -fzo), V(fw, fh, 0.30f - s*0.04f), ShadowCastingMode.Off);
                MkBox(root.transform, stepC[s], V( fzo, fy, xi), V(0.30f - s*0.04f, fh, fw), ShadowCastingMode.Off);
                MkBox(root.transform, stepC[s], V(-fzo, fy, xi), V(0.30f - s*0.04f, fh, fw), ShadowCastingMode.Off);
            }
        }
    }

    static void CreateFloor()
    {
        if (GameObject.Find("_HanokFloor") != null) return;
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "_HanokFloor";
        floor.transform.localScale = V(20f, 1f, 20f);
        Rend(floor, Hex("#B8B0A2"), 0.06f);
        BuildFloorTiles(Hex("#6A6258"), 1.20f, 0.90f, false);
    }

    static void BuildFloorTiles(Color grout, float tileW, float tileD, bool planksOnly)
    {
        if (GameObject.Find("_HanokGrid") != null) return;
        var root = new GameObject("_HanokGrid");
        const float HALF = 10f, GH = 0.014f, GW = 0.045f;

        for (float x = -HALF; x <= HALF + 0.01f; x += tileW)
            MkBox(root.transform, grout, V(x, GH*.5f, 0f), V(GW, GH, HALF*2f), ShadowCastingMode.Off);
        if (!planksOnly)
            for (float z = -HALF; z <= HALF + 0.01f; z += tileD)
                MkBox(root.transform, grout, V(0f, GH*.5f, z), V(HALF*2f, GH, GW), ShadowCastingMode.Off);

        Color frame = new Color(grout.r*.65f, grout.g*.65f, grout.b*.65f);
        const float BW = 0.28f, BH = 0.022f;
        MkBox(root.transform, frame, V(0f, BH*.5f,  HALF), V(HALF*2f+BW, BH, BW), ShadowCastingMode.Off);
        MkBox(root.transform, frame, V(0f, BH*.5f, -HALF), V(HALF*2f+BW, BH, BW), ShadowCastingMode.Off);
        MkBox(root.transform, frame, V( HALF, BH*.5f, 0f), V(BW, BH, HALF*2f+BW), ShadowCastingMode.Off);
        MkBox(root.transform, frame, V(-HALF, BH*.5f, 0f), V(BW, BH, HALF*2f+BW), ShadowCastingMode.Off);

        Color inner = new Color(grout.r*.80f, grout.g*.80f, grout.b*.80f);
        const float IW = 0.055f, IH = 0.010f, IOFF = 0.55f;
        MkBox(root.transform, inner, V(0f, IH*.5f,  HALF-IOFF), V(HALF*2f-IOFF*2f, IH, IW), ShadowCastingMode.Off);
        MkBox(root.transform, inner, V(0f, IH*.5f, -HALF+IOFF), V(HALF*2f-IOFF*2f, IH, IW), ShadowCastingMode.Off);
        MkBox(root.transform, inner, V( HALF-IOFF, IH*.5f, 0f), V(IW, IH, HALF*2f-IOFF*2f), ShadowCastingMode.Off);
        MkBox(root.transform, inner, V(-HALF+IOFF, IH*.5f, 0f), V(IW, IH, HALF*2f-IOFF*2f), ShadowCastingMode.Off);
    }

    // ── 석등 (8파트 정교한 구조) ─────────────────────────────────
    static void StoneLantern(Transform parent, Vector3 pos)
    {
        Color granite = Hex("#8A8278"); Color dark = Hex("#6A6258"); Color darkest = Hex("#4E4840");
        // 지대석
        MkBox(parent, darkest,  pos + V(0, 0.05f, 0), V(0.95f, 0.10f, 0.95f));
        // 하대석
        MkBox(parent, dark,     pos + V(0, 0.20f, 0), V(0.78f, 0.22f, 0.78f));
        MkBox(parent, granite,  pos + V(0, 0.33f, 0), V(0.88f, 0.06f, 0.88f));
        // 간주석 (2단)
        MkCyl(parent, granite,  pos + V(0, 0.82f, 0), V(0.20f, 0.88f, 0.20f));
        MkCyl(parent, dark,     pos + V(0, 1.28f, 0), V(0.25f, 0.06f, 0.25f));
        MkCyl(parent, granite,  pos + V(0, 1.58f, 0), V(0.17f, 0.52f, 0.17f));
        // 상대석
        MkBox(parent, dark,     pos + V(0, 1.88f, 0), V(0.68f, 0.08f, 0.68f));
        // 화사석 (빛 몸체)
        MkBox(parent, granite,  pos + V(0, 2.14f, 0), V(0.54f, 0.46f, 0.54f));
        // 창 (4방향)
        Color windowC = new Color(0.95f, 0.86f, 0.68f, 0.55f);
        MkBox(parent, windowC, pos + V(0,    2.14f,  0.27f), V(0.22f, 0.28f, 0.02f), ShadowCastingMode.Off);
        MkBox(parent, windowC, pos + V(0,    2.14f, -0.27f), V(0.22f, 0.28f, 0.02f), ShadowCastingMode.Off);
        MkBox(parent, windowC, pos + V( 0.27f, 2.14f, 0),   V(0.02f, 0.28f, 0.22f), ShadowCastingMode.Off);
        MkBox(parent, windowC, pos + V(-0.27f, 2.14f, 0),   V(0.02f, 0.28f, 0.22f), ShadowCastingMode.Off);
        // 옥개석 (3단)
        MkBox(parent, darkest,  pos + V(0, 2.44f, 0), V(1.00f, 0.10f, 1.00f));
        MkBox(parent, dark,     pos + V(0, 2.60f, 0), V(0.72f, 0.22f, 0.72f));
        MkBox(parent, darkest,  pos + V(0, 2.78f, 0), V(0.44f, 0.08f, 0.44f));
        // 상륜부
        MkCyl(parent, granite,  pos + V(0, 2.92f, 0), V(0.12f, 0.26f, 0.12f));
        var top = Mk(PrimitiveType.Sphere, parent);
        top.transform.position   = pos + V(0, 3.06f, 0);
        top.transform.localScale = V(0.18f, 0.18f, 0.18f);
        Rend(top, darkest, 0.15f, ShadowCastingMode.Off);
    }

    // ── 마당 석물 ─────────────────────────────────────────────
    static void CreateCourtyard()
    {
        if (GameObject.Find("_HanokCourtyard") != null) return;
        var root = new GameObject("_HanokCourtyard");

        StoneLantern(root.transform, V(-3.8f, 0f,  9.0f));
        StoneLantern(root.transform, V( 3.8f, 0f,  9.0f));

        // 물확 (석조 물그릇)
        Color wc = Hex("#7A7268");
        MkBox(root.transform, wc, V(0f, 0.18f, -7f), V(1.4f, 0.36f, 1.0f));
        MkBox(root.transform, Hex("#2A5080"), V(0f, 0.35f, -7f), V(1.0f, 0.04f, 0.70f), ShadowCastingMode.Off);

        // 장독대 (항아리 군락)
        Color jarC = Hex("#2A2016"); Color jarLid = Hex("#1C1608");
        float[] jarX = { 7.5f, 8.4f, 9.0f, 7.8f, 8.6f };
        float[] jarZ = { -3.0f, -3.5f, -2.2f, -1.5f, -1.0f };
        float[] jarS = { 0.55f, 0.70f, 0.45f, 0.60f, 0.38f };
        for (int k = 0; k < jarX.Length; k++)
        {
            var jar = Mk(PrimitiveType.Sphere, root.transform);
            jar.transform.position   = V(jarX[k], jarS[k]*0.5f, jarZ[k]);
            jar.transform.localScale = V(jarS[k], jarS[k]*1.10f, jarS[k]);
            Rend(jar, jarC, 0.12f, ShadowCastingMode.Off);
            var lid = Mk(PrimitiveType.Sphere, root.transform);
            lid.transform.position   = V(jarX[k], jarS[k]*0.95f, jarZ[k]);
            lid.transform.localScale = V(jarS[k]*0.60f, jarS[k]*0.22f, jarS[k]*0.60f);
            Rend(lid, jarLid, 0.05f, ShadowCastingMode.Off);
        }
        MkBox(root.transform, Hex("#7E7668"), V(8.4f, 0.04f, -2.3f), V(3.5f, 0.08f, 3.0f), ShadowCastingMode.Off);

        // 디딤돌
        Color sc = Hex("#8A8070");
        float[] stX = { 0f, 0.12f, -0.1f, 0.08f };
        for (int i = 0; i < 4; i++)
        {
            float scl = 0.90f + (i%2)*0.15f;
            MkBox(root.transform, sc, V(stX[i], 0.025f, -12.0f + i*2.0f), V(1.2f*scl, 0.06f, 0.85f*scl), ShadowCastingMode.Off);
        }

        // 괴석
        GardenRock(root.transform, V(-7.5f, 0f,  5.5f), 0.9f, Hex("#686058"), Hex("#4A4438"));
        GardenRock(root.transform, V(-8.0f, 0f, -2.0f), 0.7f, Hex("#686058"), Hex("#4A4438"));
        GardenRock(root.transform, V( 7.0f, 0f,  3.0f), 0.8f, Hex("#787060"), Hex("#4A4438"));
    }

    static void GardenRock(Transform p, Vector3 pos, float s, Color c, Color cd)
    {
        var g1 = Mk(PrimitiveType.Sphere, p);
        g1.transform.position   = pos + V(0, s*0.4f, 0);
        g1.transform.localScale = V(s*1.2f, s*0.8f, s*1.0f);
        Rend(g1, c, 0.05f, ShadowCastingMode.Off);
        var g2 = Mk(PrimitiveType.Sphere, p);
        g2.transform.position   = pos + V(s*0.3f, s*0.25f, 0);
        g2.transform.localScale = V(s*0.7f, s*0.55f, s*0.6f);
        Rend(g2, cd, 0.05f, ShadowCastingMode.Off);
    }

    // ── 돌담 + 솟을대문 (5간 구성) ──────────────────────────────
    static void CreateBoundaryWalls()
    {
        if (GameObject.Find("_HanokWalls") != null) return;
        var root = new GameObject("_HanokWalls");
        Color wC = Hex("#5A5048"); Color capC = Hex("#342C1A");
        const float D = 13.2f, H = 1.95f, T = 0.58f;

        WallSeg(root.transform, wC, capC, V(0f,  0f,  D),   D*2f, H, T, 0f);
        WallSeg(root.transform, wC, capC, V(-D,  0f,  0f),  D*2f, H, T, 90f);
        WallSeg(root.transform, wC, capC, V( D,  0f,  0f),  D*2f, H, T, 90f);
        WallSeg(root.transform, wC, capC, V(-9.4f, 0f, -D), 7.6f, H, T, 0f);
        WallSeg(root.transform, wC, capC, V( 9.4f, 0f, -D), 7.6f, H, T, 0f);

        Color postC = Hex("#3A2A14"); Color roofC = Hex("#1C1408"); Color roofDk = Hex("#120E06");
        // 기단
        MkBox(root.transform, Hex("#706860"), V(0f, 0.15f, -D), V(12.5f, 0.30f, 1.2f), ShadowCastingMode.Off);
        // 기둥 5개
        foreach (float gx in new[]{ -5.0f, -2.5f, 0f, 2.5f, 5.0f })
            MkCyl(root.transform, postC, V(gx, 2.0f, -D), V(0.45f, 4.0f, 0.45f), ShadowCastingMode.Off);
        // 중앙 창방
        MkBox(root.transform, roofC, V(0f, 4.5f, -D), V(6.5f, 0.30f, 1.1f), ShadowCastingMode.Off);
        // 지붕 3단
        MkBox(root.transform, roofC,  V(0f, 5.10f, -D), V(7.8f, 0.18f, 1.6f), ShadowCastingMode.Off);
        MkBox(root.transform, roofDk, V(0f, 5.50f, -D), V(5.8f, 0.50f, 1.2f), ShadowCastingMode.Off);
        MkBox(root.transform, roofDk, V(0f, 6.10f, -D), V(3.5f, 0.65f, 0.9f), ShadowCastingMode.Off);
        MkBox(root.transform, Hex("#080806"), V(0f, 6.55f, -D), V(2.2f, 0.28f, 0.75f), ShadowCastingMode.Off);
        // 좌우 행랑 지붕
        MkBox(root.transform, Hex("#241C10"), V(-7.4f, 3.60f, -D), V(5.4f, 0.16f, 1.30f), ShadowCastingMode.Off);
        MkBox(root.transform, Hex("#1A1208"), V(-7.4f, 3.95f, -D), V(3.8f, 0.42f, 0.95f), ShadowCastingMode.Off);
        MkBox(root.transform, Hex("#241C10"), V( 7.4f, 3.60f, -D), V(5.4f, 0.16f, 1.30f), ShadowCastingMode.Off);
        MkBox(root.transform, Hex("#1A1208"), V( 7.4f, 3.95f, -D), V(3.8f, 0.42f, 0.95f), ShadowCastingMode.Off);
        // 문짝
        Color doorC = Hex("#281E0C");
        MkBox(root.transform, doorC, V(-1.1f, 2.0f, -D), V(2.0f, 3.4f, 0.12f), ShadowCastingMode.Off);
        MkBox(root.transform, doorC, V( 1.1f, 2.0f, -D), V(2.0f, 3.4f, 0.12f), ShadowCastingMode.Off);
        // 창살 (5줄)
        for (int dc = 0; dc < 5; dc++)
        {
            float gy = 2.0f + (dc-2)*0.58f;
            MkBox(root.transform, Hex("#3A2A10"), V(-1.1f, gy, -D+0.07f), V(2.0f, 0.04f, 0.02f), ShadowCastingMode.Off);
            MkBox(root.transform, Hex("#3A2A10"), V( 1.1f, gy, -D+0.07f), V(2.0f, 0.04f, 0.02f), ShadowCastingMode.Off);
        }
    }

    static void WallSeg(Transform p, Color wC, Color capC,
                        Vector3 center, float len, float h, float t, float rotY)
    {
        var body = Mk(PrimitiveType.Cube, p);
        body.transform.position    = center + Vector3.up * h * 0.5f;
        body.transform.localScale  = V(len, h, t);
        body.transform.eulerAngles = V(0, rotY, 0);
        Rend(body, wC, 0f, ShadowCastingMode.Off);

        Color mortar = new Color(wC.r*.78f, wC.g*.78f, wC.b*.78f);
        for (int row = 1; row <= 5; row++)
        {
            float y = center.y + h * row / 6f;
            var line = Mk(PrimitiveType.Cube, p);
            line.transform.position    = V(center.x, y, center.z);
            line.transform.localScale  = V(len+0.02f, 0.03f, t+0.02f);
            line.transform.eulerAngles = V(0, rotY, 0);
            Rend(line, mortar, 0f, ShadowCastingMode.Off);
        }

        var cap1 = Mk(PrimitiveType.Cube, p);
        cap1.transform.position    = center + Vector3.up * h;
        cap1.transform.localScale  = V(len+0.50f, 0.12f, t+0.50f);
        cap1.transform.eulerAngles = V(0, rotY, 0);
        Rend(cap1, capC, 0f, ShadowCastingMode.Off);

        var cap2 = Mk(PrimitiveType.Cube, p);
        cap2.transform.position    = center + Vector3.up * (h + 0.15f);
        cap2.transform.localScale  = V(len+0.28f, 0.16f, t+0.28f);
        cap2.transform.eulerAngles = V(0, rotY, 0);
        Rend(cap2, new Color(capC.r*.70f, capC.g*.70f, capC.b*.70f), 0f, ShadowCastingMode.Off);
    }

    // ── 소나무 (8단 수형 + 불규칙 오프셋) ───────────────────────
    static void Pine(Transform parent, Vector3 basePos, float h, int variant)
    {
        Color trunk = Hex("#3A2610"); Color trunkRed = Hex("#5A3820");
        for (int s = 0; s < 4; s++)
        {
            float sw = h * 0.070f * (1f - s*0.18f);
            float sh = h * 0.16f;
            MkCyl(parent, s >= 2 ? trunkRed : trunk,
                  basePos + V(0, h*0.06f + h*0.16f*s + sh*0.5f, 0), V(sw, sh, sw), ShadowCastingMode.Off);
        }
        Color[][] palettes = {
            new[]{ Hex("#172E0A"),Hex("#1A3C0E"),Hex("#214E14"),Hex("#1C4418"),Hex("#28561E"),Hex("#224A16"),Hex("#183808"),Hex("#122C06") },
            new[]{ Hex("#123008"),Hex("#153608"),Hex("#1C4610"),Hex("#1A4014"),Hex("#234E1A"),Hex("#1E4412"),Hex("#143006"),Hex("#0E2804") },
            new[]{ Hex("#1A3A0E"),Hex("#1E4210"),Hex("#265416"),Hex("#20481A"),Hex("#2A5A20"),Hex("#245016"),Hex("#1A3C0C"),Hex("#143008") },
        };
        Color[] pal = palettes[variant % 3];
        float[] heights = { 0.36f,0.48f,0.59f,0.69f,0.78f,0.86f,0.92f,0.97f };
        float[] scalesX = { 0.72f,0.60f,0.49f,0.38f,0.28f,0.20f,0.13f,0.07f };
        float[] scalesY = { 0.10f,0.11f,0.12f,0.12f,0.11f,0.10f,0.09f,0.08f };
        for (int i = 0; i < 8; i++)
        {
            float ox = (i%2==0 ? 0.04f : -0.04f) * h * (i*0.1f);
            float oz = (i%3==0 ? 0.03f : -0.03f) * h * (i*0.08f);
            FlatSph(parent, pal[i], basePos + V(ox, h*heights[i], oz),
                    V(h*scalesX[i], h*scalesY[i], h*scalesX[i]));
        }
    }

    static void FlatSph(Transform p, Color c, Vector3 pos, Vector3 s)
    {
        var go = Mk(PrimitiveType.Sphere, p);
        go.transform.position = pos; go.transform.localScale = s;
        Rend(go, c, 0f, ShadowCastingMode.Off);
    }

    // ── 활엽수 (5겹 수관) ─────────────────────────────────────
    static void BroadTree(Transform parent, Vector3 basePos, float h, int variant)
    {
        Color trunk = Hex("#3A2610");
        MkCyl(parent, trunk, basePos + V(0,h*0.24f,0), V(h*0.08f, h*0.48f, h*0.08f), ShadowCastingMode.Off);
        MkCyl(parent, Hex("#4A3018"), basePos + V(0,h*0.56f,0), V(h*0.055f, h*0.20f, h*0.055f), ShadowCastingMode.Off);
        Color[][] palettes = {
            new Color[]{ Hex("#1E5410"),Hex("#287018"),Hex("#1C6012"),Hex("#22580E"),Hex("#2C7820") },
            new Color[]{ Hex("#185210"),Hex("#226618"),Hex("#1A5A10"),Hex("#1C540C"),Hex("#207018") },
            new Color[]{ Hex("#225818"),Hex("#2C7420"),Hex("#206214"),Hex("#266018"),Hex("#30801A") },
        };
        Color[] pal = palettes[variant % 3];
        FlatSph(parent, pal[0], basePos+V( 0.00f,  h*0.80f,  0.00f), V(h*0.76f, h*0.58f, h*0.72f));
        FlatSph(parent, pal[1], basePos+V( h*0.22f,h*0.72f,  h*0.10f), V(h*0.56f, h*0.46f, h*0.52f));
        FlatSph(parent, pal[2], basePos+V(-h*0.20f,h*0.74f, -h*0.08f), V(h*0.52f, h*0.43f, h*0.48f));
        FlatSph(parent, pal[3], basePos+V( h*0.06f,h*0.92f,  h*0.12f), V(h*0.42f, h*0.34f, h*0.38f));
        FlatSph(parent, pal[4], basePos+V(-h*0.10f,h*0.86f, -h*0.15f), V(h*0.38f, h*0.30f, h*0.34f));
    }

    static void CreateBroadGroves()
    {
        if (GameObject.Find("_HanokPines") != null) return;
        var root = new GameObject("_HanokPines");
        BroadTree(root.transform, V(-20f,0f,20f), 6.5f, 0); BroadTree(root.transform, V(-27f,0f,30f), 8.0f, 1);
        BroadTree(root.transform, V(-18f,0f,40f), 5.5f, 2); BroadTree(root.transform, V(-33f,0f,22f), 7.5f, 0);
        BroadTree(root.transform, V(-24f,0f,14f), 6.0f, 1); BroadTree(root.transform, V( 22f,0f,34f), 5.5f, 2);
        BroadTree(root.transform, V( 29f,0f,42f), 7.0f, 0);
        Pine(root.transform, V(-46f,0f,56f), 9.0f, 0); Pine(root.transform, V( 43f,0f,59f),10.0f, 1);
        Pine(root.transform, V(-16f,0f,62f), 8.0f, 2); Pine(root.transform, V( 21f,0f,66f), 9.5f, 0);
        Pine(root.transform, V(-36f,0f,72f),11.0f, 1); Pine(root.transform, V( 52f,0f,46f),10.0f, 2);
    }

    // ── 원경 한옥 (5기둥 + 내림마루) ───────────────────────────
    static void CreateDistantBuildings()
    {
        if (GameObject.Find("_HanokBgBuildings") != null) return;
        var root = new GameObject("_HanokBgBuildings");
        Hanok(root.transform, V(0f,   0f,  88f), 16f,5.0f,10f, Hex("#3C3220"),Hex("#22180C"));
        Hanok(root.transform, V(-40f, 0f,  72f), 11f,3.8f, 7f, Hex("#382C1A"),Hex("#201408"));
        Hanok(root.transform, V( 40f, 0f,  72f), 11f,3.8f, 7f, Hex("#382C1A"),Hex("#201408"));
        Pavilion(root.transform, V(-8f, 0f, 112f), Hex("#2E2412"));
        Pavilion(root.transform, V( 8f, 0f, 112f), Hex("#2E2412"));
        Hanok(root.transform, V(-68f, 0f,  58f),  9f,4.2f, 6f, Hex("#2E2212"),Hex("#1C1006"));
        Hanok(root.transform, V( 68f, 0f,  58f),  9f,4.2f, 6f, Hex("#2E2212"),Hex("#1C1006"));
    }

    static void Hanok(Transform p, Vector3 pos, float w, float h, float d, Color body, Color roof)
    {
        MkBox(p, Hex("#6A6254"), pos+V(0,.10f,0), V(w+1.8f,.20f,d+1.8f), ShadowCastingMode.Off);
        MkBox(p, Hex("#8A8274"), pos+V(0,.24f,0), V(w+1.2f,.10f,d+1.2f), ShadowCastingMode.Off);
        Color postC = Hex("#3A2E18");
        foreach (float xi in new[]{ -0.46f,-0.23f,0f,0.23f,0.46f })
        {
            MkCyl(p, postC, pos+V(w*xi, h*.50f,-d*0.46f), V(.22f,h,.22f), ShadowCastingMode.Off);
            MkCyl(p, postC, pos+V(w*xi, h*.50f, d*0.46f), V(.22f,h,.22f), ShadowCastingMode.Off);
        }
        MkBox(p, body, pos+V(0,h*.5f,0), V(w,h,d), ShadowCastingMode.Off);
        MkBox(p, postC, pos+V(0,h+.10f,-d*.5f), V(w*1.05f,.18f,.35f), ShadowCastingMode.Off);
        MkBox(p, postC, pos+V(0,h+.10f, d*.5f), V(w*1.05f,.18f,.35f), ShadowCastingMode.Off);
        MkBox(p, postC, pos+V(0,h+.30f,-d*.5f), V(w*1.08f,.12f,.45f), ShadowCastingMode.Off);
        MkBox(p, postC, pos+V(0,h+.30f, d*.5f), V(w*1.08f,.12f,.45f), ShadowCastingMode.Off);
        MkBox(p, roof, pos+V(0,h+.50f,0), V(w*1.50f,h*.10f,d*1.50f), ShadowCastingMode.Off);
        MkBox(p, new Color(roof.r*.85f,roof.g*.85f,roof.b*.85f),
              pos+V(0,h+.65f,0), V(w*1.35f,h*.08f,d*1.35f), ShadowCastingMode.Off);
        Color ridge = new Color(roof.r*.55f,roof.g*.55f,roof.b*.55f);
        MkBox(p, ridge, pos+V(0,h+.65f+h*.10f,0), V(w*.50f,h*.28f,d*.32f), ShadowCastingMode.Off);
        MkBox(p, ridge, pos+V( w*.36f,h+.58f,0), V(h*.06f,h*.06f,d*.45f), ShadowCastingMode.Off);
        MkBox(p, ridge, pos+V(-w*.36f,h+.58f,0), V(h*.06f,h*.06f,d*.45f), ShadowCastingMode.Off);
    }

    static void Pavilion(Transform p, Vector3 pos, Color c)
    {
        float h = 3.2f; Color postC = Hex("#4A3820");
        foreach (var xi in new[]{-1.5f,1.5f})
            foreach (var zi in new[]{-1.5f,1.5f})
                MkCyl(p, postC, pos+V(xi,h*.5f,zi), V(.26f,h,.26f), ShadowCastingMode.Off);
        MkBox(p, c, pos+V(0,h+.65f,0), V(4.8f,.18f,4.8f), ShadowCastingMode.Off);
        MkBox(p, Hex("#181008"), pos+V(0,h+.95f,0), V(2.8f,h*.50f,2.8f), ShadowCastingMode.Off);
        MkBox(p, c, pos+V(0,h+h*.5f+1.1f,0), V(3.5f,.14f,3.5f), ShadowCastingMode.Off);
        MkBox(p, Hex("#0C0C06"), pos+V(0,h+h*.5f+1.35f,0), V(2.0f,h*.24f,2.0f), ShadowCastingMode.Off);
    }

    static void CreateMainHanok()
    {
        if (GameObject.Find("_HanokMainBuilding") != null) return;
        var root = new GameObject("_HanokMainBuilding");
        Hanok(root.transform, V(0f,0f,36f), 24f,5.8f,14f, Hex("#483822"),Hex("#1E1408"));
        Hanok(root.transform, V(-17f,0f,28f),12f,4.2f, 8f, Hex("#3C2E1A"),Hex("#181008"));
        Hanok(root.transform, V( 26f,0f,22f), 9f,3.6f, 7f, Hex("#3A2A18"),Hex("#161008"));
        Color wallC = Hex("#3C3020"); Color roofC = Hex("#181008");
        MkBox(root.transform, wallC, V(18f,1.20f,14f), V(0.55f,2.40f,17f), ShadowCastingMode.Off);
        MkBox(root.transform, roofC, V(18f,2.55f,14f), V(0.80f,0.22f,17.5f), ShadowCastingMode.Off);
    }

    static void CreateMountainSilhouettes()
    {
        if (GameObject.Find("_HanokMountains") != null) return;
        var root = new GameObject("_HanokMountains");
        (Color c, Vector3 pos, Vector3 s)[] mts = {
            (Hex("#5E6E7C"),V(-125f,-30f,385f),V(170f, 80f,130f)),
            (Hex("#527060"),V(  22f,-34f,425f),V(210f, 95f,160f)),
            (Hex("#6A7882"),V( 155f,-27f,368f),V(148f, 68f,118f)),
            (Hex("#58664A"),V( -62f,-22f,285f),V(128f, 60f, 96f)),
            (Hex("#647258"),V( 225f,-24f,308f),V(108f, 54f, 86f)),
            (Hex("#4E6070"),V( -20f,-40f,460f),V(180f,100f,140f)),
        };
        foreach (var (c,pos,s) in mts)
        {
            var go = Mk(PrimitiveType.Sphere, root.transform);
            go.transform.position = pos; go.transform.localScale = s;
            Rend(go, c, 0f, ShadowCastingMode.Off);
        }
    }

    static void SetupSkybox()
    {
        var sh = Shader.Find("Skybox/Procedural"); if (sh == null) return;
        var mat = new Material(sh);
        mat.SetFloat("_SunSize",             0.028f);
        mat.SetFloat("_SunSizeConvergence",  9f);
        mat.SetFloat("_AtmosphereThickness", 0.82f);
        mat.SetColor("_SkyTint",    Hex("#4E94C0"));
        mat.SetColor("_GroundColor", Hex("#54503A"));
        mat.SetFloat("_Exposure",   1.35f);
        RenderSettings.skybox = mat;
    }

    static void SetupLighting()
    {
        RenderSettings.ambientMode         = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor     = Hex("#A8C8E0") * 0.88f;
        RenderSettings.ambientEquatorColor = Hex("#D8C8A0") * 0.78f;
        RenderSettings.ambientGroundColor  = Hex("#786858") * 0.58f;
        RenderSettings.fog              = true;
        RenderSettings.fogMode          = FogMode.Linear;
        RenderSettings.fogStartDistance = 85f;
        RenderSettings.fogEndDistance   = 600f;
        RenderSettings.fogColor         = Hex("#B8CCD8");
        QualitySettings.shadowDistance  = 120f;
        QualitySettings.pixelLightCount = 4;

        var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l.type != LightType.Directional) continue;
            if (l.gameObject.name.StartsWith("_Hanok")) continue;
            l.color                 = Hex("#FFF8E4");
            l.intensity             = 1.45f;
            l.shadows               = LightShadows.Soft;
            l.shadowStrength        = 0.58f;
            l.shadowBias            = 0.025f;
            l.shadowNormalBias      = 0.25f;
            l.transform.eulerAngles = V(42f, -36f, 0f);
            l.cullingMask           = -1;
            break;
        }
        if (GameObject.Find("_HanokFillLight") == null)
        {
            var go = new GameObject("_HanokFillLight");
            var fill = go.AddComponent<Light>();
            fill.type = LightType.Directional; fill.color = Hex("#A8C4E0");
            fill.intensity = 0.48f; fill.shadows = LightShadows.None;
            fill.cullingMask = -1; fill.transform.eulerAngles = V(24f,148f,0f);
        }
        if (GameObject.Find("_HanokGroundBounce") == null)
        {
            var go = new GameObject("_HanokGroundBounce");
            var bounce = go.AddComponent<Light>();
            bounce.type = LightType.Directional; bounce.color = Hex("#D8C8A0");
            bounce.intensity = 0.22f; bounce.shadows = LightShadows.None;
            bounce.transform.eulerAngles = V(-75f,0f,0f);
        }
    }

    static void SetupCamera()
    {
        if (Camera.main == null) return;
        Camera.main.clearFlags      = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = Hex("#7AB0CC");
        Camera.main.fieldOfView     = 55f;
        Camera.main.nearClipPlane   = 0.05f;
        Camera.main.farClipPlane    = 2000f;
    }

    // ════════════════════════════════════════════════════════════
    // 배경 프리셋 전환
    // ════════════════════════════════════════════════════════════
    public static void SetPreset(int idx)
    {
        DestroyNamed("_HanokCourtyard"); DestroyNamed("_HanokWalls");
        DestroyNamed("_HanokPines");     DestroyNamed("_HanokBgBuildings");
        DestroyNamed("_HanokMainBuilding");

        switch (idx)
        {
            default:
            case 0: // 한옥 마당 — 청명한 여름 낮, 청판석
                CreateCourtyard(); CreateBoundaryWalls(); CreateBroadGroves();
                CreateDistantBuildings(); CreateMainHanok();
                UpdateFloorStyle(Hex("#C0B8A8"), Hex("#6A6258"), 1.20f, 0.90f);
                SetAtmosphere(Hex("#4E94C0"),Hex("#B0CCD8"),Hex("#54503A"),1.35f,Hex("#FFF8E4"),1.45f,V(42f,-36f,0f));
                break;
            case 1: // 사랑채 — 황금빛 오후, 우물마루
                CreateSarangchaePerimeter(); CreateLightTrees();
                UpdateFloorStyle(Hex("#9C6A34"), Hex("#5C3A18"), 0.28f, 20f, planksOnly:true);
                SetAtmosphere(Hex("#C8A860"),Hex("#E0CCA0"),Hex("#8C6C3A"),1.10f,Hex("#FFDE90"),1.20f,V(32f,-22f,0f));
                break;
            case 2: // 조선 장터 — 흐린 오전, 황토 박석
                CreateMarketStalls(); CreatePineGroves(); CreateDistantBuildings();
                UpdateFloorStyle(Hex("#B08C54"), Hex("#7A6030"), 1.45f, 1.10f);
                SetAtmosphere(Hex("#8EA8BE"),Hex("#B0C0CC"),Hex("#645840"),1.05f,Hex("#E8DCC8"),1.05f,V(52f,-42f,0f));
                break;
            case 3: // 전통 정원 — 초록 싱그러움, 이끼 박석
                CreateGardenElements(); CreateGardenTrees();
                UpdateFloorStyle(Hex("#7E9068"), Hex("#3E5C30"), 1.05f, 0.85f);
                SetAtmosphere(Hex("#72B090"),Hex("#9AC8A8"),Hex("#284820"),1.15f,Hex("#F0FFE8"),1.25f,V(36f,-28f,0f));
                break;
        }
    }

    static void DestroyNamed(string name)
    { var go = GameObject.Find(name); if (go != null) Object.Destroy(go); }

    static void UpdateFloorStyle(Color tile, Color grout, float tileW, float tileD, bool planksOnly = false)
    {
        var floor = GameObject.Find("_HanokFloor");
        if (floor != null)
        {
            var r = floor.GetComponent<Renderer>();
            if (r != null) { r.material.color = tile; if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", tile); }
        }
        var old = GameObject.Find("_HanokGrid"); if (old != null) Object.DestroyImmediate(old);
        BuildFloorTiles(grout, tileW, tileD, planksOnly);
    }

    static void SetAtmosphere(Color skyTint, Color fogColor, Color groundColor, float exposure,
                              Color sunColor, float sunIntensity, Vector3 sunAngle)
    {
        if (RenderSettings.skybox != null)
        {
            RenderSettings.skybox.SetColor("_SkyTint",    skyTint);
            RenderSettings.skybox.SetColor("_GroundColor", groundColor);
            RenderSettings.skybox.SetFloat("_Exposure",   exposure);
        }
        RenderSettings.fogColor = fogColor;
        if (Camera.main != null) Camera.main.backgroundColor = skyTint;
        var og = GameObject.Find("_HanokOuterGround");
        if (og != null) { var r = og.GetComponent<Renderer>(); if (r != null) { r.material.color = groundColor; if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", groundColor); } }
        var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l.type != LightType.Directional) continue;
            if (l.gameObject.name.StartsWith("_Hanok")) continue;
            l.color = sunColor; l.intensity = sunIntensity; l.transform.eulerAngles = sunAngle; break;
        }
    }

    // ══════════════════════════════════════════════════════════════
    // 프리셋 1 — 사랑채 (4×4 기둥, 공포, 창살 12×8, 우물마루, 매화)
    // ══════════════════════════════════════════════════════════════
    static void CreateSarangchaePerimeter()
    {
        var root = new GameObject("_HanokWalls");
        Color woodDk  = Hex("#261808"); Color woodMid = Hex("#3E2C0E");
        Color woodLt  = Hex("#6A4818"); Color paper   = Hex("#F4ECD6");
        const float D = 13f, CH = 3.8f;

        // 기단 2단
        MkBox(root.transform, Hex("#706A5C"), V(0f,0.10f,0f), V(D*2f+0.6f,0.20f,D*2f+0.6f), ShadowCastingMode.Off);
        MkBox(root.transform, Hex("#8A8272"), V(0f,0.22f,0f), V(D*2f+0.2f,0.06f,D*2f+0.2f), ShadowCastingMode.Off);

        // 기둥 4×4 격자 + 초석
        float[] colPos = { -D, -D*0.33f, D*0.33f, D };
        foreach (float x in colPos)
            foreach (float z in colPos)
            {
                MkBox(root.transform, Hex("#888070"), V(x,0.35f,z), V(0.55f,0.08f,0.55f), ShadowCastingMode.Off);
                MkCyl(root.transform, woodDk,          V(x,CH*.5f+0.38f,z), V(0.38f,CH,0.38f), ShadowCastingMode.Off);
                MkCyl(root.transform, woodMid,          V(x,0.60f,z), V(0.35f,0.40f,0.35f), ShadowCastingMode.Off);
            }

        // 창방 + 평방 4면
        foreach (float fz in new[]{ -D, D })
        {
            MkBox(root.transform, woodDk,  V(0f,CH+0.20f,fz), V(D*2f+0.5f,0.25f,0.40f));
            MkBox(root.transform, woodMid, V(0f,CH+0.48f,fz), V(D*2f+0.5f,0.15f,0.50f));
        }
        foreach (float fx in new[]{ -D, D })
        {
            MkBox(root.transform, woodDk,  V(fx,CH+0.20f,0f), V(0.40f,0.25f,D*2f+0.5f));
            MkBox(root.transform, woodMid, V(fx,CH+0.48f,0f), V(0.50f,0.15f,D*2f+0.5f));
        }

        // 공포 (포공포) 4면 × 3칸
        for (int k = -1; k <= 1; k++)
        {
            float kx = k*(D*0.66f), kz = k*(D*0.66f);
            MkBox(root.transform, woodDk,  V(kx, CH+0.60f, -D), V(1.0f,0.35f,0.70f));
            MkBox(root.transform, woodMid, V(kx, CH+0.82f, -D), V(1.4f,0.18f,0.55f));
            MkBox(root.transform, woodDk,  V(kx, CH+0.60f,  D), V(1.0f,0.35f,0.70f));
            MkBox(root.transform, woodMid, V(kx, CH+0.82f,  D), V(1.4f,0.18f,0.55f));
            MkBox(root.transform, woodDk,  V(-D, CH+0.60f, kz), V(0.70f,0.35f,1.0f));
            MkBox(root.transform, woodMid, V(-D, CH+0.82f, kz), V(0.55f,0.18f,1.4f));
            MkBox(root.transform, woodDk,  V( D, CH+0.60f, kz), V(0.70f,0.35f,1.0f));
            MkBox(root.transform, woodMid, V( D, CH+0.82f, kz), V(0.55f,0.18f,1.4f));
        }

        // 창호지 패널 4면
        float panW = D*2f;
        MkBox(root.transform, paper, V(0f,  CH*.5f+0.38f, -D), V(panW, CH, .055f), ShadowCastingMode.Off);
        MkBox(root.transform, paper, V(0f,  CH*.5f+0.38f,  D), V(panW, CH, .055f), ShadowCastingMode.Off);
        MkBox(root.transform, paper, V(-D,  CH*.5f+0.38f, 0f), V(.055f, CH, panW), ShadowCastingMode.Off);
        MkBox(root.transform, paper, V( D,  CH*.5f+0.38f, 0f), V(.055f, CH, panW), ShadowCastingMode.Off);

        // 창살 격자 12×8
        BuildShojiGrid(root.transform, V(0f,  CH*.5f+0.38f, -D+.05f), panW, CH, 12, 8);
        BuildShojiGrid(root.transform, V(0f,  CH*.5f+0.38f,  D-.05f), panW, CH, 12, 8);

        // 처마 4단 + 용마루
        MkBox(root.transform, Hex("#1C1206"), V(0f,CH+1.10f,0f), V(D*2.7f,0.18f,D*2.7f), ShadowCastingMode.Off);
        MkBox(root.transform, Hex("#161008"), V(0f,CH+1.40f,0f), V(D*2.2f,0.14f,D*2.2f), ShadowCastingMode.Off);
        MkBox(root.transform, Hex("#100C06"), V(0f,CH+1.65f,0f), V(D*1.6f,0.90f,D*1.1f), ShadowCastingMode.Off);
        MkBox(root.transform, Hex("#0A0806"), V(0f,CH+2.60f,0f), V(D*0.8f,0.35f,D*0.5f), ShadowCastingMode.Off);

        // 우물마루 (격자 마루판)
        MkBox(root.transform, woodMid, V(0f,.28f,0f), V(D*2f-.4f,0.06f,D*2f-.4f), ShadowCastingMode.Off);
        for (float mx = -D+0.5f; mx <= D-0.5f; mx += 0.32f)
            MkBox(root.transform, woodLt, V(mx,.31f,0f), V(0.02f,0.02f,D*2f-.5f), ShadowCastingMode.Off);

        // 석등
        StoneLantern(root.transform, V(-2.5f,0f,-D+3f));
        StoneLantern(root.transform, V( 2.5f,0f,-D+3f));

        // 매화나무
        PlumTree(root.transform, V(-D+2.5f,0f,-D+4f));
        PlumTree(root.transform, V( D-2.5f,0f, D-3f));
    }

    static void PlumTree(Transform parent, Vector3 basePos)
    {
        Color trunk = Hex("#2A1E0E"); Color branch = Hex("#3A2812");
        Color blossom = Hex("#F0C8D4"); Color center = Hex("#E8A0B0");
        float h = 4.5f;
        MkCyl(parent, trunk, basePos+V(0,h*0.28f,0), V(h*0.065f,h*0.55f,h*0.065f), ShadowCastingMode.Off);
        for (int b = 0; b < 6; b++)
        {
            float ang = b * Mathf.PI * 2f / 6f;
            float bx = Mathf.Cos(ang)*h*0.30f, bz = Mathf.Sin(ang)*h*0.28f;
            float by = h*(0.55f + b*0.04f);
            MkCyl(parent, branch, basePos+V(bx*.5f,by+h*0.10f,bz*.5f), V(h*0.025f,h*0.30f,h*0.025f), ShadowCastingMode.Off);
            for (int k = 0; k < 4; k++)
            {
                float fx = bx + Mathf.Cos(ang+k*0.7f)*h*0.10f;
                float fz = bz + Mathf.Sin(ang+k*0.7f)*h*0.08f;
                var fl = Mk(PrimitiveType.Sphere, parent);
                fl.transform.position   = basePos+V(fx, by+h*(0.22f+k*0.04f), fz);
                fl.transform.localScale = V(h*0.09f,h*0.07f,h*0.09f);
                Rend(fl, k==0 ? center : blossom, 0.05f, ShadowCastingMode.Off);
            }
        }
    }

    static void BuildShojiGrid(Transform parent, Vector3 center, float w, float h, int cols, int rows)
    {
        var sh  = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        var mat = new Material(sh) { color = Hex("#5A3E20") };
        float hw = w*.5f, hh = h*.5f;
        for (int i = 0; i <= cols; i++)
        {
            float x = center.x - hw + (w/cols)*i;
            Line(parent, mat, V(x, center.y-hh, center.z), V(x, center.y+hh, center.z), 0.022f);
        }
        for (int j = 0; j <= rows; j++)
        {
            float y = center.y - hh + (h/rows)*j;
            Line(parent, mat, V(center.x-hw, y, center.z), V(center.x+hw, y, center.z), 0.022f);
        }
    }

    static void Line(Transform p, Material m, Vector3 a, Vector3 b, float w)
    {
        var go = new GameObject("L"); go.transform.SetParent(p, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.positionCount = 2;
        lr.SetPosition(0, a); lr.SetPosition(1, b);
        lr.startWidth = lr.endWidth = w; lr.material = m;
        lr.shadowCastingMode = ShadowCastingMode.Off;
        lr.receiveShadows = false; lr.generateLightingData = false;
    }

    static void CreateLightTrees()
    {
        var root = new GameObject("_HanokPines");
        Pine(root.transform,V(-20f,0f,20f),5.5f,0); Pine(root.transform,V(-27f,0f,11f),6.5f,1);
        Pine(root.transform,V( 22f,0f,19f),6.0f,2); Pine(root.transform,V( 29f,0f,25f),7.5f,0);
        Pine(root.transform,V(-16f,0f,31f),5.0f,1); Pine(root.transform,V( 19f,0f,33f),5.5f,2);
    }

    // ══════════════════════════════════════════════════════════════
    // 프리셋 2 — 조선 장터 (5+3+3 가판대, 우물, 등롱, 포목, 항아리)
    // ══════════════════════════════════════════════════════════════
    static void CreateMarketStalls()
    {
        var root = new GameObject("_HanokWalls");
        Color wood = Hex("#3C2810"); Color woodLt = Hex("#5A3C18");
        Color[] fabrics = { Hex("#B86828"),Hex("#A04820"),Hex("#28502A"),Hex("#6A3818"),Hex("#8C5820"),Hex("#7A3A28") };

        // 후면 5개 가판대
        for (int i = -2; i <= 2; i++)
        {
            float x = i * 5.8f;
            Color fc = fabrics[(i+3) % fabrics.Length];
            MkCyl(root.transform, wood, V(x-2.0f,1.4f,11.5f), V(.18f,2.8f,.18f), ShadowCastingMode.Off);
            MkCyl(root.transform, wood, V(x+2.0f,1.4f,11.5f), V(.18f,2.8f,.18f), ShadowCastingMode.Off);
            MkCyl(root.transform, wood, V(x-2.0f,1.4f, 9.5f), V(.18f,2.8f,.18f), ShadowCastingMode.Off);
            MkCyl(root.transform, wood, V(x+2.0f,1.4f, 9.5f), V(.18f,2.8f,.18f), ShadowCastingMode.Off);
            MkBox(root.transform, woodLt, V(x,.62f,10.5f), V(4.6f,0.14f,2.2f));
            MkBox(root.transform, wood, V(x,.36f,10.5f), V(4.6f,0.38f,0.14f), ShadowCastingMode.Off);
            MkBox(root.transform, fc, V(x,2.80f,10.5f), V(5.2f,0.10f,3.2f), ShadowCastingMode.Off);
            MkBox(root.transform, new Color(fc.r*.85f,fc.g*.85f,fc.b*.85f), V(x,2.60f,9.0f), V(5.2f,0.50f,0.08f), ShadowCastingMode.Off);
            for (int k = -1; k <= 1; k++)
            {
                var jar = Mk(PrimitiveType.Sphere, root.transform);
                jar.transform.position   = V(x+k*1.4f,.90f,10.5f);
                jar.transform.localScale = V(.42f,.55f,.42f);
                Rend(jar, fabrics[(k+3)%fabrics.Length], 0.10f, ShadowCastingMode.Off);
            }
            for (int k = -2; k <= 2; k++)
                MkBox(root.transform, wood, V(x+k*0.9f,2.40f,10.5f), V(0.04f,0.60f,0.04f), ShadowCastingMode.Off);
        }

        // 좌측 3개 가판대
        for (int j = 0; j < 3; j++)
        {
            float z = -5f + j*6.5f; Color fc = fabrics[(j+2)%fabrics.Length];
            MkCyl(root.transform,wood,V(-12.5f,1.4f,z-1.8f),V(.18f,2.8f,.18f),ShadowCastingMode.Off);
            MkCyl(root.transform,wood,V(-12.5f,1.4f,z+1.8f),V(.18f,2.8f,.18f),ShadowCastingMode.Off);
            MkCyl(root.transform,wood,V(-10.5f,1.4f,z-1.8f),V(.18f,2.8f,.18f),ShadowCastingMode.Off);
            MkCyl(root.transform,wood,V(-10.5f,1.4f,z+1.8f),V(.18f,2.8f,.18f),ShadowCastingMode.Off);
            MkBox(root.transform,woodLt,V(-11.5f,.62f,z),V(2.2f,0.14f,3.8f));
            MkBox(root.transform,fc,V(-11.0f,2.80f,z),V(3.2f,0.10f,4.5f),ShadowCastingMode.Off);
        }

        // 우측 3개 가판대
        for (int j = 0; j < 3; j++)
        {
            float z = -5f + j*6.5f; Color fc = fabrics[(j+1)%fabrics.Length];
            MkCyl(root.transform,wood,V(12.5f,1.4f,z-1.8f),V(.18f,2.8f,.18f),ShadowCastingMode.Off);
            MkCyl(root.transform,wood,V(12.5f,1.4f,z+1.8f),V(.18f,2.8f,.18f),ShadowCastingMode.Off);
            MkCyl(root.transform,wood,V(10.5f,1.4f,z-1.8f),V(.18f,2.8f,.18f),ShadowCastingMode.Off);
            MkCyl(root.transform,wood,V(10.5f,1.4f,z+1.8f),V(.18f,2.8f,.18f),ShadowCastingMode.Off);
            MkBox(root.transform,woodLt,V(11.5f,.62f,z),V(2.2f,0.14f,3.8f));
            MkBox(root.transform,fc,V(11.0f,2.80f,z),V(3.2f,0.10f,4.5f),ShadowCastingMode.Off);
        }

        // 우물 (중앙)
        Color wsc = Hex("#706858");
        MkCyl(root.transform,wsc,V(0f,0.40f,-5f),V(2.0f,0.80f,2.0f));
        MkCyl(root.transform,Hex("#2A5080"),V(0f,0.80f,-5f),V(1.4f,0.02f,1.4f),ShadowCastingMode.Off);
        MkBox(root.transform,wood,V(0f,1.80f,-5f),V(2.6f,0.12f,0.20f),ShadowCastingMode.Off);
        MkCyl(root.transform,wood,V(-1.2f,2.20f,-5f),V(0.12f,0.80f,0.12f),ShadowCastingMode.Off);
        MkCyl(root.transform,wood,V( 1.2f,2.20f,-5f),V(0.12f,0.80f,0.12f),ShadowCastingMode.Off);
        MkBox(root.transform,wood,V(0f,2.62f,-5f),V(2.8f,0.12f,0.16f),ShadowCastingMode.Off);

        // 등롱 (종이등, 입구 양측)
        CreateLantern(root.transform, V(-4.5f,3.0f,-11.5f));
        CreateLantern(root.transform, V( 4.5f,3.0f,-11.5f));

        // 입구 낮은 담
        Color clay = Hex("#6E5838");
        MkBox(root.transform,clay,V(-8.5f,.55f,-12.5f),V(9f,1.1f,.48f),ShadowCastingMode.Off);
        MkBox(root.transform,clay,V( 8.5f,.55f,-12.5f),V(9f,1.1f,.48f),ShadowCastingMode.Off);

        // 항아리 줄 (후면)
        Color[] jarC = { Hex("#2A1C10"),Hex("#3A2818"),Hex("#1E1408") };
        float[] jarS = { 0.65f,0.80f,0.50f,0.70f,0.60f,0.45f,0.75f };
        for (int k = 0; k < 7; k++)
        {
            var jar = Mk(PrimitiveType.Sphere, root.transform);
            jar.transform.position   = V(-6.5f+k*2.0f, jarS[k]*0.52f, -11.5f);
            jar.transform.localScale = V(jarS[k], jarS[k]*1.15f, jarS[k]);
            Rend(jar, jarC[k%3], 0.12f, ShadowCastingMode.Off);
        }

        // 포목 천 걸이
        Color[] clothC = { Hex("#B83028"),Hex("#285898"),Hex("#28784A"),Hex("#9A6820") };
        for (int c = 0; c < 4; c++)
        {
            float cx = -6f + c*3.8f;
            MkBox(root.transform, clothC[c], V(cx,1.4f,12.5f), V(2.8f,0.80f,0.08f), ShadowCastingMode.Off);
            MkBox(root.transform, new Color(clothC[c].r*.75f,clothC[c].g*.75f,clothC[c].b*.75f),
                  V(cx,2.2f,12.5f), V(2.6f,0.50f,0.06f), ShadowCastingMode.Off);
        }
    }

    static void CreateLantern(Transform parent, Vector3 pos)
    {
        Color frame = Hex("#2A1E08"); Color lpaper = Hex("#F0C850");
        MkCyl(parent, frame, pos, V(0.08f,3.5f,0.08f), ShadowCastingMode.Off);
        var body = Mk(PrimitiveType.Sphere, parent);
        body.transform.position   = pos;
        body.transform.localScale = V(0.55f,0.70f,0.55f);
        Rend(body, lpaper, 0.25f, ShadowCastingMode.Off);
        MkBox(parent, frame, pos+V(0,-0.36f,0), V(0.58f,0.05f,0.58f), ShadowCastingMode.Off);
        MkBox(parent, frame, pos+V(0, 0.36f,0), V(0.58f,0.05f,0.58f), ShadowCastingMode.Off);
    }

    static void CreatePineGroves()
    {
        if (GameObject.Find("_HanokPines") != null) return;
        var root = new GameObject("_HanokPines");
        Pine(root.transform,V(-22f,0f,18f),5.5f,0); Pine(root.transform,V(-27f,0f,26f),7.0f,1);
        Pine(root.transform,V(-20f,0f,33f),5.0f,2); Pine(root.transform,V(-34f,0f,21f),8.0f,0);
        Pine(root.transform,V(-25f,0f,11f),6.0f,1); Pine(root.transform,V(-31f,0f,36f),5.5f,2);
        Pine(root.transform,V( 22f,0f,18f),5.5f,1); Pine(root.transform,V( 27f,0f,28f),6.5f,0);
        Pine(root.transform,V( 20f,0f,34f),5.0f,2); Pine(root.transform,V( 32f,0f,17f),8.0f,1);
        Pine(root.transform,V( 26f,0f, 9f),6.0f,0);
        Pine(root.transform,V(-46f,0f,56f), 9.0f,0); Pine(root.transform,V( 43f,0f,59f),10.0f,1);
        Pine(root.transform,V(-16f,0f,62f), 8.0f,2); Pine(root.transform,V( 21f,0f,66f), 9.5f,0);
        Pine(root.transform,V(-36f,0f,72f),11.0f,1); Pine(root.transform,V( 52f,0f,46f),10.0f,2);
    }

    // ══════════════════════════════════════════════════════════════
    // 프리셋 3 — 전통 정원 (3중 연못, 연꽃, 6각정자, 대나무, 단풍)
    // ══════════════════════════════════════════════════════════════
    static void CreateGardenElements()
    {
        var root = new GameObject("_HanokWalls");
        Color stone = Hex("#787870"); Color wood = Hex("#3C2E18");
        Color water = Hex("#2A5A8C"); Color waterSh = Hex("#3870A4");
        Color mossy = Hex("#4A5C38");

        // 연못 3중 레이어
        var pondBase = Mk(PrimitiveType.Sphere, root.transform);
        pondBase.transform.position   = V(8f,-.16f,7.5f);
        pondBase.transform.localScale = V(9.5f,0.12f,6.8f);
        Rend(pondBase, Hex("#182C48"), 0.90f, ShadowCastingMode.Off);

        var pondOuter = Mk(PrimitiveType.Sphere, root.transform);
        pondOuter.transform.position   = V(8f,-.06f,7.5f);
        pondOuter.transform.localScale = V(8.5f,0.06f,6.0f);
        Rend(pondOuter, water, 0.88f, ShadowCastingMode.Off);

        var pondInner = Mk(PrimitiveType.Sphere, root.transform);
        pondInner.transform.position   = V(8f,-.02f,7.5f);
        pondInner.transform.localScale = V(6.0f,0.04f,4.2f);
        Rend(pondInner, waterSh, 0.92f, ShadowCastingMode.Off);

        // 연꽃 6송이
        Color lotus = Hex("#F0B8C8"); Color lpad = Hex("#2C5A2A");
        for (int lf = 0; lf < 6; lf++)
        {
            float la = lf/6f*Mathf.PI*2f;
            float lx = 8f + Mathf.Cos(la)*2.5f*(0.4f+(lf%3)*0.2f);
            float lz = 7.5f + Mathf.Sin(la)*1.6f*(0.4f+(lf%2)*0.25f);
            var pad = Mk(PrimitiveType.Sphere, root.transform);
            pad.transform.position   = V(lx,0.02f,lz);
            pad.transform.localScale = V(0.65f,0.04f,0.65f);
            Rend(pad, lpad, 0.15f, ShadowCastingMode.Off);
            if (lf%2==0)
            {
                var fl = Mk(PrimitiveType.Sphere, root.transform);
                fl.transform.position   = V(lx,0.16f,lz);
                fl.transform.localScale = V(0.28f,0.30f,0.28f);
                Rend(fl, lotus, 0.15f, ShadowCastingMode.Off);
            }
        }

        // 연못 가장자리 돌 16개 (이끼 낀)
        for (int a = 0; a < 16; a++)
        {
            float rad = a/16f*Mathf.PI*2f;
            float s = 0.40f + (a%4)*0.08f;
            MkBox(root.transform, a%3==0 ? mossy : stone,
                  V(8f+Mathf.Cos(rad)*4.8f, 0.05f, 7.5f+Mathf.Sin(rad)*3.4f),
                  V(s,0.18f,s*0.8f), ShadowCastingMode.Off);
        }

        // 6각 정자
        float pz = 10.5f, pavH = 3.5f;
        for (int pp = 0; pp < 6; pp++)
        {
            float pAng = pp/6f*Mathf.PI*2f;
            float px = Mathf.Cos(pAng)*2.4f, pzp = pz+Mathf.Sin(pAng)*2.4f;
            MkCyl(root.transform, wood, V(px,pavH*.5f,pzp), V(.28f,pavH,.28f), ShadowCastingMode.Off);
            MkBox(root.transform, stone, V(px,0.05f,pzp), V(0.42f,0.10f,0.42f), ShadowCastingMode.Off);
        }
        MkBox(root.transform,Hex("#22180C"),V(0f,pavH+0.65f,pz),V(6.5f,0.20f,6.5f),ShadowCastingMode.Off);
        MkBox(root.transform,Hex("#1A1208"),V(0f,pavH+0.98f,pz),V(5.0f,0.16f,5.0f),ShadowCastingMode.Off);
        MkBox(root.transform,Hex("#14100A"),V(0f,pavH+1.28f,pz),V(3.8f,1.20f,3.8f),ShadowCastingMode.Off);
        MkBox(root.transform,Hex("#0C0A06"),V(0f,pavH+2.58f,pz),V(2.4f,0.80f,2.4f),ShadowCastingMode.Off);
        MkBox(root.transform,Hex("#100E08"),V(0f,pavH+3.45f,pz),V(1.5f,0.30f,1.5f),ShadowCastingMode.Off);
        // 마루
        MkBox(root.transform,Hex("#5C3E1A"),V(0f,0.22f,pz),V(4.6f,0.22f,4.6f));
        // 난간
        Color rail = Hex("#3C2A10");
        for (int rr = 0; rr < 6; rr++)
        {
            float rAng = rr/6f*Mathf.PI*2f+Mathf.PI/6f;
            MkCyl(root.transform, rail, V(Mathf.Cos(rAng)*2.1f,0.80f,pz+Mathf.Sin(rAng)*2.1f), V(0.08f,1.20f,0.08f), ShadowCastingMode.Off);
        }
        MkCyl(root.transform, rail, V(0f,1.42f,pz), V(4.4f,0.06f,4.4f), ShadowCastingMode.Off);

        // 석등
        StoneLantern(root.transform, V(-3.5f,0f,4.5f));
        StoneLantern(root.transform, V( 3.5f,0f,4.5f));

        // 디딤돌 9개 (불규칙)
        float[] rstX = { 0f,0.15f,-0.1f,0.08f,-0.05f,0.12f,-0.08f,0.05f,0f };
        for (int i = 0; i < 9; i++)
        {
            float scl = 0.85f + (i%3)*0.12f;
            MkBox(root.transform, i%3==0 ? mossy : stone,
                  V(rstX[i],0.025f,-11f+i*2.7f), V(1.3f*scl,0.06f,0.90f*scl), ShadowCastingMode.Off);
        }

        // 대나무 군락 (서측)
        CreateBambooGrove(root.transform, V(-10f,0f,8f));

        // 괴석 (동측)
        GardenRock(root.transform, V(14f,0f,-3f), 1.2f, Hex("#686058"), Hex("#585048"));
        GardenRock(root.transform, V(15f,0f, 2f), 0.9f, Hex("#585048"), Hex("#787060"));
        GardenRock(root.transform, V(13f,0f, 5f), 0.7f, Hex("#787060"), Hex("#686058"));

        // 담장
        Color wall = Hex("#5A5248");
        MkBox(root.transform,wall,V(0f, .85f,15f), V(32f,1.70f,.50f),ShadowCastingMode.Off);
        MkBox(root.transform,wall,V(-15f,.85f,0f), V(.50f,1.70f,30f),ShadowCastingMode.Off);
        MkBox(root.transform,wall,V( 15f,.85f,0f), V(.50f,1.70f,30f),ShadowCastingMode.Off);
        MkBox(root.transform,Hex("#28200E"),V(0f, 1.80f,15f), V(32.5f,.22f,.75f),ShadowCastingMode.Off);
        MkBox(root.transform,Hex("#1E1808"),V(0f, 2.05f,15f), V(31.0f,.14f,.60f),ShadowCastingMode.Off);
        MkBox(root.transform,Hex("#28200E"),V(-15f,1.80f,0f), V(.75f,.22f,30.5f),ShadowCastingMode.Off);
        MkBox(root.transform,Hex("#28200E"),V( 15f,1.80f,0f), V(.75f,.22f,30.5f),ShadowCastingMode.Off);

        // 이끼 패치 (담 근처)
        for (int mp = 0; mp < 8; mp++)
            MkBox(root.transform, Hex("#3A5030"), V(-12f+mp*3.5f,0.005f,14.2f), V(2.5f,0.01f,1.5f), ShadowCastingMode.Off);
    }

    static void CreateBambooGrove(Transform parent, Vector3 center)
    {
        Color bamboo = Hex("#4A7C28"); Color node = Hex("#3A6020");
        Color leaves = Hex("#386420"); Color leavesLt = Hex("#4A7830");
        float[] posX  = { 0f, 1.2f,-0.8f, 1.8f,-1.5f, 0.5f,-0.3f, 2.2f };
        float[] posZ  = { 0f, 0.5f, 1.0f,-0.3f, 0.8f, 1.5f,-1.0f, 1.2f };
        float[] hgts  = { 7.5f,6.2f,8.0f,5.8f,7.0f,6.5f,7.8f,5.5f };
        for (int b = 0; b < posX.Length; b++)
        {
            Vector3 bp = center + V(posX[b],0f,posZ[b]);
            float h = hgts[b];
            MkCyl(parent, bamboo, bp+V(0,h*.5f,0), V(0.09f,h,0.09f), ShadowCastingMode.Off);
            for (int n = 0; n < 7; n++)
                MkCyl(parent, node, bp+V(0,h*(0.15f+n*0.12f),0), V(0.12f,0.06f,0.12f), ShadowCastingMode.Off);
            for (int lv = 0; lv < 3; lv++)
            {
                float ly = h*(0.72f+lv*0.09f), ls = h*(0.22f-lv*0.04f);
                FlatSph(parent, lv%2==0?leaves:leavesLt, bp+V(ls*.3f,ly,ls*.2f), V(ls,ls*.22f,ls*.90f));
                FlatSph(parent, lv%2==0?leavesLt:leaves, bp+V(-ls*.25f,ly+h*.04f,-ls*.15f), V(ls*.85f,ls*.20f,ls*.80f));
            }
        }
    }

    static void CreateGardenTrees()
    {
        var root = new GameObject("_HanokPines");
        Pine(root.transform,V(-10f,0f,13f),5.0f,1); Pine(root.transform,V(-13f,0f, 4f),6.0f,2);
        Pine(root.transform,V(-17f,0f,19f),6.5f,0); Pine(root.transform,V( 15f,0f,13f),5.5f,1);
        Pine(root.transform,V( 19f,0f, 5f),4.5f,2); Pine(root.transform,V( -7f,0f,19f),5.5f,0);
        Pine(root.transform,V(  7f,0f,21f),5.0f,1); Pine(root.transform,V(-23f,0f,26f),7.5f,2);
        Pine(root.transform,V( 21f,0f,29f),8.5f,0); Pine(root.transform,V(-42f,0f,56f),9.5f,1);
        Pine(root.transform,V( 40f,0f,51f),8.5f,2); Pine(root.transform,V(-16f,0f,66f),8.0f,0);
        RedMapleTree(root.transform, V(10f,0f, 0f));
        RedMapleTree(root.transform, V(-5f,0f,-8f));
    }

    static void RedMapleTree(Transform parent, Vector3 basePos)
    {
        Color trunk = Hex("#2C1E0E");
        Color[] reds = { Hex("#B82010"),Hex("#CC2818"),Hex("#E03020"),Hex("#A81C0C") };
        float h = 5.5f;
        MkCyl(parent, trunk, basePos+V(0,h*.25f,0), V(h*.07f,h*.50f,h*.07f), ShadowCastingMode.Off);
        MkCyl(parent, trunk, basePos+V(0,h*.60f,0), V(h*.045f,h*.25f,h*.045f), ShadowCastingMode.Off);
        FlatSph(parent,reds[0],basePos+V( 0.00f,  h*.82f,  0.00f), V(h*.72f,h*.52f,h*.68f));
        FlatSph(parent,reds[1],basePos+V( h*.20f, h*.74f,  h*.10f), V(h*.52f,h*.44f,h*.48f));
        FlatSph(parent,reds[2],basePos+V(-h*.18f, h*.76f, -h*.08f), V(h*.48f,h*.41f,h*.44f));
        FlatSph(parent,reds[3],basePos+V( h*.06f, h*.94f,  h*.10f), V(h*.38f,h*.30f,h*.34f));
    }

    // ── 프리미티브 헬퍼 ──────────────────────────────────────────
    static GameObject Mk(PrimitiveType type, Transform parent)
    {
        var go = GameObject.CreatePrimitive(type);
        if (parent != null) go.transform.SetParent(parent, false);
        Object.Destroy(go.GetComponent<Collider>()); return go;
    }

    static GameObject MkBox(Transform p, Color c, Vector3 pos, Vector3 s,
                             ShadowCastingMode sh = ShadowCastingMode.On)
    {
        var go = Mk(PrimitiveType.Cube, p);
        go.transform.position = pos; go.transform.localScale = s;
        Rend(go, c, 0f, sh); return go;
    }

    static void MkCyl(Transform p, Color c, Vector3 pos, Vector3 s,
                       ShadowCastingMode sh = ShadowCastingMode.On)
    {
        var go = Mk(PrimitiveType.Cylinder, p);
        go.transform.position = pos; go.transform.localScale = s;
        Rend(go, c, 0f, sh);
    }

    static void Rend(GameObject go, Color c, float smooth,
                     ShadowCastingMode sh = ShadowCastingMode.On)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat    = new Material(shader) { color = c };
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smooth);
        if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic",   0f);
        if (mat.HasProperty("_BaseColor"))  mat.SetColor("_BaseColor",  c);
        var r = go.GetComponent<Renderer>();
        r.material          = mat;
        r.shadowCastingMode = sh;
        r.receiveShadows    = sh != ShadowCastingMode.Off;
    }
}
