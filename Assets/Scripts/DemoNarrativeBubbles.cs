using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class DemoBootstrap
{
    readonly List<NarrativeBubbleSpec> narrativeBubbleQueue = new List<NarrativeBubbleSpec>();
    GameObject activeNarrativeBubbleGo;
    Coroutine activeNarrativeBubbleCo;
    float narrativeGestureIdleAt = 0f;
    float lastNarrativeBubbleShownAt = -999f;
    int lastWeeklyCycleBroadcastWeek = -1;

    // P3 地图乘客微气泡池（同屏最多 2 个，3 秒自动淡出）
    readonly List<GameObject> activeMicroBubbles = new List<GameObject>();
    float nextMicroBubbleTime = 4f;
    float nextGuideCheckTime = 1.5f;

    public void ClearNarrativeBubbles()
    {
        narrativeBubbleQueue.Clear();
        if (activeNarrativeBubbleCo != null) { StopCoroutine(activeNarrativeBubbleCo); activeNarrativeBubbleCo = null; }
        if (activeNarrativeBubbleGo) { Destroy(activeNarrativeBubbleGo); activeNarrativeBubbleGo = null; }
        for (int i = activeMicroBubbles.Count - 1; i >= 0; i--)
            if (activeMicroBubbles[i]) Destroy(activeMicroBubbles[i]);
        activeMicroBubbles.Clear();
        lastNarrativeBubbleShownAt = -999f;
        lastWeeklyCycleBroadcastWeek = -1;
    }

    public void QueueGuideBubble(string guideId)
    {
        if (tutorialSkipped) return;
        if (string.IsNullOrEmpty(guideId) || !GuideBubbleTexts.TryGetValue(guideId, out string txt)) return;
        string seenKey = "nar_" + guideId;
        if (!storySeen.Add(seenKey)) return;
        EnqueueNarrativeBubble(new NarrativeBubbleSpec
        {
            key = seenKey,
            priority = NarrativeBubblePriority.P2_Guide,
            speakerTag = "塞勒涅 · 领航提示",
            text = txt
        });
        SaveGame();
    }

    public void QueueMilestoneBubble(string milestoneId, Action badgeAction = null)
    {
        if (string.IsNullOrEmpty(milestoneId) || !MilestoneBubbleSpecs.TryGetValue(milestoneId, out var spec)) return;
        string seenKey = "nar_" + milestoneId;
        if (!storySeen.Add(seenKey)) return;
        EnqueueNarrativeBubble(new NarrativeBubbleSpec
        {
            key = seenKey,
            priority = NarrativeBubblePriority.P0_Milestone,
            speakerTag = "塞勒涅 · 星域电台",
            text = spec.text,
            badgeLabel = spec.badge,
            badgeAction = badgeAction ?? (() => OpenSceneryTab(0))
        });
        SaveGame();
    }

    public void QueueWeeklyCycleBubble(int cycleIndex)
    {
        int weekIdx = Mathf.Max(0, day / 7);
        if (lastWeeklyCycleBroadcastWeek == weekIdx) return;
        lastWeeklyCycleBroadcastWeek = weekIdx;
        int idx = Mathf.Clamp(cycleIndex, 0, WeeklyCycleForecastTexts.Length - 1);
        EnqueueNarrativeBubble(new NarrativeBubbleSpec
        {
            key = "weekly_" + weekIdx + "_" + idx,
            priority = NarrativeBubblePriority.P1_WeeklyCycle,
            speakerTag = "《深空星风周报》· " + EconomyCycleNames[idx],
            text = WeeklyCycleForecastTexts[idx],
            badgeLabel = "✉ 查看公报",
            badgeAction = OpenNoticeBoard
        });
    }

    void EnqueueNarrativeBubble(NarrativeBubbleSpec spec)
    {
        if (spec == null || string.IsNullOrEmpty(spec.text)) return;
        for (int i = 0; i < narrativeBubbleQueue.Count; i++)
            if (narrativeBubbleQueue[i].key == spec.key) return;
        narrativeBubbleQueue.Add(spec);
        narrativeBubbleQueue.Sort((a, b) => a.priority.CompareTo(b.priority));
    }

    void TickNarrativeBubbles()
    {
        if (!root) return;

        // 手势避让铁律：长按蓄力、拖拽铺轨、插站或双指缩放时挂起队列，松手稳定 2 秒后再飘出
        bool gestureActive = pressing || dragging || lineInsertActive || Input.touchCount >= 2 || Input.GetMouseButton(0);
        if (gestureActive)
            narrativeGestureIdleAt = Time.unscaledTime + 2.0f;

        bool canShowOverlay = pageRoot == null && sceneryViewer == null && metroMapPageRoot == null && activePanel == null && storyCard == null;

        if (!tutorialSkipped && canShowOverlay && Time.unscaledTime >= nextGuideCheckTime)
        {
            nextGuideCheckTime = Time.unscaledTime + 1.5f;
            if (!storySeen.Contains("nar_G-01") && currentRegion == "月之亮面" && lines.Count == 0)
                QueueGuideBubble("G-01");
            if (!storySeen.Contains("nar_G-03"))
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    if (LinePeakLoadRatio(lines[i]) >= 0.78f) { QueueGuideBubble("G-03"); break; }
                }
            }
            if (!storySeen.Contains("nar_G-04") && coins >= FacilityUpgradeCost(0))
            {
                for (int i = 0; i < stations.Count; i++)
                {
                    if (stations[i] != null && stations[i].built && !stations[i].closed && stations[i].facLevel == 0)
                    {
                        QueueGuideBubble("G-04");
                        break;
                    }
                }
            }
            if (!storySeen.Contains("nar_G-05") && rp >= 15f)
                QueueGuideBubble("G-05");
        }

        if (activeNarrativeBubbleGo == null && narrativeBubbleQueue.Count > 0 && canShowOverlay && Time.unscaledTime >= narrativeGestureIdleAt)
        {
            int pickIdx = -1;
            for (int i = 0; i < narrativeBubbleQueue.Count; i++)
            {
                var cand = narrativeBubbleQueue[i];
                if (cand.priority == NarrativeBubblePriority.P2_Guide &&
                    lastNarrativeBubbleShownAt > -100f &&
                    Time.unscaledTime - lastNarrativeBubbleShownAt < 25f)
                {
                    continue;
                }
                pickIdx = i;
                break;
            }

            if (pickIdx >= 0)
            {
                var next = narrativeBubbleQueue[pickIdx];
                narrativeBubbleQueue.RemoveAt(pickIdx);
                ShowNarrativeFloatBubble(next);
            }
        }

        // P3 地图乘客微气泡（同屏最多 2 个，3 秒自动淡出，不抢视觉焦点）
        for (int i = activeMicroBubbles.Count - 1; i >= 0; i--)
            if (!activeMicroBubbles[i]) activeMicroBubbles.RemoveAt(i);

        if (canShowOverlay && !gestureActive && activeMicroBubbles.Count < 2 && Time.unscaledTime >= nextMicroBubbleTime)
        {
            nextMicroBubbleTime = Time.unscaledTime + UnityEngine.Random.Range(4.8f, 8.0f);
            SpawnPassengerMicroBubble();
        }
    }

    void ShowNarrativeFloatBubble(NarrativeBubbleSpec spec)
    {
        if (activeNarrativeBubbleCo != null) StopCoroutine(activeNarrativeBubbleCo);
        if (activeNarrativeBubbleGo) Destroy(activeNarrativeBubbleGo);

        GetSafeMarginsInRoot(out float safeL, out float safeR, out float safeT, out float safeB);
        float safeW = Mathf.Max(640f, RootW - safeL - safeR);
        float bubbleW = Mathf.Min(920f, safeW - 60f);
        bool hasBadge = !string.IsNullOrEmpty(spec.badgeLabel);
        float textW = bubbleW - 56f;
        float textH = Mathf.Max(56f, MeasureTextHeight(spec.text, 24, textW) + 8f);
        float bubbleH = 54f + textH + (hasBadge ? 44f : 18f);
        float bx = safeL + (safeW - bubbleW) * 0.5f;
        float by = safeB + 188f;

        var go = Rect("SeleneFloatBubble", root, bx, by, bubbleW, bubbleH);
        activeNarrativeBubbleGo = go;
        Color bgCol = spec.priority == NarrativeBubblePriority.P0_Milestone
            ? new Color(0.995f, 0.975f, 0.92f, 0.965f)
            : new Color(0.985f, 0.98f, 0.955f, 0.955f);
        Rounded(go, bgCol);
        SoftShadow(go);

        Color accent = spec.priority == NarrativeBubblePriority.P0_Milestone ? Gold
            : spec.priority == NarrativeBubblePriority.P1_WeeklyCycle ? Indigo : Matcha;
        StampFineRuleFrame(go.transform, bubbleW, bubbleH, 6f, new Color(accent.r, accent.g, accent.b, 0.22f));

        var tagTxt = Txt(go.transform, spec.speakerTag, 20, accent, 24f, bubbleH - 42f, bubbleW - 48f, 28f, TextAnchor.MiddleLeft);
        tagTxt.raycastTarget = false;
        FitSingleLine(tagTxt, 20);

        var bodyTxt = Txt(go.transform, spec.text, 24, Ink, 24f, hasBadge ? 48f : 16f, textW, textH, TextAnchor.UpperLeft);
        bodyTxt.raycastTarget = false;

        // 点击气泡快速跳过
        var skipBtn = go.AddComponent<Button>();
        skipBtn.targetGraphic = go.GetComponent<Image>();
        skipBtn.transition = Selectable.Transition.None;
        skipBtn.onClick.AddListener(DismissActiveNarrativeBubble);

        if (hasBadge)
        {
            float badgeW = Mathf.Clamp(spec.badgeLabel.Length * 22f + 44f, 190f, 340f);
            var badgeBtn = Btn(go.transform, "BadgeLink", spec.badgeLabel, 20, bubbleW - badgeW - 16f, 10f, badgeW, 34f, Color.Lerp(CardBg, accent, 0.16f), () => {
                DismissActiveNarrativeBubble();
                spec.badgeAction?.Invoke();
            });
            TInk(badgeBtn, accent);
        }

        if (toastGo) toastGo.transform.SetAsLastSibling();
        var cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        lastNarrativeBubbleShownAt = Time.unscaledTime;
        activeNarrativeBubbleCo = StartCoroutine(CoRunNarrativeBubble(go, cg, spec.priority == NarrativeBubblePriority.P0_Milestone ? 6.0f : 5.2f));
    }

    void DismissActiveNarrativeBubble()
    {
        if (activeNarrativeBubbleCo != null) { StopCoroutine(activeNarrativeBubbleCo); activeNarrativeBubbleCo = null; }
        if (activeNarrativeBubbleGo) { Destroy(activeNarrativeBubbleGo); activeNarrativeBubbleGo = null; }
    }

    IEnumerator CoRunNarrativeBubble(GameObject go, CanvasGroup cg, float holdSeconds)
    {
        var rt = go.GetComponent<RectTransform>();
        Vector2 targetPos = rt.anchoredPosition;
        Vector2 startPos = targetPos + new Vector2(0f, -18f);
        float t = 0f;
        while (go && cg && t < 0.22f)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / 0.22f);
            float ease = 1f - Mathf.Pow(1f - p, 3f);
            cg.alpha = ease;
            rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, ease);
            yield return null;
        }
        if (go && cg) { cg.alpha = 1f; rt.anchoredPosition = targetPos; }

        float wait = 0f;
        while (go && wait < holdSeconds)
        {
            wait += Time.unscaledDeltaTime;
            yield return null;
        }

        float fade = 0f;
        while (go && cg && fade < 0.35f)
        {
            fade += Time.unscaledDeltaTime;
            cg.alpha = 1f - Mathf.Clamp01(fade / 0.35f);
            yield return null;
        }
        if (go) Destroy(go);
        if (activeNarrativeBubbleGo == go) activeNarrativeBubbleGo = null;
        activeNarrativeBubbleCo = null;
    }

    void SpawnPassengerMicroBubble()
    {
        if (!floaterLayer || stations == null || stations.Count == 0) return;

        // 筛选当前星域已建成且非关闭的站
        var candidates = new List<int>();
        for (int i = 0; i < stations.Count; i++)
        {
            var s = stations[i];
            if (s != null && s.built && !s.closed && StationInRegion(i, currentRegion))
                candidates.Add(i);
        }
        if (candidates.Count == 0) return;

        int pick = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        var st = stations[pick];

        // 根据站点状态/时段/设施从海量句库与动态组合池中抽取贴切场景文案
        float hour = (dayClock / Mathf.Max(1f, GameDaySeconds)) * 24f;
        bool isNight = hour >= 20f || hour < 6f;
        bool isCongested = false;
        foreach (var l in lines)
        {
            if (l.st != null && l.st.Contains(pick) && LinePeakLoadRatio(l) >= 0.78f) { isCongested = true; break; }
        }

        string msg = PickPassengerMicroText(st, isCongested, isNight, StationHasScenery(pick), st.facLevel > 0);
        Vector2 pos = Pos(pick);
        float w = Mathf.Clamp(msg.Length * 19f + 38f, 150f, 400f);
        var go = Rect("MicroBubble", floaterLayer, pos.x - w * 0.5f, pos.y + 34f, w, 40f);
        var bg = Img(go, new Color(0.16f, 0.14f, 0.12f, 0.84f), spriteRound);
        bg.type = Image.Type.Sliced;
        bg.raycastTarget = false;
        var txt = Txt(go.transform, msg, 18, new Color(1f, 0.97f, 0.90f), 10f, 0f, w - 20f, 40f, TextAnchor.MiddleCenter);
        txt.raycastTarget = false;
        FitSingleLine(txt, 18);
        var cg = go.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;
        activeMicroBubbles.Add(go);
        StartCoroutine(CoRunMicroBubble(go, cg, pos + new Vector2(-w * 0.5f, 34f)));
    }

    IEnumerator CoRunMicroBubble(GameObject go, CanvasGroup cg, Vector2 basePos)
    {
        var rt = go.GetComponent<RectTransform>();
        float t = 0f;
        const float total = 3.0f;
        while (go && cg && t < total)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / total);
            float alpha = p < 0.15f ? (p / 0.15f) : (p > 0.75f ? (1f - (p - 0.75f) / 0.25f) : 1f);
            cg.alpha = alpha;
            rt.anchoredPosition = basePos + new Vector2(0f, p * 26f);
            yield return null;
        }
        if (go) Destroy(go);
    }
}
