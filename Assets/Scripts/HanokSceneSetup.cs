using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 한국 전통 야외 편집 환경 — 기단·마당·돌담·소나무·원경 한옥·산·하늘
/// </summary>
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

    // ── 외곽 자연 지면 ────────────────────────────────────────
    static void CreateOuterGround()
    {
        if (GameObject.Find("_HanokOuterGround") != null) return;
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        go.name = "_HanokOuterGround";
        go.transform.position  = V(0f, -0.01f, 0f);
        go.transform.localScale = V(300f, 1f, 300f);
        Rend(go, Hex("#6B5F4E"), 0f, ShadowCastingMode.Off);
        Object.Destroy(go.GetComponent<Collider>());
    }

    // ── 기단 ─────────────────────────────────────────────────
    static void CreateStonePlatform()
    {
        if (GameObject.Find("_HanokPlatform") != null) return;
        var root = new GameObject("_HanokPlatform");

        // 주 기단 (2단 구성 — 아래단이 약간 더 넓음)
        MkBox(root.transform, Hex("#888078"), V(0f, -0.22f, 0f), V(22.8f, 0.20f, 22.8f), ShadowCastingMode.Off); // 하단 받침
        MkBox(root.transform, Hex("#7E786E"), V(0f, -0.06f, 0f), V(22.0f, 0.24f, 22.0f), ShadowCastingMode.Off); // 상단 기단

        // 모서리 귀틀돌 (더 두껍게)
        Color corner = Hex("#625C54");
        float d = 10.7f;
        foreach (var p in new[]{ V(-d, 0.06f, d), V(d, 0.06f, d), V(-d, 0.06f,-d), V(d, 0.06f,-d) })
            MkBox(root.transform, corner, p, V(1.2f, 0.10f, 1.2f), ShadowCastingMode.Off);

        // 가장자리 마감돌 (계단형 — 약간 입체적)
        Color step1 = Hex("#706A62"), step2 = Hex("#585450");
        for (int i = -9; i <= 9; i++)
        {
            MkBox(root.transform, step1, V(i * 1.15f,  0.05f,  10.75f), V(1.08f, 0.08f, 0.35f), ShadowCastingMode.Off);
            MkBox(root.transform, step2, V(i * 1.15f, -0.01f,  10.90f), V(1.08f, 0.04f, 0.08f), ShadowCastingMode.Off);
            MkBox(root.transform, step1, V(i * 1.15f,  0.05f, -10.75f), V(1.08f, 0.08f, 0.35f), ShadowCastingMode.Off);
            MkBox(root.transform, step2, V(i * 1.15f, -0.01f, -10.90f), V(1.08f, 0.04f, 0.08f), ShadowCastingMode.Off);
            MkBox(root.transform, step1, V( 10.75f,   0.05f, i * 1.15f), V(0.35f, 0.08f, 1.08f), ShadowCastingMode.Off);
            MkBox(root.transform, step2, V( 10.90f,  -0.01f, i * 1.15f), V(0.08f, 0.04f, 1.08f), ShadowCastingMode.Off);
            MkBox(root.transform, step1, V(-10.75f,   0.05f, i * 1.15f), V(0.35f, 0.08f, 1.08f), ShadowCastingMode.Off);
            MkBox(root.transform, step2, V(-10.90f,  -0.01f, i * 1.15f), V(0.08f, 0.04f, 1.08f), ShadowCastingMode.Off);
        }
    }

    // ── 마당 바닥 ─────────────────────────────────────────────
    static void CreateFloor()
    {
        if (GameObject.Find("_HanokFloor") != null) return;
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "_HanokFloor";
        floor.transform.localScale = V(20f, 1f, 20f);
        Rend(floor, Hex("#B8B0A2"), 0.06f);
        BuildFloorTiles(Hex("#6A6258"), 1.20f, 0.90f, false);
    }

    // tileW/tileD: 타일 반복 간격(m). planksOnly=true 이면 세로줄눈만(판재용)
    static void BuildFloorTiles(Color grout, float tileW, float tileD, bool planksOnly)
    {
        if (GameObject.Find("_HanokGrid") != null) return;
        var root = new GameObject("_HanokGrid");

        const float HALF = 10f;
        const float GH   = 0.014f;   // 줄눈 높이 (입체감)
        const float GW   = 0.045f;   // 줄눈 폭

        // 세로 줄눈
        for (float x = -HALF; x <= HALF + 0.01f; x += tileW)
            MkBox(root.transform, grout,
                  V(x, GH * .5f, 0f), V(GW, GH, HALF * 2f), ShadowCastingMode.Off);

        // 가로 줄눈 (판재 모드에선 생략)
        if (!planksOnly)
            for (float z = -HALF; z <= HALF + 0.01f; z += tileD)
                MkBox(root.transform, grout,
                      V(0f, GH * .5f, z), V(HALF * 2f, GH, GW), ShadowCastingMode.Off);

        // 외곽 테두리 프레임 (더 진하고 넓게)
        Color frame = new Color(grout.r * 0.65f, grout.g * 0.65f, grout.b * 0.65f);
        const float BW = 0.28f, BH = 0.022f;
        MkBox(root.transform, frame, V(0f, BH*.5f,  HALF), V(HALF*2f+BW, BH, BW), ShadowCastingMode.Off);
        MkBox(root.transform, frame, V(0f, BH*.5f, -HALF), V(HALF*2f+BW, BH, BW), ShadowCastingMode.Off);
        MkBox(root.transform, frame, V( HALF, BH*.5f, 0f), V(BW, BH, HALF*2f+BW), ShadowCastingMode.Off);
        MkBox(root.transform, frame, V(-HALF, BH*.5f, 0f), V(BW, BH, HALF*2f+BW), ShadowCastingMode.Off);

        // 이중 내부 테두리 (장식선)
        Color inner = new Color(grout.r * 0.80f, grout.g * 0.80f, grout.b * 0.80f);
        const float IW = 0.055f, IH = 0.010f, IOFF = 0.55f;
        MkBox(root.transform, inner, V(0f, IH*.5f,  HALF-IOFF), V(HALF*2f-IOFF*2f, IH, IW), ShadowCastingMode.Off);
        MkBox(root.transform, inner, V(0f, IH*.5f, -HALF+IOFF), V(HALF*2f-IOFF*2f, IH, IW), ShadowCastingMode.Off);
        MkBox(root.transform, inner, V( HALF-IOFF, IH*.5f, 0f), V(IW, IH, HALF*2f-IOFF*2f), ShadowCastingMode.Off);
        MkBox(root.transform, inner, V(-HALF+IOFF, IH*.5f, 0f), V(IW, IH, HALF*2f-IOFF*2f), ShadowCastingMode.Off);
    }

    // ── 마당 석물 ─────────────────────────────────────────────
    static void CreateCourtyard()
    {
        if (GameObject.Find("_HanokCourtyard") != null) return;
        var root = new GameObject("_HanokCourtyard");
        StoneLantern(root.transform, V(-3.8f, 0f,  9.6f));
        StoneLantern(root.transform, V( 3.8f, 0f,  9.6f));
        // 디딤돌
        Color sc = Hex("#8A8070");
        for (int i = -1; i <= 1; i++)
            MkBox(root.transform, sc, V(i * 1.2f, 0.02f, -12.2f), V(1.1f, 0.07f, 0.75f), ShadowCastingMode.Off);
    }

    static void StoneLantern(Transform parent, Vector3 pos)
    {
        Color s = Hex("#8C8478"); Color d = Hex("#706860");
        MkBox(parent, s, pos + V(0, .18f, 0), V(0.72f, .36f, 0.72f));   // 받침
        MkCyl(parent, s, pos + V(0, .90f, 0), V(0.22f, .72f, 0.22f));   // 기둥
        MkBox(parent, s, pos + V(0, 1.55f, 0), V(0.55f, .38f, 0.55f));  // 몸체
        MkBox(parent, d, pos + V(0, 1.85f, 0), V(0.82f, .16f, 0.82f));  // 옥개석
        MkBox(parent, d, pos + V(0, 2.02f, 0), V(0.50f, .20f, 0.50f));  // 상층
        var top = Mk(PrimitiveType.Sphere, parent);
        top.transform.position = pos + V(0, 2.18f, 0);
        top.transform.localScale = V(0.22f, 0.22f, 0.22f);
        Rend(top, Hex("#60504A"), 0.05f, ShadowCastingMode.Off);
    }

    // ── 돌담 + 솟을대문 ──────────────────────────────────────
    static void CreateBoundaryWalls()
    {
        if (GameObject.Find("_HanokWalls") != null) return;
        var root = new GameObject("_HanokWalls");
        Color wC = Hex("#5E5448"); Color capC = Hex("#38301E");
        const float D = 13.2f; const float H = 1.95f; const float T = 0.58f;

        WallSeg(root.transform, wC, capC, V(0f, 0f, D),   D * 2f, H, T, 0f);
        WallSeg(root.transform, wC, capC, V(-D, 0f, 0f),  D * 2f, H, T, 90f);
        WallSeg(root.transform, wC, capC, V( D, 0f, 0f),  D * 2f, H, T, 90f);
        WallSeg(root.transform, wC, capC, V(-9.2f, 0f, -D), 8f,  H, T, 0f);
        WallSeg(root.transform, wC, capC, V( 9.2f, 0f, -D), 8f,  H, T, 0f);

        // 솟을대문
        Color postC = Hex("#3C2E18");
        MkBox(root.transform, postC, V(-5.2f, 1.8f, -D), V(0.7f, 3.6f, 0.7f));
        MkBox(root.transform, postC, V( 5.2f, 1.8f, -D), V(0.7f, 3.6f, 0.7f));
        MkBox(root.transform, postC, V(0f, 3.6f, -D), V(11.4f, 0.38f, 0.55f));
        MkBox(root.transform, Hex("#2C2410"), V(0f, 4.05f, -D), V(12.2f, 0.24f, 0.90f), ShadowCastingMode.Off);
        MkBox(root.transform, Hex("#1E180A"), V(0f, 4.40f, -D), V(8.5f, 0.65f, 0.55f), ShadowCastingMode.Off);
    }

    static void WallSeg(Transform p, Color wC, Color capC,
                        Vector3 center, float len, float h, float t, float rotY)
    {
        var body = Mk(PrimitiveType.Cube, p);
        body.transform.position    = center + Vector3.up * h * 0.5f;
        body.transform.localScale  = V(len, h, t);
        body.transform.eulerAngles = V(0, rotY, 0);
        Rend(body, wC, 0f, ShadowCastingMode.Off);

        // 벽돌 패턴 암시 (얇은 돌출 라인 3줄)
        Color mortar = new Color(wC.r * 0.75f, wC.g * 0.75f, wC.b * 0.75f);
        for (int row = 1; row <= 3; row++)
        {
            float y = center.y + h * row / 4f;
            var line = Mk(PrimitiveType.Cube, p);
            line.transform.position   = V(center.x, y, center.z);
            line.transform.localScale = V(len + 0.02f, 0.04f, t + 0.02f);
            line.transform.eulerAngles = V(0, rotY, 0);
            Rend(line, mortar, 0f, ShadowCastingMode.Off);
        }

        var cap = Mk(PrimitiveType.Cube, p);
        cap.transform.position    = center + Vector3.up * h;
        cap.transform.localScale  = V(len + 0.45f, 0.24f, t + 0.45f);
        cap.transform.eulerAngles = V(0, rotY, 0);
        Rend(cap, capC, 0f, ShadowCastingMode.Off);
    }

    // ── 소나무 군락 (6단 수형) ────────────────────────────────
    static void CreatePineGroves()
    {
        if (GameObject.Find("_HanokPines") != null) return;
        var root = new GameObject("_HanokPines");

        Pine(root.transform, V(-22f, 0f, 18f), 5.5f, 0);
        Pine(root.transform, V(-27f, 0f, 26f), 7.0f, 1);
        Pine(root.transform, V(-20f, 0f, 33f), 5.0f, 2);
        Pine(root.transform, V(-34f, 0f, 21f), 8.0f, 0);
        Pine(root.transform, V(-25f, 0f, 11f), 6.0f, 1);
        Pine(root.transform, V(-31f, 0f, 36f), 5.5f, 2);

        Pine(root.transform, V( 22f, 0f, 18f), 5.5f, 1);
        Pine(root.transform, V( 27f, 0f, 28f), 6.5f, 0);
        Pine(root.transform, V( 20f, 0f, 34f), 5.0f, 2);
        Pine(root.transform, V( 32f, 0f, 17f), 8.0f, 1);
        Pine(root.transform, V( 26f, 0f,  9f), 6.0f, 0);

        Pine(root.transform, V(-11f, 0f, 23f), 4.5f, 2);
        Pine(root.transform, V( 11f, 0f, 24f), 5.0f, 0);
        Pine(root.transform, V(  1f, 0f, 27f), 5.5f, 1);

        // 원경
        Pine(root.transform, V(-46f, 0f, 56f), 9.0f, 0);
        Pine(root.transform, V( 43f, 0f, 59f), 10f, 1);
        Pine(root.transform, V(-16f, 0f, 62f), 8.0f, 2);
        Pine(root.transform, V( 21f, 0f, 66f), 9.5f, 0);
        Pine(root.transform, V(-36f, 0f, 72f), 11f, 1);
        Pine(root.transform, V( 52f, 0f, 46f), 10f, 2);
    }

    // 6단 수형 소나무 — variant: 색상 변형 인덱스
    static void Pine(Transform parent, Vector3 basePos, float h, int variant)
    {
        // 줄기 3단 테이퍼
        Color trunk = Hex("#3A2610");
        for (int s = 0; s < 3; s++)
        {
            float sw = h * 0.075f * (1f - s * 0.22f);
            float sh = h * 0.18f;
            MkCyl(parent, trunk, basePos + V(0, h * 0.06f + h * 0.18f * s + sh * 0.5f, 0),
                  V(sw, sh, sw), ShadowCastingMode.Off);
        }

        // 6단 엽형
        Color[][] palettes = {
            new[]{ Hex("#1A3C0E"), Hex("#214E14"), Hex("#1C4418"), Hex("#28561E"), Hex("#224A16"), Hex("#183808") },
            new[]{ Hex("#153608"), Hex("#1C4610"), Hex("#1A4014"), Hex("#234E1A"), Hex("#1E4412"), Hex("#143006") },
            new[]{ Hex("#1E4210"), Hex("#265416"), Hex("#20481A"), Hex("#2A5A20"), Hex("#245016"), Hex("#1A3C0C") },
        };
        Color[] pal = palettes[variant % 3];
        float[] heights = { 0.42f, 0.55f, 0.67f, 0.77f, 0.87f, 0.94f };
        float[] scales  = { 0.68f, 0.56f, 0.44f, 0.33f, 0.22f, 0.13f };
        for (int i = 0; i < 6; i++)
            FlatSph(parent, pal[i], basePos + V(0, h * heights[i], 0),
                    V(h * scales[i], h * 0.14f, h * scales[i]));
    }

    static void FlatSph(Transform p, Color c, Vector3 pos, Vector3 s)
    {
        var go = Mk(PrimitiveType.Sphere, p);
        go.transform.position = pos; go.transform.localScale = s;
        Rend(go, c, 0f, ShadowCastingMode.Off);
    }

    // ── 원경 한옥 실루엣 ─────────────────────────────────────
    static void CreateDistantBuildings()
    {
        if (GameObject.Find("_HanokBgBuildings") != null) return;
        var root = new GameObject("_HanokBgBuildings");

        Hanok(root.transform, V(0f,   0f,  88f), 16f, 5.0f, 10f, Hex("#3C3220"), Hex("#22180C"));
        Hanok(root.transform, V(-40f, 0f,  72f), 11f, 3.8f,  7f, Hex("#382C1A"), Hex("#201408"));
        Hanok(root.transform, V( 40f, 0f,  72f), 11f, 3.8f,  7f, Hex("#382C1A"), Hex("#201408"));
        Pavilion(root.transform, V(-8f,  0f, 112f), Hex("#2E2412"));
        Pavilion(root.transform, V( 8f,  0f, 112f), Hex("#2E2412"));
        Hanok(root.transform, V(-68f, 0f,  58f), 9f,  4.2f,  6f, Hex("#2E2212"), Hex("#1C1006"));
        Hanok(root.transform, V( 68f, 0f,  58f), 9f,  4.2f,  6f, Hex("#2E2212"), Hex("#1C1006"));
    }

    static void Hanok(Transform p, Vector3 pos, float w, float h, float d, Color body, Color roof)
    {
        // 기단
        MkBox(p, Hex("#6E6456"), pos + V(0, .22f, 0), V(w + 1.2f, .44f, d + 1.2f), ShadowCastingMode.Off);
        // 기둥 (4 모서리 + 각 면 중간)
        Color postC = Hex("#3A2E18");
        float[] xs = { -0.42f, 0f, 0.42f };
        foreach (float xi in xs)
        {
            MkCyl(p, postC, pos + V(w * xi, h * .5f, -d * 0.48f), V(.25f, h, .25f), ShadowCastingMode.Off);
            MkCyl(p, postC, pos + V(w * xi, h * .5f,  d * 0.48f), V(.25f, h, .25f), ShadowCastingMode.Off);
        }
        // 본체
        MkBox(p, body, pos + V(0, h * .5f, 0), V(w, h, d), ShadowCastingMode.Off);
        // 도리
        MkBox(p, postC, pos + V(0, h + .18f, -d * .5f), V(w * 1.05f, .22f, .38f), ShadowCastingMode.Off);
        MkBox(p, postC, pos + V(0, h + .18f,  d * .5f), V(w * 1.05f, .22f, .38f), ShadowCastingMode.Off);
        // 처마
        MkBox(p, roof, pos + V(0, h + .38f, 0), V(w * 1.45f, h * .12f, d * 1.45f), ShadowCastingMode.Off);
        // 용마루
        MkBox(p, Hex("#181208"), pos + V(0, h + .55f + h * .12f, 0), V(w * .52f, h * .30f, d * .36f), ShadowCastingMode.Off);
    }

    static void Pavilion(Transform p, Vector3 pos, Color c)
    {
        float h = 3.2f;
        Color postC = Hex("#4A3820");
        foreach (var xi in new[] { -1.3f, 1.3f })
            foreach (var zi in new[] { -1.3f, 1.3f })
                MkCyl(p, postC, pos + V(xi, h * .5f, zi), V(.24f, h, .24f), ShadowCastingMode.Off);
        MkBox(p, c, pos + V(0, h + .55f, 0), V(4.4f, .16f, 4.4f), ShadowCastingMode.Off);
        MkBox(p, Hex("#181008"), pos + V(0, h + .82f, 0), V(2.5f, h * .52f, 2.5f), ShadowCastingMode.Off);
        MkBox(p, c, pos + V(0, h + h * .52f + 1.0f, 0), V(3.2f, .13f, 3.2f), ShadowCastingMode.Off);
        MkBox(p, Hex("#0C0C06"), pos + V(0, h + h * .52f + 1.25f, 0), V(1.8f, h * .25f, 1.8f), ShadowCastingMode.Off);
    }

    // ── 주 한옥 건물 + 동측 별채 (프리셋 0 전용) ─────────────
    static void CreateMainHanok()
    {
        if (GameObject.Find("_HanokMainBuilding") != null) return;
        var root = new GameObject("_HanokMainBuilding");

        // 정면 주 한옥 — 북쪽 담(z=13) 바로 뒤, 시선 중심
        Hanok(root.transform, V(0f, 0f, 36f), 24f, 5.8f, 14f,
              Hex("#483822"), Hex("#1E1408"));

        // 좌측 익랑 (연결 행랑채)
        Hanok(root.transform, V(-17f, 0f, 28f), 12f, 4.2f, 8f,
              Hex("#3C2E1A"), Hex("#181008"));

        // 동측 별채 — 동쪽 담(x=13) 바깥, 살짝 앞에
        Hanok(root.transform, V(26f, 0f, 22f), 9f, 3.6f, 7f,
              Hex("#3A2A18"), Hex("#161008"));

        // 별채 연결 행랑 (낮은 벽 형태)
        Color wallC = Hex("#3C3020"); Color roofC = Hex("#181008");
        MkBox(root.transform, wallC, V(18f, 1.20f, 14f), V(0.55f, 2.40f, 17f), ShadowCastingMode.Off);
        MkBox(root.transform, roofC, V(18f, 2.55f, 14f), V(0.80f, 0.22f, 17.5f), ShadowCastingMode.Off);
    }

    // ── 활엽수 (둥근 수관, 프리셋 0 전용) ────────────────────
    static void BroadTree(Transform parent, Vector3 basePos, float h, int variant)
    {
        Color trunk = Hex("#3A2610");
        // 줄기 (굵고 아래가 넓음)
        MkCyl(parent, trunk, basePos + V(0, h * 0.26f, 0),
              V(h * 0.075f, h * 0.52f, h * 0.075f), ShadowCastingMode.Off);
        MkCyl(parent, trunk, basePos + V(0, h * 0.58f, 0),
              V(h * 0.048f, h * 0.22f, h * 0.048f), ShadowCastingMode.Off);

        Color[][] palettes = {
            new Color[]{ Hex("#1E5410"), Hex("#287018"), Hex("#1C6012"), Hex("#22580E") },
            new Color[]{ Hex("#185210"), Hex("#226618"), Hex("#1A5A10"), Hex("#1C540C") },
            new Color[]{ Hex("#225818"), Hex("#2C7420"), Hex("#206214"), Hex("#266018") },
        };
        Color[] pal = palettes[variant % 3];

        // 수관 — 넓고 둥근 구체 4개 겹침
        FlatSph(parent, pal[0], basePos + V(0,        h * 0.80f,  0      ), V(h * 0.76f, h * 0.56f, h * 0.72f));
        FlatSph(parent, pal[1], basePos + V( h*0.22f, h * 0.72f,  h*0.10f), V(h * 0.56f, h * 0.46f, h * 0.52f));
        FlatSph(parent, pal[2], basePos + V(-h*0.20f, h * 0.74f, -h*0.08f), V(h * 0.52f, h * 0.43f, h * 0.48f));
        FlatSph(parent, pal[3], basePos + V( h*0.06f, h * 0.92f,  h*0.12f), V(h * 0.42f, h * 0.34f, h * 0.38f));
    }

    static void CreateBroadGroves()
    {
        if (GameObject.Find("_HanokPines") != null) return;
        var root = new GameObject("_HanokPines");

        // 좌측(서쪽) 활엽수 군락 — 참고 이미지 기준
        BroadTree(root.transform, V(-20f, 0f, 20f), 6.5f, 0);
        BroadTree(root.transform, V(-27f, 0f, 30f), 8.0f, 1);
        BroadTree(root.transform, V(-18f, 0f, 40f), 5.5f, 2);
        BroadTree(root.transform, V(-33f, 0f, 22f), 7.5f, 0);
        BroadTree(root.transform, V(-24f, 0f, 14f), 6.0f, 1);

        // 우측 소규모 활엽수
        BroadTree(root.transform, V(22f, 0f, 34f), 5.5f, 2);
        BroadTree(root.transform, V(29f, 0f, 42f), 7.0f, 0);

        // 원경 소나무 (깊이감)
        Pine(root.transform, V(-46f, 0f, 56f),  9.0f, 0);
        Pine(root.transform, V( 43f, 0f, 59f), 10.0f, 1);
        Pine(root.transform, V(-16f, 0f, 62f),  8.0f, 2);
        Pine(root.transform, V( 21f, 0f, 66f),  9.5f, 0);
        Pine(root.transform, V(-36f, 0f, 72f), 11.0f, 1);
        Pine(root.transform, V( 52f, 0f, 46f), 10.0f, 2);
    }

    // ── 원경 산 실루엣 ────────────────────────────────────────
    static void CreateMountainSilhouettes()
    {
        if (GameObject.Find("_HanokMountains") != null) return;
        var root = new GameObject("_HanokMountains");
        (Color c, Vector3 pos, Vector3 s)[] mts = {
            (Hex("#5E6E7C"), V(-125f, -30f, 385f), V(170f, 80f, 130f)),
            (Hex("#527060"), V(  22f, -34f, 425f), V(210f, 95f, 160f)),
            (Hex("#6A7882"), V( 155f, -27f, 368f), V(148f, 68f, 118f)),
            (Hex("#58664A"), V( -62f, -22f, 285f), V(128f, 60f,  96f)),
            (Hex("#647258"), V( 225f, -24f, 308f), V(108f, 54f,  86f)),
            (Hex("#4E6070"), V( -20f, -40f, 460f), V(180f,100f, 140f)),
        };
        foreach (var (c, pos, s) in mts)
        {
            var go = Mk(PrimitiveType.Sphere, root.transform);
            go.transform.position = pos; go.transform.localScale = s;
            Rend(go, c, 0f, ShadowCastingMode.Off);
        }
    }

    // ── 스카이박스 ────────────────────────────────────────────
    static void SetupSkybox()
    {
        var sh = Shader.Find("Skybox/Procedural");
        if (sh == null) return;
        var mat = new Material(sh);
        mat.SetFloat("_SunSize",             0.030f);
        mat.SetFloat("_SunSizeConvergence",  8f);
        mat.SetFloat("_AtmosphereThickness", 0.85f);
        mat.SetColor("_SkyTint",    Hex("#5A9EC4"));
        mat.SetColor("_GroundColor", Hex("#58503C"));
        mat.SetFloat("_Exposure",   1.30f);
        RenderSettings.skybox = mat;
    }

    // ── 조명 ──────────────────────────────────────────────────
    static void SetupLighting()
    {
        // 앰비언트 — 맑은 하늘 아래 한국 전통 분위기
        RenderSettings.ambientMode         = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor     = Hex("#A8C8E0") * 0.85f;
        RenderSettings.ambientEquatorColor = Hex("#D8C8A0") * 0.75f;
        RenderSettings.ambientGroundColor  = Hex("#786858") * 0.55f;

        // 안개 — 원경만 살짝
        RenderSettings.fog              = true;
        RenderSettings.fogMode          = FogMode.Linear;
        RenderSettings.fogStartDistance = 90f;
        RenderSettings.fogEndDistance   = 650f;
        RenderSettings.fogColor         = Hex("#B8CCD8");

        QualitySettings.shadowDistance  = 100f;
        QualitySettings.pixelLightCount = 4;

        // 메인 디렉셔널 라이트 — 맑고 따뜻한 햇살
        var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l.type != LightType.Directional) continue;
            if (l.gameObject.name.StartsWith("_Hanok")) continue;
            l.color                 = Hex("#FFF8E8");
            l.intensity             = 1.40f;
            l.shadows               = LightShadows.Soft;
            l.shadowStrength        = 0.55f;
            l.shadowBias            = 0.03f;
            l.shadowNormalBias      = 0.28f;
            l.transform.eulerAngles = V(40f, -38f, 0f);
            l.cullingMask           = -1;
            break;
        }

        // Fill light — 하늘 반사광 느낌
        if (GameObject.Find("_HanokFillLight") == null)
        {
            var go   = new GameObject("_HanokFillLight");
            var fill = go.AddComponent<Light>();
            fill.type              = LightType.Directional;
            fill.color             = Hex("#A8C4E0");
            fill.intensity         = 0.45f;
            fill.shadows           = LightShadows.None;
            fill.cullingMask       = -1;
            fill.transform.eulerAngles = V(24f, 148f, 0f);
        }
    }

    // ── 카메라 ────────────────────────────────────────────────
    static void SetupCamera()
    {
        if (Camera.main == null) return;
        bool hasSky = RenderSettings.skybox != null;
        Camera.main.clearFlags      = hasSky ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = Hex("#7AB0CC");
        Camera.main.fieldOfView     = 55f;
        Camera.main.nearClipPlane   = 0.05f;
        Camera.main.farClipPlane    = 2000f;
    }

    // ════════════════════════════════════════════════════════
    // 배경 프리셋 전환
    // ════════════════════════════════════════════════════════
    public static void SetPreset(int idx)
    {
        DestroyNamed("_HanokCourtyard");
        DestroyNamed("_HanokWalls");
        DestroyNamed("_HanokPines");
        DestroyNamed("_HanokBgBuildings");
        DestroyNamed("_HanokMainBuilding");

        switch (idx)
        {
            default:
            case 0: // 한옥 마당 — 청명한 낮, 청판석 회색 돌바닥
                CreateCourtyard();
                CreateBoundaryWalls();
                CreateBroadGroves();
                CreateDistantBuildings();
                CreateMainHanok();
                // 청판석: 연회색 돌판 + 진회색 줄눈 (1.2×0.9m)
                UpdateFloorStyle(Hex("#B8B2A4"), Hex("#68625A"), 1.20f, 0.90f);
                SetAtmosphere(Hex("#6AA4C8"), Hex("#B8D0E0"), Hex("#5A4E3C"), 1.30f,
                              Hex("#FFF8E8"), 1.35f, V(40f, -38f, 0f));
                break;
            case 1: // 사랑채 — 따뜻한 황금빛 오후, 우물마루 나무판
                CreateSarangchaePerimeter();
                CreateLightTrees();
                // 우물마루: 따뜻한 나무색 + 판재 결 (세로줄눈만)
                UpdateFloorStyle(Hex("#9C6A34"), Hex("#5C3A18"), 0.28f, 20f, planksOnly: true);
                SetAtmosphere(Hex("#C8A860"), Hex("#E0CCA0"), Hex("#8C6C3A"), 1.10f,
                              Hex("#FFDE90"), 1.20f, V(32f, -22f, 0f));
                break;
            case 2: // 조선 장터 — 흐린 하늘, 황토 다짐 + 박석
                CreateMarketStalls();
                CreatePineGroves();
                CreateDistantBuildings();
                // 박석: 황토 바탕 + 어두운 황토 줄눈 (넓은 타일)
                UpdateFloorStyle(Hex("#B08C54"), Hex("#7A6030"), 1.45f, 1.10f);
                SetAtmosphere(Hex("#8EA8BE"), Hex("#B0C0CC"), Hex("#645840"), 1.05f,
                              Hex("#E8DCC8"), 1.05f, V(52f, -42f, 0f));
                break;
            case 3: // 전통 정원 — 싱그러운 초록, 박석 이끼 바닥
                CreateGardenElements();
                CreateGardenTrees();
                // 박석: 이끼낀 돌색 + 풀 초록 줄눈 (약간 작은 타일)
                UpdateFloorStyle(Hex("#7E9068"), Hex("#3E5C30"), 1.05f, 0.85f);
                SetAtmosphere(Hex("#72B090"), Hex("#9AC8A8"), Hex("#284820"), 1.15f,
                              Hex("#F0FFE8"), 1.25f, V(36f, -28f, 0f));
                break;
        }
    }

    static void DestroyNamed(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) Object.Destroy(go);
    }

    static void UpdateFloorStyle(Color tile, Color grout, float tileW, float tileD, bool planksOnly = false)
    {
        var floor = GameObject.Find("_HanokFloor");
        if (floor != null)
        {
            var r = floor.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = tile;
                if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", tile);
            }
        }
        // DestroyImmediate 로 즉시 제거 후 재생성
        var old = GameObject.Find("_HanokGrid");
        if (old != null) Object.DestroyImmediate(old);
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

        // 외곽 자연 지면 색상도 프리셋에 맞춰 전환
        var og = GameObject.Find("_HanokOuterGround");
        if (og != null)
        {
            var r = og.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = groundColor;
                if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", groundColor);
            }
        }

        // 메인 라이트 색상·각도도 프리셋에 맞게 전환
        var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l.type != LightType.Directional) continue;
            if (l.gameObject.name.StartsWith("_Hanok")) continue;
            l.color             = sunColor;
            l.intensity         = sunIntensity;
            l.transform.eulerAngles = sunAngle;
            break;
        }
    }

    // ── 프리셋 1: 사랑채 ──────────────────────────────────────
    static void CreateSarangchaePerimeter()
    {
        var root = new GameObject("_HanokWalls");
        Color woodDark = Hex("#2E2008"); Color woodMid = Hex("#4A3010");
        Color paper    = Hex("#F0E6CA");

        const float D = 13f; const float CH = 3.4f;

        // 기둥
        float[] posArr = { -D, -D * 0.33f, D * 0.33f, D };
        foreach (float x in posArr)
        {
            MkCyl(root.transform, woodDark, V(x, CH * .5f, -D), V(.40f, CH, .40f), ShadowCastingMode.Off);
            MkCyl(root.transform, woodDark, V(x, CH * .5f,  D), V(.40f, CH, .40f), ShadowCastingMode.Off);
        }
        foreach (float z in posArr)
        {
            MkCyl(root.transform, woodDark, V(-D, CH * .5f, z), V(.40f, CH, .40f), ShadowCastingMode.Off);
            MkCyl(root.transform, woodDark, V( D, CH * .5f, z), V(.40f, CH, .40f), ShadowCastingMode.Off);
        }

        // 창호 패널
        float panW = D * 2f;
        Color paperTint = new Color(paper.r, paper.g, paper.b, 0.92f);
        MkBox(root.transform, paperTint, V(0f, CH*.5f, -D), V(panW, CH, .06f), ShadowCastingMode.Off);
        MkBox(root.transform, paperTint, V(0f, CH*.5f,  D), V(panW, CH, .06f), ShadowCastingMode.Off);
        MkBox(root.transform, paperTint, V(-D, CH*.5f, 0f), V(.06f, CH, panW), ShadowCastingMode.Off);
        MkBox(root.transform, paperTint, V( D, CH*.5f, 0f), V(.06f, CH, panW), ShadowCastingMode.Off);

        BuildShojiGrid(root.transform, V(0f, CH*.5f, -D+.05f), panW, CH, 9, 6);
        BuildShojiGrid(root.transform, V(0f, CH*.5f,  D-.05f), panW, CH, 9, 6);

        // 도리
        MkBox(root.transform, woodDark, V(0f, CH+.16f, -D), V(panW+.5f, .32f, .55f));
        MkBox(root.transform, woodDark, V(0f, CH+.16f,  D), V(panW+.5f, .32f, .55f));
        MkBox(root.transform, woodDark, V(-D, CH+.16f, 0f), V(.55f, .32f, panW+.5f));
        MkBox(root.transform, woodDark, V( D, CH+.16f, 0f), V(.55f, .32f, panW+.5f));

        // 지붕 처마
        MkBox(root.transform, Hex("#1E1408"), V(0f, CH+.60f, 0f), V(D*2.6f, .18f, D*2.6f), ShadowCastingMode.Off);
        MkBox(root.transform, Hex("#14100A"), V(0f, CH+.95f, 0f), V(D*1.6f, .75f,  D*1.0f), ShadowCastingMode.Off);

        // 마루
        MkBox(root.transform, woodMid, V(0f, .025f, 0f), V(D*2f-.3f, .05f, D*2f-.3f), ShadowCastingMode.Off);

        // 석등
        StoneLantern(root.transform, V(-2.5f, 0f, -D + 3f));
        StoneLantern(root.transform, V( 2.5f, 0f, -D + 3f));
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

    static void BuildShojiGrid(Transform parent, Vector3 center, float w, float h, int cols, int rows)
    {
        var sh  = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        var mat = new Material(sh) { color = Hex("#6A4E28") };
        float hw = w * .5f, hh = h * .5f;
        for (int i = 0; i <= cols; i++)
        {
            float x = center.x - hw + (w / cols) * i;
            Line(parent, mat, V(x, center.y - hh, center.z), V(x, center.y + hh, center.z), 0.025f);
        }
        for (int j = 0; j <= rows; j++)
        {
            float y = center.y - hh + (h / rows) * j;
            Line(parent, mat, V(center.x - hw, y, center.z), V(center.x + hw, y, center.z), 0.025f);
        }
    }

    static void CreateLightTrees()
    {
        var root = new GameObject("_HanokPines");
        Pine(root.transform, V(-20f, 0f, 20f), 5.5f, 0);
        Pine(root.transform, V(-27f, 0f, 11f), 6.5f, 1);
        Pine(root.transform, V( 22f, 0f, 19f), 6.0f, 2);
        Pine(root.transform, V( 29f, 0f, 25f), 7.5f, 0);
        Pine(root.transform, V(-16f, 0f, 31f), 5.0f, 1);
        Pine(root.transform, V( 19f, 0f, 33f), 5.5f, 2);
    }

    // ── 프리셋 2: 조선 장터 ──────────────────────────────────
    static void CreateMarketStalls()
    {
        var root = new GameObject("_HanokWalls");
        Color wood = Hex("#3C2810");
        Color[] fabrics = { Hex("#B86828"), Hex("#A04820"), Hex("#28502A"), Hex("#6A3818"), Hex("#8C5820") };

        // 후면 4개 가판대
        for (int i = -1; i <= 2; i++)
        {
            float x = (i - 0.5f) * 6.5f;
            Color fc = fabrics[((i + 2) % fabrics.Length)];
            // 기둥
            MkCyl(root.transform, wood, V(x - 2.2f, 1.3f, 11.5f), V(.20f, 2.6f, .20f), ShadowCastingMode.Off);
            MkCyl(root.transform, wood, V(x + 2.2f, 1.3f, 11.5f), V(.20f, 2.6f, .20f), ShadowCastingMode.Off);
            // 상판
            MkBox(root.transform, wood, V(x, .58f, 11.5f), V(4.8f, .16f, 1.3f));
            MkBox(root.transform, wood, V(x, .28f, 11.5f), V(4.8f, .40f, .16f), ShadowCastingMode.Off);
            // 차양
            MkBox(root.transform, fc, V(x, 2.65f, 10.8f), V(5.4f, .12f, 2.8f), ShadowCastingMode.Off);
            // 매달린 소품 암시
            for (int k = -1; k <= 1; k++)
                MkBox(root.transform, wood, V(x + k * 1.4f, 2.25f, 11.0f), V(.06f, .6f, .06f), ShadowCastingMode.Off);
        }

        // 좌측 가판대 2개
        for (int j = 0; j < 2; j++)
        {
            float z = -3f + j * 7.5f;
            Color fc = fabrics[(j + 2) % fabrics.Length];
            MkCyl(root.transform, wood, V(-12.5f, 1.3f, z - 2f), V(.20f, 2.6f, .20f), ShadowCastingMode.Off);
            MkCyl(root.transform, wood, V(-12.5f, 1.3f, z + 2f), V(.20f, 2.6f, .20f), ShadowCastingMode.Off);
            MkBox(root.transform, wood, V(-12.5f, .58f, z), V(1.3f, .16f, 4.4f));
            MkBox(root.transform, fc,   V(-11.7f, 2.65f, z), V(2.6f, .12f, 5.0f), ShadowCastingMode.Off);
        }

        // 우측 가판대 2개
        for (int j = 0; j < 2; j++)
        {
            float z = -3f + j * 7.5f;
            Color fc = fabrics[(j + 1) % fabrics.Length];
            MkCyl(root.transform, wood, V(12.5f, 1.3f, z - 2f), V(.20f, 2.6f, .20f), ShadowCastingMode.Off);
            MkCyl(root.transform, wood, V(12.5f, 1.3f, z + 2f), V(.20f, 2.6f, .20f), ShadowCastingMode.Off);
            MkBox(root.transform, wood, V(12.5f, .58f, z), V(1.3f, .16f, 4.4f));
            MkBox(root.transform, fc,   V(11.7f, 2.65f, z), V(2.6f, .12f, 5.0f), ShadowCastingMode.Off);
        }

        // 입구 낮은 담
        Color clay = Hex("#6E5838");
        MkBox(root.transform, clay, V(-8.5f, .55f, -12.5f), V(9f, 1.1f, .48f), ShadowCastingMode.Off);
        MkBox(root.transform, clay, V( 8.5f, .55f, -12.5f), V(9f, 1.1f, .48f), ShadowCastingMode.Off);
        // 중앙 항아리 배치
        for (int k = -2; k <= 2; k++)
        {
            var jar = Mk(PrimitiveType.Sphere, root.transform);
            jar.transform.position   = V(k * 2.2f, .4f, -11.2f);
            jar.transform.localScale = V(.55f, .65f, .55f);
            Rend(jar, Hex("#2A1C10"), 0.1f, ShadowCastingMode.Off);
        }
    }

    // ── 프리셋 3: 전통 정원 ──────────────────────────────────
    static void CreateGardenElements()
    {
        var root = new GameObject("_HanokWalls");
        Color stone = Hex("#787870"); Color wood = Hex("#3C2E18");
        Color water = Hex("#3868A0"); Color waterShallow = Hex("#4878B0");

        // 연못 — 2중 레이어로 깊이감
        var pondOuter = Mk(PrimitiveType.Sphere, root.transform);
        pondOuter.transform.position   = V(8f, -.08f, 7.5f);
        pondOuter.transform.localScale = V(8.0f, .06f, 5.5f);
        Rend(pondOuter, water, 0.80f, ShadowCastingMode.Off);
        var pondInner = Mk(PrimitiveType.Sphere, root.transform);
        pondInner.transform.position   = V(8f, -.05f, 7.5f);
        pondInner.transform.localScale = V(5.5f, .04f, 3.8f);
        Rend(pondInner, waterShallow, 0.85f, ShadowCastingMode.Off);

        // 연못 가장자리 석재 11개
        for (int a = 0; a < 11; a++)
        {
            float rad = a / 11f * Mathf.PI * 2f;
            float ex = 4.4f, ez = 3.1f;
            float scale = 0.45f + (a % 3) * 0.1f;
            MkBox(root.transform, stone,
                V(8f + Mathf.Cos(rad) * ex, .06f, 7.5f + Mathf.Sin(rad) * ez),
                V(scale, .16f, scale), ShadowCastingMode.Off);
        }

        // 정자
        float pz = 10.5f;
        foreach (var xi in new[] { -2.2f, 2.2f })
            foreach (var zi in new[] { pz - 2.2f, pz + 2.2f })
                MkCyl(root.transform, wood, V(xi, 1.8f, zi), V(.30f, 3.6f, .30f), ShadowCastingMode.Off);
        MkBox(root.transform, Hex("#22180A"), V(0f, 3.8f, pz), V(6.0f, .18f, 6.0f), ShadowCastingMode.Off);
        MkBox(root.transform, Hex("#16100A"), V(0f, 4.2f, pz), V(3.5f, .90f, 3.5f), ShadowCastingMode.Off);
        MkBox(root.transform, Hex("#0C0A06"), V(0f, 5.2f, pz), V(2.2f, .55f, 2.2f), ShadowCastingMode.Off);
        MkBox(root.transform, wood, V(0f, .10f, pz), V(4.8f, .18f, 4.8f));

        // 석등
        StoneLantern(root.transform, V(-3.5f, 0f, 4.5f));
        StoneLantern(root.transform, V( 3.5f, 0f, 4.5f));

        // 디딤돌 산책로 7개
        Color sc = Hex("#888078");
        for (int i = 0; i < 7; i++)
        {
            float scaleV = 0.9f + (i % 2) * 0.2f;
            MkBox(root.transform, sc,
                V((i % 3 - 1) * 0.4f, .028f, -10f + i * 3.0f),
                V(1.2f * scaleV, .055f, .85f * scaleV), ShadowCastingMode.Off);
        }

        // 담장
        Color wall = Hex("#605850");
        MkBox(root.transform, wall, V(0f, .75f, 15f),   V(32f, 1.5f, .50f), ShadowCastingMode.Off);
        MkBox(root.transform, wall, V(-15f, .75f, 0f),  V(.50f, 1.5f, 30f), ShadowCastingMode.Off);
        MkBox(root.transform, wall, V( 15f, .75f, 0f),  V(.50f, 1.5f, 30f), ShadowCastingMode.Off);

        // 담장 기와
        MkBox(root.transform, Hex("#2A2010"), V(0f, 1.55f, 15f),   V(32.5f, .20f, .70f), ShadowCastingMode.Off);
        MkBox(root.transform, Hex("#2A2010"), V(-15f, 1.55f, 0f),  V(.70f, .20f, 30.5f), ShadowCastingMode.Off);
        MkBox(root.transform, Hex("#2A2010"), V( 15f, 1.55f, 0f),  V(.70f, .20f, 30.5f), ShadowCastingMode.Off);
    }

    static void CreateGardenTrees()
    {
        var root = new GameObject("_HanokPines");
        Pine(root.transform, V(-10f, 0f, 13f), 5.0f, 1);
        Pine(root.transform, V(-13f, 0f,  4f), 6.0f, 2);
        Pine(root.transform, V(-17f, 0f, 19f), 6.5f, 0);
        Pine(root.transform, V( 15f, 0f, 13f), 5.5f, 1);
        Pine(root.transform, V( 19f, 0f,  5f), 4.5f, 2);
        Pine(root.transform, V( -7f, 0f, 19f), 5.5f, 0);
        Pine(root.transform, V(  7f, 0f, 21f), 5.0f, 1);
        Pine(root.transform, V(-23f, 0f, 26f), 7.5f, 2);
        Pine(root.transform, V( 21f, 0f, 29f), 8.5f, 0);
        Pine(root.transform, V(-42f, 0f, 56f), 9.5f, 1);
        Pine(root.transform, V( 40f, 0f, 51f), 8.5f, 2);
        Pine(root.transform, V(-16f, 0f, 66f), 8.0f, 0);
    }

    // ── 프리미티브 헬퍼 ──────────────────────────────────────
    static GameObject Mk(PrimitiveType type, Transform parent)
    {
        var go = GameObject.CreatePrimitive(type);
        if (parent != null) go.transform.SetParent(parent, false);
        Object.Destroy(go.GetComponent<Collider>());
        return go;
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
