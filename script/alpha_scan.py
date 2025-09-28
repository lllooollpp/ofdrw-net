#!/usr/bin/env python3
"""
alpha_scan.py

扫描指定目录(递归可选)下的 PNG 图片，统计是否存在透明像素，并输出：
- 总像素数
- 透明像素数与比例
- 近白像素统计（可用于阈值调参）
- 可选：将判定为“近白”的像素转为透明并写出到输出目录（dry-run 时不写）

使用全局 Python 环境，不依赖虚拟环境。
依赖：Pillow
安装：pip install Pillow

示例：
  python ./src/script/alpha_scan.py -d ./src/mock3/Doc_0/Pages/Page_0 -v
  python ./src/script/alpha_scan.py -d ./tests --ext .png .PNG --recursive
  python ./src/script/alpha_scan.py -d ./src/mock3/Doc_0/Pages/Page_0 --make-white --white-threshold 252 --output ./_alpha_out

退出码：
  0 正常完成
  2 发生错误
"""
from __future__ import annotations
import os
import sys
import argparse
from dataclasses import dataclass
from typing import List, Tuple

try:
    from PIL import Image
except ImportError:
    print("[ERROR] 未找到 Pillow，请先执行: pip install Pillow", file=sys.stderr)
    sys.exit(2)

@dataclass
class ImageAlphaStats:
    path: str
    has_alpha_channel: bool
    total_pixels: int
    transparent_pixels: int
    white_like_pixels: int
    white_threshold: int

    @property
    def transparent_ratio(self) -> float:
        return self.transparent_pixels / self.total_pixels if self.total_pixels else 0.0

    @property
    def white_like_ratio(self) -> float:
        return self.white_like_pixels / self.total_pixels if self.total_pixels else 0.0


def is_white_like(r: int, g: int, b: int, threshold: int) -> bool:
    # 所有通道 >= threshold 判定为近白
    return r >= threshold and g >= threshold and b >= threshold


def scan_image(path: str, white_threshold: int) -> ImageAlphaStats:
    with Image.open(path) as im:
        mode = im.mode
        has_alpha = 'A' in mode or mode in ('LA', 'RGBA')
        # 转 RGBA 统一处理
        if im.mode != 'RGBA':
            im = im.convert('RGBA')
        pixels = im.getdata()
        total = len(pixels)
        transparent = 0
        white_like = 0
        for (r, g, b, a) in pixels:
            if a == 0:
                transparent += 1
            if is_white_like(r, g, b, white_threshold):
                white_like += 1
        return ImageAlphaStats(path, has_alpha, total, transparent, white_like, white_threshold)


def make_white_transparent(src: str, dst: str, white_threshold: int, preserve_border: bool = True, verbose: bool = False) -> Tuple[ImageAlphaStats, bool]:
    changed = False
    with Image.open(src) as im:
        if im.mode != 'RGBA':
            im = im.convert('RGBA')
        pixels = im.load()
        w, h = im.size
        transparent = 0
        white_like = 0
        total = w * h
        for y in range(h):
            for x in range(w):
                r, g, b, a = pixels[x, y]
                if is_white_like(r, g, b, white_threshold):
                    white_like += 1
                    if a != 0:
                        pixels[x, y] = (r, g, b, 0)
                        changed = True
                        transparent += 1
                elif a == 0:
                    transparent += 1
        stats = ImageAlphaStats(src, True, total, transparent, white_like, white_threshold)
        if preserve_border and changed:
            # 保留 1px 边框避免被完全裁掉，可按需要调整策略
            for x in range(w):
                for y in (0, h-1):
                    r, g, b, a = pixels[x, y]
                    if a == 0 and is_white_like(r, g, b, white_threshold):
                        pixels[x, y] = (r, g, b, 255)
            for y in range(h):
                for x in (0, w-1):
                    r, g, b, a = pixels[x, y]
                    if a == 0 and is_white_like(r, g, b, white_threshold):
                        pixels[x, y] = (r, g, b, 255)
        if changed:
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            im.save(dst, 'PNG')
            if verbose:
                print(f"[WRITE] {dst}")
    return stats, changed


def iter_image_files(root: str, exts: List[str], recursive: bool):
    for base, dirs, files in os.walk(root):
        for f in files:
            if any(f.lower().endswith(e.lower()) for e in exts):
                yield os.path.join(base, f)
        if not recursive:
            break


def main():
    parser = argparse.ArgumentParser(description='扫描 PNG 透明度/近白像素 (支持目录或单文件)')
    parser.add_argument('-d', '--dir', help='目标目录')
    parser.add_argument('-f', '--file', help='单个文件(可重复使用多个 -f)；与 -d 二选一', action='append')
    parser.add_argument('--ext', nargs='*', default=['.png'], help='匹配的文件扩展名，默认 .png')
    parser.add_argument('-r', '--recursive', action='store_true', help='递归扫描')
    parser.add_argument('-t', '--white-threshold', type=int, default=250, help='近白阈值(0-255)，默认 250')
    parser.add_argument('-v', '--verbose', action='store_true', help='详细输出')
    parser.add_argument('--make-white', action='store_true', help='把近白像素转透明并写出')
    parser.add_argument('--output', default='./_alpha_out', help='输出目录(用于 --make-white)')
    parser.add_argument('--no-border-preserve', action='store_true', help='不保留 1px 边框')
    parser.add_argument('--json', help='输出 JSON 汇总到指定文件')
    parser.add_argument('--csv', help='输出 CSV 明细到指定文件')
    parser.add_argument('--show-head', type=int, default=0, help='仅展示前 N 条文件级结果（调试用）')
    parser.add_argument('--filter-name', help='文件名包含该子串才统计')

    args = parser.parse_args()
    if not args.dir and not args.file:
        parser.error('需要 -d/--dir 或 -f/--file 至少一个')

    files: List[str] = []
    if args.file:
        for fp in args.file:
            if not os.path.isfile(fp):
                print(f"[WARN] 文件不存在: {fp}", file=sys.stderr)
                continue
            files.append(os.path.abspath(fp))
    if args.dir:
        root = args.dir
        if not os.path.isdir(root):
            print(f"[ERROR] 目录不存在: {root}", file=sys.stderr)
            return 2
        files.extend(iter_image_files(root, args.ext, args.recursive))

    if args.filter_name:
        files = [f for f in files if args.filter_name.lower() in os.path.basename(f).lower()]

    if not files:
        print('[INFO] 未找到匹配图片')
        return 0

    total_stats: List[ImageAlphaStats] = []
    changed_files = 0

    # 去重保持顺序
    seen = set()
    ordered_files = []
    for fp in files:
        if fp in seen: continue
        seen.add(fp)
        ordered_files.append(fp)

    # 明细列表
    row_details = []

    for fp in ordered_files:
        if args.make_white:
            # 若没有 root (单文件模式) 则直接平铺输出
            if args.dir and os.path.isdir(args.dir):
                try:
                    rel = os.path.relpath(fp, args.dir)
                except ValueError:
                    rel = os.path.basename(fp)
            else:
                rel = os.path.basename(fp)
            out_path = os.path.join(args.output, rel)
            stats, changed = make_white_transparent(fp, out_path, args.white_threshold, not args.no_border_preserve, args.verbose)
            if changed:
                changed_files += 1
        else:
            stats = scan_image(fp, args.white_threshold)
        total_stats.append(stats)
        # 补充更多 alpha 细节（min/max/ mid）
        try:
            from PIL import Image as _Img
            with _Img.open(fp) as _im:
                if _im.mode != 'RGBA':
                    _im = _im.convert('RGBA')
                data = _im.getdata()
                alphas = [a for (*_, a) in data]
                a_min = min(alphas) if alphas else 0
                a_max = max(alphas) if alphas else 0
                mid = sum(1 for a in alphas if 0 < a < 255)
        except Exception:
            a_min = a_max = mid = -1
        row_details.append({
            'path': fp,
            'has_alpha_channel': stats.has_alpha_channel,
            'total_pixels': stats.total_pixels,
            'transparent_pixels': stats.transparent_pixels,
            'transparent_ratio': stats.transparent_ratio,
            'white_like_pixels': stats.white_like_pixels,
            'white_like_ratio': stats.white_like_ratio,
            'white_threshold': stats.white_threshold,
            'alpha_min': a_min,
            'alpha_max': a_max,
            'alpha_mid_pixels': mid
        })
        if args.verbose:
            print(f"[SCAN] {fp} alpha={stats.has_alpha_channel} trans_ratio={stats.transparent_ratio:.4f} white_like={stats.white_like_ratio:.4f}")

    # 汇总
    total_pixels = sum(s.total_pixels for s in total_stats)
    total_transparent = sum(s.transparent_pixels for s in total_stats)
    total_white_like = sum(s.white_like_pixels for s in total_stats)

    if total_pixels:
        print('==== SUMMARY ====')
        print(f"Files: {len(total_stats)} Changed(write): {changed_files}")
        print(f"Total Pixels: {total_pixels}")
        print(f"Transparent Pixels: {total_transparent} ({total_transparent/total_pixels:.4%})")
        print(f"White-like Pixels: {total_white_like} ({total_white_like/total_pixels:.4%}) threshold={args.white_threshold}")
        if args.show_head > 0:
            print(f"---- HEAD {args.show_head} ----")
            for row in row_details[:args.show_head]:
                print(f"{os.path.basename(row['path'])} trans={row['transparent_ratio']:.4f} aMin={row['alpha_min']} aMax={row['alpha_max']} mid={row['alpha_mid_pixels']}")

    # JSON / CSV 输出
    if args.json:
        import json
        with open(args.json, 'w', encoding='utf-8') as jf:
            json.dump({'summary': {
                'files': len(total_stats),
                'changed_files': changed_files,
                'total_pixels': total_pixels,
                'transparent_pixels': total_transparent,
                'white_like_pixels': total_white_like,
                'transparent_ratio': (total_transparent/total_pixels) if total_pixels else 0,
                'white_like_ratio': (total_white_like/total_pixels) if total_pixels else 0,
                'white_threshold': args.white_threshold
            }, 'details': row_details}, jf, ensure_ascii=False, indent=2)
            print(f"[WRITE] JSON -> {args.json}")
    if args.csv:
        import csv
        with open(args.csv, 'w', newline='', encoding='utf-8') as cf:
            writer = csv.writer(cf)
            writer.writerow(['path','has_alpha_channel','total_pixels','transparent_pixels','transparent_ratio','white_like_pixels','white_like_ratio','white_threshold','alpha_min','alpha_max','alpha_mid_pixels'])
            for row in row_details:
                writer.writerow([row['path'],row['has_alpha_channel'],row['total_pixels'],row['transparent_pixels'],f"{row['transparent_ratio']:.6f}",row['white_like_pixels'],f"{row['white_like_ratio']:.6f}",row['white_threshold'],row['alpha_min'],row['alpha_max'],row['alpha_mid_pixels']])
            print(f"[WRITE] CSV -> {args.csv}")

    return 0

if __name__ == '__main__':
    try:
        code = main()
    except Exception as e:
        print(f"[ERROR] {e}", file=sys.stderr)
        code = 2
    sys.exit(code)
