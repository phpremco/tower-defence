using Game.Levels;
using Game.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 59 (Remco chose the salvo): the mothership launches its cargo capsules first and Spencer's last, his launch keeps
    /// round 44's sequence, each capsule sounds its launch once (the cargo ones quieter and swelling in), every capsule comes down
    /// behind the planet's rim on its own way, and its glows fade out at the end of its fall.
    /// </summary>
    public sealed class OpeningSalvoTests
    {
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        // Round 44's launch: one capsule released 1.2 s into a 9.5 s shot.
        private const float SingleRelease = 1.2f;
        private const float SingleLaunchSeconds = 9.5f;
        // Remco: the cutscene may grow by about two seconds.
        private const float MaxGrowth = 2.05f;
        private const float Tolerance = 0.001f;
        // A cargo cue never lands this close to one of Spencer's (no doubled hit).
        private const float ClearOfPathfinder = 0.15f;
        // Two landing sites this far apart (as an angle at the planet's centre) read as different regions.
        private const float MinSpreadDegrees = 12f;

        private static readonly CutsceneSound[] PerCapsule =
        {
            CutsceneSound.ClampsRelease, CutsceneSound.ColdGasPuff, CutsceneSound.BoosterIgnition, CutsceneSound.StageSeparation,
        };

        private CutsceneDefinition _cutscene;
        private CutsceneCast _cast;
        private CutsceneShot _launch;

        [SetUp]
        public void SetUp()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            Assert.IsNotNull(catalog, "run Tools/Generate/Scenes and Starter Data");
            _cutscene = catalog.Story.OpeningCutscene;
            _cast = _cutscene.Cast;
            bool found = false;
            foreach (CutsceneShot shot in _cutscene.Shots)
            {
                if (shot.Action == CutsceneAction.CapsulesFall)
                {
                    _launch = shot;
                    found = true;
                }
            }

            Assert.IsTrue(found, "the opening has no launch shot");
        }

        [Test]
        public void TheSalvo_LaunchesCargoFirst_AndSpencerLast()
        {
            Assert.Greater(_cast.CapsuleCount, 2, "a salvo needs several capsules");
            Assert.AreEqual(_cast.CapsuleCount - 1, _cast.CargoFlights.Length, "one cargo flight per capsule before Spencer's");
            float pathfinder = CapsuleDrop.PathfinderRelease(_cast.CapsuleCount, _cast.ReleaseGap);
            for (int i = 0; i < _cast.CapsuleCount - 1; i++)
            {
                Assert.Less(CapsuleDrop.ReleaseAt(i, _cast.ReleaseGap), pathfinder, $"cargo capsule {i} leaves after Spencer's");
                Assert.Less(_cast.CargoFlights[i].Scale, _cast.PathfinderFlight.Scale, $"cargo capsule {i} is not smaller than Spencer's");
                Assert.IsTrue(_cast.CargoFlights[i].HasMarkings, $"cargo capsule {i} has no marker light");
            }

            Assert.AreEqual(1f, _cast.PathfinderFlight.Scale, Tolerance, "Spencer's capsule keeps its size");
            Assert.IsFalse(_cast.PathfinderFlight.HasMarkings, "Spencer's capsule keeps its own look");
        }

        [Test]
        public void SpencersLaunch_KeepsRound44sSequence_AndTheShotGrowsByTheSalvoOnly()
        {
            float release = CapsuleDrop.PathfinderRelease(_cast.CapsuleCount, _cast.ReleaseGap);
            float growth = release - SingleRelease;
            Assert.AreEqual(SingleLaunchSeconds + growth, _launch.Seconds, Tolerance, "the launch shot must grow by the salvo alone");
            Assert.LessOrEqual(growth, MaxGrowth, "the salvo makes the cutscene more than about two seconds longer");
            Assert.AreEqual(release, Loudest(CutsceneSound.ClampsRelease), Tolerance, "Spencer's clamps do not let go at his release");
            Assert.AreEqual(release + CapsuleDrop.IgnitionAge, Loudest(CutsceneSound.BoosterIgnition), Tolerance, "his ignition moved");
            Assert.AreEqual(release + CapsuleDrop.SeparationAge, Loudest(CutsceneSound.StageSeparation), Tolerance, "his separation moved");
            Assert.AreEqual(release + CapsuleDrop.SeparationAge, Only(CutsceneSound.BoosterFade).At, Tolerance, "only his spent booster fades away");
        }

        [Test]
        public void EveryCapsule_SoundsItsLaunchOnce_TheCargoQuieterAndSwellingIn()
        {
            float pathfinder = CapsuleDrop.PathfinderRelease(_cast.CapsuleCount, _cast.ReleaseGap);
            foreach (CutsceneSound sound in PerCapsule)
            {
                float offset = OffsetOf(sound);
                float loud = Only(sound, pathfinder + offset).Strength;
                for (int i = 0; i < _cast.CapsuleCount - 1; i++)
                {
                    CutsceneCue cue = Only(sound, CapsuleDrop.ReleaseAt(i, _cast.ReleaseGap) + offset);
                    Assert.Less(cue.Strength, loud * 0.5f, $"cargo capsule {i}'s {sound} is not quieter than Spencer's");
                    Assert.Greater(cue.FadeIn, 0f, $"cargo capsule {i}'s {sound} starts hard");
                }
            }
        }

        [Test]
        public void NoCargoCue_LandsOnOneOfSpencers()
        {
            foreach (CutsceneCue cargo in _launch.Cues)
            {
                if (cargo.Kind != CueKind.Sound || cargo.FadeIn <= 0f)
                {
                    continue;
                }

                foreach (CutsceneCue cue in _launch.Cues)
                {
                    if (cue.Kind == CueKind.Sound && cue.FadeIn <= 0f && cue.Sound != CutsceneSound.BayDoors)
                    {
                        Assert.Greater(Mathf.Abs(cue.At - cargo.At), ClearOfPathfinder, $"a cargo {cargo.Sound} at {cargo.At} s doubles Spencer's {cue.Sound}");
                    }
                }
            }
        }

        [Test]
        public void EveryCapsule_ComesDownBehindTheRim_InItsOwnRegion()
        {
            var flights = new CapsuleFlight[_cast.CapsuleCount];
            for (int i = 0; i < flights.Length - 1; i++)
            {
                flights[i] = _cast.CargoFlights[i];
            }

            flights[flights.Length - 1] = _cast.PathfinderFlight;
            for (int i = 0; i < flights.Length; i++)
            {
                Vector3 site = CapsuleDrop.LandingPoint(flights[i].Aim, OrbitSet.PlanetCentre, OrbitSet.PlanetRadius);
                foreach (CameraKey key in _launch.Camera)
                {
                    Assert.IsTrue(BehindTheRim(site, key.Position), $"capsule {i} lands on the face the lens sees (key at {key.At})");
                }
            }

            // The supplies go to other regions than the landing site and each other.
            for (int i = 1; i < flights.Length - 1; i++)
            {
                for (int j = 0; j < flights.Length; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }

                    Vector3 a = CapsuleDrop.LandingPoint(flights[i].Aim, OrbitSet.PlanetCentre, OrbitSet.PlanetRadius) - OrbitSet.PlanetCentre;
                    Vector3 b = CapsuleDrop.LandingPoint(flights[j].Aim, OrbitSet.PlanetCentre, OrbitSet.PlanetRadius) - OrbitSet.PlanetCentre;
                    Assert.Greater(Vector3.Angle(a, b), MinSpreadDegrees, $"capsules {i} and {j} come down in the same region");
                }
            }
        }

        [Test]
        public void TheGlows_FadeOutOverTheEndOfTheFall()
        {
            Assert.AreEqual(1f, CapsuleDrop.EndFade(0f), Tolerance, "a fresh capsule glows fully");
            Assert.AreEqual(1f, CapsuleDrop.EndFade(CapsuleDrop.Lifetime * 0.5f), Tolerance, "the glow fades too early");
            Assert.AreEqual(0f, CapsuleDrop.EndFade(CapsuleDrop.Lifetime), Tolerance, "the glow is still lit when the capsule goes");
            float last = 1f;
            for (float age = CapsuleDrop.Lifetime * 0.5f; age <= CapsuleDrop.Lifetime; age += 0.02f)
            {
                float fade = CapsuleDrop.EndFade(age);
                Assert.LessOrEqual(fade, last + Tolerance, "the fade brightens again");
                Assert.Less(last - fade, 0.06f, "the fade drops in a step");
                last = fade;
            }
        }

        [Test]
        public void TheGlows_ComeUpAfterTheCapsuleIsOutOfTheBay()
        {
            Assert.AreEqual(0f, CapsuleDrop.GlowIn(0f), Tolerance, "a glow is lit as the capsule leaves the bay");
            Assert.AreEqual(1f, CapsuleDrop.GlowIn(CapsuleDrop.IgnitionAge), Tolerance, "the glow is not up by the ignition");
            float last = 0f;
            for (float age = 0f; age <= CapsuleDrop.IgnitionAge; age += 0.02f)
            {
                float glow = CapsuleDrop.GlowIn(age);
                Assert.GreaterOrEqual(glow, last - Tolerance, "the glow dims while coming up");
                Assert.Less(glow - last, 0.08f, "the glow comes up in a step");
                last = glow;
            }
        }

        [Test]
        public void SmoothMax_IsTheLargerAwayFromTheCrossing_AndHasNoCorner()
        {
            const float blend = 1.5f;
            Assert.AreEqual(10f, CutsceneMath.SmoothMax(10f, 5f, blend), Tolerance, "well above the floor the path is left as it is");
            Assert.AreEqual(5f, CutsceneMath.SmoothMax(2f, 5f, blend), Tolerance, "well under the floor the path lies on it");
            Assert.AreEqual(Mathf.Max(3f, 4f), CutsceneMath.SmoothMax(3f, 4f, 0f), Tolerance, "no blend is the plain max");
            float step = 0.01f;
            float lastSlope = 0f;
            for (float a = 2f; a <= 8f; a += step)
            {
                float value = CutsceneMath.SmoothMax(a, 5f, blend);
                Assert.GreaterOrEqual(value, Mathf.Max(a, 5f) - Tolerance, "the smooth max dips under the larger");
                float slope = (CutsceneMath.SmoothMax(a + step, 5f, blend) - value) / step;
                Assert.Less(Mathf.Abs(slope - lastSlope), 0.05f, $"the slope breaks at {a}");
                lastSlope = slope;
            }
        }

        [Test]
        public void ASpentBooster_FallsBehindItsCapsule_AndStaysInOrbit()
        {
            Assert.AreEqual(0f, CapsuleStage.Lagged(0f), Tolerance, "it moves before the separation");
            Assert.AreEqual(0.1f, CapsuleStage.Lagged(0.1f), 0.003f, "at first it keeps the capsule's speed");
            Assert.Less(CapsuleStage.Lagged(30f), 2.01f, "it keeps falling with the capsule");
            Assert.Greater(CapsuleStage.Lagged(2f), CapsuleStage.Lagged(1f), "it stops drifting");
        }

        [Test]
        public void TheLandingPoint_LiesJustUnderTheSurface_InTheAimsDirection()
        {
            Vector3 site = CapsuleDrop.LandingPoint(new Vector2(0.5f, 0.3f), OrbitSet.PlanetCentre, OrbitSet.PlanetRadius);
            Assert.Less(Vector3.Distance(site, OrbitSet.PlanetCentre), OrbitSet.PlanetRadius, "the shield stops over the ground");
            Assert.Greater(Vector3.Distance(site, OrbitSet.PlanetCentre), OrbitSet.PlanetRadius * 0.99f, "the site is deep under the ground");
            Vector3 up = (site - OrbitSet.PlanetCentre).normalized;
            Assert.Greater(up.x, 0f, "aiming right lands right");
            Assert.Greater(up.z, 0f, "aiming away lands away from the lens");
        }

        // A point on the planet is out of sight when the lens is below its horizon plane.
        private static bool BehindTheRim(Vector3 site, Vector3 eye)
        {
            Vector3 normal = (site - OrbitSet.PlanetCentre).normalized;
            return Vector3.Dot(normal, eye - site) < 0f;
        }

        private static float OffsetOf(CutsceneSound sound)
        {
            switch (sound)
            {
                case CutsceneSound.BoosterIgnition:
                    return CapsuleDrop.IgnitionAge;
                case CutsceneSound.StageSeparation:
                    return CapsuleDrop.SeparationAge;
                default:
                    return 0f;
            }
        }

        private float Loudest(CutsceneSound sound)
        {
            float at = -1f;
            float strength = -1f;
            foreach (CutsceneCue cue in _launch.Cues)
            {
                if (cue.Kind == CueKind.Sound && cue.Sound == sound && cue.Strength > strength)
                {
                    strength = cue.Strength;
                    at = cue.At;
                }
            }

            return at;
        }

        private CutsceneCue Only(CutsceneSound sound, float at = -1f)
        {
            CutsceneCue found = default;
            int count = 0;
            foreach (CutsceneCue cue in _launch.Cues)
            {
                if (cue.Kind == CueKind.Sound && cue.Sound == sound && (at < 0f || Mathf.Abs(cue.At - at) < Tolerance))
                {
                    found = cue;
                    count++;
                }
            }

            Assert.AreEqual(1, count, $"{sound}{(at >= 0f ? $" at {at:0.00} s" : string.Empty)} must sound exactly once");
            return found;
        }
    }
}
