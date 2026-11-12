// Round 48 (copy of r47_rec_start.cs plus the cutscene shot and shot time per row, so a jump at a hard cut is told apart from a pop inside a shot; yaw is the summed turn about world up, not Euler yaw): installs a per-frame recorder (Application.onBeforeRender, so after every Update, animation and LateUpdate)
// that logs, for every animated rig in the scene, the bone pose change of that frame (summed local rotation change in
// degrees and position change), the root's world yaw and position, whether the rig is drawn, and the clip player's
// cycle, clip on top and cycle rate. Output: Logs/r48/<tag>.csv, tag from Tools/PlayTest/r48_arg.txt.
// r47_rec_stop.cs removes it; r47_rec_report.cs turns the file into the pop / stepping / sliding / yaw table.
string tag = System.IO.File.ReadAllText("Tools/PlayTest/r48_arg.txt").Trim();
var domain = System.AppDomain.CurrentDomain;
var old = domain.GetData("r48rec") as UnityEngine.Events.UnityAction;
if (old != null) { UnityEngine.Application.onBeforeRender -= old; }
var oldWriter = domain.GetData("r48writer") as System.IO.StreamWriter;
if (oldWriter != null) { oldWriter.Dispose(); }
var writer = new System.IO.StreamWriter("Logs/r48/" + tag + ".csv", false);
writer.WriteLine("frame,dt,udt,scale,label,id,visible,rot,pos,yaw,x,z,cycle,once,fading,rate,ideal,ctime,otime,shot,stime");
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
var rigs = new System.Collections.Generic.Dictionary<int, object[]>();
var inv = System.Globalization.CultureInfo.InvariantCulture;
int frame = 0;
System.Type playerType = typeof(Game.Core.ModelClipPlayer);
Game.UI.CutscenePlayer cutscene = null;
var sequencerField = typeof(Game.UI.CutscenePlayer).GetField("_sequencer", flags);

System.Func<UnityEngine.Animator, object> findPlayer = animator =>
{
    for (UnityEngine.Transform t = animator.transform; t != null; t = t.parent)
    {
        foreach (var component in t.GetComponents<UnityEngine.MonoBehaviour>())
        {
            if (component == null) continue;
            for (System.Type type = component.GetType(); type != null && type != typeof(UnityEngine.MonoBehaviour); type = type.BaseType)
            {
                foreach (var field in type.GetFields(flags | System.Reflection.BindingFlags.DeclaredOnly))
                {
                    object value = field.GetValue(component);
                    if (value == null) continue;
                    object player = null;
                    if (field.FieldType == playerType) player = value;
                    else
                    {
                        var prop = field.FieldType.GetProperty("Player", flags);
                        if (prop != null && prop.PropertyType == playerType) { try { player = prop.GetValue(value); } catch { } }
                    }

                    if (player == null) continue;
                    var graphs = (UnityEngine.Playables.PlayableGraph[])playerType.GetField("_graphs", flags).GetValue(player);
                    foreach (var graph in graphs)
                    {
                        if (!graph.IsValid()) continue;
                        var output = (UnityEngine.Animations.AnimationPlayableOutput)graph.GetOutput(0);
                        if (output.GetTarget() == animator) return player;
                    }
                }
            }
        }
    }

    return null;
};

System.Func<UnityEngine.Animator, string> labelOf = animator =>
{
    for (UnityEngine.Transform t = animator.transform.parent; t != null; t = t.parent)
    {
        foreach (var component in t.GetComponents<UnityEngine.MonoBehaviour>())
        {
            if (component != null && component.GetType().Namespace != null && component.GetType().Namespace.StartsWith("Game."))
            {
                return component.GetType().Name + "/" + animator.gameObject.name;
            }
        }
    }

    return "?/" + animator.gameObject.name;
};

System.Action<UnityEngine.Transform, UnityEngine.Animator, System.Collections.Generic.List<UnityEngine.Transform>, System.Collections.Generic.List<UnityEngine.Renderer>> collect = null;
collect = (node, owner, bones, renderers) =>
{
    var animator = node.GetComponent<UnityEngine.Animator>();
    if (animator != null && animator != owner) return;
    bones.Add(node);
    var renderer = node.GetComponent<UnityEngine.Renderer>();
    if (renderer != null) renderers.Add(renderer);
    for (int i = 0; i < node.childCount; i++) collect(node.GetChild(i), owner, bones, renderers);
};

// The clip index of a player state: the old player's private field, or the round 47 player's property.
System.Func<object, string, string, int> indexOf = (player, field, property) =>
{
    var info = playerType.GetField(field, flags);
    if (info != null) return (int)info.GetValue(player);
    var prop = property.Length > 0 ? playerType.GetProperty(property, flags) : null;
    return prop != null ? (int)prop.GetValue(player) : -2;
};

System.Func<object, int, string> roleOf = (player, index) =>
{
    if (index == -2) return "";
    if (index < 0) return "-";
    var art = (Game.Core.ModelArtDefinition)playerType.GetField("_art", flags).GetValue(player);
    return art.Clips[index].Role.ToString();
};

UnityEngine.Events.UnityAction handler = () =>
{
    if (!UnityEngine.Application.isPlaying) return;
    frame++;
    if (cutscene == null && frame % 20 == 1) cutscene = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
    var sequencer = cutscene != null ? sequencerField.GetValue(cutscene) as Game.Levels.CutsceneSequencer : null;
    string shotText = sequencer != null ? sequencer.ShotIndex.ToString(inv) : "-1";
    string shotTime = sequencer != null ? sequencer.LocalTime.ToString("0.####", inv) : "0";
    if (frame % 20 == 1)
    {
        foreach (var animator in UnityEngine.Object.FindObjectsByType<UnityEngine.Animator>())
        {
            int id = animator.GetHashCode();
            if (rigs.ContainsKey(id) || !animator.gameObject.activeInHierarchy) continue;
            var bones = new System.Collections.Generic.List<UnityEngine.Transform>();
            var renderers = new System.Collections.Generic.List<UnityEngine.Renderer>();
            collect(animator.transform, animator, bones, renderers);
            var rot = new UnityEngine.Quaternion[bones.Count];
            var pos = new UnityEngine.Vector3[bones.Count];
            for (int i = 0; i < bones.Count; i++) { rot[i] = bones[i].localRotation; pos[i] = bones[i].localPosition; }
            rigs[id] = new object[] { animator, bones.ToArray(), rot, pos, renderers.ToArray(), labelOf(animator), findPlayer(animator), animator.transform.eulerAngles.y, animator.GetComponentInParent<Game.Enemies.EnemyView>(), true, -10, animator.transform.position, animator.transform.rotation, animator.transform.eulerAngles.y };
        }
    }

    var woken = new System.Collections.Generic.List<object[]>();
    var dead = new System.Collections.Generic.List<int>();
    foreach (var pair in rigs)
    {
        object[] rig = pair.Value;
        var animator = (UnityEngine.Animator)rig[0];
        if (animator == null) { dead.Add(pair.Key); continue; }
        if (!animator.gameObject.activeInHierarchy)
        {
            if ((bool)rig[9]) { rig[9] = false; rig[10] = frame; }
            continue;
        }

        if (!(bool)rig[9] && ((string)rig[5]).StartsWith("ModelCorpse/")) woken.Add(rig);
        rig[9] = true;
        rig[11] = animator.transform.position;
        var bones = (UnityEngine.Transform[])rig[1];
        var lastRot = (UnityEngine.Quaternion[])rig[2];
        var lastPos = (UnityEngine.Vector3[])rig[3];
        float rotSum = 0f, posSum = 0f;
        for (int i = 0; i < bones.Length; i++)
        {
            if (bones[i] == null) continue;
            UnityEngine.Quaternion r = bones[i].localRotation;
            UnityEngine.Vector3 p = bones[i].localPosition;
            rotSum += UnityEngine.Quaternion.Angle(lastRot[i], r);
            posSum += (p - lastPos[i]).magnitude;
            lastRot[i] = r; lastPos[i] = p;
        }

        bool visible = false;
        foreach (var renderer in (UnityEngine.Renderer[])rig[4]) { if (renderer != null && renderer.isVisible) { visible = true; break; } }
        object player = rig[6];
        string cycle = "", once = "", fading = "", rate = "", ideal = "", ctime = "", otime = "";
        if (player != null)
        {
            int index = indexOf(player, "_cycle", "Cycle");
            int onceIndex = indexOf(player, "_once", "Once");
            cycle = roleOf(player, index);
            once = roleOf(player, onceIndex);
            fading = roleOf(player, indexOf(player, "_fadingCycle", ""));
            var speed = playerType.GetField("_cycleSpeed", flags);
            var speedProp = playerType.GetProperty("CycleRate", flags);
            if (speed != null) rate = ((float)speed.GetValue(player)).ToString("0.###", inv);
            else if (speedProp != null) rate = ((float)speedProp.GetValue(player)).ToString("0.###", inv);
            var view = rig[8] as Game.Enemies.EnemyView;
            var times = playerType.GetField("_times", flags);
            if (times != null && index >= 0) ctime = ((float[])times.GetValue(player))[index].ToString("0.###", inv);
            if (times != null && onceIndex >= 0) otime = ((float[])times.GetValue(player))[onceIndex].ToString("0.###", inv);
            else if (times == null && onceIndex >= 0) otime = ((float)playerType.GetProperty("OnceTime", flags).GetValue(player)).ToString("0.###", inv);
            if (view != null && index >= 0)
            {
                var clip = ((Game.Core.ModelArtDefinition)playerType.GetField("_art", flags).GetValue(player)).Clips[index];
                float scale = (float)typeof(Game.Enemies.EnemyView).GetField("_creatureScale", flags).GetValue(view);
                if (clip.DistancePerLoop > 0f) ideal = (clip.DistancePerLoop * scale / clip.Length).ToString("0.####", inv);
            }
        }

        // The heading as the turn about the world's up axis summed frame by frame (the twist of each frame's change): Euler yaw
        // flips by 180 deg on a model whose root is pitched near 90 deg (an imported Z-up rig), which read as false snaps.
        UnityEngine.Quaternion turned = animator.transform.rotation * UnityEngine.Quaternion.Inverse((UnityEngine.Quaternion)rig[12]);
        float twist = 2f * UnityEngine.Mathf.Atan2(turned.y, turned.w) * UnityEngine.Mathf.Rad2Deg;
        twist = UnityEngine.Mathf.DeltaAngle(0f, twist);
        rig[12] = animator.transform.rotation;
        rig[13] = (float)rig[13] + twist;
        UnityEngine.Vector3 root = animator.transform.position;
        writer.WriteLine(string.Join(",", frame, UnityEngine.Time.deltaTime.ToString("0.#####", inv), UnityEngine.Time.unscaledDeltaTime.ToString("0.#####", inv),
            UnityEngine.Time.timeScale.ToString("0.##", inv), rig[5], pair.Key, visible ? 1 : 0, rotSum.ToString("0.###", inv), posSum.ToString("0.#####", inv),
            ((float)rig[13]).ToString("0.##", inv), root.x.ToString("0.####", inv), root.z.ToString("0.####", inv), cycle, once, fading, rate, ideal, ctime, otime, shotText, shotTime));
    }

    foreach (int id in dead) rigs.Remove(id);

    // A pooled corpse sent out this frame: how far its first pose is from the last pose the body showed (the enemy rig of the
    // same model that went away this frame or the last, nearest to it), summed over the bones like a frame's change.
    foreach (object[] corpse in woken)
    {
        string name = ((string)corpse[5]).Substring("ModelCorpse/".Length);
        UnityEngine.Vector3 at = ((UnityEngine.Animator)corpse[0]).transform.position;
        object[] body = null;
        float best = 1.5f;
        foreach (object[] rig in rigs.Values)
        {
            if ((string)rig[5] != "Enemy/" + name || (bool)rig[9] || frame - (int)rig[10] > 1) continue;
            float distance = UnityEngine.Vector3.Distance(at, (UnityEngine.Vector3)rig[11]);
            if (distance < best) { best = distance; body = rig; }
        }

        if (body == null) continue;
        var corpseBones = (UnityEngine.Transform[])corpse[1];
        var bodyBones = (UnityEngine.Transform[])body[1];
        var bodyRot = (UnityEngine.Quaternion[])body[2];
        float gap = 0f;
        for (int i = 1; i < corpseBones.Length && i < bodyBones.Length; i++)
        {
            if (corpseBones[i] == null || bodyBones[i] == null || corpseBones[i].name != bodyBones[i].name) continue;
            gap += UnityEngine.Quaternion.Angle(bodyRot[i], corpseBones[i].localRotation);
        }

        writer.WriteLine(string.Join(",", frame, "0", "0", "1", "Handoff/" + name, "H" + frame + "_" + corpse[0].GetHashCode(), 1, gap.ToString("0.###", inv), "0", "0", "0", "0", "", "", "", "", "", "", "", "-1", "0"));
    }
};

UnityEngine.Application.onBeforeRender += handler;
domain.SetData("r48rec", handler);
domain.SetData("r48writer", writer);
return "recording to Logs/r48/" + tag + ".csv";
