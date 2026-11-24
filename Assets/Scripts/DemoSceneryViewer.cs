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
                description = "静海纪念碑矗立于静海南部玄武岩平原，见证人类首次踏足月球正面的历史瞬间，如今已成为月之亮面最核心的星港地标。";
                return true;
            case "哥白尼中央峰":
                imagePath = "Scenery/moon_copernicus";
                description = "哥白尼环形山中央峰高达千米，周围辐射出壮观的亮白撞击溅射纹，是月之亮面最具辨识度的地质奇观。";
                return true;
            case "第谷辐射纹":
                imagePath = "Scenery/moon_tycho";
                description = "第谷环形山位于月球南半球高地，长达千余公里的明亮辐射纹纵横交错，在星舰俯瞰下宛如银白星芒。";
                return true;
            case "亚平宁绝壁":
                imagePath = "Scenery/moon_apennines";
                description = "亚平宁山脉绵延数百公里环抱雨海东缘，陡峭的山脊与深邃月谷构成月之亮面最雄伟的天然屏障。";
                return true;
            case "阿尔卑斯月谷":
                imagePath = "Scenery/moon_alpes";
                description = "阿尔卑斯大月谷如利剑般劈开山脉，连接雨海与冷海，是早期月球轨道穿梭车最惊险的天然航道。";
                return true;
            case "雨海熔岩海":
                imagePath = "Scenery/moon_imbrium";
                description = "雨海是月球正面最大的玄武岩盆地之一，平坦开阔的暗色熔岩海面最适合高速磁轨星舰长距离巡航。";
                return true;
            case "虹湾金弧":
                imagePath = "Scenery/moon_sinus_iridum";
                description = "虹湾呈完美的半圆形湾岸镶嵌在雨海西北缘，当阳光斜照湾口绝壁时，整座星港会泛起金色弧光。";
                return true;
            case "克拉维乌斯穹顶":
                imagePath = "Scenery/moon_clavius";
                description = "克拉维乌斯环形山巨型生态穹顶建于古老高地之上，内部保留着全月最完整的地下冰川与科研温室。";
                return true;
            case "柏拉图黑曜台":
                imagePath = "Scenery/moon_plato";
                description = "柏拉图环形山平坦如镜的暗色盆底被称为“黑曜台”，也是通往火星星域的重要深空跃迁节点。";
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
            var dotImage = Img(dot, new Color(0.72f, 0.80f, 0.70f, 0.9f), spriteCircle);
            var dotLabel = Txt(dot.transform, sc.name, 22, new Color(0.22f, 0.30f, 0.22f, 0.95f), -60, -36, 180, 32, TextAnchor.MiddleCenter);
            TextShadow(dotLabel, new Color(1, 1, 1, 0.85f));
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
        var badge = Rect("UnvisitedBadge", marker.transform, 51, 51, 20, 20);
        Img(badge, new Color(0.98f, 0.96f, 0.87f, 1f), spriteCircle).raycastTarget = false;
        Img(Rect("RedDot", badge.transform, 3, 3, 14, 14), new Color(0.85f, 0.23f, 0.20f, 1f), spriteCircle).raycastTarget = false;
        badge.SetActive(!sc.found);
        sceneryMarkerBadges[sc] = badge;
        var label = Txt(marker.transform, sc.name, 22, new Color(0.22f, 0.30f, 0.22f, 0.98f), -65, -32, 198, 32, TextAnchor.MiddleCenter);
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
        var info = Rect("Info", controls.transform, 1330, 220, 520, 590);
        Rounded(info, new Color(0.10f, 0.14f, 0.13f, 0.76f));
        Txt(info.transform, sc.region + (sc.natural ? " · 自然景观" : " · 人文景观"), 25, new Color(0.85f, 0.91f, 0.83f), 36, 500, 448, 40, TextAnchor.MiddleLeft);
        Txt(info.transform, sc.name, 58, Color.white, 36, 405, 448, 80, TextAnchor.MiddleLeft);
        Txt(info.transform, description, 30, new Color(0.96f, 0.96f, 0.91f), 36, 160, 448, 220, TextAnchor.UpperLeft);
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
