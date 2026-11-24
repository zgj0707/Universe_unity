using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>仅当已打卡相片进入图鉴视口时加载对应大图。</summary>
public class JournalLazyPhoto : MonoBehaviour
{
    public RawImage image;
    public RectTransform viewport;
    public string resourcePath;
    float nextCheck;
    void Update()
    {
        if (!image || !viewport) { enabled = false; return; }
        if (Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + 0.25f;
        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, image.rectTransform);
        Rect picture = Rect.MinMaxRect(bounds.min.x, bounds.min.y, bounds.max.x, bounds.max.y);
        if (!viewport.rect.Overlaps(picture)) return;
        image.texture = Resources.Load<Texture2D>(resourcePath);
        if (image.texture) {
            float aspect = (float)image.texture.width / image.texture.height;
            float frame = image.rectTransform.rect.width / Mathf.Max(1f, image.rectTransform.rect.height);
            if (aspect > frame) { float width = frame / aspect; image.uvRect = new Rect((1f - width) * 0.5f, 0, width, 1); }
            else { float height = aspect / frame; image.uvRect = new Rect(0, (1f - height) * 0.5f, 1, height); }
            image.color = Color.white;
        }
        enabled = false;
    }
}

public class FleetCardEntrance : MonoBehaviour
{
    public float delay;
    CanvasGroup group;
    RectTransform rect;
    Vector2 original;
    float elapsed;
    void Start()
    {
        rect = (RectTransform)transform; original = rect.anchoredPosition;
        group = gameObject.AddComponent<CanvasGroup>(); group.alpha = 0;
        rect.anchoredPosition = original + new Vector2(0, -18);
    }
    void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01((elapsed - delay) / 0.22f);
        group.alpha = t;
        rect.anchoredPosition = original + new Vector2(0, -18 * (1 - Mathf.SmoothStep(0, 1, t)));
        if (t >= 1) Destroy(this);
    }
}

public class FleetPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    bool pressed;
    Vector3 original;
    void Awake() { original = transform.localScale; }
    public void OnPointerDown(PointerEventData e) { pressed = true; }
    public void OnPointerUp(PointerEventData e) { pressed = false; }
    public void OnPointerExit(PointerEventData e) { pressed = false; }
    void Update()
    {
        float targetScale = pressed ? 0.96f : 1f;
        Vector3 target = original * targetScale;
        if ((transform.localScale - target).sqrMagnitude < 0.000004f)
        {
            if (transform.localScale != target) transform.localScale = target;
            return;
        }
        transform.localScale = Vector3.Lerp(transform.localScale, target, 1f - Mathf.Exp(-24f * Time.unscaledDeltaTime));
    }
    void OnDisable() { transform.localScale = original; pressed = false; }
}

public class FleetBottomEdge : MonoBehaviour
{
    RectTransform rect, parent;
    void Awake() { rect = (RectTransform)transform; parent = rect.parent as RectTransform; }
    void LateUpdate()
    {
        if (!parent) return;
        var screen = new Vector2(Screen.width * 0.5f, Screen.safeArea.yMin);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out var local))
            rect.anchoredPosition = new Vector2(0, local.y);
    }
}

public class FleetBackgroundFit : MonoBehaviour
{
    public RectTransform page;
    public UnityEngine.UI.RawImage image;
    void LateUpdate()
    {
        if (!page || !image || !image.texture) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(page, Vector2.zero, null, out var min) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(page, new Vector2(Screen.width, Screen.height), null, out var max)) return;
        float width = Mathf.Abs(max.x - min.x), height = Mathf.Abs(max.y - min.y);
        if (width < 1 || height < 1) return;
        var rt = image.rectTransform;
        rt.anchoredPosition = new Vector2(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y)) - page.rect.min;
        rt.sizeDelta = new Vector2(width, height);
        float imageAspect = (float)image.texture.width / image.texture.height;
        float screenAspect = width / height;
        if (imageAspect > screenAspect)
        {
            float visible = screenAspect / imageAspect;
            image.uvRect = new Rect((1f - visible) / 2f, 0, visible, 1);
        }
        else
        {
            float visible = imageAspect / screenAspect;
            image.uvRect = new Rect(0, (1f - visible) / 2f, 1, visible);
        }
    }
}

/// <summary>拖拽开线时，新接入的线段从起点生长到终点。</summary>
public class DraftSegGrow : MonoBehaviour
{
    public float duration = 0.16f;
    RectTransform rect;
    float elapsed;

    void Awake()
    {
        rect = (RectTransform)transform;
        rect.localScale = new Vector3(0f, 1f, 1f);
    }

    void Update()
    {
        if (!rect) { Destroy(this); return; }
        elapsed += Time.unscaledDeltaTime;
        float k = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
        float e = 1f - Mathf.Pow(1f - k, 3f);
        rect.localScale = new Vector3(e, 1f, 1f);
        if (k >= 1f) Destroy(this);
    }
}
