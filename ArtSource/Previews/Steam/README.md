# Steam store and library images (Far Harvest: Deepwake)

Made 2026-11-10, reworked 2026-11-11, from one key-art battle scene (`Tools/Blender/steam_keyart.py`, rounds 3-4: a densely dressed Landfall map
from the game's environment kit, the road with the gate across it, towers on build pads on the ground beside the road,
a mixed C1 wave with flyers and Gravemaw rearing behind it, Spencer on his jetpack, on the approved CrysiumField logo
environment), with the CrysiumField lockup composited on top
(`Tools/Blender/steam_compose.py`). Sizes checked against
https://partner.steamgames.com/doc/store/assets and .../store/assets/libraryassets on 2026-11-10.

| File | Steam slot | Size | Logo |
| --- | --- | --- | --- |
| `Steam_header_capsule_920x430.png` | Store: Header Capsule | 920x430 | yes |
| `Steam_small_capsule_462x174.png` | Store: Small Capsule | 462x174 | yes (DEEPWAKE only, the series line is unreadable this small) |
| `Steam_main_capsule_1232x706.png` | Store: Main Capsule | 1232x706 | yes |
| `Steam_vertical_capsule_748x896.png` | Store: Vertical Capsule | 748x896 | yes (lockup above the boss) |
| `Steam_page_background_1438x810.png` | Store: Page Background (optional) | 1438x810 | no (ambient, low contrast) |
| `Steam_library_capsule_600x900.png` | Library: Capsule | 600x900 | yes (lockup above the boss) |
| `Steam_library_header_920x430.png` | Library: Header Capsule | 920x430 | yes (same image as the store header) |
| `Steam_library_hero_3840x1240.png` | Library: Hero | 3840x1240 | no text at all (Steam rule) |
| `Steam_library_logo_1280x720.png` | Library: Logo | 1280x720, alpha | the logo itself (v4 `CrysiumField_library_logo.png`) |
| `Steam_shortcut_icon.ico` | Client: Shortcut Icon | 256/64/48/32/16 | emblem |
| `Steam_icon_256.png` | Client: Shortcut Icon (PNG alternative) | 256x256 | emblem |
| `Steam_app_icon_184.jpg` | Community: App Icon (Steam asks for .jpg) | 184x184 | emblem |
| `Steam_icon_184.png`, `Steam_icon_64.png`, `Steam_icon_32.png` | spare PNG icon sizes | | emblem |
| `Steam_event_cover_800x450.png` | Events: Cover | 800x450 | yes (add event copy per post) |
| `Steam_event_header_1920x622.png` | Events: Header (optional) | 1920x622 | yes |
| `Steam_contact_sheet.png` | overview, not uploaded | | |

`_masters/` holds the text-free renders (`keyart_wide/std/header/vert.png`) and the logo pieces
(`logo_noseries_alpha.png`, `emblem_alpha_1024.png`); rebuild everything with:

```
blender -b --factory-startup --python Tools/Blender/steam_logo_assets.py
blender -b --factory-startup --python Tools/Blender/steam_keyart.py -- --shot all --samples 96
python Tools/Blender/steam_compose.py
```

## Round 4 (2026-11-11): calmer, one focal point, the gate switched on

Remco on the round 3 library hero: "a notch calmer, I don't know where to look"; the right side gave off so much light
that the picture was busy; the gate showed no field; but it must stay brutal. Changes (all slots share the masters):

- **One focal point:** Gravemaw with Spencer in front of it, firing into its face. Spencer turns to the boss (back
  three-quarter, jetpack towards the camera) at 2.0x, with heavy blue bolts and a hit flare on the head, under a warm
  focus key light; Gravemaw's maw glows hotter and its top light is amber instead of pink.
- **Less at once:** one Shalebat and two Skyrays (was 6 + 4), one repair drone (was 2), no Wisps; fewer sparks, stray
  bolts and flak bursts; the one big blast sits in the swarm right at the gate. The swarm on the road stays dense and
  its front claws at the gate.
- **Quiet middle, dark edges:** no tall or glowing dressing in a corridor from the camera to the boss; glowing plants
  (canopy trees and their bulbs, tendrils, mushrooms), crystals and spires thinned out towards the frame edges and
  their emission cut to a fraction; the bright green bog at the right edge is gone, the other edge pools and rifts
  glow less; the crystal spires on the horizon and the pink frame crystals on the left are gone; the cliffs' crystals
  are dim. Rocks, silhouettes and the swarm keep the edges rich.
- **Depth:** shallow depth of field focused between Spencer and the boss; the hero and event header get a soft
  darkening of the outer edges in the compose step.
- **Library hero without text, with room for the logo:** the hero carries no logo or text (Steam overlays the library
  logo). The compose step lays a soft, dark pool in the bottom-left corner where we set the logo (**left bottom** in
  Steamworks' preview tool); the contact sheet shows the logo over it. The capsules and event images keep their logo.
- **Gate on:** the energy field is rebuilt the way `GateView` does it in the game: a vertical sheet between the
  pylons (0.78 of the gate width, 0.08-0.98 m high) and a strip on the road under it, both drawn with the game's
  EnergyField texture (`Exports/Decal_GlowPool/Field_Energy.png`: scanlines, hex lattice, shimmer) in the gate colour,
  additive and see-through, lighting the road and the creatures; hit ripples, short arcs and sparks where Skitters
  strike it.

## Library hero and logo placement

Steam keeps an 860x380 safe area in the centre of the hero uncropped at every client size; critical content belongs
inside it. The logo is a separate asset whose position you pick in Steamworks' preview tool (left bottom, centred top,
centred middle or centred bottom). The battle's focal point (Spencer firing into Gravemaw's face) sits round
the centre; pick **left bottom** for the logo so it does not cover the action. The contact sheet shows that placement,
over the darker pool the compose step leaves for it.

## Still needed from the game itself

- **Screenshots:** at least 5 gameplay screenshots, 1920x1080 or larger, 16:9. These must be real captures from the
  game (not these renders). Steam's store page shows the first ones in search previews, so lead with a busy wave.
- A **trailer** is optional but strongly recommended.
- Event covers usually carry the event's own text; the cover here is a generic template.
- The renders are key art: the towers, creatures, Spencer and Gravemaw are the game's own exported models, but the
  camera, scale and lighting are cinematic, not the in-game view. Steam's rules allow that for capsules; screenshots
  must show actual gameplay.
