#!/usr/bin/env python3
"""Generate Properties/Thumbnail.png — the Paradox Mods store thumbnail (512x512).

Run from the repo root:  python3 Tools/make_thumbnail.py
"""
import math
import os

from PIL import Image, ImageDraw, ImageFont

SIZE = 512
OUT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)),
                                    "..", "Properties", "Thumbnail.png"))


def load_font(size):
    for path in (
        "/System/Library/Fonts/Helvetica.ttc",
        "/System/Library/Fonts/HelveticaNeue.ttc",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
    ):
        try:
            return ImageFont.truetype(path, size)
        except Exception:
            continue
    return ImageFont.load_default()


img = Image.new("RGB", (SIZE, SIZE), (16, 20, 34))
d = ImageDraw.Draw(img)

# Vertical gradient background (deep navy -> plum).
top = (18, 22, 40)
bottom = (46, 28, 66)
for y in range(SIZE):
    t = y / (SIZE - 1)
    d.line([(0, y), (SIZE, y)], fill=(
        int(top[0] + (bottom[0] - top[0]) * t),
        int(top[1] + (bottom[1] - top[1]) * t),
        int(top[2] + (bottom[2] - top[2]) * t),
    ))

# Soul pulse line with a spike.
cy = 290
pts = []
for x in range(36, SIZE - 36):
    t = (x - 36) / (SIZE - 72)
    y = cy + 30 * math.sin(t * math.pi * 3.0) * math.exp(-2.0 * t)
    pts.append((x, y))
d.line(pts, fill=(255, 122, 89), width=7)
d.line([(SIZE // 2 - 8, cy), (SIZE // 2, cy - 32),
        (SIZE // 2 + 8, cy + 26), (SIZE // 2 + 18, cy)], fill=(255, 122, 89), width=7)


def center(text, font, y, fill):
    w = d.textlength(text, font=font)
    d.text(((SIZE - w) / 2, y), text, font=font, fill=fill)


center("CITIZEN", load_font(62), 84, (240, 240, 250))
center("SOUL", load_font(62), 152, (255, 122, 89))
center("Every citizen is a real person", load_font(23), 356, (205, 210, 230))
center("a Cities: Skylines II mod", load_font(21), 430, (198, 203, 226))

os.makedirs(os.path.dirname(OUT), exist_ok=True)
img.save(OUT)
print("wrote", OUT, img.size)
