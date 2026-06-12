using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 런타임 3축 스케일 기즈모 — Unity 에디터 스타일
/// X(빨강)·Y(초록)·Z(파랑) 큐브 핸들 + 중앙 흰색 구 (균일 스케일)
/// renderQueue 4000으로 항상 오브젝트 위에 렌더링
/// </summary>
public class HanokScaleGizmo : MonoBehaviour
{
    public enum Axis { None, X, Y, Z, All }

    const float HIT_PX     = 22f;    // 클릭 판정 반경 (픽셀)
    const float HANDLE_EXT = 1.30f;  // 바운딩 구 반경 × 이 배율 = 핸들 길이
    const float CUBE_FRAC  = 0.18f;  // 핸들 큐브 크기 = 핸들 길이 × 이 값
    const float LINE_FRAC  = 0.022f; // 선 두께

    static readonly Color C_X = new Color(0.92f, 0.22f, 0.18f);
    static readonly Color C_Y = new Color(0.18f, 0.82f, 0.28f);
    static readonly Color C_Z = new Color(0.20f, 0.48f, 0.96f);
    static readonly Color C_W = new Color(0.88f, 0.88f, 0.88f);

    GameObject     _target;
    GameObject     _root;
    LineRenderer[] _lines   = new LineRenderer[3];
    Renderer[]     _handles = new Renderer[4];   // 0-2=축 큐브, 3=중앙 구
    Material[]     _mats    = new Material[4];

    Axis    _hover  = Axis.None;
    Axis    _drag   = Axis.None;
    Vector2 _prevMp;
    Vector2 _smooth;
    float   _handleLen;
    Vector3 _center;

    public System.Action<GameObject> onDragEnd;
    public bool IsConsuming => _drag != Axis.None;

    /// <summary>Update에서 호출 — 이 마우스 위치가 핸들 위인지 확인 (클릭 우선권 판정)</summary>
    public bool WouldCapture(Vector2 mp)
        => _root != null && _root.activeSelf && GetHoverAxis(mp) != Axis.None;

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
        _center    = VisualCenter(_target);
        _handleLen = CalcHandleLen(_target);
        RefreshVisuals();
        HandleInput();
    }

    // ── 빌드 ────────────────────────────────────────────────
    void Build()
    {
        _root = new GameObject("_HanokScaleGizmo");
        _root.transform.SetParent(transform, false);

        var shader = Shader.Find("Universal Render Pipeline/Unlit")
                  ?? Shader.Find("Unlit/Color");
        Color[] cols = { C_X, C_Y, C_Z, C_W };

        for (int i = 0; i < 4; i++)
        {
            _mats[i] = new Material(shader) { color = cols[i] };
            _mats[i].renderQueue = 4000;
        }

        // 3축 선 ─────────────────────────────────────────────
        for (int i = 0; i < 3; i++)
        {
            var go = new GameObject("SLine" + i);
            go.transform.SetParent(_root.transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial       = _mats[i];
            lr.useWorldSpace        = true;
            lr.positionCount        = 2;
            lr.startWidth = lr.endWidth = 0.04f;
            lr.shadowCastingMode    = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows       = false;
            lr.generateLightingData = false;
            _lines[i] = lr;
        }

        // 축 큐브 3개 + 중앙 구 1개 ───────────────────────────
        var primitives = new PrimitiveType[]
        {
            PrimitiveType.Cube, PrimitiveType.Cube, PrimitiveType.Cube,
            PrimitiveType.Sphere
        };
        for (int i = 0; i < 4; i++)
        {
            var go = GameObject.CreatePrimitive(primitives[i]);
            go.name = i < 3 ? "SCube" + i : "SCenter";
            go.transform.SetParent(_root.transform, false);
            var col = go.GetComponent<Collider>();
            if (col) Destroy(col);
            go.GetComponent<Renderer>().sharedMaterial = _mats[i];
            _handles[i] = go.GetComponent<Renderer>();
        }
    }

    // ── 비주얼 갱신 ─────────────────────────────────────────
    void RefreshVisuals()
    {
        Color[] def    = { C_X, C_Y, C_Z, C_W };
        Vector3[] axes = { Vector3.right, Vector3.up, Vector3.forward };

        for (int i = 0; i < 3; i++)
        {
            Vector3 end = _center + axes[i] * _handleLen;

            _lines[i].SetPosition(0, _center);
            _lines[i].SetPosition(1, end);
            float lw = Mathf.Max(_handleLen * LINE_FRAC, 0.02f);
            _lines[i].startWidth = _lines[i].endWidth = lw;

            float cs = Mathf.Max(_handleLen * CUBE_FRAC, 0.05f);
            _handles[i].transform.position  = end;
            _handles[i].transform.localScale = Vector3.one * cs;

            bool active = _hover == (Axis)(i + 1) || _drag == (Axis)(i + 1);
            _mats[i].color = active ? Color.white : def[i];
        }

        // 중앙 구
        float ss = Mathf.Max(_handleLen * CUBE_FRAC * 0.75f, 0.04f);
        _handles[3].transform.position  = _center;
        _handles[3].transform.localScale = Vector3.one * ss;
        bool ca = _hover == Axis.All || _drag == Axis.All;
        _mats[3].color = ca ? Color.yellow : C_W;
    }

    // ── 입력 처리 ────────────────────────────────────────────
    void HandleInput()
    {
        var mouse = Mouse.current;
        if (mouse == null || Camera.main == null) return;

        Vector2 mp = mouse.position.ReadValue();

        if (_drag != Axis.None)
        {
            if (mouse.leftButton.isPressed)
            {
                Vector2 raw = mp - _prevMp;
                float s = 1f - Mathf.Exp(-Time.deltaTime * 20f);
                _smooth = Vector2.Lerp(_smooth, raw, s);
                ApplyScale(_drag, _smooth);
            }
            else
            {
                _drag   = Axis.None;
                _smooth = Vector2.zero;
                onDragEnd?.Invoke(_target);   // 바닥 스냅 등 후처리
            }
        }
        else
        {
            _hover = GetHoverAxis(mp);
            if (mouse.leftButton.wasPressedThisFrame && _hover != Axis.None)
            {
                _drag   = _hover;
                _smooth = Vector2.zero;
            }
        }

        _prevMp = mp;
    }

    // ── 스케일 적용 ─────────────────────────────────────────
    void ApplyScale(Axis axis, Vector2 delta)
    {
        if (_target == null || delta.sqrMagnitude < 0.02f) return;

        var cam = Camera.main;
        Vector3 scale = _target.transform.localScale;

        if (axis == Axis.All)
        {
            float len = ScreenLen(Vector3.right);
            float f   = (delta.x + delta.y) / Mathf.Max(len, 40f) * 1.8f;
            scale = Vector3.Max(scale * (1f + f), Vector3.one * 0.02f);
        }
        else
        {
            Vector3 worldAxis = axis switch
            {
                Axis.X => Vector3.right,
                Axis.Y => Vector3.up,
                Axis.Z => Vector3.forward,
                _      => Vector3.right
            };
            float len  = ScreenLen(worldAxis);
            Vector2 sc = cam.WorldToScreenPoint(_center);
            Vector2 se = cam.WorldToScreenPoint(_center + worldAxis * _handleLen);
            Vector2 dir = (se - sc).sqrMagnitude > 4f
                        ? (se - sc).normalized
                        : Vector2.right;

            float proj = Vector2.Dot(delta, dir);
            float f    = proj / Mathf.Max(len, 30f) * 2f;

            if (axis == Axis.X) scale.x = Mathf.Max(scale.x * (1f + f), 0.02f);
            if (axis == Axis.Y) scale.y = Mathf.Max(scale.y * (1f + f), 0.02f);
            if (axis == Axis.Z) scale.z = Mathf.Max(scale.z * (1f + f), 0.02f);
        }

        _target.transform.localScale = scale;
    }

    // ── 히트 판정 ────────────────────────────────────────────
    Axis GetHoverAxis(Vector2 mp)
    {
        if (Camera.main == null) return Axis.None;

        Vector3[] ends = {
            _center + Vector3.right   * _handleLen,
            _center + Vector3.up      * _handleLen,
            _center + Vector3.forward * _handleLen,
        };

        float best = HIT_PX;
        Axis  res  = Axis.None;

        for (int i = 0; i < 3; i++)
        {
            Vector3 sp = Camera.main.WorldToScreenPoint(ends[i]);
            if (sp.z <= 0f) continue;
            float d = Vector2.Distance(mp, new Vector2(sp.x, sp.y));
            if (d < best) { best = d; res = (Axis)(i + 1); }
        }

        // 중앙 구 — 더 작은 히트 반경으로 축 큐브보다 우선순위 낮음
        Vector3 cp = Camera.main.WorldToScreenPoint(_center);
        if (cp.z > 0f)
        {
            float cd = Vector2.Distance(mp, new Vector2(cp.x, cp.y));
            if (cd < HIT_PX * 0.65f && cd < best) res = Axis.All;
        }

        return res;
    }

    float ScreenLen(Vector3 worldAxis)
    {
        if (Camera.main == null) return 80f;
        Vector2 sc = Camera.main.WorldToScreenPoint(_center);
        Vector2 se = Camera.main.WorldToScreenPoint(_center + worldAxis * _handleLen);
        return Mathf.Max(Vector2.Distance(sc, se), 20f);
    }

    // ── 도우미 ───────────────────────────────────────────────
    static Vector3 VisualCenter(GameObject obj)
    {
        var rs = obj.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return obj.transform.position + Vector3.up;
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b.center;
    }

    static float CalcHandleLen(GameObject obj)
    {
        var cam = Camera.main;
        Vector3 c  = VisualCenter(obj);
        float cd   = cam != null ? Vector3.Distance(cam.transform.position, c) : 8f;
        var rs     = obj.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return Mathf.Clamp(cd * 0.10f, 0.5f, 15f);
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        if (b.extents.magnitude > cd * 5f) return Mathf.Clamp(cd * 0.10f, 0.5f, 15f);
        return Mathf.Clamp(b.extents.magnitude * HANDLE_EXT, 0.4f, 25f);
    }
}
