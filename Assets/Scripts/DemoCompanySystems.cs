using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class DemoBootstrap
{
    // ---------- M2: 公司经营科技树（企业治理树）与人才招募培养系统 ----------
    [Serializable]
    public sealed class SavedTalent
    {
        public int id;
        public string name;
        public int role;       // 0: 星港事务长, 1: 王牌舰长, 2: 精算幕僚, 3: 星旅专员
        public int rarity;     // 1: 良才, 2: 精英, 3: 卓异
        public int level = 1;  // 1..5
        public int xp;
        public int assignKind; // 0: 待命, 1: 驻港, 2: 随航线
        public int assignLineId;
        public string assignStationKey = "";
        public int flawIndex;
        public bool flawCleansed;
        public string bio = "";
    }

    sealed class CompanyTechNode
    {
        public string key;
        public int eraGroup; // 0: 核热纪 (开放), 1: 聚变纪 (暂未开放), 2: 曲速纪 (暂未开放)
        public int year;
        public int branch; // 0: 组织人力, 1: 商业地产, 2: 资本金融, 3: 运营品牌, 4: 经营教训
        public string title;
        public string periodLabel;
        public string desc;
        public string effectSummary;
        public string effKind;
        public float effVal;
        public string resolvedBy; // 阵痛成本在对应合规节点掌握后解除
        public string[] parents;
    }

    readonly List<string> companyTechDone = new List<string>();
    readonly Dictionary<string, CompanyTechNode> coTechByKey = new Dictionary<string, CompanyTechNode>();
    readonly List<SavedTalent> talents = new List<SavedTalent>();
    readonly List<SavedTalent> talentPool = new List<SavedTalent>();
    int nextTalentId = 1;
    int lastTalentRefreshDay = -1;
    int techTreeTab = 0;       // 0: 工程技术史, 1: 企业治理树
    int companyBoardSubTab = 0; // 0: 核心理事会, 1: 才俊招募与培养

    static readonly string[] CoTechBranchNames = { "组织人力", "商业地产", "资本金融", "运营品牌", "经营教训" };
    static readonly Color[] CoTechBranchColors = {
        new Color(0.39f, 0.62f, 0.37f), // 组织人力（松绿）
        new Color(0.84f, 0.63f, 0.25f), // 商业地产（鎏金）
        new Color(0.30f, 0.49f, 0.72f), // 资本金融（靛蓝）
        new Color(0.82f, 0.41f, 0.35f), // 运营品牌（朱砂）
        new Color(0.55f, 0.34f, 0.28f)  // 经营教训（赭褐）
    };
    static readonly string[] TalentRoleNames = { "星港事务长", "王牌舰长", "精算幕僚", "星旅专员" };
    static readonly string[] TalentRarityNames = { "", "良才", "精英", "卓异" };
    static readonly string[] TalentFlawDescs = {
        "初任磨合：随航线或驻港日耗略增（维保 +5%，Lv.3或淬炼可除）",
        "排班保守：发车节奏偏稳（车速 -3%，Lv.3或淬炼可除）",
        "文书繁琐：开港筹备略慢（港点费 +4%，Lv.3或淬炼可除）",
        "慢热性格：需配合老线磨合（初期收益 -3%，Lv.3或淬炼可除）"
    };
    static readonly string[] TalentSurnames = { "沈", "顾", "许", "陆", "温", "唐", "谢", "苏", "江", "何", "潘", "叶", "宋", "钟" };
    static readonly string[] TalentGivenNames = { "明远", "清嘉", "怀瑾", "予安", "景行", "思齐", "知微", "承泽", "若水", "闻轩", "星桥", "舒雅", "致远", "青晏" };
    static readonly string[] TalentBios = {
        "曾在苍岚星岛飞车厂担任见习调度员，手写星航时刻表从不出错一分。",
        "自小在星港廊街老街长大，熟稔街坊商铺门道与早晚市客流规律。",
        "毕业于星湾统计研修班，喜欢带着算盘与钢笔巡检每一段磁轨。",
        "热衷拍摄星域风物明信片，总能把普通登船台办成旅途里的打卡地。",
        "出身三代星轨世家，箱底藏着一本泛黄的等离子飞车保养札记。",
        "擅长协调跨区口岸星港事务，再拥挤的换线接驳通道也能理出秩序。"
    };

    static readonly CompanyTechNode[] CompanyTechNodes = {
        // 历史阶段：1800—1829 · 矿轨契约与特许立社
        new CompanyTechNode {
            key = "co_charter_1801", eraGroup = 0, year = 1801, branch = 1,
            title = "月面矿道公约", periodLabel = "2046 年 · 公共收费星轨雏形",
            desc = "2046 年月面矿道公约获星穹理事会特许作为星域首条公共收费磁轨运输社运营，奠定“集资修路、按程收费”的商业起点。",
            effectSummary = "开港与商用筹备造价 -3%", parents = Array.Empty<string>()
        },
        new CompanyTechNode {
            key = "co_stock_1821", eraGroup = 0, year = 1821, branch = 2,
            title = "质量投射器配额特许案", periodLabel = "2055 年 · 静海—危海航线特许案",
            desc = "2055 年地月首条公共等离子星轨历经星穹理事会辩论，取得法定筑路征地特许状与首批质量投射器配额，确立近代星轨公司股权筹资法规。",
            effectSummary = "全网铺设磁轨造价 -4%", parents = new[] { "co_inst_1_07", "co_inst_2_13" }
        },
        new CompanyTechNode {
            key = "co_shares_1825", eraGroup = 0, year = 1825, branch = 0,
            title = "星际股权募资章程", periodLabel = "2058 年 · 公开招股与经理制",
            desc = "2058 年通航前夕星轨公司向公众发售股份并聘任专职总监管账，确立资合制星轨社的初代治理与人才框架。",
            effectSummary = "全网人才编制上限 +1", parents = new[] { "co_stock_1821", "co_inst_0_06" }
        },
        new CompanyTechNode {
            key = "co_booth_1826", eraGroup = 0, year = 1826, branch = 1,
            title = "初代登船台星票厅", periodLabel = "2058 年 · 初代星港票务厅",
            desc = "早期公共旅客星运在航线旁搭设兼办售票、寄存与小卖部的初代星港票务厅，让星港首次具备稳定商铺租金价值。",
            effectSummary = "新开星港与星港前商用设施造价 -4%", parents = new[] { "co_shares_1825" }
        },

        // 历史阶段：1830—1845 · 清算萌芽与标准时刻
        new CompanyTechNode {
            key = "co_fin_1", eraGroup = 0, year = 1830, branch = 2,
            title = "复式分账精算", periodLabel = "2061 年 · 第谷—哥白尼会计制",
            desc = "2061 年利曼星轨严格区隔工程资本支出与能耗、维护等日常营运账目，开创星轨精算成本核查传统。",
            effectSummary = "铺设磁轨造价再 -4%，航线与星舰日常维保 -4%", parents = new[] { "co_inst_4_10", "co_inst_2_18" }
        },
        new CompanyTechNode {
            key = "co_hr_1", eraGroup = 0, year = 1835, branch = 0,
            title = "星港事务班组规章", periodLabel = "2064 年 · 制服班组与轮值表",
            desc = "主干航线旅客星运兴盛后，各站配备着装整洁、照表交接的港长、信标调度员与行包班组，员工培养自成章法。",
            effectSummary = "人才训练经验 +4%，全网人才编制上限再 +1", parents = new[] { "co_fin_1" }
        },
        new CompanyTechNode {
            key = "co_re_1", eraGroup = 0, year = 1838, branch = 1,
            title = "星港平台书报茶座", periodLabel = "2066 年 · 星港特许商铺兴起",
            desc = "2061 年代末旅途报刊与茶点需求激增，书报商铺率先批量入驻大站星港平台，形成持续稳健的特许经营租金流。",
            effectSummary = "开港与星港前商业设施造价再 -4%，物业租金 +4%", parents = new[] { "co_fin_1" }
        },
        new CompanyTechNode {
            key = "co_ops_1", eraGroup = 0, year = 1840, branch = 3,
            title = "星轨统一标准时", periodLabel = "2067 年 · 静海星港标准时标准时",
            desc = "2067 年静海星港标准时率先废除沿线各镇地方日晷时间、全网统一按静海标准时运行，塑造准点金字口碑。",
            effectSummary = "全网飞车平均运营车速 +4%，每日科研产速 +4%", parents = new[] { "co_inst_3_04", "co_hr_1" }
        },
        new CompanyTechNode {
            key = "co_rch_1842", eraGroup = 0, year = 1842, branch = 3,
            title = "地月清算所", periodLabel = "2068 年 · 跨社联运清分机构",
            desc = "2068 年地月清算所（RCH）设立，各家星轨公司据此实行联程一票直达并按里程自动清分星票款。",
            effectSummary = "跨线换线接驳阻力降低 4%", parents = new[] { "co_ops_1", "co_fin_1", "co_booth_1826" }
        },
        new CompanyTechNode {
            key = "co_fare_act_1844", eraGroup = 0, year = 1844, branch = 3,
            title = "分级星票章程", periodLabel = "2069 年 · 星港平权协议",
            desc = "2069 年星穹协议规定各主干航线须每日开航每星程一信用点的平价三等星舱，促成大众平价出行时代与分级票价制度。",
            effectSummary = "全网基础客流需求 +4%，星票营收结算 +3%", parents = new[] { "co_rch_1842", "co_inst_3_09" }
        },
        new CompanyTechNode {
            key = "co_mania_1845", eraGroup = 0, year = 1845, branch = 4,
            title = "星轨狂热泡沫", periodLabel = "2070 年 · 历史经营教训",
            desc = "2070—2071 年间上百家空壳路社扎堆募资引爆投机股灾，虽短期拖高合规审计开销，却倒逼出严格公司债法制。",
            effectSummary = "历史教训：航线维保短期 +3%（债券与规范审计前置）", parents = new[] { "co_ops_1" }
        },

        // 历史阶段：1846—1864 · 主干航线整并与基建融资
        new CompanyTechNode {
            key = "co_fin_2", eraGroup = 0, year = 1849, branch = 2,
            title = "星轨债券与优先股", periodLabel = "2072 年 · 长期公司债发行制度",
            desc = "投机泡沫退潮后，规范发行固定息票的公司债券与优先股取代高息短拆，为长周期星轨建设提供低息长期本金。",
            effectSummary = "基建债券可贷本金 +4%，每期还本付息压力 -4%", parents = new[] { "co_mania_1845", "co_inst_2_33", "co_inst_2_08" }
        },
        new CompanyTechNode {
            key = "co_ops_3", eraGroup = 0, year = 1851, branch = 3,
            title = "太阳系星旅套票", periodLabel = "2074 年 · 星旅旅行社专航",
            desc = "2074 年静海环月博览会期间，旅行包租专航与风景区往返套票爆红，开创“星轨+文旅”爆款营销先河。",
            effectSummary = "含名胜星港的航线星票收入额外 +4%", parents = new[] { "co_inst_3_34", "co_fare_act_1844" }
        },
        new CompanyTechNode {
            key = "co_hotel_1854", eraGroup = 0, year = 1854, branch = 1,
            title = "轨道枢纽星港酒店", periodLabel = "2076 年 · 站城一体物业先驱",
            desc = "2076 年静海静海星港酒店直通终点站星港平台开业，集住宿、宴会与商铺于一体，使星轨社从买路修桥迈向星港前造城。",
            effectSummary = "星港前物业每日租金 +4%，名胜地标港点吸客 +4%", parents = new[] { "co_ops_3", "co_re_1" }
        },
        new CompanyTechNode {
            key = "co_ops_2", eraGroup = 0, year = 1858, branch = 3,
            title = "宏观星流推演联调", periodLabel = "2078 年 · 电报协同导视制",
            desc = "核热纪中叶量子信标线沿磁轨铺通，总部以宏观星流推演模型统合调度房与大站导视牌的实时报信，极大减少了乘客跨线换线接驳等待。",
            effectSummary = "跨线换线接驳惩罚再降 4%，全网星票营收结算 +4%", parents = new[] { "co_inst_0_36", "co_inst_2_28" }
        },
        new CompanyTechNode {
            key = "co_hr_3", eraGroup = 0, year = 1862, branch = 0,
            title = "星舰维护师徒传帮带", periodLabel = "2080 年 · 星舰工程师研修班与工坊",
            desc = "月背、静海等星轨重镇兴办研修班与工坊师徒制，老舰长与大聚变工程师言传身教，大幅摊薄人才进修耗费。",
            effectSummary = "人才进修经费 -4%，每日自然实训经验 +4%", parents = new[] { "co_ops_2", "co_inst_0_06" }
        },

        // 历史阶段：1865—1879 · 总部科层与星域初探
        new CompanyTechNode {
            key = "co_hr_2", eraGroup = 0, year = 1866, branch = 0,
            title = "航运理事会大厅", periodLabel = "2083 年 · 星港治理职能架构图",
            desc = "丹尼尔·星港治理绘制史上首张现代公司职能分部架构图，划分星轨工程、星舰维护、票务与督察理事常驻议事席。",
            effectSummary = "理事会常驻高管席位 +1，人才编制上限再 +1", parents = new[] { "co_inst_0_41", "co_inst_2_28" }
        },
        new CompanyTechNode {
            key = "co_pension_1868", eraGroup = 0, year = 1868, branch = 0,
            title = "员工互助抚恤金", periodLabel = "2084 年 · 星航船员保障基金",
            desc = "大型主干航线社自 2079 年代末普遍成立养老与伤残互助基金，骨干队伍高度稳定，日常实训事半功倍。",
            effectSummary = "人才进修经费再 -4%，人才训练经验再 +4%", parents = new[] { "co_hr_2", "co_inst_0_31" }
        },
        new CompanyTechNode {
            key = "co_waybill_1871", eraGroup = 0, year = 1871, branch = 2,
            title = "联运提单押汇", periodLabel = "2086 年 · 路行票据结算制度",
            desc = "星轨货物星运单据纳入银行贴现押汇体系，货主与商家可凭路单融资，星轨社流动资金周转效率成倍加快。",
            effectSummary = "航线与星舰日常维保费再 -4%", parents = new[] { "co_inst_2_43", "co_inst_2_08" }
        },
        new CompanyTechNode {
            key = "co_woosung_1876", eraGroup = 0, year = 1876, branch = 4,
            title = "星礁经营教训", periodLabel = "2089 年 · 星滨航运教训",
            desc = "2089 年星隅星礁窄幅磁轨未获妥善征地公文即仓促通航，终被管理局拆撤；警示星湾开港必先理顺星港契约。",
            effectSummary = "历史教训：开港费短期 +3%（特许会计师年审后解除）", parents = new[] { "co_inst_1_47", "co_inst_4_35" }
        },
        new CompanyTechNode {
            key = "co_fin_3", eraGroup = 0, year = 1878, branch = 2,
            title = "市政招标规范道", periodLabel = "2090 年 · 公私合营工程标书",
            desc = "汲取早期征地纠葛教训，确立明文航线权、分期工程赏金与履约验收条款的市政公开招标体系。",
            effectSummary = "市政中标工程赏金 +4%，同时可挂牌标书上限 +1", parents = new[] { "co_woosung_1876", "co_hr_2" }
        },

        // 历史阶段：1880—1889 · 星港廊街与独立年审
        new CompanyTechNode {
            key = "co_kaiping_1881", eraGroup = 0, year = 1881, branch = 3,
            title = "政企共建航运章程", periodLabel = "2092 年 · 矿区自营航线先声",
            desc = "2092 年月背矿业局自建矿区磁轨并首创商民自营商业运务，探索出兼顾督管与商股分红的本土章法。",
            effectSummary = "全网基础客流需求 +4%，每日科研点产速 +4%", parents = new[] { "co_fin_3" }
        },
        new CompanyTechNode {
            key = "co_re_2", eraGroup = 0, year = 1883, branch = 1,
            title = "星港廊街连廊商圈", periodLabel = "2093 年 · 南岸星港前铺面",
            desc = "核热纪八十年代三域埠岸兴建遮阳避雨的星港廊街商廊，把下车旅客自然引入茶室银庄与杂货行。",
            effectSummary = "所有已落成星港前物业的每日租金收益 +4%", parents = new[] { "co_inst_1_52", "co_kaiping_1881" }
        },
        new CompanyTechNode {
            key = "co_audit_1885", eraGroup = 0, year = 1885, branch = 2,
            title = "特许会计师年审", periodLabel = "2094 年 · 独立路账年审制度",
            desc = "大型星轨公司延请外部特许会计师审计轨料折旧与客货星票款，大幅增进银行信用并压缩筑路虚耗。",
            effectSummary = "铺设磁轨造价再 -4%，基建债券每期还款压力再 -4%", parents = new[] { "co_inst_4_50", "co_inst_2_53", "co_fin_3" }
        },
        new CompanyTechNode {
            key = "co_rateact_1887", eraGroup = 0, year = 1887, branch = 3,
            title = "跨埠公平运价约", periodLabel = "2096 年 · 公开透明运价条例",
            desc = "参照 2096 年公平运价协议禁止暗箱歧视加价与恶性割价竞争，稳定了长途与跨埠主干航线的合理利润率。",
            effectSummary = "全网星票营收结算 +4%", parents = new[] { "co_audit_1885" }
        },
        new CompanyTechNode {
            key = "co_re_3", eraGroup = 0, year = 1888, branch = 1,
            title = "月脊观光缆车特许", periodLabel = "2096 年 · 月之亮面山顶缆车通航",
            desc = "2096 年月之亮面山顶缆车通航，缔造观光交通与半山地产共荣典范，名胜星港由此成为全网客流高地。",
            effectSummary = "名胜地标港点基础吸引客流再 +4%", parents = new[] { "co_rateact_1887", "co_inst_0_56" }
        },

        // 历史阶段：1890—1899 · 星湾口岸与现代集团
        new CompanyTechNode {
            key = "co_hr_4", eraGroup = 0, year = 1891, branch = 0,
            title = "资深理事礼遇与委任状", periodLabel = "2098 年 · 资深理事授衔章程",
            desc = "纪末大型星轨集团向历任高管颁授礼遇与独立权责委任状，大幅化解部门本位掣肘与磨合内耗。",
            effectSummary = "理事会高管负面特质削减 4%，常驻理事会席位再 +1", parents = new[] { "co_inst_0_56", "co_pension_1868" }
        },
        new CompanyTechNode {
            key = "co_ops_4", eraGroup = 0, year = 1893, branch = 3,
            title = "星域旅行手账刊", periodLabel = "2099 年 · 版画沿线风物指南",
            desc = "随舰附赠精致的星港风光版画手册与名胜盖章本，使星轨品牌深入人心，屡夺年度行业评比桂冠。",
            effectSummary = "全网基础客流需求再 +4%，年度星轨大赏赏金 +4%", parents = new[] { "co_inst_3_59", "co_inst_2_63" }
        },
        new CompanyTechNode {
            key = "co_re_4", eraGroup = 0, year = 1898, branch = 1,
            title = "跨湾枢纽综合体", periodLabel = "2102 年 · 跨湾主干航线草约与总站规划",
            desc = "2102 年火星至月之亮面主干航线勘线草约签订，规划星港大枢纽与周边货栈商业街，打造跨城站城一体化枢纽。",
            effectSummary = "星港前物业总租金再 +4%，全网星港吞吐上限 +4%", parents = new[] { "co_inst_0_70", "co_inst_3_64", "co_inst_4_73" }
        },
        new CompanyTechNode {
            key = "co_fin_4", eraGroup = 0, year = 1899, branch = 2,
            title = "跨境银团授信", periodLabel = "2103 年 · 核热纪末银团联贷协议",
            desc = "纪元之交多家星湾星际银行结成跨星基础设施银团授信，以极低长期成本护航三域星轨总部跨入新纪元。",
            effectSummary = "全网星票营收结算再 +4%，航线与星车维保费再 -4%", parents = new[] { "co_inst_2_68", "co_re_4", "co_inst_4_75" }
        },

        // 新增 75 项：年代为游戏中的制度推演定位，并非全部制度的首创年份。
        new CompanyTechNode { key = "co_inst_0_01", eraGroup = 0, year = 1802, branch = 0, title = "雇员名册", periodLabel = "2046 年 · 核热纪制度推演", desc = "统一登记工种与资历，减少班组交接遗漏。以书面规程和定期核验落实到日常经营。", effectSummary = "人才训练经验 +1%", effKind = "TrainXp", effVal = 0.01f, parents = new[] { "co_charter_1801" } },
        new CompanyTechNode { key = "co_inst_1_02", eraGroup = 0, year = 1803, branch = 1, title = "星港前货栈", periodLabel = "2047 年 · 核热纪制度推演", desc = "按批次出租货位，让零担集散形成租金。以书面规程和定期核验落实到日常经营。", effectSummary = "物业租金 +2%", effKind = "Commercial", effVal = 0.02f, parents = new[] { "co_charter_1801" } },
        new CompanyTechNode { key = "co_inst_2_03", eraGroup = 0, year = 1804, branch = 2, title = "汇票结算", periodLabel = "2045 年 · 核热纪制度推演", desc = "用汇票核销沿线账款，减少星票款损耗。以书面规程和定期核验落实到日常经营。", effectSummary = "星票营收 +3%", effKind = "Revenue", effVal = 0.03f, parents = new[] { "co_charter_1801" } },
        new CompanyTechNode { key = "co_inst_3_04", eraGroup = 0, year = 1826, branch = 3, title = "行航星航时刻表册", periodLabel = "2058 年 · 核热纪制度推演", desc = "向市集与旅店分发班次和接驳资料。以书面规程和定期核验落实到日常经营。", effectSummary = "基础客流 +4%", effKind = "Demand", effVal = 0.04f, parents = new[] { "co_shares_1825" } },
        new CompanyTechNode { key = "co_inst_4_05", eraGroup = 0, year = 1822, branch = 4, title = "盲目圈地", periodLabel = "2056 年 · 核热纪制度推演", desc = "未核实需求便囤积星港前用地。将教训列为后续合规制度的必修前置。", effectSummary = "历史教训：开港造价短期 +2%（星港前铺租契掌握后解除）", effKind = "OpenCost", effVal = 0.02f, resolvedBy = "co_inst_1_22", parents = new[] { "co_stock_1821" } },
        new CompanyTechNode { key = "co_inst_0_06", eraGroup = 0, year = 1815, branch = 0, title = "星港事务培训", periodLabel = "2052 年 · 核热纪制度推演", desc = "用岗位手册讲授售票、行包与星港平台引导。以书面规程和定期核验落实到日常经营。", effectSummary = "人才训练经验 +2%", effKind = "TrainXp", effVal = 0.02f, parents = new[] { "co_inst_0_01" } },
        new CompanyTechNode { key = "co_inst_1_07", eraGroup = 0, year = 1810, branch = 1, title = "头等候航室", periodLabel = "2049 年 · 核热纪制度推演", desc = "分区经营候航与茶点服务。以书面规程和定期核验落实到日常经营。", effectSummary = "物业租金 +3%", effKind = "Commercial", effVal = 0.03f, parents = new[] { "co_inst_1_02" } },
        new CompanyTechNode { key = "co_inst_2_08", eraGroup = 0, year = 1832, branch = 2, title = "贴现银行", periodLabel = "2062 年 · 核热纪制度推演", desc = "凭到期票据贴现，降低融资周转负担。以书面规程和定期核验落实到日常经营。", effectSummary = "债券偿付压力 -4%", effKind = "BondDue", effVal = -0.04f, parents = new[] { "co_fin_1" } },
        new CompanyTechNode { key = "co_inst_3_09", eraGroup = 0, year = 1842, branch = 3, title = "磁码星票", periodLabel = "2068 年 · 核热纪制度推演", desc = "以预印编号磁码星票核对售票与收入。以书面规程和定期核验落实到日常经营。", effectSummary = "星票营收 +1%", effKind = "Revenue", effVal = 0.01f, parents = new[] { "co_rch_1842" } },
        new CompanyTechNode { key = "co_inst_4_10", eraGroup = 0, year = 1828, branch = 4, title = "账册混同", periodLabel = "2060 年 · 核热纪制度推演", desc = "把资本支出混入日常账目。将教训列为后续合规制度的必修前置。", effectSummary = "历史教训：航线与星舰维保费短期 +3%（复式分账精算掌握后解除）", effKind = "Maint", effVal = 0.03f, resolvedBy = "co_fin_1", parents = new[] { "co_shares_1825" } },
        new CompanyTechNode { key = "co_inst_0_11", eraGroup = 0, year = 1837, branch = 0, title = "轮班条例", periodLabel = "2065 年 · 核热纪制度推演", desc = "按工时交接班，减少疲劳作业返修。以书面规程和定期核验落实到日常经营。", effectSummary = "航线与星舰维保费 -3%", effKind = "Maint", effVal = -0.03f, parents = new[] { "co_hr_1", "co_inst_1_07" } },
        new CompanyTechNode { key = "co_inst_1_12", eraGroup = 0, year = 1838, branch = 1, title = "星港书报亭", periodLabel = "2066 年 · 核热纪制度推演", desc = "以特许租约经营沿线报刊摊位。以书面规程和定期核验落实到日常经营。", effectSummary = "物业租金 +4%", effKind = "Commercial", effVal = 0.04f, parents = new[] { "co_re_1", "co_inst_2_23" } },
        new CompanyTechNode { key = "co_inst_2_13", eraGroup = 0, year = 1817, branch = 2, title = "工程预算册", periodLabel = "2053 年 · 核热纪制度推演", desc = "逐项核算磁轨基座与材料预算。以书面规程和定期核验落实到日常经营。", effectSummary = "铺设磁轨造价 -1%", effKind = "TrackBuild", effVal = -0.01f, parents = new[] { "co_inst_2_03" } },
        new CompanyTechNode { key = "co_inst_3_14", eraGroup = 0, year = 1843, branch = 3, title = "星旅旅行社团体游", periodLabel = "2069 年 · 核热纪制度推演", desc = "组织团体订座与统一接待。以书面规程和定期核验落实到日常经营。", effectSummary = "名胜航线票收 +2%", effKind = "Tourism", effVal = 0.02f, parents = new[] { "co_rch_1842" } },
        new CompanyTechNode { key = "co_inst_4_15", eraGroup = 0, year = 1846, branch = 4, title = "削价恶性竞争", periodLabel = "2071 年 · 核热纪制度推演", desc = "以低于成本的运价争抢客源。将教训列为后续合规制度的必修前置。", effectSummary = "历史教训：航线与星舰维保费短期 +2%（运价公示审计掌握后解除）", effKind = "Maint", effVal = 0.02f, resolvedBy = "co_inst_2_63", parents = new[] { "co_fare_act_1844", "co_inst_1_27" } },
        new CompanyTechNode { key = "co_inst_0_16", eraGroup = 0, year = 1839, branch = 0, title = "宿舍补贴", periodLabel = "2066 年 · 核热纪制度推演", desc = "为异地雇员安排宿舍，降低集中培训支出。以书面规程和定期核验落实到日常经营。", effectSummary = "人才进修经费 -4%", effKind = "TrainCost", effVal = -0.04f, parents = new[] { "co_inst_0_11", "co_inst_1_22" } },
        new CompanyTechNode { key = "co_inst_1_17", eraGroup = 0, year = 1845, branch = 1, title = "地面接驳车接驳院", periodLabel = "2070 年 · 核热纪制度推演", desc = "统一星港前地面接驳车候客和行包装卸位置。以书面规程和定期核验落实到日常经营。", effectSummary = "跨线换线接驳阻力 -1%", effKind = "Transfer", effVal = -0.01f, parents = new[] { "co_inst_1_32", "co_inst_1_22" } },
        new CompanyTechNode { key = "co_inst_2_18", eraGroup = 0, year = 1823, branch = 2, title = "股东查账权", periodLabel = "2057 年 · 核热纪制度推演", desc = "开放账册核对，约束虚列运营开销。以书面规程和定期核验落实到日常经营。", effectSummary = "航线与星舰维保费 -2%", effKind = "Maint", effVal = -0.02f, parents = new[] { "co_stock_1821" } },
        new CompanyTechNode { key = "co_inst_3_19", eraGroup = 0, year = 1841, branch = 3, title = "邮政专航", periodLabel = "2068 年 · 核热纪制度推演", desc = "按契约承运邮袋，稳定班次收入。以书面规程和定期核验落实到日常经营。", effectSummary = "星票营收 +3%", effKind = "Revenue", effVal = 0.03f, parents = new[] { "co_ops_1" } },
        new CompanyTechNode { key = "co_inst_4_20", eraGroup = 0, year = 1846, branch = 4, title = "外包工程违约", periodLabel = "2071 年 · 核热纪制度推演", desc = "承包商收款后延误或偷减材料。将教训列为后续合规制度的必修前置。", effectSummary = "历史教训：铺设磁轨造价短期 +3%（担保责任书掌握后解除）", effKind = "TrackBuild", effVal = 0.03f, resolvedBy = "co_inst_2_33", parents = new[] { "co_inst_2_13", "co_inst_1_17" } },
        new CompanyTechNode { key = "co_inst_0_21", eraGroup = 0, year = 1836, branch = 0, title = "舰长考核", periodLabel = "2065 年 · 核热纪制度推演", desc = "用行航记录考核舰长，改善发车衔接。以书面规程和定期核验落实到日常经营。", effectSummary = "运营车速 +1%", effKind = "Speed", effVal = 0.01f, parents = new[] { "co_hr_1", "co_inst_3_04" } },
        new CompanyTechNode { key = "co_inst_1_22", eraGroup = 0, year = 1829, branch = 1, title = "星港前铺租契", periodLabel = "2060 年 · 核热纪制度推演", desc = "明确商铺租约和修缮责任，减少筹备支出。以书面规程和定期核验落实到日常经营。", effectSummary = "开港造价 -2%", effKind = "OpenCost", effVal = -0.02f, parents = new[] { "co_inst_4_05", "co_booth_1826" } },
        new CompanyTechNode { key = "co_inst_2_23", eraGroup = 0, year = 1827, branch = 2, title = "资本实缴册", periodLabel = "2059 年 · 核热纪制度推演", desc = "核实建港资金到账后再分期承包。以书面规程和定期核验落实到日常经营。", effectSummary = "开港造价 -3%", effKind = "OpenCost", effVal = -0.03f, parents = new[] { "co_shares_1825" } },
        new CompanyTechNode { key = "co_inst_3_24", eraGroup = 0, year = 1847, branch = 3, title = "失物招领章", periodLabel = "2071 年 · 核热纪制度推演", desc = "建立失物登记和认领手续。以书面规程和定期核验落实到日常经营。", effectSummary = "基础客流 +4%", effKind = "Demand", effVal = 0.04f, parents = new[] { "co_inst_0_21", "co_inst_1_12" } },
        new CompanyTechNode { key = "co_inst_4_25", eraGroup = 0, year = 1855, branch = 4, title = "虚报客流丑闻", periodLabel = "2076 年 · 核热纪制度推演", desc = "夸大客流误导股东扩建。将教训列为后续合规制度的必修前置。", effectSummary = "历史教训：航线与星舰维保费短期 +2%（独立监事掌握后解除）", effKind = "Maint", effVal = 0.02f, resolvedBy = "co_inst_2_28", parents = new[] { "co_hotel_1854", "co_inst_3_34" } },
        new CompanyTechNode { key = "co_inst_0_26", eraGroup = 0, year = 1847, branch = 0, title = "双语星港事务", periodLabel = "2071 年 · 核热纪制度推演", desc = "以双语告示疏导跨区旅客。以书面规程和定期核验落实到日常经营。", effectSummary = "跨线换线接驳阻力 -2%", effKind = "Transfer", effVal = -0.02f, parents = new[] { "co_inst_1_17", "co_inst_3_19" } },
        new CompanyTechNode { key = "co_inst_1_27", eraGroup = 0, year = 1844, branch = 1, title = "星港平台广告牌", periodLabel = "2069 年 · 核热纪制度推演", desc = "统一出租广告位置，形成固定租金。以书面规程和定期核验落实到日常经营。", effectSummary = "物业租金 +3%", effKind = "Commercial", effVal = 0.03f, parents = new[] { "co_inst_1_12", "co_inst_0_21" } },
        new CompanyTechNode { key = "co_inst_2_28", eraGroup = 0, year = 1856, branch = 2, title = "独立监事", periodLabel = "2077 年 · 核热纪制度推演", desc = "由监事复核支出与关联交易。以书面规程和定期核验落实到日常经营。", effectSummary = "航线与星舰维保费 -4%", effKind = "Maint", effVal = -0.04f, parents = new[] { "co_inst_4_25", "co_inst_2_18" } },
        new CompanyTechNode { key = "co_inst_3_29", eraGroup = 0, year = 1850, branch = 3, title = "行包联运票", periodLabel = "2073 年 · 核热纪制度推演", desc = "凭同一行包票办理换线接续。以书面规程和定期核验落实到日常经营。", effectSummary = "跨线换线接驳阻力 -1%", effKind = "Transfer", effVal = -0.01f, parents = new[] { "co_inst_3_09", "co_inst_0_26" } },
        new CompanyTechNode { key = "co_inst_4_30", eraGroup = 0, year = 1856, branch = 4, title = "关联采购回扣", periodLabel = "2077 年 · 核热纪制度推演", desc = "关联供应商通过回扣抬价。将教训列为后续合规制度的必修前置。", effectSummary = "历史教训：铺设磁轨造价短期 +3%（关联交易披露掌握后解除）", effKind = "TrackBuild", effVal = 0.03f, resolvedBy = "co_inst_2_58", parents = new[] { "co_inst_1_37", "co_inst_2_38" } },
        new CompanyTechNode { key = "co_inst_0_31", eraGroup = 0, year = 1850, branch = 0, title = "工伤抚恤", periodLabel = "2073 年 · 核热纪制度推演", desc = "建立工伤申报和互助程序，稳定星舰维护队伍。以书面规程和定期核验落实到日常经营。", effectSummary = "航线与星舰维保费 -3%", effKind = "Maint", effVal = -0.03f, parents = new[] { "co_inst_0_11", "co_inst_3_24" } },
        new CompanyTechNode { key = "co_inst_1_32", eraGroup = 0, year = 1844, branch = 1, title = "星轨旅馆", periodLabel = "2069 年 · 核热纪制度推演", desc = "把住宿与沿线游览接驳安排在一起。以书面规程和定期核验落实到日常经营。", effectSummary = "名胜吸客 +4%", effKind = "SceneryDemand", effVal = 0.04f, parents = new[] { "co_rch_1842" } },
        new CompanyTechNode { key = "co_inst_2_33", eraGroup = 0, year = 1848, branch = 2, title = "担保责任书", periodLabel = "2072 年 · 核热纪制度推演", desc = "明确承包商履约责任，减少返工。以书面规程和定期核验落实到日常经营。", effectSummary = "铺设磁轨造价 -1%", effKind = "TrackBuild", effVal = -0.01f, parents = new[] { "co_inst_4_20", "co_inst_2_23" } },
        new CompanyTechNode { key = "co_inst_3_34", eraGroup = 0, year = 1847, branch = 3, title = "观光套票", periodLabel = "2071 年 · 核热纪制度推演", desc = "把往返星票和沿线游览合并售卖。以书面规程和定期核验落实到日常经营。", effectSummary = "名胜航线票收 +2%", effKind = "Tourism", effVal = 0.02f, parents = new[] { "co_inst_3_14", "co_inst_1_32" } },
        new CompanyTechNode { key = "co_inst_4_35", eraGroup = 0, year = 1852, branch = 4, title = "征地契约纠纷", periodLabel = "2074 年 · 核热纪制度推演", desc = "口头约定取代正式土地契约。将教训列为后续合规制度的必修前置。", effectSummary = "历史教训：开港造价短期 +2%（星港前市集约掌握后解除）", effKind = "OpenCost", effVal = 0.02f, resolvedBy = "co_inst_1_62", parents = new[] { "co_fin_2" } },
        new CompanyTechNode { key = "co_inst_0_36", eraGroup = 0, year = 1851, branch = 0, title = "调度总局", periodLabel = "2074 年 · 核热纪制度推演", desc = "总局汇总沿线舰报，统一编排交会。以书面规程和定期核验落实到日常经营。", effectSummary = "运营车速 +4%", effKind = "Speed", effVal = 0.04f, parents = new[] { "co_ops_1" } },
        new CompanyTechNode { key = "co_inst_1_37", eraGroup = 0, year = 1853, branch = 1, title = "能质补给契", periodLabel = "2075 年 · 核热纪制度推演", desc = "以定期供给合同稳定能质采购。以书面规程和定期核验落实到日常经营。", effectSummary = "航线与星舰维保费 -1%", effKind = "Maint", effVal = -0.01f, parents = new[] { "co_inst_0_36", "co_inst_2_33" } },
        new CompanyTechNode { key = "co_inst_2_38", eraGroup = 0, year = 1855, branch = 2, title = "设备抵押信托", periodLabel = "2076 年 · 核热纪制度推演", desc = "以星舰设备收益保障长期借款。以书面规程和定期核验落实到日常经营。", effectSummary = "债券可贷本金 +2%", effKind = "BondCap", effVal = 0.02f, parents = new[] { "co_fin_2" } },
        new CompanyTechNode { key = "co_inst_3_39", eraGroup = 0, year = 1857, branch = 3, title = "星舱餐饮服务", periodLabel = "2077 年 · 核热纪制度推演", desc = "为长途旅客提供随舰餐饮。以书面规程和定期核验落实到日常经营。", effectSummary = "星票营收 +3%", effKind = "Revenue", effVal = 0.03f, parents = new[] { "co_hotel_1854", "co_inst_3_29" } },
        new CompanyTechNode { key = "co_inst_4_40", eraGroup = 0, year = 1859, branch = 4, title = "过度举债危机", periodLabel = "2079 年 · 核热纪制度推演", desc = "用短债滚动填补长期建设缺口。将教训列为后续合规制度的必修前置。", effectSummary = "历史教训：债券偿付压力短期 +3%（债务偿付准备掌握后解除）", effKind = "BondDue", effVal = 0.03f, resolvedBy = "co_inst_2_53", parents = new[] { "co_inst_2_38", "co_inst_3_39" } },
        new CompanyTechNode { key = "co_inst_0_41", eraGroup = 0, year = 1861, branch = 0, title = "工会章程", periodLabel = "2080 年 · 核热纪制度推演", desc = "以书面章程协商工时与学徒培养。以书面规程和定期核验落实到日常经营。", effectSummary = "人才训练经验 +1%", effKind = "TrainXp", effVal = 0.01f, parents = new[] { "co_inst_0_31", "co_inst_0_16" } },
        new CompanyTechNode { key = "co_inst_1_42", eraGroup = 0, year = 1863, branch = 1, title = "冷链仓库", periodLabel = "2081 年 · 核热纪制度推演", desc = "出租冰藏货位并规定周转时限。以书面规程和定期核验落实到日常经营。", effectSummary = "物业租金 +2%", effKind = "Commercial", effVal = 0.02f, parents = new[] { "co_ops_2" } },
        new CompanyTechNode { key = "co_inst_2_43", eraGroup = 0, year = 1865, branch = 2, title = "保险联保", periodLabel = "2082 年 · 核热纪制度推演", desc = "按事故风险共同投保并核查保费。以书面规程和定期核验落实到日常经营。", effectSummary = "航线与星舰维保费 -3%", effKind = "Maint", effVal = -0.03f, parents = new[] { "co_ops_2" } },
        new CompanyTechNode { key = "co_inst_3_44", eraGroup = 0, year = 1867, branch = 3, title = "卧铺服务", periodLabel = "2083 年 · 核热纪制度推演", desc = "用卧铺改善长途夜间旅行体验。以书面规程和定期核验落实到日常经营。", effectSummary = "基础客流 +4%", effKind = "Demand", effVal = 0.04f, parents = new[] { "co_inst_3_39", "co_inst_0_46" } },
        new CompanyTechNode { key = "co_inst_4_45", eraGroup = 0, year = 1869, branch = 4, title = "星票款挪用案", periodLabel = "2085 年 · 核热纪制度推演", desc = "代理星票厅挪用尚未清分的星票款。将教训列为后续合规制度的必修前置。", effectSummary = "历史教训：航线与星舰维保费短期 +2%（分局稽核制掌握后解除）", effKind = "Maint", effVal = 0.02f, resolvedBy = "co_inst_0_70", parents = new[] { "co_inst_3_29", "co_inst_3_44" } },
        new CompanyTechNode { key = "co_inst_0_46", eraGroup = 0, year = 1864, branch = 0, title = "星轨工程巡检册", periodLabel = "2082 年 · 核热纪制度推演", desc = "逐段登记巡检责任，减少漏检。以书面规程和定期核验落实到日常经营。", effectSummary = "航线与星舰维保费 -2%", effKind = "Maint", effVal = -0.02f, parents = new[] { "co_hr_3", "co_inst_1_37" } },
        new CompanyTechNode { key = "co_inst_1_47", eraGroup = 0, year = 1872, branch = 1, title = "保税货场", periodLabel = "2087 年 · 核热纪制度推演", desc = "分区保管待验货物，提供仓储租赁。以书面规程和定期核验落实到日常经营。", effectSummary = "物业租金 +3%", effKind = "Commercial", effVal = 0.03f, parents = new[] { "co_waybill_1871", "co_inst_1_42" } },
        new CompanyTechNode { key = "co_inst_2_48", eraGroup = 0, year = 1873, branch = 2, title = "跨币结算", periodLabel = "2087 年 · 核热纪制度推演", desc = "统一异地银钱折算与清分口径。以书面规程和定期核验落实到日常经营。", effectSummary = "星票营收 +4%", effKind = "Revenue", effVal = 0.04f, parents = new[] { "co_waybill_1871", "co_inst_0_26" } },
        new CompanyTechNode { key = "co_inst_3_49", eraGroup = 0, year = 1875, branch = 3, title = "旅客赔付章", periodLabel = "2088 年 · 核热纪制度推演", desc = "公示误点与行包赔付手续。以书面规程和定期核验落实到日常经营。", effectSummary = "基础客流 +1%", effKind = "Demand", effVal = 0.01f, parents = new[] { "co_inst_3_44", "co_inst_2_43" } },
        new CompanyTechNode { key = "co_inst_4_50", eraGroup = 0, year = 1876, branch = 4, title = "燃料囤积失算", periodLabel = "2089 年 · 核热纪制度推演", desc = "过量采购燃料导致仓储与损耗增加。将教训列为后续合规制度的必修前置。", effectSummary = "历史教训：航线与星舰维保费短期 +3%（特许会计师年审掌握后解除）", effKind = "Maint", effVal = 0.03f, resolvedBy = "co_audit_1885", parents = new[] { "co_inst_1_42", "co_inst_0_46" } },
        new CompanyTechNode { key = "co_inst_0_51", eraGroup = 0, year = 1878, branch = 0, title = "职员申诉簿", periodLabel = "2090 年 · 核热纪制度推演", desc = "设立申诉与复核手续，减少人员流失。以书面规程和定期核验落实到日常经营。", effectSummary = "人才进修经费 -3%", effKind = "TrainCost", effVal = -0.03f, parents = new[] { "co_pension_1868", "co_inst_3_49" } },
        new CompanyTechNode { key = "co_inst_1_52", eraGroup = 0, year = 1880, branch = 1, title = "星港联运仓", periodLabel = "2091 年 · 核热纪制度推演", desc = "将星港泊位与星轨货位接续，缩短换装等待。以书面规程和定期核验落实到日常经营。", effectSummary = "跨线换线接驳阻力 -4%", effKind = "Transfer", effVal = -0.04f, parents = new[] { "co_fin_3" } },
        new CompanyTechNode { key = "co_inst_2_53", eraGroup = 0, year = 1881, branch = 2, title = "债务偿付准备", periodLabel = "2092 年 · 核热纪制度推演", desc = "按期留存偿债资金，降低续借压力。以书面规程和定期核验落实到日常经营。", effectSummary = "债券偿付压力 -1%", effKind = "BondDue", effVal = -0.01f, parents = new[] { "co_inst_4_40", "co_inst_2_48" } },
        new CompanyTechNode { key = "co_inst_3_54", eraGroup = 0, year = 1883, branch = 3, title = "夜班特快品牌", periodLabel = "2093 年 · 核热纪制度推演", desc = "以稳定夜间快车班次建立品牌。以书面规程和定期核验落实到日常经营。", effectSummary = "运营车速 +2%", effKind = "Speed", effVal = 0.02f, parents = new[] { "co_re_2", "co_inst_3_49" } },
        new CompanyTechNode { key = "co_inst_4_55", eraGroup = 0, year = 1884, branch = 4, title = "仓栈火险", periodLabel = "2094 年 · 核热纪制度推演", desc = "仓栈防火责任和通道长期缺位。将教训列为后续合规制度的必修前置。", effectSummary = "历史教训：航线与星舰维保费短期 +2%（消防共保约掌握后解除）", effKind = "Maint", effVal = 0.02f, resolvedBy = "co_inst_1_67", parents = new[] { "co_re_2", "co_inst_1_47" } },
        new CompanyTechNode { key = "co_inst_0_56", eraGroup = 0, year = 1886, branch = 0, title = "岗位晋级制", periodLabel = "2095 年 · 核热纪制度推演", desc = "用实操成绩安排晋级，鼓励持续学习。以书面规程和定期核验落实到日常经营。", effectSummary = "人才训练经验 +4%", effKind = "TrainXp", effVal = 0.04f, parents = new[] { "co_inst_0_51", "co_hr_3" } },
        new CompanyTechNode { key = "co_inst_1_57", eraGroup = 0, year = 1887, branch = 1, title = "行包寄存所", periodLabel = "2096 年 · 核热纪制度推演", desc = "分离寄存和进出站动线，释放站厅容量。以书面规程和定期核验落实到日常经营。", effectSummary = "星港吞吐上限 +1%", effKind = "StationCap", effVal = 0.01f, parents = new[] { "co_audit_1885" } },
        new CompanyTechNode { key = "co_inst_2_58", eraGroup = 0, year = 1889, branch = 2, title = "关联交易披露", periodLabel = "2097 年 · 核热纪制度推演", desc = "公开材料采购关系，防止抬价承包。以书面规程和定期核验落实到日常经营。", effectSummary = "铺设磁轨造价 -2%", effKind = "TrackBuild", effVal = -0.02f, parents = new[] { "co_inst_4_30", "co_audit_1885" } },
        new CompanyTechNode { key = "co_inst_3_59", eraGroup = 0, year = 1890, branch = 3, title = "沿线旅行指南", periodLabel = "2098 年 · 核热纪制度推演", desc = "介绍地方墟市与名胜接驳。以书面规程和定期核验落实到日常经营。", effectSummary = "名胜吸客 +3%", effKind = "SceneryDemand", effVal = 0.03f, parents = new[] { "co_re_3", "co_inst_1_57" } },
        new CompanyTechNode { key = "co_inst_4_60", eraGroup = 0, year = 1891, branch = 4, title = "跨币汇兑亏损", periodLabel = "2098 年 · 核热纪制度推演", desc = "收入和债务币种错配引发损失。将教训列为后续合规制度的必修前置。", effectSummary = "历史教训：债券偿付压力短期 +3%（风险准备金掌握后解除）", effKind = "BondDue", effVal = 0.03f, resolvedBy = "co_inst_2_68", parents = new[] { "co_inst_2_48", "co_inst_1_52" } },
        new CompanyTechNode { key = "co_inst_0_61", eraGroup = 0, year = 1891, branch = 0, title = "研修班奖学金", periodLabel = "2098 年 · 核热纪制度推演", desc = "补贴职员研修班，减轻进修开销。以书面规程和定期核验落实到日常经营。", effectSummary = "人才进修经费 -1%", effKind = "TrainCost", effVal = -0.01f, parents = new[] { "co_hr_4", "co_inst_2_58" } },
        new CompanyTechNode { key = "co_inst_1_62", eraGroup = 0, year = 1892, branch = 1, title = "星港前市集约", periodLabel = "2099 年 · 核热纪制度推演", desc = "协商市集用地与道路边界，降低开港成本。以书面规程和定期核验落实到日常经营。", effectSummary = "开港造价 -2%", effKind = "OpenCost", effVal = -0.02f, parents = new[] { "co_inst_4_35", "co_re_3" } },
        new CompanyTechNode { key = "co_inst_2_63", eraGroup = 0, year = 1892, branch = 2, title = "运价公示审计", periodLabel = "2099 年 · 核热纪制度推演", desc = "核验收费与公示一致，减少结算争议。以书面规程和定期核验落实到日常经营。", effectSummary = "星票营收 +3%", effKind = "Revenue", effVal = 0.03f, parents = new[] { "co_inst_4_15", "co_rateact_1887" } },
        new CompanyTechNode { key = "co_inst_3_64", eraGroup = 0, year = 1893, branch = 3, title = "服务质量稽查", periodLabel = "2099 年 · 核热纪制度推演", desc = "按投诉和服务记录改善品牌信誉。以书面规程和定期核验落实到日常经营。", effectSummary = "年度大赏赏金 +4%", effKind = "Award", effVal = 0.04f, parents = new[] { "co_ops_4", "co_inst_0_61" } },
        new CompanyTechNode { key = "co_inst_4_65", eraGroup = 0, year = 1893, branch = 4, title = "支线星轨航线重复投资", periodLabel = "2099 年 · 核热纪制度推演", desc = "邻近支线星轨航线争夺同一小片货源。将教训列为后续合规制度的必修前置。", effectSummary = "历史教训：铺设磁轨造价短期 +2%（分局稽核制掌握后解除）", effKind = "TrackBuild", effVal = 0.02f, resolvedBy = "co_inst_0_70", parents = new[] { "co_inst_3_54", "co_inst_1_62" } },
        new CompanyTechNode { key = "co_inst_0_66", eraGroup = 0, year = 1894, branch = 0, title = "自律星舰安全三准则", periodLabel = "2100 年 · 核热纪制度推演", desc = "重要岗位须熟记『自律星舰安全三准则』并经考核持证值勤，把风险挡在登船台之外。以书面规程和定期核验落实到日常经营。", effectSummary = "航线与星舰维保费 -2%", effKind = "Maint", effVal = -0.02f, parents = new[] { "co_hr_4", "co_inst_0_51" } },
        new CompanyTechNode { key = "co_inst_1_67", eraGroup = 0, year = 1894, branch = 1, title = "消防共保约", periodLabel = "2100 年 · 核热纪制度推演", desc = "约定仓栈防火检查与共担修缮。以书面规程和定期核验落实到日常经营。", effectSummary = "航线与星舰维保费 -3%", effKind = "Maint", effVal = -0.03f, parents = new[] { "co_inst_4_55", "co_inst_0_66" } },
        new CompanyTechNode { key = "co_inst_2_68", eraGroup = 0, year = 1899, branch = 2, title = "深空守望冗余协议", periodLabel = "2103 年 · 核热纪制度推演", desc = "为汇兑、保险、租约与债务风险分类提存准备，并以深空守望冗余协议逐项审核使用——星海不信侥幸，只信备份。以书面规程和定期核验落实到日常经营。", effectSummary = "航线与星舰维保费 -4%", effKind = "Maint", effVal = -0.04f, parents = new[] { "co_inst_4_60", "co_inst_4_72", "co_inst_4_73", "co_inst_4_74" } },
        new CompanyTechNode { key = "co_inst_4_69", eraGroup = 0, year = 1895, branch = 4, title = "廉价工料返修", periodLabel = "2101 年 · 核热纪制度推演", desc = "压低工料标准造成重复修缮。将教训列为后续合规制度的必修前置。", effectSummary = "历史教训：铺设磁轨造价短期 +2%（分局稽核制掌握后解除）", effKind = "TrackBuild", effVal = 0.02f, resolvedBy = "co_inst_0_70", parents = new[] { "co_inst_0_46", "co_inst_2_58" } },
        new CompanyTechNode { key = "co_inst_0_70", eraGroup = 0, year = 1897, branch = 0, title = "分局稽核制", periodLabel = "2102 年 · 核热纪制度推演", desc = "分局定期上报营运数据供总局研究。以书面规程和定期核验落实到日常经营。", effectSummary = "科研产速 +2%", effKind = "Rp", effVal = 0.02f, parents = new[] { "co_inst_4_45", "co_inst_4_65", "co_inst_4_69", "co_inst_4_71" } },
        new CompanyTechNode { key = "co_inst_4_71", eraGroup = 0, year = 1895, branch = 4, title = "无序兼并", periodLabel = "2101 年 · 核热纪制度推演", desc = "未清点债务便吞并经营困难路社。将教训列为后续合规制度的必修前置。", effectSummary = "历史教训：航线与星舰维保费短期 +2%（分局稽核制掌握后解除）", effKind = "Maint", effVal = 0.02f, resolvedBy = "co_inst_0_70", parents = new[] { "co_ops_4", "co_inst_2_38" } },
        new CompanyTechNode { key = "co_inst_4_72", eraGroup = 0, year = 1896, branch = 4, title = "保险漏保争议", periodLabel = "2101 年 · 核热纪制度推演", desc = "重要设备未纳入保单清单。将教训列为后续合规制度的必修前置。", effectSummary = "历史教训：航线与星舰维保费短期 +3%（风险准备金掌握后解除）", effKind = "Maint", effVal = 0.03f, resolvedBy = "co_inst_2_68", parents = new[] { "co_inst_1_67", "co_waybill_1871" } },
        new CompanyTechNode { key = "co_inst_4_73", eraGroup = 0, year = 1896, branch = 4, title = "星港前租约失衡", periodLabel = "2101 年 · 核热纪制度推演", desc = "长期低租约锁死改建空间。将教训列为后续合规制度的必修前置。", effectSummary = "历史教训：开港造价短期 +2%（风险准备金掌握后解除）", effKind = "OpenCost", effVal = 0.02f, resolvedBy = "co_inst_2_68", parents = new[] { "co_inst_1_62", "co_re_2" } },
        new CompanyTechNode { key = "co_inst_4_74", eraGroup = 0, year = 1896, branch = 4, title = "债券到期挤兑", periodLabel = "2101 年 · 核热纪制度推演", desc = "多批债券集中到期且缺少准备。将教训列为后续合规制度的必修前置。", effectSummary = "历史教训：债券偿付压力短期 +3%（风险准备金掌握后解除）", effKind = "BondDue", effVal = 0.03f, resolvedBy = "co_inst_2_68", parents = new[] { "co_inst_2_53", "co_kaiping_1881" } },
        new CompanyTechNode { key = "co_inst_4_75", eraGroup = 0, year = 1896, branch = 4, title = "股息透支", periodLabel = "2101 年 · 核热纪制度推演", desc = "以借款支付股息掏空偿债资金。将教训列为后续合规制度的必修前置。", effectSummary = "历史教训：债券偿付压力短期 +2%（跨境银团授信掌握后解除）", effKind = "BondDue", effVal = 0.02f, resolvedBy = "co_fin_4", parents = new[] { "co_inst_3_54", "co_inst_2_63" } }
    };

    static CompanyTechNode[] cachedAllCompanyTechNodes;
    static CompanyTechNode[] AllCompanyTechNodes
    {
        get
        {
            if (cachedAllCompanyTechNodes == null)
            {
                int a = CompanyTechNodes.Length, b = CompanyTechNodesEra20.Length;
                cachedAllCompanyTechNodes = new CompanyTechNode[a + b];
                Array.Copy(CompanyTechNodes, 0, cachedAllCompanyTechNodes, 0, a);
                Array.Copy(CompanyTechNodesEra20, 0, cachedAllCompanyTechNodes, a, b);
            }
            return cachedAllCompanyTechNodes;
        }
    }

    void EnsureCoTechIndexBuilt()
    {
        if (coTechByKey.Count > 0) return;
        var allCo = AllCompanyTechNodes;
        for (int i = 0; i < allCo.Length; i++)
        {
            var n = allCo[i];
            coTechByKey[n.key] = n;
        }
    }

    void EnsureCompanyTechMigrated()
    {
        EnsureCoTechIndexBuilt();
        if (companyTechDone.Count == 0) return;
        // 兼容旧档：保留全部已掌握 key，并补齐当前 DAG 的祖先链，避免悬空孤岛
        bool added;
        do
        {
            added = false;
            var snapshot = new List<string>(companyTechDone);
            foreach (string key in snapshot)
            {
                if (!coTechByKey.TryGetValue(key, out var node) || node.parents == null) continue;
                foreach (string parent in node.parents)
                {
                    if (!companyTechDone.Contains(parent))
                    {
                        companyTechDone.Add(parent);
                        added = true;
                    }
                }
            }
        } while (added);
    }

    int CoTechEraNodeCount(int era)
    {
        int c = 0;
        var allCo = AllCompanyTechNodes;
        for (int i = 0; i < allCo.Length; i++) if (allCo[i].eraGroup == era) c++;
        return c;
    }

    int CoTechEraDoneCount(int era)
    {
        EnsureCompanyTechMigrated();
        int c = 0;
        var allCo = AllCompanyTechNodes;
        for (int i = 0; i < allCo.Length; i++)
            if (allCo[i].eraGroup == era && companyTechDone.Contains(allCo[i].key)) c++;
        return c;
    }

    int CoTechRingIndex(CompanyTechNode node) => node == null ? 0 : TreeRingIndex(node.year, node.eraGroup);

    bool IsCoTechVisible(CompanyTechNode node)
    {
        if (node == null) return false;
        if (node.parents == null || node.parents.Length == 0) return true;
        foreach (string p in node.parents) if (companyTechDone.Contains(p)) return true;
        return false;
    }

    bool HasCoTech(string key) => companyTechDone.Contains(key);
    bool CanUnlockCoTech(CompanyTechNode node)
    {
        if (node == null || HasCoTech(node.key)) return false;
        if (node.parents == null) return true;
        foreach (string p in node.parents) if (!HasCoTech(p)) return false;
        return true;
    }

    int CoTechCoinCost(CompanyTechNode node)
    {
        if (node == null || node.key == "co_charter_1801") return 0;
        if (node.eraGroup == 1)
        {
            float t1 = Mathf.Clamp01((node.year - 1900f) / 99f);
            float mult1 = node.branch == 4 ? 0.72f : 1f;
            float raw1 = 140000f * Mathf.Pow(22f, t1) * mult1;
            int step1 = raw1 >= 500000f ? 10000 : 5000;
            return Mathf.Clamp(Mathf.RoundToInt(raw1 / step1) * step1, 140000, 3500000);
        }
        float t = Mathf.Clamp01((node.year - 1800f) / 99f);
        float mult = node.branch == 4 ? 0.65f : 1f;
        float raw = 200f * Mathf.Pow(5000f, t) * mult;
        int step = raw >= 10000f ? 1000 : raw >= 1000f ? 100 : 10;
        return Mathf.Clamp(Mathf.RoundToInt(raw / step) * step, 100, 1000000);
    }
    int CoTechRpCost(CompanyTechNode node)
    {
        if (node == null || node.key == "co_charter_1801") return 0;
        if (node.eraGroup == 1)
        {
            if (node.key == "co20_root_1900") return 40;
            float t1 = Mathf.Clamp01((node.year - 1900f) / 99f);
            float raw1 = (45f + 245f * Mathf.Pow(t1, 1.35f)) * (node.branch == 4 ? 0.8f : 1f);
            return Mathf.Clamp(Mathf.RoundToInt(raw1 / 5f) * 5, 40, 300);
        }
        float t = Mathf.Clamp01((node.year - 1800f) / 99f);
        return Mathf.Clamp(Mathf.RoundToInt(1f + t * 14f), 1, 15);
    }
    float CoTechCostScale(CompanyTechNode node)
    {
        if (node == null) return 1f;
        int cost = CoTechCoinCost(node);
        float costTier = Mathf.Max(0f, Mathf.Log10(Mathf.Max(1000f, cost) / 1000f));
        return 1f + costTier * 0.75f;
    }
    float CoTechScaledEffVal(CompanyTechNode node)
    {
        if (node == null) return 0f;
        if (!string.IsNullOrEmpty(node.resolvedBy)) return node.effVal; // 历史教训负面数值不放大
        return node.effVal * CoTechCostScale(node);
    }
    float CoTechLegacyMult(string key, float baseSignedDelta)
    {
        if (!HasCoTech(key)) return 1f;
        EnsureCoTechIndexBuilt();
        float scale = coTechByKey.TryGetValue(key, out var node) ? CoTechCostScale(node) : 1f;
        return 1f + baseSignedDelta * scale;
    }
    string CoTechScaledSummary(CompanyTechNode node)
    {
        if (node == null || string.IsNullOrEmpty(node.effectSummary)) return "";
        if (!string.IsNullOrEmpty(node.resolvedBy) || node.branch == 4) return node.effectSummary;
        float scale = CoTechCostScale(node);
        if (scale <= 1.05f) return node.effectSummary;
        return System.Text.RegularExpressions.Regex.Replace(node.effectSummary, @"([+-]?)(\d+)%", m =>
        {
            if (int.TryParse(m.Groups[2].Value, out int pct))
                return m.Groups[1].Value + Mathf.Max(pct, Mathf.RoundToInt(pct * scale)) + "%";
            return m.Value;
        });
    }
    float CoTechEffectMult(string kind)
    {
        EnsureCoTechIndexBuilt();
        float result = 1f;
        foreach (string key in companyTechDone)
            if (coTechByKey.TryGetValue(key, out var node) && node.effKind == kind &&
                (string.IsNullOrEmpty(node.resolvedBy) || !HasCoTech(node.resolvedBy))) result *= 1f + CoTechScaledEffVal(node);
        return result;
    }

    // ---------- 公司科技全局乘区（纯读取、与已有模型无缝相乘） ----------
    int CoTechExtraSeats() => (HasCoTech("co_hr_2") ? 1 : 0) + (HasCoTech("co_hr_4") ? 1 : 0);
    float CoTechPartnerConMitigation() => HasCoTech("co_hr_4") ? 0.90f : 1f;
    float CoTechTrainXpMult() => (CoTechLegacyMult("co_hr_1", 0.04f) * CoTechLegacyMult("co_hr_3", 0.04f) * CoTechLegacyMult("co_pension_1868", 0.04f)) * CoTechEffectMult("TrainXp");
    float CoTechTrainCostMult() => Mathf.Max(0.32f, (CoTechLegacyMult("co_hr_3", -0.04f) * CoTechLegacyMult("co_pension_1868", -0.04f)) * CoTechEffectMult("TrainCost") * CoTechEffectMult("Cost"));
    float CoTechOpenCostMult() => Mathf.Max(0.35f, (CoTechLegacyMult("co_charter_1801", -0.03f) * CoTechLegacyMult("co_booth_1826", -0.04f) * CoTechLegacyMult("co_re_1", -0.04f) * (HasCoTech("co_woosung_1876") && !HasCoTech("co_audit_1885") ? 1.03f : 1f)) * CoTechEffectMult("OpenCost"));
    float CoTechCommercialMult() => (CoTechLegacyMult("co_re_1", 0.04f) * CoTechLegacyMult("co_hotel_1854", 0.04f) * CoTechLegacyMult("co_re_2", 0.04f) * CoTechLegacyMult("co_re_4", 0.04f)) * CoTechEffectMult("Commercial");
    float CoTechSceneryDemandMult() => (CoTechLegacyMult("co_hotel_1854", 0.04f) * CoTechLegacyMult("co_re_3", 0.04f)) * CoTechEffectMult("SceneryDemand");
    float CoTechStationCapMult() => CoTechLegacyMult("co_re_4", 0.04f) * CoTechEffectMult("StationCap") * CoTechEffectMult("Capacity");
    float CoTechTrackBuildMult() => Mathf.Max(0.35f, (CoTechLegacyMult("co_stock_1821", -0.04f) * CoTechLegacyMult("co_fin_1", -0.04f) * CoTechLegacyMult("co_audit_1885", -0.04f)) * CoTechEffectMult("TrackBuild"));
    float CoTechMaintMult() => Mathf.Max(0.32f, (CoTechLegacyMult("co_fin_1", -0.04f) * (HasCoTech("co_mania_1845") && !HasCoTech("co_fin_2") ? 1.03f : 1f) * CoTechLegacyMult("co_waybill_1871", -0.04f) * CoTechLegacyMult("co_fin_4", -0.04f)) * CoTechEffectMult("Maint"));
    float CoTechBondCapMult() => CoTechLegacyMult("co_fin_2", 0.04f) * CoTechEffectMult("BondCap");
    float CoTechBondDueMult() => Mathf.Max(0.35f, (CoTechLegacyMult("co_fin_2", -0.04f) * CoTechLegacyMult("co_audit_1885", -0.04f)) * CoTechEffectMult("BondDue"));
    int CoTechContractCap() => HasCoTech("co_fin_3") ? 3 : 2;
    float CoTechContractBonusMult() => CoTechLegacyMult("co_fin_3", 0.04f);
    float CoTechSpeedMult() => CoTechLegacyMult("co_ops_1", 0.04f) * CoTechEffectMult("Speed");
    float CoTechRpMult() => (CoTechLegacyMult("co_ops_1", 0.04f) * CoTechLegacyMult("co_kaiping_1881", 0.04f)) * CoTechEffectMult("Rp") * CoTechEffectMult("Tech");
    float CoTechTransferMult() => Mathf.Max(0.38f, (CoTechLegacyMult("co_rch_1842", -0.04f) * CoTechLegacyMult("co_ops_2", -0.04f)) * CoTechEffectMult("Transfer"));
    float CoTechRevenueMult() => (CoTechLegacyMult("co_fare_act_1844", 0.03f) * CoTechLegacyMult("co_ops_2", 0.04f) * CoTechLegacyMult("co_rateact_1887", 0.04f) * CoTechLegacyMult("co_fin_4", 0.04f)) * CoTechEffectMult("Revenue");
    float CoTechTourismLineMult() => CoTechLegacyMult("co_ops_3", 0.04f) * CoTechEffectMult("Tourism");
    float CoTechBaseDemandMult() => (CoTechLegacyMult("co_fare_act_1844", 0.04f) * CoTechLegacyMult("co_kaiping_1881", 0.04f) * CoTechLegacyMult("co_ops_4", 0.04f)) * CoTechEffectMult("Demand");
    float CoTechAwardMult() => CoTechLegacyMult("co_ops_4", 0.04f) * CoTechEffectMult("Award");

    float MitigateCon(float rawDelta)
    {
        // 例：rawDelta = 0.10f 表示负面增加 10%，减免 4% 后变为 +9.6%；若为 -0.05f 同样缩窄幅度
        return 1f + rawDelta * CoTechPartnerConMitigation();
    }

    // ---------- 人才编制、培养、加值计算 ----------
    int TalentRosterCapacity()
    {
        return 2 + (HasCoTech("co_shares_1825") ? 1 : 0) + (HasCoTech("co_hr_1") ? 1 : 0) + (HasCoTech("co_hr_2") ? 1 : 0)
            + Mathf.Min(3, DailyConnectedCount() / 10);
    }

    int TalentNextLevelXp(int level) => 40 + Mathf.Clamp(level, 1, 5) * 35;

    float TalentPowerScale(SavedTalent t)
    {
        if (t == null) return 0f;
        float rMult = t.rarity == 3 ? 1.45f : t.rarity == 2 ? 1.20f : 1.0f;
        return (0.04f + (t.level - 1) * 0.025f) * rMult;
    }

    bool TalentHasActiveFlaw(SavedTalent t) => t != null && !t.flawCleansed && t.level < 3;

    string TalentRoleEffectSummary(SavedTalent t)
    {
        if (t == null) return "";
        int pctMain = Mathf.RoundToInt(TalentPowerScale(t) * 100f);
        int pctSub = Mathf.Max(2, Mathf.RoundToInt(TalentPowerScale(t) * 65f));
        switch (t.role)
        {
            case 0: return $"驻港吞吐 +{pctMain}% · 星港前物业租金 +{pctMain + 4}%";
            case 1: return $"随航线星车速度 +{pctMain}% · 编队运力 +{pctSub}%";
            case 2: return $"维保支出 -{pctMain}% · 星票综合净利 +{pctSub}%";
            default: return $"沿线名胜吸引力 +{pctMain + 3}% · 票务营收 +{pctSub}%";
        }
    }

    string TalentAssignmentLabel(SavedTalent t)
    {
        if (t == null) return "待命中";
        if (t.assignKind == 1 && !string.IsNullOrEmpty(t.assignStationKey))
        {
            int stIdx = FindStationIndexByKey(t.assignStationKey);
            return stIdx >= 0 ? "驻港 · " + StationDisplayName(stIdx, stations[stIdx].region) : "驻港 · " + t.assignStationKey;
        }
        if (t.assignKind == 2 && t.assignLineId > 0)
        {
            var line = lines.Find(l => l.id == t.assignLineId);
            return line != null ? "随航线 · " + line.name : "待命中";
        }
        return "总部待命";
    }

    int FindStationIndexByKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return -1;
        for (int i = 0; i < stations.Count; i++)
            if (StationSaveKey(stations[i]) == key) return i;
        return -1;
    }

    void EnsureTalentsInitialized(bool forceMarketRefresh = false)
    {
        // 自动修正失效的派驻目标
        foreach (var t in talents)
        {
            if (t == null) continue;
            t.level = Mathf.Clamp(t.level, 1, 5);
            if (t.assignKind == 2 && lines.Find(l => l.id == t.assignLineId) == null)
            { t.assignKind = 0; t.assignLineId = 0; }
            if (t.assignKind == 1 && FindStationIndexByKey(t.assignStationKey) < 0)
            { t.assignKind = 0; t.assignStationKey = ""; }
        }
        if (forceMarketRefresh || talentPool.Count == 0 || lastTalentRefreshDay != day)
            RefreshTalentMarket();
    }

    void RefreshTalentMarket()
    {
        talentPool.Clear();
        lastTalentRefreshDay = day;
        int seedBase = Mathf.Abs(day * 131 + DailyConnectedCount() * 17 + nextTalentId * 29 + (int)Mathf.Round((float)(coins % 97d)));
        for (int i = 0; i < 3; i++)
        {
            int h = seedBase + i * 73;
            int roll = h % 100;
            int rarity = roll >= 88 ? 3 : roll >= 52 ? 2 : 1;
            int role = (h / 7 + i) % TalentRoleNames.Length;
            string fullName = TalentSurnames[(h / 3) % TalentSurnames.Length] + TalentGivenNames[(h / 11 + i * 3) % TalentGivenNames.Length];
            talentPool.Add(new SavedTalent {
                id = 0,
                name = fullName,
                role = role,
                rarity = rarity,
                level = rarity >= 3 ? 2 : 1,
                xp = 0,
                assignKind = 0,
                flawIndex = (h / 5) % TalentFlawDescs.Length,
                flawCleansed = false,
                bio = TalentBios[(h / 13 + i) % TalentBios.Length]
            });
        }
        SaveGame();
    }

    int TalentHireCost(SavedTalent cand)
    {
        if (cand == null) return 300;
        return cand.rarity == 3 ? 1280 : cand.rarity == 2 ? 680 : 320;
    }

    int TalentTrainCoinCost(SavedTalent t)
    {
        int raw = (240 + t.level * 180) * t.rarity;
        return Mathf.Max(80, Mathf.RoundToInt(raw * CoTechTrainCostMult() / 10f) * 10);
    }

    int TalentTrainRpCost(SavedTalent t) => Mathf.Clamp(2 + t.level * 2, 2, 12);

    void GrantTalentXp(SavedTalent t, int rawXp, bool notify)
    {
        if (t == null || t.level >= 5) return;
        t.xp += Mathf.Max(1, Mathf.RoundToInt(rawXp * CoTechTrainXpMult()));
        bool leveled = false;
        while (t.level < 5 && t.xp >= TalentNextLevelXp(t.level))
        {
            t.xp -= TalentNextLevelXp(t.level);
            t.level++;
            leveled = true;
            if (t.level >= 3) t.flawCleansed = true;
        }
        if (t.level >= 5) t.xp = 0;
        if (leveled && notify) Toast($"人才晋升：「{t.name}」升至 Lv.{t.level}" + (t.level == 3 ? "（磨合期结束！）" : ""));
    }

    void TickDailyTalents()
    {
        if (talents.Count == 0) return;
        bool anyChange = false;
        foreach (var t in talents)
        {
            if (t == null || t.level >= 5) continue;
            int dailyXp = t.assignKind != 0 ? 15 : 6;
            int prevLvl = t.level;
            GrantTalentXp(t, dailyXp, true);
            if (t.level != prevLvl) anyChange = true;
        }
        if (anyChange) hasForecastSignature = false;
    }

    // ---------- 人才乘区注入 ----------
    float LineTalentSpeedMult(Line line)
    {
        if (line == null) return 1f;
        float m = 1f;
        foreach (var t in talents)
        {
            if (t == null || t.assignKind != 2 || t.assignLineId != line.id) continue;
            if (t.role == 1) m *= 1f + TalentPowerScale(t);
            if (TalentHasActiveFlaw(t) && t.flawIndex == 1) m *= 0.97f;
        }
        return m;
    }

    float LineTalentCapMult(Line line)
    {
        if (line == null) return 1f;
        float m = 1f;
        foreach (var t in talents)
        {
            if (t == null || t.assignKind != 2 || t.assignLineId != line.id) continue;
            if (t.role == 1) m *= 1f + TalentPowerScale(t) * 0.65f;
        }
        return m;
    }

    float LineTalentMaintMult(Line line)
    {
        if (line == null) return 1f;
        float m = 1f;
        foreach (var t in talents)
        {
            if (t == null || t.assignKind != 2 || t.assignLineId != line.id) continue;
            if (t.role == 2) m *= Mathf.Max(0.65f, 1f - TalentPowerScale(t));
            if (TalentHasActiveFlaw(t) && t.flawIndex == 0) m *= 1.05f;
        }
        return m;
    }

    float LineTalentRevenueMult(Line line)
    {
        if (line == null) return 1f;
        float m = 1f;
        foreach (var t in talents)
        {
            if (t == null || t.assignKind != 2 || t.assignLineId != line.id) continue;
            if (t.role == 2 || t.role == 3) m *= 1f + TalentPowerScale(t) * 0.65f;
            if (TalentHasActiveFlaw(t) && t.flawIndex == 3) m *= 0.97f;
        }
        return m;
    }

    float StationTalentCapMult(int stationIdx)
    {
        if (stationIdx < 0 || stationIdx >= stations.Count) return 1f;
        string key = StationSaveKey(stations[stationIdx]);
        float m = 1f;
        foreach (var t in talents)
        {
            if (t == null || t.assignKind != 1 || t.assignStationKey != key) continue;
            if (t.role == 0) m *= 1f + TalentPowerScale(t);
        }
        return m;
    }

    float StationTalentDemandMult(int stationIdx)
    {
        if (stationIdx < 0 || stationIdx >= stations.Count) return 1f;
        string key = StationSaveKey(stations[stationIdx]);
        float m = 1f;
        foreach (var t in talents)
        {
            if (t == null || t.assignKind != 1 || t.assignStationKey != key) continue;
            if (t.role == 3) m *= 1f + TalentPowerScale(t) + 0.03f;
        }
        return m;
    }

    float StationTalentCommercialMult(int stationIdx)
    {
        if (stationIdx < 0 || stationIdx >= stations.Count) return 1f;
        string key = StationSaveKey(stations[stationIdx]);
        float m = 1f;
        foreach (var t in talents)
        {
            if (t == null || t.assignKind != 1 || t.assignStationKey != key) continue;
            if (t.role == 0) m *= 1f + TalentPowerScale(t) + 0.04f;
            if (t.role == 2) m *= 1f + TalentPowerScale(t) * 0.5f;
        }
        return m;
    }

    // ---------- M2 UI A: 企业治理树（共享同心圆历史图谱引擎，XIX世纪史实经营节点） ----------
    readonly HashSet<string> coTechSessionUnlocked = new HashSet<string>();
    bool coTechCollapseDone = true;
    string coTechJustUnlocked;

    void OpenCompanyTechScreen(int mode = 0)
    {
        EnsureCompanyTechMigrated();
        OpenConcentricTechScreen(true, mode);
    }

    void TapCompanyTechNode(CompanyTechNode node)
    {
        if (node == null) return;
        if (HasCoTech(node.key))
        {
            HoldCoTechNode(node.key);
            return;
        }
        if (!CanUnlockCoTech(node))
        {
            HoldCoTechNode(node.key);
            Invoke(nameof(HideHistoryInfo), 2f);
            return;
        }
        if (coins < CoTechCoinCost(node))
        {
            PlayErrorSfx();
            Toast("研发经费不足：还差 ¥" + FormatShortfall(CoTechCoinCost(node) - coins));
            if (historyCoinWidget) historyCoinWidget.Shake();
            return;
        }
        if (rp < CoTechRpCost(node))
        {
            PlayErrorSfx();
            float gap = CoTechRpCost(node) - rp;
            NotifyOrOfferDiamondSupply("科研点不足：需要 " + CoTechRpCost(node) + " RP（还差 " + Mathf.CeilToInt(gap) + " RP）", true, gap);
            return;
        }

        historyAnimated.Clear();
        var allCo = AllCompanyTechNodes;
        for (int i = 0; i < allCo.Length; i++)
        {
            var cand = allCo[i];
            if (cand.eraGroup == currentTechEra && IsCoTechVisible(cand)) historyAnimated.Add(cand.key);
        }

        BeginUndo("解锁企业治理科技");
        coins -= CoTechCoinCost(node);
        rp -= CoTechRpCost(node);
        companyTechDone.Add(node.key);
        coTechSessionUnlocked.Add(node.key);
        lastUnlockedCoTechEra = node.eraGroup;
        coTechJustUnlocked = node.key;
        EnsureCompanyInitialized();
        operatingRates.Clear();
        RecalcIncome();
        EndUndo();
        PlayTechSfx();
        TriggerHaptic(22);
        RefreshHUD();
        RefreshTechTreeIncremental(true, node.key);
        Toast("企业治理升级：「" + node.title + "」已生效！");
    }

    // ---------- M2 UI B: 人才招募与培养页签（嵌入公司->高管与人才页） ----------
    void BuildTalentManagementView(Transform page, float cardW)
    {
        EnsureTalentsInitialized();
        float poolH = 708f;
        float rosterH = Mathf.Max(280f, talents.Count * 246f + 130f);
        float totalH = poolH + rosterH + 60f;
        var content = FleetScroll(page, 36f, PgH - 350f, totalH, scrollKey: "company:talents");
        float topY = content.sizeDelta.y;
        float cardX = 4f, innerW = cardW - 8f, cW = innerW - 64f;

        // 1. 人才市场（每日免费刷新 / ¥150 即刻招募）
        float y1 = topY - 12f - poolH;
        var poolCard = Rect("TalentMarketCard", content, cardX, y1, innerW, poolH);
        Rounded(poolCard, CardBg); SoftShadow(poolCard); JournalTape(poolCard.transform, innerW * 0.5f - 72f, poolH - 17f, 144f);
        Txt(poolCard.transform, "星域猎聘阁 · 今日待聘名才", 29, Ink, 32f, poolH - 64f, 560f, 44f, TextAnchor.MiddleLeft);
        var refreshBtn = Btn(poolCard.transform, "RefreshMarket", "刷新名录 ¥150", 23, innerW - 276f, poolH - 72f, 244f, 58f, Paper2, () => {
            if (coins < 150) { PlayErrorSfx(); Toast("刷新猎聘名录需 ¥150"); return; }
            coins -= 150;
            RefreshTalentMarket();
            SaveGame(); RefreshHUD(); OpenCompanyScreen();
            Toast("已更新今日待聘人才名录");
        });
        TInk(refreshBtn, Indigo);

        for (int i = 0; i < talentPool.Count; i++)
        {
            var cand = talentPool[i];
            int idx = i;
            float ry = poolH - 258f - i * 184f;
            var row = Rect("Cand_" + i, poolCard.transform, 32f, ry, cW, 170f);
            Rounded(row, Color.Lerp(CardBg, Ink, 0.04f));
            Color rCol = cand.rarity == 3 ? Gold : cand.rarity == 2 ? Indigo : Matcha;
            var badge = Rect("Rarity", row.transform, 16f, 43f, 84f, 84f);
            Rounded(badge, rCol);
            Txt(badge.transform, TalentRarityNames[cand.rarity] + "\n" + TalentRoleNames[cand.role].Substring(0, 2), 22, Color.white, 0f, 0f, 84f, 84f, TextAnchor.MiddleCenter);

            Txt(row.transform, $"{cand.name} · Lv.{cand.level} {TalentRoleNames[cand.role]}", 25, Ink, 114f, 120f, 560f, 38f, TextAnchor.MiddleLeft);
            Txt(row.transform, "✓ " + TalentRoleEffectSummary(cand), 23, Matcha, 114f, 78f, 580f, 36f, TextAnchor.MiddleLeft);
            var flawTxt = Txt(row.transform, "• " + TalentFlawDescs[Mathf.Clamp(cand.flawIndex, 0, TalentFlawDescs.Length - 1)], 20, InkSoft, 114f, 16f, 580f, 56f, TextAnchor.UpperLeft);
            flawTxt.resizeTextForBestFit = true; flawTxt.resizeTextMinSize = 15; flawTxt.resizeTextMaxSize = ScaledFontSize(20); flawTxt.horizontalOverflow = HorizontalWrapMode.Overflow; flawTxt.verticalOverflow = VerticalWrapMode.Truncate;

            int hireCost = TalentHireCost(cand);
            bool full = talents.Count >= TalentRosterCapacity();
            var hBtn = Btn(row.transform, "HireTalent_" + i, full ? "编制已满" : ("签约 ¥" + hireCost), 24, cW - 230f, 44f, 212f, 82f,
                full ? new Color(0.82f, 0.80f, 0.75f) : Matcha, () => {
                    if (talents.Count >= TalentRosterCapacity()) { PlayErrorSfx(); Toast("在籍人才已满上限（" + TalentRosterCapacity() + "），可在「企业治理树」扩充编制"); return; }
                    if (coins < hireCost) { PlayErrorSfx(); Toast("签约金币不足，还差 ¥" + FormatShortfall(hireCost - coins)); return; }
                    coins -= hireCost;
                    cand.id = nextTalentId++;
                    talents.Add(cand);
                    talentPool.RemoveAt(idx);
                    SaveGame(); RefreshHUD();
                    PlayCoinSfx();
                    SpawnConfettiBurst(new Vector2(RootW * 0.5f, RootH * 0.5f), Matcha, Gold, 12);
                    OpenCompanyScreen();
                    Toast($"欢迎「{cand.name}」加入{companyName}！");
                });
            TInk(hBtn, full ? InkSoft : Color.white);
        }

        // 2. 在籍人才名册与培养中心
        float y2 = y1 - 22f - rosterH;
        var rosterCard = Rect("TalentRosterCard", content, cardX, y2, innerW, rosterH);
        Rounded(rosterCard, CardBg); SoftShadow(rosterCard);
        Txt(rosterCard.transform, $"在籍人才档案与培养（{talents.Count} / {TalentRosterCapacity()} 人）", 29, Ink, 32f, rosterH - 64f, 880f, 44f, TextAnchor.MiddleLeft);

        if (talents.Count == 0)
        {
            Txt(rosterCard.transform, "尚未签约专业人才。\n从上方猎聘阁签约后，可指派其【驻守星港】或【随航线执勤】，亦可投入金币与科研点进修晋升至 Lv.5。",
                24, InkSoft, 32f, 40f, cW, rosterH - 130f, TextAnchor.MiddleCenter);
            return;
        }

        for (int i = 0; i < talents.Count; i++)
        {
            var t = talents[i];
            float ry = rosterH - 316f - i * 246f;
            var card = Rect("TalentItem_" + t.id, rosterCard.transform, 32f, ry, cW, 228f);
            Rounded(card, Color.Lerp(CardBg, Indigo, 0.05f));

            string xpStr = t.level >= 5 ? "已满级" : $"经验 {t.xp}/{TalentNextLevelXp(t.level)}";
            Txt(card.transform, $"{t.name} · {TalentRarityNames[t.rarity]}{TalentRoleNames[t.role]}  Lv.{t.level}  ({xpStr})", 25, Ink, 20f, 170f, 650f, 42f, TextAnchor.MiddleLeft);
            Txt(card.transform, TalentAssignmentLabel(t), 22, t.assignKind == 0 ? InkSoft : Indigo, 660f, 172f, cW - 680f, 38f, TextAnchor.MiddleRight);
            Txt(card.transform, "✓ " + TalentRoleEffectSummary(t), 23, Matcha, 20f, 128f, cW - 40f, 36f, TextAnchor.MiddleLeft);
            string flawLine = t.flawCleansed || t.level >= 3
                ? "✓ 初任磨合已突破，全特质纯正生效"
                : "• " + TalentFlawDescs[Mathf.Clamp(t.flawIndex, 0, TalentFlawDescs.Length - 1)];
            Txt(card.transform, flawLine, 22, t.flawCleansed || t.level >= 3 ? Matcha : Vermilion, 20f, 90f, cW - 40f, 34f, TextAnchor.MiddleLeft);

            float bw = (cW - 60f) * 0.25f;
            var assignBtn = Btn(card.transform, "Assign_" + t.id, "指派岗位", 22, 20f, 14f, bw, 66f, Indigo, () => OpenTalentAssignPopup(t));
            TInk(assignBtn, Color.white);

            bool maxLv = t.level >= 5;
            int trC = TalentTrainCoinCost(t), trRp = TalentTrainRpCost(t);
            var trainBtn = Btn(card.transform, "Train_" + t.id, maxLv ? "Lv.5 圆满" : $"进修 ¥{trC}/{trRp}RP", 22, 20f + bw + 12f, 14f, bw, 66f,
                maxLv ? Paper2 : Matcha, () => {
                    if (t.level >= 5) { Toast("该人才已修业至最高 Lv.5"); return; }
                    if (coins < trC) { PlayErrorSfx(); NotifyOrOfferDiamondSupply("进修经费不足：需 ¥" + trC, false, (float)(trC - coins)); return; }
                    if (rp < trRp) { PlayErrorSfx(); NotifyOrOfferDiamondSupply("科研点不足：需 " + trRp + " RP", true, trRp - rp); return; }
                    coins -= trC; rp -= trRp;
                    GrantTalentXp(t, TalentNextLevelXp(t.level), false);
                    hasForecastSignature = false; operatingRates.Clear(); RecalcIncome();
                    PlayTechSfx();
                    SaveGame(); RefreshHUD(); OpenCompanyScreen();
                    Toast($"「{t.name}」进修顺利，晋升为 Lv.{t.level}！");
                });
            TInk(trainBtn, maxLv ? InkSoft : Color.white);

            bool cleanable = TalentHasActiveFlaw(t);
            var cleanseBtn = Btn(card.transform, "Cleanse_" + t.id, cleanable ? "淬炼除弊 5RP" : "传记履历", 22, 20f + (bw + 12f) * 2f, 14f, bw, 66f,
                cleanable ? Gold : Paper2, () => {
                    if (cleanable)
                    {
                        if (rp < 5f) { PlayErrorSfx(); Toast("淬炼需 5 RP 科研指导点数"); return; }
                        rp -= 5f; t.flawCleansed = true;
                        hasForecastSignature = false; operatingRates.Clear(); RecalcIncome();
                        PlayTechSfx(); SaveGame(); RefreshHUD(); OpenCompanyScreen();
                        Toast($"已为「{t.name}」解除初任磨合短板！");
                    }
                    else OpenTalentBioPopup(t);
                });
            TInk(cleanseBtn, cleanable ? Color.white : Ink);

            var fireBtn = Btn(card.transform, "Fire_" + t.id, "礼送解聘", 22, 20f + (bw + 12f) * 3f, 14f, bw, 66f, Vermilion, () => {
                Confirm("礼送解聘", $"确定礼送「{t.name}」离任吗？", () => {
                    talents.Remove(t);
                    hasForecastSignature = false; operatingRates.Clear(); RecalcIncome();
                    SaveGame(); OpenCompanyScreen();
                    Toast("已与「" + t.name + "」友好解约");
                });
            });
            TInk(fireBtn, Color.white);
        }
    }

    void OpenTalentBioPopup(SavedTalent t)
    {
        if (t == null) return;
        var p = OpenPopup(t.name + " · 人物小传", 520f, 820f);
        Txt(p.transform, $"{TalentRarityNames[t.rarity]} {TalentRoleNames[t.role]} · 当前 Lv.{t.level}\n岗前履历：{TalentAssignmentLabel(t)}", 24, Indigo, 36f, 368f, 748f, 70f, TextAnchor.UpperLeft);
        Txt(p.transform, t.bio, 24, Ink, 36f, 220f, 748f, 130f, TextAnchor.UpperLeft);
        Txt(p.transform, "专长效用：" + TalentRoleEffectSummary(t), 23, Matcha, 36f, 136f, 748f, 52f, TextAnchor.MiddleLeft);
        var ok = Btn(p.transform, "Ok", "合上档案", 25, 36f, 30f, 748f, 78f, Paper2, ClosePanel);
        TInk(ok, Ink);
    }

    void OpenTalentAssignPopup(SavedTalent t)
    {
        if (t == null) return;
        bool lineMode = t.role == 1 || t.role == 2; // 舰长/精算师偏随航线，也可手动切换
        var items = new List<(string label, int kind, int lineId, string stKey)>();
        items.Add(("返回总部待命", 0, 0, ""));
        foreach (var l in lines)
            items.Add(($"随航线执勤 · {l.name}（{l.st.Count} 站）", 2, l.id, ""));
        for (int i = 0; i < stations.Count && items.Count < 42; i++)
            if (stations[i].built && !stations[i].closed)
                items.Add(($"驻守星港 · {StationDisplayName(i, stations[i].region)}（{stations[i].region}）", 1, 0, StationSaveKey(stations[i])));

        float rowH = 84f;
        const float popW = 860f, scrollX = 28f, scrollW = 804f, scrollBottom = 114f;
        float h = Mathf.Clamp(252f + Mathf.Min(items.Count, 6) * rowH, 460f, 820f);
        var p = OpenPopup($"指派「{t.name}」({TalentRoleNames[t.role]})", h, popW, "talent_assign_" + t.id);
        string hint = lineMode ? "建议优先随航线执勤；也可按需派驻核心大港。" : "建议优先驻守已开港的高客流港点或枢纽线。";
        Txt(p.transform, hint, 23, InkSoft, 34f, h - 122f, 792f, 38f, TextAnchor.MiddleLeft);

        float viewH = Mathf.Max(140f, h - 134f - scrollBottom);
        var view = Rect("AssignViewport", p.transform, scrollX, scrollBottom, scrollW, viewH);
        Img(view, new Color(1f, 1f, 1f, 0.01f));
        var mask = view.AddComponent<Mask>(); mask.showMaskGraphic = false;
        var scroll = view.AddComponent<ScrollRect>();
        scroll.horizontal = false; scroll.vertical = true;
        scroll.inertia = true; scroll.decelerationRate = 0.12f;
        scroll.scrollSensitivity = 96f; scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.viewport = view.GetComponent<RectTransform>();

        float contentH = Mathf.Max(viewH, items.Count * rowH + 8f);
        var contentGo = Rect("AssignContent", view.transform, 0f, 0f, scrollW, contentH);
        var content = contentGo.GetComponent<RectTransform>();
        content.anchorMin = content.anchorMax = new Vector2(0f, 1f);
        content.pivot = new Vector2(0f, 1f);
        content.anchoredPosition = Vector2.zero;
        scroll.content = content;
        BindScrollPosition(scroll, "talent:assign:" + t.id);

        for (int i = 0; i < items.Count; i++)
        {
            var entry = items[i];
            float y = contentH - (i + 1) * rowH + 4f;
            bool isCur = (entry.kind == 0 && t.assignKind == 0)
                || (entry.kind == 2 && t.assignKind == 2 && t.assignLineId == entry.lineId)
                || (entry.kind == 1 && t.assignKind == 1 && t.assignStationKey == entry.stKey);
            var btn = Btn(content, "Slot_" + i, entry.label + (isCur ? "  ✓ 当前" : ""), 22, 4f, y, scrollW - 8f, rowH - 12f,
                isCur ? Indigo : Paper2, () => {
                    t.assignKind = entry.kind;
                    t.assignLineId = entry.lineId;
                    t.assignStationKey = entry.stKey;
                    hasForecastSignature = false; operatingRates.Clear(); RecalcIncome();
                    ClosePanel(true); SaveGame(); RefreshHUD(); OpenCompanyScreen();
                    Toast($"已将「{t.name}」派往：{entry.label}");
                });
            TInk(btn, isCur ? Color.white : Ink);
        }
        var cancel = Btn(p.transform, "Cancel", "取消", 24, 34f, 26f, 792f, 74f, Paper2, ClosePanel);
        TInk(cancel, Ink);
    }
}
