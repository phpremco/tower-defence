"""Round 24: the URP liquid shader recipe for the liquid set, as data for Liquids_report.json. The same text, in prose,
is in ArtSource/README.md (round 24). liquid_preview.py builds its Blender preview shaders along these lines.

Shared inputs every recipe uses:
  uvW   = worldPos.xz / tile_world  (world-space UVs: pools of any cell shape tile seamlessly; no mesh UVs needed)
  depth = scene depth behind the surface - surface depth (URP _CameraDepthTexture, linear eye units); with the
          ortho camera, use linear 0..1 depth * (far - near). depth01 = saturate(depth / depth_range).
  shore = 1 - saturate(depth / shore_width)  (1 at the bank, 0 in open liquid). Low profile: no depth texture,
          use a per-pool distance-to-bank texture baked from the cell mask (CPU, 8 px per cell) or vertex colour.
"""

CONVENTIONS = {
    "colour_space": "Only *_Albedo are sRGB. Normals: import as Normal map (OpenGL, G up = Unity). Everything "
                    "else (masks, flow, heat, flipbooks, mist) linear: sRGB off.",
    "wrap": "Tiling maps Repeat; flipbooks and Quicksand_Vortex Clamp; ShoreProfile Repeat U / Clamp V.",
    "flipbooks": "4 x 4 frames of 256 px, frame 0 top-left then row by row (Unity order); every flipbook loops. "
                 "Caustics frames also tile: sample frac(uvW) inside the frame cell, mip bias -0.5 or point-clamp "
                 "the frame edge (or use 1 px gutter by scaling the cell UV by 254/256).",
    "flow_maps": "RG = vector * 0.5 + 0.5 (R = +x, G = +y of the texture = world +z), B = speed 0..1.",
    "low": "Low/ = every map at half size (box filter; normals renormalised).",
    "tint": "Textures are neutral; the palettes in this report are suggested linear tints per continent "
            "(VisualTheme / LiquidStyle data, not code).",
    "world_scale": "Tile sizes below are world units per texture repeat with CellSize 1.",
}

SHADER = {
    "Water": {
        "layers": [
            "N1 = Water_Normal_Swell at uvW / 1.6 tiles, scroll (0.035, 0.012) tiles/s",
            "N2 = Water_Normal_Ripple at uvW rotated 40 deg / 0.55, scroll (-0.05, 0.07) tiles/s, strength 0.7",
            "N = normalize(blend RNM or whiteout(N1, N2)); refraction offset = N.xy * 0.03 * depth01 on the "
            "opaque-texture UV (High only)",
            "colour = lerp(shallow, deep, depth01) (depth_range ~0.8 world units); alpha = lerp(0.72, 0.96, depth01)",
            "caustics: sample Water_Caustics_Flip (8 fps) at the reconstructed floor position xz / 2.1, "
            "x tint * 0.22 * (1 - depth01) * N.L; add to emission (it lights the floor seen through the water)",
            "foam: F = Water_Foam at uvW / 0.6 scroll (0.01, 0.006). phase = frac(shore_dist * 2.2 - time * 0.28 "
            "+ F.b * 0.45); band = ShoreProfile(phase) * smoothstep(0.75, 0.05, shore_dist); fringe = "
            "smoothstep(0.1, 0, shore_dist); cover = max(band * 0.85, fringe); foam = saturate((F.r + cover - 1) * 3) "
            "max F.g * cover * 0.9; albedo = lerp(colour, foamTint, foam), smoothness 0.97 -> 0.45 under foam",
            "specular: GGX smoothness 0.95, fresnel to sky colour; sun glints from N2",
        ],
        "emission": "caustics only (0.2-0.3); WaterGlow per theme stays below bloom threshold 1.0",
        "low": "one normal (Swell) + static Water_Caustics sampled twice (scroll +/-) with min(); no refraction; "
               "foam fringe only (no moving band); distance texture instead of depth",
    },
    "Lava": {
        "layers": [
            "flow = (Lava_Flow.rg - 0.5) * 0.06 at uvW / 1.5 scroll (0.01, 0.004)",
            "A = CrustA_* at uvW / 5.5 + flow, scroll (0.018, 0.006); B = CrustB_* at uvW rotated 63 deg / 4.4 + "
            "flow, scroll (-0.012, 0.014)",
            "w = smoothstep(0.42, 0.58, noise3D(worldPos * 0.45, time * 0.05)) (or a third scrolled Lava_Flow.b): "
            "which crust is on top; as w passes, plates of one layer break up and those of the other form",
            "albedo = lerp(A.albedo, B.albedo, w); normal = lerp(A.n, B.n, w); smoothness lerp(0.25, 0.65, seam)",
            "seam = lerp(A.heat.r, B.heat.r, w); halo = lerp(A.heat.g, B.heat.g, w); under = CrustA_Heat.b at "
            "uvW / 3.3 scroll (0.04, -0.02)",
            "heat = (seam * (0.55 + 0.6 * under) + halo * 0.1) * (0.55 + 0.6 * smoothstep(0.1, 1.4, shore_dist))",
            "molten = smoothstep(0.7, 0.95, under) * smoothstep(0.6, 1.2, shore_dist); heat = max(heat, molten * "
            "0.75); albedo = lerp(albedo, dark red, molten): open molten patches in the hot middle",
            "emission = ramp(heat) (palette ramp, HDR, peak ~4) * LavaEmission (1.6); pulse +-15% at 1.1 rad/s",
            "Lava_Bubble_Flip sprites (10 fps, 1.6 s) on the pool from LiquidLifeBuilder: alpha-blend G as dark skin "
            "colour, add R * ramp(0.9) as emission; B height for a lit rim",
            "Steam_Flip cards (Mist/) + Heat_Distortion screen offset above the pool (High only)",
        ],
        "emission": "seams 2.5-4 (bloom), halo <= 0.6, crust 0",
        "low": "layer A only, no flow distortion, no steam distortion; keep the pulse",
    },
    "Bog": {
        "layers": [
            "N = Bog_Normal at uvW / 4.5 scroll (0.008, 0.004) + Bog_Normal at uvW rot 75 deg / 2.45 scroll "
            "(-0.006, 0.009) * 0.6",
            "water = lerp(shallow, deep, depth01) (depth_range 1.3); alpha lerp(0.85, 0.97, depth01)",
            "S = Bog_Scum at uvW / 3.5 scroll (0.004, 0.0015); a = saturate(S.a + shore * 0.35) "
            "(scum collects at the bank)",
            "albedo = lerp(water, scumTint * S.r, a); smoothness = lerp(0.9, lerp(0.25, 0.65, S.b), a)",
            "emission = glowTint * S.g * a * 1.4 (alien spots on the duckweed)",
            "Bog_Bubble_Flip sprites (10 fps) and steam puffs from LiquidLifeBuilder",
        ],
        "low": "one normal sample; scum without the shore boost",
    },
    "Quicksand": {
        "layers": [
            "opaque. flow = (Quicksand_Flow.rg - 0.5) * 0.1 at uvW / 4.5 (or Quicksand_Vortex per pool, pool-local "
            "UV, for one sinking centre)",
            "two-phase flow map: p0 = frac(time * 0.35), p1 = frac(p0 + 0.5); uv0 = uvW + flow * p0, "
            "uv1 = uvW + (0.37, 0.21) + flow * p1; w = abs(1 - 2 * p0); lerp the two samples of Albedo and Normal",
            "albedo = Quicksand_Albedo.rgb * sandTint, darkened by 45% * 0.35 * smoothstep(0.2, 1.6, shore_dist) "
            "towards the sinking middle; smoothness = lerp(0.15, 0.65, max(A.a, damp * 0.3))",
            "Quicksand_Sparkle * pow(N.H, 200) * sparkleTint (crystal grains)",
            "Quicksand_Ring_Flip sprites (8 fps) where something sinks: RG normal blended, A alpha",
        ],
        "low": "single sample (no two-phase), no sparkle",
    },
    "Ice": {
        "layers": [
            "opaque (or alpha 0.95 over a dark floor). A = Ice_Albedo at uvW / 4.5; milky = A.a",
            "colour = lerp(deep (x2.2 at the bank -> x1 in the middle), A.rgb * iceTint, milky * 0.85 + 0.1)",
            "parallax depth: sample Ice_Cracks.g and Ice_Albedo.a a second time at uvW + viewDir.xz * 0.04 "
            "(fake inner layer) and add 0.3 of it: the planes and bubbles read as under the surface",
            "frost = smoothstep(-0.05, 0.12, Ice_Frost.r(uvW / 2.25) - shore_dist * 2.6) -> frostTint, "
            "smoothness 0.3 under frost, 0.96 / 0.75 on clear / milky ice",
            "emission = crackGlow * (Ice_Cracks.r * 0.35 + Ice_Frost.g * frost * 0.3) * IceGlow; glints: "
            "Ice_Cracks.r * pow(N.H, 80)",
            "normal = Ice_Normal (static: frozen)",
        ],
        "low": "no parallax layer, no glints",
    },
    "Corruption": {
        "layers": [
            "N = Corruption_Normal at uvW / 4 scroll (0.006, 0.003) + at uvW rot 115 deg / 2.4 scroll "
            "(-0.01, 0.006) * 0.5 (slow, heavy)",
            "V = Corruption_Veins at uvW / 4 (same scroll as N1)",
            "albedo = lerp(base, deep, depth01) darkened by V.b * 0.5; smoothness 0.82 (oily)",
            "pulse = 0.35 + 0.65 * (0.5 + 0.5 * sin(2 pi * (V.g * 3 - time * 0.35)))",
            "emission = veinTint * V.r * pulse * smoothstep(0, 0.25, shore_dist) * 1.0-1.3",
        ],
        "low": "one normal sample",
    },
    "Mist": {
        "layers": [
            "three world-space quads (or one quad sampling three layers) at heights 0.12 / 0.32 / 0.6",
            "A = Mist_Layer_A at uvW / 7 scroll (0.012, 0.003); B = Mist_Layer_B at uvW / 5 scroll (0.025, 0.004); "
            "C = Mist_Layer_C at uvW / 3.5 scroll (0.04, -0.01)",
            "alpha = A * 0.16, B * 0.1, C * 0.06 in game (previews use 0.22 / 0.14 / 0.09); colour = theme mist, "
            "unlit or lit by ambient only; depth fade: alpha *= saturate((sceneDepth - fragDepth) / 0.3) so it "
            "never cuts hard into towers and banks",
            "readability: keep total alpha <= 0.3 over the road and build pads (a mask per cell kind is optional)",
            "Mist_Wisp_Flip cards (6 fps, 2.7 s loop) at the border and in nests; Steam_Flip (10 fps) over lava "
            "and hot vents, with Heat_Distortion as a screen-space offset of 0.004 (High only)",
        ],
        "low": "layer A only (alpha 0.12), no wisp cards, no heat distortion",
    },
}
