using System.Collections.Generic;
using Game.UI;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// The surprise ping's place on the HUD (round 88b): whatever the direction to the spawn point, the arrow and its word
    /// stay inside the safe area and off every panel, a direction into a panel slides the marker along that panel's free
    /// border, markers never share a spot, and a ring's word flips off a panel. The frames are the HUD's own at 16:9
    /// (1920x1080 and 1280x720 scale to the same canvas), with a safe-area inset, and the 4:3 frame of a batch editor;
    /// the panels are laid out from the frame the way the HUD anchors them.
    /// </summary>
    public sealed class MarkerLayoutTests
    {
        private const float ArrowBox = 79.2f;
        private const float Gap = 2f;
        private const float RingOffset = 54f;
        private const float Spacing = 8f;
        private const int Directions = 72;
        private static readonly Vector2 Label = new Vector2(132f, 40f);

        private static readonly Rect[] Frames =
        {
            new Rect(-960f, -540f, 1920f, 1080f),
            // 1280x720 with a 5% title-safe inset, in canvas units.
            new Rect(-864f, -486f, 1728f, 972f),
            // A notch on one side only: the frame is off-centre in the canvas but its own space is still its rect.
            new Rect(-900f, -540f, 1800f, 1080f),
            new Rect(-831f, -624f, 1663f, 1247f),
        };

        // The HUD's panels as the keep-out reports them: top strip, tower bar, radar, hero bar, money, countdown, status,
        // and a base panel bottom right.
        private static List<Rect> Hud(Rect frame)
        {
            return new List<Rect>
            {
                Rect.MinMaxRect(frame.xMin - 6f, frame.yMax - 78f, frame.xMax + 6f, frame.yMax + 6f),
                Rect.MinMaxRect(frame.xMin - 6f, frame.yMin - 6f, frame.xMax + 6f, frame.yMin + 172f),
                Rect.MinMaxRect(frame.xMax - 296f, frame.yMax - 360f, frame.xMax - 14f, frame.yMax - 78f),
                Rect.MinMaxRect(frame.xMin + 12f, -227f, frame.xMin + 260f, 227f),
                Rect.MinMaxRect(frame.xMin + 14f, frame.yMin + 166f, frame.xMin + 171f, frame.yMin + 222f),
                Rect.MinMaxRect(-103f, frame.yMax - 110f, 104f, frame.yMax - 54f),
                Rect.MinMaxRect(-220f, frame.yMin + 166f, 220f, frame.yMin + 222f),
                Rect.MinMaxRect(frame.xMax - 340f, frame.yMin + 172f, frame.xMax - 14f, frame.yMin + 420f),
            };
        }

        private static Vector2 Far(Rect frame, int step)
        {
            float angle = step * 2f * Mathf.PI / Directions;
            return frame.center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 4000f;
        }

        private static void AssertClear(Rect block, Rect frame, List<Rect> obstacles, string what)
        {
            Assert.GreaterOrEqual(block.xMin, frame.xMin - 0.02f, what + ": left of the safe area");
            Assert.LessOrEqual(block.xMax, frame.xMax + 0.02f, what + ": right of the safe area");
            Assert.GreaterOrEqual(block.yMin, frame.yMin - 0.02f, what + ": under the safe area");
            Assert.LessOrEqual(block.yMax, frame.yMax + 0.02f, what + ": over the safe area");
            for (int i = 0; i < obstacles.Count; i++)
            {
                Assert.IsFalse(block.Overlaps(obstacles[i]), what + ": on obstacle " + i + " " + obstacles[i] + " with " + block);
            }
        }

        private static Rect Around(Vector2 centre, Vector2 size) => new Rect(centre - size * 0.5f, size);

        [Test]
        public void EdgeMarkerIsClearOfEveryPanelInEveryDirection()
        {
            var layout = new MarkerLayout();
            foreach (Rect frame in Frames)
            {
                List<Rect> hud = Hud(frame);
                for (int step = 0; step < Directions; step++)
                {
                    string what = frame.size + " direction " + step;
                    Assert.IsTrue(layout.PlaceEdge(Far(frame, step), ArrowBox, Label, Gap, frame, hud, out EdgeMarkerPlacement placed), what + ": no place");
                    AssertClear(placed.Block, frame, hud, what);
                    // The block really holds both parts.
                    Rect arrow = Around(placed.Arrow, new Vector2(ArrowBox, ArrowBox));
                    Rect label = Around(placed.Label, Label);
                    Assert.IsFalse(arrow.Overlaps(label), what + ": the arrow is on its word");
                    Assert.GreaterOrEqual(arrow.xMin, placed.Block.xMin - 0.01f, what);
                    Assert.LessOrEqual(arrow.xMax, placed.Block.xMax + 0.01f, what);
                    Assert.GreaterOrEqual(Mathf.Min(arrow.yMin, label.yMin), placed.Block.yMin - 0.01f, what);
                    Assert.LessOrEqual(Mathf.Max(arrow.yMax, label.yMax), placed.Block.yMax + 0.01f, what);
                    Assert.GreaterOrEqual(label.xMin, placed.Block.xMin - 0.01f, what);
                    Assert.LessOrEqual(label.xMax, placed.Block.xMax + 0.01f, what);
                }
            }
        }

        [Test]
        public void ArrowKeepsPointingAtTheRealPlace()
        {
            var layout = new MarkerLayout();
            foreach (Rect frame in Frames)
            {
                List<Rect> hud = Hud(frame);
                for (int step = 0; step < Directions; step++)
                {
                    Vector2 target = Far(frame, step);
                    Assert.IsTrue(layout.PlaceEdge(target, ArrowBox, Label, Gap, frame, hud, out EdgeMarkerPlacement placed));
                    Vector2 aim = target - placed.Arrow;
                    float expected = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
                    Assert.AreEqual(0f, Mathf.DeltaAngle(expected, placed.Angle), 0.01f, "direction " + step);
                }
            }
        }

        [Test]
        public void DirectionIntoTheTowerBarSlidesTheMarkerOntoItsUpperBorder()
        {
            var layout = new MarkerLayout();
            Rect frame = Frames[0];
            List<Rect> hud = Hud(frame);
            // Down and to the right, between the status line and the base panel: only the tower bar is in the way.
            var target = new Vector2(3000f, -4000f);
            Assert.IsTrue(layout.PlaceEdge(target, ArrowBox, Label, Gap, frame, hud, out EdgeMarkerPlacement placed));
            Assert.AreEqual(hud[1].yMax, placed.Block.yMin, 1f, "The block rests on the bar's upper edge.");
            float leaves = target.x * (frame.height * 0.5f - placed.Block.height * 0.5f) / -target.y;
            Assert.AreEqual(leaves, placed.Block.center.x, 1f, "It stays over where its line leaves the frame: no sideways move when up is enough.");
            Assert.Less(placed.Arrow.y, placed.Label.y, "Pointing down, the arrow is under its word.");
        }

        [Test]
        public void DirectionIntoTheMoneyLineLandsOnTheNearestFreeSpot()
        {
            var layout = new MarkerLayout();
            Rect frame = Frames[0];
            List<Rect> hud = Hud(frame);
            // Bottom left: the line to the place leaves the frame through the money line and the bar.
            var target = new Vector2(-4000f, -1800f);
            Assert.IsTrue(layout.PlaceEdge(target, ArrowBox, Label, Gap, frame, hud, out EdgeMarkerPlacement placed));
            AssertClear(placed.Block, frame, hud, "bottom left");
            // No free spot is nearer to where the line leaves the frame: a fine grid finds none.
            Vector2 size = placed.Block.size;
            Vector2 half = size * 0.5f;
            Vector2 direction = target - frame.center;
            float reach = Mathf.Min((frame.width * 0.5f - half.x) / Mathf.Abs(direction.x), (frame.height * 0.5f - half.y) / Mathf.Abs(direction.y));
            Vector2 ideal = frame.center + direction * reach;
            float found = (placed.Block.center - ideal).magnitude;
            for (float x = frame.xMin + half.x; x <= frame.xMax - half.x; x += 4f)
            {
                for (float y = frame.yMin + half.y; y <= frame.yMax - half.y; y += 4f)
                {
                    var centre = new Vector2(x, y);
                    if ((centre - ideal).magnitude < found - 6f)
                    {
                        Assert.IsFalse(MarkerLayout.IsFree(Around(centre, size), frame, hud), "A nearer free spot at " + centre);
                    }
                }
            }
        }

        [Test]
        public void MarkersInTheSameDirectionDoNotOverlap()
        {
            var layout = new MarkerLayout();
            foreach (Rect frame in Frames)
            {
                for (int step = 0; step < Directions; step++)
                {
                    List<Rect> obstacles = Hud(frame);
                    int panels = obstacles.Count;
                    Vector2 target = Far(frame, step);
                    var blocks = new List<Rect>();
                    for (int marker = 0; marker < 3; marker++)
                    {
                        string what = frame.size + " direction " + step + " marker " + marker;
                        Assert.IsTrue(layout.PlaceEdge(target, ArrowBox, Label, Gap, frame, obstacles, out EdgeMarkerPlacement placed), what + ": no place");
                        AssertClear(placed.Block, frame, obstacles, what);
                        foreach (Rect other in blocks)
                        {
                            Assert.IsFalse(placed.Block.Overlaps(other), what + ": on another marker");
                        }

                        blocks.Add(placed.Block);
                        Rect block = placed.Block;
                        obstacles.Add(Rect.MinMaxRect(block.xMin - Spacing, block.yMin - Spacing, block.xMax + Spacing, block.yMax + Spacing));
                    }

                    Assert.AreEqual(panels + 3, obstacles.Count);
                }
            }
        }

        [Test]
        public void PlaceInViewButUnderAPanelGetsAMarkerBesideThePanel()
        {
            var layout = new MarkerLayout();
            Rect frame = Frames[0];
            List<Rect> hud = Hud(frame);
            // On screen, under the tower bar.
            var target = new Vector2(300f, frame.yMin + 60f);
            Assert.IsTrue(layout.PlaceEdge(target, ArrowBox, Label, Gap, frame, hud, out EdgeMarkerPlacement placed));
            AssertClear(placed.Block, frame, hud, "under the bar");
            Assert.AreEqual(300f, placed.Block.center.x, 1f, "Right over the place.");
            Assert.AreEqual(-90f, placed.Angle, 1f, "Pointing straight down at it.");
        }

        [Test]
        public void RingWordSitsOverTheRingWhenThatIsFree()
        {
            var layout = new MarkerLayout();
            Rect frame = Frames[0];
            Assert.IsTrue(layout.PlaceRingLabel(new Vector2(100f, 0f), RingOffset, Label, frame, Hud(frame), out Vector2 label));
            Assert.AreEqual(new Vector2(100f, RingOffset), label);
        }

        [Test]
        public void RingWordFlipsUnderTheRingOffAPanel()
        {
            var layout = new MarkerLayout();
            foreach (Rect frame in Frames)
            {
                List<Rect> hud = Hud(frame);
                // Just under the top strip: the word over the ring would sit on the strip.
                var ring = new Vector2(400f, hud[0].yMin - 50f);
                Assert.IsTrue(layout.PlaceRingLabel(ring, RingOffset, Label, frame, hud, out Vector2 label));
                Assert.AreEqual(ring - Vector2.up * RingOffset, label);
                AssertClear(Around(label, Label), frame, hud, "ring word");
            }
        }

        [Test]
        public void RingWordBetweenTwoPanelsMovesToTheNearestFreePlace()
        {
            var layout = new MarkerLayout();
            Rect frame = Frames[0];
            List<Rect> hud = Hud(frame);
            // A band over and under the ring: neither over nor under is free.
            var ring = new Vector2(500f, 0f);
            hud.Add(Rect.MinMaxRect(400f, 30f, 600f, 90f));
            hud.Add(Rect.MinMaxRect(400f, -90f, 600f, -30f));
            Assert.IsTrue(layout.PlaceRingLabel(ring, RingOffset, Label, frame, hud, out Vector2 label));
            AssertClear(Around(label, Label), frame, hud, "ring word");
            Assert.Less((label - ring).magnitude, 200f, "It stays by its ring.");
        }

        [Test]
        public void MarkerWithNoRoomIsNotPlaced()
        {
            var layout = new MarkerLayout();
            var frame = new Rect(-200f, -60f, 400f, 120f);
            var hud = new List<Rect> { Rect.MinMaxRect(-200f, -60f, 200f, 0f) };
            Assert.IsFalse(layout.PlaceEdge(new Vector2(0f, -4000f), ArrowBox, Label, Gap, frame, hud, out _));
        }

        [Test]
        public void NearestKeepsAFreeBlockWhereItIs()
        {
            var layout = new MarkerLayout();
            Rect frame = Frames[0];
            var ideal = new Vector2(30f, -40f);
            Assert.IsTrue(layout.Nearest(ideal, Label, frame, Hud(frame), out Vector2 centre));
            Assert.AreEqual(ideal, centre);
        }
    }
}
