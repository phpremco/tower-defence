# Round 88b, package "hud": the surprise ping's place on the HUD (doc patch for the curator)

## Docs/STATUS.md, section "Round 88b" (new item)

**The surprise ping keeps off the HUD by rule.** What the still `Logs/r88a/a0_ping_edge.png` showed (the word
"SURPRISE" on the money line and on the tower bar) had two causes:

1. *The still itself.* The stage's batch editor has a 640x480 screen. `SurprisePingView` placed its markers in
   `LateUpdate` for that 4:3 canvas (safe area 1663x1247 canvas units), and `r88_shot.cs` then drew the HUD at
   1920x1080: the panels follow their anchors to the new frame, the markers keep the offsets from the middle they
   were given for the old one. Measured in the session that reproduced it (`Logs/r88b/ping_edge_before.png`): in the
   640x480 layout no marker rect touched a keep-out zone; in the picture they sit on the bar. The view also read
   the screen point with a `null` camera, which is only right for an overlay canvas.
2. *Real gaps in the view,* independent of the still: the arrow's word was centred 54 units behind the arrow, so at
   a side edge the word lay across its own arrow; nothing kept two or three markers apart; a ring in view was dragged
   off its spawn point together with its word when the word touched a panel; a marker that the greedy
   `KeepOutMath.Resolve` could not free was hidden; Voss's rear-rift marker was only avoided for the same point.

The rule now (pure, `Game.UI.MarkerLayout`, tested in `MarkerLayoutTests`):

- An edge marker is one block: the arrow's box (the square that holds the arrow at any turn, 79 units) and the word
  at its measured width (`Text.preferredWidth`, 40 high), the word on the side away from the edge. The block goes to
  the free point nearest to where the line from the middle of the safe area to the spawn point leaves the frame:
  inside the safe area, on no obstacle. A direction into a panel therefore slides the block along that panel's free
  border (over the tower bar, beside the hero bar, under the top strip). The arrow turns towards the real place from
  where it ends up.
- Obstacles come from one source, `HudKeepOut` (the HUD's real rects at runtime, no typed boxes): the top strip
  (wave, score, next-wave preview, speed and pause), the tower bar with the path objects, the money, status and
  countdown lines by their words, the radar, the hero bar, the boss bar, the base, hero and tower menus, the story
  box, the priority labels, and (new, `HudKeepOut.AddMarker` / `CollectObstacles`) the arrow and the word of Voss's
  rear-rift marker while it shows.
- Markers are placed one after the other and each placed block (8 units of air around it) is an obstacle for the
  next, so up to three never overlap.
- A spawn point counts as in view when its ring is on screen and under no panel. Its ring stays on the point; the
  word sits over it, flips under it when over is taken, else takes the nearest free place. A point on screen but
  under a panel gets the arrow block beside that panel, pointing at it.
- The view reads the point through the canvas's own camera, so a HUD drawn through a camera (stills) is laid out
  right. No allocation per frame (reused lists), no `Find`.

## Docs/COMMANDS.md, section "Round 88b" (new bullets)

- **Stills with world labels in the right place:** `r88_shot.cs` (and `r65_shot.cs`) draws the HUD at the still's
  size while markers and badges were placed for the batch editor's own 640x480 screen: world labels land on panels
  they never touch in the game. `Tools/PlayTest/r88b_shot.cs` gives the camera its target first, steps two frames so
  every view lays itself out for that size, then takes the picture; it prints the safe area and every showing world
  label, with "ON PANEL" / "OUTSIDE" for one that touches a keep-out zone or leaves the safe area.
  `Tools/PlayTest/r88b_shot_arg.txt`: `<width> <height> <png path relative to the stage>`. In `r88_shots.sh`:
  `"arg:r88b_shot_arg.txt=1920 1080 Logs/r88b/x.png" "eval:r88b_shot.cs"`.
  Only views that read the canvas's camera are right in it (`SurprisePingView`, `RearNestMarkerView`); the boss
  marker, the base alert and the rebuild badges still pass `null` and are NOT placed right in such a still.
- **Pings where a still needs them:** `Tools/PlayTest/r88b_ping.cs`, `r88b_ping_arg.txt` one line of words:
  `vp:<x>:<y>` a surprise ping at the ground point under that viewport point (outside 0..1 = off screen that way),
  `rear:<x>:<y>` Voss's rear-rift warning there, `margin:<percent>` the title-safe margin for the session (in memory),
  `dump` screen, canvas, safe area, zones and showing labels.
- **Pitfall:** the stage's batch editor screen is 640x480 (4:3). Anything that is laid out in screen space is laid
  out for that, whatever size a still is drawn at.
