using System;
using Game.Hero;
using Game.Levels;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Hands out speaker portraits: the drawn sprite when the speaker has one, otherwise the rendered 3D bust. The only
    /// place that asks <see cref="ModelIconRenderer"/> for portraits; pictures are cached per speaker and framing.
    /// </summary>
    public sealed class PortraitProvider
    {
        private readonly HeroDefinition _hero;

        /// <param name="hero">The commander whose figure stands in for the hero portrait; may be null.</param>
        public PortraitProvider(HeroDefinition hero)
        {
            _hero = hero;
        }

        /// <summary>Calls <paramref name="done"/> with the portrait, at once when it is ready, otherwise a frame or two later.</summary>
        public void Get(SpeakerDefinition speaker, Action<Sprite> done)
        {
            if (speaker == null)
            {
                return;
            }

            if (speaker.PortraitOverride != null)
            {
                done(speaker.PortraitOverride);
                return;
            }

            IconFraming framing = IconFraming.Bust(speaker.RoleColour);
            // The hero's look follows the definition, so a different commander gets a different picture.
            object key = (speaker, framing.Size, _hero);
            HeroDefinition hero = _hero;
            ModelIconRenderer.Request(key, stage => PortraitModels.Build(stage, speaker.Portrait, speaker.RoleColour, hero), done, framing);
        }
    }
}
