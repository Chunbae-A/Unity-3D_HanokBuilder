using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 런타임 3축 회전 기즈모 — Unity 에디터 스타일
/// X(빨강)·Y(초록)·Z(파랑) 링이 오브젝트를 감싸며 표시
/// renderQueue 4000 으로 항상 오브젝트 위에 렌더링
/// </summary>
public class HanokRotationGizmo : MonoBehaviour
{
    public enum Axis { None, X, Y, Z }

    const int   SEGS          = 96;
    const float HIT_PX        = 22f;
    const float LINE_RATIO    = 0.055f;   // 링 반지름 대비 선 두께
    const float LINE_RATIO_HV = 0.10f;   // 호버/드래그 시 두께

    static readonly Color C_X = new Color(0.92f, 0.22f, 0.18f);
    static readonly Color C_Y = new Color(0.18f, 0.82f, 0.28f);
    static readonly Color C_Z = new Color(0.20f, 0.48f, 0.96f);

    GameObject     _target;
    GameObject     _root;
    LineRenderer[] _rings = new LineRenderer[3];
    Material[]     _mats  = new Material[3];

    Axis    _hover       = Axis.None;
    Axis    _drag        = Axis.None;
    Vector2 _prevMp;
    Vector2 _smoothDelta;
    float   _radius;

    public System.Action<GameObject> onDragEnd;
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

    void Build()
    {
        _root = new GameObject("_HanokRotGizmo");
        _root.transform.SetParent(transform, false);

        // URP Unlit shader — renderQueue 4000 으로 항상 위에 렌더링
        var shader = Shader.Find("Universal Render Pipeline/Unlit")
                  ?? Shader.Find("Unlit/Color");
        Color[] cols = { C_X, C_Y, C_Z };

        for (int i = 0; i < 3; i++)
        {
            var go = new GameObject("Ring" + i);
            go.transform.SetParent(_root.transform, false);
            var lr = go.AddComponent<LineRenderer>();

            _mats[i] = new Material(shader) { color = cols[i] };
            _mats[i].renderQueue = 4000; // 씬 위에 항상 렌더링

            lr.material             = _mats[i];
            lr.useWorldSpace        = true;
            lr.positionCount        = SEGS + 1;
            lr.loop                 = false;
            lr.startWidth           = 0.08f;
            lr.endWidth             = 0.08f;
            lr.shadowCastingMode    = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows       = false;
            lr.generateLightingData = false;
            _rings[i] = lr;
        }
    }

    void RefreshRing(int i, Vector3 center, Vector3 normal, float r)
    {
        var (t1, t2) = RingTangents(normal);
        var lr = _rings[i];

        for (int s = 0; s <= SEGS; s++)
        {
            float a = s / (float)SEGS * Mathf.PI * 2f;
            lr.SetPosition(s, center + (t1 * Mathf.Cos(a) + t2 * Mathf.Sin(a)) * r);
        }

        bool   active = _drag == (Axis)(i + 1) || _hover == (Axis)(i + 1);
        Color[] def   = { C_X, C_Y, C_Z };
        _mats[i].color = active ? Color.white : def[i];
        float lw = _radius * (active ? LINE_RATIO_HV : LINE_RATIO);
        lr.startWidth = lr.endWidth = Mathf.Max(lw, 0.04f);
    }

    void HandleInput(Vector3 center)
    {
        var mouse = Mouse.current;
        if (mouse == null || Camera.main == null) return;

        Vector2 mp = mouse.position.ReadValue();

        if (_drag != Axis.None)
        {
            if (mouse.leftButton.isPressed)
            {
                Vector2 rawDelta = mp - _prevMp;
                // 0.5px 미만 미세 진동 제거
                if (rawDelta.sqrMagnitude < 0.25f) rawDelta = Vector2.zero;
                // 프레임 독립적 지수 스무딩 (응답 속도 k=25)
                float smooth = 1f - Mathf.Exp(-Time.deltaTime * 25f);
                _smoothDelta = Vector2.Lerp(_smoothDelta, rawDelta, smooth);
                ApplyRotation(_drag, _smoothDelta, center);
            }
            else
            {
                _drag = Axis.None;
                _smoothDelta = Vector2.zero;
                onDragEnd?.Invoke(_target);   // 바닥 스냅 등 후처리
            }
        }
        else
        {
            _hover = ClosestAxis(mp);
            if (mouse.leftButton.wasPressedThisFrame && _hover != Axis.None)
            {
                _drag = _hover;
                _smoothDelta = Vector2.zero;
            }
        }

        _prevMp = mp;
    }

    void ApplyRotation(Axis axis, Vector2 delta, Vector3 pivot)
    {
        float screenR  = GetScreenRadius(pivot);
        float degPerPx = Mathf.Clamp(360f / (2f * Mathf.PI * screenR), 0.15f, 2.5f);

        Vector3 worldAxis = axis switch
        {
            Axis.X => Vector3.right,
            Axis.Y => Vector3.up,
            Axis.Z => Vector3.forward,
            _      => Vector3.up
        };

        float angle;
        var cam = Camera.main;
        if (cam != null)
        {
            // 링 법선(worldAxis)과 카메라→피벗 방향의 외적 = 링의 스크린 탄젠트
            // 드래그를 이 탄젠트에 투영하면 "링을 잡고 돌리는" 자연스러운 감도 구현
            Vector3 camToPivot = (pivot - cam.transform.position).normalized;
            Vector3 tangent3D  = Vector3.Cross(worldAxis, camToPivot).normalized;

            if (tangent3D.sqrMagnitude > 0.01f)
            {
                Vector2 sc   = cam.WorldToScreenPoint(pivot);
                Vector2 se   = (Vector2)(Vector3)cam.WorldToScreenPoint(pivot + tangent3D);
                Vector2 sDir = (se - sc).sqrMagnitude > 0.25f
                             ? (se - sc).normalized
                             : Vector2.right;
                angle = Vector2.Dot(delta, sDir) * degPerPx;
            }
            else
            {
                // 링이 카메라 정면을 향할 때 (엣지케이스) — 반시계방향 기준
                angle = (delta.x - delta.y) * degPerPx * 0.5f;
            }
        }
        else
        {
            angle = axis switch
            {
                Axis.X =>  delta.y * degPerPx,
                Axis.Y =>  delta.x * degPerPx,
                Axis.Z => -delta.x * degPerPx,
                _      => 0f
            };
        }

        // 제자리 회전 — transform.position 변경 없이 오브젝트 자신의 피벗 기준으로 회전
        _target.transform.Rotate(worldAxis, angle, Space.World);
    }

    // 월드 반경 _radius 를 스크린 픽셀 단위로 변환
    float GetScreenRadius(Vector3 center)
    {
        var cam = Camera.main;
        if (cam == null) return 80f;
        Vector2 sc = (Vector2)(Vector3)cam.WorldToScreenPoint(center);
        Vector3 edgeWorld = center + cam.transform.right * _radius;
        Vector2 se = (Vector2)(Vector3)cam.WorldToScreenPoint(edgeWorld);
        return Mathf.Max(Vector2.Distance(sc, se), 20f);
    }

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
            if (Vector3.Dot(wp - cam.transform.position, cam.transform.forward) <= 0f) continue;
            float d = Vector2.Distance(mp, (Vector2)(Vector3)cam.WorldToScreenPoint(wp));
            if (d < min) min = d;
        }
        return min;
    }

    static (Vector3 t1, Vector3 t2) RingTangents(Vector3 normal)
    {
        Vector3 h  = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) < 0.99f
                   ? Vector3.up : Vector3.right;
        Vector3 t1 = Vector3.Cross(h, normal).normalized;
        Vector3 t2 = Vector3.Cross(t1, normal).normalized;
        return (t1, t2);
    }

    static Vector3 VisualCenter(GameObject obj)
    {
        var rs = obj.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return obj.transform.position + Vector3.up * 1.5f;
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        Vector3 p = obj.transform.position;
        return new Vector3(
            Mathf.Clamp(b.center.x, p.x - 20f, p.x + 20f),
            Mathf.Clamp(b.center.y, p.y,        p.y + 20f),
            Mathf.Clamp(b.center.z, p.z - 20f,  p.z + 20f)
        );
    }

    static float RingRadius(GameObject obj)
    {
        Vector3 center  = VisualCenter(obj);
        float   camDist = Camera.main != null
                        ? Vector3.Distance(Camera.main.transform.position, center)
                        : 8f;

        var rs = obj.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0)
            return Mathf.Clamp(camDist * 0.10f, 1.5f, 18f);

        var b = rs[0].bounds;
        foreach (var rv in rs) b.Encapsulate(rv.bounds);

        // bounds 비정상 (cm-scale FBX)
        if (b.extents.magnitude > camDist * 5f)
            return Mathf.Clamp(camDist * 0.10f, 1.5f, 18f);

        // 경계 구 반지름의 110% — 링이 모든 꼭짓점을 감쌈
        return Mathf.Clamp(b.extents.magnitude * 1.1f, 0.8f, 30f);
    }
}
