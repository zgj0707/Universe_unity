using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HistoryGraphZoom : MonoBehaviour, IScrollHandler
{
    public RectTransform viewport, content;
    public ScrollRect scroll;
    public float scale = 1f;
    public Action<float> onScaleChanged;
    public Action<bool> onPinch;
    bool pinching;
    float previousDistance;
    public void OnScroll(PointerEventData e)
    {
        if (Input.touchCount > 0 || Mathf.Approximately(e.scrollDelta.y, 0)) return;
        ApplyZoom(scale * Mathf.Pow(1.12f, e.scrollDelta.y), e.position);
    }
    void Update()
    {
        if (Input.touchCount >= 2)
        {
            var a = Input.GetTouch(0); var b = Input.GetTouch(1);
            float distance = Vector2.Distance(a.position, b.position);
            Vector2 center = (a.position + b.position) * 0.5f;
            if (!pinching)
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(viewport, center, null)) return;
                pinching = true; previousDistance = Mathf.Max(1f, distance); scroll.enabled = false; onPinch?.Invoke(true);
            }
            else if (distance > 1 && previousDistance > 1)
            {
                ApplyZoom(scale * distance / previousDistance, center);
                previousDistance = distance;
            }
        }
        else if (pinching) EndPinch();
    }
    void ApplyZoom(float target, Vector2 screenPoint)
    {
        if (!viewport || !content) return;
        float next = Mathf.Clamp(target, 0.45f, 2.0f);
        if (Mathf.Abs(next - scale) < 0.0001f) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(content, screenPoint, null, out var local)) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, screenPoint, null, out var desired)) return;
        scale = next; content.localScale = Vector3.one * scale;
        Vector2 actual = viewport.InverseTransformPoint(content.TransformPoint(local));
        content.anchoredPosition += desired - actual;
        onScaleChanged?.Invoke(scale);
    }
    void EndPinch()
    {
        pinching = false;
        if (scroll) scroll.enabled = true;
        onPinch?.Invoke(false);
    }
    void OnDisable() { if (pinching) EndPinch(); }
}

public class HistoryTooltipFollow : MonoBehaviour
{
    public RectTransform page, target;
    public CanvasGroup visibility;
    readonly Vector3[] corners = new Vector3[4];
    void LateUpdate()
    {
        if (visibility && visibility.alpha <= 0f) return;
        RefreshPosition();
    }

    public void RefreshPosition()
    {
        if (!page || !target) return;
        var tooltip = (RectTransform)transform;
        target.GetWorldCorners(corners);
        Vector2 topScreen = RectTransformUtility.WorldToScreenPoint(null, (corners[1] + corners[2]) * 0.5f);
        Vector2 bottomScreen = RectTransformUtility.WorldToScreenPoint(null, (corners[0] + corners[3]) * 0.5f);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(page, topScreen, null, out var top) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(page, bottomScreen, null, out var bottom)) return;

        Rect bounds = page.rect;
        Rect safe = Screen.safeArea;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(page, safe.min, null, out var safeMin) &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(page, safe.max, null, out var safeMax))
        {
            float left = Mathf.Max(bounds.xMin, Mathf.Min(safeMin.x, safeMax.x));
            float right = Mathf.Min(bounds.xMax, Mathf.Max(safeMin.x, safeMax.x));
            float bottomEdge = Mathf.Max(bounds.yMin, Mathf.Min(safeMin.y, safeMax.y));
            float topEdge = Mathf.Min(bounds.yMax, Mathf.Max(safeMin.y, safeMax.y));
            if (right > left && topEdge > bottomEdge) bounds = Rect.MinMaxRect(left, bottomEdge, right, topEdge);
        }

        const float margin = 24f; // 包括跨出纸页顶边的胶带
        // 顶部为宿主胶囊与导航预留空间，气泡不要进入该区域。
        if (bounds.height > 320f) bounds.yMax -= 160f;
        float availableWidth = Mathf.Max(1, bounds.width - 2 * margin);
        float availableHeight = Mathf.Max(1, bounds.height - 2 * margin);
        float factor = Mathf.Min(1f, availableWidth / tooltip.rect.width, availableHeight / tooltip.rect.height);
        tooltip.localScale = Vector3.one * factor;
        float width = tooltip.rect.width * factor, height = tooltip.rect.height * factor;
        float x = Mathf.Clamp(top.x, bounds.xMin + width / 2 + margin, bounds.xMax - width / 2 - margin);
        float y = top.y + 24;
        if (y + height > bounds.yMax - margin) y = bottom.y - height - 24;
        y = Mathf.Clamp(y, bounds.yMin + margin, bounds.yMax - height - margin);

        // 坐标转换得到的是父对象局部坐标；anchoredPosition 以锚点为原点。
        Vector2 anchor = new Vector2(Mathf.Lerp(page.rect.xMin, page.rect.xMax, tooltip.anchorMin.x),
                                     Mathf.Lerp(page.rect.yMin, page.rect.yMax, tooltip.anchorMin.y));
        tooltip.anchoredPosition = new Vector2(x, y) - anchor;
    }
}
