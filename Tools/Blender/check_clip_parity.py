"""Round 49E follow-up: clip parity of a redesigned export against an older version (the curator found that the round-49
rebuilds of the Prowler and the SlimeMonarch had dropped their `Strike` clips: a species build script re-exports only
its own clips; the attack passes (build_attacks36.py, build_slug_strike.py, the boss mechanic scripts) must run after).

For each creature it imports <Name>.fbx and <Name>_LOD1.fbx of the current export and of a base version (a git revision,
read with `git show`, or a backup folder) and compares:
  - the clip set and every clip's length (from the FBX actions, not from the report),
  - the bone names (sockets are bones: a missing bone is a missing socket; new bones are listed),
  - every event frame and socket list in the report (any key containing "frame", any list under a key containing
    "bone" or "socket", flattened with its path).
Prints one line per creature and writes ArtSource/Previews/QA/clip_parity_<tag>.json.

Run: blender -b --factory-startup --python Tools/Blender/check_clip_parity.py -- --only Prowler,SlimeMonarch
     [--base 2e4fc688 | --base-dir ArtSource/Exports/_backup_r49/{name}] [--tag r49e]
"""

import json
import os
import subprocess
import sys
import tempfile

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402

import common as C  # noqa: E402

QA = os.path.join(C.REPO, "ArtSource", "Previews", "QA")


def clear():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def fbx_facts(path):
    """{clip: frames} and the bone names of an FBX (empty when the file does not exist)."""
    if not os.path.exists(path):
        return None
    clear()
    bpy.ops.import_scene.fbx(filepath=path)
    clips = {}
    for act in bpy.data.actions:
        name = act.name.split("|")[-1]
        clips[name] = int(round(act.frame_range[1] - act.frame_range[0]))
    bones = sorted({b.name for o in bpy.data.objects if o.type == "ARMATURE" for b in o.data.bones})
    return {"clips": clips, "bones": bones}


def report_facts(path):
    """Flattened event frames and socket lists of a report."""
    if not os.path.exists(path):
        return {}
    with open(path, encoding="utf-8") as fh:
        rep = json.load(fh)
    out = {}

    def walk(node, prefix):
        if isinstance(node, dict):
            for k, v in node.items():
                if k in ("detail", "uv") or k.endswith("_detail") or k == "parts":
                    continue
                key = f"{prefix}.{k}" if prefix else k
                low = k.lower()
                if isinstance(v, (int, float)) and not isinstance(v, bool) and "frame" in low:
                    out[key] = v
                elif isinstance(v, list) and ("bone" in low or "socket" in low) and all(isinstance(x, str) for x in v):
                    out[key] = sorted(v)
                else:
                    walk(v, key)
    walk(rep, "")
    return out


def base_files(name, base, base_dir, tmp):
    """Paths of the base version's FBX, LOD1 FBX and report (extracted from git into `tmp` when `base` is a rev)."""
    files = {"fbx": f"{name}.fbx", "lod": f"{name}_LOD1.fbx", "report": f"{name}_report.json"}
    out = {}
    for key, fname in files.items():
        if base_dir:
            out[key] = os.path.join(C.REPO, base_dir.format(name=name), fname)
            continue
        rel = f"ArtSource/Exports/{name}/{fname}"
        dst = os.path.join(tmp, f"{name}_{fname}")
        res = subprocess.run(["git", "-C", C.REPO, "show", f"{base}:{rel}"], capture_output=True)
        if res.returncode == 0 and res.stdout.startswith(b"version https://git-lfs"):
            # The exports live in Git LFS: `git show` gives the pointer; smudge it into the file from the local store.
            res = subprocess.run(["git", "-C", C.REPO, "lfs", "smudge"], input=res.stdout, capture_output=True)
        if res.returncode == 0:
            with open(dst, "wb") as fh:
                fh.write(res.stdout)
            out[key] = dst
        else:
            out[key] = dst + ".missing"
    return out


def compare(name, base, base_dir, tmp):
    cur_dir = os.path.join(C.EXPORTS, name)
    cur = {"fbx": fbx_facts(os.path.join(cur_dir, name + ".fbx")),
           "lod": fbx_facts(os.path.join(cur_dir, name + "_LOD1.fbx")),
           "report": report_facts(os.path.join(cur_dir, name + "_report.json"))}
    bf = base_files(name, base, base_dir, tmp)
    old = {"fbx": fbx_facts(bf["fbx"]), "lod": fbx_facts(bf["lod"]), "report": report_facts(bf["report"])}
    issues, notes = [], []
    for key in ("fbx", "lod"):
        a, b = old[key], cur[key]
        if a is None:
            notes.append(f"{key}: no base file")
            continue
        if b is None:
            issues.append(f"{key}: missing")
            continue
        for clip, n in sorted(a["clips"].items()):
            if clip not in b["clips"]:
                issues.append(f"{key}: clip {clip} ({n}) missing")
            elif b["clips"][clip] != n:
                issues.append(f"{key}: clip {clip} length {n} -> {b['clips'][clip]}")
        for clip in sorted(set(b["clips"]) - set(a["clips"])):
            notes.append(f"{key}: new clip {clip} ({b['clips'][clip]})")
        lost = sorted(set(a["bones"]) - set(b["bones"]))
        if lost:
            issues.append(f"{key}: bones missing {lost}")
        new = sorted(set(b["bones"]) - set(a["bones"]))
        if new:
            notes.append(f"{key}: new bones {new}")
    for k, v in sorted(old["report"].items()):
        if k.startswith(("lod1.", "actions.")):
            continue
        if k not in cur["report"]:
            issues.append(f"report: {k} = {v} missing")
        elif cur["report"][k] != v:
            now = cur["report"][k]
            if isinstance(v, list) and isinstance(now, list) and set(v) <= set(now):
                notes.append(f"report: {k} gains {sorted(set(now) - set(v))}")  # an addition, nothing lost
            else:
                issues.append(f"report: {k} {v} -> {now}")
    return {"creature": name, "ok": not issues, "issues": issues, "notes": notes,
            "clips_before": (old["fbx"] or {}).get("clips"), "clips_after": (cur["fbx"] or {}).get("clips")}


def main():
    args = C.cli_args()
    base = args.get("base") or "2e4fc688"
    base_dir = args.get("base-dir")
    tag = args.get("tag") or "check"
    results = []
    with tempfile.TemporaryDirectory() as tmp:
        for name in args["only"].split(","):
            if name:
                r = compare(name, base, base_dir, tmp)
                results.append(r)
                C.log("PARITY", name, "OK" if r["ok"] else "FAIL", json.dumps(r["issues"]), json.dumps(r["notes"]))
    os.makedirs(QA, exist_ok=True)
    with open(os.path.join(QA, f"clip_parity_{tag}.json"), "w", encoding="utf-8") as fh:
        json.dump({"base": base_dir or base, "results": results}, fh, indent=1)


if __name__ == "__main__":
    main()
