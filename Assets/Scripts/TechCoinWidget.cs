using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TechCoinWidget : MonoBehaviour
{
    public RectTransform shakeTarget;
    public Text coinText;
    public Image bgImage;

    public Func<double> getCoins;
    public Color normalTextColor = new Color(0.18f, 0.16f, 0.14f);
    public Color normalBgColor = new Color(1f, 0.99f, 0.97f, 0.95f);
    public Color alertColor = new Color(0.88f, 0.22f, 0.16f); // 朱红警示色

    private string lastCoinsText = "";
    private Coroutine shakeRoutine;
    private Vector2 initialPos = Vector2.zero;

    void Awake()
    {
        if (shakeTarget) initialPos = shakeTarget.anchoredPosition;
    }

    void Update()
    {
        if (getCoins != null && coinText != null)
        {
            string formatted = DemoBootstrap.FormatCoins(getCoins());
            if (formatted != lastCoinsText)
            {
                lastCoinsText = formatted;
                coinText.text = formatted;
            }
        }
    }

    public void Shake()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            if (shakeTarget) shakeTarget.anchoredPosition = initialPos;
            if (coinText) coinText.color = normalTextColor;
            if (bgImage) bgImage.color = normalBgColor;
        }
        shakeRoutine = StartCoroutine(DoShake());
    }

    private IEnumerator DoShake()
    {
        if (!shakeTarget) yield break;

        float duration = 0.45f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float decay = 1f - t;
            // 高频强震动：X 轴快速振荡，并带轻微弹性缩放
            float offsetX = Mathf.Sin(t * Mathf.PI * 14f) * 16f * decay;
            float offsetY = Mathf.Cos(t * Mathf.PI * 12f) * 4f * decay;
            shakeTarget.anchoredPosition = initialPos + new Vector2(offsetX, offsetY);
            shakeTarget.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(t * Mathf.PI) * decay);

            // 前半段保持鲜红警示，后半段平滑褪回墨黑
            float colorProgress = Mathf.Clamp01((t - 0.25f) / 0.75f);
            if (coinText)
            {
                coinText.color = Color.Lerp(alertColor, normalTextColor, colorProgress);
            }
            if (bgImage)
            {
                Color redTintBg = new Color(1f, 0.90f, 0.90f, 0.98f);
                bgImage.color = Color.Lerp(redTintBg, normalBgColor, colorProgress);
            }
            yield return null;
        }

        shakeTarget.anchoredPosition = initialPos;
        shakeTarget.localScale = Vector3.one;
        if (coinText) coinText.color = normalTextColor;
        if (bgImage) bgImage.color = normalBgColor;
        shakeRoutine = null;
    }

    void OnDisable()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }
        if (shakeTarget)
        {
            shakeTarget.anchoredPosition = initialPos;
            shakeTarget.localScale = Vector3.one;
        }
        if (coinText) coinText.color = normalTextColor;
        if (bgImage) bgImage.color = normalBgColor;
    }
}
