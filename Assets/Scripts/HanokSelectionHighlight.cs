using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 선택 오브젝트 시각 피드백
///  - LineRenderer 원형 링 (바닥, NAVY 색)  — 오브젝트 이동 추적
///  - Point Light (오브젝트 위, 부드러운 포커스) — 이동 추적
/// </summary>
public class HanokSelectionHighlight : MonoBehaviour
{
    GameObject _ringGO;
    Light      _focusLight;
    float      _ringRadius;   // 생성 시 계산된 반지름 캐시

    // ── 공개 인터페이스 ──────────────────────────────────
    public void Show()
    {
        EnsureRing();
        EnsureFocusLight();
        if (_ringGO)     _ringGO.SetActive(true);
        if (_focusLight) _focusLight.enabled = true;
    }

    public void Hide()
    {
        if (_ringGO)     _ringGO.SetActive(false);
        if (_focusLight) _focusLight.enabled = false;
    }

    void OnDestroy()
    {
        if (_ringGO     != null) Destroy(_ringGO);
        if (_focusLight != null) Destroy(_focusLight.gameObject);
    }

    // ── 매 프레임: 오브젝트 이동 추적 ────────────────────
    void LateUpdate()
    {
        if (_ringGO != null && _ringGO.activeSelf)
            RefreshRingPositions();

        if (_focusLight != null && _focusLight.enabled)
        {
            var b = GetBounds();
            _focusLight.transform.position = b.center + Vector3.up * (b.extents.y + 1.5f);
        }
    }

    // ── 바운드 계산 ──────────────────────────────────────
    Bounds GetBounds()
    {
        var rends = GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return new Bounds(transform.position, Vector3.one);
        var b = rends[0].bounds;
        foreach (var r in rends) b.Encapsulate(r.bounds);
        return b;
    }

    // ── 선택 링 (LineRenderer 원형) ──────────────────────
    void EnsureRing()
    {
        if (_ringGO != null) return;
        _ringGO = new GameObject("_SelRing");

        var lr  = _ringGO.AddComponent<LineRenderer>();
        var sh  = Shader.Find("Universal Render Pipeline/Unlit")
               ?? Shader.Find("Unlit/Color")
               ?? Shader.Find("Sprites/Default");
        var mat = new Material(sh);
        ColorUtility.TryParseHtmlString("#1B3A6B", out Color col);
        mat.color = col;

        lr.material             = mat;
        lr.startWidth           = lr.endWidth = 0.055f;
        lr.useWorldSpace        = true;
        lr.loop                 = true;
        lr.shadowCastingMode    = ShadowCastingMode.Off;
        lr.receiveShadows       = false;
        lr.generateLightingData = false;
        lr.positionCount        = 36;

        // 반지름 계산 후 캐시
        var b      = GetBounds();
        _ringRadius = Mathf.Max(b.extents.x, b.extents.z) + 0.22f;

        RefreshRingPositions();
    }

    void RefreshRingPositions()
    {
        var lr = _ringGO?.GetComponent<LineRenderer>();
        if (lr == null) return;
        // XZ 중심은 오브젝트 pivot 기준, Y는 항상 월드 0.018f (바닥 위 부유)
        float cx = transform.position.x;
        float cz = transform.position.z;
        const int SEG = 36;
        for (int i = 0; i < SEG; i++)
        {
            float a = i / (float)SEG * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(
                cx + Mathf.Cos(a) * _ringRadius,
                0.018f,
                cz + Mathf.Sin(a) * _ringRadius));
        }
    }

    // ── 포커스 포인트 라이트 ─────────────────────────────
    void EnsureFocusLight()
    {
        if (_focusLight != null) return;
        var b   = GetBounds();
        var go  = new GameObject("_SelLight");
        go.transform.position = b.center + Vector3.up * (b.extents.y + 1.5f);

        _focusLight                = go.AddComponent<Light>();
        _focusLight.type           = LightType.Point;
        ColorUtility.TryParseHtmlString("#FFF8E8", out Color lc);
        _focusLight.color          = lc;
        _focusLight.intensity      = 1.1f;
        _focusLight.range          = Mathf.Max(b.size.magnitude * 2.5f, 4f);
        _focusLight.shadows        = LightShadows.Soft;
        _focusLight.shadowStrength = 0.28f;
        _focusLight.shadowBias     = 0.04f;
        _focusLight.cullingMask    = -1; // 모든 레이어
    }
}
