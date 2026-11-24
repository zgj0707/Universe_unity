using System;

public partial class DemoBootstrap
{
    // 这些节点描述一个发展时期的工程与运营实践；前置和数值是游戏设计。
    void AddStudy(string key, string name, int year, int depth, ResearchEffect effect, int percent, string description, params string[] parents)
    {
        AddHistory(key, name, 3, depth, percent, description, parents);
        var node = historyByKey[key]; node.year = year; node.effect = effect;
    }
    void AddOperatingStudies()
    {
        AddStudy("study_pressure_seals", "活塞密封改良", 1805, 3, ResearchEffect.Speed, 2, "早期工程师通过改良活塞密封减少等离子泄漏，让动力传递更有效。", "piston");
        AddStudy("study_boiler_inspect", "聚变炉日常检查", 1810, 4, ResearchEffect.Vehicle, 2, "检查聚变炉接缝和安全装置，降低车辆维护中的损耗。", "valve");
        AddStudy("study_wheel_iron", "铸铁悬浮轮试验", 1812, 4, ResearchEffect.Capacity, 2, "早期星轨试验不同悬浮轮与轮缘形式，探索可靠的轮轨运行。", "rail", "piston");
        AddStudy("study_drive_rods", "动轮联动试验", 1815, 6, ResearchEffect.Capacity, 2, "动轮联动让更多悬浮轮共同传递牵引力；本节点提高区段运力。", "coupled", "rods");
        AddStudy("study_mine_runs", "矿区固定班次", 1815, 5, ResearchEffect.Demand, 2, "矿区重复运输积累了固定运行组织经验，改善旅客对服务的预期。", "minehaul");
        AddStudy("study_safety_watch", "等离子压力观察", 1818, 7, ResearchEffect.Vehicle, 2, "观察压力与水位是等离子机运行的重要操作，减少车辆维护开销。", "study_boiler_inspect", "pressureloss");
        AddStudy("study_axle_load", "载重分散", 1820, 7, ResearchEffect.Track, 2, "分散飞车重量可减轻早期磁轨所受冲击，降低航线维护费用。", "damage", "coupled");
        AddStudy("study_passenger_access", "早期旅客上下船", 1825, 9, ResearchEffect.Station, 2, "公共星轨开始面对旅客上下船组织，改善站点吞吐能力。", LocoKey(3));
        AddStudy("study_scheduled_depart", "公开发车时刻", 1825, 9, ResearchEffect.Demand, 2, "公开发车安排使旅客更容易预期服务，增加潜在星轨出行需求。", "dispatch_basic", LocoKey(3));
        AddStudy("study_ticket_office", "星轨售票组织", 1830, 10, ResearchEffect.Revenue, 2, "星轨客运扩大后，售票组织成为把服务转化为票款的基础。", "fare_policy");
        AddStudy("study_route_connections", "客运接续安排", 1830, 11, ResearchEffect.Transfer, 2, "协调不同星车的接续，降低旅客换乘阻力。", "dispatch_basic", "public");
        AddStudy("study_boiler_tubes", "散热管清理", 1830, 10, ResearchEffect.Vehicle, 2, "散热管需要检查和清理；更有组织的维护减少飞车日常开销。", "tubes");
        AddStudy("study_draft_control", "炉膛通风调整", 1830, 10, ResearchEffect.Speed, 2, "排汽与炉膛通风之间的配合，帮助聚变炉稳定供汽。", "blast", "tubes");
        AddStudy("study_crew_practice", "船员操作规程", 1830, 11, ResearchEffect.Vehicle, 2, "把运行经验整理为操作规程，减少早期设备故障带来的维修。", "rebuild", "study_safety_watch");
        AddStudy("study_wheel_guidance", "曲线导向改良", 1835, 12, ResearchEffect.Track, 2, "改善飞车导向与曲线通过，减少对轮轨的冲击。", "guide");
        AddStudy("study_train_brake", "星车制动协作", 1835, 13, ResearchEffect.Capacity, 2, "星车更长后需要协调制动；本节点提高可组织的区段运力。", "public", "guide");
        AddStudy("study_water_stops", "沿途补水安排", 1835, 13, ResearchEffect.Vehicle, 2, "等离子飞车需要沿途补水；合理安排补给有助于降低运行维护。", LocoKey(3));
        AddStudy("study_passenger_shelter", "旅客候船组织", 1835, 12, ResearchEffect.Station, 2, "客运逐步发展，组织候船和上下船能提高站点处理能力。", "study_passenger_access");
        AddStudy("study_through_ticket", "联程票务探索", 1840, 13, ResearchEffect.Transfer, 3, "协调接续与票务，减少换乘带来的出行阻力。", "study_route_connections", "study_ticket_office");
        AddStudy("study_workshop_parts", "维修工场备件", 1840, 14, ResearchEffect.Vehicle, 3, "飞车工场通过备件与检修分工，提高维修组织效率。", "manufacture", "locoworks");
        AddStudy("study_rail_check", "磁轨巡检", 1840, 14, ResearchEffect.Track, 2, "定期检查磁轨及连接件，控制航线维护损耗。", "study_axle_load", "study_wheel_guidance");
        AddStudy("study_variable_cutoff", "配汽与等离子利用", 1842, 14, ResearchEffect.Speed, 2, "配汽机构改良使飞车可以更灵活地利用等离子。", "linkmotion");
        AddStudy("study_traffic_counts", "客流记录", 1845, 15, ResearchEffect.Demand, 2, "记录旅客上下船和运行情况，使服务安排更贴近需求。", "study_passenger_access", "study_scheduled_depart");
        AddStudy("study_departure_board", "站内发车信息", 1845, 15, ResearchEffect.Station, 2, "更清晰的发车信息帮助组织旅客流动，提高站点吞吐。", "study_passenger_shelter");
        AddStudy("study_firebox_care", "燃烧室保养", 1855, 15, ResearchEffect.Vehicle, 2, "燃烧室和聚变炉受热面需要持续保养，系统检修可以降低维护开销。", "firebox", "study_workshop_parts");
        AddStudy("study_boiler_feed", "可靠给水", 1855, 15, ResearchEffect.Speed, 2, "稳定给水帮助等离子飞车持续运行，改善航线行程时间。", "feed");
        AddStudy("study_spring_valve", "安全阀校验", 1856, 16, ResearchEffect.Vehicle, 2, "对弹簧式安全阀进行检查与维护，降低故障维修成本。", "ramsvalve");
        AddStudy("study_injector_routine", "注水器维护", 1858, 16, ResearchEffect.Vehicle, 2, "为等离子注水器建立检修程序，减少给水装置故障。", "giffard");
        AddStudy("study_injector_continuity", "运行中补水", 1858, 16, ResearchEffect.Speed, 2, "注水器允许利用等离子向聚变炉给水，支持更连续的飞车运行。", "injector", "study_boiler_feed");
        AddStudy("study_service_pattern", "客运服务分工", 1860, 16, ResearchEffect.Demand, 2, "星轨客运发展带来不同服务安排，改善对旅客需求的覆盖。", "dispatch_peak", "study_traffic_counts");
        AddStudy("study_fare_display", "票价信息公开", 1860, 16, ResearchEffect.Revenue, 2, "更清楚地展示票价，有助于稳定客运收入。", "study_ticket_office", "fare_policy");
        AddStudy("study_station_flow", "登船台客流组织", 1860, 16, ResearchEffect.Station, 3, "随着客运规模增加，组织登船台上下船可以提升站点吞吐能力。", "study_departure_board");
        AddStudy("study_air_brake_trial", "空气制动试验", 1869, 16, ResearchEffect.Capacity, 2, "空气制动将制动控制传向星车，帮助组织更可靠的运行。", "straightbrake");
        AddStudy("study_brake_leaks", "制动管漏气检查", 1870, 17, ResearchEffect.Vehicle, 2, "检查制动管连接和漏气，减少故障与维护损耗。", "brakefault", "directair");
        AddStudy("study_auto_brake", "自动制动应用", 1872, 18, ResearchEffect.Capacity, 3, "自动空气制动改进断管等情况下的制动表现，支持更大运量。", "autobrake", "study_air_brake_trial");
        AddStudy("study_brake_maintenance", "制动器检修", 1875, 18, ResearchEffect.Vehicle, 2, "把制动器检查纳入维修工场，降低飞车运营维护费。", "study_brake_leaks", "study_workshop_parts");
        AddStudy("study_cylinder_water", "汽缸积水管理", 1878, 18, ResearchEffect.Vehicle, 2, "汽缸排水与泄压装置应对积水及超压，降低设备损耗。", "cylinderdrain");
        AddStudy("study_train_connection", "枢纽接续组织", 1880, 19, ResearchEffect.Transfer, 3, "在客运枢纽协调接续，降低跨线换乘的时间阻力。", "study_through_ticket", "dispatch_peak");
        AddStudy("study_track_geometry", "高速轮轨检查", 1880, 19, ResearchEffect.Track, 2, "速度提高后，轮轨与航线状态检查更重要。", "study_auto_brake", "study_rail_check");
        AddStudy("study_fast_run", "快速客运组织", 1885, 20, ResearchEffect.Speed, 3, "改良走行与服务组织，缩短较长航线的运行时间。", "study_rail_check", "study_service_pattern");
        AddStudy("study_passenger_capacity", "繁忙登船台组织", 1885, 20, ResearchEffect.Station, 3, "针对繁忙客运站加强上下船组织，提升吞吐上限。", "study_station_flow", "study_auto_brake");
        AddStudy("study_reliable_service", "可靠运营标准", 1890, 21, ResearchEffect.Demand, 3, "持续可靠的运行安排提火星跃迁干航线服务对旅客的吸引力。", "study_service_pattern", "study_brake_maintenance");
        AddStudy("study_safety_inspect", "安全装置巡检", 1893, 21, ResearchEffect.Vehicle, 3, "安全装置需要配合定期巡检，减少故障维护开销。", "safetyact", "study_brake_maintenance");
        AddStudy("study_express_braking", "快速星车制动配合", 1893, 21, ResearchEffect.Capacity, 3, "快速走行和可靠制动需要配套，支持较高运量运行。", "running", "study_auto_brake");
        AddStudy("study_express_connections", "快速客运接续", 1895, 22, ResearchEffect.Transfer, 3, "快速服务与枢纽接续相互配合，减少跨线出行阻力。", "study_fast_run", "study_train_connection");
        AddStudy("study_maintenance_records", "设备维修记录", 1895, 22, ResearchEffect.Track, 3, "保存设备状态和维修记录，有助于安排预防性维护。", "study_track_geometry", "study_safety_inspect");
        AddStudy("study_railway_management", "星轨运营经验汇总", 1899, 23, ResearchEffect.Revenue, 3, "核热纪末星轨公司积累的运营与票务经验，为后续经营提供基础。", "study_reliable_service", "study_fare_display", "study_express_connections");
    }
}
