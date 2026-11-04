"""Round 24: liquid and mist texture set (water, lava, bog, quicksand, frozen lake, corruption pool, ground mist,
steam) for a URP liquid surface shader, plus Blender preview renders of each liquid on a shore with the real bank
pieces and the game camera, animated strips and mist over a field.

Everything is procedural (numpy: FFT ocean, photon caustics, periodic Worley and spectral noise); no downloads.
Outputs ArtSource/Exports/Liquids/<Kind>/<Name>.png, <Kind>/Low/ (half size) and Liquids_report.json.

Run:  blender --background --factory-startup --python Tools/Blender/build_liquids.py --
        [--only Water,Lava] [--preview <dir>] [--norender] [--previewonly] [--samples 32] [--compare-dir <unity screenshot dir>]
The texture half also runs without Blender:
  "C:\\Program Files\\Blender Foundation\\Blender 5.2\\5.2\\python\\bin\\python.exe" Tools/Blender/build_liquids.py
  -- --norender
Modules: liquid_kit.py (noise, ocean, caustics, PNG), liquid_textures.py (one builder per kind),
liquid_preview.py (bpy shore scenes and shaders that imitate the intended Unity shader).
"""

import json
import os
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import numpy as np  # noqa: E402

import liquid_kit as K  # noqa: E402
import liquid_textures as T  # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
OUT = os.path.join(ROOT, "ArtSource", "Exports", "Liquids")
ARGV = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []


def arg(name, default=None):
    if name in ARGV:
        i = ARGV.index(name)
        return ARGV[i + 1] if i + 1 < len(ARGV) and not ARGV[i + 1].startswith("--") else True
    return default


def log(*parts):
    print("[td] liquids:", *parts, flush=True)


# Suggested tints per continent (linear RGB). Textures are neutral; the shader multiplies these in.
PALETTES = {
    "Water": {
        "Crystal Plains": {"shallow": [0.035, 0.13, 0.19], "deep": [0.006, 0.022, 0.055], "foam": [0.80, 0.88, 0.95],
                           "caustic": [0.55, 0.85, 1.0], "glow": [0.25, 0.55, 1.0]},
        "Mirewood": {"shallow": [0.10, 0.28, 0.22], "deep": [0.010, 0.040, 0.035], "foam": [0.70, 0.80, 0.70],
                     "caustic": [0.45, 0.95, 0.75], "glow": [0.2, 0.9, 0.6]},
        "Northern Crown": {"shallow": [0.14, 0.34, 0.45], "deep": [0.012, 0.040, 0.085], "foam": [0.90, 0.95, 1.0],
                           "caustic": [0.7, 0.9, 1.0], "glow": [0.4, 0.7, 1.0]},
        "Ashen Reach": {"shallow": [0.16, 0.24, 0.26], "deep": [0.015, 0.025, 0.035], "foam": [0.75, 0.72, 0.68],
                        "caustic": [1.0, 0.75, 0.5], "glow": [1.0, 0.5, 0.2]},
    },
    "Lava": {
        "Ashen Reach": {"crust": [1.0, 1.0, 1.0], "ramp": [[0.0, [0.0, 0.0, 0.0]], [0.3, [0.4, 0.03, 0.005]],
                                                           [0.55, [1.6, 0.35, 0.05]], [0.85, [3.2, 1.3, 0.25]],
                                                           [1.0, [4.0, 2.8, 1.4]]]},
        "Northern Crown (amber)": {"crust": [0.85, 0.9, 1.0], "ramp": [[0.0, [0, 0, 0]], [0.3, [0.5, 0.12, 0.01]],
                                                                       [0.7, [2.2, 0.9, 0.12]], [1.0, [3.5, 2.5, 1.2]]]},
    },
    "Bog": {"Mirewood": {"shallow": [0.04, 0.06, 0.028], "deep": [0.010, 0.016, 0.010], "scum": [0.13, 0.20, 0.06],
                         "glow": [0.35, 1.2, 0.6], "bubble": [0.5, 0.6, 0.4]}},
    "Quicksand": {"Crystal Plains": {"sand": [0.50, 0.40, 0.30], "wet": [0.6, 0.6, 0.65], "sparkle": [1.0, 0.5, 0.9]},
                  "Ashen Reach": {"sand": [0.55, 0.38, 0.24], "wet": [0.5, 0.45, 0.4], "sparkle": [1.0, 0.7, 0.4]}},
    "Ice": {"Northern Crown": {"ice": [0.80, 0.90, 1.0], "deep": [0.02, 0.07, 0.14], "crack_glow": [0.4, 0.8, 1.0],
                               "frost": [0.9, 0.95, 1.0]},
            "Southern Crown": {"ice": [0.85, 0.85, 0.95], "deep": [0.04, 0.03, 0.10], "crack_glow": [0.8, 0.45, 1.0],
                               "frost": [0.92, 0.9, 1.0]}},
    "Corruption": {"The Core": {"base": [0.035, 0.008, 0.055], "deep": [0.012, 0.0, 0.025], "vein": [1.4, 0.25, 2.4],
                                "sheen": [0.55, 0.3, 0.9]}},
    "Mist": {"Crystal Plains": [0.55, 0.60, 0.75], "Ashen Reach": [0.45, 0.40, 0.38], "Northern Crown": [0.75, 0.82, 0.9],
             "Mirewood": [0.40, 0.55, 0.48], "Southern Crown": [0.70, 0.70, 0.82], "The Core": [0.42, 0.30, 0.55]},
}


def write_kind(kind, texes, report):
    folder = os.path.join(OUT, kind)
    low = os.path.join(folder, "Low")
    entries = []
    for t in texes:
        path = K.save_png(os.path.join(folder, t.name + ".png"), t.img)
        is_normal = t.meta.get("kind") == "normal"
        small = K.resize_normal(t.img) if is_normal else K.resize_box(t.img)
        K.save_png(os.path.join(low, t.name + ".png"), small)
        img = t.img if t.img.ndim == 3 else t.img[..., None]
        entry = {"file": f"{kind}/{t.name}.png", "size": [int(img.shape[1]), int(img.shape[0])],
                 "channels": int(img.shape[2]), "srgb": t.meta.get("kind") == "albedo",
                 "type": t.meta.get("kind"), "wrap": t.meta.get("wrap"), "note": t.meta.get("note"),
                 "bytes": os.path.getsize(path)}
        if "flipbook" in t.meta:
            entry["flipbook"] = {"cols": t.meta["flipbook"][0], "rows": t.meta["flipbook"][1],
                                 "frames": T.FRAMES, "fps": t.meta["fps"], "order": "top-left, row by row",
                                 "loops": True}
        elif "Repeat" == t.meta.get("wrap"):
            entry["seam_ratio"] = round(K.seam_error(img[..., 0]), 3)
        if is_normal:
            entry["mean_z"] = round(K.normal_flat_rate(t.img), 4)
        entries.append(entry)
        log(kind, t.name, entry["size"], f"{entry['bytes'] / 1e6:.2f} MB")
    report["kinds"][kind] = {"textures": entries, "palettes": PALETTES.get(kind)}


def main():
    t0 = time.time()
    only = arg("--only")
    kinds = [k for k in T.KINDS if not only or k in only.split(",")]
    report_path = os.path.join(OUT, "Liquids_report.json")
    report = {"round": 24, "generator": "Tools/Blender/build_liquids.py", "kinds": {}}
    if os.path.exists(report_path):
        with open(report_path, encoding="utf-8") as f:
            old = json.load(f)
        report["kinds"] = old.get("kinds", {})
    for kind in ([] if arg("--previewonly") else kinds):
        started = time.time()
        write_kind(kind, T.KINDS[kind](), report)
        log(kind, f"done in {time.time() - started:.0f}s")
    import liquid_shader_notes as notes
    report["shader"] = notes.SHADER
    report["conventions"] = notes.CONVENTIONS
    total = sum(e["bytes"] for k in report["kinds"].values() for e in k["textures"])
    report["total_mb_full"] = round(total / 1e6, 1)
    with open(report_path, "w", encoding="utf-8") as f:
        json.dump(report, f, indent=2)
    log("report", report_path, f"{total / 1e6:.1f} MB full size", f"{time.time() - t0:.0f}s")

    if not arg("--norender"):
        try:
            import bpy  # noqa: F401
        except ImportError:
            log("no bpy: skipping previews")
            return
        import liquid_preview
        liquid_preview.run(kinds, OUT, arg("--preview"), int(arg("--samples", 32)), arg("--compare-dir"))


if __name__ == "__main__":
    main()
