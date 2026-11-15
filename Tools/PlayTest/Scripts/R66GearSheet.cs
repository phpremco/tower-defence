using System.Globalization;
using System.IO;
using System.Text;
using Game.Core;
using Game.Hero;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Round 66: stills of the hero's display figure wearing his gear, without Play Mode: per armour tier a front-left, a
/// back-right and a side view (so a plate that floats or sinks shows), and the companion drone at his shoulder.
/// Tools/PlayTest/r66_sheet_arg.txt: "label=x", "tiers=0,1,2,3,4,5,10", "drone=1", "size=900", "out=ArtSource/Previews/QA".
/// unity command run_script --file Tools/PlayTest/Scripts/R66GearSheet.cs --entry R66GearSheet.Run --timeout_ms 120000 --timeout 180
/// </summary>
public static class R66GearSheet
{
    private static readonly (string Name, float Yaw, float Pitch)[] Views =
    {
        ("front", 205f, 8f),
        ("back", 25f, 8f),
        ("left", 270f, 8f),
        ("top", 205f, 44f),
    };

    public static string Run()
    {
        string label = "sheet";
        string outFolder = "ArtSource/Previews/QA";
        int[] tiers = { 0, 1, 2, 3, 4, 5, 10 };
        int drone = 1;
        int size = 900;
        const string ArgPath = "Tools/PlayTest/r66_sheet_arg.txt";
        if (File.Exists(ArgPath))
        {
            foreach (string line in File.ReadAllLines(ArgPath))
            {
                string[] pair = line.Trim().Split('=');
                if (pair.Length != 2) continue;
                if (pair[0] == "label") label = pair[1];
                if (pair[0] == "out") outFolder = pair[1];
                if (pair[0] == "drone") drone = int.Parse(pair[1]);
                if (pair[0] == "size") size = int.Parse(pair[1]);
                if (pair[0] == "tiers") tiers = System.Array.ConvertAll(pair[1].Split(','), int.Parse);
            }
        }

        var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>("Assets/_Project/Data/Hero/Commander.asset");
        var report = new StringBuilder();
        var rig = new GameObject("R66GearSheet") { hideFlags = HideFlags.HideAndDontSave };
        rig.transform.position = new Vector3(7000f, 0f, 0f);
        var texture = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        try
        {
            Light key = AddLight(rig.transform, new Vector3(42f, -150f, 0f), new Color(1f, 0.96f, 0.9f), 1.5f);
            AddLight(rig.transform, new Vector3(15f, 40f, 0f), new Color(0.6f, 0.75f, 1f), 0.7f);
            key.shadows = LightShadows.Soft;
            var cameraGo = new GameObject("Camera") { hideFlags = HideFlags.HideAndDontSave };
            cameraGo.transform.SetParent(rig.transform, false);
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 1.15f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.16f, 0.18f, 0.22f, 1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 40f;
            camera.targetTexture = texture;
            camera.enabled = false;

            Directory.CreateDirectory(outFolder);
            foreach (int tier in tiers)
            {
                var gear = new HeroGear(0f, drone, 0f, tier, 10);
                GameObject figure = HeroView.BuildDisplayFigure(rig.transform, hero, gear);
                SetFlags(figure.transform);
                int plates = 0;
                foreach (MeshRenderer renderer in figure.GetComponentsInChildren<MeshRenderer>())
                {
                    if (renderer.name == "ArmourPlate") plates++;
                }

                foreach ((string Name, float Yaw, float Pitch) view in Views)
                {
                    Quaternion look = Quaternion.Euler(view.Pitch, view.Yaw, 0f);
                    camera.transform.SetPositionAndRotation(rig.transform.position + new Vector3(0f, 1.05f, 0f) - look * Vector3.forward * 10f, look);
                    camera.Render();
                    string path = Path.Combine(outFolder, "r66_" + label + "_tier" + tier.ToString(CultureInfo.InvariantCulture) + "_" + view.Name + ".png");
                    Save(texture, path);
                }

                report.Append("tier ").Append(tier).Append(": ").Append(plates).Append(" plates; ");
                Object.DestroyImmediate(figure);
            }
        }
        finally
        {
            Object.DestroyImmediate(rig);
            texture.Release();
            Object.DestroyImmediate(texture);
        }

        return report.ToString();
    }

    private static Light AddLight(Transform parent, Vector3 euler, Color colour, float intensity)
    {
        var go = new GameObject("Light") { hideFlags = HideFlags.HideAndDontSave };
        go.transform.SetParent(parent, false);
        go.transform.rotation = Quaternion.Euler(euler);
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = colour;
        light.intensity = intensity;
        return light;
    }

    private static void SetFlags(Transform root)
    {
        root.gameObject.hideFlags = HideFlags.HideAndDontSave;
        for (int i = 0; i < root.childCount; i++)
        {
            SetFlags(root.GetChild(i));
        }
    }

    private static void Save(RenderTexture texture, string path)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = texture;
        var image = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
        image.Apply();
        RenderTexture.active = previous;
        File.WriteAllBytes(path, image.EncodeToPNG());
        Object.DestroyImmediate(image);
    }
}
