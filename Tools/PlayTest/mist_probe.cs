// Reports the ground mist sheets (position, bounds, material state, visibility) and every loop on the music player
// with its volume, so the look and the ambience of a level can be checked without eyes or ears.
var sb = new System.Text.StringBuilder();
foreach (var name in new[] { "MistLow", "MistHigh" })
{
    var go = UnityEngine.GameObject.Find(name);
    if (go == null) { sb.Append(name).Append(": missing\n"); continue; }
    var r = go.GetComponent<UnityEngine.MeshRenderer>();
    var m = r.sharedMaterial;
    sb.Append(name).Append(" pos=").Append(go.transform.position).Append(" scale=").Append(go.transform.localScale)
      .Append(" bounds=").Append(r.bounds).Append(" visible=").Append(r.isVisible).Append(" enabled=").Append(r.enabled)
      .Append(" shader=").Append(m.shader.name).Append(" queue=").Append(m.renderQueue)
      .Append(" color=").Append(m.GetColor("_BaseColor")).Append(" emis=").Append(m.GetColor("_EmissionColor"))
      .Append(" src=").Append(m.GetFloat("_SrcBlend")).Append(" dst=").Append(m.GetFloat("_DstBlend"))
      .Append(" tex=").Append(m.GetTexture("_BaseMap") == null ? "none" : m.GetTexture("_BaseMap").width.ToString())
      .Append(" kw=").Append(string.Join(",", m.shaderKeywords)).Append('\n');
}
var cam = UnityEngine.Camera.main;
if (cam != null) { sb.Append("cam pos=").Append(cam.transform.position).Append(" ortho=").Append(cam.orthographicSize).Append(" rot=").Append(cam.transform.eulerAngles).Append('\n'); }
var music = UnityEngine.Object.FindAnyObjectByType<Game.Boot.MusicPlayer>();
if (music != null)
{
    sb.Append("MUSIC:");
    foreach (var s in music.GetComponents<UnityEngine.AudioSource>()) { if (s.clip != null) sb.Append(' ').Append(s.clip.name).Append('=').Append(s.volume.ToString("0.000")); }
}
return sb.ToString();
