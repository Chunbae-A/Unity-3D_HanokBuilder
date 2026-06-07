using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// HanokBuilder 런타임 카메라 컨트롤러
/// 우클릭 드래그 = 회전, 중간 버튼 = 패닝, 휠 = 줌, F = 선택 오브젝트 포커스
/// </summary>
public class HanokCameraController : MonoBehaviour
{
    [Header("회전 (우클릭 드래그)")]
    public float rotateSpeed = 180f;

    [Header("패닝 (중간 버튼 드래그)")]
    public float panSpeed = 0.3f;

    [Header("줌 (마우스 휠)")]
    public float zoomSpeed = 8f;
    public float minDistance = 1f;
    public float maxDistance = 200f;

    [Header("WASD 이동")]
    public float moveSpeed = 10f;

    // 회전 중심점
    Vector3 _pivot;
    float   _distance;
    float   _yaw;
    float   _pitch;

    Vector2 _prevMouse;

    void Start()
    {
        // 초기 피벗: 카메라 전방 distance 거리
        _distance = Vector3.Distance(transform.position, Vector3.zero);
        if (_distance < 1f) _distance = 20f;

        // 초기 각도
        var angles = transform.eulerAngles;
        _yaw   = angles.y;
        _pitch = angles.x;

        _pivot = transform.position + transform.forward * _distance;
        RecalcPivot();
    }

    void LateUpdate()
    {
        // UI 위에서는 카메라 조작 무시
        bool overUI = EventSystem.current != null &&
                      EventSystem.current.IsPointerOverGameObject();

        var mouse  = Mouse.current;
        var kb     = Keyboard.current;
        if (mouse == null) return;

        Vector2 curMouse  = mouse.position.ReadValue();
        Vector2 mouseDelta = curMouse - _prevMouse;
        _prevMouse = curMouse;

        if (!overUI)
        {
            // ── 우클릭 드래그: 피벗 회전 ─────────────────────
            if (mouse.rightButton.isPressed)
            {
                _yaw   += mouseDelta.x * rotateSpeed * Time.deltaTime;
                _pitch -= mouseDelta.y * rotateSpeed * Time.deltaTime;
                _pitch  = Mathf.Clamp(_pitch, -80f, 80f);
            }

            // ── 중간 버튼 드래그: 패닝 ───────────────────────
            if (mouse.middleButton.isPressed)
            {
                Vector3 right = transform.right   * (-mouseDelta.x * panSpeed * _distance * 0.005f);
                Vector3 up    = transform.up      * (-mouseDelta.y * panSpeed * _distance * 0.005f);
                _pivot += right + up;
            }

            // ── 휠: 줌 ───────────────────────────────────────
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _distance -= scroll * zoomSpeed * Time.deltaTime * 20f;
                _distance  = Mathf.Clamp(_distance, minDistance, maxDistance);
            }
        }

        // ── WASD: 피벗 이동 ──────────────────────────────────
        if (kb != null && !overUI)
        {
            float spd = moveSpeed * Time.deltaTime;
            if (kb.leftShiftKey.isPressed) spd *= 3f;

            if (kb.wKey.isPressed) _pivot += transform.forward * spd;
            if (kb.sKey.isPressed) _pivot -= transform.forward * spd;
            if (kb.aKey.isPressed) _pivot -= transform.right   * spd;
            if (kb.dKey.isPressed) _pivot += transform.right   * spd;
            if (kb.eKey.isPressed) _pivot += Vector3.up        * spd;
            if (kb.qKey.isPressed) _pivot -= Vector3.up        * spd;
        }

        // ── F: 선택 오브젝트 포커스 ──────────────────────────
        if (kb != null && kb.fKey.wasPressedThisFrame)
            FocusSelected();

        // ── 카메라 위치/회전 적용 ────────────────────────────
        ApplyTransform();
    }

    void ApplyTransform()
    {
        Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
        transform.position = _pivot - rot * Vector3.forward * _distance;
        transform.rotation = rot;
    }

    void RecalcPivot()
    {
        var rot = Quaternion.Euler(_pitch, _yaw, 0f);
        _pivot  = transform.position + rot * Vector3.forward * _distance;
    }

    // ── 선택 오브젝트 포커스 (F키) ───────────────────────────
    public void FocusSelected()
    {
        var manager = FindFirstObjectByType<HanokUIManager>();
        if (manager == null) return;

        var target = manager.GetSelectedObject();
        if (target == null) return;

        var rends = target.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0)
        {
            _pivot = target.transform.position;
            _distance = 10f;
        }
        else
        {
            var b = rends[0].bounds;
            foreach (var r in rends) b.Encapsulate(r.bounds);
            _pivot    = b.center;
            _distance = b.size.magnitude * 1.5f;
            _distance = Mathf.Clamp(_distance, 2f, 80f);
        }
    }
}
