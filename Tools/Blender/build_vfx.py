"""Round 20: the VFX texture set (projectiles, muzzle flashes, explosions, smoke, sparks, rings, decals).

Writes ArtSource/Exports/VFX/*.png (+ Low/ at half size) and VFX_report.json, and contact sheets into --preview.
Volume flipbooks (fireballs, smoke, dust) come from Mantaflow sims (vfx_sim.py) rendered into Unity six-way
lightmaps; everything else is procedural numpy (vfx_2d.py). No downloaded textures.

Run: blender --background --factory-startup --python Tools/Blender/build_vfx.py -- [--preview <dir>]
     [--cache <dir>] [--only Name,Name] [--no-volumes] [--norender]
--only takes texture names (Muzzle_Flashes builds Muzzle_Side and Muzzle_Front). --cache keeps the rendered 2D maps of each sim (default %TEMP%/td_vfx_cache), so a rerun only re-packs; delete it
(or a single <Name>.npz) after changing a sim spec.
"""

import json
import os
import sys
import tempfile
import time

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import numpy as np  # noqa: E402

import vfx_2d as D  # noqa: E402
import vfx_kit as K  # noqa: E402


def _args():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    opts = {"preview": os.path.join(K.REPO, "ArtSource", "Previews", "VFX"),
            "cache": os.path.join(tempfile.gettempdir(), "td_vfx_cache"), "only": None, "volumes": True,
            "render": True}
    i = 0
    while i < len(argv):
        a = argv[i]
        if a in ("--preview", "--cache", "--only"):
            opts[a[2:]] = argv[i + 1]
            i += 2
            continue
        if a == "--no-volumes":
            opts["volumes"] = False
        if a == "--norender":
            opts["render"] = False
        i += 1
    if opts["only"]:
        opts["only"] = set(opts["only"].split(","))
    return opts


# ------------------------------------------------------------------------------------------------ volume specs

def _keep(sim_frames, count, ease=1.3, first=2):
    """`count` sim frames, denser at the start where an explosion changes fastest."""
    span = sim_frames - first
    out = []
    for i in range(count):
        f = int(round(first + span * (i / (count - 1)) ** ease))
        out.append(max(f, out[-1] + 1 if out else f))
    return out


def _satellites(seed, count, ring, z, radius, on, vn, fuel=3.0, temp=3.0, delay=0):
    rng = np.random.default_rng(seed)
    out = []
    for _ in range(count):
        a = rng.uniform(0, 2 * np.pi)
        r = rng.uniform(*ring)
        start = delay + int(rng.integers(0, 2))
        out.append(dict(loc=(float(np.cos(a) * r), float(np.sin(a) * r * 0.7), float(rng.uniform(*z))),
                        radius=float(rng.uniform(*radius)), vn=vn, vr=2.0, temperature=temp, fuel=fuel,
                        on=(max(1, start), start + int(rng.integers(*on))),
                        scale_keys=[(max(1, start), 0.3), (start + int(rng.integers(2, 5)), 1.0)]))
    return out


FIRE = dict(vorticity=0.45, alpha=0.3, beta=1.4, burning_rate=0.4, flame_smoke=2.0, flame_vorticity=0.3,
            flame_ignition=1.25, flame_max_temp=1.75, use_dissolve_smoke=True, dissolve_speed=60,
            use_dissolve_smoke_log=True)
SMOKE = dict(vorticity=0.35, alpha=0.35, beta=0.2, use_dissolve_smoke=True, dissolve_speed=80,
             use_dissolve_smoke_log=True)


def volume_specs():
    large_keep = _keep(88, 64)
    small_keep = _keep(56, 64, ease=1.2)
    boss_keep = _keep(104, 64)
    smoke_keep = _keep(90, 64, ease=1.1)
    return [
        dict(name="Explosion_Small", domain=(2.6, 2.6, 3.4), res=112, seed=11, sim_frames=max(small_keep),
             keep=small_keep, cell=128, grid=(8, 8), fps=60, ground=0.0,
             domain_settings=dict(FIRE, time_scale=0.7, dissolve_speed=40, flame_smoke=1.4),
             emitters=[dict(loc=(0, 0, 0.55), radius=0.38, vn=6, vr=2.0, temperature=3, fuel=2.5, on=(1, 4),
                            scale_keys=[(1, 0.25), (3, 0.8), (5, 1.1)])] +
             _satellites(12, 3, (0.2, 0.35), (0.45, 0.7), (0.16, 0.24), (2, 4), 5, fuel=2.0),
             sigma=16.0, emit_gain=1.8, emit_power=2.0, flame_clear=0.6, heat_glow=1.0,
             use="Cannon shell, Pulse hits on armour, mines, small gadgets"),
        dict(name="Explosion_Large", domain=(3.6, 3.6, 5.4), res=176, seed=7, sim_frames=max(large_keep),
             keep=large_keep, cell=256, grid=(8, 8), fps=48, ground=0.0, domain_settings=dict(FIRE),
             emitters=[dict(loc=(0, 0, 0.8), radius=0.55, vn=7, vr=2.0, temperature=3, fuel=3.0, on=(1, 7),
                            scale_keys=[(1, 0.25), (4, 0.8), (7, 1.15)])] +
             _satellites(3, 5, (0.35, 0.6), (0.7, 1.1), (0.25, 0.38), (4, 7), 6),
             sigma=16.0, emit_gain=1.5, emit_power=2.0, flame_clear=0.6, heat_glow=1.0,
             use="Artillery (Mortar), Rocket, Barrel, Lava Ball, tower destruction"),
        dict(name="Explosion_Boss", domain=(5.0, 5.0, 7.0), res=192, seed=21, sim_frames=max(boss_keep),
             keep=boss_keep, cell=256, grid=(8, 8), fps=40, ground=0.0,
             domain_settings=dict(FIRE, dissolve_speed=75, vorticity=0.5),
             emitters=[dict(loc=(0, 0, 0.9), radius=0.7, vn=8, vr=2.5, temperature=3, fuel=3.5, on=(1, 8),
                            scale_keys=[(1, 0.25), (4, 0.8), (8, 1.2)])] +
             _satellites(22, 5, (0.5, 0.9), (0.7, 1.3), (0.3, 0.45), (4, 7), 7) +
             _satellites(23, 4, (0.6, 1.1), (0.8, 1.6), (0.3, 0.42), (3, 6), 6, delay=7) +
             _satellites(24, 3, (0.3, 0.8), (1.2, 1.9), (0.28, 0.4), (3, 5), 5, delay=13),
             sigma=14.0, emit_gain=1.4, emit_power=2.0, flame_clear=0.6, heat_glow=1.2,
             use="Boss death, base destruction, superweapons (chained sub-bursts)"),
        dict(name="Smoke_Puff", domain=(2.6, 2.6, 3.0), res=112, seed=31, sim_frames=max(smoke_keep),
             keep=smoke_keep, cell=128, grid=(8, 8), fps=30, ground=0.0, flow_type="SMOKE",
             domain_settings=dict(SMOKE),
             emitters=[dict(loc=(0, 0, 0.9), radius=0.42, vn=2.5, vr=1.5, temperature=0.4, density=1.0, on=(1, 5),
                            scale_keys=[(1, 0.4), (5, 1.0)])] +
             [dict(loc=(0.25, 0.1, 1.15), radius=0.25, vn=2.0, vr=1.5, temperature=0.4, on=(2, 5)),
              dict(loc=(-0.28, -0.1, 0.8), radius=0.22, vn=2.0, vr=1.5, temperature=0.4, on=(1, 4))],
             sigma=18.0, emit_gain=1.0, use="after every explosion, wrecks, damaged towers (dark, billowing)"),
        dict(name="Smoke_Linger", domain=(2.6, 2.6, 3.4), res=112, seed=41, sim_frames=max(smoke_keep),
             keep=smoke_keep, cell=128, grid=(8, 8), fps=24, ground=0.0, flow_type="SMOKE",
             domain_settings=dict(SMOKE, alpha=0.6, dissolve_speed=55, vorticity=0.45),
             emitters=[dict(loc=(0, 0, 0.5), radius=0.3, vn=1.0, vr=1.0, temperature=0.6, density=0.8,
                            on=(1, 26), vel=(0.15, 0.0, 0.6))],
             sigma=14.0, emit_gain=1.0, use="lingering column over craters, burning wrecks, damaged base"),
        dict(name="Dust_Burst", domain=(3.4, 3.4, 2.2), res=112, seed=51, sim_frames=max(smoke_keep),
             keep=smoke_keep, cell=128, grid=(8, 8), fps=40, ground=0.0, flow_type="SMOKE",
             domain_settings=dict(SMOKE, alpha=-0.25, beta=0.0, dissolve_speed=50, vorticity=0.5),
             emitters=[dict(loc=(0, 0, 0.25), radius=0.4, vn=7, vr=2.5, temperature=0.0, density=1.0, on=(1, 3),
                            scale_keys=[(1, 0.5), (3, 1.0)], lump=0.4)],
             sigma=16.0, emit_gain=1.0, use="ground hits (mortar landing, heavy footfalls, crawler emerge)"),
    ]


def _crop(frames):
    """Square crop around the union of all frames (alpha > 2%), centred horizontally on the domain."""
    alpha = np.max(np.stack([f["alpha"] for f in frames]), axis=0)
    h, w = alpha.shape
    ys, xs = np.nonzero(alpha > 0.02)
    half_w = max(w / 2 - xs.min(), xs.max() + 1 - w / 2)
    top, bottom = ys.min(), ys.max() + 1
    side = int(np.ceil(max(2 * half_w, bottom - top) * 1.06))
    cx = w / 2
    cy = (top + bottom) / 2
    y0 = int(round(cy - side / 2))
    x0 = int(round(cx - side / 2))
    return y0, x0, side


def _cut(img, y0, x0, side):
    h, w = img.shape
    out = np.zeros((side, side), np.float32)
    ys, xs = max(y0, 0), max(x0, 0)
    ye, xe = min(y0 + side, h), min(x0 + side, w)
    out[ys - y0:ye - y0, xs - x0:xe - x0] = img[ys:ye, xs:xe]
    return out


def build_volume(spec, opts, report, previews):
    import vfx_sim as S  # needs bpy
    frames = S.simulate(spec, opts["cache"])
    y0, x0, side = _crop(frames)
    cell = spec["cell"]
    n = len(frames)
    pos_frames, neg_frames = [], []
    for i, m in enumerate(frames):
        # Fade the tail so the last frame is empty (no pop when the particle dies).
        fade = 1.0 - K.smoothstep(0.82, 1.0, i / (n - 1))
        cut = {k: K.resize(_cut(v, y0, x0, side), cell, cell) for k, v in m.items()}
        cut["alpha"] = np.clip(cut["alpha"], 0, 1) * fade
        cut["emissive"] = np.clip(cut["emissive"], 0, 1) * fade
        for k in ("right", "left", "top", "bottom", "back", "front"):
            cut[k] = np.clip(cut[k], 0, 1)
        pos, neg = K.six_way(cut)
        pos_frames.append(pos)
        neg_frames.append(neg)
    rows, cols = spec["grid"]
    h_img = frames[0]["alpha"].shape[0]
    ground_v = (y0 + side - h_img) / side  # ground line (z = 0) in V from the bottom of a cell
    meta = dict(kind="flipbook six-way", rows=rows, cols=cols, frames=rows * cols, fps=spec["fps"], loop=False,
                cell=[cell, cell], lifetime=round(rows * cols / spec["fps"], 2),
                pivot=[0.5, round(max(ground_v, 0.0), 3)], use=spec["use"],
                world_size=round(side / h_img * spec["domain"][2], 2),
                sim=dict(res=spec["res"], domain=spec["domain"], sim_frames=spec["sim_frames"]))
    pos_meta = dict(meta, channels="R light from right (+X), G light from top (+Y), B light from back (behind "
                    "the smoke, away from the camera), A alpha (straight)", pair=spec["name"] + "_SixWayNeg.png")
    neg_meta = dict(meta, channels="R light from left (-X), G light from bottom (-Y), B light from front "
                    "(camera side), A emissive temperature 0..1 (map through a fire gradient)",
                    pair=spec["name"] + "_SixWayPos.png")
    blend = ("Alpha blend (straight alpha in Pos.A); lighting = sum of the six maps weighted by the squared light "
             "direction in billboard space; emission = gradient(Neg.A) * intensity added on top")
    pos_meta["blend"] = neg_meta["blend"] = blend
    pos_meta["tint"] = neg_meta["tint"] = ("smoke albedo dark grey-brown (0.15-0.25); dust: tan (0.45, 0.38, 0.3); "
                                           "fire gradient on Neg.A")
    K.save_set(spec["name"] + "_SixWayPos", K.atlas(pos_frames, rows, cols), report, pos_meta)
    K.save_set(spec["name"] + "_SixWayNeg", K.atlas(neg_frames, rows, cols), report, neg_meta)
    previews[spec["name"]] = (pos_frames, neg_frames, spec)


# --------------------------------------------------------------------------------------------- 2D textures

TEXTURES_2D = [
    # name, builder, preview mode, preview tint
    ("Bolt_Core", D.bolt_core, "cg", (0.3, 0.7, 1.0)),
    ("Trail_Ribbon", D.trail_ribbon, "cg", (0.3, 0.7, 1.0)),
    ("Trail_Noise", D.trail_noise, "mask", None),
    ("Plasma_Bolt", D.plasma_bolt, "cg", (0.55, 0.35, 1.0)),
    ("Rocket_Exhaust", D.rocket_exhaust, "cg", (1.0, 0.5, 0.15)),
    ("Smoke_Trail", D.smoke_trail, "smoke", (0.35, 0.34, 0.33)),
    ("Beam_Core", D.beam_core, "cg", (0.35, 0.9, 1.0)),
    ("Beam_Noise", D.beam_noise, "mask", None),
    ("Tesla_Arcs", D.tesla_arcs, "cg", (0.7, 0.5, 1.0)),
    ("Spark_Burst", D.spark_burst, "cg", (1.0, 0.55, 0.18)),
    ("Spark_Streaks", D.spark_streaks, "cg", (1.0, 0.55, 0.18)),
    ("Debris_Shards", D.debris_shards, "shards", (1.0, 0.45, 0.1)),
    ("Shockwave_Ring", D.shockwave_ring, "cg", (1.0, 0.7, 0.35)),
    ("Shockwave_Distort", D.shockwave_distort, "distort", None),
    ("HeatHaze_Distort", D.heat_haze, "distort", None),
    ("Scorch_Decal", D.scorch_decals, "scorch", (1.0, 0.35, 0.08)),
    ("Muzzle_Flashes", D.muzzle_flashes, "cg", (1.0, 0.65, 0.25)),
    ("Energy_Impact", D.energy_impact, "cg", (0.3, 0.75, 1.0)),
    ("Cryo_Burst", D.cryo_burst, "cryo", (0.55, 0.95, 1.0)),
    ("Acid_Splash", D.acid_splash, "acid", (0.55, 1.0, 0.15)),
]


def preview_2d(img, mode, tint):
    h, w = img.shape[:2]
    bg = K.backdrop(h, w)
    if mode == "cg":
        out = K.shade_additive(img[..., 0], img[..., 1], tint, bg=bg)
        if img.shape[2] > 2:
            out = np.clip(out + img[..., 2:3] * 0.18, 0, 1)
        return out
    if mode == "mask":
        return img[..., :3]
    if mode == "smoke":
        col = img[..., 0:1] * np.array(tint) * 1.6
        return K.composite(col, img[..., 3:4], 0.0, bg)
    if mode == "shards":
        col = img[..., 2:3] * np.array([0.25, 0.23, 0.22])
        em = img[..., 0:1] * np.array(tint) * 3.0
        return K.composite(col, img[..., 1:2], em, bg)
    if mode == "scorch":
        ground = np.array([0.16, 0.14, 0.11]) + 0.0 * bg
        col = img[..., 1:2] * np.array([0.5, 0.45, 0.4])
        a = img[..., 0:1]
        em = img[..., 2:3] * (img[..., 3:4] > 0.45) * np.array(tint) * 3.0
        return K.composite(col, a, em, ground)
    if mode == "cryo":
        mist = img[..., 2:3]
        base = bg * (1 - mist * 0.8) + mist * 0.8 * np.array([0.75, 0.9, 1.0])
        em = img[..., 1:2] * np.array(tint) * 1.2 + img[..., 0:1] * 1.6
        return K.tonemap(base + em)
    if mode == "acid":
        a = img[..., 3:4]
        col = img[..., 0:1] * (np.array([0.08, 0.2, 0.02]) + img[..., 1:2] * np.array([0.6, 1.0, 0.15])) \
            + img[..., 2:3] * 0.6
        em = img[..., 1:2] * a * np.array(tint) * 0.35
        return K.tonemap(bg * (1 - a) + col + em)
    if mode == "distort":
        # Bend a grid pattern by the encoded offset, the way the refraction pass will bend the scene.
        y, x = np.mgrid[0:h, 0:w].astype(np.float64)
        ox = (img[..., 0] * 2 - 1) * img[..., 2]
        oy = -(img[..., 1] * 2 - 1) * img[..., 2]
        sx, sy = x + ox * w * 0.04, y + oy * h * 0.04
        pat = ((np.floor(sx / (w / 16)) + np.floor(sy / (h / 16))) % 2) * 0.25 + 0.15
        return np.repeat(pat[..., None], 3, -1)
    return img[..., :3]


def build_2d(opts, report, previews):
    for name, fn, mode, tint in TEXTURES_2D:
        if opts["only"] and name not in opts["only"]:
            continue
        started = time.time()
        res = fn()
        if name == "Muzzle_Flashes":
            (side, side_meta), (front, front_meta) = res
            items = [("Muzzle_Side", side, side_meta), ("Muzzle_Front", front, front_meta)]
        else:
            items = [(name, res[0], res[1])]
        for out_name, img, meta in items:
            meta = dict(meta, preview_tint=tint)
            K.save_set(out_name, img, report, meta)
            previews[out_name] = (img, mode, tint, meta)
        print(f"[vfx] {name} built in {time.time() - started:.1f}s", flush=True)


# ------------------------------------------------------------------------------------------------ previews

def _frames_of(img, meta):
    if meta.get("kind", "").startswith(("flipbook", "sprite atlas", "decal atlas")) and "rows" in meta:
        rows, cols = meta["rows"], meta.get("cols", 1)
        ch, cw = img.shape[0] // rows, img.shape[1] // cols
        return [img[r * ch:(r + 1) * ch, q * cw:(q + 1) * cw] for r in range(rows) for q in range(cols)]
    return [img]


def _fit(tile, width):
    h = max(1, int(round(tile.shape[0] * width / tile.shape[1])))
    return K.resize(tile, h, width)


def _overview(previews, folder):
    """One representative frame per texture on one sheet (the quick look at the whole set)."""
    tiles = []
    for name, item in previews.items():
        if len(item) == 3:
            pos_frames, neg_frames, _ = item
            i = len(pos_frames) // 6
            tile = K.shade_six_way(pos_frames[i], neg_frames[i])
        else:
            img, mode, tint, meta = item
            frames = _frames_of(img, meta)
            tile = preview_2d(frames[len(frames) // 4], mode, tint)
        h, w = tile.shape[:2]
        canvas = np.zeros((192, 192, 3))
        tile = K.resize(tile, max(1, int(192 * min(1, h / w))), 192) if w >= h else K.resize(tile, 192, int(192 * w / h))
        canvas[:tile.shape[0], :tile.shape[1]] = tile[..., :3]
        tiles.append(canvas)
    if tiles:
        K.save_png(os.path.join(folder, "vfx_overview.png"), K.sheet(tiles, 8))


def write_previews(previews, folder):
    os.makedirs(folder, exist_ok=True)
    _overview(previews, folder)
    for name, item in previews.items():
        if len(item) == 3:  # volume
            pos_frames, neg_frames, spec = item
            n = len(pos_frames)
            picks = list(range(0, n, max(1, n // 16)))[:16]
            tiles = [K.shade_six_way(pos_frames[i], neg_frames[i]) for i in picks]
            tiles = [_fit(t, 192) for t in tiles]
            K.save_png(os.path.join(folder, f"vfx_{name}.png"), K.sheet(tiles, 8))
            # Light from four sides on one mid frame: proves the six-way maps relight the smoke.
            mid = int(n * 0.55)
            lights = [(-0.7, 0.5, -0.4), (0.7, 0.5, -0.4), (0.0, 1.0, 0.2), (0.0, -0.4, -0.9)]
            tiles = [_fit(K.shade_six_way(pos_frames[mid], neg_frames[mid], light=l), 256) for l in lights]
            K.save_png(os.path.join(folder, f"vfx_{name}_relight.png"), K.sheet(tiles, 4))
            channels = [pos_frames[mid][..., c] for c in range(4)] + [neg_frames[mid][..., c] for c in range(4)]
            tiles = [_fit(np.repeat(c[..., None], 3, -1), 160) for c in channels]
            K.save_png(os.path.join(folder, f"vfx_{name}_channels.png"), K.sheet(tiles, 8))
        else:
            img, mode, tint, meta = item
            frames = _frames_of(img, meta)
            if len(frames) > 16:
                step = len(frames) / 16
                frames = [frames[int(i * step)] for i in range(16)]
            width = 192 if frames[0].shape[1] <= frames[0].shape[0] else 384
            cols = 8 if width == 192 else 4
            if len(frames) == 1:
                width, cols = min(768, max(384, frames[0].shape[1])), 1
            tiles = [_fit(preview_2d(f, mode, tint), width) for f in frames]
            K.save_png(os.path.join(folder, f"vfx_{name}.png"), K.sheet(tiles, cols))


def main():
    opts = _args()
    os.makedirs(K.OUT, exist_ok=True)
    report_path = os.path.join(K.OUT, "VFX_report.json")
    report = {}
    if os.path.exists(report_path):
        with open(report_path) as fh:
            report = json.load(fh).get("textures", {})
    previews = {}
    build_2d(opts, report, previews)
    if opts["volumes"]:
        for spec in volume_specs():
            if opts["only"] and spec["name"] not in opts["only"]:
                continue
            build_volume(spec, opts, report, previews)
    total = sum(v["bytes"] + v["low_bytes"] for v in report.values())
    doc = {"round": 20, "folder": "ArtSource/Exports/VFX", "low_folder": "ArtSource/Exports/VFX/Low (half size)",
           "colour_space": "all textures are linear data: import with sRGB off; colour comes from tints/gradients",
           "frame_order": "flipbooks start top-left and run row by row (Unity Texture Sheet Animation order)",
           "total_bytes": total, "textures": dict(sorted(report.items()))}
    with open(report_path, "w") as fh:
        json.dump(doc, fh, indent=1)
    print(f"[vfx] total {total / 1e6:.1f} MB in {len(report)} textures", flush=True)
    if opts["render"]:
        write_previews(previews, opts["preview"])


if __name__ == "__main__":
    main()
