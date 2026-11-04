"""Round 25: the game's own UI art, built in Blender (replaces the code-painted UI textures and the Kenney UI pack).

Every piece is flat 2D curve geometry with a small bevel (or a gradient/noise shader on a quad), rendered top-down
with an orthographic camera in Cycles at 2x the size it is shown at; numpy only composites (glow, dark rim, alpha)
and packs the atlases. Outputs in ArtSource/Exports/UI:

  UI_Icons.png (+ @2x) / .json   40 symbolic icons; names match Game.UI.IconType (+ extras)
  UI_Kit.png / .json             panels, cards, buttons (normal/focus/pressed/disabled), bars, slider, tab, tooltip,
                                 scroll bar, toggle, focus frame, ornaments, divider; 9-slice borders in the json
  UI_Dialogue.png / .json        dialogue panel + edge (shared geometry), name tag, portrait frame, next arrow,
                                 radar sweep and contact; plus tiling textures UI_Dialogue_{Noise,Scanlines,Radial,Shade}
  UI_Radar.png                   the cutscene radar scope (bezel, rings, ticks, crosshair)
  UI_Starfield.png               the menu sky, 2048 x 1152
  UI_PreviewBackdrop.png         backdrop behind the model preview
  UI_MapHalo.png                 halo behind the planet on the map
  UI_report.json                 usage, sizes and the code site each file replaces

Mockups (icon sheet, HUD, dialogue, menu) go to --preview (default ArtSource/Previews/UI).

    blender --background --factory-startup --python Tools/Blender/build_ui.py -- [--only icons,kit,...] [--preview DIR]
"""

import json
import math
import os
import sys
import time

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import ui_art as A  # noqa: E402

# ------------------------------------------------------------------------------------------------------ palette
# Our faction: cool and technical. Display-space colours (the renders are written without a view transform).
NAVY = A.hex_to_rgb("#08121F")
NAVY_2 = A.hex_to_rgb("#0D1B2E")
BTN = A.hex_to_rgb("#10284A")
BTN_FOCUS = A.hex_to_rgb("#1B4C7E")
BTN_PRESS = A.hex_to_rgb("#081830")
CYAN = A.hex_to_rgb("#4FD2FF")
CYAN_DIM = A.hex_to_rgb("#2A6E96")
ICE = A.hex_to_rgb("#E8F7FF")
SIGNAL = A.hex_to_rgb("#FFD040")
GREY_FILL = A.hex_to_rgb("#14181E")
GREY_EDGE = A.hex_to_rgb("#4A525C")
ACCENT = {"Blue": A.hex_to_rgb("#4FA8FF"), "Green": A.hex_to_rgb("#52F08C"),
          "Yellow": A.hex_to_rgb("#FFCC40"), "Red": A.hex_to_rgb("#FF5A48")}
# The aliens: warm and organic.
WARM = A.hex_to_rgb("#FF8A24")
WARM_RED = A.hex_to_rgb("#FF4020")
WARM_GLOW = A.hex_to_rgb("#FF3A10")

ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []


def arg(name, default=None):
    return ARGS[ARGS.index(name) + 1] if name in ARGS else default


ONLY = set(arg("--only", "icons,kit,dialogue,radar,starfield,backdrop,halo,mockups").split(","))
PREVIEW = arg("--preview", os.path.join(A.REPO, "ArtSource", "Previews", "UI"))
REPORT = {"round": 25, "script": "Tools/Blender/build_ui.py", "files": {}}


def out(name):
    os.makedirs(A.OUT, exist_ok=True)
    return os.path.join(A.OUT, name)


# ======================================================================================================== ICONS
# One stroke weight for the whole set: main strokes 0.075 of the cell, secondary 0.045.
SW = 0.075
SW2 = 0.045


def _shield(scale=1.0):
    pts = [(0.5, 0.92), (0.84, 0.80), (0.84, 0.54), (0.79, 0.38), (0.67, 0.21), (0.5, 0.08), (0.33, 0.21),
           (0.21, 0.38), (0.16, 0.54), (0.16, 0.80)]
    return [(0.5 + (u - 0.5) * scale, 0.5 + (v - 0.5) * scale) for u, v in pts]


def _keyhole():
    cx, cy, r, hw, bottom = 0.5, 0.35, 0.068, 0.03, 0.19
    a = math.asin(hw / r)
    start = -math.pi / 2 + a
    end = 3 * math.pi / 2 - a
    arc = A.circle_pts(cx, cy, r, 64, start, end)
    return arc + [(cx - hw, bottom), (cx + hw, bottom)]


def _gear(cx, cy, ro, ri, teeth):
    pts = []
    for i in range(teeth):
        a = i * 2 * math.pi / teeth
        step = 2 * math.pi / teeth
        for frac, r in ((0.0, ri), (0.16, ro), (0.5, ro), (0.66, ri)):
            pts.append((cx + r * math.cos(a + frac * step), cy + r * math.sin(a + frac * step)))
    return pts


def _ellipse(cx, cy, rx, ry, n=72, rot=0.0):
    c, s = math.cos(rot), math.sin(rot)
    out_pts = []
    for i in range(n):
        t = i * 2 * math.pi / n
        x, y = rx * math.cos(t), ry * math.sin(t)
        out_pts.append((cx + x * c - y * s, cy + x * s + y * c))
    return out_pts


def icon_upgrade(c):
    c.poly([(0.15, 0.47), (0.85, 0.47), (0.5, 0.89)])
    c.poly(A.chamfer_rect(0.38, 0.12, 0.62, 0.5, 0, 0, 0.04, 0.04))


def icon_repair(c):
    c.capsule(0.18, 0.18, 0.52, 0.52, SW)
    gap = math.radians(42)
    mid = math.radians(45)
    c.poly(A.sector_pts(0.65, 0.65, 0.1, 0.225, mid + gap, mid - gap + 2 * math.pi))


def icon_sell(c):
    c.ring(0.5, 0.5, 0.33, 0.44)
    c.poly(A.hex_pts(0.5, 0.5, 0.2))


def _tri(u0, u1, v0=0.18, v1=0.82):
    return [(u0, v0), (u1, (v0 + v1) / 2), (u0, v1)]


def icon_speed(c):
    c.poly(_tri(0.27, 0.81))


def icon_speed_double(c):
    c.poly(_tri(0.1, 0.5, 0.2, 0.8))
    c.poly(_tri(0.5, 0.9, 0.2, 0.8))


def icon_speed_triple(c):
    for u in (0.05, 0.345, 0.64):
        c.poly(_tri(u, u + 0.31, 0.25, 0.75))


def icon_pause(c):
    c.poly(A.chamfer_rect(0.23, 0.18, 0.42, 0.82, 0.04, 0.04, 0.04, 0.04))
    c.poly(A.chamfer_rect(0.58, 0.18, 0.77, 0.82, 0.04, 0.04, 0.04, 0.04))


def icon_skip(c):
    c.poly(_tri(0.14, 0.66, 0.2, 0.8))
    c.poly(A.chamfer_rect(0.7, 0.2, 0.86, 0.8, 0.03, 0.03, 0.03, 0.03))


def icon_star(c):
    c.poly(A.star_pts(0.5, 0.47, 0.47, 0.205))


def icon_star_empty(c):
    c.poly(A.star_pts(0.5, 0.47, 0.47, 0.205), [A.star_pts(0.5, 0.475, 0.315, 0.13)])


def icon_check(c):
    c.polyline([(0.16, 0.5), (0.4, 0.26), (0.84, 0.72)], 0.085)


def icon_hero(c):
    c.poly(_shield(), [_shield(0.72)])
    c.polyline([(0.36, 0.4), (0.5, 0.56), (0.64, 0.4)], SW2 + 0.01)


def icon_shockwave(c):
    c.circle(0.5, 0.5, 0.11)
    c.ring(0.5, 0.5, 0.215, 0.295)
    c.ring(0.5, 0.5, 0.385, 0.455)


def icon_combine(c):
    c.ring(0.36, 0.5, 0.17, 0.27)
    c.ring(0.64, 0.5, 0.17, 0.27)


def icon_ground(c):
    c.poly([(0.06, 0.27), (0.37, 0.8), (0.53, 0.53), (0.65, 0.67), (0.94, 0.27)])
    c.poly(A.chamfer_rect(0.06, 0.13, 0.94, 0.21, 0, 0, 0.03, 0.03))


def icon_air(c):
    c.polyline([(0.08, 0.4), (0.5, 0.7), (0.92, 0.4)], SW)
    c.capsule(0.5, 0.64, 0.5, 0.22, SW - 0.01)
    c.capsule(0.36, 0.24, 0.64, 0.24, SW2)


def icon_slow(c):
    for k in range(3):
        a = math.pi / 2 + k * math.pi / 3
        dx, dy = math.cos(a) * 0.42, math.sin(a) * 0.42
        c.capsule(0.5 - dx, 0.5 - dy, 0.5 + dx, 0.5 + dy, SW2 + 0.005)
    for k in range(6):
        a = math.pi / 2 + k * math.pi / 3
        bx, by = 0.5 + math.cos(a) * 0.27, 0.5 + math.sin(a) * 0.27
        for side in (-1, 1):
            b = a + side * math.radians(48)
            c.capsule(bx, by, bx + math.cos(b) * 0.11, by + math.sin(b) * 0.11, SW2 - 0.008)
    c.poly(A.hex_pts(0.5, 0.5, 0.12, flat_top=False))


def icon_arrow(c):
    c.poly([(0.12, 0.18), (0.92, 0.5), (0.12, 0.82), (0.3, 0.5)])


def icon_disc(c):
    c.circle(0.5, 0.5, 0.44)


def icon_ring(c):
    c.ring(0.5, 0.5, 0.34, 0.45)


def icon_beacon(c):
    c.circle(0.5, 0.5, 0.17)
    c.ring(0.5, 0.5, 0.32, 0.43)


def icon_blast(c):
    c.poly(A.star_pts(0.5, 0.5, 0.47, 0.22, 8, math.pi / 8))


def icon_roll(c):
    c.ring(0.63, 0.5, 0.15, 0.28)
    c.circle(0.63, 0.5, 0.065)
    for k in range(3):
        a = math.pi / 2 + k * 2 * math.pi / 3
        c.capsule(0.63, 0.5, 0.63 + math.cos(a) * 0.2, 0.5 + math.sin(a) * 0.2, 0.03)
    c.capsule(0.1, 0.35, 0.26, 0.35, SW2)
    c.capsule(0.04, 0.5, 0.26, 0.5, SW2)
    c.capsule(0.1, 0.65, 0.26, 0.65, SW2)


def icon_net(c):
    c.ring(0.5, 0.5, 0.36, 0.455)
    r = 0.38
    for off in (-0.15, 0.0, 0.15):
        h = math.sqrt(r * r - off * off)
        c.capsule(0.5 + off, 0.5 - h, 0.5 + off, 0.5 + h, 0.028, bevel=3.0)
        c.capsule(0.5 - h, 0.5 + off, 0.5 + h, 0.5 + off, 0.028, bevel=3.0)


def icon_trap(c):
    c.poly(A.chamfer_rect(0.08, 0.12, 0.92, 0.27, 0.03, 0.03, 0.03, 0.03))
    for u in (0.14, 0.38, 0.62):
        c.poly([(u, 0.26), (u + 0.24, 0.26), (u + 0.12, 0.84 if u == 0.38 else 0.74)])


def icon_patrol(c):
    c.poly(A.chamfer_rect(0.1, 0.38, 0.9, 0.6, 0.06, 0.06, 0.03, 0.03))
    c.poly(A.chamfer_rect(0.3, 0.58, 0.7, 0.82, 0.1, 0.1, 0, 0), [A.chamfer_rect(0.37, 0.64, 0.63, 0.75, 0.05, 0.05, 0, 0)])
    for u in (0.29, 0.71):
        c.circle(u, 0.28, 0.13, [A.circle_pts(u, 0.28, 0.05)])


def icon_book(c):
    c.capsule(0.5, 0.15, 0.5, 0.85, 0.032)
    for side in (-1, 1):
        def m(u):
            return 0.5 + side * (u - 0.5)
        page = [(m(0.455), 0.18), (m(0.455), 0.82), (m(0.08), 0.78), (m(0.08), 0.22)]
        if side < 0:
            page = page[::-1]
        holes = []
        for v in (0.62, 0.5, 0.38):
            ux = sorted((m(0.14), m(0.39)))
            holes.append([(ux[0], v - 0.022), (ux[1], v - 0.022), (ux[1], v + 0.022), (ux[0], v + 0.022)])
        c.poly(page, holes)


def icon_hex(c):
    c.poly(A.hex_pts(0.5, 0.5, 0.46))


def icon_lock(c):
    c.poly(A.chamfer_rect(0.2, 0.1, 0.8, 0.52, 0.05, 0.05, 0.05, 0.05), [_keyhole()])
    c.arc(0.5, 0.58, 0.185, 0.0, math.pi, 0.058, caps=False)
    c.poly(A.chamfer_rect(0.257, 0.48, 0.373, 0.585, 0, 0, 0, 0))
    c.poly(A.chamfer_rect(0.627, 0.48, 0.743, 0.585, 0, 0, 0, 0))


def icon_boss_marker(c):
    cx, cy, r, tip = 0.42, 0.5, 0.31, 0.94
    t = math.acos(r / (tip - cx))
    loop = [(tip, cy)] + A.circle_pts(cx, cy, r, 96, t, 2 * math.pi - t)
    c.poly(loop, [A.circle_pts(cx, cy, 0.2)])
    # Two claw rakes inside the ring: something with teeth is coming from this side (bold enough for 32 px).
    for off in (-0.06, 0.06):
        c.capsule(cx + off - 0.045, cy - 0.11, cx + off + 0.045, cy + 0.11, 0.036, bevel=3.0)


def icon_enemy_origin(c):
    c.ring(0.5, 0.5, 0.3, 0.45)
    for k in range(8):
        a = k * math.pi / 4 + math.pi / 8
        w = math.radians(11)
        c.poly([(0.5 + 0.31 * math.cos(a - w), 0.5 + 0.31 * math.sin(a - w)), (0.5 + 0.16 * math.cos(a), 0.5 + 0.16 * math.sin(a)),
                (0.5 + 0.31 * math.cos(a + w), 0.5 + 0.31 * math.sin(a + w))], bevel=3.0)
    c.circle(0.5, 0.5, 0.075)


def icon_drone(c):
    for sx in (-1, 1):
        for sy in (-1, 1):
            c.capsule(0.5, 0.5, 0.5 + sx * 0.25, 0.5 + sy * 0.25, SW2)
            c.ring(0.5 + sx * 0.27, 0.5 + sy * 0.27, 0.1, 0.16)
    c.poly(A.hex_pts(0.5, 0.5, 0.14, flat_top=False))


def icon_base(c):
    # The colony base: a domed hall on a wide platform between two pylons (not a bell: the pylons and the platform).
    c.poly(A.circle_pts(0.5, 0.36, 0.25, 96, 0, math.pi), [A.hex_pts(0.5, 0.48, 0.07)])
    c.poly([(0.04, 0.12), (0.96, 0.12), (0.88, 0.32), (0.12, 0.32)])
    for u in (0.15, 0.85):
        c.poly(A.chamfer_rect(u - 0.055, 0.3, u + 0.055, 0.62, 0.03, 0.03, 0, 0))
        c.circle(u, 0.7, 0.045, bevel=3.0)
    c.capsule(0.5, 0.6, 0.5, 0.8, 0.026, bevel=3.0)
    c.circle(0.5, 0.84, 0.045, bevel=3.0)


def icon_gear(c):
    c.poly(_gear(0.5, 0.5, 0.46, 0.34, 8), [A.circle_pts(0.5, 0.5, 0.14)])


def icon_close(c):
    c.capsule(0.2, 0.2, 0.8, 0.8, 0.08)
    c.capsule(0.2, 0.8, 0.8, 0.2, 0.08)


def icon_research(c):
    # Stencil-cut flask: the liquid line is a gap through the whole glass (holes in concave outlines fill in).
    def side(v):
        return 0.4 - (0.62 - v) / 0.44 * 0.26
    c.poly([(0.4, 0.88), (0.4, 0.62), (side(0.45), 0.45), (1 - side(0.45), 0.45), (0.6, 0.62), (0.6, 0.88)][::-1])
    c.poly([(side(0.38), 0.38), (0.14, 0.18), (0.18, 0.12), (0.82, 0.12), (0.86, 0.18), (1 - side(0.38), 0.38)][::-1])
    c.poly(A.chamfer_rect(0.34, 0.86, 0.66, 0.92, 0, 0, 0, 0))


def icon_power(c):
    c.poly([(0.6, 0.93), (0.2, 0.45), (0.46, 0.45), (0.38, 0.07), (0.8, 0.57), (0.54, 0.57)][::-1])


def icon_enemy(c):
    c.poly(_ellipse(0.5, 0.44, 0.3, 0.25), [_ellipse(0.385, 0.49, 0.075, 0.05, 40, -0.5), _ellipse(0.615, 0.49, 0.075, 0.05, 40, 0.5)])
    for side in (-1, 1):
        c.polyline([(0.5 + side * 0.1, 0.66), (0.5 + side * 0.2, 0.84), (0.5 + side * 0.32, 0.9)], 0.034)
        c.poly([(0.5 + side * 0.06, 0.24), (0.5 + side * 0.18, 0.22), (0.5 + side * 0.09, 0.07)][:: -side])


def icon_credits(c):
    for k, v in enumerate((0.12, 0.33, 0.54)):
        c.poly(A.chamfer_rect(0.16 + k * 0.02, v, 0.84 - k * 0.02, v + 0.16, 0.05, 0.05, 0.05, 0.05))
    c.poly(A.hex_pts(0.5, 0.82, 0.09))


def icon_info(c):
    c.circle(0.5, 0.5, 0.45, [A.circle_pts(0.5, 0.71, 0.065), A.chamfer_rect(0.45, 0.2, 0.55, 0.56, 0.02, 0.02, 0, 0)])


# Order = Game.UI.IconType order, then the extras; the json carries the index of every enum member.
ICONS = [
    ("Upgrade", icon_upgrade), ("Repair", icon_repair), ("Sell", icon_sell), ("Speed", icon_speed),
    ("SpeedDouble", icon_speed_double), ("SpeedTriple", icon_speed_triple), ("Pause", icon_pause),
    ("SkipWave", icon_skip), ("Star", icon_star), ("Check", icon_check), ("Hero", icon_hero),
    ("Shockwave", icon_shockwave), ("Combine", icon_combine), ("Ground", icon_ground), ("Air", icon_air),
    ("Slow", icon_slow), ("Arrow", icon_arrow), ("Disc", icon_disc), ("Ring", icon_ring), ("Beacon", icon_beacon),
    ("Blast", icon_blast), ("Roll", icon_roll), ("Net", icon_net), ("Trap", icon_trap), ("Patrol", icon_patrol),
    ("Book", icon_book), ("Hex", icon_hex), ("Lock", icon_lock),
    # Extras (not in IconType yet): add enum members with these names to use them.
    ("StarEmpty", icon_star_empty), ("BossMarker", icon_boss_marker), ("EnemyOrigin", icon_enemy_origin),
    ("Drone", icon_drone), ("Base", icon_base), ("Gear", icon_gear), ("Close", icon_close),
    ("Research", icon_research), ("Power", icon_power), ("Enemy", icon_enemy), ("Credits", icon_credits),
    ("Info", icon_info),
]
ENUM_COUNT = 28
# Pre-coloured alien markers (use with Image.color = white); everything else is white and tinted in code.
WARM_ICONS = {"BossMarker": WARM, "EnemyOrigin": WARM_RED}
ICON_USES = {
    "Upgrade": "TowerMenuView, UpgradeScreen", "Repair": "TowerMenuView, HeroAbilityHud", "Sell": "TowerMenuView, ResultView",
    "Speed": "HudView speed button (also the play symbol)", "SpeedDouble": "HudView", "SpeedTriple": "HudView",
    "Pause": "HudView", "SkipWave": "HudView", "Star": "tower stars, ResultView, UpgradeCelebration",
    "Check": "WorldMapScreen badges, LoadoutPanelView, UpgradeScreen", "Hero": "HeroPanelView, HeroQuickBar",
    "Shockwave": "HeroAbilityHud", "Combine": "TowerMenuView", "Ground": "tower target layer tag",
    "Air": "tower target layer tag", "Slow": "path object cards", "Arrow": "BossMarkerView, BaseAlertView, map",
    "Disc": "WorldMapScreen route dots, markers", "Ring": "map/node rings", "Beacon": "map node", "Blast": "path objects",
    "Roll": "path objects", "Net": "path objects", "Trap": "path objects", "Patrol": "path objects",
    "Book": "Codex button", "Hex": "WorldMapScreen region plate", "Lock": "WorldMapScreen locked region",
    "StarEmpty": "unearned upgrade star", "BossMarker": "BossMarkerView off-screen pointer (pre-coloured, points right)",
    "EnemyOrigin": "spawn/nest marker (pre-coloured)", "Drone": "drones panel", "Base": "base HP / base screen",
    "Gear": "Options", "Close": "close/back", "Research": "research points", "Power": "power currency",
    "Enemy": "next-wave enemy preview", "Credits": "money", "Info": "tooltips/codex info",
}

ICON_CELL = 256      # 2x master cell
ICON_PAD = 16        # glyph square inset inside a cell
ICON_COLS = 8


def build_icons():
    A.reset()
    rows = math.ceil(len(ICONS) / ICON_COLS)
    W, H = ICON_COLS * ICON_CELL, rows * ICON_CELL
    A.camera(W, H)
    coll = A.layer("icons")
    for i, (name, fn) in enumerate(ICONS):
        col, row = i % ICON_COLS, i // ICON_COLS
        ox = col * ICON_CELL + ICON_PAD
        oy = H - (row + 1) * ICON_CELL + ICON_PAD
        colour = WARM_ICONS.get(name, (1, 1, 1))
        fn(A.Cell(ox, oy, ICON_CELL - 2 * ICON_PAD, coll, colour=colour, bevel=6.0))
    raw = A.render(W, H, samples=32)

    atlas = np.zeros((H, W, 4))
    for i, (name, _) in enumerate(ICONS):
        col, row = i % ICON_COLS, i // ICON_COLS
        ys, xs = slice(row * ICON_CELL, (row + 1) * ICON_CELL), slice(col * ICON_CELL, (col + 1) * ICON_CELL)
        cell = raw[ys, xs]
        cov = cell[..., 3]
        rgb = np.clip(cell[..., :3] * 0.88, 0, 1)
        glyph = np.concatenate([rgb * cov[..., None], cov[..., None]], axis=-1)
        if name in WARM_ICONS:
            glow = A.solid(np.clip(A.blur(cov, 9.0) * 1.1, 0, 0.85), WARM_GLOW)
            rim = A.solid(np.clip(A.blur(A.dilate(cov, 2.5), 1.6) * 0.7, 0, 0.7), (0.18, 0.03, 0.0))
            layer_img = A.over(A.over(glow, rim), glyph)
        else:
            # A soft dark rim keeps a white (or tinted) icon readable on any background, light or dark.
            rim = A.solid(np.clip(A.blur(A.dilate(cov, 3.0), 2.2) * 0.62, 0, 0.62), (0.0, 0.02, 0.05))
            layer_img = A.over(rim, glyph)
        atlas[ys, xs] = A.unpremul(layer_img)

    one = A.unpremul(A.resize(A.premul(atlas), H // 2, W // 2))
    A.save_png(out("UI_Icons@2x.png"), atlas)
    A.save_png(out("UI_Icons.png"), one)
    entries = {}
    cell1 = ICON_CELL // 2
    for i, (name, _) in enumerate(ICONS):
        col, row = i % ICON_COLS, i // ICON_COLS
        x, y = col * cell1, row * cell1
        entries[name] = {"index": i, "enum": i < ENUM_COUNT, "x": x, "y": y, "w": cell1, "h": cell1,
                         "unity_rect": [x, H // 2 - y - cell1, cell1, cell1], "pivot": [0.5, 0.5],
                         "precoloured": name in WARM_ICONS, "use": ICON_USES.get(name, "")}
    meta = {"texture": "UI_Icons.png", "texture_2x": "UI_Icons@2x.png", "size": [W // 2, H // 2], "cell": cell1,
            "glyph_area": "the glyph sits inside the central 112 of 128 px; a soft dark rim fills the margin",
            "tint": "white icons are tinted with Image.color exactly like IconFactory sprites; precoloured ones use white",
            "coords": "x, y from the top-left; unity_rect is x, y from the bottom-left (Sprite Editor)", "sprites": entries}
    with open(out("UI_Icons.json"), "w") as fh:
        json.dump(meta, fh, indent=1)
    REPORT["files"]["UI_Icons.png"] = {"size": meta["size"], "entries": len(entries),
                                        "replaces": "Assets/_Project/Scripts/UI/IconFactory.cs:58-73 (runtime 64 px masks)",
                                        "used_by": "BaseAlertView, BossMarkerView, HeroAbilityHud, HeroPanelView, HeroQuickBar, HudView, "
                                                   "LoadoutPanelView, ResultView, TowerMenuView, UpgradeCelebration, UpgradeScreen, WorldMapScreen"}
    return atlas, one, entries


# ========================================================================================================== KIT
# Every frame is one convex outline (45 degree chamfers, bigger on the top-left and bottom-right like our build
# pads) drawn as three layers: fill, edge (a bevelled band), ornaments. Glow is the edge blurred. Sizes are 2x.

def mix(a, b, t):
    return tuple(a[i] + (b[i] - a[i]) * t for i in range(3))


def _towards(p, q, length):
    """The point `length` px from q towards p."""
    dx, dy = p[0] - q[0], p[1] - q[1]
    d = math.hypot(dx, dy) or 1.0
    t = min(1.0, length / d)
    return (q[0] + dx * t, q[1] + dy * t)


class Spec:
    def __init__(self, name, w, h, border, pad=12, ch=(20, 6), fill=NAVY, fill_a=0.94, edge=CYAN_DIM, ew=3.0,
                 glow=None, glow_r=8.0, glow_s=0.0, inner=0.25, orn=("ticks", "line", "rivet"), orn_c=CYAN,
                 alpha=1.0, use="", shape="chamfer", hfade=False):
        self.name, self.w, self.h, self.border, self.pad, self.ch = name, w, h, border, pad, ch
        self.fill, self.fill_a, self.edge, self.ew = fill, fill_a, edge, ew
        self.glow, self.glow_r, self.glow_s, self.inner = glow or edge or CYAN, glow_r, glow_s, inner
        self.orn, self.orn_c, self.alpha, self.use, self.shape, self.hfade = orn, orn_c, alpha, use, shape, hfade


def outline(spec, x0, y0):
    """The frame outline in Blender pixels for an entry whose rect starts at (x0, y0) bottom-left."""
    p = spec.pad
    a, b = x0 + p, y0 + p
    c, d = x0 + spec.w - p, y0 + spec.h - p
    big, small = spec.ch
    if spec.shape == "tab":
        return A.chamfer_rect(a, b, c, d, big, big, 1, 1)
    if spec.shape == "hex":
        return A.hex_pts((a + c) / 2, (b + d) / 2, (c - a) / 2, flat_top=False)
    if spec.shape == "tri":
        return [(a, d), ((a + c) / 2, b), (c, d)]
    return A.chamfer_rect(a, b, c, d, big, small, big, small)


def _bar(L, a, b, hw, colour, z=3.0, bevel=0.9):
    A.px_poly(L["orn"], A.capsule_pts(a[0], a[1], b[0], b[1], hw), colour=colour, z=z, bevel=bevel, shaded=bevel > 0)


def _brackets(spec, pts, L):
    # Focus brackets just outside the frame at every corner: impossible to miss from the sofa.
    grown = A.offset_convex(pts, -5.5)
    n = len(grown)
    arm = 14.0
    oc = spec.orn_c
    if spec.shape == "chamfer":
        for i in (1, 3, 5, 7):
            poly = [_towards(grown[i - 1], grown[i], arm), grown[i], grown[(i + 1) % n],
                    _towards(grown[(i + 2) % n], grown[(i + 1) % n], arm)]
            for a, b in zip(poly, poly[1:]):
                _bar(L, a, b, 2.4, oc)
    else:
        for i in range(n):
            poly = [_towards(grown[i - 1], grown[i], arm * 0.9), grown[i], _towards(grown[(i + 1) % n], grown[i], arm * 0.9)]
            for a, b in zip(poly, poly[1:]):
                _bar(L, a, b, 2.2, oc)


def build_frame(spec, x0, y0, L):
    pts = outline(spec, x0, y0)
    if spec.fill is not None:
        A.px_poly(L["fill"], pts, colour=spec.fill, name=spec.name + "_fill")
    if spec.edge is not None and spec.ew > 0:
        A.px_poly(L["edge"], pts, [A.offset_convex(pts, spec.ew)], colour=spec.edge, bevel=min(1.4, spec.ew * 0.4),
                  shaded=True, z=1.0, name=spec.name + "_edge")
    oc = spec.orn_c
    big = spec.ch[0]
    p = spec.pad
    for kind in spec.orn:
        if kind == "line":
            # A faint inner rule, the second line a machined plate has.
            # Drawn as a closed chain of thin capsules: a 2D fill with a hole this close to the outline can fill in.
            inner = A.offset_convex(pts, spec.ew + 5.8)
            for a, b in zip(inner, inner[1:] + inner[:1]):
                _bar(L, a, b, 0.8, mix(spec.fill or NAVY, spec.edge or CYAN, 0.5), z=2.0, bevel=0)
        elif kind == "ticks" and spec.shape == "chamfer":
            inner = A.offset_convex(pts, spec.ew + 6)
            for i, j in ((1, 2), (5, 6)):
                m = ((inner[i][0] + inner[j][0]) / 2, (inner[i][1] + inner[j][1]) / 2)
                _bar(L, _towards(inner[i], m, big * 0.32), _towards(inner[j], m, big * 0.32), 2.2, oc, z=2.0, bevel=0.8)
        elif kind == "rivet" and spec.shape == "chamfer":
            # A small hex rivet in the top-right corner: the hex of our pads and gates.
            x1, y1 = x0 + spec.w - p - spec.ew - 10, y0 + spec.h - p - spec.ew - 10
            A.px_poly(L["orn"], A.hex_pts(x1, y1, 3.6), colour=oc, z=2.0, bevel=1.0, shaded=True)
        elif kind == "brackets":
            _brackets(spec, pts, L)
        elif kind == "underline":
            A.px_poly(L["orn"], A.chamfer_rect(x0 + p + 12, y0 + p + 4, x0 + spec.w - p - 12, y0 + p + 9, 0, 0, 2, 2),
                      colour=oc, z=2.0, bevel=1.0, shaded=True)
        elif kind == "highlight":
            y = y0 + spec.h - p - spec.ew - 4
            _bar(L, (x0 + p + big + 4, y), (x0 + spec.w - p - 10, y), 1.4, mix(spec.fill, ICE, 0.45), z=2.0, bevel=0)
        elif kind == "check":
            s = spec.w - 2 * p
            bx, by = x0 + p, y0 + p
            q = [(bx + s * 0.22, by + s * 0.52), (bx + s * 0.42, by + s * 0.3), (bx + s * 0.78, by + s * 0.7)]
            for a, b in zip(q, q[1:]):
                _bar(L, a, b, s * 0.08, oc, bevel=2.0)
        elif kind == "grip":
            cx, cy = x0 + spec.w / 2, y0 + spec.h / 2
            vertical = spec.h > spec.w * 1.5
            for k in (-1, 0, 1):
                if vertical:
                    _bar(L, (cx - 4, cy + k * 7), (cx + 4, cy + k * 7), 1.5, oc, bevel=0)
                else:
                    _bar(L, (cx + k * 7, cy - 6), (cx + k * 7, cy + 6), 1.5, oc, bevel=0)
        elif kind == "corner":
            s = spec.w
            q = [(x0 + 8, y0 + s - 32), (x0 + 8, y0 + s - 8), (x0 + 32, y0 + s - 8)]
            for a, b in zip(q, q[1:]):
                _bar(L, a, b, 2.6, oc, bevel=1.0)
            A.px_poly(L["orn"], A.hex_pts(x0 + 20, y0 + s - 20, 4.2), colour=oc, z=3.0, bevel=1.0, shaded=True)
        elif kind == "rule":
            cy = y0 + spec.h / 2
            _bar(L, (x0 + 6, cy), (x0 + spec.w - 6, cy), 1.5, oc, z=2.0, bevel=0)
            A.px_poly(L["orn"], A.hex_pts(x0 + spec.w / 2, cy, 4.5, flat_top=False), colour=oc, z=3.0, bevel=1.0, shaded=True)


def compose_frame(spec, f, e, o):
    """Glow, fill, inner glow, edge, ornaments; straight alpha out."""
    h, w = f.shape[:2]
    img = np.zeros((h, w, 4))
    if spec.glow_s > 0:
        g = np.clip(A.blur(e[..., 3], spec.glow_r) * spec.glow_s, 0, 1)
        img = A.over(img, A.solid(g, spec.glow))
    if spec.fill is not None:
        img = A.over(img, A.premul(np.concatenate([f[..., :3], f[..., 3:4] * spec.fill_a], -1)))
        if spec.inner > 0:
            ig = np.clip(A.blur(e[..., 3], 5.0) * spec.inner, 0, 1) * f[..., 3]
            img = A.over(img, A.solid(ig, spec.glow))
    for layer_img in (e, o):
        img = A.over(img, A.premul(np.concatenate([np.clip(layer_img[..., :3], 0, 1), layer_img[..., 3:4]], -1)))
    if spec.hfade:
        x = (np.arange(w) + 0.5) / w
        img *= (np.clip(np.minimum(x, 1 - x) * 4, 0, 1) ** 1.5)[None, :, None]
    img *= spec.alpha
    return A.unpremul(img)


def kit_specs():
    S = []
    pb = (44, 44, 44, 44)
    S.append(Spec("Panel", 160, 160, pb, use="UiSkin._panel: every framed panel (HUD groups, menus, result)"))
    S.append(Spec("Panel_Selected", 160, 160, pb, fill=NAVY_2, edge=ICE, ew=4, glow=CYAN, glow_s=1.3, inner=0.4,
                  orn=("ticks", "line", "rivet", "brackets"), orn_c=SIGNAL, use="a focused/selected panel or list row"))
    S.append(Spec("Panel_Disabled", 160, 160, pb, fill=GREY_FILL, edge=GREY_EDGE, inner=0.1, orn_c=GREY_EDGE, alpha=0.85,
                  use="a locked/disabled panel"))
    S.append(Spec("PanelSquare", 160, 160, (32, 32, 32, 32), ch=(6, 6), orn=("line",), use="UiSkin._panelSquare"))
    tip = A.hex_to_rgb("#050C16")
    S.append(Spec("Tooltip", 160, 112, (30, 30, 30, 30), ch=(10, 3), fill=tip, fill_a=0.97, edge=CYAN, ew=2.0,
                  inner=0.2, orn=("rivet",), use="tooltips and the tower info strip"))
    S.append(Spec("TooltipPointer", 40, 32, (0, 0, 0, 0), pad=4, shape="tri", fill=tip, fill_a=0.97, edge=None, ew=0,
                  orn=(), inner=0, use="pointer under a tooltip (overlap its bottom edge by 2 px)"))
    cb = (44, 44, 44, 44)
    for colour, acc in ACCENT.items():
        S.append(Spec(f"Card_{colour}", 144, 176, cb, fill=mix(NAVY_2, acc, 0.16), edge=mix(acc, (0, 0, 0), 0.15), ew=3,
                      glow=acc, inner=0.35, orn_c=acc, use=f"UiSkin._card{colour}: tower/drone/path-object cards"))
    S.append(Spec("Card_Selected", 144, 176, cb, fill=mix(NAVY_2, CYAN, 0.24), edge=ICE, ew=4, glow=CYAN, glow_s=1.4, inner=0.5,
                  orn=("ticks", "line", "rivet", "brackets"), orn_c=SIGNAL, use="the focused card in the tower bar / loadout"))
    S.append(Spec("Card_Disabled", 144, 176, cb, fill=GREY_FILL, edge=GREY_EDGE, inner=0.1, orn_c=GREY_EDGE, alpha=0.85,
                  use="a card that cannot be afforded or is locked"))
    # Buttons: the focus state is unmistakable (light edge, glow, yellow brackets); pressed sinks darker.
    for base, w, h, border in (("Button", 192, 80, (36, 34, 36, 34)), ("ButtonSquare", 112, 112, (36, 36, 36, 36))):
        ch = (14, 4)
        key = "_button" if base == "Button" else "_buttonSquare"
        S.append(Spec(f"{base}_Normal", w, h, border, ch=ch, fill=BTN, edge=CYAN_DIM, ew=3, inner=0.3, orn=("ticks", "highlight"),
                      orn_c=CYAN, use=f"UiSkin.{key} (normal)"))
        S.append(Spec(f"{base}_Focus", w, h, border, ch=ch, fill=BTN_FOCUS, edge=ICE, ew=4.5, glow=CYAN, glow_s=1.5, inner=0.55,
                      orn=("ticks", "highlight", "brackets"), orn_c=SIGNAL, use="highlighted + selected (gamepad focus, mouse hover)"))
        S.append(Spec(f"{base}_Pressed", w, h, border, ch=ch, fill=BTN_PRESS, edge=CYAN, ew=3, glow=CYAN, glow_s=0.6, inner=0.15,
                      orn=("ticks",), orn_c=CYAN, use="pressed"))
        S.append(Spec(f"{base}_Disabled", w, h, border, ch=ch, fill=GREY_FILL, edge=GREY_EDGE, ew=3, inner=0.0, orn=(),
                      alpha=0.8, use="disabled"))
        S.append(Spec(f"{base}_Neutral", w, h, border, ch=ch, fill=(0.80, 0.82, 0.86), edge=(1, 1, 1), ew=3, inner=0.0,
                      orn=("highlight",), use="light grey version for the ColorBlock tint transition (UiKit.ButtonColors)"))
    barb = (12, 12, 12, 12)
    S.append(Spec("Bar_Back", 96, 32, barb, pad=2, ch=(6, 2), fill=A.hex_to_rgb("#050B14"), fill_a=0.9, edge=CYAN_DIM, ew=2,
                  inner=0.0, orn=(), use="UiSkin._barBack, slider track"))
    for colour, acc in list(ACCENT.items()) + [("White", (0.92, 0.94, 0.96))]:
        S.append(Spec(f"Bar_{colour}", 96, 32, barb, pad=2, ch=(6, 2), fill=acc, fill_a=1.0, edge=mix(acc, (1, 1, 1), 0.45), ew=2,
                      inner=0.0, orn=("highlight",), use=f"UiSkin._bar{colour} fill" if colour != "White" else "tintable bar fill"))
    S.append(Spec("Slider_Handle", 56, 56, (0, 0, 0, 0), pad=6, shape="hex", fill=BTN, edge=CYAN, ew=3, inner=0.3, orn=("grip",),
                  orn_c=CYAN, use="slider handle (normal)"))
    S.append(Spec("Slider_Handle_Focus", 56, 56, (0, 0, 0, 0), pad=6, shape="hex", fill=BTN_FOCUS, edge=ICE, ew=4, glow=CYAN,
                  glow_s=1.5, glow_r=5, inner=0.5, orn=("grip",), orn_c=SIGNAL, use="slider handle (focused)"))
    tb = (30, 12, 30, 30)
    S.append(Spec("Tab", 160, 72, tb, pad=6, shape="tab", ch=(14, 0), fill=NAVY, fill_a=0.9, edge=CYAN_DIM, ew=2.5, inner=0.15,
                  orn=(), use="an unselected tab"))
    S.append(Spec("Tab_Selected", 160, 72, tb, pad=6, shape="tab", ch=(14, 0), fill=BTN_FOCUS, edge=ICE, ew=3.5, glow=CYAN,
                  glow_s=0.9, glow_r=5, inner=0.4, orn=("underline",), orn_c=SIGNAL, use="the selected tab"))
    track = A.hex_to_rgb("#050B14")
    S.append(Spec("Scroll_Track", 24, 128, (10, 12, 10, 12), pad=2, ch=(5, 2), fill=track, fill_a=0.85, edge=CYAN_DIM, ew=1.5,
                  inner=0, orn=(), use="ScrollRect vertical bar background"))
    S.append(Spec("Scroll_Thumb", 24, 64, (10, 14, 10, 14), pad=2, ch=(5, 2), fill=CYAN_DIM, fill_a=1.0, edge=CYAN, ew=2,
                  inner=0, orn=("grip",), orn_c=ICE, use="scroll bar handle"))
    S.append(Spec("Scroll_Thumb_Focus", 24, 64, (10, 14, 10, 14), pad=2, ch=(5, 2), fill=CYAN, fill_a=1.0, edge=ICE, ew=2,
                  inner=0, orn=("grip",), orn_c=NAVY, use="scroll bar handle while dragged/focused"))
    S.append(Spec("Toggle_Off", 56, 56, (0, 0, 0, 0), pad=6, ch=(8, 3), fill=NAVY, edge=CYAN_DIM, ew=3, inner=0.2, orn=(),
                  use="checkbox off"))
    S.append(Spec("Toggle_On", 56, 56, (0, 0, 0, 0), pad=6, ch=(8, 3), fill=BTN, edge=CYAN, ew=3, glow=CYAN, glow_s=0.7,
                  glow_r=4, inner=0.4, orn=("check",), orn_c=ICE, use="checkbox on"))
    S.append(Spec("FocusFrame", 128, 128, (40, 40, 40, 40), fill=None, edge=ICE, ew=3.5, glow=CYAN, glow_s=1.5, inner=0,
                  orn=("brackets",), orn_c=SIGNAL, use="overlay on any focused element (map nodes, portraits) without its own focus sprite"))
    S.append(Spec("CornerOrnament", 48, 48, (0, 0, 0, 0), pad=0, fill=None, edge=None, ew=0, inner=0, orn=("corner",), orn_c=ICE,
                  use="loose corner bracket for headers; rotate/flip for the other corners; tint freely"))
    S.append(Spec("Divider", 256, 16, (0, 0, 0, 0), pad=0, fill=None, edge=None, ew=0, inner=0, orn=("rule",), orn_c=CYAN,
                  hfade=True, use="horizontal rule between list sections (Simple, stretch horizontally)"))
    return S


KIT_WIDTH = 1024


def build_kit():
    specs = kit_specs()
    rects, used = A.shelf_pack([(s.name, s.w, s.h) for s in specs], KIT_WIDTH, 4)
    W, H = KIT_WIDTH, (used + 3) // 4 * 4
    A.reset()
    A._layers.clear()
    A.camera(W, H)
    L = {k: A.layer(k) for k in ("fill", "edge", "orn")}
    for s in specs:
        x, y, w, h = rects[s.name]
        build_frame(s, x, H - y - h, L)
    lay = {k: A.render(W, H, only={k}, samples=32) for k in L}
    atlas = np.zeros((H, W, 4))
    entries = {}
    for s in specs:
        x, y, w, h = rects[s.name]
        atlas[y:y + h, x:x + w] = compose_frame(s, *[lay[k][y:y + h, x:x + w] for k in ("fill", "edge", "orn")])
        entries[s.name] = {"x": x, "y": y, "w": w, "h": h, "unity_rect": [x, H - y - h, w, h],
                           "border_lbrt": list(s.border), "pivot": [0.5, 0.5], "glow_pad": s.pad, "use": s.use}
    A.save_png(out("UI_Kit.png"), atlas)
    meta = {"texture": "UI_Kit.png", "size": [W, H],
            "scale": "2x master: import with Pixels Per Unit 200 (or Image.pixelsPerUnitMultiplier 0.5) so borders read at 1x",
            "coords": "x, y from the top-left; unity_rect from the bottom-left; border_lbrt = Sprite Editor L, B, R, T (2x px)",
            "glow_pad": "transparent margin (2x px) around the frame that holds the glow; all states of one element share it",
            "sprites": entries}
    with open(out("UI_Kit.json"), "w") as fh:
        json.dump(meta, fh, indent=1)
    REPORT["files"]["UI_Kit.png"] = {"size": [W, H], "entries": len(entries),
                                      "replaces": "Kenney UI pack sprites referenced by Assets/_Project/Settings/UiSkin.asset (panel, panelSquare, "
                                                  "button, buttonSquare, barBack, bar*, card*) + UiKit slider/scroll/toggle styling"}
    return atlas, entries


# ================================================================================================ SHADER NODES

class G:
    """A tiny wrapper to wire scalar/vector math in a material without a page of node boilerplate."""

    def __init__(self, nt):
        self.nt = nt

    def _in(self, node, i, value):
        if hasattr(value, "is_linked") or hasattr(value, "links"):
            self.nt.links.new(value, node.inputs[i])
        else:
            node.inputs[i].default_value = value

    def m(self, op, a, b=0.0, clamp=False):
        n = self.nt.nodes.new("ShaderNodeMath")
        n.operation = op
        n.use_clamp = clamp
        self._in(n, 0, a)
        self._in(n, 1, b)
        return n.outputs[0]

    def v(self, op, a, b=(0.0, 0.0, 0.0), scale=None):
        n = self.nt.nodes.new("ShaderNodeVectorMath")
        n.operation = op
        self._in(n, 0, a)
        if op == "SCALE":
            self._in(n, 3, scale)
        else:
            self._in(n, 1, b)
        return n.outputs[1] if op in ("LENGTH", "DOT_PRODUCT", "DISTANCE") else n.outputs[0]

    def mapr(self, x, a, b, c, d, smooth=False):
        n = self.nt.nodes.new("ShaderNodeMapRange")
        n.clamp = True
        if smooth:
            n.interpolation_type = "SMOOTHSTEP"
        self._in(n, "Value", x)
        n.inputs["From Min"].default_value = a
        n.inputs["From Max"].default_value = b
        n.inputs["To Min"].default_value = c
        n.inputs["To Max"].default_value = d
        return n.outputs[0]

    def noise(self, vec, scale, detail=4.0, rough=0.55):
        n = self.nt.nodes.new("ShaderNodeTexNoise")
        self.nt.links.new(vec, n.inputs["Vector"])
        n.inputs["Scale"].default_value = scale
        n.inputs["Detail"].default_value = detail
        n.inputs["Roughness"].default_value = rough
        return n.outputs["Fac"]

    def combine(self, x, y, z=0.0):
        n = self.nt.nodes.new("ShaderNodeCombineXYZ")
        for i, val in enumerate((x, y, z)):
            self._in(n, i, val)
        return n.outputs[0]

    def separate(self, vec):
        n = self.nt.nodes.new("ShaderNodeSeparateXYZ")
        self.nt.links.new(vec, n.inputs[0])
        return n.outputs[0], n.outputs[1]

    def uv(self):
        return self.nt.nodes.new("ShaderNodeTexCoord").outputs["UV"]

    def centred(self):
        """UV moved so the quad's centre is 0 and its edges are +-1, plus its length."""
        c = self.v("SCALE", self.v("SUBTRACT", self.uv(), (0.5, 0.5, 0.0)), scale=2.0)
        return c, self.v("LENGTH", c)

    def emission(self, colour, strength=1.0):
        n = self.nt.nodes.new("ShaderNodeEmission")
        self._in(n, 0, colour)
        self._in(n, 1, strength)
        return n.outputs[0]

    def alpha_out(self, out, fac, colour=(1, 1, 1, 1)):
        """Emission of `colour` with coverage `fac` (Cycles writes it as alpha)."""
        mix = self.nt.nodes.new("ShaderNodeMixShader")
        self._in(mix, 0, fac)
        self.nt.links.new(self.nt.nodes.new("ShaderNodeBsdfTransparent").outputs[0], mix.inputs[1])
        self.nt.links.new(self.emission(colour), mix.inputs[2])
        self.nt.links.new(mix.outputs[0], out.inputs["Surface"])

    def additive_out(self, out, colour):
        add = self.nt.nodes.new("ShaderNodeAddShader")
        self.nt.links.new(self.emission(colour), add.inputs[0])
        self.nt.links.new(self.nt.nodes.new("ShaderNodeBsdfTransparent").outputs[0], add.inputs[1])
        self.nt.links.new(add.outputs[0], out.inputs["Surface"])


def fac_material(name, fac_fn, colour=(1, 1, 1, 1)):
    def build(nt, out):
        g = G(nt)
        g.alpha_out(out, fac_fn(g), colour)
    return A.gradient_material(name, build)


def radial_fac(power):
    def fn(g):
        _, r = g.centred()
        return g.m("POWER", g.m("SUBTRACT", 1.0, r, clamp=True), power)
    return fn


# ===================================================================================================== DIALOGUE
# White pieces the dialogue box, cutscene captions and radar tint at runtime (as DialogueArt did), drawn in the
# kit's chamfered language. Panel and Edge share one outline, so the light sits exactly on the panel's border.

DLG_SLICE = 128
DLG_INSET = 36        # 2x of DialogueArt.SliceInset (EdgeOverhang 18 at 1x)
DLG_BORDER = 60       # 2x of DialogueArt.SliceBorder


def _dlg_outline(x0, y0, size=DLG_SLICE, inset=DLG_INSET, big=22, small=8):
    return A.chamfer_rect(x0 + inset, y0 + inset, x0 + size - inset, y0 + size - inset, big, small, big, small)


def sweep_fac(g):
    c, r = g.centred()
    x, y = g.separate(c)
    a = g.m("ARCTAN2", y, x)
    lead = math.pi / 2
    trail = g.m("POWER", g.mapr(a, lead, lead + math.pi / 3, 1.0, 0.0), 2.2)
    trail = g.m("MULTIPLY", trail, g.m("GREATER_THAN", a, lead))
    line = g.m("SUBTRACT", 1.0, g.mapr(g.m("ABSOLUTE", g.m("SUBTRACT", a, lead)), 0.0, 0.035, 0.0, 1.0), clamp=True)
    fac = g.m("MAXIMUM", g.m("MULTIPLY", trail, 0.55), line)
    rim = g.mapr(r, 0.96, 1.0, 1.0, 0.0)
    outward = g.mapr(r, 0.0, 1.0, 0.35, 1.0)
    return g.m("MULTIPLY", g.m("MULTIPLY", fac, rim), outward)


def build_dialogue():
    items = [("Dialogue_Panel", 128, 128), ("Dialogue_Edge", 128, 128), ("Dialogue_NameTag", 160, 64),
             ("Dialogue_PortraitFrame", 160, 160), ("Dialogue_Arrow", 64, 64), ("Radar_Sweep", 512, 512),
             ("Radar_Contact", 64, 64)]
    rects, used = A.shelf_pack(items, 1024, 4)
    W, H = 1024, (used + 3) // 4 * 4
    A.reset()
    A._layers.clear()
    A.camera(W, H)
    L = {k: A.layer(k) for k in ("fill", "line", "mask", "grad")}

    def bl(name):
        x, y, w, h = rects[name]
        return x, H - y - h

    x, y = bl("Dialogue_Panel")
    A.px_poly(L["fill"], _dlg_outline(x, y))
    x, y = bl("Dialogue_Edge")
    pts = _dlg_outline(x, y)
    A.px_poly(L["mask"], pts)
    A.px_poly(L["line"], pts, [A.offset_convex(pts, 3.2)], bevel=1.2, shaded=True)
    x, y = bl("Dialogue_NameTag")
    tag = A.chamfer_rect(x + 8, y + 8, x + 152, y + 56, 14, 4, 14, 4)
    A.px_poly(L["fill"], tag)
    A.px_poly(L["line"], tag, [A.offset_convex(tag, 2.5)], bevel=1.0, shaded=True)
    x, y = bl("Dialogue_PortraitFrame")
    frame = A.chamfer_rect(x + 10, y + 10, x + 150, y + 150, 18, 6, 18, 6)
    A.px_poly(L["line"], frame, [A.offset_convex(frame, 3.0)], bevel=1.2, shaded=True)
    spec = Spec("pf", 140, 140, (0, 0, 0, 0), pad=0, ch=(18, 6), orn_c=(1, 1, 1))
    _brackets(spec, frame, {"orn": L["line"]})
    x, y = bl("Dialogue_Arrow")
    A.Cell(x + 8, y + 8, 48, L["fill"], bevel=3.0, shaded=True).poly([(0.12, 0.74), (0.88, 0.74), (0.5, 0.2)])
    x, y = bl("Radar_Sweep")
    A.plane(L["grad"], x, y, x + 512, y + 512, fac_material("Sweep", sweep_fac))
    x, y = bl("Radar_Contact")
    A.plane(L["grad"], x, y, x + 64, y + 64, fac_material("ContactGlow", radial_fac(2.4)))
    A.px_poly(L["fill"], A.hex_pts(x + 32, y + 32, 8.0, flat_top=False), bevel=1.5, shaded=True, z=2.0)

    lay = {k: A.render(W, H, only={k}, samples=32) for k in L}
    atlas = np.zeros((H, W, 4))
    for name, (x, y, w, h) in rects.items():
        f, ln, mk, gr = (lay[k][y:y + h, x:x + w] for k in ("fill", "line", "mask", "grad"))
        if name == "Dialogue_Edge":
            line_a = ln[..., 3]
            inside = mk[..., 3]
            outer = np.clip(A.blur(line_a, 9.0) * 1.4, 0, 0.6) * (1 - inside)
            inner = np.clip(A.blur(line_a, 4.0) * 0.9, 0, 0.35) * inside
            a = np.maximum(line_a * np.clip(ln[..., 0], 0, 1), np.maximum(outer, inner))
            a = np.maximum(a, line_a * 0.85)
            img = np.dstack([np.ones_like(a)] * 3 + [a])
        elif name in ("Radar_Sweep",):
            img = gr
        elif name == "Radar_Contact":
            img = A.unpremul(A.over(A.premul(gr), A.premul(np.concatenate([np.clip(f[..., :3], 0, 1), f[..., 3:4]], -1))))
        else:
            base = A.premul(np.concatenate([np.clip(f[..., :3], 0, 1), f[..., 3:4]], -1))
            if name == "Dialogue_PortraitFrame":
                glow = A.solid(np.clip(A.blur(ln[..., 3], 6.0) * 0.8, 0, 0.5), (1, 1, 1))
                base = A.over(glow, base)
            base = A.over(base, A.premul(np.concatenate([np.clip(ln[..., :3], 0, 1), ln[..., 3:4]], -1)))
            if name == "Dialogue_NameTag":
                # The tag is tinted as a whole; keep the fill a step darker than its rim.
                base[..., :3] *= np.where(ln[..., 3:4] > 0.5, 1.0, 0.82)
            img = A.unpremul(base)
        atlas[y:y + h, x:x + w] = img
    A.save_png(out("UI_Dialogue.png"), atlas)
    borders = {"Dialogue_Panel": [DLG_BORDER] * 4, "Dialogue_Edge": [DLG_BORDER] * 4, "Dialogue_NameTag": [26, 20, 26, 20],
               "Dialogue_PortraitFrame": [44, 44, 44, 44]}
    uses = {"Dialogue_Panel": "DialogueArt.Panel (DialogueView, CutsceneOverlay:195)",
            "Dialogue_Edge": "DialogueArt.Edge (offset by EdgeOverhang = 18 px at 1x, same geometry as the panel)",
            "Dialogue_NameTag": "speaker name plate above the box", "Dialogue_PortraitFrame": "frame around the portrait",
            "Dialogue_Arrow": "DialogueArt.Arrow (the 'next' cue, points down)",
            "Radar_Sweep": "RadarOverlay sweep: replace the 7 trail lines; leading edge points up, trail on the left (it turns clockwise)",
            "Radar_Contact": "RadarOverlay contact dots / hostile mass (instead of DialogueArt.Radial)"}
    entries = {n: {"x": x, "y": y, "w": w, "h": h, "unity_rect": [x, H - y - h, w, h], "border_lbrt": borders.get(n, [0, 0, 0, 0]),
                   "pivot": [0.5, 0.5], "use": uses[n]} for n, (x, y, w, h) in rects.items()}

    # Tiling textures (each its own file: RawImage uvRect tiling needs Repeat wrap, which an atlas cannot give).
    A.reset()
    A._layers.clear()
    singles = {}

    def noise_fac(g):
        uvs = g.uv()
        cell = g.v("FLOOR", g.v("MULTIPLY", uvs, (256.0, 32.0, 1.0)))
        wn = g.nt.nodes.new("ShaderNodeTexWhiteNoise")
        wn.noise_dimensions = "3D"
        g.nt.links.new(cell, wn.inputs["Vector"])
        return wn.outputs["Value"]

    def noise_build(nt, o):
        g = G(nt)
        nt.links.new(g.emission(noise_fac(g)), o.inputs["Surface"])

    for name, w, h, make in (
            ("UI_Dialogue_Noise.png", 256, 32, lambda: A.gradient_material("Noise", noise_build)),
            ("UI_Dialogue_Radial.png", 128, 128, lambda: fac_material("Radial", radial_fac(2.0))),
            ("UI_Dialogue_Shade.png", 4, 128, lambda: fac_material(
                "Shade", lambda g: g.mapr(g.separate(g.uv())[1], 1.0, 0.0, 0.0, 1.0, smooth=True)))):
        A.reset()
        A._layers.clear()
        A.camera(w, h)
        A.plane(A.layer("p"), 0, 0, w, h, make())
        singles[name] = A.render(w, h, samples=16 if "Noise" in name else 32)
    noise = singles["UI_Dialogue_Noise.png"]
    v = np.clip(noise[..., 0], 0, 1) * 0.16
    singles["UI_Dialogue_Noise.png"] = np.dstack([v, v, v * 1.15, np.ones_like(v)])
    # Scanlines: one dark line every four pixels at 1x (2x: 8 rows), as DialogueArt drew them.
    A.reset()
    A._layers.clear()
    A.camera(8, 8)
    for row, alpha in ((4, 0.55), (5, 0.55), (6, 0.25), (7, 0.25)):
        A.plane(A.layer("s"), 0, 7 - row, 8, 8 - row, fac_material(f"Scan{row}", lambda g, a=alpha: a))
    singles["UI_Dialogue_Scanlines.png"] = A.render(8, 8, samples=8)
    for name, img in singles.items():
        A.save_png(out(name), img)

    meta = {"texture": "UI_Dialogue.png", "size": [W, H], "scale": "2x master (Pixels Per Unit 200)",
            "tint": "all white: DialogueView / CutsceneOverlay / RadarOverlay tint them as before",
            "sprites": entries,
            "textures": {"UI_Dialogue_Noise.png": "DialogueArt.Noise (redacted bars; wrap Repeat, filter Point)",
                         "UI_Dialogue_Scanlines.png": "DialogueArt.Scanlines (wrap Repeat, filter Point; 8 rows = 4 px at 1x)",
                         "UI_Dialogue_Radial.png": "DialogueArt.Radial (Clamp, Bilinear)",
                         "UI_Dialogue_Shade.png": "DialogueArt.Shade (Clamp, Bilinear; opaque at the bottom)"}}
    with open(out("UI_Dialogue.json"), "w") as fh:
        json.dump(meta, fh, indent=1)
    REPORT["files"]["UI_Dialogue.png"] = {"size": [W, H], "entries": list(entries),
                                           "replaces": "Assets/_Project/Scripts/UI/DialogueArt.cs:55-217 (Panel, Edge, Arrow) + "
                                                       "RadarOverlay.cs sweep arms/contacts"}
    for name, desc in meta["textures"].items():
        REPORT["files"][name] = {"size": list(singles[name].shape[1::-1]), "replaces": desc}
    return atlas, entries, singles


# ======================================================================================================== RADAR

RADAR = 1024
TEAL = (0.3, 0.95, 0.85)


def build_radar():
    A.reset()
    A._layers.clear()
    A.camera(RADAR, RADAR)
    L = {k: A.layer(k) for k in ("back", "grid", "bezel")}
    c = RADAR / 2
    R = RADAR * 0.46

    def back_build(nt, out):
        g = G(nt)
        _, r = g.centred()
        cov = g.mapr(r, 0.995, 1.0, 1.0, 0.0)
        shade = g.mapr(r, 0.0, 1.0, 1.0, 0.55)
        colour = g.v("SCALE", (0.03, 0.13, 0.14), scale=shade)
        mix = nt.nodes.new("ShaderNodeMixShader")
        g._in(mix, 0, cov)
        nt.links.new(nt.nodes.new("ShaderNodeBsdfTransparent").outputs[0], mix.inputs[1])
        nt.links.new(g.emission(colour), mix.inputs[2])
        nt.links.new(mix.outputs[0], out.inputs["Surface"])

    A.plane(L["back"], c - R, c - R, c + R, c + R, A.gradient_material("RadarBack", back_build))
    for k in (1, 2, 3):
        rr = R * k / 3.0
        w = 2.2 if k < 3 else 3.0
        A.px_poly(L["grid"], A.circle_pts(c, c, rr + w, 192), [A.circle_pts(c, c, rr - w, 192)], colour=TEAL, z=1.0)
    for ang in (0, 90):
        a = math.radians(ang)
        A.px_poly(L["grid"], A.capsule_pts(c - R * math.cos(a), c - R * math.sin(a), c + R * math.cos(a), c + R * math.sin(a), 1.6),
                  colour=TEAL, z=1.0)
    for deg in range(0, 360, 10):
        a = math.radians(deg)
        long_tick = deg % 30 == 0
        r0 = R - (26 if long_tick else 13)
        A.px_poly(L["grid"], A.capsule_pts(c + r0 * math.cos(a), c + r0 * math.sin(a), c + (R - 3) * math.cos(a), c + (R - 3) * math.sin(a),
                                           2.0 if long_tick else 1.4), colour=TEAL, z=1.0)
    A.px_poly(L["grid"], A.hex_pts(c, c, 9.0, flat_top=False), colour=TEAL, z=2.0, bevel=2.0, shaded=True)
    # The bezel: a machined ring with four clamps, our faction's steel and a teal inlay.
    steel = (0.32, 0.42, 0.48)
    A.px_poly(L["bezel"], A.circle_pts(c, c, R + 30, 256), [A.circle_pts(c, c, R + 2, 256)], colour=steel, bevel=5.0, shaded=True, z=3.0)
    A.px_poly(L["bezel"], A.circle_pts(c, c, R + 16, 256), [A.circle_pts(c, c, R + 12, 256)], colour=TEAL, z=4.0)
    for ang in (45, 135, 225, 315):
        a = math.radians(ang)
        px, py = c + (R + 16) * math.cos(a), c + (R + 16) * math.sin(a)
        A.px_poly(L["bezel"], A.hex_pts(px, py, 26.0), colour=(0.42, 0.54, 0.6),
                  bevel=4.0, shaded=True, z=5.0)
        A.px_poly(L["bezel"], A.hex_pts(px, py, 8.0), colour=TEAL, z=6.0, bevel=1.5, shaded=True)
    lay = {k: A.render(RADAR, RADAR, only={k}, samples=32) for k in L}
    img = A.premul(lay["back"])
    img[..., 3] *= 0.92
    img[..., :3] *= 0.92
    grid = lay["grid"]
    glow = A.solid(np.clip(A.blur(grid[..., 3], 6.0) * 0.5, 0, 0.4), TEAL)
    img = A.over(img, glow)
    g2 = np.concatenate([np.clip(grid[..., :3], 0, 1), grid[..., 3:4] * 0.55], -1)
    img = A.over(img, A.premul(g2))
    bez = lay["bezel"]
    shadow = A.solid(np.clip(A.blur(bez[..., 3], 8.0) * 0.8, 0, 0.6), (0, 0.01, 0.02))
    img = A.over(img, shadow)
    img = A.over(img, A.premul(np.concatenate([np.clip(bez[..., :3], 0, 1), bez[..., 3:4]], -1)))
    final = A.unpremul(img)
    A.save_png(out("UI_Radar.png"), final)
    REPORT["files"]["UI_Radar.png"] = {"size": [RADAR, RADAR], "scope_radius_px": round(R),
                                        "replaces": "RadarOverlay.cs:66-75 (Back radial, Ring1-3, Cross lines); the sweep and contacts are "
                                                    "Radar_Sweep / Radar_Contact in UI_Dialogue. Pre-coloured: Image.color white. Draw at "
                                                    f"Size * {RADAR / (2 * R):.3f} so the scope circle matches Size."}
    return final


# ==================================================================================================== STARFIELD
SKY_W, SKY_H = 2048, 1152
NEBULA_A, NEBULA_B, BAND = (0.32, 0.12, 0.42), (0.06, 0.32, 0.42), (0.62, 0.56, 0.5)
STAR_COLOURS = [((0.68, 0.78, 1.0), 0.25), ((0.92, 0.95, 1.0), 0.4), ((1.0, 0.94, 0.82), 0.25), ((1.0, 0.76, 0.55), 0.1)]


def sky_build(nt, out):
    g = G(nt)
    aspect = SKY_W / SKY_H
    u, v = g.separate(g.uv())
    px = g.m("MULTIPLY", u, aspect)
    vec = g.combine(px, v, 0.0)
    vec2 = g.combine(g.m("ADD", px, 9.0), g.m("ADD", v, 2.0), 0.0)
    vec3 = g.combine(px, g.m("ADD", v, 3.0), 4.0)
    width = 0.16
    wobble = g.m("MULTIPLY", g.m("SUBTRACT", g.noise(vec, 1.5, 3.0), 0.5), width)
    centre = g.m("ADD", 0.62, g.m("MULTIPLY", -0.45, g.m("SUBTRACT", u, 0.5)))
    across = g.m("ADD", g.m("SUBTRACT", v, centre), wobble)

    def bell(x, w):
        q = g.m("DIVIDE", x, w)
        return g.m("EXPONENT", g.m("MULTIPLY", -1.0, g.m("MULTIPLY", q, q)))

    band = bell(across, width)
    core = bell(across, width * 0.35)
    gas = g.mapr(g.noise(g.combine(g.m("ADD", px, 4.0), v, 1.0), 3.2, 6.0, 0.6), 0.25, 0.8, 0.0, 1.0)
    mixing = g.mapr(g.noise(vec2, 1.6, 4.0), 0.3, 0.7, 0.0, 1.0, smooth=True)
    nebula_col = g.v("ADD", g.v("SCALE", NEBULA_A, scale=g.m("SUBTRACT", 1.0, mixing)), g.v("SCALE", NEBULA_B, scale=mixing))
    nebula = g.v("SCALE", nebula_col, scale=g.m("MULTIPLY", g.m("POWER", gas, 2.2), 2.5))
    dust = g.mapr(g.noise(vec3, 6.0, 4.0), 0.5, 0.72, 0.0, 1.0, smooth=True)
    light = g.v("ADD", g.v("SCALE", BAND, scale=g.m("ADD", g.m("MULTIPLY", core, 0.8), g.m("MULTIPLY", band, 0.4))),
                g.v("SCALE", nebula, scale=band))
    light = g.v("SCALE", light, scale=g.m("SUBTRACT", 1.0, g.m("MULTIPLY", g.m("MULTIPLY", dust, core), 0.85)))
    light = g.v("ADD", light, g.v("SCALE", nebula, scale=g.m("MULTIPLY", g.m("POWER", gas, 3.0), 0.12)))
    nt.links.new(g.emission(g.v("SCALE", light, scale=0.4)), out.inputs["Surface"])


def star_build(nt, out):
    g = G(nt)
    _, r = g.centred()
    fall = g.m("POWER", g.m("SUBTRACT", 1.0, r, clamp=True), 2.6)
    attr = nt.nodes.new("ShaderNodeAttribute")
    attr.attribute_name = "Col"
    g.additive_out(out, g.v("SCALE", attr.outputs["Color"], scale=fall))


def _stars_mesh(coll, rng):
    verts, faces, cols = [], [], []

    def quad(x, y, hw, hh, col):
        i = len(verts)
        verts.extend([(x - hw, y - hh, 1.0), (x + hw, y - hh, 1.0), (x + hw, y + hh, 1.0), (x - hw, y + hh, 1.0)])
        faces.append((i, i + 1, i + 2, i + 3))
        cols.extend([col] * 4)

    weights = np.array([w for _, w in STAR_COLOURS])
    count = int(SKY_W * SKY_H * 0.0035 * 0.5)
    for _ in range(count):
        x, y = rng.uniform(0, SKY_W), rng.uniform(0, SKY_H)
        roll = rng.random()
        base = np.array(STAR_COLOURS[rng.choice(len(STAR_COLOURS), p=weights / weights.sum())][0])
        level = 0.25 + 0.75 * roll ** 2.2
        bright = rng.random() < 0.04
        if bright:
            col = tuple(base * (1.6 + 1.4 * rng.random())) + (1.0,)
            quad(x, y, 6.5, 6.5, col)
            arm = 14 + 22 * rng.random()
            dim = tuple(c * 0.55 for c in col[:3]) + (1.0,)
            quad(x, y, arm, 1.3, dim)
            quad(x, y, 1.3, arm, dim)
        else:
            s = 1.6 + 1.6 * roll
            quad(x, y, s, s, tuple(base * level * 1.4) + (1.0,))
    me = A.bpy.data.meshes.new("Stars")
    me.from_pydata(verts, [], faces)
    uv = me.uv_layers.new(name="UVMap")
    uvs = np.tile(np.array([(0, 0), (1, 0), (1, 1), (0, 1)], np.float32), (len(faces), 1))
    uv.data.foreach_set("uv", uvs.ravel())
    ca = me.color_attributes.new("Col", "FLOAT_COLOR", "CORNER")
    ca.data.foreach_set("color", np.array(cols, np.float32).ravel())
    me.materials.append(A.gradient_material("Star", star_build))
    ob = A.bpy.data.objects.new("Stars", me)
    coll.objects.link(ob)


def build_starfield():
    A.reset()
    A._layers.clear()
    A.camera(SKY_W, SKY_H)
    coll = A.layer("sky")
    A.plane(coll, 0, 0, SKY_W, SKY_H, A.gradient_material("Sky", sky_build))
    _stars_mesh(coll, np.random.default_rng(7))
    img = A.render(SKY_W, SKY_H, samples=24)
    rgb = np.clip(img[..., :3], 0, 1)
    # As StarfieldPainter: alpha follows the light, so over a running level it only adds a faint glow.
    alpha = np.clip(rgb.max(axis=-1) * 3.0, 0, 1)
    # Fully clear pixels keep a dark navy colour so bilinear filtering never pulls black fringes in.
    straight = np.where(alpha[..., None] > 1e-4, rgb / np.maximum(alpha[..., None], 1e-4), np.array([0.05, 0.08, 0.14]))
    final = np.dstack([straight.clip(0, 1), alpha])
    A.save_png(out("UI_Starfield.png"), final)
    REPORT["files"]["UI_Starfield.png"] = {"size": [SKY_W, SKY_H], "replaces": "StarfieldPainter.Paint via UiKit.AddStarfield (UiKit.cs:278-291)",
                                            "note": "straight alpha = 3 x brightness, same rule as the painter; Clamp, Bilinear, no mips"}
    return final


# ===================================================================================================== BACKDROP
BACK = 1024
BACK_TOP, BACK_BOTTOM = (0.12, 0.2, 0.36), (0.03, 0.04, 0.08)


def backdrop_build(nt, out):
    g = G(nt)
    u, v = g.separate(g.uv())
    t = g.m("MULTIPLY", v, v)
    col = g.v("ADD", g.v("SCALE", BACK_BOTTOM, scale=g.m("SUBTRACT", 1.0, t)), g.v("SCALE", BACK_TOP, scale=t))
    c = g.v("MULTIPLY", g.v("SUBTRACT", g.uv(), (0.5, 0.58, 0.0)), (1.6, 2.0, 0.0))
    r = g.v("LENGTH", c)
    glow = g.m("POWER", g.m("SUBTRACT", 1.0, r, clamp=True), 2.0)
    col = g.v("ADD", col, g.v("SCALE", (0.05, 0.12, 0.2), scale=glow))
    vign = g.mapr(g.v("LENGTH", g.v("SCALE", g.v("SUBTRACT", g.uv(), (0.5, 0.5, 0.0)), scale=2.0)), 0.7, 1.45, 1.0, 0.55, smooth=True)
    nt.links.new(g.emission(g.v("SCALE", col, scale=vign)), out.inputs["Surface"])


def build_backdrop():
    A.reset()
    A._layers.clear()
    A.camera(BACK, BACK)
    A.plane(A.layer("back"), 0, 0, BACK, BACK, A.gradient_material("Backdrop", backdrop_build))
    grid = A.layer("grid")
    # A faint hex lattice, like the build-pad grid seen from far away.
    r = 46.0
    dx, dy = r * 1.5, r * math.sqrt(3)
    for i in range(-1, int(BACK / dx) + 2):
        for j in range(-1, int(BACK / dy) + 2):
            cx, cy = i * dx, j * dy + (dy / 2 if i % 2 else 0)
            pts = A.hex_pts(cx, cy, r)
            for a, b in zip(pts, pts[1:] + pts[:1]):
                A.px_poly(grid, A.capsule_pts(a[0], a[1], b[0], b[1], 1.1, 4), colour=(0.35, 0.75, 1.0), z=1.0)
    back = A.render(BACK, BACK, only={"back"}, samples=24)
    lines = A.render(BACK, BACK, only={"grid"}, samples=16)
    yy = (np.arange(BACK) + 0.5) / BACK
    # Strongest just below the middle (where the model's floor sits), gone at the top and bottom.
    fade = np.exp(-((yy - 0.62) / 0.2) ** 2)[:, None] * 0.07
    rgb = back[..., :3] + lines[..., :3] * (lines[..., 3] * fade)[..., None]
    final = np.dstack([np.clip(rgb, 0, 1), np.ones((BACK, BACK))])
    A.save_png(out("UI_PreviewBackdrop.png"), final)
    REPORT["files"]["UI_PreviewBackdrop.png"] = {"size": [BACK, BACK], "replaces": "ModelPreview.Gradient (ModelPreview.cs:239-249)",
                                                  "note": "opaque; same bottom/top colours as the old gradient plus a centre glow, a faint hex lattice and a vignette"}
    return final


# ========================================================================================================= HALO
HALO = 512


def halo_fac(g):
    _, r = g.centred()
    soft = g.m("POWER", g.m("SUBTRACT", 1.0, r, clamp=True), 1.6)
    # A slightly brighter shoulder where the planet's limb sits (about 0.62 of the halo radius on the map).
    shoulder = g.m("MULTIPLY", g.m("EXPONENT", g.m("MULTIPLY", -1.0, g.m("POWER", g.m("DIVIDE", g.m("SUBTRACT", r, 0.64), 0.09), 2.0))), 0.18)
    return g.m("ADD", soft, shoulder, clamp=True)


def build_halo():
    A.reset()
    A._layers.clear()
    A.camera(HALO, HALO)
    A.plane(A.layer("h"), 0, 0, HALO, HALO, fac_material("Halo", halo_fac))
    img = A.render(HALO, HALO, samples=32)
    img[..., :3] = 1.0
    A.save_png(out("UI_MapHalo.png"), img)
    REPORT["files"]["UI_MapHalo.png"] = {"size": [HALO, HALO], "replaces": "PlanetPainter.PaintHalo (PlanetPainter.cs:13) used by WorldMapScreen.cs:230",
                                          "note": "white, tinted by WorldMapScreen.HaloColor as before"}
    return img


# ======================================================================================================== MAIN

def main():
    started = time.time()
    results = {}
    steps = [("icons", build_icons), ("kit", build_kit), ("dialogue", build_dialogue), ("radar", build_radar),
             ("starfield", build_starfield), ("backdrop", build_backdrop), ("halo", build_halo)]
    for key, fn in steps:
        if key in ONLY:
            t = time.time()
            results[key] = fn()
            print(f"[ui] {key} {time.time() - t:.0f}s", flush=True)
    if "mockups" in ONLY:
        import ui_mockups
        ui_mockups.run(PREVIEW, REPORT)
    if REPORT["files"]:
        path = out("UI_report.json")
        old = {}
        if os.path.exists(path):
            with open(path) as fh:
                old = json.load(fh).get("files", {})
        old.update(REPORT["files"])
        REPORT["files"] = old
        REPORT["fonts"] = "unchanged: KenneyFuture / KenneyFutureNarrow (CC0). Drones/MoneyPopup.cs:25-27 still uses the built-in " \
                          "LegacyRuntime font and should take UiKit.Font (UiSkin._font) instead; no art needed."
        with open(path, "w") as fh:
            json.dump(REPORT, fh, indent=1)
    print(f"[ui] done in {time.time() - started:.0f}s", flush=True)


if __name__ == "__main__":
    main()
