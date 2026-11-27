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
        // —— 赤尘撤离志（40 节点可缩放分支科技树） ——
        new SandboxTreeNodeSpec { key = "ash_cap_1", eventId = EventIdAshDome, branch = 0, tier = 1, branchName = "重装运力", title = "加挂车厢", desc = "全队单车核载 +10%", cost = 3, treeX = 0.12f, treeY = 0.90f },
        new SandboxTreeNodeSpec { key = "ash_cap_1b", eventId = EventIdAshDome, branch = 0, tier = 2, branchName = "重装运力", title = "双层改造", desc = "全队单车核载再 +10%", cost = 5, parentKey = "ash_cap_1", treeX = 0.12f, treeY = 0.79f },
        new SandboxTreeNodeSpec { key = "ash_cap_2", eventId = EventIdAshDome, branch = 0, tier = 3, branchName = "重装运力", title = "重型客舱", desc = "核载再 +15%（与长程连挂互斥）", cost = 8, parentKey = "ash_cap_1b", exclusiveKey = "ash_cap_2b", treeX = 0.06f, treeY = 0.68f },
        new SandboxTreeNodeSpec { key = "ash_cap_2b", eventId = EventIdAshDome, branch = 0, tier = 3, branchName = "重装运力", title = "长程连挂", desc = "全队车速 +10%（与重型客舱互斥）", cost = 8, parentKey = "ash_cap_1b", exclusiveKey = "ash_cap_2", treeX = 0.18f, treeY = 0.68f },
        new SandboxTreeNodeSpec { key = "ash_cap_3", eventId = EventIdAshDome, branch = 0, tier = 4, branchName = "重装运力", title = "铁壁重载组", desc = "解锁「铁壁」护航车车库购置资格", cost = 12, parentKey = "ash_cap_1b", unlockVehicleKey = "veh_ash_iron", treeX = 0.12f, treeY = 0.57f },
        new SandboxTreeNodeSpec { key = "ash_cap_4", eventId = EventIdAshDome, branch = 0, tier = 5, branchName = "重装运力", title = "重载制动", desc = "全队单车核载再 +12%", cost = 16, parentKey = "ash_cap_3", treeX = 0.12f, treeY = 0.46f },
        new SandboxTreeNodeSpec { key = "ash_cap_5", eventId = EventIdAshDome, branch = 0, tier = 6, branchName = "重装运力", title = "宽体底盘", desc = "核载再 +15%（与高频发车互斥）", cost = 20, parentKey = "ash_cap_4", exclusiveKey = "ash_cap_5b", treeX = 0.06f, treeY = 0.35f },
        new SandboxTreeNodeSpec { key = "ash_cap_5b", eventId = EventIdAshDome, branch = 0, tier = 6, branchName = "重装运力", title = "高频发车", desc = "全队车速再 +12%（与宽体底盘互斥）", cost = 20, parentKey = "ash_cap_4", exclusiveKey = "ash_cap_5", treeX = 0.18f, treeY = 0.35f },
        new SandboxTreeNodeSpec { key = "ash_cap_6", eventId = EventIdAshDome, branch = 0, tier = 7, branchName = "重装运力", title = "编组调度", desc = "撤离反哺主线资金 +20%", cost = 26, parentKey = "ash_cap_4", treeX = 0.12f, treeY = 0.23f },
        new SandboxTreeNodeSpec { key = "ash_cap_7", eventId = EventIdAshDome, branch = 0, tier = 8, branchName = "重装运力", title = "巨无霸专列", desc = "全队单车核载再 +20%", cost = 34, parentKey = "ash_cap_6", treeX = 0.12f, treeY = 0.11f },
        new SandboxTreeNodeSpec { key = "ash_def_1", eventId = EventIdAshDome, branch = 1, tier = 1, branchName = "固守延缓", title = "防尘滤网", desc = "沦陷站滞留缓冲 +0.4 天", cost = 3, treeX = 0.37f, treeY = 0.90f },
        new SandboxTreeNodeSpec { key = "ash_def_1b", eventId = EventIdAshDome, branch = 1, tier = 2, branchName = "固守延缓", title = "密封站台", desc = "风暴扩散延缓 +6 小时", cost = 5, parentKey = "ash_def_1", treeX = 0.37f, treeY = 0.79f },
        new SandboxTreeNodeSpec { key = "ash_def_2", eventId = EventIdAshDome, branch = 1, tier = 3, branchName = "固守延缓", title = "装甲蒙皮", desc = "滞留缓冲再 +0.8 天（与冷却注浆互斥）", cost = 9, parentKey = "ash_def_1b", exclusiveKey = "ash_def_2b", treeX = 0.31f, treeY = 0.68f },
        new SandboxTreeNodeSpec { key = "ash_def_2b", eventId = EventIdAshDome, branch = 1, tier = 3, branchName = "固守延缓", title = "冷却注浆", desc = "风暴延缓再 +10 小时（与装甲蒙皮互斥）", cost = 9, parentKey = "ash_def_1b", exclusiveKey = "ash_def_2", treeX = 0.43f, treeY = 0.68f },
        new SandboxTreeNodeSpec { key = "ash_def_3", eventId = EventIdAshDome, branch = 1, tier = 4, branchName = "固守延缓", title = "白鸽专列", desc = "解锁「白鸽」医疗专列车库购置资格", cost = 14, parentKey = "ash_def_1b", unlockVehicleKey = "veh_ash_dove", treeX = 0.37f, treeY = 0.57f },
        new SandboxTreeNodeSpec { key = "ash_def_4", eventId = EventIdAshDome, branch = 1, tier = 5, branchName = "固守延缓", title = "穹顶闭环", desc = "沦陷站滞留缓冲再 +0.8 天", cost = 18, parentKey = "ash_def_3", treeX = 0.37f, treeY = 0.46f },
        new SandboxTreeNodeSpec { key = "ash_def_5", eventId = EventIdAshDome, branch = 1, tier = 6, branchName = "固守延缓", title = "应急气闸", desc = "风暴延缓再 +12 小时（与防沙墙互斥）", cost = 22, parentKey = "ash_def_4", exclusiveKey = "ash_def_5b", treeX = 0.31f, treeY = 0.35f },
        new SandboxTreeNodeSpec { key = "ash_def_5b", eventId = EventIdAshDome, branch = 1, tier = 6, branchName = "固守延缓", title = "防沙墙", desc = "滞留缓冲再 +0.8 天（与应急气闸互斥）", cost = 22, parentKey = "ash_def_4", exclusiveKey = "ash_def_5", treeX = 0.43f, treeY = 0.35f },
        new SandboxTreeNodeSpec { key = "ash_def_6", eventId = EventIdAshDome, branch = 1, tier = 7, branchName = "固守延缓", title = "地下掩体", desc = "风暴延缓再 +14 小时", cost = 28, parentKey = "ash_def_4", treeX = 0.37f, treeY = 0.23f },
        new SandboxTreeNodeSpec { key = "ash_def_7", eventId = EventIdAshDome, branch = 1, tier = 8, branchName = "固守延缓", title = "全城防风网", desc = "沦陷站滞留缓冲再 +1.0 天，风暴延缓 +12 小时", cost = 36, parentKey = "ash_def_6", treeX = 0.37f, treeY = 0.11f },
        new SandboxTreeNodeSpec { key = "ash_res_1", eventId = EventIdAshDome, branch = 2, tier = 1, branchName = "医疗速援", title = "医疗车厢", desc = "全队单车核载 +10%", cost = 3, treeX = 0.63f, treeY = 0.90f },
        new SandboxTreeNodeSpec { key = "ash_res_1b", eventId = EventIdAshDome, branch = 2, tier = 2, branchName = "医疗速援", title = "净尘引擎", desc = "全队车速 +10%", cost = 5, parentKey = "ash_res_1", treeX = 0.63f, treeY = 0.79f },
        new SandboxTreeNodeSpec { key = "ash_res_2", eventId = EventIdAshDome, branch = 2, tier = 3, branchName = "医疗速援", title = "避风港扩容", desc = "中转接力效率 +20%（与速援补贴互斥）", cost = 9, parentKey = "ash_res_1b", exclusiveKey = "ash_res_2b", treeX = 0.57f, treeY = 0.68f },
        new SandboxTreeNodeSpec { key = "ash_res_2b", eventId = EventIdAshDome, branch = 2, tier = 3, branchName = "医疗速援", title = "速援补贴", desc = "转运反哺主线资金 +20%（与避风港扩容互斥）", cost = 9, parentKey = "ash_res_1b", exclusiveKey = "ash_res_2", treeX = 0.69f, treeY = 0.68f },
        new SandboxTreeNodeSpec { key = "ash_res_3", eventId = EventIdAshDome, branch = 2, tier = 4, branchName = "医疗速援", title = "晨曦旗舰", desc = "解锁「晨曦号」旗舰车库购置资格", cost = 14, parentKey = "ash_res_1b", unlockVehicleKey = "veh_ash_ark", treeX = 0.63f, treeY = 0.57f },
        new SandboxTreeNodeSpec { key = "ash_res_4", eventId = EventIdAshDome, branch = 2, tier = 5, branchName = "医疗速援", title = "远郊救护站", desc = "全队车速 +12%，核载 +10%", cost = 18, parentKey = "ash_res_3", treeX = 0.63f, treeY = 0.46f },
        new SandboxTreeNodeSpec { key = "ash_res_5", eventId = EventIdAshDome, branch = 2, tier = 6, branchName = "医疗速援", title = "急诊绿道", desc = "全队车速再 +15%（与随车医护互斥）", cost = 22, parentKey = "ash_res_4", exclusiveKey = "ash_res_5b", treeX = 0.57f, treeY = 0.35f },
        new SandboxTreeNodeSpec { key = "ash_res_5b", eventId = EventIdAshDome, branch = 2, tier = 6, branchName = "医疗速援", title = "随车医护", desc = "全队核载再 +12%（与急诊绿道互斥）", cost = 22, parentKey = "ash_res_4", exclusiveKey = "ash_res_5", treeX = 0.69f, treeY = 0.35f },
        new SandboxTreeNodeSpec { key = "ash_res_6", eventId = EventIdAshDome, branch = 2, tier = 7, branchName = "医疗速援", title = "生命走廊", desc = "转运反哺主线资金 +25%", cost = 28, parentKey = "ash_res_4", treeX = 0.63f, treeY = 0.23f },
        new SandboxTreeNodeSpec { key = "ash_res_7", eventId = EventIdAshDome, branch = 2, tier = 8, branchName = "医疗速援", title = "曙光总动员", desc = "全队车速再 +18%，核载 +10%", cost = 35, parentKey = "ash_res_6", treeX = 0.63f, treeY = 0.11f },
        new SandboxTreeNodeSpec { key = "ash_sci_1", eventId = EventIdAshDome, branch = 3, tier = 1, branchName = "通用统筹", title = "应急冷却剂", desc = "风暴延缓 +8 小时", cost = 4, treeX = 0.88f, treeY = 0.90f },
        new SandboxTreeNodeSpec { key = "ash_sci_2", eventId = EventIdAshDome, branch = 3, tier = 2, branchName = "通用统筹", title = "磁约束水闸", desc = "风暴延缓再 +10 小时", cost = 6, parentKey = "ash_sci_1", treeX = 0.88f, treeY = 0.79f },
        new SandboxTreeNodeSpec { key = "ash_sci_3", eventId = EventIdAshDome, branch = 3, tier = 3, branchName = "通用统筹", title = "深层导风井", desc = "全队核载 +12%（与跨线接力站互斥）", cost = 10, parentKey = "ash_sci_2", exclusiveKey = "ash_uni_a", treeX = 0.82f, treeY = 0.68f },
        new SandboxTreeNodeSpec { key = "ash_uni_a", eventId = EventIdAshDome, branch = 3, tier = 3, branchName = "通用统筹", title = "跨线接力站", desc = "全城火种券产出 +15%（与深层导风井互斥）", cost = 10, parentKey = "ash_sci_2", exclusiveKey = "ash_sci_3", treeX = 0.94f, treeY = 0.68f },
        new SandboxTreeNodeSpec { key = "ash_uni_b", eventId = EventIdAshDome, branch = 3, tier = 4, branchName = "通用统筹", title = "末班车调度令", desc = "全队车速 +12%", cost = 14, parentKey = "ash_sci_2", treeX = 0.88f, treeY = 0.57f },
        new SandboxTreeNodeSpec { key = "ash_sci_4", eventId = EventIdAshDome, branch = 3, tier = 5, branchName = "通用统筹", title = "火种统筹令", desc = "全城火种券产出 +15%", cost = 18, parentKey = "ash_uni_b", treeX = 0.88f, treeY = 0.46f },
        new SandboxTreeNodeSpec { key = "ash_sci_5", eventId = EventIdAshDome, branch = 3, tier = 6, branchName = "通用统筹", title = "轮回档案", desc = "每轮开局额外赠送资金 ¥1,500（与备战储备互斥）", cost = 22, parentKey = "ash_sci_4", exclusiveKey = "ash_sci_5b", treeX = 0.82f, treeY = 0.35f },
        new SandboxTreeNodeSpec { key = "ash_sci_5b", eventId = EventIdAshDome, branch = 3, tier = 6, branchName = "通用统筹", title = "备战储备", desc = "全城火种券产出再 +15%（与轮回档案互斥）", cost = 22, parentKey = "ash_sci_4", exclusiveKey = "ash_sci_5", treeX = 0.94f, treeY = 0.35f },
        new SandboxTreeNodeSpec { key = "ash_sci_6", eventId = EventIdAshDome, branch = 3, tier = 7, branchName = "通用统筹", title = "极速调度网", desc = "全队车速再 +15%", cost = 28, parentKey = "ash_sci_4", treeX = 0.88f, treeY = 0.23f },
        new SandboxTreeNodeSpec { key = "ash_sci_7", eventId = EventIdAshDome, branch = 3, tier = 8, branchName = "通用统筹", title = "方舟协议", desc = "运力 +15%，车速 +15%，火种券 +15%", cost = 38, parentKey = "ash_sci_6", treeX = 0.88f, treeY = 0.11f },

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
        if (node.eventId == EventIdAshDome || node.branch >= 3 || node.tier < 2) return false;
        return st.activeBranch >= 0 && st.activeBranch != node.branch;
    }

    bool IsSandboxExclusiveLocked(SavedEventSandbox st, SandboxTreeNodeSpec node)
        => !string.IsNullOrEmpty(node.exclusiveKey) && st.treeNodes.Contains(node.exclusiveKey);

    bool HasSandboxTierInBranch(SavedEventSandbox st, string eventId, int branch, int tier)
    {
        for (int i = 0; i < SandboxTreeNodes.Length; i++)
        {
            var n = SandboxTreeNodes[i];
            if (n.eventId == eventId && n.branch == branch && n.tier == tier && st.treeNodes.Contains(n.key)) return true;
        }
        return false;
    }

    bool CanResearchSandboxTreeNode(string eventId, SandboxTreeNodeSpec node)
    {
        if (node == null || node.eventId != eventId) return false;
        var st = GetOrCreateEventSandbox(eventId);
        if (st.treeNodes.Contains(node.key)) return false;
        if (IsSandboxBranchLocked(st, node) || IsSandboxExclusiveLocked(st, node)) return false;
        if (eventId == EventIdAshDome && (node.tier == 4 || node.tier == 7))
        {
            if (!HasSandboxTierInBranch(st, eventId, node.branch, node.tier - 1)) return false;
        }
        else if (eventId != EventIdAshDome && node.tier == 3 && node.branch < 3)
        {
            if (!HasSandboxTierInBranch(st, eventId, node.branch, 2)) return false;
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
        if (eventId != EventIdAshDome && !string.IsNullOrEmpty(target.unlockVehicleKey))
            UnlockSandboxVehicleAndSkin(eventId, target.unlockVehicleKey);
        else if (eventId == EventIdAshDome && !string.IsNullOrEmpty(target.unlockVehicleKey))
            Toast($"已解锁「{target.title}」，可前往救援车车库购置");
        else
            Toast($"已研发「{target.title}」");
        RefreshEventBadge();
        if (activeSandboxEventId == EventIdAshDome && eventId == EventIdAshDome) RefreshSandboxHudText();
        SaveSandboxStorage();
    }

    int GetSandboxLineLimit(string eventId)
    {
        var spec = GetEventSpec(eventId);
        int cap = spec != null ? spec.lineLimit : 8;
        if (eventId == EventIdAshDome) return 99;
        else if (eventId == EventIdNeonNight)
        {
            if (HasSandboxTreeNode(EventIdNeonNight, "neon_fleet_2b")) cap += 3;
            if (HasSandboxTreeNode(EventIdNeonNight, "neon_uni_b")) cap += 2;
        }
        return cap;
    }

    float GetAshCapMult()
    {
        float m = 1f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_cap_1")) m *= 1.10f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_cap_1b")) m *= 1.10f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_cap_2")) m *= 1.15f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_cap_4")) m *= 1.12f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_cap_5")) m *= 1.15f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_cap_7")) m *= 1.20f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_res_1")) m *= 1.10f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_res_4")) m *= 1.10f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_res_5b")) m *= 1.12f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_res_7")) m *= 1.10f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_sci_3")) m *= 1.12f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_sci_7")) m *= 1.15f;
        return m;
    }

    float GetAshSpeedMult()
    {
        float m = 1f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_cap_2b")) m *= 1.10f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_cap_5b")) m *= 1.12f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_res_1b")) m *= 1.10f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_res_4")) m *= 1.12f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_res_5")) m *= 1.15f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_res_7")) m *= 1.18f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_uni_b")) m *= 1.12f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_sci_6")) m *= 1.15f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_sci_7")) m *= 1.15f;
        return m;
    }

    float GetAshTokenMult()
    {
        float m = 1f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_uni_a")) m *= 1.15f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_sci_4")) m *= 1.15f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_sci_5b")) m *= 1.15f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_sci_7")) m *= 1.15f;
        return m;
    }

    int GetAshTotalVehiclePool(SavedEventSandbox st) => st.ownedVehicles.Count;

    int GetAshFreeVehicles(SavedEventSandbox st) => Mathf.Max(0, st.ownedVehicles.Count - st.lines.Count);

    int AshOwnedCount(SavedEventSandbox st, string key)
    {
        int count = 0; foreach (var v in st.ownedVehicles) if (v == key) count++;
        return count;
    }

    int AshUsedCount(List<SavedEventLine> assigned, string key, int exceptIndex = -1)
    {
        int count = 0;
        for (int i = 0; i < assigned.Count; i++) if (i != exceptIndex && assigned[i] != null && assigned[i].vehicleKey == key) count++;
        return count;
    }

    string FindAshIdleVehicle(SavedEventSandbox st, List<SavedEventLine> assigned)
    {
        foreach (var key in st.ownedVehicles)
            if (GetSandboxVehicleDef(EventIdAshDome, key) != null && AshUsedCount(assigned, key) < AshOwnedCount(st, key)) return key;
        return null;
    }

    const string AshNoVehiclesToast = "当前救援车已全部派在线路上，请前往车库购买更多救援车或拆除闲置线路";
    float GetAshOverflowScale()
    {
        float m = 1f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_cap_6")) m *= 1.20f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_res_2b")) m *= 1.20f;
        if (HasSandboxTreeNode(EventIdAshDome, "ash_res_6")) m *= 1.25f;
        return m;
    }

    float GetAshExtraImmuneDays() => (HasSandboxTreeNode(EventIdAshDome, "ash_def_1") ? 0.4f : 0f) + (HasSandboxTreeNode(EventIdAshDome, "ash_def_2") ? 0.8f : 0f) + (HasSandboxTreeNode(EventIdAshDome, "ash_def_4") ? 0.8f : 0f) + (HasSandboxTreeNode(EventIdAshDome, "ash_def_5b") ? 0.8f : 0f) + (HasSandboxTreeNode(EventIdAshDome, "ash_def_7") ? 1f : 0f);
    float GetAshDelayHours() => (HasSandboxTreeNode(EventIdAshDome, "ash_def_1b") ? 6f : 0f) + (HasSandboxTreeNode(EventIdAshDome, "ash_def_2b") ? 10f : 0f) + (HasSandboxTreeNode(EventIdAshDome, "ash_def_5") ? 12f : 0f) + (HasSandboxTreeNode(EventIdAshDome, "ash_def_6") ? 14f : 0f) + (HasSandboxTreeNode(EventIdAshDome, "ash_def_7") ? 12f : 0f) + (HasSandboxTreeNode(EventIdAshDome, "ash_sci_1") ? 8f : 0f) + (HasSandboxTreeNode(EventIdAshDome, "ash_sci_2") ? 10f : 0f);
    int GetAshStartFundsBonus() => HasSandboxTreeNode(EventIdAshDome, "ash_sci_5") ? 1500 : 0;

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
        var spec = GetEventSpec(eventId);
        if (spec == null) return;
        var st = GetOrCreateEventSandbox(eventId);
        string treeTitle = eventId == EventIdAshDome ? "方舟专精树" : "夜班专精树";
        string specName = eventId == EventIdAshDome ? "自由研修" : st.activeBranch < 0 ? "未定（2阶任选一派锁定主修）"
            : eventId == EventIdAshDome
                ? (st.activeBranch == 0 ? "重装运力" : st.activeBranch == 1 ? "固守延缓" : "医疗速援")
                : (st.activeBranch == 0 ? "车队扩张" : st.activeBranch == 1 ? "跨区调度" : "口碑礼宾");
        var pg = OpenPage($"{spec.displayTitle}  {treeTitle}", "滚轮缩放与拖拽画布；单击节点预览详情，双击节点立项解锁。", "PageArt/album_watercolor", "evt_tree_" + eventId);

        GetSafeMarginsInRoot(out float safeL, out float safeR, out float safeT, out float safeB);
        float safeW = Mathf.Max(320f, RootW - safeL - safeR);
        float safeH = Mathf.Max(1200f, RootH - safeT - safeB);
        float padX = safeL + (safeW - W) * 0.5f;

        // 顶部状态条（在 safeT 之下）
        float topY = RootH - safeT - 160f - 82f;
        var topInfo = Rect("TreeTopInfo", pageRoot.transform, padX + 28f, topY, W - 56f, 68f);
        Rounded(topInfo, CardBg); SoftShadow(topInfo);
        FitSingleLine(Txt(topInfo.transform, eventId == EventIdAshDome ? $"第 {Mathf.Max(1, st.cycleCount)} 轮" : $"第 {Mathf.Max(1, st.cycleCount)} 轮   主修：{specName}", 21, Ink, 22f, 0f, W - 370f, 68f, TextAnchor.MiddleLeft), 21);
        FitSingleLine(Txt(topInfo.transform, $"可用{spec.tokenName}：{st.tokens}", 23, Indigo, W - 310f, 0f, 220f, 68f, TextAnchor.MiddleRight), 23);

        // 底部预览条（单击预览详情，再点直接解锁）
        float previewH = 128f;
        float previewY = safeB + 20f;
        var previewBar = Rect("TreePreviewBar", pageRoot.transform, padX + 28f, previewY, W - 56f, previewH);
        Rounded(previewBar, CardBg); SoftShadow(previewBar);
        var prevTitle = Txt(previewBar.transform, "选择节点查看详情", 23, Ink, 24f, 74f, W - 300f, 40f, TextAnchor.MiddleLeft);
        FitSingleLine(prevTitle, 23);
        var prevDesc = Txt(previewBar.transform, eventId == EventIdAshDome ? "" : "选中 2 阶分叉后将锁定本轮主修路线，互斥分支自动置灰", 20, InkSoft, 24f, 16f, W - 300f, 52f, TextAnchor.UpperLeft);
        prevDesc.horizontalOverflow = HorizontalWrapMode.Wrap;
        prevDesc.verticalOverflow = VerticalWrapMode.Truncate; prevDesc.lineSpacing = 1.08f;
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
        float vpH = Mathf.Max(0f, topY - 14f - vpY);
        float vpW = W - 56f;
        var vpGo = Rect("TreeViewport", pageRoot.transform, padX + 28f, vpY, vpW, vpH);
        var vpImg = Img(vpGo, new Color(0.975f, 0.962f, 0.930f, 0.96f), spriteRound);
        vpImg.type = Image.Type.Sliced; vpImg.raycastTarget = true;
        vpGo.AddComponent<Mask>().showMaskGraphic = true;
        SoftShadow(vpGo);

        float cw = eventId == EventIdAshDome ? 2500f : 2200f, ch = eventId == EventIdAshDome ? 2250f : 1320f;
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
        TInk(Btn(pageRoot.transform, "ZoomIn", "+", 22, padX + W - 220, vpY + vpH - 60, 50, 48, Paper2, () => setZoom(1.2f)), Ink);
        TInk(Btn(pageRoot.transform, "ZoomOut", "−", 22, padX + W - 165, vpY + vpH - 60, 50, 48, Paper2, () => setZoom(1f / 1.2f)), Ink);
        TInk(Btn(pageRoot.transform, "ResetView", "复位", 18, padX + W - 110, vpY + vpH - 60, 72, 48, Paper2, () => { resetView(); sandboxTreeSaveView(); }), Ink);
        var header = pageRoot.GetComponent<PageHeaderLayout>();
        header.topBarBg.SetAsLastSibling(); header.topDivider.SetAsLastSibling();
        header.back.SetAsLastSibling(); header.home.SetAsLastSibling(); header.title.SetAsLastSibling();
        if (header.help) header.help.SetAsLastSibling();

        for (int gx = 0; gx < cw; gx += 100) { var grid = Img(Rect("GridV", canvasGo.transform, gx, 0, 1, ch), new Color(0.4f, 0.35f, 0.3f, 0.08f)); grid.raycastTarget = false; }
        for (int gy = 0; gy < ch; gy += 100) { var grid = Img(Rect("GridH", canvasGo.transform, 0, gy, cw, 1), new Color(0.4f, 0.35f, 0.3f, 0.08f)); grid.raycastTarget = false; }
        string[] headers = eventId == EventIdAshDome ? new[] { "重装运力", "固守延缓", "医疗速援", "通用统筹" } : new[] { "通用基础", "车队扩张", "跨区调度", "口碑礼宾" };
        for (int h = 0; h < headers.Length; h++) { var banner = Rect("BranchHeader", canvasGo.transform, eventId == EventIdAshDome ? cw * new[] { 0.12f, 0.37f, 0.63f, 0.88f }[h] - 100f : 175 + h * 550, eventId == EventIdAshDome ? ch * 0.95f : 1190, 200, 50); Rounded(banner, Indigo); Txt(banner.transform, headers[h], 21, Color.white, 0, 0, 200, 50, TextAnchor.MiddleCenter).raycastTarget = false; }
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
            posByKey[n.key] = eventId == EventIdAshDome ? new Vector2(cw * n.treeX, ch * n.treeY) : new Vector2(center + offset, 1070f - (n.tier - 1) * 270f);
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            Vector2 toPos = posByKey[n.key];
            bool nLocked = IsSandboxBranchLocked(st, n) || IsSandboxExclusiveLocked(st, n);
            Color edgeCol = st.treeNodes.Contains(n.key) ? Matcha : nLocked ? new Color(0.65f, 0.63f, 0.60f, 0.28f) : new Color(InkSoft.r, InkSoft.g, InkSoft.b, 0.42f);
            bool merge = eventId == EventIdAshDome && (n.tier == 4 || n.tier == 7);
            if (!merge && !string.IsNullOrEmpty(n.parentKey) && posByKey.TryGetValue(n.parentKey, out var fromPos))
                DrawTreeEdge(lineLayer.transform, fromPos + Vector2.down * 54f, toPos + Vector2.up * 54f, edgeCol);
            if (merge || eventId != EventIdAshDome && n.tier == 3 && n.branch < 3)
            {
                for (int j = 0; j < nodes.Count; j++)
                    if (nodes[j].branch == n.branch && nodes[j].tier == (merge ? n.tier - 1 : 2))
                        DrawTreeEdge(lineLayer.transform, posByKey[nodes[j].key] + Vector2.down * 54f, toPos + Vector2.up * 54f, edgeCol);
            }
        }

        foreach (var n in nodes)
            if (!string.IsNullOrEmpty(n.exclusiveKey) && string.CompareOrdinal(n.key, n.exclusiveKey) < 0 && posByKey.ContainsKey(n.exclusiveKey))
            { var mid = (posByKey[n.key] + posByKey[n.exclusiveKey]) * 0.5f; var mark = Rect("Exclusive", canvasGo.transform, mid.x - 50, mid.y - 90, 100, 32); Rounded(mark, Vermilion); Txt(mark.transform, "⇄ 互斥", 16, Color.white, 0, 0, 100, 32, TextAnchor.MiddleCenter).raycastTarget = false; }
        for (int branch = 0; eventId != EventIdAshDome && branch < 2; branch++)
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
        var pg = OpenPage($"{spec.displayTitle}  {(eventId == EventIdAshDome ? "撤离专线" : "线路调度")}", $"{(eventId == EventIdAshDome ? $"可用救援车：{GetAshFreeVehicles(st)}/{GetAshTotalVehiclePool(st)}" : $"已通车 {st.lines.Count} / {cap} 条街道线路")}", "PageArt/line_watercolor", "evt_lines_" + eventId);

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
            Txt(card.transform, eventId == EventIdAshDome ? $"{vDef?.name ?? "基础车型"}" : $"配属车型：{vDef?.name ?? "基础车型"}   当前编组：{Mathf.Max(1, line.cars)} 辆", 21, InkSoft, 22f, 76f, w - 44f, 34f, TextAnchor.MiddleLeft);

            int li = i;
            float bw = (w - 64f) / 3f;
            if (eventId != EventIdAshDome)
            TInk(Btn(card.transform, "AddCar", $"+1 辆车 (4 {spec.tokenName})", 20, 22f, 16f, bw, 48f, Matcha, () => {
                if (eventId == EventIdAshDome && GetAshFreeVehicles(st) < 1) { Toast(AshNoVehiclesToast); return; }
                if (eventId != EventIdAshDome && st.tokens < 4) { PlayErrorSfx(); Toast($"{spec.tokenName}不足 4"); return; }
                if (eventId != EventIdAshDome && st.lines[li].cars >= 4) { PlayErrorSfx(); Toast("单线编组上限为 4 辆"); return; }
                if (eventId != EventIdAshDome) st.tokens -= 4; st.lines[li].cars++;
                PlayBuildSfx(); SaveSandboxStorage(); RebuildSandboxLines(); OpenSandboxLinesScreen(eventId);
            }), Color.white);

            TInk(Btn(card.transform, "SwapVeh", "更换车型", 20, 22f + (bw + 10f) * (eventId == EventIdAshDome ? 0f : 1f), 16f, bw, 48f, Indigo, () => {
                OpenSandboxLineVehiclePicker(eventId, li);
            }), Color.white);

            TInk(Btn(card.transform, "DelLine", "撤除线路", 20, 22f + (bw + 10f) * (eventId == EventIdAshDome ? 1f : 2f), 16f, bw, 48f, Vermilion, () => {
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
        var choices = new List<string>();
        foreach (var key in st.ownedVehicles) if (!choices.Contains(key) && (eventId != EventIdAshDome || AshOwnedCount(st, key) > AshUsedCount(st.lines, key, lineIdx))) choices.Add(key);
        for (int i = 0; i < choices.Count; i++)
        {
            string vk = choices[i];
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
        var pg = OpenPage($"{spec.displayTitle}  {(eventId == EventIdAshDome ? "救援车与防沙改装" : "车辆与涂装工坊")}", eventId == EventIdAshDome ? $"火种券：{st.tokens}" : $"可用{spec.tokenName}：{st.tokens}", "FleetArt/garage_watercolor", "evt_fleet_" + eventId);

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
                FitSingleLine(Txt(card.transform, v.name, 27, Ink, 236f, 114f, w - 470f, 40f, TextAnchor.MiddleLeft), 27);
                var dt = Txt(card.transform, eventId == EventIdAshDome ? $"运力 {v.capMult:0.##}倍  速度 {v.speedMult:0.##}倍" + (v.deliveredBonus > 0 ? $"  转运 +{v.deliveredBonus * 100:0}%" : "") + (v.extraImmuneDays > 0 ? $"  防护 +{v.extraImmuneDays}天" : "") : v.desc, 20, InkSoft, 236f, 54f, w - 470f, 54f, TextAnchor.UpperLeft);
                dt.horizontalOverflow = HorizontalWrapMode.Wrap;
                dt.verticalOverflow = VerticalWrapMode.Truncate; dt.lineSpacing = 1.08f;
                Txt(card.transform, eventId == EventIdAshDome ? $"已购 {AshOwnedCount(st, v.key)} 辆  空闲 {Mathf.Max(0, AshOwnedCount(st, v.key) - AshUsedCount(st.lines, v.key))} 辆" : owned ? "已入库，可在「航线」派车" : $"购置费：{v.costTokens} {spec.tokenName}", 20, owned ? Matcha : Indigo, 236f, 14f, w - 470f, 30f, TextAnchor.MiddleLeft);

                if (eventId == EventIdAshDome)
                {
                    string vk = v.key; int cost = v.costTokens;
                    string prerequisite = vk == "veh_ash_iron" ? "ash_cap_3" : vk == "veh_ash_dove" ? "ash_def_3" : vk == "veh_ash_ark" ? "ash_res_3" : null;
                    bool unlocked = prerequisite == null || st.treeNodes.Contains(prerequisite);
                    TInk(Btn(card.transform, "BuyAsh", unlocked ? $"购置 1 辆 ({cost} 火种券)" : "专精树解锁", 18, w - 210f, 52f, 180f, 68f, unlocked ? Indigo : Paper2, () => {
                        if (!unlocked) { OpenSandboxTreeScreen(eventId); return; }
                        if (st.tokens < cost) { Toast("火种券不足"); return; }
                        st.tokens -= cost; st.ownedVehicles.Add(vk); SaveSandboxStorage(); RefreshSandboxHudText(); OpenSandboxFleetAndSkinScreen(eventId);
                    }), unlocked ? Color.white : InkSoft);
                }
                else if (owned) TInk(Btn(card.transform, "Owned", "已入库", 22, w - 210f, 52f, 180f, 68f, Matcha, null), Color.white);
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
                Img(swatch, LineSkinColors[sIdx], spriteCircle).raycastTarget = false;
                Img(Rect("Trim", card.transform, 38f, 58f, 28f, 28f), LineSkinTrimColors[sIdx], spriteCircle).raycastTarget = false;
                Txt(card.transform, $"涂装「{LineSkinNames[sIdx]}」", 26, Ink, 98f, 82f, w - 330f, 40f, TextAnchor.MiddleLeft);
                Txt(card.transform, SkinPerkSummary(sIdx), 20, InkSoft, 98f, 28f, w - 330f, 40f, TextAnchor.MiddleLeft);

                if (owned) TInk(Btn(card.transform, "Apply", "涂装全线", 21, w - 210f, 38f, 180f, 66f, Matcha, () => {
                    Color sc = LineSkinColors[sIdx];
                    for (int li = 0; li < st.lines.Count; li++) st.lines[li].color = sc;
                    SaveSandboxStorage(); RebuildSandboxLines(); Toast($"已为沙盒全线换装「{LineSkinNames[sIdx]}」");
                }), Color.white);
                else if (eventId == EventIdAshDome) TInk(Btn(card.transform, "LockedSkin", "尚未解锁", 21, w - 210f, 38f, 180f, 66f, Paper2, null), InkSoft);
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
    void OpenSandboxRewardsScreen(string eventId)
    {
        var spec = GetEventSpec(eventId);
        if (spec == null) return;
        var st = GetOrCreateEventSandbox(eventId);
        bool ash = eventId == EventIdAshDome;
        var pg = OpenPage(spec.displayTitle + "  奖励", "", "PageArt/company_watercolor", "evt_rewards_" + eventId);
        int count = ash ? 4 : 1;
        var content = FleetScroll(pg.transform, 32f, PgH - 188f, 200f + count * 190f, scrollKey: "evt_rewards_" + eventId);
        float w = content.sizeDelta.x - 8f, top = content.sizeDelta.y;
        long total = ash ? st.totalDelivered : (ownedTrainSkins.Contains(5) ? 1L : 0L);
        long overallTarget = ash ? 4000L : 1L;
        var header = Rect("Overall", content, 4f, top - 150f, w, 132f);
        Rounded(header, CardBg); SoftShadow(header);
        Txt(header.transform, ash ? "累计转运" : "限定车皮", 25, Ink, 24f, 78f, w - 48f, 36f, TextAnchor.MiddleLeft);
        Txt(header.transform, ash ? $"{Math.Min(total, overallTarget):N0} / {overallTarget:N0} 人" : (total > 0 ? "已解锁" : "购入夜燕、紫星或零号车辆解锁"), 20, InkSoft, 24f, 40f, w - 48f, 32f, TextAnchor.MiddleLeft);
        var bar = Rect("Progress", header.transform, 24f, 18f, w - 48f, 14f);
        Img(bar, Paper2).raycastTarget = false;
        Img(Rect("Fill", bar.transform, 0f, 0f, (w - 48f) * Mathf.Clamp01((float)total / overallTarget), 14f), spec.accentColor).raycastTarget = false;
        for (int i = 0; i < count; i++)
        {
            int skinId = ash ? i + 1 : 5;
            long target = ash ? skinId * 1000L : 1L;
            var skin = MainTrainSkins[skinId];
            bool unlocked = ownedTrainSkins.Contains(skinId);
            var card = Rect("SkinReward" + skinId, content, 4f, top - 340f - i * 190f, w, 174f);
            Rounded(card, CardBg); SoftShadow(card);
            AddTrainSkinArt(card.transform, skin, 16f, 35f, 150f, 100f, false);
            Txt(card.transform, skin.name, 24, Ink, 180f, 132f, w - 370f, 34f, TextAnchor.MiddleLeft);
            Txt(card.transform, LocoNames[skin.locoNode], 19, InkSoft, 180f, 99f, w - 370f, 30f, TextAnchor.MiddleLeft);
            Txt(card.transform, unlocked ? "已解锁" : "未解锁", 19, InkSoft, w - 170f, 128f, 145f, 30f, TextAnchor.MiddleCenter);
            TInk(Btn(card.transform, "Preview", "预览", 20, w - 170f, 60f, 145f, 54f, Matcha, () => OpenTrainSkinPreviewPopup(skinId, true)), Color.white);
            Txt(card.transform, ash ? $"{Math.Min(total, target):N0} / {target:N0} 人" : (unlocked ? "1 / 1" : "0 / 1"), 18, InkSoft, 180f, 51f, w - 370f, 30f, TextAnchor.MiddleLeft);
            float barWidth = w - 370f;
            var milestone = Rect("Milestone", card.transform, 180f, 30f, barWidth, 12f);
            Img(milestone, Paper2).raycastTarget = false;
            Img(Rect("Fill", milestone.transform, 0f, 0f, barWidth * Mathf.Clamp01((float)total / target), 12f), spec.accentColor).raycastTarget = false;
        }
    }

    void OpenSandboxCommandScreen(string eventId)
    {
        var spec = GetEventSpec(eventId);
        if (spec == null) return;
        var st = GetOrCreateEventSandbox(eventId);
        var pg = OpenPage($"{spec.displayTitle}  调度指挥部", eventId == EventIdAshDome ? "" : $"可用{spec.tokenName}：{st.tokens}", "PageArt/company_watercolor", "evt_cmd_" + eventId);

        if (eventId == EventIdAshDome)
        {
            var ashContent = FleetScroll(pg.transform, 32f, PgH - 188f, 850f, scrollKey: "evt_cmd_" + eventId);
            float aw = ashContent.sizeDelta.x - 8f;
            var policy = Rect("Policy", ashContent, 4f, 530f, aw, 290f);
            Rounded(policy, CardBg); SoftShadow(policy);
            Txt(policy.transform, "执勤方针", 27, Ink, 24f, 230f, aw - 48f, 40f, TextAnchor.MiddleLeft);
            string[] titles = { "稳健均衡", "抢险加急", "远郊援护" };
            string[] effects = { "主线资金 +10%", "主线资金 -5%", "主线资金 +25%" };
            for (int i = 0; i < titles.Length; i++)
            {
                int choice = i; bool selected = st.activeDirective == i;
                var row = Rect("Policy" + i, policy.transform, 24f, 164f - i * 72f, aw - 48f, 62f);
                Rounded(row, selected ? Color.Lerp(Paper2, Matcha, 0.16f) : Paper2);
                Txt(row.transform, titles[i] + "  " + effects[i], 21, Ink, 18f, 0f, aw - 240f, 62f, TextAnchor.MiddleLeft);
                TInk(Btn(row.transform, "Pick", selected ? "执行中" : "切换", 20, aw - 214f, 9f, 140f, 44f, selected ? Matcha : Indigo, () => { st.activeDirective = choice; SaveSandboxStorage(); OpenSandboxCommandScreen(eventId); }), Color.white);
            }
            var situation = Rect("Situation", ashContent, 4f, 390f, aw, 120f);
            Rounded(situation, CardBg); SoftShadow(situation);
            Txt(situation.transform, "本轮态势", 26, Ink, 24f, 68f, aw - 48f, 36f, TextAnchor.MiddleLeft);
            Txt(situation.transform, $"当前轮次  {Mathf.Max(1, st.cycleCount)}     已转运  {st.progress:N0}     火种券  {st.tokens}", 22, Ink, 24f, 14f, aw - 48f, 42f, TextAnchor.MiddleLeft);
            var feedback = Rect("MainFeedback", ashContent, 4f, 190f, aw, 180f);
            Rounded(feedback, CardBg); SoftShadow(feedback);
            Txt(feedback.transform, "主线回馈", 26, Ink, 24f, 126f, aw - 48f, 36f, TextAnchor.MiddleLeft);
            Txt(feedback.transform, "每人 ¥20   通关 ¥25,000 + 60 钻", 22, Ink, 24f, 76f, aw - 48f, 36f, TextAnchor.MiddleLeft);
            return;
        }

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
        string[] dTitles = { "稳健均衡", "抢险加急", "远郊援护" };
        string[] dDescs = eventId == EventIdAshDome ? new[] { "常规撤离节奏，稳定积累火种", "优先保障近圈主干转运速度", "加大对主线公司的救援物资反哺" } : new[] { "转运与夜班收益 +10%，口碑稳步增长", "全线运力 +15%，让等车的人早点出发", "送回主线金库的收益比例 +25%" };
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
        var ct = Txt(cycCard.transform, eventId == EventIdAshDome ? "专精节点与成就保留，线路和火种券将清零。" + (st.tokens > 0 ? $"\n尚余 {st.tokens} 枚火种券，重生后将清零，建议先在专精树解锁节点" : "") : "新一轮会重置街道线路和专精树点数。已赢得的主线金币、钻石、涂装、成就与购入车辆都会留下，可以安心换条路试试。", 21, InkSoft, 26f, 102f, w - 52f, 88f, TextAnchor.UpperLeft);
        ct.horizontalOverflow = HorizontalWrapMode.Wrap;

        TInk(Btn(cycCard.transform, "OpenStory", eventId == EventIdAshDome ? "前往专精树" : "翻阅档案手记", 22, 26f, 20f, (w - 72f) * 0.48f, 64f, Matcha, () => { if (eventId == EventIdAshDome) OpenSandboxTreeScreen(eventId); else OpenSandboxStoryScreen(eventId); }), Color.white);
        TInk(Btn(cycCard.transform, "ResetCycle", eventId == EventIdAshDome ? $"开启第 {st.cycleCount + 1} 轮重生" : "开启新一轮执勤", 22, 26f + (w - 72f) * 0.52f, 20f, (w - 72f) * 0.48f, 64f, spec.accentColor, () => {
            ResetSandboxCycle(eventId);
            OpenSandboxCommandScreen(eventId);
        }), Color.white);
    }

    // ---------- 5. 剧情（7 章 + 7 夜） ----------
    public sealed class SandboxStoryChapter { public string eventId, title, body, logQuote; public int chapterIndex; }

    static readonly SandboxStoryChapter[] SandboxStoryChapters =
    {
        new SandboxStoryChapter { eventId = EventIdAshDome, chapterIndex = 1, title = "第一章  风起老城", body = "调度员：严工，七天之内沙暴就要压过运河了，手里却只有两辆老救援车。\n严清禾：两万多街坊全指着这几条街，再难也得先把第一批人送出去。\n调度员：可运力和时间摆在这儿，根本顾不过来那么远……\n严清禾：先别算总数，能拉走一个是一个。我去通知街口，你画线。", logQuote = "严清禾：明知救不完，第一班车也必须准点发出去。" },
        new SandboxStoryChapter { eventId = EventIdAshDome, chapterIndex = 2, title = "第二章  尘雾初班", body = "调度员：第一批人进避风港了，可外圈还有好几座站台黑着灯。\n严清禾：人手不够的时候，只能先保住离港口近、人又最密的街区。\n调度员：那西边那些来不及接上的站台怎么办？\n严清禾：把路况和缺口记在图谱上……别停手，下一班车已经进站了。", logQuote = "严清禾：每一张送到港里的车票，都在替后来者攒下火种。" },
        new SandboxStoryChapter { eventId = EventIdAshDome, chapterIndex = 3, title = "第三章  断桥取舍", body = "老陈：东堤高架风压太猛，旧车顶着风走一趟要多耗一半工夫。\n调度员：那就缩线跑短途接力，或者换进更快的救援车。\n严清禾：别把车耗死在远路上，算清每小时能拉走多少人。\n老陈：明白，我把车轴再紧一紧，争取多抢回几趟。", logQuote = "老陈：车少的时候，跑对一条近道比硬撑十里更有用。" },
        new SandboxStoryChapter { eventId = EventIdAshDome, chapterIndex = 4, title = "第四章  守闸抢时", body = "调度员：严工，外环封控比预想的快，很多站台撑不到天亮了！\n严清禾：我带人去顶住西堤水闸，至少能替你们多拖出半天窗口。\n老陈：就剩两把扳手，我也跟着下井。\n严清禾：外面的线路交给你了，能多撤空一座是一座。", logQuote = "严清禾：抢出来的不是时间，是站台上多出来的几十条退路。" },
        new SandboxStoryChapter { eventId = EventIdAshDome, chapterIndex = 5, title = "第五章  风沙逼近", body = "调度员：已经有站台彻底失联了，剩下的街坊全挤在中心几条街。\n严清禾：别回头看灰掉的站名，把有限的车全压到还亮着的线路上。\n站务员：有位老人把家里的旧怀表塞给我，说让后来的车队拿去换零件。\n严清禾：收好吧，我们不会让这趟车白跑的。", logQuote = "站务员：站台越来越静，只有车灯还一遍遍往返。" },
        new SandboxStoryChapter { eventId = EventIdAshDome, chapterIndex = 6, title = "第六章  末班号角", body = "调度员：今晚二十点之后，最后两天的末班紧急通道就彻底打开了。\n严清禾：所有备用车全放出去，能拼进避风港一批是一批。\n老陈：车厢超载警示一直在响，还跑吗？\n严清禾：跑！把门锁死，天亮前能冲过去多少算多少。", logQuote = "调度员：第 6、7 日 20:00 起进入末班冲刺，全线转运效率与支援翻倍。" },
        new SandboxStoryChapter { eventId = EventIdAshDome, chapterIndex = 7, title = "终章  风止港门", body = "调度员：……严工，最后一批车到港了。全城站台，全部清零。\n老陈：奇怪，沙幕怎么停在防风墙外面不动了？表针终于走过七点整了。\n严清禾：因为这一次，我们把所有人都带出来了，再也不用回到清晨六点。\n调度员：嗯，这一次，天终于亮了。", logQuote = "严清禾：一轮又一轮攒下的火种，终于把整座老城带进了晨曦。" },
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
        if (eventId == EventIdAshDome)
        {
            if (st.storyStage < 1 || st.eventDay == 1 && st.lines.Count == 0 && st.progress == 0)
            {
                if (st.storyStage < 1) st.storyStage = 1;
                SaveSandboxStorage();
                OpenSandboxOnboardingStory(eventId, 0);
            }
            return;
        }
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
        if (!ash)
        {
            var neonLines = new[] {
                new SandboxDialogueLine("郑师傅", "夜班开始了。十四个夜晚，我们一起把等车的人平安送到家。"),
                new SandboxDialogueLine("调度员", "紫色站点是夜班热点，把街区和热点连起来吧。"),
                new SandboxDialogueLine("郑师傅", "按住站台，等红环走满后沿街拖到下一站。拖回上一站可以撤销，连回起点就能成环。", "按住 0.42 秒，红环走满后拖动"),
                new SandboxDialogueLine("调度员", "点站台可以查看客流弧线；点线路可以增减车辆或拆除线路。"),
                new SandboxDialogueLine("郑师傅", "专精树里能研究新的调度办法，指挥部可以切换流派。先从眼前这条路开始吧。")
            };
            OpenSandboxAvgDialogue(eventId, "开局手记", neonLines, null, null);
            return;
        }

        var st = GetOrCreateEventSandbox(eventId);
        int cycle = Mathf.Max(1, st.cycleCount);
        SandboxDialogueLine[] lines;
        string title;
        if (cycle == 1)
        {
            title = "第 1 轮  沙暴倒计时";
            lines = new[] {
                new SandboxDialogueLine("严清禾", "风沙七天后就会彻底吞掉老城……可两万三千多名街坊，我们手里却只有两辆旧救援车。"),
                new SandboxDialogueLine("调度员", "哪怕只够救下眼前这几条街，也不能干看着。先把线接到绿色避风港，能拉走多少是多少！"),
                new SandboxDialogueLine("严清禾", "记住，每开一条新线至少要派出一辆车，长按站台蓄满红环再沿街拖向下一站。", "长按 0.42 秒连线  任意绿色避风港均可下客"),
                new SandboxDialogueLine("调度员", "一车一线，攒火种券解锁专精并去车库购置更多救援车。")
            };
        }
        else if (cycle == 2)
        {
            title = "第 2 轮  清晨六点的回响";
            lines = new[] {
                new SandboxDialogueLine("严清禾", "等一下……窗外的钟声怎么又响在第一天清晨六点？沙尘明明已经漫过钟楼了！"),
                new SandboxDialogueLine("调度员", "严工你看手边的图纸——我们上一轮攒下的专精图谱和车辆经验全都还在脑子里！"),
                new SandboxDialogueLine("严清禾", "……不是梦。那些倒在风沙里的火种真的把我们带回来了。"),
                new SandboxDialogueLine("调度员", "那就别浪费这次重来的机会！用上上一轮点开的专精，再去车库购置车辆，这一次我们能多救下好几条街。", "前世专精节点永久保留  车辆与线路每轮重置")
            };
        }
        else if (cycle == 3)
        {
            title = "第 3 轮  痛心取舍";
            lines = new[] {
                new SandboxDialogueLine("严清禾", "又回到六点整了。看清地图了吗——两三轮之内，外郊那些小站还是来不及全保住。"),
                new SandboxDialogueLine("调度员", "嗯，与其把车耗死在远路上两头落空，不如先把重车压在人最多、倒计时最紧的圈层。"),
                new SandboxDialogueLine("严清禾", "挑几条站台先把它们彻底撤空，攒够火种券继续点亮专精树。", "先保高回报近圈  必要时忍痛放弃远郊")
            };
        }
        else
        {
            title = $"第 {cycle} 轮  火种未熄";
            lines = new[] {
                new SandboxDialogueLine("调度员", "茶还是温的。严工，这次车队比上一轮更齐整了。"),
                new SandboxDialogueLine("严清禾", "老规矩，盯紧每座站台的剩余人数和封控倒计时，别让任何一辆车空转在死线上。"),
                new SandboxDialogueLine("调度员", "明白。一轮一轮把防线往外推，直到把老城最后一个街坊也送进避风港！", $"当前可用救援车 {GetAshTotalVehiclePool(st)} 辆  全员撤空即可破局")
            };
        }
        OpenSandboxAvgDialogue(eventId, title, lines, null, null);
    }

    void OpenSandboxAvgDialogue(string eventId, string headerTitle, SandboxDialogueLine[] lines, string summaryQuote, Action onFinish)
    {
        if (lines == null || lines.Length == 0 || !eventSandboxRoot) { onFinish?.Invoke(); return; }
        PlayStorySfx();
        GetSafeMarginsInRoot(out float safeL, out float safeR, out float safeT, out float safeB);
        var overlay = new GameObject("SandboxAvg", typeof(RectTransform));
        overlay.transform.SetParent(eventSandboxRoot.transform, false);
        var ovRt = overlay.GetComponent<RectTransform>();
        ovRt.anchorMin = Vector2.zero; ovRt.anchorMax = Vector2.one;
        ovRt.offsetMin = Vector2.zero; ovRt.offsetMax = Vector2.zero;
        var scrim = Img(overlay, new Color(0.06f, 0.05f, 0.04f, 0.24f)); scrim.raycastTarget = true;
        float width = Mathf.Min(W - 48f, 1000f);
        float y = safeB + 24f;
        float barH = Mathf.Clamp(Mathf.Round(RootH * 0.22f), 380f, 460f);
        bool ash = eventId == EventIdAshDome; string lead = ash ? "严清禾" : "郑师傅";
        float portraitW = Mathf.Round(Mathf.Min(W, RootW) * 0.52f), portraitH = Mathf.Round(RootH * 0.48f);
        var portrait = Rect("Portrait", overlay.transform, 0f, 0f, portraitW, portraitH);
        var portraitRt = portrait.GetComponent<RectTransform>();
        portraitRt.anchorMin = portraitRt.anchorMax = new Vector2(0.5f, 0f); portraitRt.pivot = new Vector2(0.5f, 0f);
        portraitRt.sizeDelta = new Vector2(portraitW, portraitH); portraitRt.anchoredPosition = new Vector2(width * 0.22f, y + barH - 40f);
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
        var partner = Rect("PartnerPortrait", overlay.transform, 0f, 0f, portraitW, portraitH);
        var partnerRt = partner.GetComponent<RectTransform>();
        partnerRt.anchorMin = partnerRt.anchorMax = new Vector2(0.5f, 0f); partnerRt.pivot = new Vector2(0.5f, 0f);
        partnerRt.sizeDelta = new Vector2(portraitW, portraitH); partnerRt.anchoredPosition = new Vector2(-width * 0.22f, y + barH - 40f);
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
        var bar = Rect("DialogueBar", overlay.transform, 0f, 0f, width, barH);
        var barRt = bar.GetComponent<RectTransform>();
        barRt.anchorMin = barRt.anchorMax = new Vector2(0.5f, 0f); barRt.pivot = new Vector2(0.5f, 0f);
        barRt.sizeDelta = new Vector2(width, barH); barRt.anchoredPosition = new Vector2(0f, y);
        Rounded(bar, CardBg); SoftShadow(bar);
        var click = bar.AddComponent<Button>(); click.targetGraphic = bar.GetComponent<Image>(); click.transition = Selectable.Transition.None;
        var title = Txt(bar.transform, headerTitle, 18, InkSoft, 28, barH - 46, width - 180, 30, TextAnchor.MiddleLeft); title.raycastTarget = false;
        var badge = Rect("Speaker", bar.transform, 24, barH - 100, 160, 38); Rounded(badge, Indigo);
        var speaker = Txt(badge.transform, "", 21, Color.white, 4, 0, 152, 38, TextAnchor.MiddleCenter); speaker.raycastTarget = false;
        var dialogue = Txt(bar.transform, "", 26, Ink, 28, 120, width - 56, barH - 232, TextAnchor.UpperLeft); dialogue.horizontalOverflow = HorizontalWrapMode.Wrap; dialogue.verticalOverflow = VerticalWrapMode.Truncate; dialogue.lineSpacing = 1.18f; dialogue.raycastTarget = false;
        var tip = Txt(bar.transform, "", 18, Indigo, 28, 72, width - 56, 40, TextAnchor.MiddleLeft); tip.horizontalOverflow = HorizontalWrapMode.Wrap; tip.verticalOverflow = VerticalWrapMode.Truncate; tip.raycastTarget = false;
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
            tip.gameObject.SetActive(!string.IsNullOrEmpty(tip.text));
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
        if (chapter == 7 && eventId != EventIdAshDome) GrantSandboxCompletionRewards(eventId);
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
    public sealed class SandboxAchSpec { public string key, eventId, title, desc, previewArt; public int rewardDiamonds, rewardCoins; public Func<SavedEventSandbox, bool> checkReady; }

    static readonly SandboxAchSpec[] SandboxAchievements =
    {
        new SandboxAchSpec { key = "evt_ash_lines3", eventId = EventIdAshDome, previewArt = "PageArt/ach_ash_1", title = "老城起步", desc = "同时开通 3 条或以上转运线路", rewardDiamonds = 15, rewardCoins = 2000, checkReady = st => st.lines.Count >= 3 },
        new SandboxAchSpec { key = "evt_ash_r1k", eventId = EventIdAshDome, previewArt = "PageArt/ach_ash_6", title = "第一批", desc = "累计撤离 1,000 名民众", rewardDiamonds = 20, rewardCoins = 3000, checkReady = st => st.progress >= 1000 },
        new SandboxAchSpec { key = "evt_ash_r5k", eventId = EventIdAshDome, previewArt = "PageArt/ach_ash_2", title = "人潮如海", desc = "累计撤离 5,000 名民众", rewardDiamonds = 30, rewardCoins = 6000, checkReady = st => st.progress >= 5000 },
        new SandboxAchSpec { key = "evt_ash_r20k", eventId = EventIdAshDome, previewArt = "PageArt/ach_ash_3", title = "城里的灯还亮着", desc = "累计撤离 20,000 名民众", rewardDiamonds = 50, rewardCoins = 12000, checkReady = st => st.progress >= 20000 },
        new SandboxAchSpec { key = "evt_ash_r50k", eventId = EventIdAshDome, previewArt = "PageArt/ach_ash_4", title = "一个都不落下", desc = "累计撤离 50,000 名民众", rewardDiamonds = 80, rewardCoins = 20000, checkReady = st => st.progress >= 50000 },
        new SandboxAchSpec { key = "evt_ash_fleet", eventId = EventIdAshDome, previewArt = "PageArt/ach_ash_7", title = "整备车队", desc = "让至少 2 款撤离列车加入车队", rewardDiamonds = 25, rewardCoins = 4000, checkReady = st => st.ownedVehicles.Count >= 2 },
        new SandboxAchSpec { key = "evt_ash_nolost", eventId = EventIdAshDome, previewArt = "PageArt/ach_ash_5", title = "零失联", desc = "完成第 7 日撤离且无人失联", rewardDiamonds = 40, checkReady = st => st.eventDay > AshSandboxTotalDays && st.missedPop == 0 },
        new SandboxAchSpec { key = "evt_ash_reborn", eventId = EventIdAshDome, previewArt = "PageArt/ach_ash_8", title = "再走一程", desc = "在指挥部开启第 2 轮执勤", rewardDiamonds = 30, rewardCoins = 5000, checkReady = st => st.cycleCount >= 2 },

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
        if (eventId == EventIdNeonNight && (vehicleKey == "veh_neon_amethyst" || vehicleKey == "veh_neon_zero" || vehicleKey == "veh_neon_owl")) UnlockMainTrainSkin(5);
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

    void ConfirmAshEarlySettlementPopup()
    {
        var st = GetOrCreateEventSandbox(EventIdAshDome);
        EnsureAshStationPopInitialized(st);
        int remainingPop = 0;
        for (int i = 0; i < st.stationPop.Count; i++) remainingPop += Mathf.Max(0, st.stationPop[i]);
        if (remainingPop == 0 && st.missedPop == 0 && st.progress > 0)
        {
            OpenAshCompletionReportPopup();
            return;
        }
        var panel = OpenPopup("确认立即结算？", 380f, 680f);
        if (!panel) return;
        string body = remainingPop > 0
            ? $"当前仍有 {remainingPop:N0} 名居民待转运，提前结算将按现状结束本轮。是否继续？"
            : "确认立即结束本轮调度并进入结案结算？";
        var msg = Txt(panel.transform, body, 23, Ink, 36f, 148f, 608f, 110f, TextAnchor.UpperLeft);
        msg.horizontalOverflow = HorizontalWrapMode.Wrap; msg.lineSpacing = 1.15f;
        TInk(Btn(panel.transform, "Cancel", "继续调度", 22, 36f, 44f, 290f, 68f, Paper2, () => ClosePanel(true)), Ink);
        TInk(Btn(panel.transform, "Confirm", "确认结算", 22, 354f, 44f, 290f, 68f, Vermilion, () => { ClosePanel(true); OpenAshCompletionReportPopup(); }), Color.white);
    }

    void OpenAshCompletionReportPopup()
    {
        var st = GetOrCreateEventSandbox(EventIdAshDome);
        EnsureAshStationPopInitialized(st);
        int cleared = st.ashClearedStations != null ? st.ashClearedStations.Count : 0;
        int remainingPop = 0;
        for (int i = 0; i < st.stationPop.Count; i++) remainingPop += Mathf.Max(0, st.stationPop[i]);
        int unrescued = st.missedPop + remainingPop;
        bool allSaved = st.missedPop == 0 && remainingPop == 0 && st.progress > 0;
        if (allSaved) GrantSandboxCompletionRewards(EventIdAshDome);
        string grade = allSaved ? "S" : (st.progress >= 12000 && unrescued < 2000) ? "A" : "B";
        var panel = OpenPopup(allSaved ? "全城撤离完成  风止港门" : "七日撤离结案报告", 590f, 760f);
        if (!panel) return;
        string moodLine = allSaved
            ? "所有老城街坊都已平安进港，这一次，清晨六点不会再重复。"
            : st.cycleCount <= 1
                ? "风沙还是先一步封住了街口……但这一次带回来的火种，也许能改写下一轮。"
                : "没能全部带走，但前世的路线与经验还在图谱里。点亮更多节点再来一次。";
        string completion = st.claimed.Contains("completed_reward_" + EventIdAshDome) ? "终章奖励：¥25,000 和 60 钻石已发放（每活动一次）" : "终章奖励：尚未达成（需全员撤空）";
        var report = Txt(panel.transform, $"{moodLine}\n\n本轮安全转运总数：{st.progress:N0} 人\n已撤空站台数：{cleared}\n未能转运人数：{unrescued:N0} 人\n综合评级：{grade}   支援主线：¥{st.ashCycleRewards:N0}\n{completion}", 23, Ink, 36f, 185f, 688f, 320f, TextAnchor.UpperLeft);
        report.horizontalOverflow = HorizontalWrapMode.Wrap; report.verticalOverflow = VerticalWrapMode.Truncate; report.lineSpacing = 1.08f;
        if (st.tokens > 0) FitSingleLine(Txt(panel.transform, $"尚余 {st.tokens} 枚火种券，重生后清零", 20, Vermilion, 36f, 128f, 688f, 42f, TextAnchor.MiddleLeft), 20);
        TInk(Btn(panel.transform, "Tree", "前往专精树", 22, 30f, 48f, 340f, 72f, Indigo, () => { ClosePanel(true); OpenSandboxTreeScreen(EventIdAshDome); }), Color.white);
        TInk(Btn(panel.transform, "NewCycle", $"开启第 {st.cycleCount + 1} 轮重生", 22, 390f, 48f, 340f, 72f, Matcha, () => { ClosePanel(true); ResetSandboxCycle(EventIdAshDome); }), Color.white);
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
            var tex = string.IsNullOrEmpty(ach.previewArt) ? null : Resources.Load<Texture2D>(ach.previewArt);
            if (tex != null)
            {
                var body = Rect("PreviewHitArea", card.transform, 0f, 0f, w - 220f, rowH);
                var hit = Img(body, new Color(1f, 1f, 1f, 0f));
                var previewButton = body.AddComponent<Button>();
                previewButton.targetGraphic = hit; previewButton.transition = Selectable.Transition.None;
                previewButton.onClick.AddListener(() => OpenPureImagePreview(ach.title, tex));
                var thumb = Rect("Thumbnail", card.transform, 18f, 23f, 192f, 108f);
                Rounded(thumb, Color.white); thumb.GetComponent<Image>().raycastTarget = false;
                var thumbMask = thumb.AddComponent<Mask>(); thumbMask.showMaskGraphic = false;
                var image = Rect("Art", thumb.transform, 0f, 0f, 192f, 108f);
                var art = image.AddComponent<RawImage>(); art.texture = tex; art.raycastTarget = false;
            }
            float textX = tex != null ? 228f : 28f;
            float textW = tex != null ? w - 460f : w - 280f;
            Txt(card.transform, ach.title, 28, Ink, textX, 94f, textW, 40f, TextAnchor.MiddleLeft).raycastTarget = false;
            Txt(card.transform, ach.desc, 22, InkSoft, textX, 50f, textW, 36f, TextAnchor.MiddleLeft).raycastTarget = false;
            Txt(card.transform, $"奖励  {ach.rewardDiamonds} 💎" + (ach.rewardCoins > 0 ? $"   ¥{ach.rewardCoins:N0}" : ""), 22, Indigo, textX, 14f, textW, 34f, TextAnchor.MiddleLeft).raycastTarget = false;

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
