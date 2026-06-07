using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 런타임 실내 편집 환경 초기화 — 바닥 평면, 그리드, 조명, 카메라 배경/FOV
/// HanokUIManager.Start() 에서 한 번 호출
/// </summary>
public static class HanokSceneSetup
{
    static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out Color c); return c; }

    public static void Setup()
    {
        CreateFloor();
        SetupLighting();
        SetupCamera();
    }

    // ── 바닥 평면 ─────────────────────────────────────────
    static void CreateFloor()
    {
        if (GameObject.Find("_HanokFloor") != null) return;

        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "_HanokFloor";
        floor.transform.localScale = new Vector3(20f, 1f, 20f); // 200×200 유닛

        var shader = Shader.Find("Universal Render Pipeline/Lit")
                  ?? Shader.Find("Standard");
        var mat = new Material(shader);
        mat.color = Hex("#D2CBBF"); // 따뜻한 크림 베이지
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.06f);
        if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic",   0f);
        floor.GetComponent<Renderer>().material = mat;

        CreateGridOverlay();
    }

    // ── 그리드 선 오버레이 ────────────────────────────────
    static void CreateGridOverlay()
    {
        if (GameObject.Find("_HanokGrid") != null) return;

        var root = new GameObject("_HanokGrid");
        root.transform.position = Vector3.zero;

        // 선 머티리얼 — URP Unlit (불투명, 회색)
        var shader = Shader.Find("Universal Render Pipeline/Unlit")
                  ?? Shader.Find("Unlit/Color")
                  ?? Shader.Find("Sprites/Default");
        var matThin = new Material(shader);
        matThin.color = new Color(0.48f, 0.44f, 0.38f, 1f);

        var matBold = new Material(shader);
        matBold.color = new Color(0.32f, 0.28f, 0.23f, 1f);

        const int   HALF  = 10;      // ±10m 범위
        const float Y_OFF = 0.004f;  // 바닥면보다 살짝 위 (z-fighting 방지)

        for (int i = -HALF; i <= HALF; i++)
        {
            bool isMajor = (i % 5 == 0);
            var  m       = isMajor ? matBold : matThin;
            float w      = isMajor ? 0.04f   : 0.012f;

            // Z 방향 선 (X = i)
            DrawLine(root.transform, m,
                new Vector3(i, Y_OFF, -HALF),
                new Vector3(i, Y_OFF,  HALF), w);

            // X 방향 선 (Z = i)
            DrawLine(root.transform, m,
                new Vector3(-HALF, Y_OFF, i),
                new Vector3( HALF, Y_OFF, i), w);
        }
    }

    // useWorldSpace=true + 절대 좌표 → 그리드 왜곡 없음
    static void DrawLine(Transform parent, Material mat,
                         Vector3 from, Vector3 to, float width = 0.012f)
    {
        var go = new GameObject("L");
        go.transform.SetParent(parent, false);
        var lr              = go.AddComponent<LineRenderer>();
        lr.useWorldSpace    = true;          // 월드 좌표 기준
        lr.positionCount    = 2;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
        lr.startWidth        = width;
        lr.endWidth          = width;
        lr.material          = mat;
        lr.shadowCastingMode = ShadowCastingMode.Off;
        lr.receiveShadows    = false;
        lr.generateLightingData = false;
    }

    // ── 조명 ─────────────────────────────────────────────
    static void SetupLighting()
    {
        // 따뜻한 실내 환경광
        RenderSettings.ambientMode  = AmbientMode.Flat;
        RenderSettings.ambientLight = Hex("#C8B89A") * 0.7f;

        // Directional Light (씬에 있는 것) 보정
        var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l.type != LightType.Directional) continue;
            l.color              = Hex("#FFF4E0");
            l.intensity          = 1.1f;
            l.shadows            = LightShadows.Soft;
            l.shadowStrength     = 0.5f;
            l.transform.eulerAngles = new Vector3(50f, -30f, 0f);
            break;
        }
    }

    // ── 카메라 배경 + FOV ─────────────────────────────────
    static void SetupCamera()
    {
        if (Camera.main == null) return;

        Camera.main.clearFlags       = CameraClearFlags.SolidColor;
        Camera.main.backgroundColor  = Hex("#C4D4E0"); // 밝은 회청색
        Camera.main.fieldOfView      = 55f;            // Unity Scene view와 유사
        Camera.main.nearClipPlane    = 0.1f;
        Camera.main.farClipPlane     = 2000f;
    }
}
