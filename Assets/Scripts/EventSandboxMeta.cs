using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class DemoBootstrap
{
    public sealed class SandboxTreeNodeSpec
    {
        public string key, eventId, branchName, title, desc, parentKey, exclusiveKey, unlockVehicleKey;
        public int branch, tier, cost;
        public float treeX, treeY; // 树状画布归一化坐标 (0..1)
    }

    static readonly SandboxTreeNodeSpec[] SandboxTreeNodes =
    {
        // —— 赤尘撤离志（20 节点可缩放分支科技树） ——
        new SandboxTreeNodeSpec { key = "ash_sci_1", eventId = EventIdAshDome, branch = 3, tier = 1, branchName = "通用预案", title = "应急冷却剂", desc = "灾变圈层扩散推迟 1 天（全流派通用）", cost = 8, treeX = 0.25f, treeY = 0.89f },
        new SandboxTreeNodeSpec { key = "ash_sci_2", eventId = EventIdAshDome, branch = 3, tier = 2, branchName = "通用预案", title = "磁约束水闸", desc = "灾变圈层扩散再推迟 1 天", cost = 16, parentKey = "ash_sci_1", treeX = 0.50f, treeY = 0.89f },
        new SandboxTreeNodeSpec { key = "ash_sci_3", eventId = EventIdAshDome, branch = 3, tier = 3, branchName = "通用预案", title = "深层导风井", desc = "全线撤离收益 +15%", cost = 22, parentKey = "ash_sci_2", treeX = 0.75f, treeY = 0.89f },

        new SandboxTreeNodeSpec { key = "ash_cap_1", eventId = EventIdAshDome, branch = 0, tier = 1, branchName = "重装运力", title = "加挂车厢", desc = "撤离列车运力 +15%", cost = 6, treeX = 0.17f, treeY = 0.71f },
        new SandboxTreeNodeSpec { key = "ash_cap_2", eventId = EventIdAshDome, branch = 0, tier = 2, branchName = "重装运力", title = "双层客舱", desc = "运力提升至 +30%（与长程连挂互斥）", cost = 12, parentKey = "ash_cap_1", exclusiveKey = "ash_cap_2b", treeX = 0.09f, treeY = 0.53f },
        new SandboxTreeNodeSpec { key = "ash_cap_2b", eventId = EventIdAshDome, branch = 0, tier = 2, branchName = "重装运力", title = "长程连挂", desc = "老城轨道上限 +3 条（与双层客舱互斥）", cost = 12, parentKey = "ash_cap_1", exclusiveKey = "ash_cap_2", treeX = 0.25f, treeY = 0.53f },
        new SandboxTreeNodeSpec { key = "ash_cap_3", eventId = EventIdAshDome, branch = 0, tier = 3, branchName = "重装运力", title = "铁壁重载组", desc = "获赠「铁壁」护航车并反哺主线涂装", cost = 20, unlockVehicleKey = "veh_ash_iron", treeX = 0.17f, treeY = 0.35f },
        new SandboxTreeNodeSpec { key = "ash_cap_4", eventId = EventIdAshDome, branch = 0, tier = 4, branchName = "重装运力", title = "重载制动", desc = "列车编组转运效率再 +20%", cost = 26, parentKey = "ash_cap_3", treeX = 0.17f, treeY = 0.16f },

        new SandboxTreeNodeSpec { key = "ash_def_1", eventId = EventIdAshDome, branch = 1, tier = 1, branchName = "固守延缓", title = "防尘滤网", desc = "沦陷站滞留缓冲 +0.5 天", cost = 6, treeX = 0.50f, treeY = 0.71f },
        new SandboxTreeNodeSpec { key = "ash_def_2", eventId = EventIdAshDome, branch = 1, tier = 2, branchName = "固守延缓", title = "装甲蒙皮", desc = "滞留缓冲提升至 +1.5 天（与冷却注浆互斥）", cost = 14, parentKey = "ash_def_1", exclusiveKey = "ash_def_2b", treeX = 0.41f, treeY = 0.53f },
        new SandboxTreeNodeSpec { key = "ash_def_2b", eventId = EventIdAshDome, branch = 1, tier = 2, branchName = "固守延缓", title = "冷却注浆", desc = "沙尘圈层扩散推迟 1 次（与装甲蒙皮互斥）", cost = 14, parentKey = "ash_def_1", exclusiveKey = "ash_def_2", treeX = 0.59f, treeY = 0.53f },
        new SandboxTreeNodeSpec { key = "ash_def_3", eventId = EventIdAshDome, branch = 1, tier = 3, branchName = "固守延缓", title = "晨曦旗舰", desc = "获赠「晨曦号」旗舰并反哺主线涂装", cost = 24, unlockVehicleKey = "veh_ash_ark", treeX = 0.50f, treeY = 0.35f },
        new SandboxTreeNodeSpec { key = "ash_def_4", eventId = EventIdAshDome, branch = 1, tier = 4, branchName = "固守延缓", title = "穹顶闭环", desc = "沦陷区滞留缓冲再 +1 天", cost = 28, parentKey = "ash_def_3", treeX = 0.50f, treeY = 0.16f },

        new SandboxTreeNodeSpec { key = "ash_res_1", eventId = EventIdAshDome, branch = 2, tier = 1, branchName = "医疗速援", title = "医疗车厢", desc = "避风港满员溢出滞留 -30%", cost = 6, treeX = 0.83f, treeY = 0.71f },
        new SandboxTreeNodeSpec { key = "ash_res_2", eventId = EventIdAshDome, branch = 2, tier = 2, branchName = "医疗速援", title = "避风港扩容", desc = "避风港吞吐量 +30%（与速援补贴互斥）", cost = 14, parentKey = "ash_res_1", exclusiveKey = "ash_res_2b", treeX = 0.75f, treeY = 0.53f },
        new SandboxTreeNodeSpec { key = "ash_res_2b", eventId = EventIdAshDome, branch = 2, tier = 2, branchName = "医疗速援", title = "速援补贴", desc = "转运反哺主线金币 +20%（与扩容互斥）", cost = 14, parentKey = "ash_res_1", exclusiveKey = "ash_res_2", treeX = 0.91f, treeY = 0.53f },
        new SandboxTreeNodeSpec { key = "ash_res_3", eventId = EventIdAshDome, branch = 2, tier = 3, branchName = "医疗速援", title = "白鸽专列", desc = "获赠「白鸽」医疗专列并反哺主线涂装", cost = 22, unlockVehicleKey = "veh_ash_dove", treeX = 0.83f, treeY = 0.35f },
        new SandboxTreeNodeSpec { key = "ash_res_4", eventId = EventIdAshDome, branch = 2, tier = 4, branchName = "医疗速援", title = "远郊救护站", desc = "避风港吞吐再 +20%，主线分红 +15%", cost = 26, parentKey = "ash_res_3", treeX = 0.83f, treeY = 0.16f },

        new SandboxTreeNodeSpec { key = "ash_uni_a", eventId = EventIdAshDome, branch = 3, tier = 4, branchName = "调度统筹", title = "跨线接力站", desc = "全城火种券产出 +15%", cost = 18, parentKey = "ash_sci_1", treeX = 0.33f, treeY = 0.06f },
        new SandboxTreeNodeSpec { key = "ash_uni_b", eventId = EventIdAshDome, branch = 3, tier = 4, branchName = "调度统筹", title = "末班车调度令", desc = "老城轨道上限再 +2 条", cost = 20, parentKey = "ash_sci_2", treeX = 0.67f, treeY = 0.06f },

        // —— 长夜车行（20 节点可缩放分支科技树） ——
        new SandboxTreeNodeSpec { key = "neon_night_1", eventId = EventIdNeonNight, branch = 3, tier = 1, branchName = "通用夜航", title = "热点预测", desc = "夜班订单提前 30 分钟在站台显示", cost = 8, treeX = 0.25f, treeY = 0.89f },
        new SandboxTreeNodeSpec { key = "neon_night_2", eventId = EventIdNeonNight, branch = 3, tier = 2, branchName = "通用夜航", title = "加班灯牌", desc = "夜间巡游整体车资提升 +10%", cost = 16, parentKey = "neon_night_1", treeX = 0.50f, treeY = 0.89f },
        new SandboxTreeNodeSpec { key = "neon_night_3", eventId = EventIdNeonNight, branch = 3, tier = 3, branchName = "通用夜航", title = "夜宵专线", desc = "夜班订单生成速度 +20%", cost = 22, parentKey = "neon_night_2", treeX = 0.75f, treeY = 0.89f },

        new SandboxTreeNodeSpec { key = "neon_fleet_1", eventId = EventIdNeonNight, branch = 0, tier = 1, branchName = "车队扩张", title = "增发牌照", desc = "全城夜班出租牌照上限 +2", cost = 6, treeX = 0.17f, treeY = 0.71f },
        new SandboxTreeNodeSpec { key = "neon_fleet_2", eventId = EventIdNeonNight, branch = 0, tier = 2, branchName = "车队扩张", title = "快速整备", desc = "牌照上限再 +3（与增设夜线互斥）", cost = 12, parentKey = "neon_fleet_1", exclusiveKey = "neon_fleet_2b", treeX = 0.09f, treeY = 0.53f },
        new SandboxTreeNodeSpec { key = "neon_fleet_2b", eventId = EventIdNeonNight, branch = 0, tier = 2, branchName = "车队扩张", title = "增设夜线", desc = "不夜港线路上限 +3 条（与快速整备互斥）", cost = 12, parentKey = "neon_fleet_1", exclusiveKey = "neon_fleet_2", treeX = 0.25f, treeY = 0.53f },
        new SandboxTreeNodeSpec { key = "neon_fleet_3", eventId = EventIdNeonNight, branch = 0, tier = 3, branchName = "车队扩张", title = "零号传承", desc = "获赠「零号老的士」并反哺主线涂装", cost = 24, unlockVehicleKey = "veh_neon_zero", treeX = 0.17f, treeY = 0.35f },
        new SandboxTreeNodeSpec { key = "neon_fleet_4", eventId = EventIdNeonNight, branch = 0, tier = 4, branchName = "车队扩张", title = "老车队编组", desc = "车队订单小费额外 +15%", cost = 26, parentKey = "neon_fleet_3", treeX = 0.17f, treeY = 0.16f },

        new SandboxTreeNodeSpec { key = "neon_disp_1", eventId = EventIdNeonNight, branch = 1, tier = 1, branchName = "跨区调度", title = "邻站抢单", desc = "允许绕行相邻街区跨站接单", cost = 6, treeX = 0.50f, treeY = 0.71f },
        new SandboxTreeNodeSpec { key = "neon_disp_2", eventId = EventIdNeonNight, branch = 1, tier = 2, branchName = "跨区调度", title = "免费换乘", desc = "双线换乘不再扣除 10% 车费（与远郊互斥）", cost = 14, parentKey = "neon_disp_1", exclusiveKey = "neon_disp_2b", treeX = 0.41f, treeY = 0.53f },
        new SandboxTreeNodeSpec { key = "neon_disp_2b", eventId = EventIdNeonNight, branch = 1, tier = 2, branchName = "跨区调度", title = "远郊津贴", desc = "接单额外小费提升 +20%（与免费换乘互斥）", cost = 14, parentKey = "neon_disp_1", exclusiveKey = "neon_disp_2", treeX = 0.59f, treeY = 0.53f },
        new SandboxTreeNodeSpec { key = "neon_disp_3", eventId = EventIdNeonNight, branch = 1, tier = 3, branchName = "跨区调度", title = "夜燕小巴", desc = "获赠「夜燕」双座小巴并反哺主线涂装", cost = 22, unlockVehicleKey = "veh_neon_owl", treeX = 0.50f, treeY = 0.35f },
        new SandboxTreeNodeSpec { key = "neon_disp_4", eventId = EventIdNeonNight, branch = 1, tier = 4, branchName = "跨区调度", title = "大桥直通", desc = "跨区长单车资提升 +20%", cost = 26, parentKey = "neon_disp_3", treeX = 0.50f, treeY = 0.16f },

        new SandboxTreeNodeSpec { key = "neon_rep_1", eventId = EventIdNeonNight, branch = 2, tier = 1, branchName = "口碑礼宾", title = "热茶礼宾", desc = "订单小费 +15%，乘客耐心 +20%", cost = 6, treeX = 0.83f, treeY = 0.71f },
        new SandboxTreeNodeSpec { key = "neon_rep_2", eventId = EventIdNeonNight, branch = 2, tier = 2, branchName = "口碑礼宾", title = "车载香氛", desc = "超时口碑下降减半（与夜场合作互斥）", cost = 14, parentKey = "neon_rep_1", exclusiveKey = "neon_rep_2b", treeX = 0.75f, treeY = 0.53f },
        new SandboxTreeNodeSpec { key = "neon_rep_2b", eventId = EventIdNeonNight, branch = 2, tier = 2, branchName = "口碑礼宾", title = "夜场合作", desc = "订单基础票价权重 +25%（与香氛互斥）", cost = 14, parentKey = "neon_rep_1", exclusiveKey = "neon_rep_2", treeX = 0.91f, treeY = 0.53f },
        new SandboxTreeNodeSpec { key = "neon_rep_3", eventId = EventIdNeonNight, branch = 2, tier = 3, branchName = "口碑礼宾", title = "紫星礼宾", desc = "获赠「紫星」礼宾专车并反哺主线涂装", cost = 22, unlockVehicleKey = "veh_neon_amethyst", treeX = 0.83f, treeY = 0.35f },
        new SandboxTreeNodeSpec { key = "neon_rep_4", eventId = EventIdNeonNight, branch = 2, tier = 4, branchName = "口碑礼宾", title = "金字招牌", desc = "乘客耐心再 +25%", cost = 26, parentKey = "neon_rep_3", treeX = 0.83f, treeY = 0.16f },

        new SandboxTreeNodeSpec { key = "neon_uni_a", eventId = EventIdNeonNight, branch = 3, tier = 4, branchName = "夜航统筹", title = "夜间电台", desc = "里程券获取效率微增", cost = 18, parentKey = "neon_night_1", treeX = 0.33f, treeY = 0.06f },
        new SandboxTreeNodeSpec { key = "neon_uni_b", eventId = EventIdNeonNight, branch = 3, tier = 4, branchName = "夜航统筹", title = "深夜扩网", desc = "不夜港线路上限再 +2 条", cost = 20, parentKey = "neon_night_2", treeX = 0.67f, treeY = 0.06f }
    };

    string sandboxSelectedTreeNodeKey = "";
    float sandboxLastTreeTapTime;
    string sandboxJustUnlockedTreeNodeKey, sandboxJustUnlockedTreeEventId;

    bool HasSandboxTreeNode(string eventId, string nodeKey) => GetOrCreateEventSandbox(eventId).treeNodes.Contains(nodeKey);

    bool IsSandboxBranchLocked(SavedEventSandbox st, SandboxTreeNodeSpec node)
    {
        if (node.branch >= 3 || node.tier < 2) return false;
        return st.activeBranch >= 0 && st.activeBranch != node.branch;
    }

    bool IsSandboxExclusiveLocked(SavedEventSandbox st, SandboxTreeNodeSpec node)
        => !string.IsNullOrEmpty(node.exclusiveKey) && st.treeNodes.Contains(node.exclusiveKey);

    bool HasSandboxTier2InBranch(SavedEventSandbox st, string eventId, int branch)
    {
        for (int i = 0; i < SandboxTreeNodes.Length; i++)
        {
            var n = SandboxTreeNodes[i];
            if (n.eventId == eventId && n.branch == branch && n.tier == 2 && st.treeNodes.Contains(n.key)) return true;
        }
        return false;
    }

    bool CanResearchSandboxTreeNode(string eventId, SandboxTreeNodeSpec node)
    {
        if (node == null || node.eventId != eventId) return false;
        var st = GetOrCreateEventSandbox(eventId);
        if (st.treeNodes.Contains(node.key)) return false;
        if (IsSandboxBranchLocked(st, node) || IsSandboxExclusiveLocked(st, node)) return false;
        if (node.tier == 3 && node.branch < 3)
        {
            if (!HasSandboxTier2InBranch(st, eventId, node.branch)) return false;
        }
        else if (!string.IsNullOrEmpty(node.parentKey) && !st.treeNodes.Contains(node.parentKey))
            return false;
        return st.tokens >= node.cost;
    }

    bool HasAffordableSandboxTreeNode(string eventId)
    {
        for (int i = 0; i < SandboxTreeNodes.Length; i++)
            if (CanResearchSandboxTreeNode(eventId, SandboxTreeNodes[i])) return true;
        return false;
    }

    void ResearchSandboxTreeNode(string eventId, string nodeKey)
    {
        SandboxTreeNodeSpec target = null;
        for (int i = 0; i < SandboxTreeNodes.Length; i++)
            if (SandboxTreeNodes[i].key == nodeKey) { target = SandboxTreeNodes[i]; break; }
        if (target == null) return;
        var st = GetOrCreateEventSandbox(eventId);
        if (st.treeNodes.Contains(nodeKey)) return;
        if (!CanResearchSandboxTreeNode(eventId, target)) { PlayErrorSfx(); Toast("前置条件未满足或代币不足"); return; }

        st.tokens -= target.cost;
        st.treeNodes.Add(nodeKey);
        sandboxJustUnlockedTreeNodeKey = nodeKey;
        sandboxJustUnlockedTreeEventId = eventId;
        if (target.branch < 3 && target.tier >= 2 && st.activeBranch < 0)
            st.activeBranch = target.branch;
        PlayTechSfx();
        if (!string.IsNullOrEmpty(target.unlockVehicleKey))
            UnlockSandboxVehicleAndSkin(eventId, target.unlockVehicleKey);
        else
            Toast($"已研发「{target.title}」");
        RefreshEventBadge();
        SaveSandboxStorage();
    }

    int GetSandboxLineLimit(string eventId)
    {
        var spec = GetEventSpec(eventId);
        int cap = spec != null ? spec.lineLimit : 8;
        if (eventId == EventIdAshDome)
        {
            if (HasSandboxTreeNode(EventIdAshDome, "ash_cap_2b")) cap += 3;
            if (HasSandboxTreeNode(EventIdAshDome, "ash_uni_b")) cap += 2;
        }
        else if (eventId == EventIdNeonNight)
        {
            if (HasSandboxTreeNode(EventIdNeonNight, "neon_fleet_2b")) cap += 3;
            if (HasSandboxTreeNode(EventIdNeonNight, "neon_uni_b")) cap += 2;
        }
        return cap;
    }

    float GetAshCapMult()
    {
        float m = HasSandboxTreeNode(EventIdAshDome, "ash_cap_2") ? 1.30f : HasSandboxTreeNode(EventIdAshDome, "ash_cap_1") ? 1.15f : 1f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_cap_4")) m *= 1.20f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_sci_3")) m *= 1.15f;
        return m;
    }

    float GetAshRefugeCapMult() => (HasSandboxTreeNode(EventIdAshDome, "ash_res_2") ? 1.30f : 1f) * (HasSandboxTreeNode(EventIdAshDome, "ash_res_4") ? 1.20f : 1f);
    float GetAshOverflowScale()
    {
        float s = HasSandboxTreeNode(EventIdAshDome, "ash_res_1") ? 0.70f : 1f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_res_2b")) s *= 1.20f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_res_4")) s *= 1.15f;
        return s;
    }
    float GetAshExtraImmuneDays() => (HasSandboxTreeNode(EventIdAshDome, "ash_def_2") ? 1.5f : HasSandboxTreeNode(EventIdAshDome, "ash_def_1") ? 0.5f : 0f) + (HasSandboxTreeNode(EventIdAshDome, "ash_def_4") ? 1.0f : 0f);

    int GetNeonLicenseLimit()
    {
        int cap = 4;
        if (HasSandboxTreeNode(EventIdNeonNight, "neon_fleet_1")) cap += 2;
        if (HasSandboxTreeNode(EventIdNeonNight, "neon_fleet_2")) cap += 3;
        return cap;
    }

    bool HasNeonNeighborPickup() => HasSandboxTreeNode(EventIdNeonNight, "neon_disp_1");
    bool HasNeonFreeTransfer() => HasSandboxTreeNode(EventIdNeonNight, "neon_disp_2");
    float GetNeonTipBonus() => (HasSandboxTreeNode(EventIdNeonNight, "neon_rep_1") ? 0.15f : 0f) + (HasSandboxTreeNode(EventIdNeonNight, "neon_disp_2b") ? 0.20f : 0f) + (HasSandboxTreeNode(EventIdNeonNight, "neon_fleet_4") ? 0.15f : 0f);
    float GetNeonPatienceMult() => (HasSandboxTreeNode(EventIdNeonNight, "neon_rep_1") ? 1.20f : 1f) * (HasSandboxTreeNode(EventIdNeonNight, "neon_rep_4") ? 1.25f : 1f);
    float GetNeonRepLossMult() => HasSandboxTreeNode(EventIdNeonNight, "neon_rep_2") ? 0.50f : 1f;
    float GetNeonEntertainmentWeightMult() => (HasSandboxTreeNode(EventIdNeonNight, "neon_rep_2b") ? 1.25f : 1f) * (HasSandboxTreeNode(EventIdNeonNight, "neon_night_2") ? 1.10f : 1f) * (HasSandboxTreeNode(EventIdNeonNight, "neon_disp_4") ? 1.20f : 1f);

    // ---------- 1. 可缩放/拖拽树状科技树（主线同款单击预览 + 双击解锁 + 互斥置灰） ----------
    float sandboxTreeSavedScale = -1f;
    Vector2 sandboxTreeSavedPos;
    string sandboxTreeSavedEventId = "";
    Action sandboxTreeSaveView;

    void OpenSandboxTreeScreen(string eventId)
    {
        if (AshCycleCompleted()) { Toast("本轮已结束，请前往指挥部开启新轮"); return; }
        var spec = GetEventSpec(eventId);
        if (spec == null) return;
        var st = GetOrCreateEventSandbox(eventId);
        string treeTitle = eventId == EventIdAshDome ? "方舟专精树" : "夜班专精树";
        string specName = st.activeBranch < 0 ? "未定（2阶任选一派锁定主修）"
            : eventId == EventIdAshDome
                ? (st.activeBranch == 0 ? "重装运力" : st.activeBranch == 1 ? "固守延缓" : "医疗速援")
                : (st.activeBranch == 0 ? "车队扩张" : st.activeBranch == 1 ? "跨区调度" : "口碑礼宾");
        var pg = OpenPage($"{spec.displayTitle}  {treeTitle}", "滚轮缩放与拖拽画布；单击节点预览详情，双击节点立项解锁。", "PageArt/album_watercolor", "evt_tree_" + eventId);

        GetSafeMarginsInRoot(out float safeL, out float safeR, out float safeT, out float safeB);
        float safeW = Mathf.Max(320f, RootW - safeL - safeR);
        float safeH = Mathf.Max(1200f, RootH - safeT - safeB);
        float padX = safeL + (safeW - W) * 0.5f;

        // 顶部状态条（在 safeT 之下）
        float topY = RootH - safeT - 246f;
        var topInfo = Rect("TreeTopInfo", pg.transform, padX + 28f, topY, W - 56f, 68f);
        Rounded(topInfo, CardBg); SoftShadow(topInfo);
        Txt(topInfo.transform, $"第 {Mathf.Max(1, st.cycleCount)} 轮   主修：{specName}", 21, Ink, 22f, 0f, 640f, 68f, TextAnchor.MiddleLeft);
        Txt(topInfo.transform, $"可用{spec.tokenName}：{st.tokens}", 23, Indigo, W - 310f, 0f, 220f, 68f, TextAnchor.MiddleRight);

        // 底部预览条（单击预览详情，再点直接解锁）
        float previewH = 128f;
        float previewY = safeB + 20f;
        var previewBar = Rect("TreePreviewBar", pg.transform, padX + 28f, previewY, W - 56f, previewH);
        Rounded(previewBar, CardBg); SoftShadow(previewBar);
        var prevTitle = Txt(previewBar.transform, "单击节点预览详情  双击节点直接解锁（支持滚轮缩放与拖拽画布）", 23, Ink, 24f, 74f, W - 300f, 40f, TextAnchor.MiddleLeft);
        var prevDesc = Txt(previewBar.transform, "选中 2 阶分叉后将锁定本轮主修路线，互斥分支自动置灰", 20, InkSoft, 24f, 16f, W - 300f, 52f, TextAnchor.UpperLeft);
        prevDesc.horizontalOverflow = HorizontalWrapMode.Wrap;
        Text prevActionTxt = null;
        Button prevActionBtn = Btn(previewBar.transform, "UnlockSelected", "等待选点", 21, W - 260f, 28f, 180f, 70f, Paper2, () => {
            if (string.IsNullOrEmpty(sandboxSelectedTreeNodeKey)) return;
            ResearchSandboxTreeNode(eventId, sandboxSelectedTreeNodeKey);
            sandboxTreeSaveView?.Invoke();
            OpenSandboxTreeScreen(eventId);
        });
        TInk(prevActionBtn, InkSoft);
        prevActionTxt = prevActionBtn.GetComponentInChildren<Text>();

        // 中部可缩放可拖拽树状画布（复用主线 MapWheelZoom）
        float vpY = previewY + previewH + 14f;
        float vpH = Mathf.Max(600f, topY - 14f - vpY);
        float vpW = W - 56f;
        var vpGo = Rect("TreeViewport", pg.transform, padX + 28f, vpY, vpW, vpH);
        var vpImg = Img(vpGo, new Color(0.975f, 0.962f, 0.930f, 0.96f), spriteRound);
        vpImg.type = Image.Type.Sliced; vpImg.raycastTarget = true;
        vpGo.AddComponent<Mask>().showMaskGraphic = true;
        SoftShadow(vpGo);

        const float cw = 2200f, ch = 1320f;
        var canvasGo = Rect("TreeCanvas", vpGo.transform, 0, 0, cw, ch);
        var canvasRt = canvasGo.GetComponent<RectTransform>();
        canvasRt.anchorMin = Vector2.zero; canvasRt.anchorMax = Vector2.zero; canvasRt.pivot = Vector2.zero;
        Img(canvasGo, new Color(1, 1, 1, 0.001f)).raycastTarget = true;

        var scroll = vpGo.AddComponent<ScrollRect>(); scroll.viewport = vpGo.GetComponent<RectTransform>(); scroll.content = canvasRt; scroll.movementType = ScrollRect.MovementType.Unrestricted;
        var zoom = vpGo.AddComponent<HistoryGraphZoom>(); zoom.viewport = scroll.viewport; zoom.content = canvasRt; zoom.scroll = scroll;
        float initScale = Mathf.Clamp(vpW / cw, 0.45f, 1.0f);
        Action resetView = () => { zoom.scale = initScale; canvasRt.localScale = Vector3.one * initScale; canvasRt.anchoredPosition = new Vector2((vpW - cw * initScale) * 0.5f, (vpH - ch * initScale) * 0.5f); };
        Action<float> setZoom = factor => { zoom.scale = Mathf.Clamp(zoom.scale * factor, 0.45f, 2f); canvasRt.localScale = Vector3.one * zoom.scale; };
        if (sandboxTreeSavedEventId == eventId && sandboxTreeSavedScale > 0f) { zoom.scale = sandboxTreeSavedScale; canvasRt.localScale = Vector3.one * zoom.scale; canvasRt.anchoredPosition = sandboxTreeSavedPos; }
        else resetView();
        sandboxTreeSaveView = () => { sandboxTreeSavedEventId = eventId; sandboxTreeSavedScale = zoom.scale; sandboxTreeSavedPos = canvasRt.anchoredPosition; };
        sandboxTreeSaveView();
        TInk(Btn(pg.transform, "ZoomIn", "+", 22, padX + W - 220, vpY + vpH - 60, 50, 48, Paper2, () => setZoom(1.2f)), Ink);
        TInk(Btn(pg.transform, "ZoomOut", "−", 22, padX + W - 165, vpY + vpH - 60, 50, 48, Paper2, () => setZoom(1f / 1.2f)), Ink);
        TInk(Btn(pg.transform, "ResetView", "复位", 18, padX + W - 110, vpY + vpH - 60, 72, 48, Paper2, () => { resetView(); sandboxTreeSaveView(); }), Ink);

        for (int gx = 0; gx < cw; gx += 100) { var grid = Img(Rect("GridV", canvasGo.transform, gx, 0, 1, ch), new Color(0.4f, 0.35f, 0.3f, 0.08f)); grid.raycastTarget = false; }
        for (int gy = 0; gy < ch; gy += 100) { var grid = Img(Rect("GridH", canvasGo.transform, 0, gy, cw, 1), new Color(0.4f, 0.35f, 0.3f, 0.08f)); grid.raycastTarget = false; }
        string[] headers = eventId == EventIdAshDome ? new[] { "通用基础", "重装运力", "固守延缓", "医疗速援" } : new[] { "通用基础", "车队扩张", "跨区调度", "口碑礼宾" };
        for (int h = 0; h < headers.Length; h++) { var banner = Rect("BranchHeader", canvasGo.transform, 175 + h * 550, 1190, 200, 50); Rounded(banner, Indigo); Txt(banner.transform, headers[h], 21, Color.white, 0, 0, 200, 50, TextAnchor.MiddleCenter).raycastTarget = false; }
        var lineLayer = Rect("TreeLines", canvasGo.transform, 0, 0, cw, ch);
        var nodes = new List<SandboxTreeNodeSpec>();
        var posByKey = new Dictionary<string, Vector2>();
        for (int i = 0; i < SandboxTreeNodes.Length; i++)
        {
            var n = SandboxTreeNodes[i];
            if (n.eventId != eventId) continue;
            nodes.Add(n);
            int col = n.branch == 3 ? 0 : n.branch + 1;
            float center = 275f + col * 550f;
            bool pair = n.tier == 2 && n.branch < 3 || n.tier == 4 && n.branch == 3;
            float offset = pair ? (n.key.EndsWith("b") ? 116f : -116f) : 0f;
            posByKey[n.key] = new Vector2(center + offset, 1070f - (n.tier - 1) * 270f);
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            Vector2 toPos = posByKey[n.key];
            bool nLocked = IsSandboxBranchLocked(st, n) || IsSandboxExclusiveLocked(st, n);
            Color edgeCol = st.treeNodes.Contains(n.key) ? Matcha : nLocked ? new Color(0.65f, 0.63f, 0.60f, 0.28f) : new Color(InkSoft.r, InkSoft.g, InkSoft.b, 0.42f);
            if (!string.IsNullOrEmpty(n.parentKey) && posByKey.TryGetValue(n.parentKey, out var fromPos))
                DrawTreeEdge(lineLayer.transform, fromPos + Vector2.down * 54f, toPos + Vector2.up * 54f, edgeCol);
            if (n.tier == 3 && n.branch < 3)
            {
                for (int j = 0; j < nodes.Count; j++)
                    if (nodes[j].branch == n.branch && nodes[j].tier == 2)
                        DrawTreeEdge(lineLayer.transform, posByKey[nodes[j].key] + Vector2.down * 54f, toPos + Vector2.up * 54f, edgeCol);
            }
        }

        foreach (var n in nodes)
            if (!string.IsNullOrEmpty(n.exclusiveKey) && string.CompareOrdinal(n.key, n.exclusiveKey) < 0 && posByKey.ContainsKey(n.exclusiveKey))
            { var mid = (posByKey[n.key] + posByKey[n.exclusiveKey]) * 0.5f; var mark = Rect("Exclusive", canvasGo.transform, mid.x - 50, mid.y - 90, 100, 32); Rounded(mark, Vermilion); Txt(mark.transform, "⇄ 互斥", 16, Color.white, 0, 0, 100, 32, TextAnchor.MiddleCenter).raycastTarget = false; }
        for (int branch = 0; branch < 2; branch++)
        {
            var marker = Rect("BranchExclusive", canvasGo.transform, 775 + branch * 550, 755, 100, 32);
            Rounded(marker, Vermilion); Txt(marker.transform, "⇄ 互斥", 16, Color.white, 0, 0, 100, 32, TextAnchor.MiddleCenter).raycastTarget = false;
        }
        string revealKey = sandboxJustUnlockedTreeEventId == eventId ? sandboxJustUnlockedTreeNodeKey : null;
        if (revealKey != null) { sandboxJustUnlockedTreeNodeKey = null; sandboxJustUnlockedTreeEventId = null; }
        const float nodeW = 204f, nodeH = 108f;
        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            Vector2 p = posByKey[node.key];
            bool done = st.treeNodes.Contains(node.key);
            bool branchLocked = IsSandboxBranchLocked(st, node);
            bool exLocked = IsSandboxExclusiveLocked(st, node);
            bool locked = branchLocked || exLocked;
            bool ready = CanResearchSandboxTreeNode(eventId, node);
            bool selected = sandboxSelectedTreeNodeKey == node.key;

            var card = Rect("Node_" + node.key, canvasGo.transform, p.x - nodeW * 0.5f, p.y - nodeH * 0.5f, nodeW, nodeH);
            Color bg = done ? Color.Lerp(CardBg, Matcha, 0.22f)
                : locked ? new Color(0.80f, 0.78f, 0.75f, 0.78f)
                : selected ? Color.Lerp(CardBg, Gold, 0.25f)
                : ready ? Color.Lerp(CardBg, Indigo, 0.14f) : CardBg;
            Rounded(card, bg); SoftShadow(card);
            if (ready) card.AddComponent<HistoryNodePulse>();
            if (done && node.key == revealKey) RevealHistoryCard(card, false);

            Color tagCol = locked ? new Color(0.55f, 0.54f, 0.52f)
                : node.branch == 3 ? new Color(0.32f, 0.48f, 0.56f)
                : node.branch == 0 ? Indigo : node.branch == 1 ? spec.accentColor : Matcha;
            var bar = Rect("Bar", card.transform, 10f, nodeH - 28f, nodeW - 20f, 20f);
            Rounded(bar, tagCol);
            Txt(bar.transform, $"{node.branchName} {node.tier}阶", 14, Color.white, 0, 0, nodeW - 20f, 20f, TextAnchor.MiddleCenter).raycastTarget = false;

            FitSingleLine(Txt(card.transform, node.title, 21, locked ? new Color(0.42f, 0.41f, 0.39f) : Ink, 8f, 34f, nodeW - 16f, 36f, TextAnchor.MiddleCenter), 21);
            string stLbl = done ? "已解锁" : branchLocked ? "他派已互斥" : exLocked ? "同阶已互斥" : $"{node.cost} {spec.tokenName}";
            Txt(card.transform, stLbl, 17, done ? Matcha : locked ? new Color(0.48f, 0.46f, 0.44f) : (ready ? Indigo : InkSoft), 8f, 6f, nodeW - 16f, 24f, TextAnchor.MiddleCenter).raycastTarget = false;

            var btn = card.AddComponent<Button>(); btn.targetGraphic = card.GetComponent<Image>();
            var capNode = node;
            var touch = card.AddComponent<HistoryNodeTouch>();
            touch.onHold = () => btn.onClick.Invoke();
            btn.onClick.AddListener(() => {
                float now = Time.unscaledTime;
                if (sandboxSelectedTreeNodeKey == capNode.key && now - sandboxLastTreeTapTime <= 0.48f)
                {
                    // 双击同一节点直接解锁
                    ResearchSandboxTreeNode(eventId, capNode.key);
                    sandboxTreeSaveView?.Invoke();
                    OpenSandboxTreeScreen(eventId);
                    return;
                }
                sandboxSelectedTreeNodeKey = capNode.key;
                sandboxLastTreeTapTime = now;
                bool nDone = st.treeNodes.Contains(capNode.key);
                bool nLocked = IsSandboxBranchLocked(st, capNode) || IsSandboxExclusiveLocked(st, capNode);
                bool nReady = CanResearchSandboxTreeNode(eventId, capNode);
                prevTitle.text = $"{capNode.branchName}  {capNode.title}（再点一次或双击解锁）";
                prevDesc.text = capNode.desc + (nLocked ? "  [已与当前所选分支互斥置灰]" : "");
                prevActionBtn.GetComponent<Image>().color = nDone ? Matcha : nLocked ? Paper2 : nReady ? Indigo : Paper2;
                if (prevActionTxt)
                {
                    prevActionTxt.text = nDone ? "已解锁" : nLocked ? "已互斥" : nReady ? $"解锁 ({capNode.cost})" : $"缺前置/代币";
                    prevActionTxt.color = (nDone || nReady) ? Color.white : InkSoft;
                }
            });
        }
    }

    void DrawTreeEdge(Transform parent, Vector2 a, Vector2 b, Color col)
    {
        var go = Rect("TreeEdge", parent, 0, 0, 100f, 100f);
        var poly = go.AddComponent<LinePolylineGraphic>();
        poly.raycastTarget = false;
        Vector2 mid = new Vector2(b.x, (a.y + b.y) * 0.5f);
        var pts = new List<Vector2> { a, new Vector2(a.x, mid.y), mid, b };
        poly.SetPolyline(pts, Vector2.zero, col, new Color(1f, 1f, 1f, 0.25f), 5.5f, 1.5f);
    }

    // ---------- 2. 线路调度系统（沙盒「航线」页） ----------
    void OpenSandboxLinesScreen(string eventId)
    {
        if (AshCycleCompleted()) { Toast("本轮已结束，请前往指挥部开启新轮"); return; }
        var spec = GetEventSpec(eventId);
        if (spec == null) return;
        var st = GetOrCreateEventSandbox(eventId);
        var defs = GetSandboxStationDefs(eventId);
        int cap = GetSandboxLineLimit(eventId);
        var pg = OpenPage($"{spec.displayTitle}  {(eventId == EventIdAshDome ? "撤离专线" : "线路调度")}", $"已通车 {st.lines.Count} / {cap} 条街道线路  可增派车辆、换装车型或撤回线路。", "PageArt/line_watercolor", "evt_lines_" + eventId);

        const float rowH = 186f, gap = 18f, padTop = 22f;
        var content = FleetScroll(pg.transform, 32f, PgH - 188f, padTop + Mathf.Max(1, st.lines.Count) * (rowH + gap) + 120f, scrollKey: "evt_lines_" + eventId);
        float topY = content.sizeDelta.y, w = content.sizeDelta.x - 8f;

        if (st.lines.Count == 0)
        {
            Txt(content.transform, "当前尚未开通任何街道线路\n返回城市地图按住任一站台拖向下一座站台，即可沿街连线。", 25, InkSoft, 40f, topY - 260f, w - 80f, 160f, TextAnchor.MiddleCenter);
            return;
        }

        for (int i = 0; i < st.lines.Count; i++)
        {
            var line = st.lines[i];
            float ry = topY - padTop - (i + 1) * rowH - i * gap;
            var card = Rect("SLine_" + line.id, content, 4f, ry, w, rowH);
            Rounded(card, CardBg); SoftShadow(card);

            var badge = Rect("Badge", card.transform, 22f, 124f, 92f, 40f);
            Rounded(badge, line.color);
            Txt(badge.transform, line.name, 20, Color.white, 0, 0, 92f, 40f, TextAnchor.MiddleCenter);

            string head = (line.stations != null && line.stations.Count >= 2)
                ? $"{defs[line.stations[0]].name} 至 {defs[line.stations[line.stations.Count - 1]].name}  （{line.stations.Count} 站）"
                : line.name;
            FitSingleLine(Txt(card.transform, head, 25, Ink, 128f, 124f, w - 150f, 40f, TextAnchor.MiddleLeft), 25);

            var vDef = GetSandboxVehicleDef(eventId, line.vehicleKey);
            Txt(card.transform, $"配属车型：{vDef?.name ?? "基础车型"}   当前编组：{Mathf.Max(1, line.cars)} 辆", 21, InkSoft, 22f, 76f, w - 44f, 34f, TextAnchor.MiddleLeft);

            int li = i;
            float bw = (w - 64f) / 3f;
            TInk(Btn(card.transform, "AddCar", $"+1 辆车 (4 {spec.tokenName})", 20, 22f, 16f, bw, 48f, Matcha, () => {
                if (st.tokens < 4) { PlayErrorSfx(); Toast($"{spec.tokenName}不足 4"); return; }
                if (st.lines[li].cars >= 4) { PlayErrorSfx(); Toast("单线编组上限为 4 辆"); return; }
                st.tokens -= 4; st.lines[li].cars++;
                PlayBuildSfx(); SaveSandboxStorage(); RebuildSandboxLines(); OpenSandboxLinesScreen(eventId);
            }), Color.white);

            TInk(Btn(card.transform, "SwapVeh", "更换配车", 20, 22f + bw + 10f, 16f, bw, 48f, Indigo, () => {
                OpenSandboxLineVehiclePicker(eventId, li);
            }), Color.white);

            TInk(Btn(card.transform, "DelLine", "撤除线路", 20, 22f + (bw + 10f) * 2f, 16f, bw, 48f, Vermilion, () => {
                st.lines.RemoveAt(li);
                PlayTone(false); SaveSandboxStorage(); RebuildSandboxLines(); OpenSandboxLinesScreen(eventId);
                Toast("已撤除该街道线路");
            }), Color.white);
        }
    }

    void OpenSandboxLineVehiclePicker(string eventId, int lineIdx)
    {
        var st = GetOrCreateEventSandbox(eventId);
        if (lineIdx < 0 || lineIdx >= st.lines.Count) return;
        var p = OpenPopup($"为 {st.lines[lineIdx].name} 配置车型", 160f + st.ownedVehicles.Count * 96f, 740f);
        if (!p) return;
        for (int i = 0; i < st.ownedVehicles.Count; i++)
        {
            string vk = st.ownedVehicles[i];
            var v = GetSandboxVehicleDef(eventId, vk);
            if (v == null) continue;
            bool cur = st.lines[lineIdx].vehicleKey == vk || (string.IsNullOrEmpty(st.lines[lineIdx].vehicleKey) && i == 0);
            TInk(Btn(p.transform, "Veh_" + vk, cur ? $"{v.name}（执勤中）" : $"指派：{v.name}", 23, 36f, p.GetComponent<RectTransform>().sizeDelta.y - 140f - i * 92f, 668f, 76f,
                cur ? Matcha : Indigo, () => {
                    st.lines[lineIdx].vehicleKey = vk;
                    ClosePanel(true); SaveSandboxStorage(); RebuildSandboxLines(); OpenSandboxLinesScreen(eventId);
                    Toast($"{st.lines[lineIdx].name} 已换装 {v.name}");
                }), Color.white);
        }
    }

    // ---------- 3. 车辆购买与涂装工坊（修复页签层级遮挡） ----------
    int sandboxFleetTab;
    void OpenSandboxFleetAndSkinScreen(string eventId)
    {
        if (AshCycleCompleted()) { Toast("本轮已结束，请前往指挥部开启新轮"); return; }
        var spec = GetEventSpec(eventId);
        if (spec == null) return;
        var st = GetOrCreateEventSandbox(eventId);
        var pg = OpenPage($"{spec.displayTitle}  {(eventId == EventIdAshDome ? "救援车与防沙改装" : "车辆与涂装工坊")}", $"可用{spec.tokenName}：{st.tokens}  新车可以派上线，涂装也能带回主线。", "FleetArt/garage_watercolor", "evt_fleet_" + eventId);

        if (sandboxFleetTab == 0)
        {
            const float rowH = 178f, gap = 18f, padTop = 20f;
            var content = FleetScroll(pg.transform, 32f, PgH - 270f, padTop + spec.vehicles.Length * (rowH + gap) + 80f, scrollKey: "evt_fleet_v_" + eventId);
            float topY = content.sizeDelta.y, w = content.sizeDelta.x - 8f;
            for (int i = 0; i < spec.vehicles.Length; i++)
            {
                var v = spec.vehicles[i];
                float ry = topY - padTop - (i + 1) * rowH - i * gap;
                bool owned = st.ownedVehicles.Contains(v.key);
                var card = Rect("V_" + v.key, content, 4f, ry, w, rowH);
                Rounded(card, owned ? Color.Lerp(CardBg, Matcha, 0.12f) : CardBg); SoftShadow(card);
                AddSandboxArtCard(card.transform, "VehicleArt", "FleetArt/" + v.key, 18f, 14f, 200f, 150f, v.name);
                Txt(card.transform, v.name, 27, Ink, 236f, 114f, w - 470f, 40f, TextAnchor.MiddleLeft);
                var dt = Txt(card.transform, v.desc, 20, InkSoft, 236f, 54f, w - 470f, 54f, TextAnchor.UpperLeft);
                dt.horizontalOverflow = HorizontalWrapMode.Wrap;
                Txt(card.transform, owned ? "已入库，可在「航线」派车" : $"购置费：{v.costTokens} {spec.tokenName}", 20, owned ? Matcha : Indigo, 236f, 14f, w - 470f, 30f, TextAnchor.MiddleLeft);

                if (owned) TInk(Btn(card.transform, "Owned", "已入库", 22, w - 210f, 52f, 180f, 68f, Matcha, null), Color.white);
                else
                {
                    string vk = v.key; int cost = v.costTokens;
                    bool ok = st.tokens >= cost;
                    TInk(Btn(card.transform, "BuyV", ok ? $"购置 ({cost})" : "代币不足", 22, w - 210f, 52f, 180f, 68f, ok ? Indigo : Paper2, () => {
                        if (st.tokens < cost) { PlayErrorSfx(); Toast($"{spec.tokenName}不足"); return; }
                        st.tokens -= cost;
                        UnlockSandboxVehicleAndSkin(eventId, vk);
                        OpenSandboxFleetAndSkinScreen(eventId);
                    }), ok ? Color.white : InkSoft);
                }
            }
        }
        else
        {
            int[] shopSkins = { 2, 5, 7, 8, 10, 12, 13, 14 };
            const float rowH = 144f, gap = 16f, padTop = 20f;
            var content = FleetScroll(pg.transform, 32f, PgH - 270f, padTop + shopSkins.Length * (rowH + gap) + 80f, scrollKey: "evt_fleet_s_" + eventId);
            float topY = content.sizeDelta.y, w = content.sizeDelta.x - 8f;
            for (int i = 0; i < shopSkins.Length; i++)
            {
                int sIdx = shopSkins[i];
                if (sIdx < 0 || sIdx >= LineSkinNames.Length) continue;
                float ry = topY - padTop - (i + 1) * rowH - i * gap;
                bool owned = ownedLineSkins.Contains(sIdx) || st.ownedSkins.Contains(sIdx);
                var card = Rect("Skin_" + sIdx, content, 4f, ry, w, rowH);
                Rounded(card, owned ? Color.Lerp(CardBg, Gold, 0.12f) : CardBg); SoftShadow(card);
                var swatch = Rect("Swatch", card.transform, 24f, 44f, 56f, 56f);
                Rounded(swatch, LineColors[sIdx % LineColors.Length]);
                Txt(card.transform, $"涂装「{LineSkinNames[sIdx]}」", 26, Ink, 98f, 82f, w - 330f, 40f, TextAnchor.MiddleLeft);
                Txt(card.transform, owned ? "已收进主线公司的涂装库" : $"兑换后也可在主线使用（12 {spec.tokenName}）", 20, InkSoft, 98f, 28f, w - 330f, 40f, TextAnchor.MiddleLeft);

                if (owned) TInk(Btn(card.transform, "Apply", "涂装全线", 21, w - 210f, 38f, 180f, 66f, Matcha, () => {
                    Color sc = LineColors[sIdx % LineColors.Length];
                    for (int li = 0; li < st.lines.Count; li++) st.lines[li].color = sc;
                    SaveSandboxStorage(); RebuildSandboxLines(); Toast($"已为沙盒全线换装「{LineSkinNames[sIdx]}」");
                }), Color.white);
                else
                {
                    bool ok = st.tokens >= 12;
                    TInk(Btn(card.transform, "BuyS", ok ? "兑换 (12)" : "代币不足", 21, w - 210f, 38f, 180f, 66f, ok ? Indigo : Paper2, () => {
                        if (st.tokens < 12) { PlayErrorSfx(); Toast($"{spec.tokenName}不足 12"); return; }
                        st.tokens -= 12;
                        if (!st.ownedSkins.Contains(sIdx)) st.ownedSkins.Add(sIdx);
                        if (!ownedLineSkins.Contains(sIdx)) ownedLineSkins.Add(sIdx);
                        PlayCoinSfx(); SaveSandboxStorage(); RequestSaveGame(0.5f);
                        Toast($"兑换成功！主线已同步解锁涂装「{LineSkinNames[sIdx]}」");
                        OpenSandboxFleetAndSkinScreen(eventId);
                    }), ok ? Color.white : InkSoft);
                }
            }
        }

        // 在 FleetScroll 之后创建页签并置于最前，确保任意时刻均可丝滑来回切换
        GetSafeMarginsInRoot(out float safeL, out float safeR, out float safeT, out _);
        float safeW = Mathf.Max(320f, RootW - safeL - safeR);
        float padX = safeL + (safeW - W) * 0.5f;
        float tabY = RootH - safeT - 246f;
        var b0 = Btn(pg.transform, "TabVeh", "特种车库", 23, padX + 36f, tabY, 490f, 64f, sandboxFleetTab == 0 ? Indigo : Paper2, () => { sandboxFleetTab = 0; OpenSandboxFleetAndSkinScreen(eventId); });
        TInk(b0, sandboxFleetTab == 0 ? Color.white : Ink);
        b0.transform.SetAsLastSibling();
        var b1 = Btn(pg.transform, "TabSkin", eventId == EventIdAshDome ? "防沙改装" : "涂装商店", 23, padX + 554f, tabY, 490f, 64f, sandboxFleetTab == 1 ? Indigo : Paper2, () => { sandboxFleetTab = 1; OpenSandboxFleetAndSkinScreen(eventId); });
        TInk(b1, sandboxFleetTab == 1 ? Color.white : Ink);
        b1.transform.SetAsLastSibling();
    }

    // ---------- 4. 指挥部（公司 like） ----------
    void OpenSandboxCommandScreen(string eventId)
    {
        var spec = GetEventSpec(eventId);
        if (spec == null) return;
        var st = GetOrCreateEventSandbox(eventId);
        var pg = OpenPage($"{spec.displayTitle}  调度指挥部", $"可用{spec.tokenName}：{st.tokens}  在这里调整执勤方针或重开一轮。", "PageArt/company_watercolor", "evt_cmd_" + eventId);

        const float totalH = 1280f;
        var content = FleetScroll(pg.transform, 32f, PgH - 188f, totalH, scrollKey: "evt_cmd_" + eventId);
        float topY = content.sizeDelta.y, w = content.sizeDelta.x - 8f;

        var staffCard = Rect("StaffCard", content, 4f, topY - 260f, w, 236f);
        Rounded(staffCard, CardBg); SoftShadow(staffCard);
        string chief = eventId == EventIdAshDome ? "值班主任：严清禾（水闸与防灾统筹）" : "夜班顾问：郑师傅（老澄海调度司）";
        string deputy = eventId == EventIdAshDome ? "现场工长：老陈（轨交检修与加挂班组）" : "调度台值班：阿雯（夜间电台热线）";
        string note = eventId == EventIdAshDome
            ? "阿卡迪亚老城的每一条街巷都要守好，先把主干通到三座避风港，再去接远郊的街坊。"
            : "夜班车不图抢那几十秒，稳稳把客人送到家门口，整条河湾的口碑自然会亮起来。";
        Txt(staffCard.transform, chief, 26, spec.accentColor, 26f, 174f, w - 52f, 38f, TextAnchor.MiddleLeft);
        Txt(staffCard.transform, deputy, 22, Ink, 26f, 134f, w - 52f, 34f, TextAnchor.MiddleLeft);
        var nt = Txt(staffCard.transform, note, 21, InkSoft, 26f, 24f, w - 52f, 96f, TextAnchor.UpperLeft);
        nt.horizontalOverflow = HorizontalWrapMode.Wrap;

        var dirCard = Rect("DirCard", content, 4f, topY - 590f, w, 306f);
        Rounded(dirCard, CardBg); SoftShadow(dirCard);
        Txt(dirCard.transform, "今日执勤方针", 27, Ink, 26f, 244f, w - 52f, 40f, TextAnchor.MiddleLeft);
        string[] dTitles = { "稳健均衡", "全力加车", "远郊补贴" };
        string[] dDescs = { "转运与夜班收益 +10%，口碑稳步增长", "全线运力 +15%，让等车的人早点出发", "送回主线金库的收益比例 +25%" };
        for (int i = 0; i < 3; i++)
        {
            int di = i;
            float ry = 172f - i * 76f;
            bool sel = st.activeDirective == di;
            var row = Rect("Dir_" + di, dirCard.transform, 24f, ry, w - 48f, 64f);
            Rounded(row, sel ? Color.Lerp(Paper2, Matcha, 0.16f) : Paper2);
            Txt(row.transform, $"{dTitles[di]}   {dDescs[di]}", 20, Ink, 18f, 0f, w - 240f, 64f, TextAnchor.MiddleLeft);
            TInk(Btn(row.transform, "Pick", sel ? "执行中" : "切换", 20, w - 214f, 10f, 140f, 44f, sel ? Matcha : Indigo, () => {
                st.activeDirective = di;
                SaveSandboxStorage();
                OpenSandboxCommandScreen(eventId);
                Toast($"执勤方针已切换为「{dTitles[di]}」");
            }), Color.white);
        }

        var cycCard = Rect("CycleCard", content, 4f, topY - 880f, w, 266f);
        Rounded(cycCard, Color.Lerp(CardBg, Indigo, 0.06f)); SoftShadow(cycCard);
        Txt(cycCard.transform, $"当前执勤轮次：第 {Mathf.Max(1, st.cycleCount)} 轮", 26, Ink, 26f, 200f, w - 52f, 38f, TextAnchor.MiddleLeft);
        var ct = Txt(cycCard.transform, "新一轮会重置街道线路和专精树点数。已赢得的主线金币、钻石、涂装、成就与购入车辆都会留下，可以安心换条路试试。", 21, InkSoft, 26f, 102f, w - 52f, 88f, TextAnchor.UpperLeft);
        ct.horizontalOverflow = HorizontalWrapMode.Wrap;

        TInk(Btn(cycCard.transform, "OpenStory", "翻阅档案手记", 22, 26f, 20f, (w - 72f) * 0.48f, 64f, Matcha, () => OpenSandboxStoryScreen(eventId)), Color.white);
        TInk(Btn(cycCard.transform, "ResetCycle", "开启新一轮执勤", 22, 26f + (w - 72f) * 0.52f, 20f, (w - 72f) * 0.48f, 64f, spec.accentColor, () => {
            ResetSandboxCycle(eventId);
            OpenSandboxCommandScreen(eventId);
        }), Color.white);
    }

    // ---------- 5. 剧情（7 章 + 7 夜） ----------
    public sealed class SandboxStoryChapter { public string eventId, title, body, logQuote; public int chapterIndex; }

    static readonly SandboxStoryChapter[] SandboxStoryChapters =
    {
        new SandboxStoryChapter { eventId = EventIdAshDome, chapterIndex = 1, title = "第一章  风起之前", body = "调度员：严工，沉降井的警报响了一早上，还能撑多久？\n严清禾：照这个风势，七天内沙尘就会压过运河。\n调度员：我先把钟楼到避风港的线路画出来。\n严清禾：好，我去通知街坊。茶先替我温着。", logQuote = "严清禾：第一条线通了，街坊才知道该往哪里走。" },
        new SandboxStoryChapter { eventId = EventIdAshDome, chapterIndex = 2, title = "第二章  晨雾首班", body = "调度员：首班车满座了，窗边那个孩子一直在数灯。\n严清禾：他奶奶带了两大袋馒头，说要分给邻座。\n调度员：西堤闸门已经打开，车马上进站。\n严清禾：让大家慢点下车，热水已经烧好了。", logQuote = "严清禾：今天接到的第一句报平安，比热茶还暖。" },
        new SandboxStoryChapter { eventId = EventIdAshDome, chapterIndex = 3, title = "第三章  断桥风声", body = "老陈：东堤高架风太大，护栏都在响。\n调度员：直行停掉，先绕老石桥。\n严清禾：滤网和护板还差多少？\n老陈：再给我一锅热水的工夫，工友们就装好。", logQuote = "老陈：螺栓拧紧一点，车上的人就能安心一点。" },
        new SandboxStoryChapter { eventId = EventIdAshDome, chapterIndex = 4, title = "第四章  守闸人", body = "调度员：严工，最后一班检修车要开了，快上来！\n严清禾：压力表还在往上走，我得把水闸稳住。\n老陈：我陪你，扳手你总不能一个人递给自己。\n严清禾：那就快些干，做完咱们一起赶车。", logQuote = "严清禾：水闸稳住了，今天又多抢出一段转运时间。" },
        new SandboxStoryChapter { eventId = EventIdAshDome, chapterIndex = 5, title = "第五章  满座站台", body = "调度员：西堤快满了，下一车往哪儿送？\n严清禾：转去南丘，医疗车厢也一起过去。\n站务员：有位阿姨不肯上车，非要找她的猫。\n严清禾：猫在我工具箱旁边打盹呢，连箱子一起送来。", logQuote = "站务员：阿姨抱着猫上车时，总算肯喝那杯热茶了。" },
        new SandboxStoryChapter { eventId = EventIdAshDome, chapterIndex = 6, title = "第六章  末班钟声", body = "调度员：邮局的钟敲九下了，末班车定在二十三点五十九分。\n严清禾：广播再念一遍，别忘了通知巷子里的老住户。\n站务员：我去敲门。有人行李太多搬不动怎么办？\n严清禾：先接人，照片和药也带上，柜子留给我操心。", logQuote = "调度员：末班时段从二十点到午夜，转运效率和收益翻倍。" },
        new SandboxStoryChapter { eventId = EventIdAshDome, chapterIndex = 7, title = "终章  晨曦入港", body = "调度员：晨曦号到南丘了，还差你们两个。\n老陈：严工的工具箱重得很，她非说里面都是宝贝。\n严清禾：少贫嘴，那是给水闸留的备用零件。\n调度员：车门关好啦。等风过去，我们再一起回家看看。", logQuote = "严清禾：大家都到了，协作基金也已送往主线公司。" },
        new SandboxStoryChapter { eventId = EventIdNeonNight, chapterIndex = 1, title = "第一夜  借盏车灯", body = "郑师傅：这块黄铜车牌给你，擦一擦还能照见人影。\n调度员：您真放心让我跑第一晚？\n郑师傅：放心。沿河灯亮着，认不清路就问电台。\n调度员：好，今晚把还在等车的人都接上。", logQuote = "郑师傅：夜班从二十点到早上六点，按住站台拖到下一站就能画线。" },
        new SandboxStoryChapter { eventId = EventIdNeonNight, chapterIndex = 2, title = "第二夜  制图师的咖啡", body = "乘客：师傅，到星象观景台。咖啡洒了，我先擦干净。\n郑师傅：不急，你眼睛都快睁不开了，歇会儿吧。\n乘客：到了叫我……咦，河里的星星怎么比天上还亮？\n郑师傅：因为你刚睡醒，连加班的眉头都松开了。", logQuote = "乘客：多留一点车资。路线图也送你，背面没有咖啡渍。" },
        new SandboxStoryChapter { eventId = EventIdNeonNight, chapterIndex = 3, title = "第三夜  老站台的旧客", body = "老乘客：麻烦沿河慢慢开，我想看看旧站台。\n郑师傅：那地方早停运了，门口的糖炒栗子倒还在。\n老乘客：我年轻时就在那儿等人，每次都买一纸袋。\n郑师傅：那咱们停一会儿，我去买两袋，您别急着走。", logQuote = "郑师傅：栗子还是烫手，老先生笑起来也像个年轻人。" },
        new SandboxStoryChapter { eventId = EventIdNeonNight, chapterIndex = 4, title = "第四夜  赶船的旅人", body = "旅人：师傅，北港码头！船马上就要开了。\n郑师傅：箱子放稳，雨天我得慢过这座桥。\n旅伴：船票在我这儿，你别把伞也落下。\n郑师傅：到了，先看脚下台阶。船笛还没响，来得及。", logQuote = "郑师傅：他们上船后还在挥伞，我这才想起关雨刷。" },
        new SandboxStoryChapter { eventId = EventIdNeonNight, chapterIndex = 5, title = "第五夜  车票作礼", body = "旅人：糟了，口袋里只有一张旧车票，钱忘带了。\n郑师傅：先别翻了，外面风大。家就在前面？\n旅人：是。车票背面写着月背的故事，先押给您。\n郑师傅：成，下回带着车费和故事的后半段来找我。", logQuote = "郑师傅：旧票夹在账本里，等那位健忘的客人回来取。" },
        new SandboxStoryChapter { eventId = EventIdNeonNight, chapterIndex = 6, title = "第六夜  郑师傅归家", body = "郑师傅：三十个星历年了，今晚换我坐副驾。\n调度员：那最后一趟，您指路，我开车。\n郑师傅：前面巷口右转，慢点，门口常有只睡觉的狗。\n调度员：到了。钥匙落在座位上了，您看。", logQuote = "郑师傅：钥匙留给你。车要有人开，热茶也要有人续。" },
        new SandboxStoryChapter { eventId = EventIdNeonNight, chapterIndex = 7, title = "终章  零号灯火", body = "调度员：原来零号老的士是您年轻时开的那辆！\n郑师傅：车门有点紧，抬一下把手就好。\n调度员：河湾的灯都亮了，我这就去接下一位。\n郑师傅：去吧，记得回来吃早饭，粥我替你温着。", logQuote = "调度员：零号又出车了，终章嘉奖已汇入主线公司。" },
    };
    void TriggerSandboxEntryStoryIfNeeded(string eventId)
    {
        var st = GetOrCreateEventSandbox(eventId);
        if (st.storyStage < 1) { st.storyStage = 1; SaveSandboxStorage(); OpenSandboxOnboardingStory(eventId, 0); }
    }

    struct SandboxDialogueLine
    {
        public string speaker, text, tip;
        public SandboxDialogueLine(string speaker, string text, string tip = null) { this.speaker = speaker; this.text = text; this.tip = tip; }
    }

    void OpenSandboxOnboardingStory(string eventId, int page = 0)
    {
        bool ash = eventId == EventIdAshDome;
        string lead = ash ? "严清禾" : "郑师傅";
        var lines = new[] {
            new SandboxDialogueLine(lead, ash ? "沙暴还有七天就到了。我们得赶在那之前，把居民送出老城。" : "夜班开始了。十四个夜晚，我们一起把等车的人平安送到家。"),
            new SandboxDialogueLine("调度员", ash ? "地图上的绿色站点是避风港，尽量让线路通往那里。" : "紫色站点是夜班热点，把街区和热点连起来吧。"),
            new SandboxDialogueLine(lead, "按住站台，等红环走满后沿街拖到下一站。拖回上一站可以撤销，连回起点就能成环。", "按住 0.42 秒，红环走满后拖动"),
            new SandboxDialogueLine("调度员", "点站台可以查看客流弧线；点线路可以增减车辆或拆除线路。"),
            new SandboxDialogueLine(lead, "专精树里能研究新的调度办法，指挥部可以切换流派。先从眼前这条路开始吧。")
        };
        OpenSandboxAvgDialogue(eventId, "开局手记", lines, null, null);
    }

    void OpenSandboxAvgDialogue(string eventId, string headerTitle, SandboxDialogueLine[] lines, string summaryQuote, Action onFinish)
    {
        if (lines == null || lines.Length == 0 || !eventSandboxRoot) { onFinish?.Invoke(); return; }
        PlayStorySfx();
        GetSafeMarginsInRoot(out float safeL, out float safeR, out float safeT, out float safeB);
        var overlay = Rect("SandboxAvg", eventSandboxRoot.transform, 0, 0, RootW, RootH);
        var scrim = Img(overlay, new Color(0.06f, 0.05f, 0.04f, 0.24f)); scrim.raycastTarget = true;
        float safeW = RootW - safeL - safeR;
        float x = safeL + 24f, width = safeW - 48f, y = safeB + 24f;
        float barH = Mathf.Clamp(Mathf.Round(RootH * 0.17f), 260f, 360f);
        bool ash = eventId == EventIdAshDome; string lead = ash ? "严清禾" : "郑师傅";
        float portraitW = Mathf.Round(RootW * 0.56f), portraitH = Mathf.Round(RootH * 0.50f);
        var portrait = Rect("Portrait", overlay.transform, safeL + safeW - portraitW + 20f, y + barH - 40f, portraitW, portraitH);
        var art = Rect("Artwork", portrait.transform, 0f, 0f, portraitW, portraitH).AddComponent<RawImage>();
        art.texture = Resources.Load<Texture2D>(ash ? "PageArt/char_yanqinghe" : "PageArt/char_zhengshifu");
        art.raycastTarget = false;
        if (art.texture)
        {
            var fit = art.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = (float)art.texture.width / Mathf.Max(1, art.texture.height);
        }
        else art.enabled = false;
        var partner = Rect("PartnerPortrait", overlay.transform, safeL - 20f, y + barH - 40f, portraitW, portraitH);
        var partnerArt = Rect("Artwork", partner.transform, 0f, 0f, portraitW, portraitH).AddComponent<RawImage>();
        partnerArt.texture = Resources.Load<Texture2D>("PageArt/char_dispatcher");
        partnerArt.raycastTarget = false;
        if (partnerArt.texture)
        {
            var fit = partnerArt.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = (float)partnerArt.texture.width / Mathf.Max(1, partnerArt.texture.height);
        }
        else partnerArt.enabled = false;
        var bar = Rect("DialogueBar", overlay.transform, x, y, width, barH); Rounded(bar, CardBg); SoftShadow(bar);
        var click = bar.AddComponent<Button>(); click.targetGraphic = bar.GetComponent<Image>(); click.transition = Selectable.Transition.None;
        var title = Txt(bar.transform, headerTitle, 18, InkSoft, 28, barH - 46, width - 180, 30, TextAnchor.MiddleLeft); title.raycastTarget = false;
        var badge = Rect("Speaker", bar.transform, 24, barH - 100, 160, 38); Rounded(badge, Indigo);
        var speaker = Txt(badge.transform, "", 21, Color.white, 4, 0, 152, 38, TextAnchor.MiddleCenter); speaker.raycastTarget = false;
        var dialogue = Txt(bar.transform, "", 28, Ink, 28, 92, width - 56, barH - 198, TextAnchor.UpperLeft); dialogue.horizontalOverflow = HorizontalWrapMode.Wrap; dialogue.raycastTarget = false;
        var tip = Txt(bar.transform, "", 17, Indigo, 28, 62, width - 56, 28, TextAnchor.UpperLeft); tip.horizontalOverflow = HorizontalWrapMode.Wrap; tip.raycastTarget = false;
        var count = Txt(bar.transform, "", 18, InkSoft, width - 150, barH - 46, 124, 30, TextAnchor.MiddleRight); count.raycastTarget = false;
        int index = 0; Action refresh = null; Action finish = () => { Destroy(overlay); onFinish?.Invoke(); };
        Button previous = null, next = null; Text nextText = null;
        Action advance = () => { if (index < lines.Length - 1) { index++; refresh(); } else finish(); };
        click.onClick.AddListener(() => advance());
        previous = Btn(bar.transform, "Previous", "上一句", 20, 24, 12, 150, 48, Paper2, () => { if (index > 0) { index--; refresh(); } }); TInk(previous, Ink);
        TInk(Btn(bar.transform, "Skip", "跳过", 20, 184, 12, 130, 48, Paper2, finish), Ink);
        next = Btn(bar.transform, "Next", "下一句", 20, width - 204, 12, 180, 48, Matcha, () => advance()); TInk(next, Color.white); nextText = next.GetComponentInChildren<Text>();
        refresh = () => {
            var line = lines[index]; speaker.text = line.speaker;
            badge.GetComponent<RectTransform>().anchoredPosition = new Vector2(line.speaker == "调度员" ? width - 184 : 24, barH - 100);
            bool speaking = line.speaker == lead;
            Color dim = new Color(0.62f, 0.60f, 0.58f, 0.85f);
            art.color = speaking ? Color.white : dim;
            partnerArt.color = speaking ? dim : Color.white;
            portrait.transform.localScale = Vector3.one * (speaking ? 1.05f : 0.94f);
            partner.transform.localScale = Vector3.one * (speaking ? 0.94f : 1.05f);
            (speaking ? portrait.transform : partner.transform).SetAsLastSibling();
            bar.transform.SetAsLastSibling();
            dialogue.text = line.text;
            tip.text = index == lines.Length - 1 && !string.IsNullOrEmpty(summaryQuote) ? "手记摘录：" + summaryQuote : (line.tip ?? "");
            count.text = (index + 1) + " / " + lines.Length; previous.interactable = index > 0;
            nextText.text = index == lines.Length - 1 ? (string.IsNullOrEmpty(summaryQuote) ? "开始调度" : "收起手记") : "下一句";
        };
        refresh();
    }

    void AdvanceSandboxStoryTo(string eventId, int chapter)
    {
        var st = GetOrCreateEventSandbox(eventId);
        if (chapter <= st.storyStage || chapter < 1 || chapter > 7) return;
        st.storyStage = chapter;
        if (eventId == EventIdAshDome && chapter == 4) st.extraA |= 0x2000;
        if (chapter == 7) GrantSandboxCompletionRewards(eventId);
        SaveSandboxStorage();
        OpenSandboxStoryChapterPopup(eventId, chapter);
    }

    void OpenSandboxStoryChapterPopup(string eventId, int chapterIndex)
    {
        SandboxStoryChapter ch = null;
        foreach (var chapter in SandboxStoryChapters)
            if (chapter.eventId == eventId && chapter.chapterIndex == chapterIndex) { ch = chapter; break; }
        if (ch == null) return;
        var lines = new List<SandboxDialogueLine>();
        foreach (string raw in ch.body.Split('\n'))
        {
            string text = raw.Trim(); if (text.Length == 0) continue;
            int colon = text.IndexOf('：');
            lines.Add(colon >= 0 ? new SandboxDialogueLine(text.Substring(0, colon).Trim(), text.Substring(colon + 1).Trim()) : new SandboxDialogueLine("手记", text));
        }
        OpenSandboxAvgDialogue(eventId, ch.title, lines.ToArray(), ch.logQuote, null);
    }

    void AddSandboxArtCard(Transform parent, string name, string resourcePath, float x, float y, float width, float height, string fallback)
    {
        var frame = Rect(name, parent, x, y, width, height);
        Rounded(frame, Color.Lerp(Paper2, CardBg, 0.45f));
        var frameImage = frame.GetComponent<Image>();
        frameImage.raycastTarget = false;
        frame.AddComponent<Mask>().showMaskGraphic = true;
        var texture = Resources.Load<Texture2D>(resourcePath);
        if (!texture)
        {
            Txt(frame.transform, fallback, 22, InkSoft, 8f, 0f, width - 16f, height, TextAnchor.MiddleCenter);
            return;
        }
        var art = Rect("Artwork", frame.transform, 0f, 0f, width, height).AddComponent<RawImage>();
        art.texture = texture;
        art.raycastTarget = false;
        var fit = art.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = (float)texture.width / Mathf.Max(1, texture.height);
    }

    void OpenSandboxStoryScreen(string eventId)
    {
        var spec = GetEventSpec(eventId);
        if (spec == null) return;
        var st = GetOrCreateEventSandbox(eventId);
        var pg = OpenPage(spec.displayTitle + "  档案手记", "重温活动期间解锁的每一个夜晚与调度记录。", "PageArt/album_watercolor", "evt_story_" + eventId);
        const float cardH = 220f, gap = 20f, padTop = 24f;
        var content = FleetScroll(pg.transform, 32f, PgH - 188f, padTop + 7 * (cardH + gap) + 170f, scrollKey: "evt_story_" + eventId);
        float topY = content.sizeDelta.y, w = content.sizeDelta.x - 8f;
        TInk(Btn(content, "ReplayGuide", "重温开局指引", 23, 4f, topY - 90f, w, 66f, Matcha, () => OpenSandboxOnboardingStory(eventId)), Color.white);
        topY -= 90f;
        for (int c = 1; c <= 7; c++)
        {
            SandboxStoryChapter ch = null;
            for (int i = 0; i < SandboxStoryChapters.Length; i++)
                if (SandboxStoryChapters[i].eventId == eventId && SandboxStoryChapters[i].chapterIndex == c) { ch = SandboxStoryChapters[i]; break; }
            if (ch == null) continue;
            float cy = topY - padTop - c * cardH - (c - 1) * gap;
            bool unlocked = st.storyStage >= c;
            var card = Rect("Chapter_" + c, content, 4f, cy, w, cardH);
            Rounded(card, unlocked ? CardBg : new Color(Paper2.r, Paper2.g, Paper2.b, 0.72f));
            SoftShadow(card);
            Txt(card.transform, unlocked ? ch.title : $"第 {c} 章  尚未解锁", 26, unlocked ? spec.accentColor : InkSoft, 26f, 166f, w - 52f, 40f, TextAnchor.MiddleLeft);
            if (unlocked)
            {
                var bt = Txt(card.transform, ch.body, 21, Ink, 26f, 56f, w - 52f, 104f, TextAnchor.UpperLeft);
                bt.horizontalOverflow = HorizontalWrapMode.Wrap; bt.verticalOverflow = VerticalWrapMode.Truncate;
                FitSingleLine(Txt(card.transform, ch.logQuote, 20, Indigo, 26f, 14f, w - 52f, 36f, TextAnchor.MiddleLeft), 20);
            }
            else Txt(card.transform, "继续推进沙盒进程即可自动解锁该章节手记。", 22, InkSoft, 26f, 82f, w - 52f, 48f, TextAnchor.MiddleLeft);
        }
    }

    // ---------- 6. 完善的多维度成就系统（16 项） ----------
    public sealed class SandboxAchSpec { public string key, eventId, title, desc; public int rewardDiamonds, rewardCoins; public Func<SavedEventSandbox, bool> checkReady; }

    static readonly SandboxAchSpec[] SandboxAchievements =
    {
        new SandboxAchSpec { key = "evt_ash_lines3", eventId = EventIdAshDome, title = "老城起步", desc = "同时开通 3 条或以上转运线路", rewardDiamonds = 15, rewardCoins = 2000, checkReady = st => st.lines.Count >= 3 },
        new SandboxAchSpec { key = "evt_ash_r1k", eventId = EventIdAshDome, title = "第一批", desc = "累计撤离 1,000 名民众", rewardDiamonds = 20, rewardCoins = 3000, checkReady = st => st.progress >= 1000 },
        new SandboxAchSpec { key = "evt_ash_r5k", eventId = EventIdAshDome, title = "人潮如海", desc = "累计撤离 5,000 名民众", rewardDiamonds = 30, rewardCoins = 6000, checkReady = st => st.progress >= 5000 },
        new SandboxAchSpec { key = "evt_ash_r20k", eventId = EventIdAshDome, title = "城里的灯还亮着", desc = "累计撤离 20,000 名民众", rewardDiamonds = 50, rewardCoins = 12000, checkReady = st => st.progress >= 20000 },
        new SandboxAchSpec { key = "evt_ash_r50k", eventId = EventIdAshDome, title = "一个都不落下", desc = "累计撤离 50,000 名民众", rewardDiamonds = 80, rewardCoins = 20000, checkReady = st => st.progress >= 50000 },
        new SandboxAchSpec { key = "evt_ash_fleet", eventId = EventIdAshDome, title = "整备车队", desc = "让至少 2 款撤离列车加入车队", rewardDiamonds = 25, rewardCoins = 4000, checkReady = st => st.ownedVehicles.Count >= 2 },
        new SandboxAchSpec { key = "evt_ash_nolost", eventId = EventIdAshDome, title = "零失联", desc = "完成第 7 日撤离且全程失联车辆为 0", rewardDiamonds = 40, checkReady = st => st.storyStage >= 7 && st.extraB == 0 },
        new SandboxAchSpec { key = "evt_ash_reborn", eventId = EventIdAshDome, title = "再走一程", desc = "在指挥部开启第 2 轮执勤", rewardDiamonds = 30, rewardCoins = 5000, checkReady = st => st.cycleCount >= 2 },

        new SandboxAchSpec { key = "evt_neon_lines3", eventId = EventIdNeonNight, title = "夜路连起来了", desc = "同时运营 3 条或以上夜班巡游线路", rewardDiamonds = 15, rewardCoins = 2000, checkReady = st => st.lines.Count >= 3 },
        new SandboxAchSpec { key = "evt_neon_o100", eventId = EventIdNeonNight, title = "夜班新人", desc = "累计完成 100 笔夜班订单", rewardDiamonds = 20, rewardCoins = 3000, checkReady = st => st.progress >= 100 },
        new SandboxAchSpec { key = "evt_neon_o500", eventId = EventIdNeonNight, title = "熟门熟路", desc = "累计完成 500 笔夜班订单", rewardDiamonds = 40, rewardCoins = 8000, checkReady = st => st.progress >= 500 },
        new SandboxAchSpec { key = "evt_neon_o2k", eventId = EventIdNeonNight, title = "不夜港活地图", desc = "累计完成 2,000 笔夜班订单", rewardDiamonds = 80, rewardCoins = 20000, checkReady = st => st.progress >= 2000 },
        new SandboxAchSpec { key = "evt_neon_fleet", eventId = EventIdNeonNight, title = "深夜车行", desc = "让至少 2 款夜班车加入车队", rewardDiamonds = 25, rewardCoins = 4000, checkReady = st => st.ownedVehicles.Count >= 2 },
        new SandboxAchSpec { key = "evt_neon_5star", eventId = EventIdNeonNight, title = "好口碑传开了", desc = "不夜港车队口碑达到 5.0 星", rewardDiamonds = 40, checkReady = st => st.extraA >= 500 },
        new SandboxAchSpec { key = "evt_neon_stories", eventId = EventIdNeonNight, title = "七个夜晚", desc = "接送齐全部 7 位夜班故事乘客", rewardDiamonds = 30, rewardCoins = 10000, checkReady = st => CountSandboxBits(st.extraB) >= 7 },
        new SandboxAchSpec { key = "evt_neon_reborn", eventId = EventIdNeonNight, title = "换班再出发", desc = "在指挥部开启第 2 轮夜班", rewardDiamonds = 30, rewardCoins = 5000, checkReady = st => st.cycleCount >= 2 }
    };

    static int CountSandboxBits(long mask) { int c = 0; while (mask != 0) { c += (int)(mask & 1L); mask >>= 1; } return c; }

    void EvaluateSandboxAchievements(string eventId)
    {
        var st = GetOrCreateEventSandbox(eventId);
        for (int i = 0; i < SandboxAchievements.Length; i++)
        {
            var a = SandboxAchievements[i];
            if (a.eventId == eventId && !st.achievements.Contains(a.key) && a.checkReady != null && a.checkReady(st))
                st.achievements.Add(a.key);
        }
    }

    int CountClaimableSandboxAchievements(string eventId)
    {
        EvaluateSandboxAchievements(eventId);
        var st = GetOrCreateEventSandbox(eventId);
        int c = 0;
        for (int i = 0; i < SandboxAchievements.Length; i++)
            if (SandboxAchievements[i].eventId == eventId && st.achievements.Contains(SandboxAchievements[i].key) && !st.claimed.Contains(SandboxAchievements[i].key)) c++;
        return c;
    }

    void ClaimSandboxAchievement(string eventId, string key)
    {
        var st = GetOrCreateEventSandbox(eventId);
        if (!st.achievements.Contains(key) || st.claimed.Contains(key)) return;
        for (int i = 0; i < SandboxAchievements.Length; i++)
        {
            var a = SandboxAchievements[i];
            if (a.key != key) continue;
            st.claimed.Add(key);
            if (a.rewardDiamonds > 0) diamonds += a.rewardDiamonds;
            if (a.rewardCoins > 0) coins += a.rewardCoins;
            PlayCoinSfx(); RefreshHUD(); RefreshEventBadge();
            SaveSandboxStorage();
            RequestSaveGame(0.5f);
            Toast($"领取成就「{a.title}」：+{a.rewardDiamonds} 💎" + (a.rewardCoins > 0 ? $"  +¥{a.rewardCoins:N0}" : ""));
            return;
        }
    }

    void UnlockSandboxVehicleAndSkin(string eventId, string vehicleKey)
    {
        var spec = GetEventSpec(eventId);
        if (spec == null || spec.vehicles == null) return;
        var st = GetOrCreateEventSandbox(eventId);
        if (!st.ownedVehicles.Contains(vehicleKey)) st.ownedVehicles.Add(vehicleKey);
        for (int i = 0; i < spec.vehicles.Length; i++)
        {
            var v = spec.vehicles[i];
            if (v.key != vehicleKey) continue;
            int idx = Mathf.Clamp(v.skinFallbackIndex, 0, LineSkinNames.Length - 1);
            if (!st.ownedSkins.Contains(idx)) st.ownedSkins.Add(idx);
            if (!ownedLineSkins.Contains(idx))
            {
                ownedLineSkins.Add(idx);
                Toast($"入库限定车辆 {v.name}  主线同步解锁涂装「{LineSkinNames[idx]}」");
            }
            else
            {
                coins += 5000;
                Toast($"入库限定车辆 {v.name}  主线涂装折现 +¥5,000");
            }
            break;
        }
        SaveSandboxStorage();
        RequestSaveGame(0.5f);
    }

    void GrantSandboxCompletionRewards(string eventId)
    {
        var st = GetOrCreateEventSandbox(eventId);
        string rewardTag = "completed_reward_" + eventId;
        if (st.claimed.Contains(rewardTag)) return;
        st.claimed.Add(rewardTag);
        coins += 25000;
        diamonds += 60;
        RefreshHUD();
        Toast("终章礼金 +¥25,000  +60 💎 已直汇主线公司账户");
        SaveSandboxStorage();
        RequestSaveGame(0.5f);
    }

    void OpenAshCompletionReportPopup()
    {
        var st = GetOrCreateEventSandbox(EventIdAshDome);
        int cleared = st.ashClearedStations != null ? st.ashClearedStations.Count : 0;
        string grade = st.missedPop == 0 ? "S" : st.missedPop < 600 ? "A" : "B";
        var panel = OpenPopup("七日撤离最终报告", 580f, 760f);
        if (!panel) return;
        string completion = st.claimed.Contains("completed_reward_" + EventIdAshDome) ? "终章奖励：¥25,000 和 60 钻石已发放（每活动一次）" : "终章奖励：尚未领取";
        Txt(panel.transform, $"本轮安全转运总数：{st.progress:N0} 人\n已撤空站台数：{cleared}\n封控失联人数：{st.missedPop:N0} 人\n综合评级：{grade}\n\n本轮转运支援主线：¥{st.ashCycleRewards:N0}\n{completion}", 25, Ink, 36f, 175f, 688f, 310f, TextAnchor.UpperLeft);
        TInk(Btn(panel.transform, "NewCycle", "前往指挥部开启新轮", 22, 30f, 48f, 340f, 72f, Matcha, () => { ClosePanel(true); OpenSandboxCommandScreen(EventIdAshDome); }), Color.white);
        TInk(Btn(panel.transform, "Stay", "留览地图", 22, 390f, 48f, 340f, 72f, Indigo, () => ClosePanel(true)), Color.white);
    }

    void OpenSandboxAchievementsScreen(string eventId)
    {
        var spec = GetEventSpec(eventId);
        if (spec == null) return;
        EvaluateSandboxAchievements(eventId);
        var st = GetOrCreateEventSandbox(eventId);
        var pg = OpenPage(spec.displayTitle + "  成就与典藏", "达成里程碑可领钻石与金币，并同步反哺主线公司。", "PageArt/album_watercolor", "evt_ach_" + eventId);

        var ordered = new List<SandboxAchSpec>();
        for (int pass = 0; pass < 3; pass++)
        {
            for (int i = 0; i < SandboxAchievements.Length; i++)
            {
                var a = SandboxAchievements[i];
                if (a.eventId != eventId) continue;
                bool unlocked = st.achievements.Contains(a.key), claimed = st.claimed.Contains(a.key);
                if ((pass == 0 && unlocked && !claimed) || (pass == 1 && !unlocked) || (pass == 2 && claimed))
                    ordered.Add(a);
            }
        }

        const float rowH = 154f, gap = 18f, padTop = 24f;
        var content = FleetScroll(pg.transform, 32f, PgH - 188f, padTop + ordered.Count * (rowH + gap) + 120f, scrollKey: "evt_ach_" + eventId);
        float topY = content.sizeDelta.y, w = content.sizeDelta.x - 8f;

        for (int rowIdx = 0; rowIdx < ordered.Count; rowIdx++)
        {
            var ach = ordered[rowIdx];
            float ry = topY - padTop - (rowIdx + 1) * rowH - rowIdx * gap;
            bool unlocked = st.achievements.Contains(ach.key), claimed = st.claimed.Contains(ach.key);
            var card = Rect("Ach_" + ach.key, content, 4f, ry, w, rowH);
            Rounded(card, unlocked && !claimed ? Color.Lerp(CardBg, Gold, 0.14f) : CardBg);
            SoftShadow(card);
            Txt(card.transform, ach.title, 28, Ink, 28f, 94f, w - 280f, 40f, TextAnchor.MiddleLeft);
            Txt(card.transform, ach.desc, 22, InkSoft, 28f, 50f, w - 280f, 36f, TextAnchor.MiddleLeft);
            Txt(card.transform, $"奖励  {ach.rewardDiamonds} 💎" + (ach.rewardCoins > 0 ? $"   ¥{ach.rewardCoins:N0}" : ""), 22, Indigo, 28f, 14f, w - 280f, 34f, TextAnchor.MiddleLeft);

            if (claimed) TInk(Btn(card.transform, "Claimed", "已领取", 22, w - 210f, 42f, 180f, 68f, Paper2, null), InkSoft);
            else if (unlocked)
            {
                string k = ach.key;
                TInk(Btn(card.transform, "Claim", "领取奖励", 23, w - 210f, 42f, 180f, 68f, Vermilion, () => { ClaimSandboxAchievement(eventId, k); OpenSandboxAchievementsScreen(eventId); }), Color.white);
            }
            else TInk(Btn(card.transform, "Locked", "未达成", 22, w - 210f, 42f, 180f, 68f, new Color(Paper2.r, Paper2.g, Paper2.b, 0.65f), null), InkSoft);
        }
    }
}

// 沙盒专精树中可解锁节点的轻柔呼吸效果。
public sealed class HistoryNodePulse : MonoBehaviour
{
    RectTransform rect;
    void Awake() { rect = (RectTransform)transform; }
    void Update()
    {
        float scale = 1f + 0.025f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f));
        rect.localScale = new Vector3(scale, scale, 1f);
    }
    void OnDisable() { if (rect) rect.localScale = Vector3.one; }
}
