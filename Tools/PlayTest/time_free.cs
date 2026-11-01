// Lets game time run at real speed again after burst_probe.cs slowed it down for screenshots.
UnityEngine.Time.timeScale = 1f;
UnityEngine.Time.captureDeltaTime = 0f;
return "time free";
