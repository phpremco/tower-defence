// Round 47: removes the recorder r47_rec_start.cs installed and closes its file.
var domain = System.AppDomain.CurrentDomain;
var handler = domain.GetData("r47rec") as UnityEngine.Events.UnityAction;
if (handler != null) { UnityEngine.Application.onBeforeRender -= handler; }
var writer = domain.GetData("r47writer") as System.IO.StreamWriter;
if (writer != null) { writer.Dispose(); }
domain.SetData("r47rec", null);
domain.SetData("r47writer", null);
return handler != null ? "stopped" : "no recorder";
