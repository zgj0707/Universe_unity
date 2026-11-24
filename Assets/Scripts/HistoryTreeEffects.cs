using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HistoryNodeTouch : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    public Action onTap, onHold, onRelease;
    public bool requireDoubleTap;
    bool pressed, held, moved;
    float startTime;
    float lastTapTime = -10f;
    Vector2 startPosition;
    const float DoubleTapWindow = 0.36f;

    public void OnPointerDown(PointerEventData eventData)
    {
        pressed = true; held = false; moved = false; startTime = Time.unscaledTime; startPosition = eventData.position;
    }
    void Update()
    {
        if (!pressed) return;
        if (Input.touchCount > 1) { moved = true; if (held) onRelease?.Invoke(); held = false; return; }
        if (Vector2.Distance(startPosition, Input.mousePosition) > 20f) { moved = true; if (held) onRelease?.Invoke(); held = false; return; }
        if (!moved && !held && Time.unscaledTime - startTime >= 0.42f) { held = true; lastTapTime = -10f; onHold?.Invoke(); }
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        pressed = false;
        if (held) onRelease?.Invoke();
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!held && !moved)
        {
            if (requireDoubleTap)
            {
                float now = Time.unscaledTime;
                if (now - lastTapTime <= DoubleTapWindow)
                {
                    lastTapTime = -10f;
                    onTap?.Invoke();
                }
                else
                {
                    lastTapTime = now;
                }
            }
            else
            {
                onTap?.Invoke();
            }
        }
        held = false; moved = false;
    }
    void OnDisable() { if (held) onRelease?.Invoke(); pressed = false; held = false; lastTapTime = -10f; }
}

public class HistoryDragFeedback : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    public Action<bool> callback;
    public void OnBeginDrag(PointerEventData eventData) { callback?.Invoke(true); }
    public void OnEndDrag(PointerEventData eventData) { callback?.Invoke(false); }
    void OnDisable() { callback?.Invoke(false); }
}

public class HistoryNodeReveal : MonoBehaviour
{
    public float startDelay;
    public RectTransform halo;
    public readonly List<RectTransform> sparks = new List<RectTransform>();
    float elapsed;
    CanvasGroup group;
    RectTransform rect;
    void Start()
    {
        rect = (RectTransform)transform;
        var size = rect.sizeDelta;
        Vector2 center = rect.anchoredPosition + Vector2.Scale(new Vector2(0.5f, 0.5f) - rect.pivot, size);
        rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = center;
        group = GetComponent<CanvasGroup>(); if (!group) group = gameObject.AddComponent<CanvasGroup>(); group.alpha = 0;
        rect.localScale = Vector3.one * 0.35f;
    }
    public void FinishImmediately()
    {
        enabled = false;
        foreach (var spark in sparks) if (spark) Destroy(spark.gameObject);
        if (halo) Destroy(halo.gameObject);
        if (rect) rect.localScale = Vector3.one;
        if (group) group.alpha = 1f;
        Destroy(this);
    }
    void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01((elapsed - startDelay) / 0.9f);
        if (elapsed < startDelay) return;
        float bounce = 1f + 0.25f * Mathf.Sin(t * Mathf.PI) * (1f - t);
        rect.localScale = Vector3.one * (Mathf.SmoothStep(0.35f, 1f, Mathf.Min(1, t * 2.5f)) * bounce);
        group.alpha = Mathf.Clamp01(t * 7f);
        if (halo)
        {
            halo.localScale = Vector3.one * (0.35f + t * 1.25f);
            var glow = halo.GetComponent<Image>(); var glowColor = glow.color; glowColor.a = 0.85f * (1f - t); glow.color = glowColor;
        }
        for (int i = 0; i < sparks.Count; i++)
        {
            if (!sparks[i]) continue;
            float angle = i * Mathf.PI * 2f / Mathf.Max(1, sparks.Count);
            sparks[i].anchoredPosition = new Vector2(96 + Mathf.Cos(angle) * t * (130 + (i % 3) * 35), 56 + Mathf.Sin(angle) * t * (130 + (i % 3) * 35));
            var img = sparks[i].GetComponent<Image>(); var c = img.color; c.a = 1f - t; img.color = c;
        }
        if (t >= 1)
        {
            foreach (var spark in sparks) if (spark) Destroy(spark.gameObject);
            if (halo) Destroy(halo.gameObject);
            rect.localScale = Vector3.one; group.alpha = 1; Destroy(this);
        }
    }
}

public class HistoryCameraShake : MonoBehaviour
{
    public RectTransform target;
    Vector2 applied;
    float elapsed;
    void LateUpdate()
    {
        if (!target) { Destroy(this); return; }
        target.anchoredPosition -= applied;
        elapsed += Time.unscaledDeltaTime;
        float fade = 1f - Mathf.Clamp01(elapsed / 0.38f);
        applied = new Vector2(Mathf.Sin(elapsed * 70f), Mathf.Cos(elapsed * 57f)) * (4f * fade);
        target.anchoredPosition += applied;
        if (fade <= 0f) { target.anchoredPosition -= applied; applied = Vector2.zero; Destroy(this); }
    }
    void OnDisable() { if (target) target.anchoredPosition -= applied; applied = Vector2.zero; }
}

public class HistoryParallaxLayer : MonoBehaviour
{
    public RectTransform content, target;
    public float parallaxFactor = 0.42f;
    Vector2 baseCenter, lastContentPos;
    float lastContentScale = -1f;
    bool initialized;

    void LateUpdate()
    {
        if (!content || !target) return;
        Vector2 pos = content.anchoredPosition;
        float cs = content.localScale.x;
        if (!initialized) { baseCenter = pos; initialized = true; }
        else if ((pos - lastContentPos).sqrMagnitude < 0.01f && Mathf.Abs(cs - lastContentScale) < 0.0005f) return;
        lastContentPos = pos;
        lastContentScale = cs;
        Vector2 delta = pos - baseCenter;
        target.anchoredPosition = delta * parallaxFactor;
        float s = Mathf.Lerp(1f, cs, parallaxFactor);
        target.localScale = new Vector3(s, s, 1f);
    }
}
