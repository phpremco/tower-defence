// Round 48: removes the recorder r48_rec_start.cs installed and closes its file.
var domain = System.AppDomain.CurrentDomain;
var handler = domain.GetData("r48rec") as UnityEngine.Events.UnityAction;
if (handler != null) { UnityEngine.Application.onBeforeRender -= handler; }
var writer = domain.GetData("r48writer") as System.IO.StreamWriter;
if (writer != null) { writer.Dispose(); }
domain.SetData("r48rec", null);
domain.SetData("r48writer", null);
return handler != null ? "stopped" : "no recorder";
