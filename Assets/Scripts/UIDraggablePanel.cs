using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 마우스로 드래그해서 위치를 옮길 수 있는 UI 패널.
/// CanvasScaler의 화면 비율 스케일을 보정해 드래그 거리를 anchoredPosition에 반영한다.
/// </summary>
public class UIDraggablePanel : MonoBehaviour, IDragHandler
{
    RectTransform _rt;
    Canvas _canvas;

    void Awake()
    {
        _rt = transform as RectTransform;
        _canvas = GetComponentInParent<Canvas>();
    }

    public void OnDrag(PointerEventData eventData)
    {
        float scale = (_canvas != null && _canvas.scaleFactor != 0f) ? _canvas.scaleFactor : 1f;
        _rt.anchoredPosition += eventData.delta / scale;
    }
}
