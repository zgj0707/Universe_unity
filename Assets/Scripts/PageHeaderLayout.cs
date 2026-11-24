using System.Collections.Generic;
using UnityEngine;

public class PageHeaderLayout : MonoBehaviour
{
    struct TopOffsetEntry
    {
        public RectTransform target;
        public Vector2 offsetFromSafeTopLeft;
    }

    public RectTransform page, back, home, title, help, coinBadge;
    public RectTransform topBarBg, topDivider, bottomSafeTarget;
    public float bottomSafeMargin = 20f;
    readonly List<TopOffsetEntry> extraTopLeftTargets = new List<TopOffsetEntry>();
    int lastScreenW = -1, lastScreenH = -1, lastExtraCount = -1;
    Rect lastSafeArea;
    Vector2 lastPageSize = new Vector2(-1f, -1f);

    public void AddSafeTopLeftTarget(RectTransform target, Vector2 offsetFromSafeTopLeft)
    {
        if (!target) return;
        target.anchorMin = target.anchorMax = Vector2.zero;
        target.pivot = new Vector2(0, 1);
        int existing = extraTopLeftTargets.FindIndex(entry => entry.target == target);
        var value = new TopOffsetEntry { target = target, offsetFromSafeTopLeft = offsetFromSafeTopLeft };
        if (existing >= 0) extraTopLeftTargets[existing] = value; else extraTopLeftTargets.Add(value);
        lastExtraCount = -1;
        Apply(true);
    }

    void OnEnable() { Apply(true); }

    void LateUpdate() { Apply(false); }

    void Apply(bool force)
    {
        if (!page) return;
        Rect r = page.rect;
        if (r.width < 1f || r.height < 1f) return;

        Rect safe = Screen.safeArea;
        if (safe.width <= 0f || safe.height <= 0f)
            safe = new Rect(0, 0, Screen.width, Screen.height);

        if (!force &&
            Screen.width == lastScreenW &&
            Screen.height == lastScreenH &&
            safe == lastSafeArea &&
            Mathf.Approximately(r.width, lastPageSize.x) &&
            Mathf.Approximately(r.height, lastPageSize.y) &&
            extraTopLeftTargets.Count == lastExtraCount)
            return;

        lastScreenW = Screen.width;
        lastScreenH = Screen.height;
        lastSafeArea = safe;
        lastPageSize = r.size;
        lastExtraCount = extraTopLeftTargets.Count;

        Vector2 safeMin = r.min, safeMax = r.max;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(page, new Vector2(safe.xMin, safe.yMin), null, out var pMin) &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(page, new Vector2(safe.xMax, safe.yMax), null, out var pMax))
        {
            safeMin = pMin;
            safeMax = pMax;
        }

        float sLeft = Mathf.Clamp(safeMin.x, r.xMin, Mathf.Max(r.xMin, r.xMax - 320f));
        float sRight = Mathf.Clamp(safeMax.x, Mathf.Min(r.xMax, sLeft + 320f), r.xMax);
        float sTop = Mathf.Clamp(safeMax.y, Mathf.Min(r.yMax, r.yMin + 220f), r.yMax);
        float sBottom = Mathf.Clamp(safeMin.y, r.yMin, Mathf.Max(r.yMin, sTop - 220f));

        Vector2 origin = r.min;
        Vector2 leftTop = new Vector2(sLeft, sTop) - origin;
        Vector2 rightTop = new Vector2(sRight, sTop) - origin;

        if (back) { back.anchorMin = back.anchorMax = Vector2.zero; back.pivot = new Vector2(0, 1); back.anchoredPosition = leftTop + new Vector2(24, -28); }
        if (home) { home.anchorMin = home.anchorMax = Vector2.zero; home.pivot = new Vector2(0, 1); home.anchoredPosition = leftTop + new Vector2(134, -28); }
        if (coinBadge) { coinBadge.anchorMin = coinBadge.anchorMax = Vector2.zero; coinBadge.pivot = new Vector2(1, 1); coinBadge.anchoredPosition = rightTop + new Vector2(-24, -28); }
        if (help) { help.anchorMin = help.anchorMax = Vector2.zero; help.pivot = new Vector2(0, 1); help.anchoredPosition = leftTop + new Vector2(254, -43); }
        if (title)
        {
            title.anchorMin = title.anchorMax = Vector2.zero;
            title.pivot = new Vector2(0, 1);
            float titleX = help ? 330 : 254;
            title.anchoredPosition = leftTop + new Vector2(titleX, -45);
            float rightPadding = coinBadge ? Mathf.Max(256f, coinBadge.sizeDelta.x + 36f) : 28f;
            title.sizeDelta = new Vector2(Mathf.Max(120f, rightTop.x - leftTop.x - titleX - rightPadding), 60);
        }

        if (topBarBg)
        {
            float divY = (sTop - 160f) - origin.y;
            float bgHeight = Mathf.Max(160f, (r.yMax - sTop) + 160f);
            topBarBg.anchorMin = topBarBg.anchorMax = Vector2.zero;
            topBarBg.pivot = Vector2.zero;
            topBarBg.anchoredPosition = new Vector2(0f, divY);
            topBarBg.sizeDelta = new Vector2(r.width, bgHeight);
        }
        if (topDivider)
        {
            float divY = (sTop - 160f) - origin.y;
            topDivider.anchorMin = topDivider.anchorMax = Vector2.zero;
            topDivider.pivot = Vector2.zero;
            topDivider.anchoredPosition = new Vector2(0f, divY);
            topDivider.sizeDelta = new Vector2(r.width, 3f);
        }

        for (int i = 0; i < extraTopLeftTargets.Count; i++)
        {
            var entry = extraTopLeftTargets[i];
            if (!entry.target) continue;
            entry.target.anchoredPosition = leftTop + entry.offsetFromSafeTopLeft;
        }

        if (bottomSafeTarget)
        {
            float botOffset = (sBottom - origin.y) + bottomSafeMargin;
            bottomSafeTarget.anchoredPosition = new Vector2(bottomSafeTarget.anchoredPosition.x, botOffset);
        }
    }
}
