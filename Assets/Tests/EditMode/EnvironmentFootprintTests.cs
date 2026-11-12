using Game.Levels;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 54: the ruin gate's plinth is wider than the break-out burst, so boulders stood through it. Clearing a turned
    /// rectangle of ground leaves out every piece on it (solid or breakable) and keeps the pieces beside it.
    /// </summary>
    public sealed class EnvironmentFootprintTests
    {
        [Test]
        public void ClearFootprint_LeavesOutWhatStandsOnTheTurnedRectangle_AndKeepsTheRest()
        {
            var host = new GameObject("Instancer");
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            try
            {
                EnvironmentInstancer instancer = EnvironmentInstancer.Create(host.transform);
                Mesh cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                EnvironmentMesh boulder = new EnvironmentMesh("Boulder", cube, material, null, true, Matrix4x4.identity).WithContact(DecorContact.Climbable, default);
                EnvironmentMesh crystal = new EnvironmentMesh("Crystal", cube, material, null, true, Matrix4x4.identity).WithContact(DecorContact.Breakable, default);
                // Facing +x: 3 m either side across (along z), 1 m either side through (along x).
                instancer.AddDecor(boulder, Matrix4x4.Translate(new Vector3(0.5f, 0f, 2.5f)), false);
                instancer.AddDecor(crystal, Matrix4x4.Translate(new Vector3(-0.5f, 0f, -2f)), false);
                instancer.AddDecor(boulder, Matrix4x4.Translate(new Vector3(2.5f, 0f, 0f)), false);
                instancer.AddDecor(crystal, Matrix4x4.Translate(new Vector3(0f, 0f, 4f)), false);
                instancer.Finish();

                Assert.AreEqual(2, instancer.ClearFootprint(Vector3.zero, Vector3.right, new Vector2(3f, 1f)), "not the two pieces on the rectangle");
                Assert.AreEqual(1, instancer.BreakableCount, "the cleared crystal can still break (or the one beside it went too)");
                Assert.IsFalse(instancer.IsClear(new Vector3(2.5f, 0f, 0f), 0.1f, 0.5f), "the boulder beside the rectangle went too");
                Assert.IsTrue(instancer.IsClear(new Vector3(0.5f, 0f, 2.5f), 0.1f, 0.5f), "the boulder on the rectangle still stands");
                Assert.AreEqual(0, instancer.ClearFootprint(Vector3.zero, Vector3.right, new Vector2(3f, 1f)));
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(material);
            }
        }
    }
}
