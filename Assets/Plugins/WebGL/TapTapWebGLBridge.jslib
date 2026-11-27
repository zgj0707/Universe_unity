mergeInto(LibraryManager.library, {
  TapTapJS_IsAvailable: function () {
    if (typeof window === "undefined") return 0;
    var hasAd = (window.TapAdManager && typeof window.tap !== "undefined" && typeof window.tap.createRewardedVideoAd === "function") ||
      typeof window.ShowRewardVideoAd === "function" ||
      (window.tap && typeof window.tap.createRewardedVideoAd === "function") ||
      (window.tap && typeof window.tap.showRewardVideoAd === "function") ||
      (window.TapSDK && typeof window.TapSDK.showRewardVideoAd === "function");
    return hasAd ? 1 : 0;
  },

  TapTapJS_ShowRewardVideoAd: function (requestIdPtr, adKeyPtr, itemNamePtr) {
    var readStr = typeof UTF8ToString === "function" ? UTF8ToString : Pointer_stringify;
    var requestId = readStr(requestIdPtr);
    var adKey = readStr(adKeyPtr);
    var itemName = readStr(itemNamePtr);
    var settled = false;

    function sendResult(success, manualClose, errorCode, message) {
      if (settled) return;
      settled = true;
      var payload = JSON.stringify({
        requestId: requestId,
        adKey: adKey,
        success: !!success,
        manualClose: !!manualClose,
        errorCode: String(errorCode || ""),
        message: String(message || "")
      });
      if (typeof SendMessage === "function") {
        SendMessage("TapTapSdkBridgeHost", "OnJsAdCallback", payload);
      }
    }

    function resolveAdFn() {
      if (typeof window === "undefined") return null;
      if (window.TapAdManager && typeof window.tap !== "undefined" && typeof window.tap.createRewardedVideoAd === "function") {
        return function (opts) { return window.TapAdManager.showRewardedVideo(opts); };
      }
      if (typeof window.ShowRewardVideoAd === "function") return window.ShowRewardVideoAd;
      if (window.tap && typeof window.tap.showRewardVideoAd === "function") {
        return function (opts) { return window.tap.showRewardVideoAd(opts); };
      }
      if (window.TapSDK && typeof window.TapSDK.showRewardVideoAd === "function") {
        return function (opts) { return window.TapSDK.showRewardVideoAd(opts); };
      }
      return null;
    }

    var adFn = resolveAdFn();
    if (adFn) {
      try {
        adFn({
          adKey: adKey,
          onSuccess: function (res) {
            var msg = res && res.message ? String(res.message) : "";
            if (msg === "embed manual close") {
              sendResult(false, true, "MANUAL_CLOSE", msg);
              return;
            }
            if (!res || res.success === true) {
              sendResult(true, false, "", msg || "ok");
            } else {
              sendResult(false, false, res.code || "FAILED", msg || "ad failed");
            }
          },
          onError: function (res) {
            var msg = res && res.message ? String(res.message) : (res ? String(res) : "ad error");
            var code = res && res.code ? String(res.code) : "AD_ERROR";
            var isManual = msg === "embed manual close" || code === "MANUAL_CLOSE";
            sendResult(false, isManual, code, msg);
          }
        });
      } catch (e) {
        sendResult(false, false, "EXCEPTION", e && e.message ? e.message : "ad invoke exception");
      }
      return;
    }

    // 浏览器本地预览环境未注入宿主 TapTap 广告 SDK 时，弹出仿真激励短片倒计时框（支持正常看完或中途关闭）
    try {
      if (typeof document !== "undefined" && document.body) {
        var existing = document.getElementById("tt-preview-ad-overlay");
        if (existing && existing.parentNode) existing.parentNode.removeChild(existing);

        var overlay = document.createElement("div");
        overlay.id = "tt-preview-ad-overlay";
        overlay.style.cssText = "position:fixed;inset:0;z-index:999999;background:rgba(16,20,28,0.88);display:flex;align-items:center;justify-content:center;font-family:sans-serif;color:#f7f4ec;";

        var card = document.createElement("div");
        card.style.cssText = "width:min(88vw,420px);max-height:90vh;overflow-y:auto;background:#1e2633;border:1px solid rgba(255,255,255,0.16);border-radius:16px;padding:24px;box-sizing:border-box;text-align:center;box-shadow:0 20px 50px rgba(0,0,0,0.5);";

        var title = document.createElement("div");
        title.style.cssText = "font-size:18px;font-weight:700;margin-bottom:8px;";
        title.textContent = "TapTap 激励视频预览";

        var sub = document.createElement("div");
        sub.style.cssText = "font-size:14px;color:#b8c3d4;margin-bottom:18px;line-height:1.5;";
        sub.textContent = "奖励项目：" + (itemName || adKey) + "（当前为离线预览通道）";

        var timerEl = document.createElement("div");
        timerEl.style.cssText = "font-size:28px;font-weight:700;color:#7cc47f;margin-bottom:18px;";
        var remain = 3;
        timerEl.textContent = "还剩 " + remain + " 秒可领取奖励";

        var btnRow = document.createElement("div");
        btnRow.style.cssText = "display:flex;gap:12px;justify-content:center;";

        var claimBtn = document.createElement("button");
        claimBtn.disabled = true;
        claimBtn.style.cssText = "flex:1;padding:10px 14px;border:none;border-radius:10px;background:#49617a;color:#cbd5e1;font-size:14px;font-weight:600;cursor:not-allowed;";
        claimBtn.textContent = "请稍候...";

        var closeBtn = document.createElement("button");
        closeBtn.style.cssText = "padding:10px 16px;border:none;border-radius:10px;background:#593232;color:#ffd6d6;font-size:14px;cursor:pointer;";
        closeBtn.textContent = "提前关闭";

        function cleanup() {
          if (intervalId) { clearInterval(intervalId); intervalId = 0; }
          if (overlay && overlay.parentNode) overlay.parentNode.removeChild(overlay);
        }

        closeBtn.onclick = function () {
          cleanup();
          sendResult(false, true, "MANUAL_CLOSE", "embed manual close");
        };

        claimBtn.onclick = function () {
          if (claimBtn.disabled) return;
          cleanup();
          sendResult(true, false, "", "preview ad completed");
        };

        btnRow.appendChild(claimBtn);
        btnRow.appendChild(closeBtn);
        card.appendChild(title);
        card.appendChild(sub);
        card.appendChild(timerEl);
        card.appendChild(btnRow);
        overlay.appendChild(card);
        document.body.appendChild(overlay);

        var intervalId = setInterval(function () {
          remain -= 1;
          if (remain <= 0) {
            clearInterval(intervalId);
            intervalId = 0;
            timerEl.textContent = "已达成完整观看条件";
            claimBtn.disabled = false;
            claimBtn.style.background = "#589e5c";
            claimBtn.style.color = "#ffffff";
            claimBtn.style.cursor = "pointer";
            claimBtn.textContent = "领取奖励";
          } else {
            timerEl.textContent = "还剩 " + remain + " 秒可领取奖励";
          }
        }, 1000);
        return;
      }
    } catch (e) {}

    sendResult(false, false, "NO_HOST", "TapTap Ad SDK unavailable");
  }
});
