using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 全星域表驱动地图底图加载、地形标注与低显存按需卸载管理。
/// </summary>
public partial class DemoBootstrap
{
    void BuildAllRegionMaps(Transform parent)
    {
        regionMapArts.Clear();
        regionMapRaws.Clear();
        for (int i = 0; i < RegionOrder.Length; i++)
        {
            string id = RegionOrder[i];
            var spec = GetRegionSpec(id);
            if (spec == null) continue;
            var go = BuildSingleRegionMap(parent, spec);
            regionMapArts[id] = go;
            go.SetActive(id == currentRegion);
        }
        moonNearMapArt = regionMapArts.TryGetValue("月之亮面", out var moonNear) ? moonNear : null;
        moonFarMapArt = regionMapArts.TryGetValue("月背", out var moonFar) ? moonFar : null;
        marsWestMapArt = regionMapArts.TryGetValue("火星·西半球", out var marsWest) ? marsWest : null;
    }

    GameObject BuildSingleRegionMap(Transform parent, RegionSpec spec)
    {
        float mw = W * spec.canvasWidthScale;
        float x0 = spec.canvasOffsetX * W - mw;
        var go = Rect("RegionArt_" + spec.id, parent, 0, 0, W, H);
        var texGo = Rect("MapTex_" + spec.id, go.transform, x0, -H, mw * 3f, H * 3f);
        var raw = texGo.AddComponent<RawImage>();
        raw.raycastTarget = false;
        raw.uvRect = new Rect(-1f, -1f, 3f, 3f);
        if (qualityTier > 0 || currentRegion == spec.id)
            SetMirrorTexture(raw, Resources.Load<Texture2D>(spec.texResource));
        raw.color = Themes[Mathf.Clamp(currentTheme, 0, Themes.Length - 1)].mapTint;
        regionMapRaws[spec.id] = raw;

        // 若存在对应区域的离线天堑/陡坡叠加图层，自动挂载（默认隐藏，长按开线/编线时显示）
        string overlayPath = spec.texResource.Replace("MapArt/", "MapData/") + "_hazard_overlay";
        var overlayTex = Resources.Load<Texture2D>(overlayPath);
        if (overlayTex)
        {
            var ovGo = Rect("HazardOverlay_" + spec.id, go.transform, spec.canvasOffsetX * W, 0, mw, H);
            var ovRaw = ovGo.AddComponent<RawImage>();
            ovRaw.texture = overlayTex;
            ovRaw.color = new Color(1f, 1f, 1f, 0.82f);
            ovRaw.raycastTarget = false;
            ovGo.SetActive(hazardOverlayHighlighted && spec.id == currentRegion);
            regionHazardOverlays[spec.id] = ovGo;
        }

        if (spec.id == "月之亮面") moonNearMapRaw = raw;
        else if (spec.id == "月背") moonFarMapRaw = raw;
        else if (spec.id == "火星·西半球") marsWestMapRaw = raw;

        var rt = go.GetComponent<RectTransform>();
        var labelCol = new Color(1f, 0.96f, 0.80f, 0.88f);
        if (spec.terrainLabels != null)
        {
            for (int i = 0; i < spec.terrainLabels.Length; i++)
            {
                var lbl = spec.terrainLabels[i];
                Vector2 p = GeoToRegionCanvas(spec.id, lbl.lon, lbl.lat);
                Label(rt, lbl.text, lbl.size, p.x, p.y, labelCol, 240);
            }
        }
        if (spec.id == "月背")
        {
            Label(rt, "莫斯科海", 25, MoonFarX(147.9f), MoonFarY(27.3f), labelCol, 180);
            Label(rt, "科罗廖夫环", 24, MoonFarX(202.6f), MoonFarY(-4.0f), labelCol, 170);
            Label(rt, "艾特肯盆地", 24, MoonFarX(173.4f), MoonFarY(-24.0f), labelCol, 170);
            Label(rt, "阿波罗环山", 24, MoonFarX(208.2f), MoonFarY(-36.1f), labelCol, 170);
            var credit = Txt(go.transform, "月背星图 · LRO 地形投影", 20, new Color(1f, 1f, 1f, 0.68f), 540, 188, 520, 32, TextAnchor.MiddleRight);
            TextShadow(credit, new Color(0f, 0f, 0f, 0.35f));
            credit.raycastTarget = false;
        }
        else if (spec.planet == "Mars")
        {
            var hint = Txt(go.transform, spec.displayTitle + " · MOLA 实景投影", 21, new Color(1f, 1f, 1f, 0.72f), 520, 212, 520, 32, TextAnchor.MiddleRight);
            TextShadow(hint, new Color(0f, 0f, 0f, 0.35f));
            hint.raycastTarget = false;
        }
        return go;
    }

    void SetActiveRegionMapArt(string activeId)
    {
        foreach (var kv in regionMapArts)
            if (kv.Value) kv.Value.SetActive(kv.Key == activeId);
    }

    static void SetMirrorTexture(RawImage raw, Texture2D tex)
    {
        if (!raw) return;
        if (tex) tex.wrapMode = TextureWrapMode.Mirror;
        raw.texture = tex;
    }

    // 省电/低显存模式仅在显存保留当前区地图纹理，中/高档常驻实现切区瞬开。
    void SyncRegionMapTextureMemory()
    {
        bool lowMem = qualityTier <= 0;
        for (int i = 0; i < RegionOrder.Length; i++)
        {
            string id = RegionOrder[i];
            var spec = GetRegionSpec(id);
            if (spec == null) continue;
            if (regionMapRaws.TryGetValue(id, out var raw))
                SyncSingleRegionTexture(raw, !lowMem || currentRegion == id, spec.texResource);
        }
        if (planetBackdropRaw)
        {
            var curSpec = GetRegionSpec(currentRegion);
            string activePath = curSpec != null ? curSpec.texResource : "MapArt/moon_nearside";
            SetMirrorTexture(planetBackdropRaw, Resources.Load<Texture2D>(activePath));
            planetBackdropRaw.color = Themes[Mathf.Clamp(currentTheme, 0, Themes.Length - 1)].mapTint;
        }
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
            SetMirrorTexture(raw, Resources.Load<Texture2D>(resourcePath));
        }
    }
}
