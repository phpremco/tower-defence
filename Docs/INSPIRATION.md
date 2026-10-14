# INSPIRATION.md

Notes on the genre and on Sentinel 4: Dark Star, written in my own words. This is inspiration, not specification:
`GAME_DESIGN.md` wins. No text, names, numbers or level designs are copied, and nothing from that game is used in code, UI or data.

Researched 2026-10-14 from public reviews and store listings. The sources are thin on detailed mechanics; community guides and a wiki were not found.

## What the sources say

- **Shape of the game:** a classic, dense tower defence with many systems and little hand-holding. Reviewers praise depth and criticise the lack of a tutorial and a visual jump from the previous entry.
  Sources: [Pocket Gamer review](https://www.pocketgamer.com/sentinel-4-dark-star/review/), [TouchArcade review](https://toucharcade.com/2014/08/14/sentinel-4-dark-star-review/)
- **Several kinds of defence:** fixed turrets, units placed on the path, a commander mech, and a stronghold with drones and special strikes.
  Sources: [Engadget](https://www.engadget.com/2014-08-19-sentinel-4-dark-star-is-a-tower-defense-star.html), [Steam page](https://store.steampowered.com/app/386840)
- **Progression:** rewards arrive as new weapons, upgrades and enemy types as you advance, with a large set of persistent upgrades kept separate from in-level spending.
  Sources: [Engadget](https://www.engadget.com/2014-08-19-sentinel-4-dark-star-is-a-tower-defense-star.html), [Droid Gamers](https://www.droidgamers.com/news/origin8-unleashes-sentinel-4-dark-star-onto-android/)
- **Loadout and modes:** the player picks a loadout from the unlocked weapons before a map, and every map has an endless variant next to the campaign.
  Source: [Steam page](https://store.steampowered.com/app/386840)
- **Difficulty:** four levels per mode; the top one mainly makes every enemy much tougher.
  Source: [Engadget](https://www.engadget.com/2014-08-19-sentinel-4-dark-star-is-a-tower-defense-star.html)
- **Commander units:** controllable, level up and gain abilities.
  Source: [Steam page](https://store.steampowered.com/app/386840)

## Ideas not in the design yet (proposals, not built)

1. **Loadout before a level:** pick which unlocked towers come along. Would fit between the level map and the level.
2. **Endless mode** next to the campaign for each level, using the same `LevelDefinition` with generated waves.
3. **Wave preview:** show which enemies the next wave brings (also a known community wish). Cheap to add because waves are data.
4. **Support from the air:** a limited ability the player triggers on the field, funded by the power currency.
5. **Units placed on the path** as another purchase type, which overlaps with the planned grounddrone path object.
