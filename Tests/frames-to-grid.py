"""Lay out a sequence of PNG frames as a grid (contact sheet) PNG.

Usage:
    python frames-to-grid.py <frames_dir> <output_png> --cols N --pick K
"""
import argparse
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("frames_dir", type=Path)
    p.add_argument("output_png", type=Path)
    p.add_argument("--cols", type=int, default=4)
    p.add_argument("--pick", type=int, default=12, help="pick this many evenly-spaced frames")
    p.add_argument("--cell-width", type=int, default=300)
    args = p.parse_args()

    files = sorted(args.frames_dir.glob("frame_*.png"))
    if not files:
        print(f"no frames in {args.frames_dir}", file=sys.stderr)
        return 1

    # Pick K evenly-spaced frames
    if len(files) > args.pick:
        step = (len(files) - 1) / (args.pick - 1)
        idxs = [round(i * step) for i in range(args.pick)]
        files = [files[i] for i in idxs]

    cell_w = args.cell_width
    # Compute cell height from first image aspect
    first = Image.open(files[0])
    cell_h = int(first.height * cell_w / first.width)
    cols = args.cols
    rows = (len(files) + cols - 1) // cols

    label_h = 24
    pad = 6
    grid_w = cols * cell_w + (cols + 1) * pad
    grid_h = rows * (cell_h + label_h) + (rows + 1) * pad

    grid = Image.new("RGB", (grid_w, grid_h), color=(20, 22, 26))
    draw = ImageDraw.Draw(grid)
    try:
        font = ImageFont.truetype("arial.ttf", 16)
    except Exception:
        font = ImageFont.load_default()

    for i, f in enumerate(files):
        img = Image.open(f).convert("RGB").resize((cell_w, cell_h), Image.LANCZOS)
        col = i % cols
        row = i // cols
        x = pad + col * (cell_w + pad)
        y = pad + row * (cell_h + label_h + pad)
        grid.paste(img, (x, y))
        # frame label
        draw.text((x + 4, y + cell_h + 2), f"{i+1:02d} / {len(files)}", fill=(200, 200, 200), font=font)

    grid.save(args.output_png, optimize=True)
    print(f"wrote {args.output_png} ({len(files)} frames in {cols}x{rows})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
