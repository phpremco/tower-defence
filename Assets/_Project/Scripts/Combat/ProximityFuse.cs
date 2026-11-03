using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// When and where a flak shell bursts: as soon as it is within the fuse distance of its target, or once this frame's
    /// step would carry it onto the target anyway. Pure, so the rule is tested without a scene.
    /// </summary>
    public static class ProximityFuse
    {
        public static bool ShouldBurst(Vector3 shell, Vector3 target, float fuse, float step)
        {
            float reach = fuse > step ? fuse : step;
            return (target - shell).sqrMagnitude <= reach * reach;
        }

        /// <summary>
        /// Where the burst happens: the fuse distance short of the target along the shell's line. A long frame step can
        /// leave the shell further out than the splash reaches, so bursting where the shell is would miss the flyer.
        /// </summary>
        public static Vector3 BurstPoint(Vector3 shell, Vector3 target, float fuse)
        {
            Vector3 toTarget = target - shell;
            float distance = toTarget.magnitude;
            return distance <= fuse ? shell : shell + toTarget / distance * (distance - fuse);
        }
    }
}
