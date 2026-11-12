"""The Shattered, round 28 improvement pass (see ArtSource/DESIGN_NOTES.md round 28). Replaces the round-16 export
of build_shattered.py (run after it; build_shattered_mech.py runs after this one).

Review finding: the round-16 brute read as a brown gorilla with Gravemaw's plum-bronze armour, a small low head and
no weapon, and nothing on it told the player it would split into three. This pass keeps the rig and the round-16
clips (Walk/Idle/Hit/PhaseBreak1/PhaseBreak2/Death, phase table by bone scale) and changes what is read at play size:

- palette: pale shattered-ice granite plates over a black glass under-layer (the Southern Crown's pale ice over dark
  rock), magenta-white overcharge light; no longer Gravemaw's plum-bronze;
- silhouette: a tall crown of crystal blades on the head (the head now reads above the hump), long crystal cleavers
  grown out of both fists (its weapon), and three big crystal spires on the back (shoulders, hump, pelvis), each in
  the silhouette of one of the three shards it splits into (ShardRunner, ShardFlyer, ShardCrawler);
- the three spire bones `Core_Road`, `Core_Air`, `Core_Land` (scale) let Unity show which shards died before the
  rejoin (a dead shard's spire at 0.35).

Run: blender --background --factory-startup --python Tools/Blender/build_shattered2.py -- [--preview <dir>]
     [--norender] [--blockout]
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import numpy as np  # noqa: E402
from mathutils import Vector  # noqa: E402

import build_shattered as BS  # noqa: E402
import chitin as H  # noqa: E402
import common as C  # noqa: E402
import crysium as Y  # noqa: E402
import crystal_growth as G49  # noqa: E402
import titan_kit as T  # noqa: E402
from common import G  # noqa: E402

NAME = BS.NAME
LOD_NAME = BS.LOD_NAME
K = BS.K

# Pale shattered-ice granite over black glass; the overcharge light stays the continent's magenta-white.
BS.PAL = H.Palette(top=(0.64, 0.66, 0.72), side=(0.29, 0.3, 0.36), belly=(0.06, 0.06, 0.08), seam=(0.03, 0.025, 0.04),
                   dark=(0.1, 0.1, 0.13), glow=(1.0, 0.42, 0.78), eye=(1.0, 0.86, 0.96), membrane=(0.42, 0.1, 0.3),
                   bone=(0.93, 0.93, 0.97))
BS.CORE = H.Palette(top=(0.14, 0.06, 0.12), side=(0.07, 0.03, 0.07), belly=(0.04, 0.015, 0.035), seam=(0.03, 0.01, 0.03),
                    dark=BS.PAL.dark, glow=(1.0, 0.8, 0.96), eye=BS.PAL.eye, membrane=(0.6, 0.12, 0.4), bone=BS.PAL.bone)
Y.PALETTES["overcharge_w"] = ((np.array((0.3, 0.04, 0.3)), np.array((1.0, 0.5, 0.86)), np.array((1.0, 0.97, 1.0))),
                              (np.array((1.0, 0.6, 0.9)), np.array((1.0, 0.97, 1.0))))

# The three shard spires: (bone, parent, base, [(direction, length, radius)]) - runner = a forward-raked spear
# fan (shoulders), flyer = two swept wing blades (hump), crawler = a low ring of hooked claws (pelvis).
CORES = [("Core_Road", "Chest", G(0, 2.55, 0.35), [((0, 1.0, 0.55), 1.05, 0.12), ((0.35, 0.95, 0.4), 0.8, 0.09),
                                                    ((-0.35, 0.95, 0.4), 0.8, 0.09)]),
         ("Core_Air", "Chest", G(0, 2.75, -0.35), [((0.9, 0.55, -0.45), 1.2, 0.11), ((-0.9, 0.55, -0.45), 1.2, 0.11),
                                                   ((0.0, 1.0, -0.2), 0.75, 0.1)]),
         ("Core_Land", "Pelvis", G(0, 1.95, -1.35), [((0.6, 0.8, -0.2), 0.65, 0.09), ((-0.6, 0.8, -0.2), 0.65, 0.09),
                                                     ((0.0, 0.85, -0.6), 0.7, 0.1), ((0.0, 1.0, 0.2), 0.55, 0.08)])]
CORE_NAMES = [c[0] for c in CORES]


# Round 49G (strict loose-part check): every crystal cluster's satellites and every spire start this much deeper, so at
# any phase scale they grow out of the core instead of standing over its curve.
CLUSTER_SINK = 0.25
SPIRE_SINK = 0.5
# The crest grows tighter (spread 0.32 -> 0.2) and deeper: a front satellite stood over the face, 0.28 m off the head.
CREST_SINK = 0.35


def separate_shell_fuse():
    """Round 49G: the shell plates now carry sinew straps into the core, so one voxel remesh would weld each plate to
    the core and the phase breaks could no longer scale a plate away. The body material's parts are remeshed twice in
    this process (the plates with their spikes and straps, BS.SHELL_PARTS, apart from the rest) and joined, and every
    skinning is preceded by a speck clean-up (islands under 8 vertices) and followed by a purity pass: an island led by a Shell_n bone goes wholly to it, every other island loses
    its Shell_n weights (the core round a strap must not follow a plate)."""
    import bmesh
    import bpy
    import check_floating_parts as F
    plain_fuse, plain_skin = C.fuse, C.skin
    body = BS.body_parts()
    body_faces = len(body.face_pid)

    def split(obj, keep):
        o2 = C.link(bpy.data.objects.new(obj.name + "_split", obj.data.copy()))
        bm = bmesh.new()
        bm.from_mesh(o2.data)
        bm.faces.ensure_lookup_table()
        bmesh.ops.delete(bm, geom=[bm.faces[i] for i in np.nonzero(~keep)[0]], context="FACES")
        bm.to_mesh(o2.data)
        bm.free()
        return o2

    def fuse(obj, voxel, smooth_iterations=6):
        if len(obj.data.polygons) != body_faces:
            return plain_fuse(obj, voxel, smooth_iterations)
        shell = np.array([body.parts[p].name in BS.SHELL_PARTS for p in body.face_pid])
        verts, faces = [], []
        for keep in (~shell, shell):
            o2 = split(obj, keep)
            me = plain_fuse(o2, voxel, smooth_iterations)
            data = o2.data
            bpy.data.objects.remove(o2)
            bpy.data.meshes.remove(data)
            co = np.empty(len(me.vertices) * 3)
            me.vertices.foreach_get("co", co)
            base = sum(len(v) for v in verts)
            verts.append(co.reshape(-1, 3))
            faces += [[base + i for i in p.vertices] for p in me.polygons]
            bpy.data.meshes.remove(me)
        mesh = bpy.data.meshes.new(obj.name + "_fused")
        mesh.from_pydata(np.concatenate(verts).tolist(), [], faces)
        mesh.update()
        C.log("Shattered shells remeshed apart:", len(faces), "faces")
        return mesh

    def skin(mesh_obj, arm, builder, *a, **kw):
        import build_bulwark as BW
        BW.drop_specks(mesh_obj, min_verts=8)  # single triangles the symmetrise/decimate leaves (3-vertex islands)
        out = plain_skin(mesh_obj, arm, builder, *a, **kw)
        if not any(g.name.startswith("Shell_") for g in mesh_obj.vertex_groups):
            return out
        groups = {g.index: g.name for g in mesh_obj.vertex_groups}
        by_name = {g.name: g for g in mesh_obj.vertex_groups}
        verts = mesh_obj.data.vertices
        fixed = 0
        for isl in F.islands(mesh_obj.data):
            lead = F.dominant_bone(mesh_obj, isl) or ""
            shell = lead if lead.startswith("Shell_") else None
            for i in isl:
                v = verts[int(i)]
                ws = {groups[g.group]: g.weight for g in v.groups}
                if shell:
                    new = {shell: 1.0}
                else:
                    new = {n_: w for n_, w in ws.items() if not n_.startswith("Shell_")}
                    tot = sum(new.values())
                    new = {n_: w / tot for n_, w in new.items()} if tot > 1e-6 else {"Chest": 1.0}
                if new == ws:
                    continue
                for g in list(v.groups):
                    mesh_obj.vertex_groups[g.group].remove([v.index])
                for n_, w in new.items():
                    (by_name.get(n_) or mesh_obj.vertex_groups.new(name=n_)).add([v.index], w, "REPLACE")
                    by_name = {g.name: g for g in mesh_obj.vertex_groups}
                fixed += 1
        C.log(mesh_obj.name, "shell weights made pure on", fixed, "vertices")
        return out
    C.fuse, C.skin = fuse, skin


def overcharge(seed, strength=1.0, palette="overcharge_w"):
    """Overcharged Crysium whose light comes in pockets and differs per shard (round 49D)."""
    return G49.uneven_glow(Y.shard_style(palette, strength, seed=seed), seed, 0.4, 1.3)


def half(v):
    """Round 49D: the boss's crystal material is mirrored (titan_kit symmetrises the game mesh, +X over -X), so every
    cluster is grown on the +X side only and the mirror makes its twin: a midline growth is pushed a little to +X and
    leans outwards, and the pair reads as a cracked-open V instead of a shard cut in half at x = 0."""
    return G(abs(v[0]), v[1], v[2])


def cluster_frame(shards):
    """Mean direction and longest reach of a round-16 CRYS entry."""
    d = sum((G(*s[0]).normalized() for s in shards), Vector()).normalized()
    return d, max(s[1] for s in shards) * 1.45


def crystal_parts():
    """Round 49D (Remco: "crystals too perfect"): every crystal is irregular growth (crystal_growth.py) instead of
    fluted 7/8-sided prisms: fractured faces, chipped steps, broken or off-centre points, bent and spiralling shards,
    hovering fragments, uneven inner light. Same bones, places and silhouettes: the inner clusters Crys_1..6 (scale
    phases), the jaw teeth, the crest on the head, the fist cleavers and the three shard spires."""
    b = C.PartBuilder()
    b.mirror_weights = True  # round 49F, see mirror_crystal_weights()
    # Inner clusters (Crys_1..6): grown from the bone head along the old fan's mean direction.
    for i, (name, head, shards, mirrored) in enumerate(BS.CRYS):
        d, reach = cluster_frame(shards)
        if not mirrored:
            d = (d + G(0.35, 0, 0)).normalized()
        base = head + G(0.14, 0, 0)
        G49.growth_cluster(b, base + d * 0.05, half(d), reach * 0.95, 5000 + i, bone=name, chain=[name],
                           style=overcharge(60 + i, 1.0, "overcharge"), count=4 if mirrored else 5, spread=0.3,
                           lean=(0.1, 0.55), floating=0, spiral=1, bent=1, radius_ratio=0.12, name="Cluster",
                           sink=CLUSTER_SINK)
    # Round 49G: the crystal teeth that rose from the jaw's front lip are gone; the mouth's teeth are rooted in its gums
    # (shattered_detail.py) and the new muzzle sits where they stood.
    # Crest on the head: one lopsided growth raked back (mirrored into a cracked crown), fragments over it.
    hc = BS.HEAD_C
    G49.growth_cluster(b, hc + G(0.16, 0.2, 0.05), G(0.35, 1.0, -0.05), 1.7, 5200, bone="Head", chain=["Head"],
                       style=overcharge(80), count=6, spread=0.2, lean=(0.08, 0.7), sideways=G(0.3, 0.0, -0.2),
                       floating=0, spiral=1, bent=2, radius_ratio=0.085, name="Crest", sink=CREST_SINK)
    # Cleavers out of the fists (the right fist; the mirror makes the left): a long fractured blade forward and down
    # past the knuckles with a chipped, stepped edge and spurs along its back, a short hooked spur behind.
    for (side, i), lg in BS.arms().items():
        if side != "R":
            continue
        sh, el, wr = lg.rest_points
        base = wr + Vector((0, 0, 0.32))
        blade_d = G(0.12, -0.25, 1.0).normalized()
        G49.fractured_shard(b, "Blade", base + G(0.08, 0.0, 0.1), blade_d, 0.13, 1.3, 5300, sides=5, flat=0.38,
                            tip="point", steps=2, chips=3, bend=G(0.0, 0.18, 0.12), bone=lg.names[1],
                            style=overcharge(90))
        for j, t in enumerate((0.3, 0.52, 0.7)):
            root = base + G(0.08, 0.0, 0.1) + blade_d * (1.3 * t) + G(0.0, 0.07 - 0.02 * j, 0.0)
            G49.fractured_shard(b, "Blade", root, (G(0.05, 1.0, -0.6)).normalized(), 0.045, 0.22 - 0.04 * j,
                                5310 + j, flat=0.4, tip="point", steps=0, bone=lg.names[1], style=overcharge(92 + j))
        G49.fractured_shard(b, "Blade", base + G(0.1, 0.15, -0.1), G(0.5, 0.5, -0.7).normalized(), 0.075, 0.6, 5320,
                            flat=0.5, tip="point", bend=G(0.0, -0.12, -0.05), bone=lg.names[1], style=overcharge(91, 0.9))
    # The three shard spires keep their silhouettes (spear fan, swept wings, claw ring), each shard fractured and
    # uneven. Round 49F (Remco: "check the crystal positions"): no hovering fragments anywhere (they read as loose
    # crystals); the floating-part check (check_floating_49f.py) confirms every crystal sits on the body.
    for k, (name, parent, base, shards) in enumerate(CORES):
        for j, (d, length, radius) in enumerate(shards):
            dv = G(*d)
            if dv.x < -1e-6:
                continue  # the mirror makes the -X shard
            dv = (dv + G(0.12, 0, 0) if abs(dv.x) < 1e-6 else dv).normalized()
            # Round 49G: every spire shard starts SPIRE_SINK deeper (inside the core, not on the shell that phase 2
            # strips away), so with the plates gone it still grows out of the body.
            sink = SPIRE_SINK / max(dv.z, 0.4)
            G49.fractured_shard(b, "Spire", base + G(0.06 if abs(d[0]) < 1e-6 else 0.0, 0, 0) - dv * sink, dv, radius,
                                length + sink,
                                5400 + 10 * k + j, flat=0.55, steps=1 + (j % 2), bend=dv.orthogonal() * 0.08 * length,
                                bone=name, chain=[name], style=overcharge(100 + j))
    return b


def mirror_crystal_weights():
    """Round 49F (Remco: "check the crystal positions"): the crystal mesh is grown on +X and symmetrised, but skinning
    gives each vertex the bone of its nearest part, and the -X half has no parts of its own: the left fist's cleaver
    took the jaw teeth's bone (Jaw) and drifted off the fist by up to 0.4 m (Climb_Up, Roar), other left-side shards
    could pick a wrong bone the same way. Every skinning of a crystal mesh in this process is followed by a mirror of
    the weights: each -X vertex gets the weights of its +X twin, with _L/_R swapped in the bone names (the builders
    of crystal_parts() carry `mirror_weights`, so the LOD's crystal mesh gets it too)."""
    import re
    from mathutils.kdtree import KDTree
    plain = C.skin

    def swap(name):
        return re.sub(r"_(L|R)(?=\d|_|$)", lambda m: "_R" if m.group(1) == "L" else "_L", name)

    def skin(mesh_obj, arm, builder, *a, **kw):
        plain(mesh_obj, arm, builder, *a, **kw)
        if not getattr(builder, "mirror_weights", False):
            return
        verts = mesh_obj.data.vertices
        tree = KDTree(len(verts))
        for v in verts:
            tree.insert(v.co, v.index)
        tree.balance()
        groups = {g.index: g.name for g in mesh_obj.vertex_groups}
        by_name = {g.name: g for g in mesh_obj.vertex_groups}
        fixed = 0
        for v in verts:
            if v.co.x >= -1e-4:
                continue
            twin = tree.find(Vector((-v.co.x, v.co.y, v.co.z)))[1]
            source = [(groups[g.group], g.weight) for g in verts[twin].groups]
            for g in list(v.groups):
                mesh_obj.vertex_groups[g.group].remove([v.index])
            for gname, w in source:
                target = by_name.get(swap(gname)) or by_name.get(gname)
                if target is not None:
                    target.add([v.index], w, "REPLACE")
            fixed += 1
        C.log(mesh_obj.name, "mirrored crystal weights onto", fixed, "vertices")
    C.skin = skin


def bones():
    out = BS.bones()
    for name, parent, base, shards in CORES:
        out.append((name, base - G(0, 0.15, 0), base + G(0, 0.25, 0), parent, True))
    return out


def main():
    separate_shell_fuse()
    mirror_crystal_weights()
    args = C.cli_args()
    out = C.preview_dir()
    scale_bones = BS.SCALE_BONES + CORE_NAMES
    rigid = BS.CRYSTAL_RIGID | {n + x for n in ("Cluster", "Crest", "Spire") for x in ("", "Fragment", "Root")} | {"Blade"}
    if args.get("blockout"):
        C.reset_scene()
        T.blockout("shattered2", [(BS.body_parts(), (0.6, 0.62, 0.68), None), (crystal_parts(), (1.0, 0.5, 0.85), (1.0, 0.5, 0.85))],
                   bones(), BS.CLIPS, out, (0, 0, 1.5), 13.0,
                   [("close", "close", "Idle", 20), ("side", "side", "Walk", 8), ("front", "front", "Idle", 20),
                    ("p3", "close", "PhaseBreak2", BS.BREAK2_FRAMES)], [], scale_bones=scale_bones,
                   rigid=BS.BODY_RIGID | rigid)
        return
    C.reset_scene()
    mats = [T.Mat(BS.body_parts(), 17500, ("chitin", "membrane", "rock"), voxel=0.012, ao=0.35, k=K, emission=2.6,
                  specular=0.3, extra=H.weathering(K, scratches=0.6, dust=0.35, seed=191, markings=BS.PAL.dark,
                                                   marking_amount=0.2), rigid=BS.BODY_RIGID, blend=0, lod_share=0.7,
                  islands=BS.SHELL_NAMES),
            T.Mat(crystal_parts(), 6500, ("crystal", "rock"), suffix="Crystal", voxel=0.009, ao=0.2, extrusion=0.04,
                  ray=0.12, k=K * 0.6, emission=2.8, specular=0.45, rigid=rigid, blend=0, smooth=2, lod_share=0.3)]
    report = {"walk_distance_per_loop": round(BS.STRIDE / BS.STANCE, 3), "pivot": "ground, under the chest",
              "phases": {str(p): {"shells": v[0], "crystals": v[1]} for p, v in BS.PHASE.items()},
              "phase_break_1": {"burst_frame": BS.BREAK1_BURST, "shells_off": BS.SHELL_NAMES[:4], "frames": BS.BREAK1_FRAMES},
              "phase_break_2": {"burst_frame": BS.BREAK2_BURST, "shells_off": BS.SHELL_NAMES[4:], "frames": BS.BREAK2_FRAMES},
              "death": {"explosion_frame": BS.DEATH_BLAST, "crystals_gone_frame": BS.DEATH_BLAST + 8},
              "emission_per_phase": {"1": 2.6, "2": 3.4, "3": 4.5},
              "round28": {"pass": "improvement (palette, crown, cleavers, three shard spires)",
                          "shard_spires": {"Core_Road": "ShardRunner", "Core_Air": "ShardFlyer", "Core_Land": "ShardCrawler"}}}
    assets, arm, mesh, actions, poser, lod = T.build_titan(
        NAME, mats, bones(), BS.CLIPS, lod_name=LOD_NAME, lod_tris=9500, lod_drop=BS.LOD_DROP, scale_bones=scale_bones,
        clip_scales={c: BS.SCALE_BONES for c in BS.CLIPS}, report=report, unity_scale=BS.UNITY_SCALE)
    if args.get("norender"):
        return
    T.previews("shattered2", arm, mesh, actions, lod, out, assets, (0, 0, 1.6), 13.5, ("Walk", 8),
               shots=[("close", "close", "Idle", 20), ("side", "side", "Walk", 8), ("front", "front", "Idle", 20),
                      ("top", "top", "Walk", 12), ("back", "back", "Idle", 40)],
               strips=[], macros=[("head", G(0, 1.6, 1.5), 3.2, -30, 12, "Idle", 20)])


if __name__ == "__main__":
    main()
