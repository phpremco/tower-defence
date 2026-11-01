// Reports the sizes behind the planet map: the image rects, the render texture, the camera and where the rim projects.
var sb = new System.Text.StringBuilder();
var canvas = UnityEngine.Object.FindAnyObjectByType<UnityEngine.Canvas>();
sb.Append("screen=" + UnityEngine.Screen.width + "x" + UnityEngine.Screen.height + " canvasScale=" + (canvas != null ? canvas.scaleFactor.ToString("0.000") : "none"));
foreach (var raw in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.RawImage>(UnityEngine.FindObjectsSortMode.None))
{
    var r = raw.rectTransform;
    sb.Append(" | " + raw.name + " rect=" + r.rect.width.ToString("0") + "x" + r.rect.height.ToString("0") + " scale=" + r.lossyScale.ToString("0.00") + " tex=" + (raw.texture != null ? raw.texture.width + "x" + raw.texture.height : "none"));
}
foreach (var cam in UnityEngine.Object.FindObjectsByType<UnityEngine.Camera>(UnityEngine.FindObjectsSortMode.None))
{
    sb.Append(" | cam " + cam.name + " aspect=" + cam.aspect.ToString("0.000") + " px=" + cam.pixelWidth + "x" + cam.pixelHeight + " fov=" + cam.fieldOfView + " rt=" + (cam.targetTexture != null ? cam.targetTexture.width + "x" + cam.targetTexture.height : "none"));
    if (cam.name == "GlobeCamera")
    {
        var globe = UnityEngine.GameObject.Find("Globe");
        if (globe != null)
        {
            var c = cam.WorldToViewportPoint(globe.transform.position);
            var right = cam.WorldToViewportPoint(globe.transform.position + cam.transform.right);
            var up = cam.WorldToViewportPoint(globe.transform.position + cam.transform.up);
            sb.Append(" centre=" + c.ToString("0.000") + " rimRight=" + right.ToString("0.000") + " rimUp=" + up.ToString("0.000") + " globeScale=" + globe.transform.lossyScale.ToString("0.00") + " bounds=" + globe.GetComponent<UnityEngine.MeshRenderer>().bounds.size.ToString("0.00"));
        }
    }
}
return sb.ToString();
