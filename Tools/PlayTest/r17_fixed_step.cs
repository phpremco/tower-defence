// Round 17: makes every stepped frame exactly 1/30 s (Time.captureFramerate, reset when play mode ends), so stills
// "two frames apart" are two animation frames apart, not two editor hitches.
UnityEngine.Time.captureFramerate = 30;
return "captureFramerate 30";
