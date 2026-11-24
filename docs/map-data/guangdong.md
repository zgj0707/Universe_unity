# 广东地图数据与示意处理

地图覆盖广东全省，以有铁路车站的县、县级市和市辖区为主要站点粒度。区县筛选的来源和局限见 [铁路站点核对](guangdong-rail-audit.md)。站点名称与中心点参考 [DataV 行政区划数据](https://datav.aliyun.com/tools/atlas/) 提取；东莞、中山按现有地级市站点呈现，未将其镇街误称为县级行政区。站点有小幅视觉避让，地图不是测绘图。

`Assets/Resources/MapData/guangdong_stations.json` 是游戏使用的站点数据；`Assets/Resources/MapArt/guangdong_illustrated.png` 是基于站点分布制作的手绘示意底图。地图不绘制行政区划界线，海岸和山纹仅作视觉示意。

澳门与内地交界站参考澳门特别行政区政府关于关闸、青茂、横琴、珠澳跨境工业区及港珠澳大桥陆路口岸的资料：https://www.gov.mo/zh-hans/news/929859/ 。跨区站点为玩法上的伪连接，广东画面中作了少量位置避让。
