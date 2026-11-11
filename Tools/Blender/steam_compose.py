"""Steam store and library images (2026-11-10): cuts the key-art masters (steam_keyart.py) to every Steam slot and puts
the approved CrysiumField logo on the formats that carry a title. Plain Python with Pillow (no Blender needed).

Logo sources:
- ArtSource/Previews/Logo/v4/CrysiumField_library_logo.png  full lockup (FAR HARVEST + DEEPWAKE), alpha
- ArtSource/Previews/Steam/_masters/logo_noseries_alpha.png  DEEPWAKE only, alpha (steam_logo_assets.py)
- ArtSource/Previews/Steam/_masters/emblem_alpha_1024.png    the emblem, alpha (steam_logo_assets.py)
- ArtSource/Previews/Logo/v4/CrysiumField_emblem_512.png     the emblem on its scene (icons)

Run: python Tools/Blender/steam_compose.py [--sheet-only]
Out: ArtSource/Previews/Steam/*.png, *.jpg, *.ico, Steam_contact_sheet.png
"""

import os
import sys

from PIL import Image, ImageChops, ImageDraw, ImageEnhance, ImageFilter, ImageFont

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
STEAM = os.path.join(REPO, "ArtSource", "Previews", "Steam")
MASTERS = os.path.join(STEAM, "_masters")
LOGO_V4 = os.path.join(REPO, "ArtSource", "Previews", "Logo", "v4")
FONT = os.path.join(REPO, "ArtSource", "Previews", "Logo", "v2", "_fonts", "michroma_Michroma-Regular.ttf")
LANCZOS = Image.Resampling.LANCZOS


def load(path):
    return Image.open(path).convert("RGBA")


def master(name):
    sfx = "_draft" if "--draft" in sys.argv else ""
    return load(os.path.join(MASTERS, f"keyart_{name}{sfx}.png"))


def trimmed(img, pad=0.0):
    """Crop to the alpha's bounding box (faint bloom below 3% ignored), with a relative margin."""
    a = img.getchannel("A").point(lambda v: 255 if v > 8 else 0)
    x0, y0, x1, y1 = a.getbbox()
    px, py = int((x1 - x0) * pad), int((y1 - y0) * pad)
    return img.crop((max(0, x0 - px), max(0, y0 - py), min(img.width, x1 + px), min(img.height, y1 + py)))


def cover(img, w, h, focus=(0.5, 0.5)):
    """Scale to cover w x h, then crop round `focus` (fractions of the source)."""
    s = max(w / img.width, h / img.height)
    big = img.resize((max(w, round(img.width * s)), max(h, round(img.height * s))), LANCZOS)
    cx, cy = focus[0] * big.width, focus[1] * big.height
    x0 = int(min(max(cx - w / 2, 0), big.width - w))
    y0 = int(min(max(cy - h / 2, 0), big.height - h))
    return big.crop((x0, y0, x0 + w, y0 + h))


def scrim(size, box, strength=0.6, feather=0.35):
    """A soft dark pool behind the logo box so the title reads over the battle (not a hard panel)."""
    w, h = size
    x0, y0, x1, y1 = box
    m = Image.new("L", size, 0)
    d = ImageDraw.Draw(m)
    fx, fy = (x1 - x0) * 0.08, (y1 - y0) * 0.12
    d.ellipse((x0 - fx, y0 - fy, x1 + fx, y1 + fy), fill=int(255 * strength))
    m = m.filter(ImageFilter.GaussianBlur(max(x1 - x0, y1 - y0) * feather * 0.5))
    layer = Image.new("RGBA", size, (2, 3, 10, 0))
    layer.putalpha(m)
    return layer


def put_logo(img, logo, cx, cy, width, shadow=0.7, scrim_strength=0.55):
    """Logo centred at (cx, cy) in pixels, `width` pixels wide, over a scrim and a tight dark shadow."""
    out = img.copy()
    lw = int(width)
    lh = int(logo.height * lw / logo.width)
    lg = logo.resize((lw, lh), LANCZOS)
    x, y = int(cx - lw / 2), int(cy - lh / 2)
    if scrim_strength:
        out = Image.alpha_composite(out, scrim(out.size, (x, y, x + lw, y + lh), scrim_strength))
    if shadow:
        sh = Image.new("RGBA", out.size, (0, 0, 0, 0))
        a = Image.new("L", out.size, 0)
        a.paste(lg.getchannel("A"), (x, y + max(1, lh // 60)))
        a = a.filter(ImageFilter.GaussianBlur(max(1.0, lh / 40))).point(lambda v: int(v * shadow))
        sh.putalpha(a)
        out = Image.alpha_composite(out, sh)
    layer = Image.new("RGBA", out.size, (0, 0, 0, 0))
    layer.paste(lg, (x, y), lg)
    return Image.alpha_composite(out, layer)


def vignette(img, strength=0.45):
    w, h = img.size
    m = Image.new("L", (w, h), 0)
    ImageDraw.Draw(m).ellipse((-w * 0.15, -h * 0.2, w * 1.15, h * 1.2), fill=255)
    m = m.filter(ImageFilter.GaussianBlur(min(w, h) * 0.18))
    dark = Image.new("RGBA", (w, h), (0, 0, 4, 255))
    inv = ImageChops.invert(m).point(lambda v: int(v * strength))
    return Image.composite(dark, img, inv)


def edge_falloff(img, left=0.35, right=0.5, span=0.2):
    """Darkens the outer `span` of each side (round 4: the light gathers on the focus, the edges recede)."""
    w, h = img.size
    row = []
    for x in range(w):
        t = x / (w - 1)
        a = left * (1 - t / span) if t < span else right * (t - (1 - span)) / span if t > 1 - span else 0.0
        row.append(int(255 * a))
    line = Image.new("L", (w, 1))
    line.putdata(row)
    dark = Image.new("RGBA", (w, h), (1, 2, 8, 255))
    return Image.composite(dark, img, line.resize((w, h)))


def logo_pool(img, centre, radius, strength):
    """A soft dark pool (no text) where Steam lays the library logo, so the logo reads over the hero."""
    w, h = img.size
    m = Image.new("L", (w, h), 0)
    cx, cy = centre[0] * w, centre[1] * h
    rx, ry = radius[0] * w, radius[1] * h
    ImageDraw.Draw(m).ellipse((cx - rx, cy - ry, cx + rx, cy + ry), fill=int(255 * strength))
    m = m.filter(ImageFilter.GaussianBlur(min(rx, ry) * 0.6))
    dark = Image.new("RGBA", (w, h), (2, 3, 10, 255))
    return Image.composite(dark, img, m)


def icon_base(emblem, n):
    bg = Image.new("RGBA", (n, n), (6, 8, 18, 255))
    glow = Image.new("L", (n, n), 0)
    ImageDraw.Draw(glow).ellipse((n * 0.2, n * 0.2, n * 0.8, n * 0.8), fill=150)
    glow = glow.filter(ImageFilter.GaussianBlur(n * 0.12))
    tint = Image.new("RGBA", (n, n), (120, 40, 110, 255))
    tint.putalpha(glow)
    bg = Image.alpha_composite(bg, tint)
    e = emblem.resize((int(n * 0.9), int(n * 0.9 * emblem.height / emblem.width)), LANCZOS)
    bg.alpha_composite(e, ((n - e.width) // 2, (n - e.height) // 2))
    return bg


def save(img, name, jpg=False):
    path = os.path.join(STEAM, name)
    rgb = img.convert("RGB")
    if jpg:
        rgb.save(path, quality=93)
    else:
        rgb.save(path, optimize=True)
    print("wrote", path, img.size)
    return path


# =============================================================================================================
# Slots
# =============================================================================================================

def build_all():
    os.makedirs(STEAM, exist_ok=True)
    full = trimmed(load(os.path.join(LOGO_V4, "CrysiumField_library_logo.png")), 0.02)
    title = trimmed(load(os.path.join(MASTERS, "logo_noseries_alpha.png")), 0.02)
    emblem = trimmed(load(os.path.join(MASTERS, "emblem_alpha_1024.png")), 0.01)
    wide, std, header, vert = master("wide"), master("std"), master("header"), master("vert")
    L = LAYOUT
    made = {}

    # Store: header capsule and library header (920x430, logo).
    h = cover(header, 920, 430, L["header_focus"])
    h = put_logo(vignette(h, 0.35), full, *L["header_logo"])
    made["header"] = save(h, "Steam_header_capsule_920x430.png")
    made["lib_header"] = save(h, "Steam_library_header_920x430.png")

    # Store: small capsule (462x174): the title alone, filling most of the frame.
    s = cover(header, 462, 174, L["small_focus"])
    s = ImageEnhance.Brightness(s).enhance(0.8)
    s = put_logo(vignette(s, 0.4), title, 231, 87, 462 * 0.9, scrim_strength=0.7)
    made["small"] = save(s, "Steam_small_capsule_462x174.png")

    # Store: main capsule (1232x706, logo).
    m = cover(std, 1232, 706, L["main_focus"])
    m = put_logo(vignette(m, 0.35), full, *L["main_logo"])
    made["main"] = save(m, "Steam_main_capsule_1232x706.png")

    # Store: vertical capsule (748x896) and library capsule (600x900): the lockup in the sky above the boss (an
    # emblem above it pushed the title down onto Gravemaw's spines).
    for key, (w, hh), fname in (("vcap", (748, 896), "Steam_vertical_capsule_748x896.png"),
                                ("libcap", (600, 900), "Steam_library_capsule_600x900.png")):
        v = cover(vert, w, hh, L[key + "_focus"])
        v = vignette(v, 0.35)
        lw = w * 0.9
        lh = full.height * lw / full.width
        top = hh * L[key + "_top"]
        v = put_logo(v, full, w / 2, top + lh / 2, lw, scrim_strength=0.6)
        made[key] = save(v, fname)

    # Store: page background (1438x810): ambient, low contrast, no logo; Steam fades it into the page.
    bg = cover(std, 1438, 810, L["bg_focus"]).filter(ImageFilter.GaussianBlur(3))
    bg = ImageEnhance.Contrast(ImageEnhance.Color(bg).enhance(0.7)).enhance(0.6)
    bg = ImageEnhance.Brightness(bg).enhance(0.7)
    made["bg"] = save(vignette(bg, 0.6), "Steam_page_background_1438x810.png")

    # Library hero (3840x1240): artwork only, no text. Edges recede, and the bottom-left corner (where we set the
    # library logo in Steamworks) is a calm, darker pool.
    hero = edge_falloff(cover(wide, 3840, 1240), *L["hero_edges"])
    hero = logo_pool(hero, *L["hero_logo_pool"])
    made["hero"] = save(hero, "Steam_library_hero_3840x1240.png")

    # Library logo (1280x720, alpha): the v4 lockup as approved.
    lib = load(os.path.join(LOGO_V4, "CrysiumField_library_logo.png"))
    p = os.path.join(STEAM, "Steam_library_logo_1280x720.png")
    lib.save(p, optimize=True)
    made["lib_logo"] = p
    print("wrote", p, lib.size)

    # Event cover (800x450) and event header (1920x622).
    e = cover(std, 800, 450, L["event_focus"])
    made["ev_cover"] = save(put_logo(vignette(e, 0.35), full, *L["event_logo"]), "Steam_event_cover_800x450.png")
    eh = edge_falloff(cover(wide, 1920, 622, L["evh_focus"]), *L["hero_edges"])
    made["ev_header"] = save(put_logo(vignette(eh, 0.3), full, *L["evh_logo"]), "Steam_event_header_1920x622.png")

    # Icons: the alpha emblem on a dark field with a faint Crysium glow behind it (reads cleaner at 32 px than the
    # emblem's busy scene background).
    icon_src = icon_base(emblem, 1024)
    for n in (256, 184, 64, 32):
        ic = icon_src.resize((n, n), LANCZOS)
        if n <= 64:
            ic = ImageEnhance.Sharpness(ic).enhance(1.4)
        made[f"icon{n}"] = save(ic, f"Steam_icon_{n}.png")
    save(icon_src.resize((184, 184), LANCZOS), "Steam_app_icon_184.jpg", jpg=True)
    ico = os.path.join(STEAM, "Steam_shortcut_icon.ico")
    icon_src.resize((256, 256), LANCZOS).save(
        ico, sizes=[(256, 256), (64, 64), (48, 48), (32, 32), (16, 16)])
    print("wrote", ico)
    return made


# Per-slot framing: focus points are fractions of the master; logos are (cx, cy, width) in pixels.
LAYOUT = dict(
    header_focus=(0.5, 0.5), header_logo=(460, 60, 500),
    small_focus=(0.5, 0.55),
    main_focus=(0.5, 0.5), main_logo=(616, 92, 690),
    vcap_focus=(0.5, 0.5), vcap_top=0.05,
    # Round 5: Spencer flies in from the right; the 600-wide crop of the vert master leans right so he stays whole.
    libcap_focus=(0.58, 0.5), libcap_top=0.06,
    bg_focus=(0.5, 0.5),
    event_focus=(0.5, 0.5), event_logo=(400, 72, 480),
    evh_focus=(0.5, 0.5), evh_logo=(470, 118, 760),
    # Round 5: the maw sits left of centre, so the logo pool is smaller and tucked into the corner (it must not
    # darken the petals); the logo is set at about 23% of the hero width there (HERO_LOGO_SCALE).
    hero_edges=(0.35, 0.55, 0.22), hero_logo_pool=((0.14, 0.75), (0.15, 0.22), 0.55),
)


# =============================================================================================================
# Contact sheet
# =============================================================================================================

# The library logo over the hero as we set it in Steamworks (left bottom): 0.7 x 1280 = 896 px, about 23% of the hero
# width, so it stays clear of the maw (round 5).
HERO_LOGO_SCALE = 0.7


def contact_sheet():
    files = [
        ("Header capsule 920x430", "Steam_header_capsule_920x430.png", 1.0),
        ("Main capsule 1232x706", "Steam_main_capsule_1232x706.png", 0.75),
        ("Small capsule 462x174", "Steam_small_capsule_462x174.png", 1.0),
        ("Vertical capsule 748x896", "Steam_vertical_capsule_748x896.png", 0.5),
        ("Library capsule 600x900", "Steam_library_capsule_600x900.png", 0.5),
        ("Library header 920x430", "Steam_library_header_920x430.png", 0.5),
        ("Library hero 3840x1240 (safe area marked)", "Steam_library_hero_3840x1240.png", 0.42),
        ("Library logo 1280x720 (alpha, over the hero)", "Steam_library_logo_1280x720.png", 0.42),
        ("Page background 1438x810", "Steam_page_background_1438x810.png", 0.5),
        ("Event cover 800x450", "Steam_event_cover_800x450.png", 0.6),
        ("Event header 1920x622", "Steam_event_header_1920x622.png", 0.42),
        ("Icons 256 / 184 / 64 / 32", None, 1.0),
    ]
    font = ImageFont.truetype(FONT, 15) if os.path.exists(FONT) else ImageFont.load_default()
    pad, W = 28, 1680
    cells = []
    for label, name, sc in files:
        if name is None:
            row = Image.new("RGBA", (256 + 184 + 64 + 32 + 3 * 20, 256), (0, 0, 0, 0))
            x = 0
            for n in (256, 184, 64, 32):
                ic = load(os.path.join(STEAM, f"Steam_icon_{n}.png"))
                row.paste(ic, (x, 256 - n))
                x += n + 20
            cells.append((label, row))
            continue
        im = load(os.path.join(STEAM, name))
        if "hero" in name:
            im = im.copy()
            d = ImageDraw.Draw(im)
            cx, cy = im.width / 2, im.height / 2
            d.rectangle((cx - 430, cy - 190, cx + 430, cy + 190), outline=(90, 200, 255, 255), width=6)
        if "library_logo" in name:
            hero = load(os.path.join(STEAM, "Steam_library_hero_3840x1240.png"))
            # Steam's "left bottom" logo placement, roughly as the client shows it.
            lg = im.resize((int(im.width * HERO_LOGO_SCALE), int(im.height * HERO_LOGO_SCALE)), LANCZOS)
            hero.alpha_composite(lg, (120, hero.height - lg.height - 40))
            im = hero
        im = im.resize((int(im.width * sc), int(im.height * sc)), LANCZOS)
        cells.append((label, im))
    # Flow layout.
    rows, row, x = [], [], pad
    for label, im in cells:
        if x + im.width + pad > W and row:
            rows.append(row)
            row, x = [], pad
        row.append((label, im))
        x += im.width + pad
    rows.append(row)
    H = pad + sum(max(im.height for _, im in r) + 30 + pad for r in rows)
    sheet = Image.new("RGBA", (W, H), (10, 11, 16, 255))
    d = ImageDraw.Draw(sheet)
    y = pad
    for r in rows:
        x = pad
        for label, im in r:
            d.text((x, y), label, fill=(190, 200, 215, 255), font=font)
            sheet.alpha_composite(im, (x, y + 24))
            x += im.width + pad
        y += max(im.height for _, im in r) + 30 + pad
    save(sheet, "Steam_contact_sheet.png")


if __name__ == "__main__":
    if "--sheet-only" not in sys.argv:
        build_all()
    contact_sheet()
