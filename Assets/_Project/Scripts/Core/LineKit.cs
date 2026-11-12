using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// The look of code-built lines and trails (boss and hero beams, bolts, sword, thruster and shot trails): one additive
    /// material from the Blender particle atlas wearing the beam profile (<see cref="SpriteKit.Beam"/>), a white-hot core
    /// inside a soft glowing edge, coloured by the renderer's own colours. It replaces the URP default particle material,
    /// which drew lines as flat strips in this project and is null in a player build (round 50).
    /// Round 51: no line may start or end square. Every line and trail tapers from nothing at its source and back to nothing at
    /// its far end (a width curve), its profile fades out along its length near both ends, and lines are drawn through enough
    /// points for the taper to show (<see cref="Straight"/>, <see cref="Curve"/>), with round caps and corners. The soft
    /// glow for a beam's source and impact is <see cref="Flare"/>.
    /// </summary>
    public static class LineKit
    {
        // HDR strengths of the core and the glow, so the bloom catches the core and the edge stays soft.
        private const float CoreStrength = 2.5f;
        private const float GlowStrength = 1.2f;
        // The flare is a round sprite seen face on, so it is lit a little lower than the beam it sits on.
        private const float FlareCoreStrength = 1.6f;
        private const float FlareGlowStrength = 0.9f;
        private static readonly int CoreColorId = Shader.PropertyToID("_CoreColor");
        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");

        /// <summary>Vertices that round the ends of a line that has visible width.</summary>
        public const int CapVertices = 6;

        /// <summary>
        /// No round corners: a kit line bends through many short segments, and the corner fans of a wide strip stick out of its
        /// edge as teeth where it bends (round 51 line lab).
        /// </summary>
        public const int CornerVertices = 0;

        /// <summary>
        /// Points of a line drawn by <see cref="Straight"/> or <see cref="Curve"/>: a line renderer samples its width only at
        /// its points, so a two-point line could never taper. A few of them lie inside each taper.
        /// </summary>
        public const int LinePoints = 32;

        // Points laid inside each taper, so its rounded growth shows without segments shorter than a fraction of the width.
        private const int TaperPoints = 6;

        /// <summary>Width at both ends, as a share of the full width, of a beam whose ends sit under glows.</summary>
        public const float FlaredEndWidth = 0.5f;

        /// <summary>How far a line takes to grow from nothing to its full width at its source, in multiples of its width.</summary>
        public const float SourceTaperWidths = 1.2f;

        /// <summary>How far it takes to thin back to nothing at its far end, in multiples of its width.</summary>
        public const float EndTaperWidths = 1.5f;

        // Share of a line the two tapers may take together at most, so a short bolt still has a full-width middle.
        private const float MaxTaperShare = 0.7f;
        // Share of a trail's length over which it grows from its head and thins to its tail.
        private const float TrailHeadShare = 0.12f;
        private const float TrailTailShare = 0.45f;
        private const float DefaultLineHeadShare = 0.08f;
        private const float DefaultLineTailShare = 0.12f;
        private const float MinShare = 0.001f;
        private const float EaseSlope = 2f;
        private const float FlareCoreWhiteness = 0.5f;

        private static Material _beam;
        private static Material _beamLow;
        private static Material _flaredBeam;
        private static Material _flaredBeamLow;
        private static Material _flare;
        private static Material _flareLow;
        // One scratch curve reused for every line's ends: a renderer copies the curve when it is assigned.
        private static readonly AnimationCurve Scratch = EndsCurve(DefaultLineHeadShare, DefaultLineTailShare);
        private static readonly Keyframe[] ScratchKeys = new Keyframe[4];

        /// <summary>The shared beam material of the active quality; the default material only without registered art.</summary>
        public static Material Beam() => BeamMaterial(ref _beam, ref _beamLow, SpriteKit.Beam(), "Lines_Beam");

        /// <summary>The beam material for a line whose ends sit under glows: the profile at full strength to its ends.</summary>
        public static Material FlaredBeam() => BeamMaterial(ref _flaredBeam, ref _flaredBeamLow, SpriteKit.FlaredBeam(), "Lines_FlaredBeam");

        private static Material BeamMaterial(ref Material high, ref Material lowMaterial, Texture2D profile, string name)
        {
            if (MarkerArt.Active == null)
            {
                return ParticleKit.FallbackMaterial;
            }

            bool low = ModelQuality.IsLow;
            ref Material cached = ref low ? ref lowMaterial : ref high;
            if (cached == null)
            {
                cached = ParticleKit.AdditiveSprite(profile, Color.white * CoreStrength);
                cached.SetColor(GlowColorId, Color.white * GlowStrength);
                cached.name = low ? name + "_Low" : name;
            }

            return cached;
        }

        /// <summary>
        /// The shared material of a beam's soft source and impact glow (<see cref="SpriteKit.Glow"/>): a white-hot point inside a
        /// round glow that fades to nothing before the card's edge. Tinted per card with <see cref="SetFlareColour"/>.
        /// </summary>
        public static Material Flare()
        {
            if (MarkerArt.Active == null)
            {
                return ParticleKit.FallbackMaterial;
            }

            bool low = ModelQuality.IsLow;
            ref Material cached = ref low ? ref _flareLow : ref _flare;
            if (cached == null)
            {
                cached = ParticleKit.AdditiveSprite(SpriteKit.Glow(), Color.white * FlareCoreStrength);
                cached.SetColor(GlowColorId, Color.white * FlareGlowStrength);
                cached.name = low ? "Lines_Flare_Low" : "Lines_Flare";
            }

            return cached;
        }

        /// <summary>Lights a flare card in <paramref name="colour"/> at <paramref name="strength"/> through its property block.</summary>
        public static void SetFlareColour(MaterialPropertyBlock block, Color colour, float strength)
        {
            Color core = Color.Lerp(colour, Color.white, FlareCoreWhiteness);
            block.SetColor(CoreColorId, core * (FlareCoreStrength * strength));
            block.SetColor(GlowColorId, colour * (FlareGlowStrength * strength));
        }

        /// <summary>Dresses a line in the beam, its profile stretched once along the whole line, its ends tapered and rounded.</summary>
        public static LineRenderer UseBeam(LineRenderer line)
        {
            line.sharedMaterial = Beam();
            line.textureMode = LineTextureMode.Stretch;
            return ShapeEnds(line);
        }

        /// <summary>
        /// Dresses a line whose source and far end sit under soft glows (<see cref="Flare"/>): the profile runs at full strength
        /// to its ends, and the caller lays it with <see cref="FlaredEndWidth"/>, so the beam grows straight out of the glow
        /// instead of fading in a glow's width away from it (round 51: a dark neck between the glow and the beam).
        /// </summary>
        public static LineRenderer UseFlaredBeam(LineRenderer line)
        {
            UseBeam(line);
            line.sharedMaterial = FlaredBeam();
            return line;
        }

        /// <summary>Tapers and rounds the ends of any line, also one that wears a painted material of its own.</summary>
        public static LineRenderer ShapeEnds(LineRenderer line)
        {
            line.numCapVertices = CapVertices;
            line.numCornerVertices = CornerVertices;
            line.widthCurve = EndsCurve(DefaultLineHeadShare, DefaultLineTailShare);
            return line;
        }

        /// <summary>Dresses a trail in the beam, its profile stretched once from head to tail, its ends tapered and rounded.</summary>
        public static TrailRenderer UseBeam(TrailRenderer trail)
        {
            trail.sharedMaterial = Beam();
            trail.textureMode = LineTextureMode.Stretch;
            return ShapeEnds(trail);
        }

        /// <summary>
        /// Tapers and rounds any trail, also one that wears a painted material: it grows from nothing at its head (the
        /// source) and thins to nothing at its tail. Its width is set with <see cref="TrailRenderer.widthMultiplier"/>.
        /// </summary>
        public static TrailRenderer ShapeEnds(TrailRenderer trail)
        {
            trail.numCapVertices = CapVertices;
            trail.numCornerVertices = CornerVertices;
            trail.widthCurve = EndsCurve(TrailHeadShare, TrailTailShare);
            return trail;
        }

        /// <summary>
        /// A width curve that grows from 0 to 1 over the first <paramref name="headShare"/> of a line and falls back to 0
        /// over the last <paramref name="tailShare"/>: eased (zero slope at full width), so the ends read as a rounded flare
        /// and not a cut cone.
        /// </summary>
        public static AnimationCurve EndsCurve(float headShare, float tailShare)
        {
            ClampShares(ref headShare, ref tailShare);
            var keys = new Keyframe[4];
            WriteKeys(keys, headShare, tailShare);
            return new AnimationCurve(keys);
        }

        // Hermite keys from 0 with slope EaseSlope / share to 1 with slope 0: the width follows 2s - s squared, a rounded dome.
        // A flared line starts and ends at endWidth instead of 0, rising along the same dome.
        private static void WriteKeys(Keyframe[] keys, float head, float tail, float endWidth = 0f)
        {
            float rise = 1f - endWidth;
            keys[0] = new Keyframe(0f, endWidth, 0f, EaseSlope * rise / head);
            keys[1] = new Keyframe(head, 1f, 0f, 0f);
            keys[2] = new Keyframe(1f - tail, 1f, 0f, 0f);
            keys[3] = new Keyframe(1f, endWidth, -EaseSlope * rise / tail, 0f);
        }

        /// <summary>
        /// A straight line from <paramref name="from"/> to <paramref name="to"/>, tapered at both ends: to nothing, or to
        /// <paramref name="endWidth"/> of its width for a beam whose ends sit under glows (<see cref="FlaredEndWidth"/>).
        /// </summary>
        public static void Straight(LineRenderer line, Vector3 from, Vector3 to, float endWidth = 0f) => Curve(line, from, (from + to) * 0.5f, to, endWidth);

        /// <summary>
        /// A smooth line from <paramref name="from"/> to <paramref name="to"/> that leaves <paramref name="from"/> towards
        /// <paramref name="control"/> (a quadratic Bézier, so a bend has no kink), tapered over a few of its widths at
        /// both ends whatever its length. No allocation: it reuses one scratch curve.
        /// </summary>
        public static void Curve(LineRenderer line, Vector3 from, Vector3 control, Vector3 to, float endWidth = 0f)
        {
            if (line.positionCount != LinePoints)
            {
                line.positionCount = LinePoints;
            }

            // The tapers are laid out from an estimate of the length (half the chord plus half the control polygon), so a few
            // points fall inside each taper and none closer together than a fraction of the width: segments much shorter than
            // the width fold at their joins and draw the beam as a zigzag (round 51, first try with cosine spacing).
            float estimate = 0.5f * (Vector3.Distance(from, to) + Vector3.Distance(from, control) + Vector3.Distance(control, to));
            float head, tail;
            Shares(line.widthMultiplier, endWidth, estimate, out head, out tail);
            float length = 0f;
            Vector3 previous = from;
            for (int i = 0; i < LinePoints; i++)
            {
                float t = PointAt(i, head, tail);
                float u = 1f - t;
                Vector3 point = u * u * from + 2f * u * t * control + t * t * to;
                line.SetPosition(i, point);
                length += Vector3.Distance(previous, point);
                previous = point;
            }

            FitEnds(line, length, endWidth);
        }

        // TaperPoints points up each taper (from its end inwards), the rest evenly between the tapers.
        private static float PointAt(int index, float head, float tail)
        {
            if (index <= TaperPoints)
            {
                return head * index / TaperPoints;
            }

            int last = LinePoints - 1;
            if (index >= last - TaperPoints)
            {
                return 1f - tail * (last - index) / TaperPoints;
            }

            int middle = last - 2 * TaperPoints;
            return Mathf.Lerp(head, 1f - tail, (index - TaperPoints) / (float)middle);
        }

        /// <summary>
        /// Fits the line's tapers to a line of <paramref name="length"/> (for a caller that places its own points, like a
        /// jagged arc). No allocation: it rewrites one scratch curve and hands the line a copy.
        /// </summary>
        public static void FitEnds(LineRenderer line, float length, float endWidth = 0f)
        {
            float head, tail;
            Shares(line.widthMultiplier, endWidth, length, out head, out tail);
            WriteKeys(ScratchKeys, head, tail, endWidth);
            Scratch.keys = ScratchKeys;
            line.widthCurve = Scratch;
        }

        // A flared line has less to grow, so its tapers are as much shorter.
        private static void Shares(float width, float endWidth, float length, out float head, out float tail)
        {
            float rise = 1f - endWidth;
            head = length > 0f ? SourceTaperWidths * width * rise / length : DefaultLineHeadShare;
            tail = length > 0f ? EndTaperWidths * width * rise / length : DefaultLineTailShare;
            ClampShares(ref head, ref tail);
        }

        private static void ClampShares(ref float head, ref float tail)
        {
            float total = head + tail;
            if (total > MaxTaperShare)
            {
                head *= MaxTaperShare / total;
                tail *= MaxTaperShare / total;
            }

            head = Mathf.Max(head, MinShare);
            tail = Mathf.Max(tail, MinShare);
        }
    }
}
