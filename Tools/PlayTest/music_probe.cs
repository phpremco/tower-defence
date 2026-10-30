// Reports whether the menu music is alive, its volume, the active scene and the level music player, if any.
var music = UnityEngine.Object.FindAnyObjectByType<Game.UI.MenuMusic>();
var source = music != null ? music.GetComponent<UnityEngine.AudioSource>() : null;
var level = UnityEngine.Object.FindAnyObjectByType<Game.Boot.MusicPlayer>();
return "scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
    + " menuMusic=" + (music == null ? "none" : (source.isPlaying ? "playing" : "stopped") + " vol=" + source.volume.ToString("0.00") + " clip=" + source.clip.name + " t=" + source.time.ToString("0.0"))
    + " levelMusic=" + (level != null);
