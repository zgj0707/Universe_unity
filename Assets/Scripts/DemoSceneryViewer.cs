using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public partial class DemoBootstrap
{
    GameObject sceneryViewer;
    readonly Dictionary<Scenery, GameObject> sceneryMarkerBadges = new Dictionary<Scenery, GameObject>();

    bool TryGetSceneryContent(Scenery sc, out string imagePath, out string description)
    {
        switch (sc.name)
        {
            case "静海纪念碑":
                imagePath = "Scenery/moon_tranquility";
                description = "静海纪念碑矗立于静海南部玄武岩平原，见证人类首次踏足月球正面的历史瞬间，如今已成为月之亮面东部的核心星港地标。";
                return true;
            case "冷海玄冰谷":
                imagePath = "Scenery/moon_frigoris";
                description = "冷海玄冰谷位于月球北纬高寒阴影带，岩层缝隙中保存着极地水冰矿脉，是月面北线重要的科研补给奇观。";
                return true;
            case "第谷辐射纹":
                imagePath = "Scenery/moon_tycho";
                description = "第谷环形山位于月球南半球高地，长达千余公里的明亮辐射纹纵横交错，在星舰俯瞰下宛如银白星芒。";
                return true;
            case "亚平宁绝壁":
                imagePath = "Scenery/moon_apennines";
                description = "亚平宁山脉绵延数百公里环抱雨海东缘，陡峭的山脊与深邃月谷构成月之亮面中部最雄伟的天然屏障。";
                return true;
            case "梦湖辉石崖":
                imagePath = "Scenery/moon_somniorum";
                description = "梦湖辉石崖矗立于东北熔岩湖岸，斜阳照射下富含钛铁的岩壁会折射出微弱幽蓝金属光泽。";
                return true;
            case "开普勒辐射环":
                imagePath = "Scenery/moon_kepler";
                description = "开普勒撞击坑位于风暴洋东缘，高反照率溅射物在暗色月海上勾勒出清晰星环，是西部航道的醒目信标。";
                return true;
            case "虹湾金弧":
                imagePath = "Scenery/moon_sinus_iridum";
                description = "虹湾呈完美的半圆形湾岸镶嵌在雨海西北缘，当阳光斜照湾口绝壁时，整座星港会泛起金色弧光。";
                return true;
            case "云海直壁断崖":
                imagePath = "Scenery/moon_rupes_recta";
                description = "横亘云海之上的“月面直壁”长达百余公里，笔直如刀削的断层崖壁在晨昏线下投出锋利长影。";
                return true;
            case "阿尔泰峭壁":
                imagePath = "Scenery/moon_altai";
                description = "阿尔泰峭壁沿酒海西南缘蜿蜒绵延近五百公里，古老的撞击盆地外环断崖记录着月球早期剧烈撞击史。";
                return true;
            case "南极艾特肯深渊":
                imagePath = "Scenery/moon_aitken";
                description = "南极-艾特肯盆地是太阳系已知最古老、最深邃的撞击盆地之一，蕴藏着丰富的月幔深层矿脉。";
                return true;
            case "冯·卡门着陆碑":
                imagePath = "Scenery/moon_vonkarman";
                description = "冯·卡门撞击坑位于月背南极-艾特肯盆地内，是人类首座背面软着陆探测器的纪念星港。";
                return true;
            case "莫斯科海玄武原":
                imagePath = "Scenery/moon_moscoviense";
                description = "莫斯科海是月球背面极少数拥有广阔暗色玄武岩平原的月海，也是月背北线星轨的核心中转区。";
                return true;
            case "科罗廖夫冰丘":
                imagePath = "Scenery/moon_korolev";
                description = "科罗廖夫环形山常年受极地阴影庇护，坑底保存着数米厚的纯净水冰层，为深空星舰提供宝贵燃料。";
                return true;
            case "阿波罗环山观测塔":
                imagePath = "Scenery/moon_apollo";
                description = "阿波罗环山观测塔矗立于双环撞击盆地边缘，彻底屏蔽了地球无线电干扰，是深空射电天文的圣地。";
                return true;
            default:
                imagePath = null; description = null;
                return false;
        }
    }

    void BuildSceneryMarker(Scenery sc)
    {
        if (sc.region != currentRegion) return;
        if (!sc.unlocked || !TryGetSceneryContent(sc, out _, out _))
        {
            var dot = Rect("Sc_" + sc.name, stationLayer, MAP_X + sc.x * MAP_W, MAP_Y + sc.y * MAP_H, 40, 40);
            dot.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            var dotImage = Img(dot, new Color(0.52f, 0.88f, 0.68f, 0.94f), spriteCircle);
            var dotLabel = Txt(dot.transform, sc.name, 21, new Color(0.88f, 1.00f, 0.92f, 1.00f), -110, 28, 260, 32, TextAnchor.MiddleCenter);
            dotLabel.font = stationFont;
            dotLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            dotLabel.verticalOverflow = VerticalWrapMode.Overflow;
            dotLabel.raycastTarget = false;
            TextShadow(dotLabel, new Color(0.0f, 0.0f, 0.0f, 0.55f));
            var dotButton = dot.AddComponent<Button>(); dotButton.targetGraphic = dotImage;
            dotButton.onClick.AddListener(() => Toast(sc.name + (sc.unlocked ? " · 大图筹备中" : " · 连通" + sc.triggerStation + "站解锁")));
            return;
        }
        var marker = Rect("Sc_" + sc.name, stationLayer, MAP_X + sc.x * MAP_W, MAP_Y + sc.y * MAP_H, 68, 68);
        marker.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
        var background = Img(marker, new Color(0.98f, 0.96f, 0.87f, 0.97f), spriteCircle);
        SoftShadow(marker);
        var texture = Resources.Load<Texture2D>(sc.natural ? "MapIcons/scenery_nature" : "MapIcons/scenery_culture");
        if (texture)
        {
            var icon = Rect("CategoryIcon", marker.transform, 5, 5, 58, 58).AddComponent<RawImage>();
            icon.texture = texture; icon.raycastTarget = false;
        }
        else
        {
            // 分类图标已移除：以 spriteCircle 色块徽章回退，避免地图标记留白
            Img(Rect("CategoryFallback", marker.transform, 5, 5, 58, 58),
                sc.natural ? new Color(0.45f, 0.68f, 0.50f, 0.85f) : new Color(0.66f, 0.55f, 0.72f, 0.85f),
                spriteCircle).raycastTarget = false;
        }
        var badge = Rect("UnvisitedBadge", marker.transform, 51, 51, 20, 20);
        Img(badge, new Color(0.98f, 0.96f, 0.87f, 1f), spriteCircle).raycastTarget = false;
        Img(Rect("RedDot", badge.transform, 3, 3, 14, 14), new Color(0.85f, 0.23f, 0.20f, 1f), spriteCircle).raycastTarget = false;
        badge.SetActive(!sc.found);
        sceneryMarkerBadges[sc] = badge;
        var label = Txt(marker.transform, sc.name, 22, new Color(0.22f, 0.30f, 0.22f, 0.98f), -95, -34, 250, 32, TextAnchor.MiddleCenter);
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        TextShadow(label, new Color(1, 1, 1, 0.9f));
        var button = marker.AddComponent<Button>(); button.targetGraphic = background;
        button.onClick.AddListener(() => OpenSceneryViewer(sc));
    }

    void RefreshSceneryViews()
    {
        if (!stationLayer) return;
        foreach (Transform child in stationLayer)
            if (child.name.StartsWith("Sc_")) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        sceneryMarkerBadges.Clear();
        foreach (var scenery in sceneries) BuildSceneryMarker(scenery);
    }

    void RefreshSceneryMarkerBadge(Scenery sc)
    {
        if (sceneryMarkerBadges.TryGetValue(sc, out var badge) && badge) badge.SetActive(!sc.found);
    }

    void OpenSceneryViewer(Scenery sc, System.Action after = null)
    {
        if (sc == null || !sc.unlocked || !TryGetSceneryContent(sc, out string imagePath, out string description)) return;
        var texture = Resources.Load<Texture2D>(imagePath);
        if (!texture) { Toast(sc.name + " · 图片暂不可用"); return; }
        bool newlyCheckedIn = CompleteSceneryCheckin(sc);
        if (sceneryViewer) Destroy(sceneryViewer);
        CloseLineBubble();
        sceneryViewer = new GameObject("SceneryViewer", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(SceneryViewerLayout));
        SyncRegionalMusic(false);
        var viewerCanvas = sceneryViewer.GetComponent<Canvas>();
        viewerCanvas.renderMode = RenderMode.ScreenSpaceOverlay; viewerCanvas.overrideSorting = true; viewerCanvas.sortingOrder = 100;
        var backdrop = Rect("Backdrop", sceneryViewer.transform, 0, 0, 0, 0);
        var backRt = backdrop.GetComponent<RectTransform>(); backRt.anchorMin = Vector2.zero; backRt.anchorMax = Vector2.one;
        backRt.offsetMin = backRt.offsetMax = Vector2.zero;
        Img(backdrop, new Color(0.06f, 0.08f, 0.07f, 1));
        var stage = Rect("Landscape", sceneryViewer.transform, 0, 0, 1920, 1080);
        var stageRt = stage.GetComponent<RectTransform>();
        stageRt.anchorMin = stageRt.anchorMax = new Vector2(0.5f, 0.5f); stageRt.pivot = new Vector2(0.5f, 0.5f); stageRt.anchoredPosition = Vector2.zero;
        sceneryViewer.GetComponent<SceneryViewerLayout>().stage = stageRt;
        var picture = Rect("Picture", stage.transform, 0, 0, 1920, 1080);
        var raw = picture.AddComponent<RawImage>(); raw.texture = texture; raw.color = Color.white;
        var controls = Rect("Controls", stage.transform, 0, 0, 1920, 1080);
        var reveal = picture.AddComponent<Button>(); reveal.targetGraphic = raw;
        reveal.onClick.AddListener(() => controls.SetActive(true));
        var info = Rect("Info", controls.transform, 1300, 190, 550, 650);
        Rounded(info, new Color(0.10f, 0.14f, 0.13f, 0.78f));
        var postcard = GetSceneryPostcardCopy(sc);
        Txt(info.transform, sc.region + (sc.natural ? " · 复古邮戳明信片" : " · 人文纪念明信片"), 24, new Color(0.85f, 0.91f, 0.83f), 34, 578, 482, 36, TextAnchor.MiddleLeft);
        Txt(info.transform, sc.name, 54, Color.white, 34, 486, 482, 80, TextAnchor.MiddleLeft);
        Txt(info.transform, postcard.guideText, 27, new Color(0.96f, 0.96f, 0.91f), 34, 246, 482, 224, TextAnchor.UpperLeft);
        var noteTxt = Txt(info.transform, postcard.travelerNote, 24, new Color(0.95f, 0.86f, 0.64f), 34, 48, 482, 176, TextAnchor.UpperLeft);
        noteTxt.fontStyle = FontStyle.Italic;
        if (newlyCheckedIn)
        {
            var notification = Rect("CheckinToast", stage.transform, 560, 850, 800, 88);
            Rounded(notification, new Color(0.10f, 0.13f, 0.12f, 0.88f));
            Txt(notification.transform, "打卡成功 · 钻石 +20", 32, Color.white, 20, 10, 760, 68, TextAnchor.MiddleCenter);
            notification.GetComponent<Image>().raycastTarget = false;
            Destroy(notification, 3f);
        }
        var back = Btn(controls.transform, "Back", "←", 40, 24, 958, 94, 94, new Color(0.12f, 0.16f, 0.15f, 0.75f), () => { Destroy(sceneryViewer); sceneryViewer = null; RequestResourceTrim(false); after?.Invoke(); if (pageRoot == null) SyncRegionalMusic(false); });
        TInk(back, Color.white);
        var home = Btn(controls.transform, "Home", "主页", 24, 134, 958, 104, 94, new Color(0.12f, 0.16f, 0.15f, 0.75f), GoHome);
        TInk(home, Color.white);
        var heading = Txt(controls.transform, sc.name, 44, Color.white, 254, 975, 800, 60, TextAnchor.MiddleLeft);
        heading.resizeTextForBestFit = true; heading.resizeTextMinSize = 24; heading.resizeTextMaxSize = ScaledFontSize(44);
        var clear = Btn(controls.transform, "Clear", "清屏", 30, 1690, 44, 180, 70, new Color(0.12f, 0.16f, 0.15f, 0.75f), () => controls.SetActive(false));
        sceneryViewer.GetComponent<SceneryViewerLayout>().clear = clear.GetComponent<RectTransform>();
        TInk(clear, Color.white);
    }
}

public class SceneryViewerLayout : MonoBehaviour
{
    public RectTransform stage, clear;
    int lastWidth = -1, lastHeight = -1;
    void LateUpdate()
    {
        if (!stage) return;
        if (Screen.width == lastWidth && Screen.height == lastHeight) return;
        lastWidth = Screen.width;
        lastHeight = Screen.height;
        bool portrait = lastHeight > lastWidth;
        float width = portrait ? lastHeight : lastWidth;
        float height = portrait ? lastWidth : lastHeight;
        float scale = Mathf.Min(width / 1920f, height / 1080f);
        stage.localScale = Vector3.one * scale;
        stage.localRotation = Quaternion.Euler(0, 0, portrait ? 90 : 0);
        if (clear) clear.anchoredPosition = new Vector2(portrait ? 1370 : 1690, 44);
    }
}
