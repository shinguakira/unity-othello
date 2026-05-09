"""Combine numbered PNG frames in a directory into an animated GIF.

Usage:
    python frames-to-gif.py <frames_dir> <output_gif> [--fps N] [--width W]

Defaults: fps=8 (150ms/frame), width=360 (height auto from aspect).
"""
import argparse
import sys
from pathlib import Path

from PIL import Image


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("frames_dir", type=Path)
    p.add_argument("output_gif", type=Path)
    p.add_argument("--fps", type=int, default=8)
    p.add_argument("--width", type=int, default=360)
    args = p.parse_args()

    if not args.frames_dir.is_dir():
        print(f"frames dir does not exist: {args.frames_dir}", file=sys.stderr)
        return 1

    files = sorted(args.frames_dir.glob("frame_*.png"))
    if not files:
        print(f"no frame_*.png found in {args.frames_dir}", file=sys.stderr)
        return 1
    print(f"found {len(files)} frames")

    images = []
    for f in files:
        img = Image.open(f).convert("RGB")
        ratio = args.width / img.width
        new_h = int(img.height * ratio)
        img = img.resize((args.width, new_h), Image.LANCZOS)
        # GIF needs P-mode (palette); quantize.
        img = img.quantize(colors=128, method=Image.MEDIANCUT)
        images.append(img)

    duration_ms = int(1000 / args.fps)
    images[0].save(
        args.output_gif,
        save_all=True,
        append_images=images[1:],
        duration=duration_ms,
        loop=0,
        optimize=True,
        disposal=2,
    )
    print(f"wrote {args.output_gif} ({len(images)} frames @ {args.fps} fps)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
