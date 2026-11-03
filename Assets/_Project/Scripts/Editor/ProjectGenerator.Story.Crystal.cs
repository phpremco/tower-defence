using Game.Levels;

namespace Game.Editor
{
    /// <summary>
    /// Dialogues of Crystal Plains levels 1 to 6 (S4). The English follows the Dutch script in Docs/STORY.md; dialogues
    /// marked "proposed" are additions that are not in the script and wait for Remco's approval. Which moment starts each
    /// one is in the level specs (LevelSpecs.Crystal.cs).
    /// </summary>
    public static partial class ProjectGenerator
    {
        private static void CreateCrystalDialogues(SpeakerDefinition ora, SpeakerDefinition voss, SpeakerDefinition mercer)
        {
            // Proposed: the first stone creature.
            CreateDialogue("Dlg_L01_Stone",
                Say(ora, "Heavy contact. Dense mineral hide, slow-moving."),
                Say(voss, "Use the cannon. Heavy shells for heavy hides."));

            CreateDialogue("Dlg_L02_Start",
                Say(voss, "We rebuilt the laser module. It fires fast, but light. Perfect against swarms."));
            // Proposed.
            CreateDialogue("Dlg_L02_Swarm",
                Say(ora, "Swarm signature. Estimated count: several hundred."),
                Say(mercer, "Light them up."));

            CreateDialogue("Dlg_L03_Start",
                Say(voss, "I had anti-air built. Just to be safe. You never know."));
            CreateDialogue("Dlg_L03_Air",
                Say(ora, "Warning: air contacts."),
                Say(mercer, "They fly too?!"));
            CreateDialogue("Dlg_L03_Won",
                Say(mercer, "Doc, that 'just to be safe' just saved us. Thanks."),
                Say(voss, "Honestly, I was hoping I'd be wrong."));

            // Proposed.
            CreateDialogue("Dlg_L04_Start",
                Say(ora, "Mixed signatures. Ground swarms and air contacts."),
                Say(mercer, "Guns low, flak high. Got it."));
            // Proposed.
            CreateDialogue("Dlg_L04_Boss",
                Say(ora, "Large air contact. Smaller contacts detaching."));

            CreateDialogue("Dlg_L05_Fast",
                Say(ora, "Unknown signal. Speed: three times the average."),
                Say(mercer, "What was that?! I couldn't even track it."));

            CreateDialogue("Dlg_L06_Start",
                Say(voss, "The crystals never run dry. Never. This changes everything."),
                Say(voss, "For now: a mine with a serious blast, and a field that slows time around those fast little critters."));
            // Proposed.
            CreateDialogue("Dlg_L06_Mine",
                Say(ora, "Land Mine armed. Proximity trigger active."));
            // Proposed.
            CreateDialogue("Dlg_L06_Chrono",
                Say(ora, "Chrono Field stable. Local time dilated."),
                Say(mercer, "Slow-motion bugs. I'll take it."));
            // Proposed; fires on the first fast creature of the level (curator decision 9).
            CreateDialogue("Dlg_L06_Fast",
                Say(ora, "Fast contacts inbound. Recommend a Chrono Field on the main road."));
        }
    }
}
