// Reports their compile errors (first message of each).
var text = "";
foreach (var name in new[] { "Game/Liquid", "Game/Mist" })
{
    var shader = UnityEngine.Shader.Find(name);
    if (shader == null) { text += name + " missing; "; continue; }
    var messages = UnityEditor.ShaderUtil.GetShaderMessages(shader);
    text += name + " errors=" + UnityEditor.ShaderUtil.ShaderHasError(shader) + " messages=" + messages.Length;
    if (messages.Length > 0) { text += " first: " + messages[0].message + " line " + messages[0].line; }
    text += "; ";
}
return text;
