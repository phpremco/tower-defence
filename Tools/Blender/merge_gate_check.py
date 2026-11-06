"""Round 18: puts the round-33 gate contact check (round28.gate.check, written only by a rendering run of
build_gravemaw_mech.py) back into Gravemaw_report.json after a --norender run, from the report in git HEAD saved
to the path given with --old. The gate clips do not ride a smoothed path, so the check stays valid.

Run: blender --background --factory-startup --python Tools/Blender/merge_gate_check.py -- --old <old report>
"""
import json
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import common as C  # noqa: E402

old = json.load(open(C.cli_args()["old"], encoding="utf-8"))
path = os.path.join(C.EXPORTS, "Gravemaw", "Gravemaw_report.json")
new = json.load(open(path, encoding="utf-8"))
gate = new["round28"].setdefault("gate", {})
if "check" not in gate:
    gate["check"] = old["round28"]["gate"]["check"]
    print("MERGE restored the gate check")
with open(path, "w", encoding="utf-8") as fh:
    json.dump(new, fh, indent=2)
