"""Preview mockups for the round-25 UI set (`build_ui.py --only mockups`): an icon sheet at 1x/2x, a 1080p HUD, a
dialogue box, a main menu with focused/unfocused buttons and a sheet with the radar, halo, backdrop and starfield.

They are composed from the exported PNGs exactly as Unity would draw them (9-slice, tint by multiplication, 2x
masters shown at 0.75 for 1080p), with labels rendered from the game's own fonts by Blender text objects.
Nothing here is exported to the game.
"""

import json
import os

import bpy
import numpy as np

import ui_art as A

S = 0.75  # 2x master -> 1080p
SHOTS = os.path.join(os.environ.get("TD_SHOTS", r"C:\Users\remco\AppData\Local\Temp\claude\b18d9d7fa25a4d30"
                                    r"\2aa46f30-11b3-418c-a974-fe154b605af3\scratchpad"))
BODY_FONT = os.path.join(A.REPO, "Assets", "TextMesh Pro", "Fonts", "LiberationSans.ttf")

ICE = (0.91, 0.97, 1.0)
CYAN = (0.31, 0.82, 1.0)
SIGNAL = (1.0, 0.82, 0.25)
DIM = (0.6, 0.72, 0.82)
WARN = (1.0, 0.45, 0.25)


# ------------------------------------------------------------------------------------------------------- loading

def load(path):
    img = bpy.data.images.load(path, check_existing=False)
    img.colorspace_settings.name = "Non-Color"
    w, h = img.size
    buf = np.empty(w * h * img.channels, np.float32)
    img.pixels.foreach_get(buf)
    bpy.data.images.remove(img)
    a = buf.reshape(h, w, -1)[::-1].astype(np.float64)
    if a.shape[2] == 3:
        a = np.dstack([a, np.ones((h, w))])
    return a


class Atlas:
    def __init__(self, name):
        with open(os.path.join(A.OUT, name + ".json")) as fh:
            self.meta = json.load(fh)
        self.img = load(os.path.join(A.OUT, self.meta.get("texture_2x", self.meta["texture"])))
        self.k = 2 if "texture_2x" in self.meta else 1

    def get(self, name):
        e = self.meta["sprites"][name]
        k = self.k
        return self.img[e["y"] * k:(e["y"] + e["h"]) * k, e["x"] * k:(e["x"] + e["w"]) * k], e


# ------------------------------------------------------------------------------------------------------- text

def texts(items):
    """Renders [(key, string, px, font)] as white straight-alpha crops: {key: image}."""
    A.reset()
    fonts = {}
    coll = A.layer("text")
    _, flat = A._materials()
    y = 0.0
    placed = []
    for key, string, px, font in items:
        cu = bpy.data.curves.new(key, "FONT")
        cu.body = string
        if font not in fonts:
            fonts[font] = bpy.data.fonts.load(font)
        cu.font = fonts[font]
        cu.size = px
        cu.materials.append(flat)
        ob = bpy.data.objects.new(key, cu)
        ob.color = (1, 1, 1, 1)
        coll.objects.link(ob)
        placed.append((key, ob, px))
    bpy.context.view_layer.update()
    width = 0
    boxes = []
    for key, ob, px in placed:
        bb = [v[:] for v in ob.bound_box]
        x0, x1 = min(v[0] for v in bb), max(v[0] for v in bb)
        line = px * 1.35
        ob.location = (10 - x0, -(y + px * 1.05), 0)
        boxes.append((key, int(y), int(line + 2), int(x1 - x0 + 20)))
        width = max(width, x1 - x0 + 20)
        y += line + 8
    W, H = int(width) + 4, int(y) + 4
    for _, ob, _ in placed:
        ob.location.y += H
    A.camera(W, H)
    img = A.render(W, H, samples=16)
    out = {}
    for key, top, h, w in boxes:
        crop = img[top:top + h, :w].copy()
        crop[..., :3] = 1.0
        out[key] = crop
    return out


# ------------------------------------------------------------------------------------------------------- drawing

class Canvas:
    def __init__(self, w, h, bg=None):
        self.w, self.h = w, h
        self.px = np.zeros((h, w, 4))
        if bg is not None:
            self.px[..., :3] = bg[..., :3] if np.ndim(bg) == 3 else bg
            self.px[..., 3] = 1.0

    def sprite(self, img, x, y, scale=S, tint=(1, 1, 1), alpha=1.0, centre=False):
        im = A.scale_img(img, scale) if scale != 1 else img
        if centre:
            x, y = x - im.shape[1] // 2, y - im.shape[0] // 2
        A.paste(self.px, im, int(x), int(y), tint, alpha)
        return im.shape[1], im.shape[0]

    def sliced(self, entry, x, y, w, h, tint=(1, 1, 1), alpha=1.0, scale=S):
        """Draws a 9-slice kit sprite so that its *frame* (not the glow pad) fills x, y, w, h."""
        img, meta = entry
        pad = meta.get("glow_pad", 0) * scale
        im = A.scale_img(img, scale)
        border = [max(1, int(round(b * scale))) for b in meta["border_lbrt"]]
        W, H = int(w + 2 * pad), int(h + 2 * pad)
        if any(meta["border_lbrt"]):
            im = A.nine_slice(im, border, W, H)
        else:
            im = A.scale_img(img, W / img.shape[1])
        A.paste(self.px, im, int(x - pad), int(y - pad), tint, alpha)

    def text(self, img, x, y, tint=ICE, align="left", alpha=1.0):
        w = img.shape[1]
        if align == "centre":
            x -= w // 2
        elif align == "right":
            x -= w
        A.paste(self.px, img, int(x), int(y), tint, alpha)

    def shade(self, x0, y0, x1, y1, colour=(0, 0, 0), alpha=0.5):
        region = self.px[y0:y1, x0:x1]
        self.px[y0:y1, x0:x1] = A.over(region, A.solid(np.full(region.shape[:2], alpha), colour))

    def save(self, path):
        A.save_png(path, np.clip(self.px[..., :3], 0, 1))


def backdrop(w, h):
    shot = os.path.join(SHOTS, "imp4b_shots", "env_AshenReach_play.png")
    if os.path.exists(shot):
        img = load(shot)[64:570, 130:1030]
        img = A.resize(img, h, w)
        img[..., :3] *= 0.85
        return img
    return np.dstack([np.full((h, w), 0.05), np.full((h, w), 0.07), np.full((h, w), 0.1), np.ones((h, w))])


# ------------------------------------------------------------------------------------------------------- sheets

def icon_sheet(icons, path):
    names = list(icons.meta["sprites"])
    labels = texts([(n, n, 15, A.FONT_NARROW) for n in names] + [("t1", "UI_ICONS  -  white, tinted in code; 2x (96 px) / 1x (48 px) / 32 px HUD size", 22, A.FONT)])
    cols = 8
    cw, ch = 220, 200
    canvas = Canvas(cols * cw + 40, 5 * ch + 90, bg=np.array([0.035, 0.06, 0.1]))
    canvas.text(labels["t1"], 24, 18, ICE)
    tints = [ICE, CYAN, SIGNAL]
    for i, n in enumerate(names):
        cx, cy = 20 + (i % cols) * cw, 70 + (i // cols) * ch
        if (i // cols + i % cols) % 2 == 0:
            canvas.shade(cx + 4, cy, cx + cw - 4, cy + ch - 6, (0.08, 0.13, 0.2), 0.8)
        img, meta = icons.get(n)
        tint = (1, 1, 1) if meta["precoloured"] else tints[i % 3]
        canvas.sprite(img, cx + 12, cy + 14, scale=96 / img.shape[0] * (128 / 112), tint=tint)
        canvas.sprite(img, cx + 130, cy + 30, scale=48 / img.shape[0] * (128 / 112), tint=tint)
        canvas.sprite(img, cx + 140, cy + 96, scale=32 / img.shape[0] * (128 / 112), tint=tint)
        canvas.text(labels[n], cx + 12, cy + 150, DIM if i < 28 else SIGNAL)
    canvas.save(path)


def hud(kit, icons, path):
    W, H = 1920, 1080
    T = texts([
        ("base", "BASE 800/800", 26, A.FONT), ("wave", "WAVE 3/12", 34, A.FONT), ("next", "NEXT WAVE IN 25", 20, A.FONT),
        ("score", "SCORE 12 480", 22, A.FONT), ("money", "220", 34, A.FONT), ("hero", "HERO", 22, A.FONT),
        ("mercer", "Mercer", 20, A.FONT_NARROW), ("f", "F", 18, A.FONT), ("g", "30 G", 18, A.FONT), ("h", "H", 18, A.FONT),
        ("drones", "DRONES", 22, A.FONT), ("d1", "120  0/3", 18, A.FONT), ("d2", "150  0/2", 18, A.FONT), ("d3", "180  1/3", 18, A.FONT),
        ("p50", "50", 24, A.FONT), ("p100", "100", 24, A.FONT), ("p130", "130", 24, A.FONT), ("p220", "220", 24, A.FONT),
        ("gun", "GUN TURRET  50", 22, A.FONT), ("boss", "BROOD MOTHER", 20, A.FONT), ("bossc", "BOSS", 18, A.FONT),
        ("lb", "LB", 16, A.FONT), ("rb", "RB", 16, A.FONT)])
    c = Canvas(W, H, bg=backdrop(W, H))
    ic = {n: icons.get(n)[0] for n in icons.meta["sprites"]}
    isc = 1.0 / 2  # 2x icon (256 px cell) -> 128 at 1x; scaled per use below

    def icon(n, x, y, size, tint=ICE):
        c.sprite(ic[n], x, y, scale=size / ic[n].shape[0], tint=tint, centre=True)

    # Top bar: base left, wave centre (+ boss bar), score, speed and pause right.
    c.sliced(kit.get("Panel"), 24, 20, 380, 66)
    icon("Base", 62, 53, 46)
    c.text(T["base"], 94, 34)
    c.sliced(kit.get("Bar_Back"), 94, 66, 280, 10)
    c.sliced(kit.get("Bar_Green"), 94, 66, 280, 10)
    c.sliced(kit.get("Panel"), 760, 20, 400, 96)
    c.text(T["wave"], 960, 30, ICE, "centre")
    c.text(T["next"], 960, 78, SIGNAL, "centre")
    c.sliced(kit.get("Tooltip"), 740, 130, 440, 52)
    c.text(T["boss"], 960, 136, WARN, "centre")
    c.sliced(kit.get("Bar_Back"), 770, 162, 380, 12)
    c.sliced(kit.get("Bar_Red"), 770, 162, 260, 12)
    c.sliced(kit.get("PanelSquare"), 1180, 30, 214, 50)
    c.text(T["score"], 1287, 40, DIM, "centre")
    names = ["Speed", "SpeedDouble", "Pause", "SkipWave"]
    for i, n in enumerate(names):
        x = 1420 + i * 118
        state = "ButtonSquare_Focus" if i == 1 else "ButtonSquare_Normal"
        c.sliced(kit.get(state), x, 22, 96, 76)
        icon(n, x + 48, 60, 50, ICE)
    # Hero abilities on the left.
    c.sliced(kit.get("Panel"), 24, 300, 180, 470)
    icon("Hero", 58, 334, 38, CYAN)
    c.text(T["hero"], 82, 320)
    c.text(T["mercer"], 44, 356, DIM)
    c.sliced(kit.get("Bar_Back"), 44, 384, 140, 8)
    c.sliced(kit.get("Bar_Blue"), 44, 384, 120, 8)
    for i, (n, key, focus) in enumerate((("Shockwave", "f", False), ("Repair", "g", True), ("Hero", "h", False))):
        y = 414 + i * 116
        c.sliced(kit.get("ButtonSquare_Focus" if focus else "ButtonSquare_Normal"), 54, y, 96, 84)
        icon(n, 102, y + 42, 54, ICE)
        c.text(T[key], 160, y + 30, SIGNAL if focus else DIM)
    # Money bottom left.
    c.sliced(kit.get("Panel"), 24, 880, 260, 76)
    icon("Credits", 68, 918, 50, SIGNAL)
    c.text(T["money"], 108, 896, SIGNAL)
    # Tower bar bottom centre with prices.
    c.sliced(kit.get("Tooltip"), 820, 790, 280, 44)
    c.text(T["gun"], 960, 798, ICE, "centre")
    cards = [("Card_Selected", "Blast", "p50", ICE, ["Star", "Star"]), ("Card_Blue", "Air", "p100", CYAN, ["Star"]),
             ("Card_Yellow", "Slow", "p130", SIGNAL, []), ("Card_Disabled", "Lock", "p220", (0.5, 0.55, 0.6), [])]
    x0 = 960 - (len(cards) * 136 - 16) // 2
    c.sliced(kit.get("Panel"), x0 - 70, 852, len(cards) * 136 + 124, 204)
    c.text(T["lb"], x0 - 40, 940, DIM, "centre")
    c.text(T["rb"], x0 + len(cards) * 136 + 24, 940, DIM, "centre")
    for i, (state, n, price, tint, stars) in enumerate(cards):
        x = x0 + i * 136
        c.sliced(kit.get(state), x, 870, 120, 166)
        icon(n, x + 60, 940, 70, tint)
        for k, s in enumerate(stars):
            icon(s, x + 22 + k * 22, 892, 22, SIGNAL)
        icon("Ground" if i % 2 == 0 else "Air", x + 98, 892, 22, DIM)
        c.text(T[price], x + 60, 992, SIGNAL if state != "Card_Disabled" else (0.5, 0.5, 0.55), "centre")
    # Drones panel bottom right.
    c.sliced(kit.get("Panel"), 1560, 760, 336, 196)
    c.text(T["drones"], 1580, 772)
    for i, key in enumerate(("d1", "d2", "d3")):
        x = 1580 + i * 104
        c.sliced(kit.get("Card_Blue" if i != 2 else "Card_Green"), x, 812, 92, 100)
        icon("Drone", x + 46, 858, 56, CYAN if i != 2 else (0.5, 1.0, 0.65))
        c.text(T[key], x + 46, 918, DIM, "centre")
    # World markers: an alien origin and the boss pointer on the screen edge.
    icon("EnemyOrigin", 1480, 380, 60, (1, 1, 1))
    icon("BossMarker", 1850, 520, 72, (1, 1, 1))
    c.text(T["bossc"], 1790, 568, WARN, "centre")
    c.save(path)


def dialogue(dlg, kit, icons, path):
    W, H = 1920, 1080
    T = texts([("name", "MERCER", 26, A.FONT), ("l1", "And whatever gets through is mine. Keep the turrets fed", 30, BODY_FONT),
               ("l2", "and the road clear, and leave the big ones to", 30, BODY_FONT),
               ("skip", "SKIP", 22, A.FONT), ("next", "NEXT", 22, A.FONT)])
    c = Canvas(W, H, bg=backdrop(W, H))
    shade = dlg["UI_Dialogue_Shade.png"]
    sh = A.resize(shade, 520, 8)
    sh = np.repeat(sh[:, :1], W, axis=1)
    A.paste(c.px, sh, 0, H - 520, (0.0, 0.02, 0.05), 0.85)
    bx, by, bw, bh = 340, 690, 1240, 300
    panel, pm = dlg["atlas"].get("Dialogue_Panel")
    edge, em = dlg["atlas"].get("Dialogue_Edge")
    inset = 36 * S
    for img, meta, tint, alpha in ((panel, pm, (0.05, 0.1, 0.17), 0.94), (edge, em, CYAN, 1.0)):
        im = A.scale_img(img, S)
        b = [int(v * S) for v in meta["border_lbrt"]]
        im = A.nine_slice(im, b, int(bw + 2 * inset), int(bh + 2 * inset))
        A.paste(c.px, im, int(bx - inset), int(by - inset), tint, alpha)
    scan = dlg["UI_Dialogue_Scanlines.png"]
    tile = np.tile(scan, (int(bh / 8 * 2) + 1, int(bw / 8 * 2) + 1, 1))[:bh, :bw]
    A.paste(c.px, tile, bx, by, (0, 0, 0), 0.35)
    tag, tm = dlg["atlas"].get("Dialogue_NameTag")
    im = A.nine_slice(A.scale_img(tag, S), [int(v * S) for v in tm["border_lbrt"]], 200, 56)
    A.paste(c.px, im, bx + 10, by - 44, CYAN)
    c.text(T["name"], bx + 110, by - 34, (0.02, 0.06, 0.1), "centre")
    portrait = os.path.join(SHOTS, "fb2_shots", "portrait_mercer.png")
    px0, py0, ps = bx + 30, by + 36, 220
    if os.path.exists(portrait):
        p = load(portrait)
        A.paste(c.px, A.resize(p, ps, ps), px0, py0)
    frame, fm = dlg["atlas"].get("Dialogue_PortraitFrame")
    im = A.nine_slice(A.scale_img(frame, S), [int(v * S) for v in fm["border_lbrt"]], ps + 16, ps + 16)
    A.paste(c.px, im, px0 - 8, py0 - 8, CYAN)
    c.text(T["l1"], bx + 290, by + 50, ICE)
    c.text(T["l2"], bx + 290, by + 100, ICE)
    noise = dlg["UI_Dialogue_Noise.png"]
    nx = bx + 290 + T["l2"].shape[1] - 8
    A.paste(c.px, np.repeat(np.repeat(noise, 2, 0)[:, :180], 1, 1)[:34], nx, by + 104, (1, 1, 1), 1.0)
    c.sliced(kit.get("Button_Normal"), bx + bw - 400, by + bh - 76, 170, 52)
    c.text(T["skip"], bx + bw - 315, by + bh - 64, DIM, "centre")
    c.sliced(kit.get("Button_Focus"), bx + bw - 210, by + bh - 76, 170, 52)
    c.text(T["next"], bx + bw - 125, by + bh - 64, ICE, "centre")
    arrow, _ = dlg["atlas"].get("Dialogue_Arrow")
    c.sprite(arrow, bx + bw - 440, by + bh - 50, scale=0.5, tint=SIGNAL, centre=True)
    c.save(path)


def menu(kit, icons, sky, path):
    W, H = 1920, 1080
    T = texts([("title", "TOWER DEFENCE", 64, A.FONT), ("sub", "S-69-CRYS  /  FIRST LANDING", 22, A.FONT_NARROW),
               ("start", "START", 30, A.FONT), ("cont", "CONTINUE", 30, A.FONT), ("opt", "OPTIONS", 30, A.FONT),
               ("codex", "CODEX", 30, A.FONT), ("quit", "QUIT", 30, A.FONT),
               ("t1", "AUDIO", 22, A.FONT), ("t2", "VIDEO", 22, A.FONT), ("t3", "CONTROLS", 22, A.FONT),
               ("music", "Music", 24, A.FONT_NARROW), ("sfx", "Effects", 24, A.FONT_NARROW), ("voice", "Voices", 24, A.FONT_NARROW),
               ("sub2", "Subtitles", 24, A.FONT_NARROW), ("shake", "Screen effects", 24, A.FONT_NARROW),
               ("hint", "A  select     B  back     LB / RB  tabs", 20, A.FONT_NARROW), ("v80", "80", 22, A.FONT), ("v55", "55", 22, A.FONT),
               ("v70", "70", 22, A.FONT), ("tip", "Volume of the soundtrack", 20, A.FONT_NARROW)])
    bg = np.zeros((H, W, 4))
    bg[..., :3] = (0.01, 0.02, 0.04)
    # A centred crop instead of a resample: the bundled numpy has no fast BLAS and a 2k resample takes minutes.
    sk = A.premul(sky[36:36 + H, 64:64 + W])
    bg[..., :3] = bg[..., :3] * (1 - sk[..., 3:4]) + sk[..., :3]
    c = Canvas(W, H, bg=bg)
    c.text(T["title"], 150, 150, ICE)
    c.text(T["sub"], 154, 236, CYAN)
    corner = kit.get("CornerOrnament")[0]
    c.sprite(corner, 112, 120, scale=1.0, tint=CYAN)
    div = kit.get("Divider")
    c.sliced(div, 150, 280, 520, 12)
    rows = [("start", "Button_Focus", ICE, None), ("cont", "Button_Disabled", (0.45, 0.5, 0.55), None),
            ("opt", "Button_Normal", DIM, "Gear"), ("codex", "Button_Pressed", CYAN, "Book"), ("quit", "Button_Normal", DIM, None)]
    for i, (key, state, tint, ic) in enumerate(rows):
        y = 340 + i * 104
        c.sliced(kit.get(state), 150, y, 420, 76)
        c.text(T[key], 360, y + 20, tint, "centre")
        if ic:
            c.sprite(icons.get(ic)[0], 196, y + 38, scale=40 / 256 * 1.14, tint=tint, centre=True)
    # Options panel with tabs, sliders and toggles.
    px, py, pw, ph = 820, 300, 900, 600
    for i, (key, state) in enumerate((("t1", "Tab_Selected"), ("t2", "Tab"), ("t3", "Tab"))):
        c.sliced(kit.get(state), px + 24 + i * 200, py - 50, 184, 50)
        c.text(T[key], px + 116 + i * 200, py - 38, ICE if i == 0 else DIM, "centre")
    c.sliced(kit.get("Panel"), px, py, pw, ph)
    sliders = (("music", 0.8, "v80", True), ("sfx", 0.55, "v55", False), ("voice", 0.7, "v70", False))
    for i, (key, val, vkey, focus) in enumerate(sliders):
        y = py + 60 + i * 110
        if focus:
            c.sliced(kit.get("Panel_Selected"), px + 30, y - 22, pw - 60, 84)
        c.text(T[key], px + 70, y, ICE if focus else DIM)
        x0, x1 = px + 300, px + pw - 150
        c.sliced(kit.get("Bar_Back"), x0, y + 14, x1 - x0, 16)
        c.sliced(kit.get("Bar_Blue"), x0, y + 14, int((x1 - x0) * val), 16)
        handle = kit.get("Slider_Handle_Focus" if focus else "Slider_Handle")[0]
        c.sprite(handle, x0 + (x1 - x0) * val, y + 22, tint=(1, 1, 1), centre=True)
        c.text(T[vkey], px + pw - 70, y + 4, SIGNAL if focus else DIM, "right")
    for i, (key, on) in enumerate((("sub2", True), ("shake", False))):
        y = py + 380 + i * 80
        c.text(T[key], px + 70, y, DIM)
        t = kit.get("Toggle_On" if on else "Toggle_Off")[0]
        c.sprite(t, px + 330, y + 16, tint=(1, 1, 1), centre=True)
    c.sliced(kit.get("Scroll_Track"), px + pw - 36, py + 30, 12, ph - 60)
    c.sliced(kit.get("Scroll_Thumb"), px + pw - 36, py + 40, 12, 180)
    c.sliced(kit.get("Tooltip"), px + 300, py + 120, 300, 40)
    c.sprite(kit.get("TooltipPointer")[0], px + 450, py + 112, tint=(1, 1, 1), centre=True)
    c.text(T["tip"], px + 450, py + 128, DIM, "centre")
    c.text(T["hint"], px + pw - 20, py + ph + 30, DIM, "right")
    c.save(path)


def misc(radar, dlg, halo, back, sky, path):
    W, H = 1920, 1080
    c = Canvas(W, H, bg=np.array([0.01, 0.015, 0.03]))
    sk = A.premul(sky[::2, ::2][18:558, 32:992])
    c.px[0:540, 0:960, :3] = c.px[0:540, 0:960, :3] * (1 - sk[..., 3:4]) + sk[..., :3]
    c.px[540:1080, 0:960] = np.dstack([A.resize(back[..., :3], 540, 960), np.ones((540, 960))])
    r = A.scale_img(radar, 0.5)
    A.paste(c.px, r, 1440 - r.shape[1] // 2, 280 - r.shape[0] // 2)
    sweep, _ = dlg["atlas"].get("Radar_Sweep")
    sw = A.scale_img(sweep, 0.5 * 0.46 * 1024 / 256)
    A.paste(c.px, sw, 1440 - sw.shape[1] // 2, 280 - sw.shape[0] // 2, (0.4, 1.0, 0.85), 0.75)
    dot, _ = dlg["atlas"].get("Radar_Contact")
    rng = np.random.default_rng(3)
    for k in range(14):
        a = rng.uniform(3.6, 5.8)
        d = rng.uniform(70, 200)
        A.paste(c.px, A.scale_img(dot, 0.5), int(1440 + np.cos(a) * d) - 16, int(280 + np.sin(a) * d) - 16, (1.0, 0.75, 0.3))
    hl = A.scale_img(halo, 1.0)
    A.paste(c.px, hl, 1440 - 256, 810 - 256, (0.45, 0.8, 1.0), 0.55)
    yy, xx = np.mgrid[0:300, 0:300]
    disc = np.clip(150 - np.hypot(xx - 150, yy - 150), 0, 1)
    planet = np.dstack([0.12 + 0 * disc, 0.2 + 0 * disc, 0.3 + 0.1 * (yy / 300.0), disc])
    A.paste(c.px, planet, 1440 - 150, 810 - 150)
    c.save(path)


def run(preview, report):
    os.makedirs(preview, exist_ok=True)
    icons = Atlas("UI_Icons")
    kit = Atlas("UI_Kit")
    dlg = {"atlas": Atlas("UI_Dialogue")}
    for n in ("UI_Dialogue_Noise.png", "UI_Dialogue_Scanlines.png", "UI_Dialogue_Shade.png"):
        dlg[n] = load(os.path.join(A.OUT, n))
    sky = load(os.path.join(A.OUT, "UI_Starfield.png"))
    paths = {k: os.path.join(preview, f"ui_{k}.png") for k in ("icons", "hud", "dialogue", "menu", "misc")}
    import time
    t = time.time()
    icon_sheet(icons, paths["icons"])
    print(f"[ui] mockup icons {time.time() - t:.0f}s", flush=True)
    hud(kit, icons, paths["hud"])
    print(f"[ui] mockup hud {time.time() - t:.0f}s", flush=True)
    dialogue(dlg, kit, icons, paths["dialogue"])
    print(f"[ui] mockup dialogue {time.time() - t:.0f}s", flush=True)
    menu(kit, icons, sky, paths["menu"])
    print(f"[ui] mockup menu {time.time() - t:.0f}s", flush=True)
    misc(load(os.path.join(A.OUT, "UI_Radar.png")), dlg, load(os.path.join(A.OUT, "UI_MapHalo.png")),
         load(os.path.join(A.OUT, "UI_PreviewBackdrop.png")), sky, paths["misc"])
    report["previews"] = paths
    print("[ui] mockups:", ", ".join(paths.values()), flush=True)
