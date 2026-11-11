"""Decode the opening-cutscene source recordings (mp3/ogg) to 44.1 kHz 16-bit WAV in src_intro/.

Runs inside Blender (it ships the `aud` module with an mp3/ogg decoder; the system Python has no ffmpeg):
  blender --background --factory-startup --python Tools/Audio/decode_sources.py

Re-runnable: existing WAVs are overwritten. Source files are only read.
"""
import os
import wave

import aud
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
AUDIO = os.path.normpath(os.path.join(HERE, "..", "..", "Assets", "_Project", "Audio"))
OUT = os.path.join(HERE, "src_intro")
RATE = 44100
QUALITY_BEST = 3

MP3 = [
    "cave-water-drops", "earth-rumble", "footsteps-gravel", "footsteps-metal", "footsteps-stone", "gasp",
    "ground-tearing", "hydraulic-door-opening", "long-hiss", "mechanical-clamp", "pistol-draw", "radio-static",
    "release-hiss", "rocket-booster-crackle", "space-rumble", "spaceship-alarm", "spaceship-door-opening",
    "spaceship-rumble", "spaceship-shuttle-firing", "stone-drilling", "wind-blowing", "male-breathing",
    "gregorquendel-laser-charge-175727", "metal-creaking", "metal-hammering", "drill",
]
KENNEY = ["impactMetal_000", "impactMetal_001", "impactMetal_002", "impactMetal_003", "slime_000", "slime_001",
          "lowFrequency_explosion_000", "doorOpen_000", "computerNoise_000",
          "explosionCrunch_000", "explosionCrunch_001", "explosionCrunch_002", "explosionCrunch_003",
          "explosionCrunch_004", "lowFrequency_explosion_001"]


def decode(src, name):
    snd = aud.Sound(src)
    if snd.specs[0] != RATE:
        snd = snd.resample(RATE, QUALITY_BEST)
    data = np.asarray(snd.data(), dtype=np.float32)
    if data.ndim == 1:
        data = data[:, None]
    pcm = (np.clip(data, -1, 1) * 32767).astype("<i2")
    path = os.path.join(OUT, name + ".wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(pcm.shape[1])
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(pcm.tobytes())
    print(f"{name:40s} {data.shape[0] / RATE:7.2f}s ch{data.shape[1]} peak {np.abs(data).max():.3f}")


def main():
    os.makedirs(OUT, exist_ok=True)
    for name in MP3:
        decode(os.path.join(AUDIO, name + ".mp3"), name)
    for name in KENNEY:
        decode(os.path.join(AUDIO, "Kenney_SciFi", name + ".ogg"), "kenney_" + name)


main()
