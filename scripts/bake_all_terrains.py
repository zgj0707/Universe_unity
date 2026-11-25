#!/usr/bin/env python3
"""
全星域通用地形烘焙脚本：
为 月之亮面、月背、火星·西半球、火星·东半球 分别生成 TRN1 紧凑二进制与天堑叠加图层。
- 严格收紧硬性禁建区比例 (~1.5% - 2.0%)，并保护所有站点周围 52px 安全区，避免站点被天堑包围。
- 兼容月球 1080x1920 (270x480 网格) 与火星 2592x1920 (648x480 网格)。
"""
import os
import re
import struct
import numpy as np
from PIL import Image, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
BOOTSTRAP_CS = os.path.join(ROOT, "Assets", "Scripts", "DemoBootstrap.cs")
MARS_CS = os.path.join(ROOT, "Assets", "Scripts", "MarsSectorData.cs")
MAP_ART_DIR = os.path.join(ROOT, "Assets", "Resources", "MapArt")
MAP_DATA_DIR = os.path.join(ROOT, "Assets", "Resources", "MapData")

CANVAS_W = 1080.0
CANVAS_H = 1920.0
MAP_X, MAP_Y, MAP_W, MAP_H = 0.0, 173.0, 1080.0, 1574.0

REGIONS = [
    {
        "id": "月之亮面",
        "src": "moon_nearside.jpg",
        "out": "moon_nearside",
        "grid_w": 270,
        "grid_h": 480,
        "lon_min": -35.0,
        "lon_max": 35.0,
        "lat_min": -62.0,
        "lat_max": 62.0,
        "width_scale": 1.0,
        "offset_x": 0.0,
    },
    {
        "id": "月背",
        "src": "moon_farside.jpg",
        "out": "moon_farside",
        "grid_w": 270,
        "grid_h": 480,
        "lon_min": 145.0,
        "lon_max": 215.0,
        "lat_min": -62.0,
        "lat_max": 62.0,
        "width_scale": 1.0,
        "offset_x": 0.0,
    },
    {
        "id": "火星·西半球",
        "src": "mars_west.jpg",
        "out": "mars_west",
        "grid_w": 648,
        "grid_h": 480,
        "lon_min": -186.0,
        "lon_max": 6.0,
        "lat_min": -68.0,
        "lat_max": 68.0,
        "width_scale": 2.40,
        "offset_x": -0.70,
    },
    {
        "id": "火星·东半球",
        "src": "mars_east.jpg",
        "out": "mars_east",
        "grid_w": 648,
        "grid_h": 480,
        "lon_min": -6.0,
        "lon_max": 186.0,
        "lat_min": -68.0,
        "lat_max": 68.0,
        "width_scale": 2.40,
        "offset_x": -0.70,
    },
]


def clamp(v, lo, hi):
    return max(lo, min(hi, v))


def geo_to_norm(spec, lon, lat):
    adj_lon = lon
    if spec["id"] == "火星·西半球" and adj_lon > 6.0:
        adj_lon -= 360.0
    elif spec["id"] == "火星·东半球" and adj_lon < -6.0:
        adj_lon += 360.0
    u = (adj_lon - spec["lon_min"]) / (spec["lon_max"] - spec["lon_min"])
    v = (lat - spec["lat_min"]) / (spec["lat_max"] - spec["lat_min"])
    if spec["id"] in ("月之亮面", "月背"):
        return clamp(u, 0.06, 0.94), clamp(v, 0.08, 0.92)
    return spec["offset_x"] + u * spec["width_scale"], clamp(v, 0.04, 0.96)


def collect_region_stations():
    with open(BOOTSTRAP_CS, "r", encoding="utf-8") as f:
        boot_txt = f.read()
    with open(MARS_CS, "r", encoding="utf-8") as f:
        mars_txt = f.read()

    by_region = {r["id"]: [] for r in REGIONS}
    specs = {r["id"]: r for r in REGIONS}

    # 月之亮面
    for m in re.finditer(r'AddMoonNear\("[^"]+",\s*([-\d.]+)f,\s*([-\d.]+)f\)', boot_txt):
        by_region["月之亮面"].append(geo_to_norm(specs["月之亮面"], float(m.group(1)), float(m.group(2))))
    for m in re.finditer(r'MoonNearX\(([-\d.]+)f\),\s*MoonNearY\(([-\d.]+)f\)', boot_txt):
        by_region["月之亮面"].append(geo_to_norm(specs["月之亮面"], float(m.group(1)), float(m.group(2))))
    for m in re.finditer(r'MoonNearX\(([-\d.]+)f\),\s*MoonNearY\(([-\d.]+)f\)', mars_txt):
        by_region["月之亮面"].append(geo_to_norm(specs["月之亮面"], float(m.group(1)), float(m.group(2))))

    # 月背
    for m in re.finditer(r'AddMoonFar(?:Gateway)?\("[^"]+",\s*([-\d.]+)f,\s*([-\d.]+)f', boot_txt):
        by_region["月背"].append(geo_to_norm(specs["月背"], float(m.group(1)), float(m.group(2))))
    for m in re.finditer(r'MoonFarX\(([-\d.]+)f\),\s*MoonFarY\(([-\d.]+)f\)', boot_txt):
        by_region["月背"].append(geo_to_norm(specs["月背"], float(m.group(1)), float(m.group(2))))

    # 火星东西半球（MarsSectorData.cs 中按函数块划分且使用局部变量 R 传参）
    west_block = mars_txt.split("void BuildWestHemisphereStations()")[1].split("void BuildEastHemisphereStations()")[0]
    east_block = mars_txt.split("void BuildEastHemisphereStations()")[1]
    for m in re.finditer(r'RegisterMarsStation\([^,]+,\s*"[^"]+",\s*"[^"]+",\s*\d+,\s*([-\d.]+)f,\s*([-\d.]+)f', west_block):
        by_region["火星·西半球"].append(geo_to_norm(specs["火星·西半球"], float(m.group(1)), float(m.group(2))))
    for m in re.finditer(r'RegisterMarsStation\([^,]+,\s*"[^"]+",\s*"[^"]+",\s*\d+,\s*([-\d.]+)f,\s*([-\d.]+)f', east_block):
        by_region["火星·东半球"].append(geo_to_norm(specs["火星·东半球"], float(m.group(1)), float(m.group(2))))
    # 特殊星门
    by_region["火星·西半球"].append(geo_to_norm(specs["火星·西半球"], -134.5, 20.5))
    by_region["火星·西半球"].append(geo_to_norm(specs["火星·西半球"], -86.0, 51.5))
    by_region["火星·西半球"].append(geo_to_norm(specs["火星·西半球"], -2.5, -47.0))
    by_region["火星·东半球"].append(geo_to_norm(specs["火星·东半球"], -2.5, -47.0))

    return {k: list(dict.fromkeys(v)) for k, v in by_region.items()}


def bake_region(spec, stations_norm):
    src_path = os.path.join(MAP_ART_DIR, spec["src"])
    out_bytes = os.path.join(MAP_DATA_DIR, spec["out"] + "_terrain.bytes")
    out_overlay = os.path.join(MAP_DATA_DIR, spec["out"] + "_hazard_overlay.png")

    gw, gh = spec["grid_w"], spec["grid_h"]
    img = Image.open(src_path).convert("L").transpose(Image.FLIP_TOP_BOTTOM)

    smooth = img.filter(ImageFilter.GaussianBlur(radius=4.0))
    elev = np.asarray(smooth.resize((gw, gh), Image.Resampling.BILINEAR), dtype=np.float32)

    gy, gx = np.gradient(elev)
    slope = np.hypot(gx, gy)

    fine = np.asarray(img.resize((gw, gh), Image.Resampling.BILINEAR), dtype=np.float32)
    local_mean = np.asarray(
        img.filter(ImageFilter.GaussianBlur(radius=16.0)).resize((gw, gh), Image.Resampling.BILINEAR),
        dtype=np.float32,
    )
    rift_depth = local_mean - fine

    # 仅保留最极端的 ~1.7% 深裂谷/绝壁作为硬性禁建区，避免大片包围站点
    flags = np.zeros((gh, gw), dtype=np.uint8)
    slope_steep_th = float(np.percentile(slope, 82.0))
    slope_hard_th = float(np.percentile(slope, 98.4))
    rift_hard_th = float(np.percentile(rift_depth, 98.9))

    steep_mask = (slope >= slope_steep_th) | (elev >= 170.0)
    hard_mask = (slope >= slope_hard_th) | ((rift_depth >= rift_hard_th) & (elev < 125.0))

    flags[steep_mask] |= 1
    flags[hard_mask] |= 2

    # 所有站点周围 52px (13 格) 安全区强制清除硬性禁建区
    yy, xx = np.mgrid[0:gh, 0:gw]
    tex_min_x = spec["offset_x"] * CANVAS_W
    tex_w = spec["width_scale"] * CANVAS_W
    for nx, ny in stations_norm:
        cx_px = MAP_X + nx * MAP_W
        cy_px = MAP_Y + ny * MAP_H
        gx_pt = (cx_px - tex_min_x) / tex_w * gw
        gy_pt = cy_px / CANVAS_H * gh
        r2 = (xx - gx_pt) ** 2 + (yy - gy_pt) ** 2
        flags[r2 <= (13.0 ** 2)] &= ~np.uint8(2)

    elev_u8 = np.clip(elev, 0, 255).astype(np.uint8)
    interleaved = np.stack([elev_u8, flags], axis=-1).tobytes()
    with open(out_bytes, "wb") as f:
        f.write(b"TRN1")
        f.write(struct.pack("<HH", gw, gh))
        f.write(interleaved)

    rgba = np.zeros((gh, gw, 4), dtype=np.uint8)
    steep_only = (flags & 1) != 0
    rgba[steep_only] = [235, 168, 72, 32]
    hard_only = (flags & 2) != 0
    rgba[hard_only] = [225, 58, 48, 130]
    stripe = ((xx + yy) % 4) == 0
    rgba[hard_only & stripe] = [255, 95, 78, 185]

    overlay_img = Image.fromarray(rgba, mode="RGBA").transpose(Image.FLIP_TOP_BOTTOM)
    overlay_img = overlay_img.resize((gw * 2, gh * 2), Image.Resampling.BILINEAR)
    overlay_img.save(out_overlay, optimize=True)

    print(
        f"[{spec['id']}] stations={len(stations_norm)}, grid={gw}x{gh}, "
        f"bytes={os.path.getsize(out_bytes)}, steep={np.mean(steep_only)*100:.2f}%, "
        f"hard={np.mean(hard_only)*100:.2f}%"
    )


def main():
    os.makedirs(MAP_DATA_DIR, exist_ok=True)
    stations_by_reg = collect_region_stations()
    for spec in REGIONS:
        bake_region(spec, stations_by_reg[spec["id"]])


if __name__ == "__main__":
    main()
