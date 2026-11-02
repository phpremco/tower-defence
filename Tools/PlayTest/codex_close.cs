// Closes the codex if it is open.
var codex = UnityEngine.Object.FindAnyObjectByType<Game.UI.CodexView>();
if (codex == null) { return "no codex view"; }
codex.Close();
return "codex closed";
