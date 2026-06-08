using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 런타임 3축 회전 기즈모
/// — X(빨강)·Y(초록)·Z(파랑) 링을 오브젝트 주변에 그림
/// — 링 위에서 드래그하면 해당 축으로 회전, 호버 시 흰색 강조
/// HanokUIManager 에서 Rotate 모드 진입 시 Attach, 벗어날 때 Detach
/// </summary>
public class HanokRotationGizmo : MonoBehaviour
{
    public enum Axis { None, X, Y, Z }

    // ── 상수 ──────────────────────────────────────────────
    const int   SEGS      = 64;
    const float HIT_PX    = 18f;
    const float ROT_SPEED = 0.5f;
    // 선 두께: _radius 기준 비율 (카메라 거리에 자동 비례)
    const float LINE_RATIO    = 0.018f;
    const float LINE_RATIO_HV = 0.045f;

    static readonly Color C_X = new Color(0.90f, 0.20f, 0.16f);
    static readonly Color C_Y = new Color(0.16f, 0.78f, 0.26f);
    static readonly Color C_Z = new Color(0.18f, 0.44f, 0.92f);

    // ── 상태 ──────────────────────────────────────────────
    GameObject     _target;
    GameObject     _root;
    LineRenderer[] _rings = new LineRenderer[3];  // 0=X  1=Y  2=Z
    Material[]     _mats  = new Material[3];

    Axis    _hover = Axis.None;
    Axis    _drag  = Axis.None;
    Vector2 _prevMp;
    float   _radius;

    // ── 공개 API ──────────────────────────────────────────
    /// <summary>기즈모가 현재 마우스 입력을 소비 중이면 true</summary>
    public bool IsConsuming => _drag != Axis.None || _hover != Axis.None;

    public void Attach(GameObject target)
    {
        _target = target;
        if (_root == null) Build();
        _root.SetActive(true);
    }

    public void Detach()
    {
        _target = null;
        _drag   = Axis.None;
        _hover  = Axis.None;
        if (_root) _root.SetActive(false);
    }

    // ── 생명주기 ──────────────────────────────────────────
    void LateUpdate()
    {
        if (_target == null || _root == null || !_root.activeSelf) return;

        Vector3 center = VisualCenter(_target);
        _radius = RingRadius(_target);

        RefreshRing(0, center, Vector3.right,   _radius);
        RefreshRing(1, center, Vector3.up,      _radius);
        RefreshRing(2, center, Vector3.forward, _radius);

        HandleInput(center);
    }

    // ── 기즈모 생성 ───────────────────────────────────────
    void Build()
    {
        _root = new GameObject("_HanokRotGizmo");
        _root.transform.SetParent(transform, false);

        var shader = Shader.Find("Universal Render Pipeline/Unlit")
                  ?? Shader.Find("Unlit/Color");
        Color[] cols = { C_X, C_Y, C_Z };

        for (int i = 0; i < 3; i++)
        {
            var go = new GameObject($"Ring{i}");
            go.transform.SetParent(_root.transform, false);
            var lr = go.AddComponent<LineRenderer>();

            _mats[i] = new Material(shader) { color = cols[i] };
            lr.material          = _mats[i];
            lr.useWorldSpace     = true;
            lr.positionCount     = SEGS + 1;
            lr.loop              = false;
            lr.startWidth        = 0.03f; // RefreshRing 에서 매 프레임 갱신됨
            lr.endWidth          = 0.03f;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows    = false;
            lr.generateLightingData = false;
            _rings[i] = lr;
        }
    }

    // ── 링 위치·스타일 업데이트 ───────────────────────────
    void RefreshRing(int i, Vector3 center, Vector3 normal, float r)
    {
        var (t1, t2) = RingTangents(normal);
        var lr = _rings[i];

        for (int s = 0; s <= SEGS; s++)
        {
            float a = s / (float)SEGS * Mathf.PI * 2f;
            lr.SetPosition(s, center + (t1 * Mathf.Cos(a) + t2 * Mathf.Sin(a)) * r);
        }

        bool active = _drag == (Axis)(i + 1) || _hover == (Axis)(i + 1);
        Color[] def = { C_X, C_Y, C_Z };
        _mats[i].color = active ? Color.white : def[i];
        float lw = _radius * (active ? LINE_RATIO_HV : LINE_RATIO);
        lr.startWidth = lr.endWidth = lw;
    }

    // ── 입력 처리 ─────────────────────────────────────────
    void HandleInput(Vector3 center)
    {
        var mouse = Mouse.current;
        if (mouse == null || Camera.main == null) return;

        Vector2 mp = mouse.position.ReadValue();

        if (_drag != Axis.None)
        {
            // 드래그 중: 회전 적용
            if (mouse.leftButton.isPressed)
                ApplyRotation(_drag, mp - _prevMp, center);
            else
                _drag = Axis.None;
        }
        else
        {
            // 호버 감지 → 클릭으로 드래그 시작
            _hover = ClosestAxis(mp);
            if (mouse.leftButton.wasPressedThisFrame && _hover != Axis.None)
                _drag = _hover;
        }

        _prevMp = mp;
    }

    // ── 회전 적용 ─────────────────────────────────────────
    void ApplyRotation(Axis axis, Vector2 delta, Vector3 pivot)
    {
        float angle = axis switch
        {
            Axis.X =>  delta.y * ROT_SPEED,  // 위로 드래그 = 위로 젖힘
            Axis.Y =>  delta.x * ROT_SPEED,  // 오른쪽 드래그 = 우회전
            Axis.Z => -delta.x * ROT_SPEED,  // 오른쪽 드래그 = 좌롤
            _      => 0f
        };

        Vector3 worldAxis = axis switch
        {
            Axis.X => Vector3.right,
            Axis.Y => Vector3.up,
            Axis.Z => Vector3.forward,
            _      => Vector3.up
        };

        _target.transform.RotateAround(pivot, worldAxis, angle);
    }

    // ── 가장 가까운 링 축 탐색 ────────────────────────────
    Axis ClosestAxis(Vector2 mp)
    {
        float best = HIT_PX;
        Axis  res  = Axis.None;

        for (int i = 0; i < 3; i++)
        {
            float d = RingScreenDist(_rings[i], mp);
            if (d < best) { best = d; res = (Axis)(i + 1); }
        }
        return res;
    }

    float RingScreenDist(LineRenderer lr, Vector2 mp)
    {
        float min = float.MaxValue;
        var   cam = Camera.main;

        for (int s = 0; s < SEGS; s++)
        {
            Vector3 wp = lr.GetPosition(s);
            // 카메라 뒤쪽 점은 스킵
            if (Vector3.Dot(wp - cam.transform.position, cam.transform.forward) <= 0f) continue;
            float d = Vector2.Distance(mp, (Vector2)(Vector3)cam.WorldToScreenPoint(wp));
            if (d < min) min = d;
        }
        return min;
    }

    // ── 링 평면 접선벡터 계산 ─────────────────────────────
    static (Vector3 t1, Vector3 t2) RingTangents(Vector3 normal)
    {
        Vector3 h  = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) < 0.99f
                   ? Vector3.up : Vector3.right;
        Vector3 t1 = Vector3.Cross(h, normal).normalized;
        Vector3 t2 = Vector3.Cross(t1, normal).normalized;
        return (t1, t2);
    }

    // ── 바운드 유틸 ───────────────────────────────────────

    /// <summary>
    /// FBX cm-스케일로 bounds가 비정상적으로 클 때도 시각적 중심을 안전하게 반환.
    /// transform.position 기준 XZ ±4m, Y 0~4m 이내로 클램핑.
    /// </summary>
    static Vector3 VisualCenter(GameObject obj)
    {
        var rs = obj.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return obj.transform.position + Vector3.up;
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);

        Vector3 p = obj.transform.position;
        return new Vector3(
            Mathf.Clamp(b.center.x, p.x - 4f, p.x + 4f),
            Mathf.Clamp(b.center.y, p.y,       p.y + 4f), // 바닥 이상 4m 이하
            Mathf.Clamp(b.center.z, p.z - 4f,  p.z + 4f)
        );
    }

    static float RingRadius(GameObject obj)
    {
        if (Camera.main == null) return 1.0f;

        Vector3 center = VisualCenter(obj);
        float   dist   = Vector3.Distance(Camera.main.transform.position, center);

        var rs = obj.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0)
            return Mathf.Clamp(dist * 0.06f, 0.3f, 2f);

        var b = rs[0].bounds;
        foreach (var rv in rs) b.Encapsulate(rv.bounds);
        float ext = Mathf.Max(b.size.x, b.size.y, b.size.z);

        // ── 정상 bounds: 에셋 크기에 비례해 링 자동 조정 ──────────────────
        if (ext <= dist * 5f)
            return Mathf.Clamp(ext * 0.025f, 0.08f, 3f);

        // ── 비정상 bounds (cm-scale FBX) → 카메라 거리 fallback ────────────
        return Mathf.Clamp(dist * 0.025f, 0.12f, 1.0f);
    }
}
