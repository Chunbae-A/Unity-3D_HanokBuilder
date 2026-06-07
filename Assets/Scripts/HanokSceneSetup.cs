using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 런타임 실외 편집 환경 초기화
/// 바닥·외곽 지면·그리드·원경 산·스카이박스·안개·조명·카메라
/// HanokUIManager.Start() 에서 한 번 호출
/// </summary>
public static class HanokSceneSetup
{
    static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out Color c); return c; }

    public static void Setup()
    {
        CreateOuterGround();
        CreateFloor();
        CreateMountainSilhouettes();
        SetupSkybox();
        SetupLighting();
        SetupCamera();
    }

    // ── 외곽 자연 지면 (편집 구역 바깥 넓은 지형) ──────────
    static void CreateOuterGround()
    {
        if (GameObject.Find("_HanokOuterGround") != null) return;

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "_HanokOuterGround";
        ground.transform.position   = new Vector3(0f, -0.008f, 0f);
        ground.transform.localScale = new Vector3(300f, 1f, 300f); // 3000×3000 유닛

        var mat = MakeMat(Hex("#7A6D5A"), 0f, 0f); // 자연 흙빛
        ground.GetComponent<Renderer>().material = mat;
        ground.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
    }

    // ── 편집 구역 바닥 (마당 느낌, 격자 포함) ──────────────
    static void CreateFloor()
    {
        if (GameObject.Find("_HanokFloor") != null) return;

        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "_HanokFloor";
        floor.transform.position   = new Vector3(0f, 0f, 0f);
        floor.transform.localScale = new Vector3(20f, 1f, 20f); // 200×200 유닛

        var mat = MakeMat(Hex("#D4C9B2"), 0.04f, 0f); // 한국 전통 마당 색 (회백토)
        floor.GetComponent<Renderer>().material = mat;

        CreateGridOverlay();
    }

    // ── 그리드 선 오버레이 ──────────────────────────────────
    static void CreateGridOverlay()
    {
        if (GameObject.Find("_HanokGrid") != null) return;

        var root = new GameObject("_HanokGrid");

        var shader = Shader.Find("Universal Render Pipeline/Unlit")
                  ?? Shader.Find("Unlit/Color")
                  ?? Shader.Find("Sprites/Default");

        var matThin = new Material(shader);
        matThin.color = new Color(0.40f, 0.36f, 0.30f, 1f);

        var matBold = new Material(shader);
        matBold.color = new Color(0.26f, 0.22f, 0.16f, 1f);

        const int   HALF  = 10;
        const float Y_OFF = 0.006f;

        for (int i = -HALF; i <= HALF; i++)
        {
            bool major = (i % 5 == 0);
            var  m     = major ? matBold : matThin;
            float w    = major ? 0.05f   : 0.014f;

            DrawLine(root.transform, m,
                new Vector3(i, Y_OFF, -HALF), new Vector3(i, Y_OFF,  HALF), w);
            DrawLine(root.transform, m,
                new Vector3(-HALF, Y_OFF, i), new Vector3( HALF, Y_OFF, i), w);
        }
    }

    static void DrawLine(Transform parent, Material mat, Vector3 from, Vector3 to, float w = 0.014f)
    {
        var go = new GameObject("L");
        go.transform.SetParent(parent, false);
        var lr              = go.AddComponent<LineRenderer>();
        lr.useWorldSpace    = true;
        lr.positionCount    = 2;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
        lr.startWidth        = w;
        lr.endWidth          = w;
        lr.material          = mat;
        lr.shadowCastingMode = ShadowCastingMode.Off;
        lr.receiveShadows    = false;
        lr.generateLightingData = false;
    }

    // ── 원경 산 실루엣 ──────────────────────────────────────
    static void CreateMountainSilhouettes()
    {
        if (GameObject.Find("_HanokMountains") != null) return;

        var root = new GameObject("_HanokMountains");

        // 멀리 있는 산일수록 더 연하고 차갑게 (대기 원근감)
        MakeMountain(root.transform, Hex("#6A7C8C"), new Vector3(-120f, -28f, 380f), new Vector3(160f, 75f, 120f));
        MakeMountain(root.transform, Hex("#5E7268"), new Vector3(  20f, -32f, 420f), new Vector3(200f, 90f, 150f));
        MakeMountain(root.transform, Hex("#788490"), new Vector3( 150f, -25f, 360f), new Vector3(140f, 65f, 110f));
        MakeMountain(root.transform, Hex("#607050"), new Vector3( -60f, -20f, 280f), new Vector3(120f, 55f,  90f));
        MakeMountain(root.transform, Hex("#708060"), new Vector3( 220f, -22f, 300f), new Vector3(100f, 50f,  80f));
    }

    static void MakeMountain(Transform parent, Color color, Vector3 pos, Vector3 scale)
    {
        // Sphere를 납작·넓게 — 하단은 외곽 지면 아래에 묻혀 산 정상만 보임
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "mountain";
        go.transform.SetParent(parent, false);
        go.transform.position   = pos;
        go.transform.localScale = scale;

        var shader = Shader.Find("Universal Render Pipeline/Unlit")
                  ?? Shader.Find("Unlit/Color");
        var mat = new Material(shader);
        mat.color = color;
        go.GetComponent<Renderer>().material          = mat;
        go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        go.GetComponent<Renderer>().receiveShadows    = false;
        Object.Destroy(go.GetComponent<Collider>());
    }

    // ── 스카이박스 (절차적 하늘) ────────────────────────────
    static void SetupSkybox()
    {
        var shader = Shader.Find("Skybox/Procedural");
        if (shader == null) return; // 폴백 → SetupCamera 에서 SolidColor 사용

        var mat = new Material(shader);
        mat.SetFloat("_SunSize",             0.04f);
        mat.SetFloat("_SunSizeConvergence",  5f);
        mat.SetFloat("_AtmosphereThickness", 0.85f);
        mat.SetColor("_SkyTint",    Hex("#8BBCD4")); // 한국 가을 하늘 청색
        mat.SetColor("_GroundColor", Hex("#7A6650")); // 따뜻한 지평선 아래
        mat.SetFloat("_Exposure",   1.25f);

        RenderSettings.skybox = mat;
    }

    // ── 조명 (야외 맑은 낮) ─────────────────────────────────
    static void SetupLighting()
    {
        // 삼색 환경광: 하늘·수평·지면
        RenderSettings.ambientMode         = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor     = Hex("#B8D0E8") * 0.75f;
        RenderSettings.ambientEquatorColor = Hex("#EAD8B8") * 0.65f;
        RenderSettings.ambientGroundColor  = Hex("#6B5A42") * 0.40f;

        // 안개 (원경 산을 자연스럽게 흐림)
        RenderSettings.fog              = true;
        RenderSettings.fogMode          = FogMode.Linear;
        RenderSettings.fogStartDistance = 90f;
        RenderSettings.fogEndDistance   = 400f;
        RenderSettings.fogColor         = Hex("#C4D8E8");

        // 직사광 (맑은 오후 햇빛)
        var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l.type != LightType.Directional) continue;
            l.color                 = Hex("#FFF6E0");
            l.intensity             = 1.25f;
            l.shadows               = LightShadows.Soft;
            l.shadowStrength        = 0.55f;
            l.transform.eulerAngles = new Vector3(45f, -40f, 0f);
            break;
        }
    }

    // ── 카메라 ──────────────────────────────────────────────
    static void SetupCamera()
    {
        if (Camera.main == null) return;

        bool hasSkybox = RenderSettings.skybox != null &&
                         Shader.Find("Skybox/Procedural") != null;

        Camera.main.clearFlags = hasSkybox
            ? CameraClearFlags.Skybox
            : CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = Hex("#8BBCD4"); // Skybox 없을 때 폴백 색
        Camera.main.fieldOfView     = 55f;
        Camera.main.nearClipPlane   = 0.1f;
        Camera.main.farClipPlane    = 2000f;
    }

    // ── 유틸 ────────────────────────────────────────────────
    static Material MakeMat(Color color, float smoothness, float metallic)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat    = new Material(shader);
        mat.color  = color;
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic",   metallic);
        return mat;
    }
}
