"""Round 31: the slugs (Oozer and its boss, the SlimeMonarch) strike by stretching instead of bobbing.

Remco (2026-11-05): "het hele lichaam beweegt wippend om iets te slaan, het zou mooier zijn als die lichamen rekken".
Until now neither slug had a strike clip, so Unity played its procedural Slam (the whole body springs up and crashes
down). This script adds a `Strike` clip to both and rebuilds the SlimeMonarch's `Flood` slam the same way:

- The tail end stays planted (Root and the rear of Body_1 never move). The body chain (Body_1, Body_2, Body_3, Head)
  lengthens towards the target, each segment by its own share, so the front half reaches furthest.
- Squash and stretch with volume preservation: a segment that grows by L gets width L^-0.65 and height L^-0.35 (thin
  as it reaches, fat as it gathers and as it snaps back).
- A quick gather, a fast lunge that slaps the mouth down on the target at the impact frame, then an elastic snap
  back with one damped overshoot (it bunches up shorter than its rest length, then settles).

No new bones: the chain already carries the whole body with part-aware distance weights, so stretching the four
bones lengthens the mesh evenly. The chain bones now carry scale keys, so every old clip gets constant scale-1 keys on
them (the FBX exporter would otherwise bake whatever scale the previous clip left). Eye stalks and brood sacs are
posed in absolute terms from the stretched head and body, so they ride along without being squashed themselves.

Opens ArtSource/Blend/<Asset>.blend (made by build_oozer.py / build_slimemonarch.py; run those first), keys the full
and the LOD1 rig, exports <Asset>.fbx and <Asset>_LOD1.fbx with every clip, merges a `round31` block into the report
and saves the .blend. Idempotent: Strike is re-keyed and Flood rebuilt on every run.

Run: blender --background --factory-startup --python Tools/Blender/build_slug_strike.py --
     [--only Oozer,SlimeMonarch] [--preview <dir>] [--norender] [--before]
     (--before also renders the old look: Crawl, Hit, the old Flood and a stand-in of Unity's procedural slam bob;
     run it before the first rebuild of Flood.)
"""

import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import bpy  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import boss_kit as B  # noqa: E402
import boss_mech_kit as BMK  # noqa: E402
import common as C  # noqa: E402

CHAIN = ["Body_1", "Body_2", "Body_3", "Head"]
SIDES = ("L", "R")
X = Vector((1, 0, 0))
Z = Vector((0, 0, 1))

STRIKE_FRAMES = 24
STRIKE_IMPACT = 10
FLOOD_FRAMES = 44
FLOOD_SLAM = 24

ASSETS = {
    # unit: model scale against Oozer units (the SlimeMonarch rig was scaled by 2 after keying).
    "Oozer": {"unit": 1.0, "stalks": [f"Stalk_{s}{n}" for s in SIDES for n in (1, 2)], "sacs": [],
              "original": ["Crawl", "Idle", "Hit", "Death"], "game_scale": 0.7, "strike_slow": 1.0},
    # Round 49E: the SlimeMonarch's acid vat has its own bone, Slosh (build_slimemonarch); the new clips key it with the
    # same damped-spring slosh as the species' own clips, driven by the stretched body.
    "SlimeMonarch": {"unit": 2.0, "stalks": [f"Stalk_{s}{n}" for s in SIDES for n in (1, 2, 3)],
                     "sacs": ["Sac_L", "Sac_R"], "original": ["Crawl", "Idle", "Hit", "Death", "Spawn"],
                     "game_scale": 0.9, "strike_slow": 1.25, "slosh": True},
}


def sstep(a, b, x):
    return BMK.sstep(a, b, x)


def spring(g, decay, freq):
    """A damped spring released at g = 0 from 1: overshoots below zero once and rings out."""
    return math.exp(-decay * g) * math.cos(freq * g)


# ---------------------------------------------------------------------------------------------------------------
# The stretch pose
# ---------------------------------------------------------------------------------------------------------------

class SlugPose:
    """Absolute bone matrices for a slug: per chain bone a raise of the front (radians, positive lifts the head end),
    a yaw and a length change; stalks and sacs ride on the posed (stretched) bones without being squashed."""

    def __init__(self, poser, stalks, sacs):
        self.poser = poser
        self.stalks = [s for s in stalks if s in poser.rest]
        self.sacs = [s for s in sacs if s in poser.rest]

    def specs(self, raise_=(0, 0, 0, 0), yaw=(0, 0, 0, 0), stretch=(0, 0, 0, 0), squash=None, stalk_rot=None,
              stalk_scale=1.0, sac_scale=1.0):
        """squash: extra flattening per bone (height down, width up, length kept), e.g. the head on impact."""
        rest = self.poser.rest
        specs = {"Root": {"abs": rest["Root"].copy()}}
        rot = Matrix.Identity(3)
        p = rest[CHAIN[0]].translation.copy()
        posed = {}
        for i, name in enumerate(CHAIN):
            rot = rot @ Matrix.Rotation(yaw[i], 3, Z) @ Matrix.Rotation(-raise_[i], 3, X)
            length = max(0.3, 1.0 + stretch[i])
            sx = length ** -0.65
            sz = length ** -0.35
            if squash:
                q = squash[i]
                sz *= 1.0 - q
                sx *= 1.0 + 0.7 * q
            r = rot @ rest[name].to_3x3()
            m = Matrix.Translation(p) @ r.to_4x4() @ Matrix.Diagonal((sx, length, sz, 1.0))
            specs[name] = {"abs": m}
            posed[name] = (m, rot)
            if i + 1 < len(CHAIN):
                p = p + rot @ (rest[CHAIN[i + 1]].translation - rest[name].translation) * length
        for name in self.stalks:
            pitch, yw = (stalk_rot or {}).get(name, (0.0, 0.0))
            specs[name] = {"abs": self._ride(name, "Head", posed, Matrix.Rotation(yw, 3, Z) @ Matrix.Rotation(pitch, 3, X),
                                             stalk_scale)}
        for name in self.sacs:
            specs[name] = {"abs": self._ride(name, "Body_2", posed, Matrix.Identity(3), sac_scale)}
        return specs

    def _ride(self, name, parent, posed, local_rot, scale):
        rest = self.poser.rest
        m_parent, rot = posed[parent]
        head = m_parent @ rest[parent].inverted() @ rest[name].translation
        r = rot @ local_rot @ rest[name].to_3x3()
        return Matrix.Translation(head) @ r.to_4x4() @ Matrix.Diagonal((scale, scale, scale, 1.0))


# ---------------------------------------------------------------------------------------------------------------
# Clips
# ---------------------------------------------------------------------------------------------------------------

STRIKE_SHARE = (0.1, 0.45, 0.62, 0.3)  # how much of the reach each segment takes (tail end least)


def strike_reach(f):
    """Reach of the strike, 1 at full stretch: a short gather, a fast lunge, contact, an elastic snap back."""
    if f <= 4:
        return -0.16 * sstep(0, 4, f)
    if f <= STRIKE_IMPACT:
        u = (f - 4) / (STRIKE_IMPACT - 4)
        return -0.16 + 1.16 * (1 - (1 - u) ** 2.2)
    if f <= STRIKE_IMPACT + 2:
        return 1.0
    g = f - STRIKE_IMPACT - 2
    return spring(g, 0.3, 0.48) * (1 - sstep(STRIKE_FRAMES - 4, STRIKE_FRAMES, f))


def strike_clip(sp, slow=1.0):
    """slow: time stretch (the boss strikes heavier); the impact frame becomes STRIKE_IMPACT x slow."""
    frames = int(round(STRIKE_FRAMES * slow))

    def spec(t, f):
        f = f / slow
        e = strike_reach(f)
        reach = max(0.0, e)
        gather = sstep(0, 4, f) * (1 - sstep(4, 8, f))
        contact = BMK.pulse(f, STRIKE_IMPACT - 3, STRIKE_IMPACT, STRIKE_IMPACT + 5)
        wobble = math.exp(-0.25 * max(0, f - STRIKE_IMPACT)) * math.sin(0.9 * (f - STRIKE_IMPACT)) * (f > STRIKE_IMPACT)
        wobble *= 1 - sstep(STRIKE_FRAMES - 4, STRIKE_FRAMES, f)
        rad = math.radians
        # The front cocks up while it gathers, stays level while it reaches and the mouth slaps down on contact.
        raise_ = (0.0,
                  rad(3) * gather + rad(2) * reach - rad(1) * contact,
                  rad(9) * gather + rad(3) * reach - rad(8) * contact + rad(3) * wobble,
                  rad(8) * gather - rad(30) * contact - rad(4) * wobble)
        yaw = (0.0, rad(2) * wobble, rad(3) * wobble, rad(4) * wobble)
        stretch = tuple(s * e for s in STRIKE_SHARE)
        squash = (0.0, 0.0, 0.06 * contact, 0.16 * contact)
        stalks = {n: (-rad(45) * reach + rad(12) * gather, rad(8) * wobble * (1 if "_R" in n else -1)) for n in sp.stalks}
        return sp.specs(raise_, yaw, stretch, squash, stalks, stalk_scale=1.0 - 0.35 * reach,
                        sac_scale=1.0 - 0.08 * reach + 0.08 * max(0.0, -e))
    return frames, spec, False


def flood_clip(sp):
    """The SlimeMonarch's power: it stretches up and forward into a tall column, then whips the front down and
    slaps it flat at FLOOD_SLAM (slime bursts out there), the body ripples and settles."""
    rad = math.radians

    def spec(t, f):
        g = max(0.0, f - FLOOD_SLAM)
        rise = sstep(0, 17, f) * (1 - sstep(17, FLOOD_SLAM, f))
        down = sstep(17, FLOOD_SLAM, f) * (1 - sstep(FLOOD_SLAM + 2, FLOOD_SLAM + 14, f))
        settle = 1 - sstep(FLOOD_FRAMES - 6, FLOOD_FRAMES, f)
        ripple = math.exp(-0.16 * g) * (f >= FLOOD_SLAM) * settle
        # Stretch: a tall thin column while it rears, still long as the front lands flat and far ahead, then the
        # elastic snap back (shorter and fatter than at rest once) and settle.
        if f <= 17:
            e = 0.85 * sstep(0, 17, f)
        elif f <= FLOOD_SLAM:
            e = 0.85 - 0.3 * sstep(17, FLOOD_SLAM, f)
        else:
            e = 0.55 * spring(g, 0.14, 0.42) * settle
        share = (0.08, 0.36, 0.48, 0.22)
        stretch = tuple(s * e for s in share)
        raise_ = (0.0,
                  rad(16) * rise + rad(2) * ripple * math.sin(0.7 * g),
                  rad(26) * rise - rad(3) * down + rad(4) * ripple * math.sin(0.7 * g - 1.2),
                  rad(6) * rise - rad(12) * down + rad(5) * ripple * math.sin(0.7 * g - 2.4))
        slap = BMK.pulse(f, FLOOD_SLAM - 2, FLOOD_SLAM, FLOOD_SLAM + 8)
        squash = (0.0, 0.05 * slap, 0.14 * slap, 0.2 * slap)
        yaw = (0.0, 0.0, rad(2) * ripple * math.sin(0.9 * g), rad(3) * ripple * math.sin(0.9 * g - 1))
        stalks = {n: (rad(20) * rise - rad(45) * slap, 0.0) for n in sp.stalks}
        sac = 1.0 + 0.2 * BMK.pulse(t, 0.1, 0.45, 0.6) + 0.1 * slap
        return sp.specs(raise_, yaw, stretch, squash, stalks, stalk_scale=1.0 - 0.3 * slap, sac_scale=sac)
    return FLOOD_FRAMES, spec, False


# ---------------------------------------------------------------------------------------------------------------
# One slug
# ---------------------------------------------------------------------------------------------------------------

def with_slosh(poser, frames, spec, loop):
    """Adds the SlimeMonarch's acid slosh (bone Slosh) to a clip spec (build_slimemonarch.slosh_track)."""
    import build_slimemonarch as SM
    track = SM.slosh_track(poser, frames, spec, loop)

    def full(t, f):
        specs = spec(t, f)
        pitch, roll, bulge = track[min(len(track) - 1, max(0, int(round(f))))]
        specs["Slosh"] = {"rot": [(SM.X, pitch), (SM.Yv, roll)], "scale": bulge}
        return specs
    return full


class Slug:
    def __init__(self, name):
        self.name = name
        self.cfg = ASSETS[name]
        bpy.ops.wm.open_mainfile(filepath=os.path.join(C.BLEND, name + ".blend"))
        self.arm = bpy.data.objects[name + "_Rig"]
        self.mesh = bpy.data.objects[name]
        self.lod_arm = bpy.data.objects[name + "_LOD1_Rig"]
        self.lod_mesh = bpy.data.objects[name + "_LOD1"]
        scale_bones = CHAIN + self.cfg["stalks"] + self.cfg["sacs"] + (["Slosh"] if self.cfg.get("slosh") else [])
        self.scale_bones = scale_bones
        self.poser = B.ScalePoser(self.arm, scale_bones)
        lod_names = {b.name for b in self.lod_arm.data.bones}
        self.lod_poser = B.ScalePoser(self.lod_arm, [n for n in scale_bones if n in lod_names])
        self.pose = SlugPose(self.poser, self.cfg["stalks"], self.cfg["sacs"])
        self.new, self.new_lod, self.frames = {}, {}, {}

    def key(self, clip, fn):
        for n in (clip, clip + "_LOD"):
            act = bpy.data.actions.get(n)
            if act is not None:
                bpy.data.actions.remove(act)
        frames, spec, loop = fn(self.pose)
        if self.cfg.get("slosh"):
            spec = with_slosh(self.poser, frames, spec, loop)
        self.new[clip] = self.poser.action(clip, frames, spec, loop=loop)
        self.new_lod[clip] = C.lod_action(self.poser, self.lod_poser, clip + "_LOD", frames, spec, loop, {})
        self.frames[clip] = frames
        C.log(self.name, "keyed", clip, frames)

    def hold_chain(self):
        """Constant scale 1 on the chain bones in every old clip (they had no scale keys before)."""
        for clip in self.cfg["original"]:
            BMK.hold_scale(bpy.data.actions[clip], {b: 1.0 for b in CHAIN})
            BMK.hold_scale(bpy.data.actions[clip + "_LOD"], {b: 1.0 for b in CHAIN})

    def actions(self, lod=False):
        names = self.cfg["original"] + [c for c in self.new if c not in self.cfg["original"]]
        if lod:
            return {n: self.new_lod.get(n) or bpy.data.actions[n + "_LOD"] for n in names}
        return {n: self.new.get(n) or bpy.data.actions[n] for n in names}

    def export(self):
        folder = os.path.join(C.EXPORTS, self.name)
        # Round 46: the close-up detail parts (<Name>_Face/_Claws, saved in the .blend) stay in the exports.
        extra = [bpy.data.objects.get(f"{self.name}_{p}") for p in ("Face", "Claws")]
        extra_lod = [bpy.data.objects.get(f"{self.name}_{p}_LOD1") for p in ("Face", "Claws")]
        BMK.export_rig(os.path.join(folder, self.name + ".fbx"), self.arm, [self.mesh] + [o for o in extra if o],
                       self.actions())
        BMK.export_rig(os.path.join(folder, self.name + "_LOD1.fbx"), self.lod_arm,
                       [self.lod_mesh] + [o for o in extra_lod if o], self.actions(True))

    def report(self, facts):
        path = os.path.join(C.EXPORTS, self.name, self.name + "_report.json")
        with open(path, encoding="utf-8") as fh:
            rep = json.load(fh)
        rep["actions"] = {n: int(round(a.frame_range[1])) for n, a in self.actions().items()}
        rep["scale_keyed_bones"] = self.scale_bones
        rep["round31"] = facts
        with open(path, "w", encoding="utf-8") as fh:
            json.dump(rep, fh, indent=2)
        C.log("report", path)

    def front_low(self, act, frame):
        """Lowest point of the front quarter of the deformed mesh (does the mouth reach the ground?) and the
        forward reach of the tip, plus the rear tip (must stay put)."""
        dg = bpy.context.evaluated_depsgraph_get()
        C.set_action(self.arm, act, frame)
        dg.update()
        ev = self.mesh.evaluated_get(dg)
        me = ev.to_mesh()
        pts = [ev.matrix_world @ v.co for v in me.vertices]
        ev.to_mesh_clear()
        u = self.cfg["unit"]
        tip = min(p.y for p in pts)
        rear = max(p.y for p in pts)
        front = [p for p in pts if p.y < tip + 0.12 * u]
        return {"front_min_z": round(min(p.z for p in front), 3), "tip_forward": round(-tip, 3),
                "rear": round(rear, 3), "min_z": round(min(p.z for p in pts), 3)}


# ---------------------------------------------------------------------------------------------------------------
# Previews
# ---------------------------------------------------------------------------------------------------------------

def stage(slug, target_forward):
    u = slug.cfg["unit"]
    for o in bpy.data.objects:
        if o.name.endswith("_High") or o is slug.lod_mesh:
            o.hide_render = True
    C.preview_world()
    C.light_rig(scale=1.2 * u)
    C.ground(size=40.0)
    # A stand-in for the target (a gate post): just past the reach of the stretched mouth.
    post = BMK.box("Target", -0.35 * u, 0.35 * u, target_forward, target_forward + 0.1 * u, 0.0, 0.22 * u,
                   BMK.mat("TargetMat", (0.35, 0.4, 0.5), rough=0.6))
    return [post]


def cams(slug):
    u = slug.cfg["unit"]
    side = BMK.side_cam((0, -0.15 * u, 0.18 * u), 2.4 * u, elevation=6.0, azimuth=-90.0)
    ortho = 2.0 * u
    game = C.game_camera(Vector((0, -0.2 * u, 0.15 * u)), ortho)
    return side, game


def strips(slug, act, label, frames, out):
    side, game = cams(slug)
    paths = [BMK.strip(slug.name, slug.arm, act, side, out, f"{label}_side", frames, columns=4, cell=400, samples=12),
             BMK.strip(slug.name, slug.arm, act, game, out, f"{label}_game", frames, columns=4, cell=400, samples=12)]
    for cam in (side, game):
        bpy.data.objects.remove(cam)
    return paths


def bob_clip(slug):
    """A stand-in for Unity's procedural Slam over the Idle clip (EnemyView.AttackPose with the model pose share
    0.6): the whole body springs up nose-first, then crashes forward and down. For the before sheet only."""
    act = bpy.data.actions["Idle"]
    u = slug.cfg["unit"]
    share = 0.6
    frames = list(range(0, 25, 3))
    paths = {"side": [], "game": []}
    side, game = cams(slug)
    for i, f in enumerate(frames):
        t = f / 24
        rear = math.sin(math.pi * min(1.0, t / 0.55)) if t < 0.55 else 0.0
        strike = BMK.pulse(t, 0.4, 0.6, 1.0)
        lift = rear * 0.35 * share * u
        pitch = math.radians(rear * -22 + strike * 30) * share
        along = (strike * 0.6 - rear * 0.1) * share * u
        slug.arm.location = Vector((0, -along, lift))
        slug.arm.rotation_euler = (pitch, 0, 0)
        C.set_action(slug.arm, act, f)
        for key, cam in (("side", side), ("game", game)):
            paths[key].append(C.render(cam, os.path.join(BMK_OUT[0], f"_{slug.name}_bob_{key}_{i}.png"), 640, 360,
                                       samples=12))
    slug.arm.location = Vector()
    slug.arm.rotation_euler = (0, 0, 0)
    out = []
    for key, ps in paths.items():
        path = os.path.join(BMK_OUT[0], f"{slug.name}_before_SlamBob_{key}.png")
        C.contact_sheet(ps, path, 4, 400)
        for p in ps:
            os.remove(p)
        out.append(path)
    for cam in (side, game):
        bpy.data.objects.remove(cam)
    return out


BMK_OUT = [None]


def before(slug, out):
    BMK_OUT[0] = out
    bob_clip(slug)
    strips(slug, bpy.data.actions["Crawl"], "before_Crawl", BMK.frames_of(40, 4), out)
    strips(slug, bpy.data.actions["Hit"], "before_Hit", BMK.frames_of(12, 4), out)
    if "Flood" in bpy.data.actions:
        strips(slug, bpy.data.actions["Flood"], "before_Flood", BMK.frames_of(FLOOD_FRAMES, 8), out)


def after(slug, out):
    slow = slug.cfg["strike_slow"]
    frames = [int(f * slow + 0.5) for f in (0, 3, 6, 8, 10, 12, 15, 18, 20, 22, 24)]
    strips(slug, slug.new["Strike"], "Strike", frames + [1], out)
    strips(slug, bpy.data.actions["Crawl"], "after_Crawl", BMK.frames_of(40, 4), out)
    if "Flood" in slug.new:
        strips(slug, slug.new["Flood"], "Flood", [0, 8, 14, 18, 21, 24, 27, 31, 35, 39, 44, 26], out)


# ---------------------------------------------------------------------------------------------------------------

def build(name, args, out):
    slug = Slug(name)
    if args.get("before") and not args.get("norender"):
        props = stage(slug, 0.75 * slug.cfg["unit"])
        before(slug, out)
        BMK.remove(props)
    slug.hold_chain()
    slow = slug.cfg["strike_slow"]
    impact = int(STRIKE_IMPACT * slow + 0.5)
    slug.key("Strike", lambda sp: strike_clip(sp, slow))
    if name == "SlimeMonarch":
        slug.key("Flood", flood_clip)
    sf = slug.frames["Strike"]
    facts = {"Strike": {"frames": sf, "loop": False, "impact_frame": impact,
                        "impact": slug.front_low(slug.new["Strike"], impact),
                        "rest": slug.front_low(slug.new["Strike"], 0),
                        "snap_back_shortest": slug.front_low(slug.new["Strike"], int(18 * slow + 0.5))}}
    if "Flood" in slug.new:
        facts["Flood"] = {"frames": FLOOD_FRAMES, "loop": False, "slam_frame": FLOOD_SLAM,
                          "top": slug.front_low(slug.new["Flood"], 16), "slam": slug.front_low(slug.new["Flood"], FLOOD_SLAM)}
    facts["chain_scale_bones"] = CHAIN
    facts["note"] = ("tail end planted; the chain stretches toward the target (length L, width L^-0.65, "
                     "height L^-0.35); old clips hold the chain at scale 1")
    C.log(name, json.dumps(facts))
    slug.export()
    slug.report(facts)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(C.BLEND, name + ".blend"), compress=True)
    if not args.get("norender"):
        tf = -facts["Strike"]["impact"]["tip_forward"] - 0.13 * slug.cfg["unit"]
        props = stage(slug, -tf)
        after(slug, out)
        BMK.remove(props)


def main():
    args = C.cli_args()
    out = C.preview_dir()
    only = [n for n in (args.get("only") or ",".join(ASSETS)).split(",") if n]
    for name in only:
        build(name, args, out)


if __name__ == "__main__":
    main()
