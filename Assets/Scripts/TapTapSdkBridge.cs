using System;
using System.Runtime.InteropServices;
using UnityEngine;

public sealed class TapTapSdkBridgeHost : MonoBehaviour
{
    public static TapTapSdkBridgeHost Instance { get; private set; }
    public DemoBootstrap Owner { get; set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void OnJsAdCallback(string json)
    {
        Owner?.HandleTapTapAdCallback(json);
    }
}

public partial class DemoBootstrap
{
    const float AdWatchdogTimeoutSeconds = 90f;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern int TapTapJS_IsAvailable();
    [DllImport("__Internal")] static extern void TapTapJS_ShowRewardVideoAd(string requestId, string adKey, string itemName);
#endif

    [Serializable]
    sealed class JsAdResult
    {
        public string requestId, adKey, errorCode, message;
        public bool success, manualClose;
    }

    TapTapSdkBridgeHost sdkHost;
    int sdkRequestSeq;

    // 激励视频状态与单次结算守卫
    bool adInFlight, adSettled, adMutedBgmTemporarily;
    string activeAdRequestId = "";
    float adWatchdogTimer;
    Action pendingAdGrantedCallback;

    void EnsureTapTapSdkBridge()
    {
        if (sdkHost == null)
        {
            var existing = GameObject.Find("TapTapSdkBridgeHost");
            if (existing != null) sdkHost = existing.GetComponent<TapTapSdkBridgeHost>() ?? existing.AddComponent<TapTapSdkBridgeHost>();
            if (sdkHost == null)
            {
                var go = new GameObject("TapTapSdkBridgeHost");
                sdkHost = go.AddComponent<TapTapSdkBridgeHost>();
            }
        }
        sdkHost.Owner = this;
    }

    string NextSdkRequestId(string prefix)
    {
        sdkRequestSeq++;
        return prefix + "_" + sdkRequestSeq;
    }

    bool IsTapTapHostSdkAvailable()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try { return TapTapJS_IsAvailable() != 0; } catch { return false; }
#else
        return false;
#endif
    }

    // ---------- TapTap 激励视频广告调度 ----------
    void TickTapTapSdkBridge()
    {
        if (adInFlight && !adSettled)
        {
            adWatchdogTimer -= Time.unscaledDeltaTime;
            if (adWatchdogTimer <= 0f)
            {
                adSettled = true;
                adInFlight = false;
                RestoreAudioAfterRewardedAd();
                pendingAdGrantedCallback = null;
                Toast("广告响应超时，请稍后再试");
            }
        }
    }

    void PauseAudioForRewardedAd()
    {
        adMutedBgmTemporarily = false;
        try
        {
            if (music && music.isPlaying)
            {
                music.Pause();
                adMutedBgmTemporarily = true;
            }
            if (musicB && musicB.isPlaying)
            {
                musicB.Pause();
                adMutedBgmTemporarily = true;
            }
        }
        catch {}
    }

    void RestoreAudioAfterRewardedAd()
    {
        if (!adMutedBgmTemporarily) return;
        adMutedBgmTemporarily = false;
        try { SyncRegionalMusic(true); } catch {}
    }

    void RequestTapTapRewardedAd(string adKey, string itemName, Action onGranted)
    {
        EnsureTapTapSdkBridge();
        if (adInFlight)
        {
            Toast("赞助短片正在加载中，请稍候");
            return;
        }
        adInFlight = true;
        adSettled = false;
        adWatchdogTimer = AdWatchdogTimeoutSeconds;
        activeAdRequestId = NextSdkRequestId("ad");
        pendingAdGrantedCallback = onGranted;
        PauseAudioForRewardedAd();

#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            TapTapJS_ShowRewardVideoAd(activeAdRequestId, adKey ?? "", itemName ?? "");
            return;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[TapTapSDK] 调用 WebGL 激励广告失败：" + e.Message);
        }
#endif
        // Editor 或原生非 WebGL 环境下弹出统一预览确认通道（同样受单次结算守卫控制）
        string editorJson = JsonUtility.ToJson(new JsAdResult {
            requestId = activeAdRequestId,
            adKey = adKey ?? "",
            success = true,
            manualClose = false,
            errorCode = "",
            message = "editor/standalone fallback"
        });
        HandleTapTapAdCallback(editorJson);
    }

    public void HandleTapTapAdCallback(string json)
    {
        if (adSettled || !adInFlight) return;
        JsAdResult res = null;
        try { res = JsonUtility.FromJson<JsAdResult>(json); } catch {}
        if (res != null && !string.IsNullOrEmpty(activeAdRequestId) && !string.IsNullOrEmpty(res.requestId) && res.requestId != activeAdRequestId)
            return;

        adSettled = true;
        adInFlight = false;
        RestoreAudioAfterRewardedAd();

        var granted = pendingAdGrantedCallback;
        pendingAdGrantedCallback = null;

        if (res != null && res.success && !res.manualClose && res.message != "embed manual close")
        {
            granted?.Invoke();
            return;
        }

        if (res != null && (res.manualClose || res.message == "embed manual close"))
        {
            Toast("未完整观看短片，暂未发放奖励");
            return;
        }

        string errHint = res != null && !string.IsNullOrEmpty(res.message) ? res.message : "赞助短片暂无填充";
        Toast("广告加载失败：" + errHint);
    }
}
