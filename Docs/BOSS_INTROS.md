# Boss intros: Mercer reacts

Status: curator design, 2026-11-05. Remco: "het lijkt me leuk om Mercer wat meer te betrekken bij die bazen op het moment dat ze spawnen, dat hij echt zoiets heeft van wtf???"

## The beat (first encounter of each boss, about 4-6 s, skippable)

1. **Entrance plays as normal.** The boss performs its own entrance (rig smash, lava rise, lake break, bog growth, ruin gate, rift).
2. **Short focus.**
   - The game slows to about 25% for the beat. It does not pause, so the moment feels live.
   - The camera eases to frame the boss and Mercer together, or the boss alone when Mercer is far away.
   - There is one brief impact kick on the entrance hit. There is no sustained shake.
3. **Mercer reacts in the world.**
   - The hero model plays a `Startled` clip: a half step back, weapon snaps up, a look up at the boss.
   - A short speech line appears over his head.
   - At the same time a small comic-style cut-in panel slides in at the screen edge. It shows Mercer's visor from the front, with the boss reflected in it and a warm or cold rim light from the boss. Following the art rule, his face is never shown.
4. **ORA and Dr. Voss answer** (Remco: "misschien ook een antwoord van de AI of dr Voss erna").
   - **ORA** gives the cold, useful part: the signature and a hint of the counterplay, matching the Codex entry.
   - **Voss** adds the human, scientist reaction (awe, worry, a story hint) via her portrait in the dialogue box.
   - The order is Mercer, ORA, Voss: three short lines that auto-advance. On the second and later encounters only Mercer's bark plays.
   - The Codex page unlocks with a small "New entry" toast.
5. **Back to normal speed.** Total under 6 s. Any input skips the beat.

On **later fights** with the same boss there is no slow-motion and no cut-in. Mercer only says a short bark from a pool of lines, for example "You again?". This avoids repetition.

## Mid-fight barks (one line, no slowdown, with a cooldown so they never spam)

Mercer reacts to the boss's signature moments. These make the mechanics readable and give him personality.

## Lines (English in game; Dutch notes for Remco)

### Gravemaw (the drill rig smash)
- **Intro, Mercer:** "That's... that's our drill. That WAS our drill."
- **Intro, ORA:** "Subterranean contact. It tracks the turret hurting it most. Spread your fire."
- **Intro, Voss:** "It went straight for the drill. Mercer, I think it heard us digging."
- **Bark on throw:** "Did it just THROW my turret?!"
- **Bark on burrow:** "Where'd it go? ...I hate it when they go quiet."
- **Repeat:** "Oh good. The drill-eater's back."

### Cinder Matriarch (rises from the lava)
- **Intro, Mercer:** "Okay. The lava has legs now. Great."
- **Intro, ORA:** "Thermal signature hunting cold emitters. Your Cryo towers are bait."
- **Intro, Voss:** "Cold kills her brood. She'll cross the whole map to stop it. Use that."
- **Bark when a cold tower is smashed:** "She really doesn't like the cold, huh."
- **Bark on eggs:** "Are those... eggs? Shoot the eggs! SHOOT THE EGGS!"

### Frost Warden (Rime Colossus, breaks out of the lake)
- **Intro, Mercer:** "Those fists are bigger than my gate. Why are they bigger than my gate?"
- **Intro, ORA:** "It freezes barricades and walks through them. Fire cracks its armour."
- **Intro, Voss:** "Those glyphs on its armour... it isn't hunting. It's guarding something."
- **Bark on gate freeze:** "My gate's an ice cube. Fantastic."
- **Bark on armour break:** "Armour's off! Hit it with everything!"

### Rotbloom (grows out of the bog)
- **Intro, Mercer:** "Has that flower been watching us this whole time?"
- **Intro, ORA:** "Spore launcher. It overgrows clustered turrets. Splash damage burns it clean."
- **Intro, Voss:** "The whole bog is one organism. And it's very, very angry."
- **Bark on overgrowth:** "Get these weeds off my guns!"
- **Bark on uproot:** "It's walking. The flower is walking."

### The Shattered (the sealed ruin gate opens)
- **Intro, Mercer:** "Doc... was that door supposed to open?"
- **Intro, ORA:** "Crystal construct. It splits under stress. Cover every route."
- **Intro, Voss:** "Those crystals carry our weapon signatures. Did we... make this?"
- **Bark on split:** "Three of them. Of course there are three of them."
- **Bark on rejoin:** "And they're... putting themselves back together. Wonderful."

### The Sealed One (the map comes to it)
- **Intro, Mercer:** "...Doc. The ground is moving. The whole ground."
- **Intro, ORA:** "Scale exceeds all records. It adapts to our strongest weapon. Mix your arsenal."
- **Intro, Voss:** "This is what the crystals were holding down. Don't give it one thing to learn."
- **Bark on adaptation:** "It learned. It actually learned."
- **Bark when it climbs onto the base:** "Get OFF my base!"

## Needs

- **Blender:**
  - Mercer clips `Startled` (recoil, weapon up, look up) and `Point_Shout` (for barks).
  - A visor close-up render setup for the cut-in panel: a 3D render target in Unity from a dedicated camera on the Commander head bone, or Blender-painted panel frames.
  - A comic panel frame from the UI kit.
- **Unity:**
  - `BossIntroDefinition` per boss: lines, cut-in tint, focus timing and slow factor, all in data.
  - A first-seen flag in ProgressData, the same one the Codex uses.
  - A bark pool with a cooldown.
  - The dialogue/bubble view, kept clear of the HUD via HudKeepOut.
  - Skip on any input.
  - The voice uses the existing dialogue system (text plus SFX blip). No voice acting yet.
- **Rules:**
  - Brief kicks only.
  - The beat never hides a telegraph the player needs.
  - It never fires during a pause or a cutscene.
  - It works on gamepad.
