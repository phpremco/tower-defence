using Game.Levels;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Depth in front of the painted sky of a space shot: a shell of near stars that stand still in the world (so they
    /// slide against the sky as the camera moves: parallax) and twinkle, and a cloud of faint dust motes round the
    /// camera that drift past it and fade in and out with distance, so even a slow move reads as motion through space.
    /// The dust wraps round the camera from its own clock alone; fewer of both on the low profile.
    /// </summary>
    public sealed class SpaceDepth
    {
        private const int Stars = 520;
        private const int LowStars = 220;
        private const int Motes = 110;
        private const int LowMotes = 40;
        private const float StarNear = 70f;
        private const float StarFar = 320f;
        // A star's half size per metre of its distance: about a pixel or two at 1080p whatever its distance.
        private const float StarAngle = 0.0011f;
        private const float StarTwinkle = 0.45f;
        private const float DustBox = 14f;
        private const float DustFadeNear = 0.8f;
        private const float DustFadeFar = 6.5f;
        private const float DustSizeMin = 0.008f;
        private const float DustSizeMax = 0.03f;
        private static readonly Color[] StarTints =
        {
            new Color(0.75f, 0.85f, 1f), new Color(1f, 1f, 1f), new Color(1f, 0.9f, 0.7f), new Color(0.65f, 0.75f, 1f), new Color(1f, 0.75f, 0.6f),
        };

        private static readonly Color DustColour = new Color(1.1f, 1.15f, 1.3f, 0.8f);

        private readonly SpriteBatch _stars;
        private readonly SpriteBatch _dust;
        private readonly Vector3[] _moteSeeds;
        private readonly Vector3[] _moteDrift;
        private readonly float[] _moteSizes;

        /// <param name="focus">Where the shot looks: the near stars surround it.</param>
        public SpaceDepth(Transform root, Vector3 focus, bool low)
        {
            Texture2D dot = SpaceKit.Sprite(CutsceneSprite.GlowSoft, SpaceKit.SoftDotTexture);
            int stars = low ? LowStars : Stars;
            _stars = new SpriteBatch(root, "NearStars", stars, dot, true, Color.white, StarTwinkle);
            _stars.Begin();
            for (int i = 0; i < stars; i++)
            {
                Vector3 direction = new Vector3(Signed(i, 3), Signed(i, 5), Signed(i, 7));
                if (direction.sqrMagnitude < 0.01f)
                {
                    direction = Vector3.forward;
                }

                float distance = Mathf.Lerp(StarNear, StarFar, CutsceneMath.Hash(i, 11));
                float brightness = Mathf.Lerp(0.5f, 2.6f, Mathf.Pow(CutsceneMath.Hash(i, 13), 3f));
                Color tint = StarTints[i % StarTints.Length] * brightness;
                tint.a = 1f;
                float size = distance * StarAngle * Mathf.Lerp(0.7f, 1.6f, CutsceneMath.Hash(i, 17));
                _stars.Add(focus + direction.normalized * distance, size, tint, 0f, CutsceneMath.Hash(i, 19));
            }

            _stars.End(0f);

            int motes = low ? LowMotes : Motes;
            _dust = new SpriteBatch(root, "SpaceDust", motes, dot, true, Color.white);
            _moteSeeds = new Vector3[motes];
            _moteDrift = new Vector3[motes];
            _moteSizes = new float[motes];
            for (int i = 0; i < motes; i++)
            {
                _moteSeeds[i] = new Vector3(CutsceneMath.Hash(i, 23), CutsceneMath.Hash(i, 29), CutsceneMath.Hash(i, 31)) * DustBox;
                _moteDrift[i] = new Vector3(Signed(i, 37), Signed(i, 41) * 0.5f, Signed(i, 43)) * 0.18f;
                _moteSizes[i] = Mathf.Lerp(DustSizeMin, DustSizeMax, CutsceneMath.Hash(i, 47));
            }
        }

        /// <summary>The motes drift by the shot clock and wrap round the camera; the near stars only twinkle.</summary>
        public void Pose(float time, Transform view)
        {
            _stars.End(time);
            Vector3 eye = view.position;
            Vector3 forward = view.forward;
            _dust.Begin();
            for (int i = 0; i < _moteSeeds.Length; i++)
            {
                Vector3 p = _moteSeeds[i] + _moteDrift[i] * time - eye;
                p = new Vector3(Wrap(p.x), Wrap(p.y), Wrap(p.z));
                float distance = p.magnitude;
                // Only motes in front of the lens, faded in from the near plane and out before the box edge.
                float ahead = Vector3.Dot(p, forward);
                if (ahead <= 0.2f)
                {
                    continue;
                }

                float fade = Mathf.Clamp01((distance - DustFadeNear) / DustFadeNear) * Mathf.Clamp01((DustFadeFar - distance) / (DustFadeFar * 0.4f));
                if (fade <= 0f)
                {
                    continue;
                }

                Color colour = DustColour * fade;
                colour.a = DustColour.a * fade;
                _dust.Add(eye + p, _moteSizes[i], colour);
            }

            _dust.End(time);
        }

        private static float Wrap(float value) => Mathf.Repeat(value + DustBox * 0.5f, DustBox) - DustBox * 0.5f;

        private static float Signed(int index, int salt) => CutsceneMath.Hash(index, salt) * 2f - 1f;
    }
}
