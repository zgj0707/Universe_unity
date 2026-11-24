using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 固定设计尺寸（1080×1920 竖屏）的画布自适应。
/// 窗口比设计更宽时按高度匹配（左右留背景），更窄时按宽度匹配（上下留背景），
/// 保证设计内容在任何窗口／设备比例下都完整可见，不会被裁切。
/// </summary>
[RequireComponent(typeof(CanvasScaler))]
public class CanvasLetterbox : MonoBehaviour
{
    public Vector2 designSize = new Vector2(1080f, 1920f);

    CanvasScaler scaler;
    int lastWidth = -1, lastHeight = -1;

    void Awake()
    {
        scaler = GetComponent<CanvasScaler>();
        Apply();
    }

    void Update()
    {
        if (Screen.width == lastWidth && Screen.height == lastHeight) return;
        Apply();
    }

    void Apply()
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;

        float screenAspect = (float)lastWidth / Mathf.Max(1, lastHeight);
        float designAspect = designSize.x / Mathf.Max(1f, designSize.y);

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = designSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = screenAspect >= designAspect ? 1f : 0f;
    }
}
