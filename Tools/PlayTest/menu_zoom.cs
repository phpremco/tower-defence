// Round menu 2026-11-04: stops the menu camera drift and looks at a point from Tools/PlayTest/zoom_arg.txt
// ("x y z distance pitch yaw"); also lists the liquid objects of the diorama with their material and bounds.
var a = System.IO.File.ReadAllText("Tools/PlayTest/zoom_arg.txt").Trim().Split(' ');
float x = float.Parse(a[0], System.Globalization.CultureInfo.InvariantCulture), y = float.Parse(a[1], System.Globalization.CultureInfo.InvariantCulture), z = float.Parse(a[2], System.Globalization.CultureInfo.InvariantCulture);
float d = float.Parse(a[3], System.Globalization.CultureInfo.InvariantCulture), pitch = float.Parse(a[4], System.Globalization.CultureInfo.InvariantCulture), yaw = float.Parse(a[5], System.Globalization.CultureInfo.InvariantCulture);
var cam = UnityEngine.Camera.main;
var drift = cam.GetComponent<Game.UI.MenuCameraDrift>(); if (drift != null) drift.enabled = false;
var rot = UnityEngine.Quaternion.Euler(pitch, yaw, 0f);
cam.transform.rotation = rot;
cam.transform.position = new UnityEngine.Vector3(x, y, z) - rot * UnityEngine.Vector3.forward * d;
var sb = new System.Text.StringBuilder();
foreach (var r in UnityEngine.Object.FindObjectsByType<UnityEngine.MeshRenderer>())
    if (r.name.StartsWith("Liquid_") || r.name.StartsWith("Bank_")) sb.Append(r.name).Append(" ").Append(r.sharedMaterial != null ? r.sharedMaterial.name + "/" + r.sharedMaterial.shader.name : "null").Append(" ").Append(r.bounds.center.ToString("0.0")).Append(" y=").Append(r.transform.position.y).Append("; ");
return sb.ToString();
