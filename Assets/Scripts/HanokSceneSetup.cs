using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 한국 전통 야외 편집 환경 — 기단·마당·돌담·소나무·원경 한옥·산·하늘
/// HanokUIManager.Start() 에서 한 번 호출
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
        CreatePineGroves();
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
        go.transform.position = V(0f, -0.01f, 0f);
        go.transform.localScale = V(300f, 1f, 300f);
        Rend(go, Hex("#7A6D5A"), 0f, ShadowCastingMode.Off);
        Object.Destroy(go.GetComponent<Collider>());
    }

    // ── 기단: 편집 구역 돌 플랫폼 ──────────────────────────────
    static void CreateStonePlatform()
    {
        if (GameObject.Find("_HanokPlatform") != null) return;
        var go = MkBox(null, Hex("#908070"), V(0f, -0.15f, 0f), V(22f, 0.3f, 22f), ShadowCastingMode.Off);
        go.name = "_HanokPlatform";
    }

    // ── 마당 바닥 + 격자 ──────────────────────────────────────
    static void CreateFloor()
    {
        if (GameObject.Find("_HanokFloor") != null) return;
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane); // collider 유지 (raycasting)
        floor.name = "_HanokFloor";
        floor.transform.localScale = V(20f, 1f, 20f);
        Rend(floor, Hex("#D4C9B2"), 0.04f);
        CreateGridOverlay();
    }

    static void CreateGridOverlay()
    {
        if (GameObject.Find("_HanokGrid") != null) return;
        var root = new GameObject("_HanokGrid");
        var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
        var mT = new Material(sh) { color = new Color(0.40f, 0.36f, 0.30f) };
        var mB = new Material(sh) { color = new Color(0.26f, 0.22f, 0.16f) };
        const int H = 10; const float Y = 0.006f;
        for (int i = -H; i <= H; i++)
        {
            bool maj = i % 5 == 0;
            Line(root.transform, maj ? mB : mT, V(i,Y,-H), V(i,Y,H), maj ? 0.05f : 0.014f);
            Line(root.transform, maj ? mB : mT, V(-H,Y,i), V(H,Y,i), maj ? 0.05f : 0.014f);
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

    // ── 마당 석물: 석등 2개 + 경계석 ─────────────────────────
    static void CreateCourtyard()
    {
        if (GameObject.Find("_HanokCourtyard") != null) return;
        var root = new GameObject("_HanokCourtyard");

        StoneLantern(root.transform, V(-3.5f, 0f, 9.8f));
        StoneLantern(root.transform, V( 3.5f, 0f, 9.8f));

        // 기단 모서리 귀틀돌
        Color c = Hex("#7A7060");
        foreach (var p in new[] { V(-11f,.25f, 11f), V(11f,.25f, 11f),
                                   V(-11f,.25f,-11f), V(11f,.25f,-11f) })
            MkBox(root.transform, c, p, V(0.9f,0.5f,0.9f), ShadowCastingMode.Off);

        // 디딤돌 (정면 입구 앞)
        Color sc = Hex("#8A8070");
        MkBox(root.transform, sc, V(0f,.02f,-12f), V(2.5f,.08f,0.8f), ShadowCastingMode.Off);
        MkBox(root.transform, sc, V(0f,.02f,-12.9f), V(2f,.06f,0.6f), ShadowCastingMode.Off);
    }

    static void StoneLantern(Transform parent, Vector3 pos)
    {
        Color s = Hex("#909080"), d = Hex("#707060");
        MkBox(parent, s, pos+V(0,.15f,0), V(0.65f,.30f,0.65f));   // 받침
        MkCyl(parent, s, pos+V(0,.80f,0), V(0.20f,.70f,0.20f));   // 기둥
        MkBox(parent, s, pos+V(0,1.45f,0), V(0.50f,.35f,0.50f));  // 몸체
        MkBox(parent, d, pos+V(0,1.78f,0), V(0.75f,.14f,0.75f));  // 옥개석
        var top = Mk(PrimitiveType.Sphere, parent);
        top.transform.position = pos+V(0,1.96f,0);
        top.transform.localScale = Vector3.one * 0.20f;
        Rend(top, Hex("#706858"), 0.1f, ShadowCastingMode.Off);
    }

    // ── 돌담 + 솟을대문 ──────────────────────────────────────
    static void CreateBoundaryWalls()
    {
        if (GameObject.Find("_HanokWalls") != null) return;
        var root = new GameObject("_HanokWalls");
        Color wC = Hex("#6E6558"), capC = Hex("#4A4030");
        const float D = 13f, H = 1.8f, T = 0.55f;

        WallSeg(root.transform, wC, capC, V(0f,0f,D),  D*2f, H, T, 0f);   // 뒤
        WallSeg(root.transform, wC, capC, V(-D,0f,0f), D*2f, H, T, 90f);  // 좌
        WallSeg(root.transform, wC, capC, V( D,0f,0f), D*2f, H, T, 90f);  // 우
        WallSeg(root.transform, wC, capC, V(-9f,0f,-D), 8f,  H, T, 0f);   // 앞 좌
        WallSeg(root.transform, wC, capC, V( 9f,0f,-D), 8f,  H, T, 0f);   // 앞 우

        // 솟을대문 기둥 2개
        Color postC = Hex("#4A3C28");
        MkBox(root.transform, postC, V(-5f,1.5f,-D), V(0.65f,3.0f,0.65f));
        MkBox(root.transform, postC, V( 5f,1.5f,-D), V(0.65f,3.0f,0.65f));
        // 상인방
        MkBox(root.transform, postC, V(0f,3.1f,-D), V(10.65f,0.35f,0.5f));
        // 문 지붕 (기와)
        MkBox(root.transform, Hex("#3A2E1A"), V(0f,3.5f,-D), V(11.5f,0.22f,0.85f));
        MkBox(root.transform, Hex("#2A200A"), V(0f,3.75f,-D), V(8f,0.55f,0.5f));
    }

    static void WallSeg(Transform p, Color wC, Color capC,
                        Vector3 center, float len, float h, float t, float rotY)
    {
        var body = Mk(PrimitiveType.Cube, p);
        body.transform.position = center + Vector3.up * h * 0.5f;
        body.transform.localScale = V(len, h, t);
        body.transform.eulerAngles = V(0, rotY, 0);
        Rend(body, wC, 0f, ShadowCastingMode.Off);

        var cap = Mk(PrimitiveType.Cube, p);
        cap.transform.position = center + Vector3.up * h;
        cap.transform.localScale = V(len+0.4f, 0.22f, t+0.4f);
        cap.transform.eulerAngles = V(0, rotY, 0);
        Rend(cap, capC, 0f, ShadowCastingMode.Off);
    }

    // ── 소나무 군락 ──────────────────────────────────────────
    static void CreatePineGroves()
    {
        if (GameObject.Find("_HanokPines") != null) return;
        var root = new GameObject("_HanokPines");

        // 좌측 군락
        Pine(root.transform, V(-22f,0f,18f), 5f);
        Pine(root.transform, V(-27f,0f,25f), 6.5f);
        Pine(root.transform, V(-20f,0f,32f), 4.5f);
        Pine(root.transform, V(-33f,0f,20f), 7f);
        Pine(root.transform, V(-24f,0f,10f), 5.5f);
        Pine(root.transform, V(-30f,0f,35f), 5f);

        // 우측 군락
        Pine(root.transform, V(22f,0f,18f), 5f);
        Pine(root.transform, V(27f,0f,27f), 6f);
        Pine(root.transform, V(20f,0f,33f), 4.5f);
        Pine(root.transform, V(31f,0f,16f), 7f);
        Pine(root.transform, V(25f,0f,8f),  5.5f);

        // 뒤 군락
        Pine(root.transform, V(-10f,0f,22f), 4f);
        Pine(root.transform, V( 10f,0f,23f), 4.5f);
        Pine(root.transform, V(  0f,0f,26f), 5f);

        // 원경
        Pine(root.transform, V(-45f,0f,55f), 8f);
        Pine(root.transform, V( 42f,0f,58f), 9f);
        Pine(root.transform, V(-15f,0f,60f), 7.5f);
        Pine(root.transform, V( 20f,0f,65f), 8.5f);
        Pine(root.transform, V(-35f,0f,70f), 10f);
        Pine(root.transform, V( 50f,0f,45f), 9.5f);
    }

    static void Pine(Transform parent, Vector3 basePos, float h)
    {
        Color trunk = Hex("#4A3018");
        Color darkG = Hex("#1A4010");
        Color midG  = Hex("#2A5820");

        MkCyl(parent, trunk, basePos+V(0,h*.35f,0), V(h*.07f,h*.7f,h*.07f), ShadowCastingMode.Off);
        FlatSph(parent, darkG, basePos+V(0,h*.55f,0), V(h*.55f,h*.18f,h*.55f));
        FlatSph(parent, midG,  basePos+V(0,h*.72f,0), V(h*.45f,h*.15f,h*.45f));
        FlatSph(parent, darkG, basePos+V(0,h*.87f,0), V(h*.30f,h*.11f,h*.30f));
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

        // 중앙 대청 (main hall)
        Hanok(root.transform, V(0f,0f,85f), 14f,4.5f,9f, Hex("#38301E"), Hex("#28200E"));
        // 좌익사
        Hanok(root.transform, V(-38f,0f,70f), 10f,3.5f,7f, Hex("#342C1C"), Hex("#241C0C"));
        // 우익사
        Hanok(root.transform, V( 38f,0f,70f), 10f,3.5f,7f, Hex("#342C1C"), Hex("#241C0C"));
        // 후원 정자 2기
        Pavilion(root.transform, V(-7f,0f,108f), Hex("#302818"));
        Pavilion(root.transform, V( 7f,0f,108f), Hex("#302818"));
        // 원경 문루 (대문채)
        Hanok(root.transform, V(-65f,0f,55f), 8f,4f,6f, Hex("#302818"), Hex("#201008"));
        Hanok(root.transform, V( 65f,0f,55f), 8f,4f,6f, Hex("#302818"), Hex("#201008"));
    }

    static void Hanok(Transform p, Vector3 pos, float w, float h, float d, Color body, Color roof)
    {
        // 기단
        MkBox(p, Hex("#7A7060"), pos+V(0,.2f,0), V(w+1f,.4f,d+1f), ShadowCastingMode.Off);
        // 기둥
        Color postC = Hex("#3A2E1A");
        foreach (var xi in new[]{-0.4f,0.4f})
            foreach (var zi in new[]{-0.5f,0.5f})
                MkCyl(p, postC, pos+V(w*xi,h*.5f,d*zi), V(.28f,h,.28f), ShadowCastingMode.Off);
        // 본체
        MkBox(p, body, pos+V(0,h*.5f,0), V(w,h,d), ShadowCastingMode.Off);
        // 처마
        MkBox(p, roof, pos+V(0,h+.3f,0), V(w*1.38f,h*.12f,d*1.38f), ShadowCastingMode.Off);
        // 용마루
        MkBox(p, Hex("#201808"), pos+V(0,h+.5f+h*.12f,0), V(w*.5f,h*.28f,d*.35f), ShadowCastingMode.Off);
    }

    static void Pavilion(Transform p, Vector3 pos, Color c)
    {
        float h = 3f;
        foreach (var xi in new[]{-1f,1f})
            foreach (var zi in new[]{-1f,1f})
                MkCyl(p, Hex("#4A3820"), pos+V(xi*1.2f,h*.5f,zi*1.2f), V(.22f,h,.22f), ShadowCastingMode.Off);
        MkBox(p, c, pos+V(0,h+.5f,0), V(4f,.15f,4f), ShadowCastingMode.Off);
        MkBox(p, Hex("#201808"), pos+V(0,h+.8f,0), V(2f,h*.5f,2f), ShadowCastingMode.Off);
        MkBox(p, c, pos+V(0,h+h*.5f+.95f,0), V(3f,.12f,3f), ShadowCastingMode.Off);
    }

    // ── 원경 산 실루엣 ────────────────────────────────────────
    static void CreateMountainSilhouettes()
    {
        if (GameObject.Find("_HanokMountains") != null) return;
        var root = new GameObject("_HanokMountains");
        (Color c, Vector3 pos, Vector3 s)[] mts = {
            (Hex("#6A7C8C"), V(-120f,-28f,380f), V(160f,75f,120f)),
            (Hex("#5E7268"), V(  20f,-32f,420f), V(200f,90f,150f)),
            (Hex("#788490"), V( 150f,-25f,360f), V(140f,65f,110f)),
            (Hex("#607050"), V( -60f,-20f,280f), V(120f,55f, 90f)),
            (Hex("#708060"), V( 220f,-22f,300f), V(100f,50f, 80f)),
        };
        foreach (var (c,pos,s) in mts)
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
        mat.SetFloat("_SunSize",             0.04f);
        mat.SetFloat("_SunSizeConvergence",  5f);
        mat.SetFloat("_AtmosphereThickness", 0.85f);
        mat.SetColor("_SkyTint",    Hex("#8BBCD4"));
        mat.SetColor("_GroundColor", Hex("#7A6650"));
        mat.SetFloat("_Exposure",   1.25f);
        RenderSettings.skybox = mat;
    }

    // ── 조명 ──────────────────────────────────────────────────
    static void SetupLighting()
    {
        RenderSettings.ambientMode         = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor     = Hex("#B8D0E8") * 0.75f;
        RenderSettings.ambientEquatorColor = Hex("#EAD8B8") * 0.65f;
        RenderSettings.ambientGroundColor  = Hex("#6B5A42") * 0.40f;

        RenderSettings.fog              = true;
        RenderSettings.fogMode          = FogMode.Linear;
        RenderSettings.fogStartDistance = 55f;
        RenderSettings.fogEndDistance   = 450f;
        RenderSettings.fogColor         = Hex("#C4D8E8");

        var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l.type != LightType.Directional) continue;
            l.color                 = Hex("#FFF6E0");
            l.intensity             = 1.25f;
            l.shadows               = LightShadows.Soft;
            l.shadowStrength        = 0.55f;
            l.transform.eulerAngles = V(45f, -40f, 0f);
            break;
        }
    }

    // ── 카메라 ────────────────────────────────────────────────
    static void SetupCamera()
    {
        if (Camera.main == null) return;
        bool hasSky = RenderSettings.skybox != null;
        Camera.main.clearFlags      = hasSky ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = Hex("#8BBCD4");
        Camera.main.fieldOfView     = 55f;
        Camera.main.nearClipPlane   = 0.05f; // 가까이 줌인해도 클리핑 안 됨
        Camera.main.farClipPlane    = 2000f;
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
        var sh2 = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(sh2) { color = c };
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smooth);
        if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic",   0f);
        var r = go.GetComponent<Renderer>();
        r.material          = mat;
        r.shadowCastingMode = sh;
        r.receiveShadows    = sh != ShadowCastingMode.Off;
    }
}
