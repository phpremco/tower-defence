using UnityEngine;

namespace Game.Levels
{
    /// <summary>Easing curves for camera moves: each maps 0..1 onto 0..1 and never runs backwards.</summary>
    public static class Easing
    {
        public static float Apply(CameraEase ease, float t)
        {
            t = Mathf.Clamp01(t);
            switch (ease)
            {
                case CameraEase.In:
                    return t * t * t;
                case CameraEase.Out:
                    float inverse = 1f - t;
                    return 1f - inverse * inverse * inverse;
                case CameraEase.InOut:
                    return t * t * t * (t * (t * 6f - 15f) + 10f);
                default:
                    return t;
            }
        }
    }
}
