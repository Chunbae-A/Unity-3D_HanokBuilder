using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// HanokBuilder 카메라 컨트롤러
/// - 원근(3D): 우클릭=회전, 중간버튼=패닝, 휠=줌, WASD/QE=이동
/// - 직교(위/앞/뒤/우/좌): 우클릭하면 원근으로 자동 전환
/// - F=포커스, Home=뷰 초기화
/// </summary>
public class HanokCameraController : MonoBehaviour
{
    // ── 뷰 프리셋 ────────────────────────────────────────
    public enum ViewPreset { Perspective, Top, Front, Back, Right, Left }

    [Header("회전 속도")] public float rotateSpeed  = 150f;
    [Header("패닝 속도")] public float panSpeed     = 0.25f;
    [Header("줌 속도")]   public float zoomSpeed    = 10f;
    [Header("줌 범위")]   public float minDistance  = 0.5f;
                          public float maxDistance  = 300f;
    [Header("이동 속도")] public float moveSpeed    = 12f;
    [Header("포커스")]    public float focusLerp    = 14f;

    // 오비트 상태
    Vector3 _pivot;
    float   _dist;
    float   _yaw;
    float   _pitch;

    // 포커스 타겟
    Vector3 _tPivot;
    float   _tDist;
    bool    _focusing;

    bool       _ortho;
    ViewPreset _preset = ViewPreset.Perspective;
    Vector2    _prevMouse;

    const float DEF_PITCH = 18f;
    const float DEF_YAW   = -40f;
    const float DEF_DIST  = 22f;

    // ── 외부 접근 ────────────────────────────────────────
    public ViewPreset CurrentPreset => _preset;
    public Vector3    Pivot         => _pivot;

    // ── 초기화 ───────────────────────────────────────────
    void Start()
    {
        _pitch  = DEF_PITCH;
        _yaw    = DEF_YAW;
        _dist   = DEF_DIST;
        _pivot  = Vector3.zero;
        _tPivot = _pivot;
        _tDist  = _dist;
        Apply();
    }

    // ── 메인 루프 ─────────────────────────────────────────
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

        // 포커스 애니메이션
        if (_focusing)
        {
            float t = focusLerp * Time.deltaTime;
            _pivot  = Vector3.Lerp(_pivot, _tPivot, t);
            _dist   = Mathf.Lerp(_dist,   _tDist,  t);
            SyncOrtho();
            if (Vector3.Distance(_pivot, _tPivot) < 0.02f &&
                Mathf.Abs(_dist - _tDist) < 0.02f)
            {
                _pivot = _tPivot; _dist = _tDist; _focusing = false; SyncOrtho();
            }
        }

        if (!overUI)
        {
            // 우클릭: 회전 (ortho → perspective 자동 전환)
            if (mouse.rightButton.isPressed)
            {
                if (_ortho) { SetViewPreset(ViewPreset.Perspective); }
                else
                {
                    _yaw   += delta.x * rotateSpeed * Time.deltaTime;
                    _pitch -= delta.y * rotateSpeed * Time.deltaTime;
                    _pitch  = Mathf.Clamp(_pitch, 5f, 85f);
                    _focusing = false;
                }
            }

            // 중간 버튼: 패닝
            if (mouse.middleButton.isPressed)
            {
                float   s = _dist * (_ortho ? 0.008f : 0.004f) * panSpeed;
                Vector3 r = transform.right  * (-delta.x * s);
                Vector3 u = OrthoUp()        * (-delta.y * s);
                _pivot  += r + u;
                _tPivot  = _pivot;
                _focusing = false;
            }

            // 휠: 줌
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _dist   -= scroll * zoomSpeed * Time.deltaTime * 18f;
                _dist    = Mathf.Clamp(_dist, minDistance, maxDistance);
                _tDist   = _dist;
                _focusing = false;
                SyncOrtho();
            }
        }

        // WASD/QE 이동
        if (kb != null && !overUI)
        {
            float spd = moveSpeed * Time.deltaTime;
            if (kb.leftShiftKey.isPressed) spd *= 3f;

            Vector3 fwd   = _ortho ? OrthoFwd() : transform.forward;
            Vector3 right = transform.right;

            bool moved = false;
            if (kb.wKey.isPressed) { _pivot += fwd   * spd; moved = true; }
            if (kb.sKey.isPressed) { _pivot -= fwd   * spd; moved = true; }
            if (kb.aKey.isPressed) { _pivot -= right * spd; moved = true; }
            if (kb.dKey.isPressed) { _pivot += right * spd; moved = true; }
            if (kb.eKey.isPressed) { _pivot += Vector3.up * spd; moved = true; }
            if (kb.qKey.isPressed) { _pivot -= Vector3.up * spd; moved = true; }
            if (moved) { _tPivot = _pivot; _focusing = false; }
        }

        // F: 포커스
        if (kb != null && kb.fKey.wasPressedThisFrame) FocusSelected();

        Apply();
    }

    // 뷰에 따른 화면 UP/FWD 방향 (ortho 패닝/이동에 사용)
    Vector3 OrthoUp()
    {
        return _preset switch
        {
            ViewPreset.Top   => Vector3.forward,
            _                => Vector3.up,
        };
    }
    Vector3 OrthoFwd()
    {
        return _preset switch
        {
            ViewPreset.Top   => Vector3.forward,
            _                => Vector3.up,
        };
    }

    // ── 카메라 행렬 적용 ─────────────────────────────────
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

    // ── 뷰 프리셋 전환 ───────────────────────────────────
    public void SetViewPreset(ViewPreset preset)
    {
        _preset   = preset;
        _focusing = false;

        switch (preset)
        {
            case ViewPreset.Top:
                _pitch = 89.9f; _yaw = 0f;   _ortho = true;  break;
            case ViewPreset.Front:
                _pitch = 0.5f;  _yaw = 0f;   _ortho = true;  break;
            case ViewPreset.Back:
                _pitch = 0.5f;  _yaw = 180f; _ortho = true;  break;
            case ViewPreset.Right:
                _pitch = 0.5f;  _yaw = 90f;  _ortho = true;  break;
            case ViewPreset.Left:
                _pitch = 0.5f;  _yaw = -90f; _ortho = true;  break;
            case ViewPreset.Perspective:
                _pitch = DEF_PITCH; _yaw = DEF_YAW; _ortho = false; break;
        }

        if (Camera.main != null)
        {
            Camera.main.orthographic = _ortho;
            if (_ortho) { _dist = Mathf.Max(_dist, 5f); SyncOrtho(); }
            else        { Camera.main.fieldOfView = 55f; }
        }

        Apply();
    }

    // ── 공개 메서드 ──────────────────────────────────────

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
        if (rends.Length == 0)
        {
            _tPivot = target.transform.position;
            _tDist  = 8f;
        }
        else
        {
            var b = rends[0].bounds;
            foreach (var r in rends) b.Encapsulate(r.bounds);
            _tPivot = b.center;
            float maxE = Mathf.Max(b.size.x, b.size.y, b.size.z);
            _tDist  = Mathf.Clamp(maxE * 3f, 3f, 80f);
        }
        if (!_ortho && _pitch < 25f) _pitch = DEF_PITCH;
        _focusing = true;
    }

    public void ResetView()
    {
        SetViewPreset(ViewPreset.Perspective);
        _tPivot  = Vector3.zero;
        _tDist   = DEF_DIST;
        _pitch   = DEF_PITCH;
        _yaw     = DEF_YAW;
        _focusing = true;
    }
}
