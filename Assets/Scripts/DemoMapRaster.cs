using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 三个区域共用清新手绘风格的地图底图。
/// </summary>
public partial class DemoBootstrap
{
    void BuildRasterMap(Transform parent)
    {
        var go = Rect("BaseMap", parent, 0, 0, W, H);
        var raw = go.AddComponent<RawImage>();
        if (qualityTier > 0 || currentRegion == "月之亮面")
            raw.texture = Resources.Load<Texture2D>("MapArt/moon_nearside");
        raw.color = Themes[Mathf.Clamp(currentTheme, 0, Themes.Length - 1)].mapTint;
        raw.raycastTarget = false;
        hkMapRaw = raw;
    }

    // 火星地图为手绘风格示意，不绘制行政边界。
    GameObject BuildGuangdongMap(Transform parent)
    {
        var go = Rect("GuangdongArt", parent, 0, 0, W, H);
        var raw = go.AddComponent<RawImage>(); raw.raycastTarget = false;
        if (qualityTier > 0 || currentRegion == "火星")
            raw.texture = Resources.Load<Texture2D>("MapArt/mars_tharsis");
        raw.color = Themes[Mathf.Clamp(currentTheme, 0, Themes.Length - 1)].mapTint;
        guangdongMapRaw = raw;
        var rt = go.GetComponent<RectTransform>();
        Label(rt, "火 星", 34, 0.48f, 0.84f, new Color(0.40f,0.38f,0.32f,0.70f), 200);
        var hint = Txt(go.transform, "放大星图查看星港站点", 22, new Color(0.28f,0.34f,0.31f,0.78f), 580, 230, 450, 36, TextAnchor.MiddleRight);
        hint.raycastTarget = false;
        return go;
    }

    // 月背示意轮廓为手绘海湾图。
    GameObject BuildMacauMap(Transform parent)
    {
        var go = Rect("MacauArt", parent, 0, 0, W, H);
        var raw = go.AddComponent<RawImage>(); raw.raycastTarget = false;
        if (qualityTier > 0 || currentRegion == "月背")
            raw.texture = Resources.Load<Texture2D>("MapArt/moon_farside");
        raw.color = Themes[Mathf.Clamp(currentTheme, 0, Themes.Length - 1)].mapTint;
        macauMapRaw = raw;
        var rt = go.GetComponent<RectTransform>();
        var labelColor = new Color(0.40f,0.38f,0.32f,0.70f);
        Label(rt, "莫斯科海", 25, MoonFarX(147.9f), MoonFarY(27.3f), labelColor, 180);
        Label(rt, "科罗廖夫环", 24, MoonFarX(202.6f), MoonFarY(-4.0f), labelColor, 170);
        Label(rt, "艾特肯盆地", 24, MoonFarX(173.4f), MoonFarY(-24.0f), labelColor, 170);
        Label(rt, "阿波罗环山", 24, MoonFarX(208.2f), MoonFarY(-36.1f), labelColor, 170);
        var credit = Txt(go.transform, "月背星图 · LRO 地形投影", 20, new Color(0.22f,0.30f,0.31f,0.72f), 540, 188, 520, 32, TextAnchor.MiddleRight);
        credit.raycastTarget = false;
        return go;
    }

    // 省电/低显存模式仅在显存保留当前区地图纹理，中/高档常驻三区实现切区瞬开。
    void SyncRegionMapTextureMemory()
    {
        bool lowMem = qualityTier <= 0;
        SyncSingleRegionTexture(hkMapRaw, !lowMem || currentRegion == "月之亮面", "MapArt/moon_nearside");
        SyncSingleRegionTexture(guangdongMapRaw, !lowMem || currentRegion == "火星", "MapArt/mars_tharsis");
        SyncSingleRegionTexture(macauMapRaw, !lowMem || currentRegion == "月背", "MapArt/moon_farside");
    }

    static void SyncSingleRegionTexture(RawImage raw, bool keepLoaded, string resourcePath)
    {
        if (!raw) return;
        if (!keepLoaded)
        {
            var oldTex = raw.texture;
            raw.texture = null;
            if (oldTex) Resources.UnloadAsset(oldTex);
        }
        else if (!raw.texture)
        {
            raw.texture = Resources.Load<Texture2D>(resourcePath);
        }
    }
}
