#!/usr/bin/env python3
"""
将 moon_nearside.jpg (1080x1920) 离线烘焙为紧凑地形数据 (TRN1 二进制) 与天堑叠加图层。
坐标系约定：画布左下角为 (0, 0)，右上角为 (1080, 1920)，二进制行序按 y=0 (底) -> y=height-1 (顶) 存储，
运行时可直接用 canvasPos.y / 1920 * height 索引，零翻转开销。
"""
import os
import re
import struct
import numpy as np
from PIL import Image, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
SRC_IMG = os.path.join(ROOT, "Assets", "Resources", "MapArt", "moon_nearside.jpg")
BOOTSTRAP_CS = os.path.join(ROOT, "Assets", "Scripts", "DemoBootstrap.cs")
OUT_BYTES = os.path.join(ROOT, "Assets", "Resources", "MapData", "moon_nearside_terrain.bytes")
OUT_OVERLAY = os.path.join(ROOT, "Assets", "Resources", "MapData", "moon_nearside_hazard_overlay.png")

GRID_W = 270
GRID_H = 480
CANVAS_W = 1080.0
CANVAS_H = 1920.0
MAP_X, MAP_Y, MAP_W, MAP_H = 0.0, 173.0, 1080.0, 1574.0


def moon_near_x(lon):
    return max(0.06, min(0.94, (lon + 35.0) / 70.0))


def moon_near_y(lat):
    return max(0.08, min(0.92, (lat + 62.0) / 124.0))


def load_moon_near_stations():
    with open(BOOTSTRAP_CS, "r", encoding="utf-8") as f:
        text = f.read()
    pts = []
    for m in re.finditer(r'AddMoonNear\("[^"]+",\s*([-\d.]+)f,\s*([-\d.]+)f\)', text):
        pts.append((moon_near_x(float(m.group(1))), moon_near_y(float(m.group(2)))))
    for m in re.finditer(r'MoonNearX\(([-\d.]+)f\),\s*MoonNearY\(([-\d.]+)f\)', text):
        pts.append((moon_near_x(float(m.group(1))), moon_near_y(float(m.group(2)))))
    return list(dict.fromkeys(pts))


def main():
    os.makedirs(os.path.dirname(OUT_BYTES), exist_ok=True)
    img = Image.open(SRC_IMG).convert("L")
    # 图像文件 y=0 在顶部，先上下翻转使 y=0 对应画布底部 (Unity UV 原点)
    img_flip = img.transpose(Image.FLIP_TOP_BOTTOM)

    # 平滑后缩放到 270x480 作为高程场
    smooth = img_flip.filter(ImageFilter.GaussianBlur(radius=3.5))
    elev_img = smooth.resize((GRID_W, GRID_H), Image.Resampling.BILINEAR)
    elev = np.asarray(elev_img, dtype=np.float32)

    # 局部梯度（坡度）
    gy, gx = np.gradient(elev)
    slope = np.hypot(gx, gy)

    # 精细细节图用于识别深裂谷（局部极暗）与陡峭环形山脊（高梯度+高亮度）
    fine = np.asarray(img_flip.resize((GRID_W, GRID_H), Image.Resampling.BILINEAR), dtype=np.float32)
    local_mean = np.asarray(
        img_flip.filter(ImageFilter.GaussianBlur(radius=14.0)).resize((GRID_W, GRID_H), Image.Resampling.BILINEAR),
        dtype=np.float32,
    )
    rift_depth = local_mean - fine  # 正值越大越可能是深陷月溪/裂谷

    # flags: bit0(1)=陡坡惩罚带, bit1(2)=硬性禁建区(深裂谷/极陡峭山脊)
    flags = np.zeros((GRID_H, GRID_W), dtype=np.uint8)
    slope_steep_th = float(np.percentile(slope, 82.0))
    slope_hard_th = float(np.percentile(slope, 95.5))
    rift_hard_th = float(np.percentile(rift_depth, 97.5))

    steep_mask = (slope >= slope_steep_th) | (elev >= 165.0)
    hard_mask = (slope >= slope_hard_th) | ((rift_depth >= rift_hard_th) & (elev < 130.0))

    flags[steep_mask] |= 1
    flags[hard_mask] |= 2

    # 边缘留白与所有站点周围 32px 安全区清除硬性禁建区
    stations_norm = load_moon_near_stations()
    print(f"Protected MoonNear stations: {len(stations_norm)}")
    yy, xx = np.mgrid[0:GRID_H, 0:GRID_W]
    for sx, sy in stations_norm:
        cx = (MAP_X + sx * MAP_W) / CANVAS_W * GRID_W
        cy = (MAP_Y + sy * MAP_H) / CANVAS_H * GRID_H
        r2 = (xx - cx) ** 2 + (yy - cy) ** 2
        flags[r2 <= (8.5 ** 2)] &= ~np.uint8(2)

    # 写出 TRN1 二进制: magic(4) + width(uint16) + height(uint16) + interleaved [elev, flags]
    elev_u8 = np.clip(elev, 0, 255).astype(np.uint8)
    interleaved = np.stack([elev_u8, flags], axis=-1).tobytes()
    with open(OUT_BYTES, "wb") as f:
        f.write(b"TRN1")
        f.write(struct.pack("<HH", GRID_W, GRID_H))
        f.write(interleaved)

    # 生成叠加图层 (PNG 文件顶部为 y=max，需再翻回正常图像方向)
    rgba = np.zeros((GRID_H, GRID_W, 4), dtype=np.uint8)
    # 陡坡/高地：淡琥珀微光
    steep_only = (flags & 1) != 0
    rgba[steep_only] = [235, 168, 72, 34]
    # 硬性禁建区（裂谷/陡脊）：醒目朱红斜纹感半透明高亮
    hard_only = (flags & 2) != 0
    rgba[hard_only] = [225, 58, 48, 125]
    # 禁建区内部做轻量条纹强化辨识度
    stripe = ((xx + yy) % 4) == 0
    rgba[hard_only & stripe] = [255, 95, 78, 175]

    overlay_img = Image.fromarray(rgba, mode="RGBA").transpose(Image.FLIP_TOP_BOTTOM)
    overlay_img = overlay_img.resize((540, 960), Image.Resampling.BILINEAR)
    overlay_img.save(OUT_OVERLAY, optimize=True)

    hard_pct = float(np.mean(hard_only)) * 100.0
    steep_pct = float(np.mean(steep_only)) * 100.0
    print(f"Baked {OUT_BYTES} ({GRID_W}x{GRID_H}, {os.path.getsize(OUT_BYTES)} bytes)")
    print(f"Steep={steep_pct:.2f}%, HardBlocked={hard_pct:.2f}%")
    print(f"Overlay saved to {OUT_OVERLAY}")


if __name__ == "__main__":
    main()
