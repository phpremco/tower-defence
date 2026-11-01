"""Render alle geregistreerde geluiden en schrijf manifest.json.
Gebruik: python3 build.py [categoriemodules...] [--only substring]"""
import sys, json, os, time, importlib
import sfxlib as s

args = sys.argv[1:]
only = None
if "--only" in args:
    i = args.index("--only")
    only = args[i + 1]
    del args[i:i + 2]
mods = args or ["cat_a", "cat_b", "cat_c"]
for m in mods:
    importlib.import_module(m)

man_path = os.path.join(s.OUT, "manifest.json")
manifest = json.load(open(man_path)) if os.path.exists(man_path) else {}
t0 = time.time()
for e in s.REGISTRY:
    if only and only not in e["path"]:
        continue
    files = s.render(e)
    manifest[e["path"]] = dict(title=e["title"], desc=e["desc"], files=files)
    print(f"{time.time() - t0:6.1f}s  {e['path']}  ({len(files)})", flush=True)
json.dump(manifest, open(man_path, "w"), indent=1)
print("manifest entries:", len(manifest))
