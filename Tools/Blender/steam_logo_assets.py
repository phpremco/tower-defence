"""Logo pieces for the Steam set (2026-11-10), rendered with the approved CrysiumField lockup (logo_v4 / logo_v3):
- the library logo without the series line (title only, alpha): for the small capsule, where FAR HARVEST would be
  a few pixels high;
- the emblem on its own with alpha (1024 square): for the vertical capsules and the icons.
The full lockup with the series line is v4's CrysiumField_library_logo.png, reused as is.

Run: blender -b --factory-startup --python Tools/Blender/steam_logo_assets.py -- [--samples 128]
Out: ArtSource/Previews/Steam/_masters/logo_noseries_alpha.png, emblem_alpha_1024.png
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import common as C  # noqa: E402
import logo_v4 as V4  # noqa: E402

V = V4.V
L2 = V.L2
OUT = os.path.join(C.REPO, "ArtSource", "Previews", "Steam", "_masters")


def main():
    os.makedirs(OUT, exist_ok=True)
    args = C.cli_args()
    samples = int(args.get("samples", 128))
    L2.enable_gpu()
    st = V.build("field_ns", want_env=False)
    tw, th = st["tw"], st["th"]
    L2.SUPER = 2
    L2.set_visible(st["cols"], True, False, False)
    cam = L2.camera("CamLib", (0, 0, th * 0.5), L2.fit_distance(tw, 0.86), rise=0.2, dof=False)
    L2.render_png(cam, 1280, 720, os.path.join(OUT, "logo_noseries_alpha.png"), samples, V.GRADE, transparent=True)
    L2.set_visible(st["cols"], False, True, False)
    dist = L2.fit_distance(max(st["emb_w"], st["emb_h"]), 0.86, lens=50)
    cam = L2.camera("CamEmb", (st["emb_x"], 0.05, st["emb_z"]), dist, lens=50, rise=0.1, dof=False)
    L2.SUPER = 1
    L2.render_png(cam, 1024, 1024, os.path.join(OUT, "emblem_alpha_1024.png"), samples, V.GRADE, transparent=True)


if __name__ == "__main__":
    main()
