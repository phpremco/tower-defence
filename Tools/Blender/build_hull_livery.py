"""Round 37: the mothership's hull livery for the orbit shot.

The NTI emblem (ArtSource/Exports/Brand/NTI_Logo.png) beside a large "NTI" wordmark and the full company name, white on a dark
navy panel with a white border, so the logo reads from the orbit lens at 720p as well as 1080p (the emblem's own letters are
far too small at that distance). Plain Python with Pillow (not Blender): python -I Tools/Blender/build_hull_livery.py [--unity]
Writes ArtSource/Exports/Brand/NTI_Hull_Livery.png and with --unity (Play Mode stopped) copies it to Assets/_Project/Art/Textures/Brand/; Unity makes the .meta.
"""
import os
import shutil
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
BRAND = os.path.join(ROOT, "ArtSource", "Exports", "Brand")
FONTS = os.path.join(ROOT, "Assets", "_Project", "Art", "UI", "Fonts")
OUT = os.path.join(BRAND, "NTI_Hull_Livery.png")
UNITY = os.path.join(ROOT, "Assets", "_Project", "Art", "Textures", "Brand", "NTI_Hull_Livery.png")

W, H = 2048, 1024            # 2:1, the plate on the cargo frame (3.2 m by 1.6 m)
NAVY = (13, 22, 44, 255)
NAVY_EDGE = (22, 40, 82, 255)
WHITE = (240, 244, 248, 255)
BLUE = (60, 120, 230, 255)
BORDER = 30
INSET = 58


def text_box(draw, text, font):
    left, top, right, bottom = draw.textbbox((0, 0), text, font=font)
    return left, top, right - left, bottom - top


def main():
    img = Image.new("RGBA", (W, H), NAVY)
    draw = ImageDraw.Draw(img)
    # A white frame with chamfered corners and a thin blue line inside it: the panel's edge reads as a mounted plate.
    c = 70
    outer = [(c, 0), (W - c, 0), (W, c), (W, H - c), (W - c, H), (c, H), (0, H - c), (0, c)]
    draw.polygon(outer, fill=WHITE)
    b = BORDER
    inner = [(c + b * 0.4, b), (W - c - b * 0.4, b), (W - b, c + b * 0.4), (W - b, H - c - b * 0.4), (W - c - b * 0.4, H - b),
             (c + b * 0.4, H - b), (b, H - c - b * 0.4), (b, c + b * 0.4)]
    draw.polygon(inner, fill=NAVY)
    i = INSET
    draw.line([(i + 40, i), (W - i - 40, i), (W - i, i + 40), (W - i, H - i - 40), (W - i - 40, H - i), (i + 40, H - i), (i, H - i - 40),
               (i, i + 40), (i + 40, i)], fill=BLUE, width=8)

    # The emblem on the left, almost the full height.
    logo = Image.open(os.path.join(BRAND, "NTI_Logo.png")).convert("RGBA")
    logo = logo.crop(logo.getbbox())
    size = int(H * 0.74)
    logo = logo.resize((int(logo.width * size / logo.height), size), Image.LANCZOS)
    lx = 90
    img.alpha_composite(logo, (lx, (H - size) // 2))

    # The wordmark: "NTI" as large as the panel allows, the company name under it.
    right0 = lx + logo.width + 60
    span = W - right0 - 90
    bold = os.path.join(FONTS, "ChakraPetch-Bold.ttf")
    semi = os.path.join(FONTS, "ChakraPetch-SemiBold.ttf")
    big = ImageFont.truetype(bold, 620)
    left, top, tw, th = text_box(draw, "NTI", big)
    scale = min(1.0, span / tw)
    big = ImageFont.truetype(bold, int(620 * scale))
    left, top, tw, th = text_box(draw, "NTI", big)
    name_font = ImageFont.truetype(semi, 120)
    nl, nt, nw, nh = text_box(draw, "NOVA TERRA", name_font)
    s2 = min(1.0, span / nw)
    name_font = ImageFont.truetype(semi, int(120 * s2))
    nl, nt, nw, nh = text_box(draw, "NOVA TERRA", name_font)
    gap = 40
    block = th + gap + nh
    y0 = (H - block) // 2
    x = right0 + (span - tw) // 2
    draw.text((x - left, y0 - top), "NTI", font=big, fill=WHITE)
    rule_y = y0 + th + gap // 2
    draw.rectangle([right0 + 10, rule_y - 5, right0 + span - 10, rule_y + 5], fill=BLUE)
    draw.text((right0 + (span - nw) // 2 - nl, y0 + th + gap - nt), "NOVA TERRA", font=name_font, fill=WHITE)

    img.convert("RGB").save(OUT)
    print("wrote", OUT)
    if "--unity" in sys.argv:
        os.makedirs(os.path.dirname(UNITY), exist_ok=True)
        shutil.copyfile(OUT, UNITY)
        print("copied to", UNITY)


if __name__ == "__main__":
    main()
