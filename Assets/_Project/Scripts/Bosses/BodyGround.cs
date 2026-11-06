using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// The ground a wide boss body stands on off the road: the landscape (plateaus, rim hills) and the climbable decor tops
    /// sampled under its whole footprint, not only under its pivot. The body rises as its front reaches a rise and tips
    /// with the difference between front and back, so a four-metre body never half sinks into a plateau or a boulder and
    /// never jumps when the pivot crosses a cell edge. Pure logic over the boss field.
    /// </summary>
    public readonly struct BodyGround
    {
        // The footprint is sampled this far out (share of the half width) ahead, behind and to the sides.
        private const float Reach = 0.7f;

        // The body lifts by this share of the way from the ground under its pivot to the highest point under its feet.
        private const float FootLift = 0.5f;

        private BodyGround(float height, float slope)
        {
            Height = height;
            Slope = slope;
        }

        /// <summary>The height the body should stand at.</summary>
        public float Height { get; }

        /// <summary>Rise per metre from back to front, for the pitch.</summary>
        public float Slope { get; }

        public static BodyGround Under(BossField field, Vector3 at, Vector3 forward, float halfWidth)
        {
            float reach = Mathf.Max(0.1f, halfWidth * Reach);
            Vector3 ahead = forward * reach;
            Vector3 side = Vector3.Cross(Vector3.up, forward) * reach;
            float centre = Top(field, at);
            float front = Top(field, at + ahead);
            float back = Top(field, at - ahead);
            float highest = Mathf.Max(Mathf.Max(front, back), Mathf.Max(Top(field, at + side), Top(field, at - side)));
            float height = Mathf.Max(centre, Mathf.Lerp(centre, highest, FootLift));
            return new BodyGround(height, (front - back) / (2f * reach));
        }

        private static float Top(BossField field, Vector3 at) =>
            Mathf.Max(field.GroundAt(at), field.Climb.DecorTopAt(field.Grid.WorldToCell(at)));
    }
}
