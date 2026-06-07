using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// HanokBuilder 카메라 컨트롤러 (Blender / Unity Editor / SketchUp 관행 적용)
///
/// 오비트  : 우클릭 드래그 | Alt + 좌클릭 드래그
/// 패닝    : 중간버튼 드래그 | Shift + 우클릭 드래그
/// 줌      : 스크롤 (커서 방향으로) | Ctrl + 스크롤 (정밀)
/// 포커스  : F — 선택 오브젝트 | Z — 모든 오브젝트 프레임
/// 초기화  : Home
/// 뷰 전환 : Numpad 1=앞 | 3=우 | 7=위 | 0/5=원근
/// </summary>
public class HanokCameraController : MonoBehaviour
{
    public enum ViewPreset { Perspective, Top, Front, Back, Right, Left }

    [Header("속도")] public float rotateSpeed = 150f;
                     public float panSpeed    = 0.30f;
                     public float moveSpeed   = 12f;
    [Header("줌")]  public float minDistance = 0.08f;
                     public float maxDistance = 300f;
    [Header("포커스")] public float focusLerp = 14f;

    // 오비트 상태
    Vector3 _pivot;
    float   _dist, _yaw, _pitch;

    // 포커스 보간 타겟
    Vector3 _tPivot;
    float   _tDist;
    bool    _focusing;

    bool       _ortho;
    ViewPreset _preset = ViewPreset.Perspective;
    Vector2    _prevMouse;

    const float DEF_PITCH = 18f;
    const float DEF_YAW   = -40f;
    const float DEF_DIST  = 22f;

    public ViewPreset CurrentPreset => _preset;
    public Vector3    Pivot         => _pivot;

    void Start()
    {
        _pitch = DEF_PITCH; _yaw = DEF_YAW;
        _dist  = DEF_DIST;  _pivot = Vector3.zero;
        _tPivot = _pivot;   _tDist = _dist;
        Apply();
    }

    void LateUpdate()
    {
        bool overUI = EventSystem.current != null &&
                      EventSystem.current.IsPointerOverGameObject();
        var mouse = Mouse.current;
        var kb    = Keyboard.current;
        if (mouse == null) return;

        Vector2 cur   = mouse.position.ReadValue();
        Vector2 delta = cur - _prevMouse;
        _prevMouse = cur;

        // ── 포커스 보간 ──────────────────────────────────
        if (_focusing)
        {
            float t = focusLerp * Time.deltaTime;
            _pivot = Vector3.Lerp(_pivot, _tPivot, t);
            _dist  = Mathf.Lerp(_dist,   _tDist,  t);
            SyncOrtho();
            if (Vector3.Distance(_pivot, _tPivot) < 0.015f &&
                Mathf.Abs(_dist - _tDist) < 0.015f)
            { _pivot = _tPivot; _dist = _tDist; _focusing = false; SyncOrtho(); }
        }

        if (!overUI)
        {
            bool altHeld   = kb != null && kb.altKey.isPressed;
            bool shiftHeld = kb != null && kb.leftShiftKey.isPressed;
            bool ctrlHeld  = kb != null && kb.ctrlKey.isPressed;

            // ── 오비트: 우클릭 or Alt+좌클릭 ────────────
            bool orbiting = mouse.rightButton.isPressed && !shiftHeld
                          || mouse.leftButton.isPressed && altHeld;
            if (orbiting)
            {
                if (_ortho) SetViewPreset(ViewPreset.Perspective);
                else
                {
                    _yaw   += delta.x * rotateSpeed * Time.deltaTime;
                    _pitch -= delta.y * rotateSpeed * Time.deltaTime;
                    _pitch  = Mathf.Clamp(_pitch, 5f, 85f);
                    _focusing = false;
                }
            }

            // ── 패닝: 중간버튼 or Shift+우클릭 ──────────
            bool panning = mouse.middleButton.isPressed
                         || mouse.rightButton.isPressed && shiftHeld;
            if (panning)
            {
                float   s = _dist * (_ortho ? 0.008f : 0.004f) * panSpeed;
                Vector3 r = transform.right * (-delta.x * s);
                Vector3 u = OrthoUp()       * (-delta.y * s);
                _pivot  += r + u;
                _tPivot  = _pivot;
                _focusing = false;
            }

            // ── 줌: 스크롤 (커서 방향으로) ───────────────
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                ZoomToCursor(scroll, cur, ctrlHeld);
        }

        // ── 키보드 이동 (WASD/QE) ────────────────────────
        if (kb != null && !overUI)
        {
            bool shiftHeld = kb.leftShiftKey.isPressed;
            float spd = moveSpeed * Time.deltaTime * (shiftHeld ? 3f : 1f);

            Vector3 fwd   = _ortho ? OrthoFwd() : transform.forward;
            Vector3 right = transform.right;
            bool moved = false;
            if (kb.wKey.isPressed) { _pivot += fwd    * spd; moved = true; }
            if (kb.sKey.isPressed) { _pivot -= fwd    * spd; moved = true; }
            if (kb.aKey.isPressed) { _pivot -= right  * spd; moved = true; }
            if (kb.dKey.isPressed) { _pivot += right  * spd; moved = true; }
            if (kb.eKey.isPressed) { _pivot += Vector3.up * spd; moved = true; }
            if (kb.qKey.isPressed) { _pivot -= Vector3.up * spd; moved = true; }
            if (moved) { _tPivot = _pivot; _focusing = false; }

            // ── 단축키 ───────────────────────────────────
            if (kb.fKey.wasPressedThisFrame)    FocusSelected();
            if (kb.zKey.wasPressedThisFrame)    FrameAll();
            if (kb.homeKey.wasPressedThisFrame) ResetView();

            // Numpad 뷰 전환 (CAD / Blender 표준)
            if (kb.numpad1Key.wasPressedThisFrame) SetViewPreset(ViewPreset.Front);
            if (kb.numpad3Key.wasPressedThisFrame) SetViewPreset(ViewPreset.Right);
            if (kb.numpad7Key.wasPressedThisFrame) SetViewPreset(ViewPreset.Top);
            if (kb.numpad9Key.wasPressedThisFrame) SetViewPreset(ViewPreset.Back);
            if (kb.numpad5Key.wasPressedThisFrame || kb.numpad0Key.wasPressedThisFrame)
                SetViewPreset(ViewPreset.Perspective);
        }

        Apply();
    }

    // ── 줌 → 커서 (Blender / SketchUp 표준) ─────────────────
    void ZoomToCursor(float scroll, Vector2 screenPos, bool finePrecision)
    {
        float oldDist = _dist;
        float mul = finePrecision ? 0.03f : 0.12f; // Ctrl = 정밀 줌
        _dist = Mathf.Clamp(_dist - scroll * _dist * mul, minDistance, maxDistance);
        _tDist = _dist; _focusing = false;
        SyncOrtho();

        // 원근 모드에서만 커서 방향으로 피벗 이동
        if (scroll > 0f && !_ortho && Camera.main != null)
        {
            Ray   ray    = Camera.main.ScreenPointToRay(screenPos);
            var   ground = new Plane(Vector3.up, Vector3.zero);
            float maxRay = _dist * 3f;
            Vector3 hitPt = ground.Raycast(ray, out float t)
                          ? ray.GetPoint(Mathf.Clamp(t, 0.01f, maxRay))
                          : Camera.main.transform.position + ray.direction * _dist;

            float zoomRatio = 1f - _dist / oldDist; // 줌인 비율
            _pivot = Vector3.Lerp(_pivot, hitPt, zoomRatio * 0.45f);
            _tPivot = _pivot;
        }
    }

    // ── 방향 보조 ────────────────────────────────────────────
    Vector3 OrthoUp()  => _preset == ViewPreset.Top ? Vector3.forward : Vector3.up;
    Vector3 OrthoFwd() => _preset == ViewPreset.Top ? Vector3.forward : Vector3.up;

    // ── 카메라 행렬 적용 ─────────────────────────────────────
    void Apply()
    {
        var rot = Quaternion.Euler(_pitch, _yaw, 0f);
        transform.position = _pivot - rot * Vector3.forward * _dist;
        transform.rotation = rot;
    }

    void SyncOrtho()
    {
        if (_ortho && Camera.main != null)
            Camera.main.orthographicSize = Mathf.Max(_dist * 0.5f, 0.1f);
    }

    // ── 뷰 프리셋 전환 ───────────────────────────────────────
    public void SetViewPreset(ViewPreset preset)
    {
        _preset = preset; _focusing = false;
        switch (preset)
        {
            case ViewPreset.Top:         _pitch = 89.9f; _yaw =   0f; _ortho = true;  break;
            case ViewPreset.Front:       _pitch =  0.5f; _yaw =   0f; _ortho = true;  break;
            case ViewPreset.Back:        _pitch =  0.5f; _yaw = 180f; _ortho = true;  break;
            case ViewPreset.Right:       _pitch =  0.5f; _yaw =  90f; _ortho = true;  break;
            case ViewPreset.Left:        _pitch =  0.5f; _yaw = -90f; _ortho = true;  break;
            case ViewPreset.Perspective: _pitch = DEF_PITCH; _yaw = DEF_YAW; _ortho = false; break;
        }
        if (Camera.main != null)
        {
            Camera.main.orthographic = _ortho;
            if (_ortho) { _dist = Mathf.Max(_dist, 5f); SyncOrtho(); }
            else        { Camera.main.fieldOfView = 55f; }
        }
        Apply();
    }

    // ── 공개 API ─────────────────────────────────────────────

    public void FocusSelected()
    {
        var mgr = FindFirstObjectByType<HanokUIManager>();
        if (mgr == null) return;
        var t = mgr.GetSelectedObject();
        if (t != null) FocusObject(t);
    }

    public void FocusObject(GameObject target)
    {
        if (target == null) return;
        var rends = target.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) { _tPivot = target.transform.position; _tDist = 8f; }
        else
        {
            var b = rends[0].bounds;
            foreach (var r in rends) b.Encapsulate(r.bounds);
            _tPivot = b.center;
            float maxE = Mathf.Max(b.size.x, b.size.y, b.size.z);
            _tDist = Mathf.Clamp(maxE * 3f, 3f, 80f);
        }
        if (!_ortho && _pitch < 25f) _pitch = DEF_PITCH;
        _focusing = true;
    }

    /// <summary>
    /// 배치된 오브젝트 전체를 한 화면에 — Z 단축키
    /// </summary>
    public void FrameAll()
    {
        var assets = Object.FindObjectsByType<SelectableAsset>(FindObjectsSortMode.None);
        if (assets.Length == 0) { ResetView(); return; }

        bool any = false;
        Bounds b = default;
        foreach (var a in assets)
            foreach (var r in a.GetComponentsInChildren<Renderer>())
            {
                if (!any) { b = r.bounds; any = true; }
                else b.Encapsulate(r.bounds);
            }

        if (!any) { ResetView(); return; }

        // XZ 중심에 맞추되 Y는 0으로 (마당 수평 유지)
        _tPivot = new Vector3(b.center.x, 0f, b.center.z);
        float maxE = Mathf.Max(b.size.x, b.size.y, b.size.z);
        _tDist  = Mathf.Clamp(maxE * 2.5f, 5f, 100f);
        if (!_ortho) { _pitch = DEF_PITCH; _yaw = DEF_YAW; }
        _focusing = true;
    }

    /// <summary>
    /// 오브젝트 선택 시 피벗을 부드럽게 해당 위치 방향으로 이동.
    /// 급격한 카메라 점프 없이 오비트 중심을 자연스럽게 조정.
    /// blend=0.2 → 피벗이 20% 이동 (자연스러운 값)
    /// </summary>
    public void ShiftPivotToward(Vector3 worldPos, float blend = 0.20f)
    {
        Vector3 target = new Vector3(worldPos.x, 0f, worldPos.z);
        _pivot  = Vector3.Lerp(_pivot, target, blend);
        _tPivot = _pivot;
    }

    public void ResetView()
    {
        SetViewPreset(ViewPreset.Perspective);
        _tPivot = Vector3.zero; _tDist = DEF_DIST;
        _pitch  = DEF_PITCH;   _yaw   = DEF_YAW;
        _focusing = true;
    }
}
