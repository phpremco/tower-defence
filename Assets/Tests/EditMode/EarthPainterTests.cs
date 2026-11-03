using Game.UI;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public sealed class EarthPainterTests
    {
        private const int Width = 256;

        [Test]
        public void Earth_IsDullWithFewLightsAndSmog()
        {
            EarthPainter.Painting painting = EarthPainter.Paint(Width);

            Color32[] albedo = painting.Maps.Albedo.GetPixels32();
            Color32[] lights = painting.Maps.Emission.GetPixels32();
            Color32[] smog = painting.Smog.GetPixels32();
            float saturation = 0f;
            int lit = 0;
            int hazy = 0;
            for (int i = 0; i < albedo.Length; i++)
            {
                Color.RGBToHSV(albedo[i], out _, out float s, out _);
                saturation += s;
                if (lights[i].r > 25)
                {
                    lit++;
                }

                if (smog[i].a > 20)
                {
                    hazy++;
                }
            }

            Assert.AreEqual(Width / 2, painting.Maps.Albedo.height, "an equirectangular map, twice as wide as high");
            Assert.Less(saturation / albedo.Length, 0.45f, "a grey-brown planet, no vivid colour");
            Assert.Greater(lit, 0, "a few city lights still burn");
            Assert.Less(lit, albedo.Length / 20, "but most of them have gone out");
            Assert.Greater(hazy, albedo.Length / 5, "smog covers a good part of it");
            Assert.AreSame(painting, EarthPainter.Paint(Width), "painted once, then shared");
        }
    }
}
