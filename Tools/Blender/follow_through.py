"""Round 49F: secondary motion for bone chains (Remco on the Bulwark and the Crystal Warlord: "the tail, neck and head
are very static. Make their movements more natural").

A clip is first solved as authored (every frame's armature-space pose). Then each chain (the tail, the neck and head)
follows the body instead of being carried rigidly: the tail tip of every chain bone is a damped spring that chases
where the authored pose puts it, the further down the chain the softer the spring. So the chain lags behind a turn
or a lurch of its root, overshoots when the root stops and settles (overlapping action and follow-through), and the
lag grows towards the tip (a wave runs down the tail). Each bone is then turned from its authored direction towards
its spring's position (a gain exaggerates it, a cap limits it), and everything hanging from a moved bone (the jaw on
the head) is carried along.

- Loops are solved for their steady state (the springs run three cycles; the last one is used), so frame 0 and the
  last frame still match and the loop stays seamless.
- One-shots start with the springs on the authored pose and moving with it, so frame 0 is the authored frame 0 (the
  clip starts where the previous one blends in); `settle` frames at the end blend the extra motion back out when a
  clip must end on its authored pose.
- Nothing here changes the root, the pelvis, the legs or the arms, so feet, events and clip lengths stay as they are.
"""

import math

from mathutils import Matrix, Vector


class Chain:
    """A run of bones from the root outwards (each the parent of the next). `omega` (radians per frame) and `zeta`
    per bone: lower omega = softer and later; `gain` scales the turn towards the spring, `cap` (degrees) limits it."""

    def __init__(self, bones, omega, zeta, gain, cap):
        self.bones = list(bones)
        n = len(self.bones)
        self.omega = _per_bone(omega, n)
        self.zeta = _per_bone(zeta, n)
        self.gain = _per_bone(gain, n)
        self.cap = _per_bone(cap, n)


def _per_bone(v, n):
    return list(v) if isinstance(v, (list, tuple)) else [v] * n


def tail_point(poser, pose, name):
    """The posed tail of a bone in armature space."""
    return pose[name] @ (poser.rest[name].inverted() @ poser.tail[name])


def spring_track(targets, omega, zeta, loop, substeps=4):
    """Positions of a damped spring chasing `targets` (one Vector per frame). Loops: steady state over three
    cycles (the targets repeat); one-shots: starts on the target and with its velocity."""
    n = len(targets)
    dt = 1.0 / substeps

    def target(fi):
        """Linear interpolation of the targets at a fractional frame."""
        if loop:
            fi %= n
            i = int(fi)
            return targets[i].lerp(targets[(i + 1) % n], fi - i)
        fi = min(max(fi, 0.0), n - 1)
        i = min(int(fi), n - 2)
        return targets[i].lerp(targets[i + 1], fi - i)

    x = targets[0].copy()
    v = (targets[1] - targets[0]) if n > 1 else Vector()
    if loop:
        v = (targets[1 % n] - targets[-1]) * 0.5
    cycles = 3 if loop else 1
    out = [None] * n
    total = n * cycles if loop else n
    for f in range(total):
        if f >= total - n:
            out[f % n] = x.copy()
        for s in range(substeps):
            tgt = target(f + s * dt)
            a = (tgt - x) * (omega * omega) - v * (2.0 * zeta * omega)
            v = v + a * dt
            x = x + v * dt
    return out


def limited_turn(src, dst, gain, cap_deg):
    """The rotation turning direction `src` towards `dst`, scaled by `gain` and capped at `cap_deg`."""
    src, dst = src.normalized(), dst.normalized()
    axis = src.cross(dst)
    if axis.length < 1e-9:
        return Matrix.Identity(3)
    ang = math.atan2(axis.length, src.dot(dst)) * gain
    ang = max(-math.radians(cap_deg), min(math.radians(cap_deg), ang))
    return Matrix.Rotation(ang, 3, axis.normalized())


def apply(poser, poses, chains, loop, settle=0, weights=None):
    """poses: one armature-space pose dict per frame (a loop without its repeated last frame). Returns new pose dicts
    with every chain following through. `weights` (one per frame, 0..1) scales the extra motion per frame."""
    n = len(poses)
    tracks = {}
    for ch in chains:
        for i, b in enumerate(ch.bones):
            tracks[b] = spring_track([tail_point(poser, p, b) for p in poses], ch.omega[i], ch.zeta[i], loop)
    out = []
    for f, pose in enumerate(poses):
        w = 1.0 if weights is None else weights[f]
        if settle and not loop:
            # Blend the extra motion out over the last `settle` frames (smoothstep), so a clip that must end on its
            # authored pose does.
            u = min(1.0, max(0.0, (n - 1 - f) / float(settle)))
            w *= u * u * (3 - 2 * u)
        out.append(_follow_pose(poser, pose, chains, tracks, f, w))
    return out


def _follow_pose(poser, pose, chains, tracks, f, w):
    new = dict(pose)
    moved = {}
    chain_bones = {b: (ch, i) for ch in chains for i, b in enumerate(ch.bones)}
    for name in poser.order:
        parent = poser.parent[name]
        if parent in moved:
            # Carried by a moved parent (the next chain bone, or the jaw on the head).
            new[name] = moved[parent] @ pose[name]
        if name in chain_bones and w > 0.0:
            ch, i = chain_bones[name]
            m = new[name]
            head = m.translation.copy()
            tail = tail_point(poser, {name: m}, name)
            turn = limited_turn(tail - head, tracks[name][f] - head, ch.gain[i] * w, ch.cap[i])
            rot = turn.to_4x4()
            turned = Matrix.Translation(head) @ rot @ Matrix.Translation(-head) @ m
            new[name] = turned
        if new[name] is not pose[name]:
            moved[name] = new[name] @ pose[name].inverted()
    return new


def wrap_clip(fn, chains, settle=0, weights=None):
    """Wraps a clip function (poser -> frames, spec(t, f), loop) so its keyed poses follow through. The returned spec
    gives absolute matrices for every bone (Poser.solve and common.lod_action accept them)."""
    def wrapped(poser):
        frames, spec, loop = fn(poser)
        count = frames if loop else frames + 1
        poses = [poser.solve(spec((f % frames) / frames if loop else f / frames, f)) for f in range(count)]
        w = weights(count) if callable(weights) else weights
        done = apply(poser, poses, chains, loop, settle=settle, weights=w)

        def new_spec(t, f):
            pose = done[f % frames] if loop else done[min(f, frames)]
            return {name: {"abs": m} for name, m in pose.items()}
        return frames, new_spec, loop
    return wrapped


def follow_frames(poser, poses, chains, settle=0):
    """One-shot helper for attack clips keyed frame by frame (build_attacks36): poses for frames 0..n."""
    return apply(poser, poses, chains, False, settle=settle)
