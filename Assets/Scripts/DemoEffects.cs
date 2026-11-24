using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class DemoBootstrap
{
    // ---------- M1: 轻量化手帐转场、微动效与粒子反馈系统 ----------
    GameObject regionCurtainGo;
    Coroutine bubbleAnimCo, pageAnimCo, regionCurtainCo;

    void StopRegionCurtain()
    {
        if (regionCurtainCo != null) { StopCoroutine(regionCurtainCo); regionCurtainCo = null; }
        if (regionCurtainGo) { Destroy(regionCurtainGo); regionCurtainGo = null; }
    }

    void StopBubbleAnim()
    {
        if (bubbleAnimCo != null) { StopCoroutine(bubbleAnimCo); bubbleAnimCo = null; }
    }

    void StopPageAnim()
    {
        if (pageAnimCo != null) { StopCoroutine(pageAnimCo); pageAnimCo = null; }
    }

    void AnimateBubblePop(GameObject panel, GameObject shadow = null)
    {
        StopBubbleAnim();
        if (!panel) return;
        var rt = panel.GetComponent<RectTransform>();
        if (!rt) return;
        var cg = panel.GetComponent<CanvasGroup>();
        if (!cg) cg = panel.AddComponent<CanvasGroup>();
        Vector3 targetScale = rt.localScale;
        cg.alpha = 0f;
        rt.localScale = targetScale * (qualityTier >= 2 ? 0.88f : 0.94f);
        CanvasGroup shadowCg = null;
        if (shadow)
        {
            shadowCg = shadow.GetComponent<CanvasGroup>();
            if (!shadowCg) shadowCg = shadow.AddComponent<CanvasGroup>();
            shadowCg.alpha = 0f;
            shadowCg.blocksRaycasts = false;
        }
        bubbleAnimCo = StartCoroutine(CoBubblePop(rt, cg, shadowCg, targetScale, qualityTier >= 2));
    }

    static float EvalOutBack(float p, float from, float to, float c1 = 1.70158f)
    {
        float c3 = c1 + 1f;
        float u = p - 1f;
        float back = 1f + c3 * u * u * u + c1 * u * u;
        return Mathf.LerpUnclamped(from, to, back);
    }

    IEnumerator CoBubblePop(RectTransform rt, CanvasGroup cg, CanvasGroup shadowCg, Vector3 targetScale, bool springOvershoot)
    {
        float dur = springOvershoot ? 0.19f : 0.14f;
        float t = 0f;
        while (rt && cg && t < dur)
        {
            t += Mathf.Clamp(Time.unscaledDeltaTime, 0.002f, 0.03f);
            float p = Mathf.Clamp01(t / dur);
            float sc;
            if (springOvershoot)
            {
                sc = EvalOutBack(p, 0.88f, 1f);
            }
            else
            {
                float ease = 1f - Mathf.Pow(1f - p, 3f);
                sc = Mathf.Lerp(0.94f, 1f, ease);
            }
            rt.localScale = targetScale * sc;
            float alpha = Mathf.Clamp01(p * 1.55f);
            cg.alpha = alpha;
            if (shadowCg) shadowCg.alpha = alpha;
            yield return null;
        }
        if (rt && cg)
        {
            rt.localScale = targetScale;
            cg.alpha = 1f;
        }
        if (shadowCg) shadowCg.alpha = 1f;
        bubbleAnimCo = null;
    }

    void AnimatePageEnter(GameObject page, bool slideUp = true)
    {
        StopPageAnim();
        if (!page) return;
        var cg = page.GetComponent<CanvasGroup>();
        if (!cg) cg = page.AddComponent<CanvasGroup>();
        var rt = page.GetComponent<RectTransform>();
        cg.alpha = 0f;
        float startY = qualityTier >= 2 ? -32f : -22f;
        if (rt && slideUp)
        {
            rt.offsetMin = new Vector2(0f, startY);
            rt.offsetMax = new Vector2(0f, startY);
        }
        pageAnimCo = StartCoroutine(CoPageEnter(rt, cg, slideUp, startY));
    }

    IEnumerator CoPageEnter(RectTransform rt, CanvasGroup cg, bool slideUp, float dy)
    {
        const float dur = 0.18f;
        float t = 0f;
        while (cg && t < dur)
        {
            t += Mathf.Clamp(Time.unscaledDeltaTime, 0.002f, 0.03f);
            float p = Mathf.Clamp01(t / dur);
            float ease = 1f - Mathf.Pow(1f - p, 3f);
            cg.alpha = ease;
            if (rt && slideUp)
            {
                float off = Mathf.Lerp(dy, 0f, ease);
                rt.offsetMin = new Vector2(0f, off);
                rt.offsetMax = new Vector2(0f, off);
            }
            yield return null;
        }
        if (cg) cg.alpha = 1f;
        if (rt && slideUp)
        {
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        pageAnimCo = null;
    }

    bool TryGetRegionShowcaseSpec(string titleOrRegion, out RegionShowcaseSpec spec)
    {
        string normalized = (titleOrRegion ?? "").Replace(" ", "");
        for (int i = 0; i < RegionShowcaseSpecs.Length; i++)
        {
            if (RegionShowcaseSpecs[i].key == normalized) { spec = RegionShowcaseSpecs[i]; return true; }
        }
        spec = RegionShowcaseSpecs[0];
        return false;
    }

    void PlayRegionCurtain(string title)
    {
        if (!root) return;
        StopRegionCurtain();

        bool hasSpec = TryGetRegionShowcaseSpec(title, out var spec);
        bool neon = IsNeonThemeActive();
        Color veilColor = neon ? new Color(0.04f, 0.06f, 0.16f, 0.96f) : new Color(Paper.r, Paper.g, Paper.b, 0.96f);

        regionCurtainGo = Rect("RegionCurtain", root, 0f, 0f, RootW, RootH);
        var rt = regionCurtainGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var bg = Img(regionCurtainGo, veilColor);
        bg.raycastTarget = false;

        // 中心水彩风貌明信片框：呈现“到达区域专属手绘风景 + 古代书法名家集字挥毫揭幕”
        const float cardW = 884f, cardH = 472f;
        var card = Rect("CurtainCard", regionCurtainGo.transform, (RootW - cardW) * 0.5f, (RootH - cardH) * 0.5f, cardW, cardH);
        var cardRt = card.GetComponent<RectTransform>();
        cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.anchoredPosition = new Vector2(RootW * 0.5f, RootH * 0.5f);
        Rounded(card, neon ? new Color(0.14f, 0.20f, 0.38f, 1f) : new Color(0.99f, 0.978f, 0.946f, 1f));
        card.GetComponent<Image>().raycastTarget = false;
        SoftShadow(card);

        const float pad = 10f, artW = cardW - pad * 2f, artH = cardH - pad * 2f;
        var artClip = Rect("ArtClip", card.transform, pad, pad, artW, artH);
        var clipImg = Img(artClip, neon ? new Color(0.06f, 0.08f, 0.18f, 1f) : new Color(0.96f, 0.94f, 0.89f, 1f), spriteRound);
        clipImg.type = Image.Type.Sliced; clipImg.raycastTarget = false;
        artClip.AddComponent<Mask>().showMaskGraphic = true;

        RawImage bgRaw = null;
        var bgTex = hasSpec ? Resources.Load<Texture2D>(spec.bgPath) : null;
        if (bgTex)
        {
            bgRaw = Rect("Backdrop", artClip.transform, 0f, 0f, artW, artH).AddComponent<RawImage>();
            bgRaw.texture = bgTex; bgRaw.raycastTarget = false;
            bgRaw.color = neon ? new Color(0.38f, 0.46f, 0.86f, 0.90f) : Color.white;
        }

        // 顶部抵达邮戳标签
        var arrivalPill = Rect("ArrivalBadge", artClip.transform, (artW - 220f) * 0.5f, artH - 58f, 220f, 36f);
        Rounded(arrivalPill, neon ? Gold : spec.frameAccent);
        arrivalPill.GetComponent<Image>().raycastTarget = false;
        Txt(arrivalPill.transform, "抵 达 版 图", 19, neon ? new Color(0.08f, 0.08f, 0.14f) : Color.white, 0f, 0f, 220f, 36f, TextAnchor.MiddleCenter);

        // 居中书法集字（无椭圆遮罩与副标题，放大字形并强化柔光外轮廓）
        const float titleW = 640f, titleH = 241f;
        float titleX = (artW - titleW) * 0.5f, titleY = (artH - titleH) * 0.5f + 12f;
        var titleHolder = Rect("TitleReveal", artClip.transform, titleX, titleY, titleW, titleH);
        var titleRt = titleHolder.GetComponent<RectTransform>();
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.anchoredPosition = new Vector2(artW * 0.5f, titleY + titleH * 0.5f);
        var titleCg = titleHolder.AddComponent<CanvasGroup>();
        titleCg.alpha = 0f;

        var titleTex = (hasSpec && !string.IsNullOrEmpty(spec.titleMaskPath)) ? Resources.Load<Texture2D>(spec.titleMaskPath) : null;
        if (titleTex)
        {
            Color haloCol = neon ? new Color(0.02f, 0.03f, 0.09f, 0.94f) : new Color(0.996f, 0.986f, 0.955f, 0.92f);
            float[] ox = { -2.5f, 2.5f, 0f, 0f };
            float[] oy = { 0f, 0f, -2.5f, 2.5f };
            for (int k = 0; k < 4; k++)
            {
                var halo = Rect("Halo_" + k, titleHolder.transform, ox[k], oy[k], titleW, titleH).AddComponent<RawImage>();
                halo.texture = titleTex; halo.raycastTarget = false;
                halo.color = haloCol;
            }

            var glyph = Rect("Calligraphy", titleHolder.transform, 0f, 0f, titleW, titleH).AddComponent<RawImage>();
            glyph.texture = titleTex; glyph.raycastTarget = false;
            glyph.color = neon ? spec.neonInk : spec.dayInk;
        }
        else
        {
            string spaced = (hasSpec ? spec.key : DisplayPlaceAliases(title)).Replace(" ", "");
            if (spaced.Length == 2) spaced = spaced[0] + "   " + spaced[1];
            Color haloCol = neon ? new Color(0.02f, 0.03f, 0.09f, 0.92f) : new Color(0.996f, 0.984f, 0.948f, 0.94f);
            Color inkCol = hasSpec ? (neon ? spec.neonInk : spec.dayInk) : (neon ? Gold : Ink);
            Txt(titleHolder.transform, spaced, 120, haloCol, 2f, -3f, titleW, titleH, TextAnchor.MiddleCenter).raycastTarget = false;
            Txt(titleHolder.transform, spaced, 120, inkCol, 0f, 0f, titleW, titleH, TextAnchor.MiddleCenter).raycastTarget = false;
        }

        // 中线金/青花装饰线（从中心向外舒展）
        var rule = Rect("BrushRule", artClip.transform, artW * 0.5f - 180f, titleY - 12f, 360f, 4f);
        var ruleRt = rule.GetComponent<RectTransform>();
        ruleRt.pivot = new Vector2(0.5f, 0.5f);
        ruleRt.anchoredPosition = new Vector2(artW * 0.5f, titleY - 10f);
        var ruleImg = Img(rule, neon ? spec.neonInk : spec.frameAccent, spriteRound);
        ruleImg.raycastTarget = false;
        ruleRt.localScale = new Vector3(0f, 1f, 1f);

        // 高画质附加轻盈浮金与花瓣星芒点缀
        List<RectTransform> sparkles = null;
        if (qualityTier >= 2 && (spriteDiamond || spriteCircle))
        {
            sparkles = new List<RectTransform>(10);
            Color spCol = neon ? spec.neonInk : Color.Lerp(spec.frameAccent, Gold, 0.55f);
            for (int i = 0; i < 10; i++)
            {
                bool leftWing = i % 2 == 0;
                float sx = leftWing ? 42f + (i / 2) * 34f : artW - 42f - (i / 2) * 34f;
                float sy = 96f + ((i * 67) % 270);
                float sz = (i % 3 == 0) ? 14f : 10f;
                var spGo = Rect("CurtainSpark" + i, artClip.transform, sx, sy, sz, sz);
                var spRt = spGo.GetComponent<RectTransform>();
                spRt.pivot = new Vector2(0.5f, 0.5f);
                var spImg = Img(spGo, new Color(spCol.r, spCol.g, spCol.b, 0.72f), (i % 2 == 0 && spriteDiamond) ? spriteDiamond : spriteCircle);
                spImg.raycastTarget = false;
                sparkles.Add(spRt);
            }
        }

        var cg = regionCurtainGo.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.alpha = 0f;
        regionCurtainCo = StartCoroutine(CoRegionCurtain(regionCurtainGo, cg, cardRt, bgRaw, bgTex, titleRt, titleCg, ruleRt, sparkles));
    }

    IEnumerator CoRegionCurtain(GameObject go, CanvasGroup cg, RectTransform cardRt, RawImage bgRaw, Texture2D bgTex,
        RectTransform titleRt, CanvasGroup titleCg, RectTransform ruleRt, List<RectTransform> sparkles)
    {
        float inDur = 0.16f, holdDur = 0.48f, outDur = 0.36f;
        float totalDur = inDur + holdDur + outDur;
        float elapsed = 0f;

        Vector2 cardHome = cardRt ? cardRt.anchoredPosition : Vector2.zero;
        float baseU = 0f, panSpan = 0.055f, visW = 1f - panSpan;
        float visH = 1f, baseV = 0f;
        if (bgRaw && bgTex && cardRt)
        {
            float boxAspect = (cardRt.sizeDelta.x - 20f) / Mathf.Max(1f, cardRt.sizeDelta.y - 20f);
            float imgAspect = (float)bgTex.width / Mathf.Max(1, bgTex.height);
            float zoomedImgAspect = imgAspect * visW;
            if (zoomedImgAspect < boxAspect)
            {
                visH = zoomedImgAspect / boxAspect;
                baseV = (1f - visH) * 0.36f;
            }
            bgRaw.uvRect = new Rect(0f, baseV, visW, visH);
        }

        while (go && cg && elapsed < totalDur)
        {
            elapsed += Mathf.Clamp(Time.unscaledDeltaTime, 0.001f, 0.033f);
            float totalP = Mathf.Clamp01(elapsed / totalDur);

            if (bgRaw)
                bgRaw.uvRect = new Rect(Mathf.SmoothStep(baseU, panSpan, totalP), baseV, visW, visH);

            if (sparkles != null)
            {
                for (int i = 0; i < sparkles.Count; i++)
                {
                    var sp = sparkles[i];
                    if (!sp) continue;
                    float phase = totalP * 6.28318f + i * 0.7f;
                    sp.localScale = Vector3.one * (0.65f + 0.45f * Mathf.Sin(phase));
                    sp.localRotation = Quaternion.Euler(0f, 0f, elapsed * (i % 2 == 0 ? 48f : -42f));
                }
            }

            if (elapsed <= inDur)
            {
                float p = Mathf.Clamp01(elapsed / inDur);
                float ease = 1f - Mathf.Pow(1f - p, 3f);
                cg.alpha = ease;
                if (cardRt)
                {
                    float s = qualityTier >= 2 ? EvalOutBack(p, 0.88f, 1.0f, 1.3f) : Mathf.Lerp(0.90f, 1.0f, ease);
                    cardRt.localScale = Vector3.one * s;
                }
            }
            else if (elapsed <= inDur + holdDur)
            {
                cg.alpha = 1f;
                if (cardRt) cardRt.localScale = Vector3.one;
            }
            else
            {
                float p = Mathf.Clamp01((elapsed - inDur - holdDur) / outDur);
                float ease = p * p * (3f - 2f * p);
                cg.alpha = 1f - ease;
                if (cardRt)
                {
                    cardRt.anchoredPosition = cardHome + new Vector2(0f, ease * 28f);
                    cardRt.localScale = Vector3.one * Mathf.Lerp(1f, 1.03f, ease);
                }
            }

            // 书法名家集字与饰线错峰揭幕（0.24s 内全部到位，确保高潮定格饱满清晰）
            float titleP = Mathf.Clamp01((elapsed - 0.03f) / 0.20f);
            float titleEase = 1f - Mathf.Pow(1f - titleP, 3f);
            if (titleCg) titleCg.alpha = titleEase;
            if (titleRt) titleRt.localScale = Vector3.one * Mathf.Lerp(1.13f, 1f, titleEase);

            float ruleP = Mathf.Clamp01((elapsed - 0.06f) / 0.18f);
            if (ruleRt) ruleRt.localScale = new Vector3(1f - Mathf.Pow(1f - ruleP, 3f), 1f, 1f);

            yield return null;
        }
        if (go) Destroy(go);
        if (regionCurtainGo == go) regionCurtainGo = null;
        regionCurtainCo = null;
    }

    void SpawnMapRipple(Vector2 mapPos, Color col, float maxSize = 140f)
    {
        if (!floaterLayer || !spriteRing) return;
        var go = Rect("MapRipple", floaterLayer, mapPos.x - 16f, mapPos.y - 16f, 32f, 32f);
        var rt = go.GetComponent<RectTransform>();
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = mapPos;
        var img = Img(go, col, spriteRing);
        img.raycastTarget = false;
        StartCoroutine(CoMapRipple(go, rt, img, col, maxSize, 0f));
        // 高画质附加一层金色共振外环波纹，强化触感反馈。
        if (qualityTier >= 2)
        {
            var echoGo = Rect("MapRippleEcho", floaterLayer, mapPos.x - 12f, mapPos.y - 12f, 24f, 24f);
            var echoRt = echoGo.GetComponent<RectTransform>();
            echoRt.pivot = new Vector2(0.5f, 0.5f);
            echoRt.anchoredPosition = mapPos;
            var echoCol = Color.Lerp(col, Gold, 0.6f);
            var echoImg = Img(echoGo, new Color(echoCol.r, echoCol.g, echoCol.b, 0f), spriteRing);
            echoImg.raycastTarget = false;
            StartCoroutine(CoMapRipple(echoGo, echoRt, echoImg, echoCol, maxSize * 1.28f, 0.08f));
        }
    }

    IEnumerator CoMapRipple(GameObject go, RectTransform rt, Image img, Color baseCol, float maxSize, float delay)
    {
        if (delay > 0f)
        {
            float d = 0f;
            while (go && d < delay) { d += Time.unscaledDeltaTime; yield return null; }
        }
        float dur = 0.52f, t = 0f;
        while (go && rt && img && t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / dur);
            float ease = 1f - Mathf.Pow(1f - p, 2.5f);
            float size = Mathf.Lerp(24f, maxSize, ease);
            rt.sizeDelta = new Vector2(size, size);
            img.color = new Color(baseCol.r, baseCol.g, baseCol.b, (1f - p) * 0.85f);
            yield return null;
        }
        if (go) Destroy(go);
    }

    void SpawnConfettiBurst(Vector2 rootPos, Color c1, Color c2, int count = 14)
    {
        if (!root || !spriteCircle) return;
        int n = qualityTier >= 2 ? Mathf.RoundToInt(count * 1.7f)
              : qualityTier == 1 ? count
              : Mathf.Max(6, count / 2);
        StartCoroutine(CoConfettiBurst(rootPos, c1, c2, n));
    }

    IEnumerator CoConfettiBurst(Vector2 origin, Color c1, Color c2, int count)
    {
        var holder = Rect("ConfettiBurst", root, 0f, 0f, 10f, 10f);
        var hrt = holder.GetComponent<RectTransform>();
        hrt.anchoredPosition = origin;
        var items = new RectTransform[count];
        var imgs = new Image[count];
        var vel = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            float ang = (i / (float)count) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.2f, 0.2f);
            float spd = UnityEngine.Random.Range(150f, qualityTier >= 2 ? 390f : 340f);
            vel[i] = new Vector2(Mathf.Cos(ang) * spd, Mathf.Sin(ang) * spd + 120f);
            float sz = UnityEngine.Random.Range(10f, 19f);
            var pGo = Rect("P_" + i, holder.transform, 0f, 0f, sz, sz);
            items[i] = pGo.GetComponent<RectTransform>();
            items[i].pivot = new Vector2(0.5f, 0.5f);
            items[i].anchoredPosition = Vector2.zero;
            Color col = i % 3 == 0 ? c1 : i % 3 == 1 ? c2 : Gold;
            imgs[i] = Img(pGo, col, i % 2 == 0 ? spriteDiamond : spriteCircle);
            imgs[i].raycastTarget = false;
        }

        float dur = qualityTier >= 2 ? 0.86f : 0.72f, t = 0f;
        while (holder && t < dur)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            float p = Mathf.Clamp01(t / dur);
            for (int i = 0; i < count; i++)
            {
                if (!items[i]) continue;
                vel[i].y -= 520f * dt;
                items[i].anchoredPosition += vel[i] * dt;
                items[i].localRotation = Quaternion.Euler(0f, 0f, t * 240f * (i % 2 == 0 ? 1f : -1f));
                Color c = imgs[i].color;
                imgs[i].color = new Color(c.r, c.g, c.b, 1f - p * p);
            }
            yield return null;
        }
        if (holder) Destroy(holder);
    }

    void PlayStationPathSparkles(System.Collections.Generic.List<int> path, Color col)
    {
        if (path == null || path.Count == 0) return;
        int stride = qualityTier >= 2 ? 1 : Mathf.Max(1, path.Count / 5);
        for (int i = 0; i < path.Count; i += stride)
        {
            int idx = path[i];
            if (idx >= 0 && idx < stations.Count && StationInRegion(idx, currentRegion))
                SpawnMapRipple(Pos(idx), i % 2 == 0 ? col : Color.Lerp(col, Gold, 0.45f), 130f);
        }
        if (path.Count > 0)
        {
            int last = path[path.Count - 1];
            if (last >= 0 && last < stations.Count && StationInRegion(last, currentRegion))
                SpawnMapRipple(Pos(last), Gold, 175f);
        }
    }
}
