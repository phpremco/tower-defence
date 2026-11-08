using Game.Hero;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    public sealed class HeroFlightTests
    {
        private const float Speed = 10f;
        private const float Takeoff = 0.4f;
        private const float Height = 2.5f;
        private const float Step = 0.02f;

        [Test]
        public void Flight_StaysOnTheSpotUntilLiftoff_ThenArrivesAfterDistanceOverSpeed()
        {
            var flight = new HeroFlight(Vector3.zero, new Vector3(20f, 0f, 0f), Speed, Takeoff, Height, airborne: false);

            Assert.IsFalse(flight.IsAirborne);
            Assert.AreEqual(Takeoff + 2f, flight.SecondsToTouchdown, 0.001f);
            flight.Tick(Takeoff * 0.5f);
            Assert.AreEqual(Vector3.zero, flight.Position, "still on the spot while the pack lifts him");

            float flown = Takeoff * 0.5f;
            while (!flight.IsDone && flown < 10f)
            {
                flight.Tick(Step);
                flown += Step;
            }

            Assert.AreEqual(Takeoff + 2f, flown, Step * 1.5f);
            Assert.AreEqual(20f, flight.Position.x, 0.001f);
            Assert.AreEqual(0f, flight.Position.y, 0.001f, "touches down on the ground");
        }

        [Test]
        public void Flight_PeaksInTheMiddle_AndShortHopsStayLow()
        {
            var flight = new HeroFlight(Vector3.zero, new Vector3(20f, 0f, 0f), Speed, 0f, Height, airborne: false);
            flight.Tick(1f);
            Assert.AreEqual(10f, flight.Position.x, 0.001f);
            Assert.AreEqual(Height, flight.Position.y, 0.001f);

            var hop = new HeroFlight(Vector3.zero, new Vector3(5f, 0f, 0f), Speed, 0f, Height, airborne: false);
            hop.Tick(0.25f);
            Assert.Less(hop.Position.y, Height * 0.75f, "a 5 m hop does not climb the full arc");
        }

        [Test]
        public void FlightStartedInTheAir_SkipsTheTakeoff()
        {
            var flight = new HeroFlight(new Vector3(0f, 1f, 0f), new Vector3(10f, 0f, 0f), Speed, Takeoff, Height, airborne: true);
            Assert.IsTrue(flight.IsAirborne);
            Assert.AreEqual(1f, flight.SecondsToTouchdown, 0.001f);
        }

        [Test]
        public void Definition_WalksTheLanesOfOneGate_AndFliesToAnother()
        {
            var hero = ScriptableObject.CreateInstance<HeroDefinition>();
            try
            {
                Assert.IsFalse(hero.FliesBetween(hero.WalkWithin * 0.5f));
                Assert.IsTrue(hero.FliesBetween(hero.WalkWithin * 4f));
                float far = 25f;
                Assert.Less(hero.SwitchSeconds(far), far / hero.WalkSpeed, "flying beats running across the field");
                Assert.AreEqual(hero.WalkWithin * 0.5f / hero.WalkSpeed, hero.SwitchSeconds(hero.WalkWithin * 0.5f), 0.0001f);

                var so = new SerializedObject(hero);
                so.FindProperty("_flightSpeed").floatValue = 0f;
                so.ApplyModifiedPropertiesWithoutUndo();
                Assert.IsFalse(hero.FliesBetween(far), "no jetpack: he walks everywhere");
            }
            finally
            {
                Object.DestroyImmediate(hero);
            }
        }
    }
}
