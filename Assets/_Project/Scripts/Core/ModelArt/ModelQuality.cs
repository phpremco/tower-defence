namespace Game.Core
{
    /// <summary>
    /// The model detail of the active quality profile, registered in <see cref="Services"/> at boot so views outside
    /// the platform layer can read it. Low: the 512 maps and one detail normal. Without a registration a view uses High.
    /// </summary>
    public sealed class ModelQuality
    {
        public ModelQuality(bool lowDetail) => LowDetail = lowDetail;

        public bool LowDetail { get; }

        /// <summary>The registered quality, or High when nothing registered one (tests, editor previews).</summary>
        public static bool IsLow => Services.TryGet(out ModelQuality quality) && quality.LowDetail;
    }
}
