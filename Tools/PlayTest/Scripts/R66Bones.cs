using System.Text;
using Game.Core;
using Game.Hero;
using UnityEditor;
using UnityEngine;

/// <summary>Round 66: lists Spencer's bones in his idle pose (figure space) and the repair drone's nodes, to seat the armour plates and the companion.</summary>
public static class R66Bones
{
    public static string Run()
    {
        var text = new StringBuilder();
        var art = AssetDatabase.LoadAssetAtPath<HeroArtDefinition>("Assets/_Project/Data/Art/Story/MercerArt.asset");
        var root = new GameObject("R66Bones");
        try
        {
            ArtModel body = ArtModel.CreateStill(root.transform, art.Body);
            body.Sample(ModelClipRole.Idle, 0f, loop: true);
            text.Append("body root scale ").Append(body.Root.lossyScale.ToString("0.000")).Append('\n');
            foreach (Transform t in body.Root.GetComponentsInChildren<Transform>())
            {
                text.Append(t.name).Append(" parent=").Append(t.parent != null ? t.parent.name : "-").Append(" pos=").Append(t.position.ToString("0.000")).Append(" fwd=").Append(t.forward.ToString("0.00")).Append(" up=").Append(t.up.ToString("0.00")).Append(" scale=").Append(t.lossyScale.x.ToString("0.000")).Append('\n');
            }

            foreach (Renderer r in body.Renderers)
            {
                text.Append("renderer ").Append(r.name).Append(' ').Append(r.GetType().Name).Append(" bounds ").Append(r.bounds.center.ToString("0.00")).Append(r.bounds.size.ToString("0.00")).Append('\n');
            }

            var drone = AssetDatabase.LoadAssetAtPath<Game.Drones.DroneDefinition>("Assets/_Project/Data/Drones/RepairDrone.asset");
            ArtModel d = ArtModel.CreateStill(root.transform, drone.Art);
            foreach (Transform t in d.Root.GetComponentsInChildren<Transform>())
            {
                text.Append("drone ").Append(t.name).Append(" pos=").Append(t.position.ToString("0.000")).Append('\n');
            }

            foreach (Renderer r in d.Renderers)
            {
                text.Append("drone renderer ").Append(r.name).Append(" bounds ").Append(r.bounds.center.ToString("0.00")).Append(r.bounds.size.ToString("0.00")).Append('\n');
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        System.IO.File.WriteAllText("Logs/r66/bones.txt", text.ToString());
        return "Logs/r66/bones.txt";
    }
}
