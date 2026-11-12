"""Round 56: the Fire Larva's real foot travel per Crawl loop (it slides in the game: the report's
crawl_distance_per_loop 0.20 asks Unity for a walk rate of 6.9 at base speed, over the clamp of 6).

Opens ArtSource/Blend/FireLarva.blend (or --blend <path>) and, for every frame of Crawl, takes each foot (the tail of
Leg_<side><n>_Lower) in model units. A foot is planted while it is within `--contact` of its lowest height; the
distance per loop that plants the feet is the mean backward slide of the planted feet per frame times the loop's
frames (the creature walks towards -Y, so a planted foot slides towards +Y under the body).

Writes ArtSource/Previews/QA/r56_firelarva_gait_<tag>.json.
Run: blender -b --factory-startup --python Tools/Blender/r56_larva_gait.py -- --tag before
"""

import json
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402

import common as C  # noqa: E402

QA = os.path.join(C.REPO, "ArtSource", "Previews", "QA")


def measure(arm, act, contact):
    n = int(round(act.frame_range[1]))
    feet = [b.name for b in arm.pose.bones if b.name.startswith("Leg_") and b.name.endswith("_Lower")]
    track = {f: [] for f in feet}
    for fr in range(0, n + 1):
        C.set_action(arm, act, fr)
        bpy.context.view_layer.update()
        for f in feet:
            p = arm.matrix_world @ arm.pose.bones[f].tail
            track[f].append((round(p.y, 5), round(p.z, 5)))
    out, slides = {}, []
    for f, pts in track.items():
        zmin = min(z for _, z in pts)
        planted = [i for i, (_, z) in enumerate(pts) if z - zmin <= contact]
        steps = [pts[i + 1][0] - pts[i][0] for i in planted if i + 1 in planted and i + 1 <= n]
        sweep_y = [pts[i][0] for i in planted]
        out[f] = {"planted_frames": planted, "slide_per_frame": round(sum(steps) / len(steps), 5) if steps else None,
                  "planted_sweep": round(max(sweep_y) - min(sweep_y), 4) if sweep_y else None,
                  "reach_y": round(max(y for y, _ in pts) - min(y for y, _ in pts), 4), "lift": round(
                      max(z for _, z in pts) - zmin, 4)}
        slides += steps
    per_frame = sum(slides) / len(slides) if slides else 0.0
    return {"frames": n, "feet": out, "mean_slide_per_frame": round(per_frame, 5),
            "planted_distance_per_loop": round(per_frame * n, 4)}


def main():
    args = C.cli_args()
    tag = args.get("tag") or "check"
    path = args.get("blend") or os.path.join(C.BLEND, "FireLarva.blend")
    bpy.ops.wm.open_mainfile(filepath=path)
    arm = bpy.data.objects["FireLarva_Rig"]
    res = {"blend": path, "contact": float(args.get("contact") or 0.004)}
    with open(os.path.join(C.EXPORTS, "FireLarva", "FireLarva_report.json"), encoding="utf-8") as fh:
        rep = json.load(fh)
    res["report_crawl_distance_per_loop"] = rep.get("crawl_distance_per_loop")
    res["Crawl"] = measure(arm, bpy.data.actions["Crawl"], res["contact"])
    os.makedirs(QA, exist_ok=True)
    with open(os.path.join(QA, f"r56_firelarva_gait_{tag}.json"), "w", encoding="utf-8") as fh:
        json.dump(res, fh, indent=1)
    C.log("LARVA GAIT", tag, json.dumps({k: v for k, v in res["Crawl"].items() if k != "feet"}))


if __name__ == "__main__":
    main()
