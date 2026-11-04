// Points the menu camera at the live creature named in Tools/PlayTest/enemy_arg.txt from 3.5 m, pitch 40, yaw 0.
string wanted = System.IO.File.ReadAllText("Tools/PlayTest/enemy_arg.txt").Trim();
foreach (var e in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>())
{
    if (!e.IsAlive || e.name != wanted) continue;
    var cam = UnityEngine.Camera.main;
    var drift = cam.GetComponent<Game.UI.MenuCameraDrift>(); if (drift != null) drift.enabled = false;
    var rot = UnityEngine.Quaternion.Euler(40f, 0f, 0f);
    cam.transform.rotation = rot;
    cam.transform.position = e.transform.position + e.Heading * 0.6f - rot * UnityEngine.Vector3.forward * 3.5f;
    return "at " + e.transform.position.ToString("F2") + " heading " + e.Heading.ToString("F2");
}
return "none";
