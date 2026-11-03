using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// How <see cref="ModelIconRenderer"/> frames a model: picture size, zoom, where the model sits, the view angle
    /// and an optional coloured back light that draws the silhouette. Cards use <see cref="Default"/>; dialogue
    /// portraits use <see cref="Bust"/>.
    /// </summary>
    public readonly struct IconFraming
    {
        private const int CardSize = 160;
        private const float CardOrtho = 0.78f;
        private const float CardLift = -0.45f;
        private const float CardPitch = 32f;
        private const float CardYaw = -38f;

        private const int BustSize = 256;
        private const float BustOrtho = 0.34f;
        private const float BustPitch = 7f;
        // Models face +z and the camera looks along +z, so a portrait turns the model round, then a little aside.
        private const float BustYaw = 180f - 24f;
        private const float BustRimIntensity = 5.5f;

        public IconFraming(int size, float orthoSize, float lift, float pitch, float yaw, Color rim, float rimIntensity)
        {
            Size = size;
            OrthoSize = orthoSize;
            Lift = lift;
            Pitch = pitch;
            Yaw = yaw;
            Rim = rim;
            RimIntensity = rimIntensity;
        }

        public int Size { get; }
        public float OrthoSize { get; }

        /// <summary>Height of the model's origin in the picture: negative lowers the model.</summary>
        public float Lift { get; }

        public float Pitch { get; }

        /// <summary>Turn of the model away from the camera, so it shows a three-quarter view.</summary>
        public float Yaw { get; }

        public Color Rim { get; }

        /// <summary>Zero leaves out the back light.</summary>
        public float RimIntensity { get; }

        /// <summary>The tower and path-object cards: a small picture from above at a three-quarter angle.</summary>
        public static IconFraming Default => new IconFraming(CardSize, CardOrtho, CardLift, CardPitch, CardYaw, Color.clear, 0f);

        /// <summary>
        /// A head-and-shoulders portrait near eye level, lit from behind in the speaker's colour. The model is built
        /// with the middle of the bust at its origin.
        /// </summary>
        public static IconFraming Bust(Color rim) => new IconFraming(BustSize, BustOrtho, 0f, BustPitch, BustYaw, rim, BustRimIntensity);
    }
}
