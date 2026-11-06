"""Round 38: the comic frame of the boss-intro cut-in (Docs/BOSS_INTROS.md, round 13b).

A hand-inked panel border in our faction's cool tech colours: a thick black ink outline whose edges wobble and spike a
little like a brush line, a cyan bevelled inlay just inside it, a slanted corner tab at the top left with three speed
stripes, and small tech ticks along the bottom. The window is transparent: the cut-in's picture shows through it. The
border is even on all four sides, so Unity can 9-slice it (`UI_ComicFrame.json` gives the slice border in pixels) for
the cut-in panel and for the compact answer box beside it.

Outputs ArtSource/Exports/UI/UI_ComicFrame.png (512 x 512, straight alpha) and UI_ComicFrame.json.

    blender --background --factory-startup --python Tools/Blender/build_ui_comic.py -- [--preview DIR]
"""

import json
import math
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import ui_art as A  # noqa: E402

SIZE = 512
# The ink band: outer edge inset, band thickness; the slice border covers band, inlay and tab.
INSET = 14
BAND = 30
SLICE = 96
INK = A.hex_to_rgb("#04070D")
CYAN = A.hex_to_rgb("#38D6FF")
STEEL = A.hex_to_rgb("#9FB8CC")
NAVY = A.hex_to_rgb("#0D1B2E")
WHITE = (0.92, 0.97, 1.0)


def jagged_rect(x0, y0, x1, y1, amp, step, seed, spikes=0.0):
    """A rectangle outline walked in small steps, each point pushed in or out a little (a brush line, not a ruler);
    now and then a spike pokes out, like ink flicked off the brush."""
    rng = np.random.default_rng(seed)
    pts = []
    corners = [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]
    for i in range(4):
        ax, ay = corners[i]
        bx, by = corners[(i + 1) % 4]
        length = math.hypot(bx - ax, by - ay)
        n = max(2, int(length / step))
        nx, ny = (by - ay) / length, -(bx - ax) / length
        for k in range(n):
            t = k / n
            wobble = amp * (math.sin(t * 9.0 + seed + i) * 0.5 + rng.uniform(-0.5, 0.5))
            if spikes and rng.random() < spikes and 0.08 < t < 0.92:
                wobble += amp * rng.uniform(2.0, 3.5)
            pts.append((ax + (bx - ax) * t + nx * wobble, ay + (by - ay) * t + ny * wobble))
    return pts


def build(out):
    A.reset()
    A._layers.clear()
    A.camera(SIZE, SIZE)
    L = {k: A.layer(k) for k in ("ink", "inlay", "tab")}
    s0, s1 = INSET, SIZE - INSET
    w0, w1 = INSET + BAND, SIZE - INSET - BAND
    # Ink band: outer edge rough with flicks outward, inner edge rough but tighter.
    outer = jagged_rect(s0, s0, s1, s1, 2.4, 6.0, 3, spikes=0.05)
    inner = list(reversed(jagged_rect(w0, w0, w1, w1, 1.6, 7.0, 11)))
    A.px_poly(L["ink"], outer, [inner], colour=INK, z=0.0)
    # The cyan inlay, bevelled, sitting in the band just inside the ink's inner edge.
    i0, i1 = w0 - 9, w1 + 9
    A.px_poly(L["inlay"], A.chamfer_rect(i0, i0, i1, i1, 10, 4, 10, 4),
              [list(reversed(A.chamfer_rect(i0 + 5, i0 + 5, i1 - 5, i1 - 5, 8, 3, 8, 3)))],
              colour=CYAN, z=1.0, bevel=1.5, shaded=True)
    # Corner tab, top left (y up: the top is near SIZE): a slanted plate with three speed stripes.
    top = SIZE - INSET
    tab = [(s0 - 2, top + 2), (s0 + 150, top + 2), (s0 + 122, top - 46), (s0 - 2, top - 46)]
    A.px_poly(L["tab"], tab, colour=INK, z=2.0)
    A.px_poly(L["tab"], [(s0 + 6, top - 6), (s0 + 132, top - 6), (s0 + 112, top - 38), (s0 + 6, top - 38)],
              colour=NAVY, z=3.0, bevel=1.5, shaded=True)
    for k in range(3):
        x = s0 + 22 + k * 24
        A.px_poly(L["tab"], [(x - 12, top - 32), (x + 2, top - 32), (x + 14, top - 12), (x, top - 12)],
                  colour=CYAN if k == 0 else STEEL, z=6.0)
    # Tech ticks along the bottom band and a small bracket at the bottom right corner.
    for k in range(9):
        x = SIZE * 0.5 - 96 + k * 24
        A.px_poly(L["inlay"], [(x, s0 + 9), (x + 10, s0 + 9), (x + 10, s0 + 15), (x, s0 + 15)], colour=STEEL, z=2.0)
    A.px_poly(L["inlay"], [(s1 - 70, s0 + 8), (s1 - 8, s0 + 8), (s1 - 8, s0 + 40), (s1 - 14, s0 + 40), (s1 - 14, s0 + 14),
                           (s1 - 70, s0 + 14)], colour=WHITE, z=2.0, bevel=1.0, shaded=True)
    img = A.render(SIZE, SIZE, samples=32)
    # A faint cyan glow round the inlay so it reads as lit tech against the black ink.
    inlay = A.render(SIZE, SIZE, only=("inlay",), samples=16)
    glow = A.blur(inlay[..., 3], 6.0) * 0.45
    frame = A.over(A.solid(glow, CYAN), A.premul(img))
    frame = A.unpremul(frame)
    os.makedirs(out, exist_ok=True)
    A.save_png(os.path.join(out, "UI_ComicFrame.png"), frame)
    with open(os.path.join(out, "UI_ComicFrame.json"), "w") as f:
        # The sheet format of UI_Kit.json (BlenderUiBuilder.Cut): one sprite, 9-sliced by the band.
        json.dump({"round": 38, "use": "BossCutIn frame and the boss-intro answer box (9-sliced)", "window": [w0, w1],
                   "sprites": {"ComicFrame": {"unity_rect": [0, 0, SIZE, SIZE], "border_lbrt": [SLICE, SLICE, SLICE, SLICE],
                                              "pivot": [0.5, 0.5]}}}, f, indent=2)
    return frame


def main():
    out = A.OUT
    frame = build(out)
    preview = None
    if "--preview" in sys.argv:
        preview = sys.argv[sys.argv.index("--preview") + 1]
    if preview:
        os.makedirs(preview, exist_ok=True)
        back = np.zeros_like(frame)
        back[..., :3] = (0.2, 0.25, 0.3)
        back[..., 3] = 1.0
        shown = A.unpremul(A.over(A.premul(back), A.premul(frame)))
        A.save_png(os.path.join(preview, "ui_comic_frame.png"), shown)
    print(f"[td] comic frame -> {out}", flush=True)


main()
