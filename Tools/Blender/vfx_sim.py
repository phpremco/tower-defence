"""Mantaflow gas sims for the volume flipbooks (round 20): fireballs, smoke puffs and the dust burst.

Each spec builds a fresh scene (domain + lumpy emitter spheres), steps the sim in Replay mode and, per kept frame,
reads the grids from the evaluated domain and renders them with `vfx_kit.render_volume` straight away, so only the
small 2D maps are cached (`<cache>/<name>.npz`) and the 3D grids never hit the disk. Re-running with the cache in
place only re-packs and re-shades.
"""

import os
import time

import numpy as np

import vfx_kit as K


def _clear():
    import bpy
    bpy.ops.wm.read_factory_settings(use_empty=True)


def _lumpy_sphere(name, loc, radius, seed, lump=0.25):
    import bpy
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=3, radius=radius, location=loc)
    ob = bpy.context.object
    ob.name = name
    rng = np.random.default_rng(seed)
    noise = K.Perlin(seed)
    off = rng.uniform(0, 50, 3)
    for v in ob.data.vertices:
        n = v.co.normalized()
        k = noise.fbm(n.x * 1.7 + off[0], n.y * 1.7 + off[1], n.z * 1.7 + off[2], octaves=2)
        v.co = v.co * (1.0 + lump * float(k) * 2.0)
    ob.data.update()
    return ob


def _key_inflow(flow, on_frames):
    """Inflow only during [start, end]: keyed with constant interpolation so the emission switches cleanly."""
    start, end = on_frames
    for frame, value in ((1, start <= 1), (start, True), (end + 1, False)):
        flow.use_inflow = bool(value)
        flow.keyframe_insert("use_inflow", frame=max(1, frame))


def _set_constant(ob):
    ad = ob.animation_data
    if not ad or not ad.action:
        return
    for layer in getattr(ad.action, "layers", []):
        for strip in layer.strips:
            for bag in strip.channelbags:
                for fc in bag.fcurves:
                    for kp in fc.keyframe_points:
                        kp.interpolation = "CONSTANT"
    for fc in getattr(ad.action, "fcurves", []):
        for kp in fc.keyframe_points:
            kp.interpolation = "CONSTANT"


def _soften(a):
    out = a
    for axis in range(3):
        out = 0.5 * out + 0.25 * (np.roll(out, 1, axis) + np.roll(out, -1, axis))
    return out


def simulate(spec, cache_dir):
    """Runs one spec (dict, see build_vfx.VOLUMES) and returns the list of per-frame render dicts."""
    path = os.path.join(cache_dir, spec["name"] + ".npz")
    if os.path.exists(path):
        data = np.load(path)
        n = int(data["count"])
        keys = [k for k in data.files if k != "count"]
        frames = [{} for _ in range(n)]
        for k in keys:
            arr = data[k]
            for i in range(n):
                frames[i][k] = arr[i].astype(np.float32)
        print(f"[vfx] {spec['name']}: {n} frames from cache", flush=True)
        return frames

    import bpy  # only needed to simulate; a cached sim re-packs in plain Python
    _clear()
    sc = bpy.context.scene
    sx, sy, sz = spec["domain"]
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, sz / 2))
    dom = bpy.context.object
    dom.scale = (sx, sy, sz)
    bpy.ops.object.transform_apply(scale=True)
    mod = dom.modifiers.new("Fluid", "FLUID")
    mod.fluid_type = "DOMAIN"
    d = mod.domain_settings
    d.domain_type = "GAS"
    d.resolution_max = spec["res"]
    d.cache_type = "REPLAY"
    d.cache_directory = os.path.join(cache_dir, "manta_" + spec["name"])
    d.use_adaptive_domain = False
    for key, value in spec.get("domain_settings", {}).items():
        setattr(d, key, value)

    for i, em in enumerate(spec["emitters"]):
        ob = _lumpy_sphere(f"Emit{i}", em["loc"], em["radius"], spec["seed"] + i, em.get("lump", 0.25))
        fm = ob.modifiers.new("Fluid", "FLUID")
        fm.fluid_type = "FLOW"
        f = fm.flow_settings
        f.flow_type = spec.get("flow_type", "BOTH")
        f.flow_behavior = "INFLOW"
        f.flow_source = "MESH"
        f.surface_distance = em.get("surface", 1.0)
        f.use_plane_init = False
        f.use_initial_velocity = True
        f.velocity_normal = em.get("vn", 3.0)
        f.velocity_random = em.get("vr", 0.5)
        f.velocity_coord = em.get("vel", (0.0, 0.0, 0.0))
        f.density = em.get("density", 1.0)
        f.temperature = em.get("temperature", 1.0)
        f.fuel_amount = em.get("fuel", 1.0)
        f.subframes = em.get("subframes", 1)
        _key_inflow(f, em["on"])
        _set_constant(ob)
        # A growing emitter is what makes a fireball bloom outward: initial velocity alone dies within a few cells.
        for frame, scale in em.get("scale_keys", []):
            ob.scale = (scale, scale, scale)
            ob.keyframe_insert("scale", frame=frame)

    sc.frame_start = 1
    sc.frame_end = spec["sim_frames"]
    keep = set(spec["keep"])
    dx = sx / max(d.domain_resolution[0], 1) if d.domain_resolution[0] else max(sx, sy, sz) / spec["res"]
    out = []
    started = time.time()
    for fr in range(1, spec["sim_frames"] + 1):
        sc.frame_set(fr)
        if fr not in keep:
            continue
        ev = dom.evaluated_get(bpy.context.evaluated_depsgraph_get())
        ds = ev.modifiers["Fluid"].domain_settings
        rx, ry, rz = ds.domain_resolution
        dx = sx / rx
        dens = np.array(ds.density_grid, dtype=np.float32).reshape(rz, ry, rx)
        if spec.get("flow_type", "BOTH") in ("BOTH", "FIRE"):
            flame = np.array(ds.flame_grid, dtype=np.float32).reshape(rz, ry, rx)
            heat = np.array(ds.heat_grid, dtype=np.float32).reshape(rz, ry, rx)
        else:
            flame = np.zeros_like(dens)
            heat = flame
        # One-voxel smoothing: at game resolutions Mantaflow's rising fire forms single-voxel filaments that read
        # as hair in the flipbook.
        dens, flame, heat = _soften(dens), _soften(flame), _soften(heat)
        # Flame is the burning front; heat lingers in the rising smoke and gives the ember glow inside it.
        emit = (spec.get("emit_scale", 1.0) * np.power(np.clip(flame, 0, None), spec.get("emit_power", 1.5)) +
                spec.get("heat_glow", 0.0) * np.power(np.clip(heat / spec.get("heat_ref", 3.0), 0, 1), 2.0) * dens)
        # Burning gas is thin and bright; the soot wraps it. Without this the smoke shell hides the whole fireball.
        ext = dens * (1.0 - spec.get("flame_clear", 0.8) * np.clip(flame * 2.0, 0, 1))
        soot = dens * np.clip(1.0 - 1.5 * flame, 0, 1) ** 2
        maps = K.render_volume(ext, emit, dx, spec["sigma"], spec["emit_gain"], emit_dens=soot)
        out.append(maps)
        print(f"[vfx] {spec['name']} frame {fr} ({time.time() - started:.0f}s) res {rx}x{ry}x{rz} "
              f"alpha {maps['alpha'].max():.2f} emis {maps['emissive'].max():.2f} "
              f"flame {flame.max():.2f} heat {heat.max():.2f} dens {dens.max():.2f}", flush=True)

    os.makedirs(cache_dir, exist_ok=True)
    pack = {k: np.stack([m[k] for m in out]).astype(np.float16) for k in out[0]}
    np.savez_compressed(path, count=len(out), **pack)
    return out
