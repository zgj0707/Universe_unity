# 澳门底图资料

澳门示意底图参考 OpenStreetMap 贡献者的海岸线数据，经过简化和风格化处理，来源为 Geofabrik 的 Macau 提取文件：<https://download.geofabrik.de/asia/china/macau.html>。数据适用 ODbL 1.0：<https://opendatacommons.org/licenses/odbl/1-0/>；地图画面标注 © OpenStreetMap contributors。

`macau-osm-extract.json` 保存了本图使用的提取数据；`build_macau_map.py` 从该文件生成游戏底图，需要 Python、Pillow、NumPy 和 Shapely。地图范围为经度 113.505–113.605、纬度 22.100–22.235；游戏按同一范围将站点坐标投影至底图。底图有意省略真实道路、桥梁和细碎海岸细节，不代表精确地理边界。数据提取范围以外的西侧珠海海岸使用延伸边界，并非实测海岸线。名胜标记为避免遮挡可点击车站，在地理位置附近做了少量视觉偏移。

生成的地图是游戏用示意图，不用于导航或精确测绘。

香港底图由 `build_hongkong_map.py` 生成：沿用原项目的简化岛屿轮廓（`hongkong-outline.json`），采用与澳门、广东一致的圆润海岸、浅海与淡色山纹。坐标为游戏示意，不作为实测地图；重生成时请在项目根目录运行脚本。
