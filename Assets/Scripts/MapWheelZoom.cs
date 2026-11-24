using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class MapWheelZoom : MonoBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public RectTransform map, viewport;
    public Func<bool> canZoom;
    public Action onPanStart;
    public Func<float> maxZoom;
    public Func<float> minZoom;
    public Func<Vector2> panMarginRatio;
    const float DefaultMinZoom = 1f;
    float pinchDistance;
    bool panning;
    Vector2 previous;
    public Vector2 CenterPosition(float scale = 1f)
    {
        if (!map || !viewport) return Vector2.zero;
        float vw = viewport.rect.width, vh = viewport.rect.height;
        float mw = map.rect.width, mh = map.rect.height;
        return new Vector2((vw - mw * scale) * 0.5f, (vh - mh * scale) * 0.5f);
    }
    public Vector2 ClampPosition(Vector2 position, float scale)
    {
        if (!map) return position;
        float mw = map.rect.width, mh = map.rect.height;
        float vw = viewport ? viewport.rect.width : mw;
        float vh = viewport ? viewport.rect.height : mh;
        float scaledW = mw * scale, scaledH = mh * scale;
        Vector2 ratio = panMarginRatio != null ? panMarginRatio() : new Vector2(0.25f, 0.25f);
        float marginX = mw * ratio.x, marginY = mh * ratio.y;
        float minX = scaledW >= vw ? (vw - scaledW - marginX) : ((vw - scaledW) * 0.5f - marginX);
        float maxX = scaledW >= vw ? marginX : ((vw - scaledW) * 0.5f + marginX);
        float minY = scaledH >= vh ? (vh - scaledH - marginY) : ((vh - scaledH) * 0.5f - marginY);
        float maxY = scaledH >= vh ? marginY : ((vh - scaledH) * 0.5f + marginY);
        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);
        return position;
    }
    public void PanBy(Vector2 delta)
    {
        if (!map) return;
        map.anchoredPosition = ClampPosition(map.anchoredPosition + delta, map.localScale.x);
    }
    void ZoomAt(Vector2 screenPoint, float next)
    {
        float current = map.localScale.x;
        float minZ = minZoom != null ? minZoom() : DefaultMinZoom;
        next = Mathf.Clamp(next, minZ, maxZoom != null ? maxZoom() : 2.5f);
        if (Mathf.Approximately(next, current)) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(map, screenPoint, null, out var local)) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, screenPoint, null, out var desired)) return;
        map.localScale = Vector3.one * next;
        Vector2 actual = viewport.InverseTransformPoint(map.TransformPoint(local));
        map.anchoredPosition = ClampPosition(map.anchoredPosition + desired - actual, next);
    }
    public void OnScroll(PointerEventData e)
    {
        if (!map || !viewport || (canZoom != null && !canZoom()) || Mathf.Approximately(e.scrollDelta.y, 0)) return;
        ZoomAt(e.position, map.localScale.x * Mathf.Pow(1.12f, e.scrollDelta.y));
    }
    void Update()
    {
        if (Input.touchCount != 2 || !map || !viewport || (canZoom != null && !canZoom())) { pinchDistance = 0; return; }
        Touch a = Input.GetTouch(0), b = Input.GetTouch(1);
        float distance = Vector2.Distance(a.position, b.position);
        if (pinchDistance > 1f && distance > 1f)
            ZoomAt((a.position + b.position) * 0.5f, map.localScale.x * distance / pinchDistance);
        else { panning = false; onPanStart?.Invoke(); }
        pinchDistance = distance;
    }
    public void OnBeginDrag(PointerEventData e)
    {
        if (!map || !viewport || Input.touchCount >= 2 || (canZoom != null && !canZoom())) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, e.position, null, out previous)) return;
        panning = true; onPanStart?.Invoke();
    }
    public void OnDrag(PointerEventData e)
    {
        if (!panning || (canZoom != null && !canZoom())) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, e.position, null, out var point)) return;
        map.anchoredPosition = ClampPosition(map.anchoredPosition + point - previous, map.localScale.x);
        previous = point;
    }
    public void OnEndDrag(PointerEventData e) { panning = false; }
    void OnDisable() { panning = false; pinchDistance = 0; }
}
