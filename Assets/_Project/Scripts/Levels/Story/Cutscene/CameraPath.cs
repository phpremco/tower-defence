using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Samples a camera move: a Catmull-Rom curve through the key positions, so the camera glides through each key
    /// instead of turning sharply at it, with the look-at point and the field of view eased between keys. Keys must
    /// be in world space and sorted by time. Pure, so the move can be tested and sought.
    /// </summary>
    public static class CameraPath
    {
        public static CameraSample Sample(IReadOnlyList<CameraKey> keys, float progress)
        {
            int count = keys.Count;
            if (count == 0)
            {
                return new CameraSample(Vector3.zero, Vector3.forward, CameraKey.DefaultFieldOfView);
            }

            if (count == 1 || progress <= keys[0].At)
            {
                return At(keys[0]);
            }

            if (progress >= keys[count - 1].At)
            {
                return At(keys[count - 1]);
            }

            int segment = 0;
            while (segment < count - 2 && progress > keys[segment + 1].At)
            {
                segment++;
            }

            CameraKey from = keys[segment];
            CameraKey to = keys[segment + 1];
            float span = to.At - from.At;
            float linear = span > 0f ? (progress - from.At) / span : 1f;
            float t = Easing.Apply(to.Ease, linear);

            Vector3 before = keys[Mathf.Max(segment - 1, 0)].Position;
            Vector3 after = keys[Mathf.Min(segment + 2, count - 1)].Position;
            Vector3 position = CatmullRom(before, from.Position, to.Position, after, t);
            Vector3 lookAt = Vector3.Lerp(from.LookAt, to.LookAt, t);
            float fieldOfView = Mathf.Lerp(FieldOfView(from), FieldOfView(to), t);
            return new CameraSample(position, lookAt, fieldOfView);
        }

        private static CameraSample At(CameraKey key) => new CameraSample(key.Position, key.LookAt, FieldOfView(key));

        private static float FieldOfView(CameraKey key) => key.FieldOfView > 0f ? key.FieldOfView : CameraKey.DefaultFieldOfView;

        // Uniform Catmull-Rom: passes through p1 at t = 0 and p2 at t = 1, with tangents taken from the neighbours.
        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }
    }
}
