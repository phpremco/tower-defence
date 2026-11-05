using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The maths of the hold-to-overview camera: the overview frames the whole field (the fully zoomed-out view around the
    /// field's centre), the camera eases between the play view and the overview with the zoom changing evenly in scale,
    /// and a point picked on the overview is kept on the field. Pure maths, so it is tested without a scene.
    /// </summary>
    public static class OverviewFraming
    {
        /// <summary>The zoom of the overview: one is the fitted view that shows the whole field.</summary>
        public const float Zoom = 1f;

        /// <summary>Seconds the camera takes to zoom out to the overview, and back.</summary>
        public const float BlendSeconds = 0.45f;

        /// <summary>The ground point the overview looks at: the field's centre.</summary>
        public static Vector3 Focus => Vector3.zero;

        /// <summary>
        /// The view a share <paramref name="t"/> (0 to 1, eased) of the way from the play view to the overview. The zoom
        /// is blended on a log scale, so zooming out from 3x feels as even as zooming out from 1.5x.
        /// </summary>
        public static void Blend(Vector3 playFocus, float playZoom, float t, out Vector3 focus, out float zoom)
        {
            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            focus = Vector3.Lerp(playFocus, Focus, eased);
            float from = Mathf.Log(Mathf.Max(0.01f, playZoom));
            float to = Mathf.Log(Zoom);
            zoom = Mathf.Exp(Mathf.Lerp(from, to, eased));
        }

        /// <summary>Where a ray from the camera meets the ground plane (y zero); false when it runs parallel or away.</summary>
        public static bool GroundPoint(Ray ray, out Vector3 point)
        {
            point = Vector3.zero;
            if (Mathf.Abs(ray.direction.y) < 0.0001f)
            {
                return false;
            }

            float distance = -ray.origin.y / ray.direction.y;
            if (distance < 0f)
            {
                return false;
            }

            point = ray.origin + ray.direction * distance;
            point.y = 0f;
            return true;
        }

        /// <summary>A picked point pulled inside the field, <paramref name="margin"/> units from its edges.</summary>
        public static Vector3 KeepOnField(Vector3 point, Vector2 halfField, float margin)
        {
            float x = Mathf.Clamp(point.x, -halfField.x + margin, halfField.x - margin);
            float z = Mathf.Clamp(point.z, -halfField.y + margin, halfField.y - margin);
            return new Vector3(x, 0f, z);
        }
    }
}
