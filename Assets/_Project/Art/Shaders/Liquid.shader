// One surface shader for every pool (ArtSource README round 24 recipe), a keyword per kind: water, lava, bog,
// quicksand, ice and corruption. Everything is sampled in world space (tile sizes in world units), so a pool of any
// outline tiles without mesh UVs. The distance from the bank's shelf comes per vertex in UV1.x (baked from the pool's
// smoothed shore, LiquidSurfaceMesh), standing in for the scene depth on every profile: it drives the shallow-deep
// colour, the shore foam bands, the frost and scum rings and how far from the bank lava glows. Slow waves roll through
// the vertices. _LIQUID_LOW is the Switch variant (one layer, static caustics, no refraction); _LIQUID_REFRACT bends
// the opaque picture under water (High only, needs the opaque texture).
Shader "Game/Liquid"
{
    Properties
    {
        _NormalA ("Normal A", 2D) = "bump" {}
        _NormalB ("Normal B", 2D) = "bump" {}
        _NormalTiles ("Normal tiles (A world size, B world size, B strength, B angle deg)", Vector) = (4.8, 1.65, 0.7, 40)
        _NormalScroll ("Normal scroll (A xy, B zw) tiles/s", Vector) = (0.035, 0.012, -0.05, 0.07)
        _AlbedoA ("Albedo A (crust, sand, ice)", 2D) = "white" {}
        _AlbedoB ("Albedo B", 2D) = "white" {}
        _HeatA ("Heat A (lava)", 2D) = "black" {}
        _HeatB ("Heat B (lava)", 2D) = "black" {}
        _FlowMap ("Flow (RG vector, B speed)", 2D) = "gray" {}
        _MaskMap ("Mask (foam, scum, cracks, veins, sparkle)", 2D) = "black" {}
        _FrostMap ("Frost (ice)", 2D) = "black" {}
        _Flipbook ("Flipbook 4x4 (caustics)", 2D) = "black" {}
        _StaticCaustics ("Static caustics (Low)", 2D) = "black" {}
        _ProfileMap ("Shore wave profile", 2D) = "black" {}
        _Tiles ("Tiles (albedo/mask world size, second world size, flow world size, caustic world size)", Vector) = (5.5, 4.4, 8.25, 6.3)
        _Scroll ("Scroll (first xy, second zw) tiles/s", Vector) = (0.018, 0.006, -0.012, 0.014)

        _ShallowColor ("Shallow / base colour", Color) = (0.035, 0.13, 0.19, 1)
        _DeepColor ("Deep colour", Color) = (0.006, 0.022, 0.055, 1)
        _FoamColor ("Foam / scum / frost colour", Color) = (0.8, 0.88, 0.95, 1)
        [HDR] _CausticColor ("Caustic / sparkle tint", Color) = (0.55, 0.85, 1, 1)
        [HDR] _GlowColor ("Glow (bog spots, crack glow, veins)", Color) = (0.25, 0.55, 1, 1)
        [HDR] _Ramp1 ("Lava ramp 0.3", Color) = (0.4, 0.03, 0.005, 1)
        [HDR] _Ramp2 ("Lava ramp 0.55", Color) = (1.6, 0.35, 0.05, 1)
        [HDR] _Ramp3 ("Lava ramp 0.85", Color) = (3.2, 1.3, 0.25, 1)
        [HDR] _Ramp4 ("Lava ramp 1", Color) = (4, 2.8, 1.4, 1)
        _DepthRange ("Shore distance to full depth", Float) = 0.8
        _Alpha ("Alpha shallow, deep", Vector) = (0.72, 0.96, 0, 0)
        _Smoothness ("Smoothness clear, under foam", Vector) = (0.95, 0.45, 0, 0)
        _EmissionStrength ("Emission strength", Float) = 1
        _Pulse ("Pulse (depth, speed rad/s)", Vector) = (0.15, 1.1, 0, 0)
        _Waves ("Waves (height, length, speed)", Vector) = (0.03, 1.4, 1.6, 0)
        _Refraction ("Refraction offset", Float) = 0.03
        [Toggle(_LIQUID_LOW)] _Low ("Low profile", Float) = 0
        [Toggle(_LIQUID_REFRACT)] _Refract ("Refract the opaque picture", Float) = 0
        [KeywordEnum(Water, Lava, Bog, Quicksand, Ice, Corruption)] _Liquid ("Kind", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-60" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "Liquid"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex LiquidVertex
            #pragma fragment LiquidFragment
            #pragma shader_feature_local _LIQUID_WATER _LIQUID_LAVA _LIQUID_BOG _LIQUID_QUICKSAND _LIQUID_ICE _LIQUID_CORRUPTION
            #pragma shader_feature_local _LIQUID_LOW
            #pragma shader_feature_local _LIQUID_REFRACT
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#if defined(_LIQUID_REFRACT)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
#endif

            TEXTURE2D(_NormalA);
            TEXTURE2D(_NormalB);
            TEXTURE2D(_AlbedoA);
            TEXTURE2D(_AlbedoB);
            TEXTURE2D(_HeatA);
            TEXTURE2D(_HeatB);
            TEXTURE2D(_FlowMap);
            TEXTURE2D(_MaskMap);
            TEXTURE2D(_FrostMap);
            TEXTURE2D(_Flipbook);
            TEXTURE2D(_StaticCaustics);
            TEXTURE2D(_ProfileMap);
            // Inline samplers: a kind leaves most maps unused, and a sampler named after an unused map is stripped with it.
            SAMPLER(sampler_trilinear_repeat);
            SAMPLER(sampler_linear_clamp);

            CBUFFER_START(UnityPerMaterial)
            float4 _NormalTiles;
            float4 _NormalScroll;
            float4 _Tiles;
            float4 _Scroll;
            half4 _ShallowColor;
            half4 _DeepColor;
            half4 _FoamColor;
            half4 _CausticColor;
            half4 _GlowColor;
            half4 _Ramp1;
            half4 _Ramp2;
            half4 _Ramp3;
            half4 _Ramp4;
            float _DepthRange;
            float4 _Alpha;
            float4 _Smoothness;
            half _EmissionStrength;
            float4 _Pulse;
            float4 _Waves;
            float _Refraction;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 shore : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float shore : TEXCOORD1;
                float fog : TEXCOORD2;
#if defined(_LIQUID_REFRACT)
                float4 screenPos : TEXCOORD3;
#endif
            };

            // Two wave trains at an angle: one alone reads as a conveyor belt, two make a surface.
            float WaveHeight(float2 xz, float time)
            {
                float k = 6.2831853 / max(_Waves.y, 0.1);
                float phase = time * _Waves.z;
                float a = sin(xz.x * k + xz.y * k * 0.6 + phase);
                float b = sin(xz.y * k / 1.3 - xz.x * k * 0.27 - phase * 0.8);
                return _Waves.x * (a * 0.6 + b * 0.4);
            }

            Varyings LiquidVertex(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                // The waves die out under the bank, so the sheet never lifts through the shelf.
                world.y += WaveHeight(world.xz, _Time.y) * saturate(input.shore.x * 4.0 + 0.5);
                output.positionWS = world;
                output.positionCS = TransformWorldToHClip(world);
                output.shore = input.shore.x;
                output.fog = ComputeFogFactor(output.positionCS.z);
#if defined(_LIQUID_REFRACT)
                output.screenPos = ComputeScreenPos(output.positionCS);
#endif
                return output;
            }

            float2 Rotate(float2 uv, float degrees)
            {
                float s, c;
                sincos(radians(degrees), s, c);
                return float2(uv.x * c - uv.y * s, uv.x * s + uv.y * c);
            }

            half3 SampleNormal(TEXTURE2D_PARAM(map, samplerMap), float2 uv)
            {
                return UnpackNormal(SAMPLE_TEXTURE2D(map, samplerMap, uv));
            }

            // Whiteout blend of two tangent-space normals.
            half3 Blend(half3 a, half3 b) { return normalize(half3(a.xy + b.xy, a.z * b.z)); }

            // World-space UVs: tangent x is world x, tangent y is world z, so the normal's z is world up.
            half3 ToWorld(half3 n) { return normalize(half3(n.x, n.z, n.y)); }

            half3 LavaRamp(half heat)
            {
                half3 colour = lerp(half3(0, 0, 0), _Ramp1.rgb, saturate(heat / 0.3h));
                colour = lerp(colour, _Ramp2.rgb, saturate((heat - 0.3h) / 0.25h));
                colour = lerp(colour, _Ramp3.rgb, saturate((heat - 0.55h) / 0.3h));
                return lerp(colour, _Ramp4.rgb, saturate((heat - 0.85h) / 0.15h));
            }

            float2 FlipbookUv(float2 uv, float frame)
            {
                float index = fmod(floor(frame), 16.0);
                float column = fmod(index, 4.0);
                float row = floor(index / 4.0);
                // Each caustic frame also tiles: wrap inside the cell, a pixel in from its rim against bleeding.
                float2 inside = frac(uv) * (254.0 / 256.0) + (1.0 / 256.0);
                return (float2(column, 3.0 - row) + inside) / 4.0;
            }

            struct Surface
            {
                half3 albedo;
                half3 normal;
                half3 emission;
                half alpha;
                half smoothness;
            };

            Surface Water(float2 xz, float shore, float time)
            {
                Surface s;
                half3 a = SampleNormal(TEXTURE2D_ARGS(_NormalA, sampler_trilinear_repeat), xz / _NormalTiles.x + _NormalScroll.xy * time);
                half depth01 = (half)saturate(shore / _DepthRange);
#if defined(_LIQUID_LOW)
                half3 n = a;
#else
                half3 b = SampleNormal(TEXTURE2D_ARGS(_NormalB, sampler_trilinear_repeat), Rotate(xz, _NormalTiles.w) / _NormalTiles.y + _NormalScroll.zw * time);
                b.xy *= _NormalTiles.z;
                half3 n = Blend(a, b);
#endif
                s.normal = ToWorld(n);
                s.albedo = lerp(_ShallowColor.rgb, _DeepColor.rgb, depth01);
                s.alpha = lerp(_Alpha.x, _Alpha.y, depth01);

                float2 floorUv = xz / _Tiles.w;
#if defined(_LIQUID_LOW)
                half caustic = min(SAMPLE_TEXTURE2D(_StaticCaustics, sampler_trilinear_repeat, floorUv + time * float2(0.02, 0.013)).r,
                                   SAMPLE_TEXTURE2D(_StaticCaustics, sampler_trilinear_repeat, floorUv * 1.13 - time * float2(0.016, 0.021)).r);
#else
                // Gradients of the unwrapped UV: the wrap inside a frame cell would otherwise pick the smallest mip on its seam.
                half caustic = SAMPLE_TEXTURE2D_GRAD(_Flipbook, sampler_linear_clamp, FlipbookUv(floorUv + n.xy * 0.02, time * 8.0), ddx(floorUv) * 0.25, ddy(floorUv) * 0.25).r;
#endif
                s.emission = caustic * _CausticColor.rgb * 0.22h * (1.0h - depth01);

                half4 f = SAMPLE_TEXTURE2D(_MaskMap, sampler_trilinear_repeat, xz / _Tiles.x + time * float2(0.01, 0.006));
                half fringe = (half)smoothstep(0.1, 0.0, shore);
#if defined(_LIQUID_LOW)
                half cover = fringe;
#else
                float phase = frac(shore * 2.2 - time * 0.28 + f.b * 0.45);
                half band = SAMPLE_TEXTURE2D(_ProfileMap, sampler_trilinear_repeat, float2(phase, 0.5)).r * (half)smoothstep(0.75, 0.05, shore);
                half cover = max(band * 0.85h, fringe);
#endif
                half foam = max(saturate((f.r + cover - 1.0h) * 3.0h), f.g * cover * 0.9h);
                s.albedo = lerp(s.albedo, _FoamColor.rgb, foam);
                s.alpha = max(s.alpha, foam);
                s.smoothness = lerp(_Smoothness.x, _Smoothness.y, foam);
                return s;
            }

            Surface Lava(float2 xz, float shore, float time)
            {
                Surface s;
#if defined(_LIQUID_LOW)
                float2 uvA = xz / _Tiles.x + _Scroll.xy * time;
                half4 albedo = SAMPLE_TEXTURE2D(_AlbedoA, sampler_trilinear_repeat, uvA);
                half4 heatMap = SAMPLE_TEXTURE2D(_HeatA, sampler_trilinear_repeat, uvA);
                half3 n = SampleNormal(TEXTURE2D_ARGS(_NormalA, sampler_trilinear_repeat), uvA);
#else
                half4 flow = SAMPLE_TEXTURE2D(_FlowMap, sampler_trilinear_repeat, xz / _Tiles.z);
                float2 offset = (flow.rg - 0.5) * 0.06;
                float2 uvA = xz / _Tiles.x + _Scroll.xy * time + offset;
                float2 uvB = Rotate(xz, 63.0) / _Tiles.y + _Scroll.zw * time + offset;
                // A slow third sample picks the top crust, so plates break up and form again.
                half pick = (half)smoothstep(0.42, 0.58, SAMPLE_TEXTURE2D(_FlowMap, sampler_trilinear_repeat, xz / (_Tiles.z * 2.7) + time * 0.004).b);
                half4 albedo = lerp(SAMPLE_TEXTURE2D(_AlbedoA, sampler_trilinear_repeat, uvA), SAMPLE_TEXTURE2D(_AlbedoB, sampler_trilinear_repeat, uvB), pick);
                half4 heatMap = lerp(SAMPLE_TEXTURE2D(_HeatA, sampler_trilinear_repeat, uvA), SAMPLE_TEXTURE2D(_HeatB, sampler_trilinear_repeat, uvB), pick);
                half3 n = normalize(lerp(SampleNormal(TEXTURE2D_ARGS(_NormalA, sampler_trilinear_repeat), uvA), SampleNormal(TEXTURE2D_ARGS(_NormalB, sampler_trilinear_repeat), uvB), pick));
#endif
                half heat = (heatMap.r * (0.55h + 0.6h * heatMap.b) + heatMap.g * 0.1h) * (0.55h + 0.6h * (half)smoothstep(0.1, 1.4, shore));
                half molten = (half)(smoothstep(0.7, 0.95, heatMap.b) * smoothstep(0.6, 1.2, shore));
                heat = max(heat, molten * 0.75h);
                half pulse = 1.0h + (half)(_Pulse.x * sin(time * _Pulse.y));
                s.normal = ToWorld(n);
                s.albedo = albedo.rgb * _ShallowColor.rgb;
                s.emission = LavaRamp(heat) * 1.6h * pulse;
                s.alpha = 1.0h;
                s.smoothness = lerp(_Smoothness.y, _Smoothness.x, saturate(heat * 2.0h));
                return s;
            }

            Surface Bog(float2 xz, float shore, float time)
            {
                Surface s;
                half depth01 = (half)saturate(shore / _DepthRange);
                half3 a = SampleNormal(TEXTURE2D_ARGS(_NormalA, sampler_trilinear_repeat), xz / _NormalTiles.x + _NormalScroll.xy * time);
#if defined(_LIQUID_LOW)
                half3 n = a;
#else
                half3 b = SampleNormal(TEXTURE2D_ARGS(_NormalA, sampler_trilinear_repeat), Rotate(xz, _NormalTiles.w) / _NormalTiles.y + _NormalScroll.zw * time);
                b.xy *= _NormalTiles.z;
                half3 n = Blend(a, b);
#endif
                half4 scum = SAMPLE_TEXTURE2D(_MaskMap, sampler_trilinear_repeat, xz / _Tiles.x + _Scroll.xy * time);
                // The film gathers at the bank.
                half cover = saturate(scum.a + (1.0h - depth01) * 0.35h);
                half3 water = lerp(_ShallowColor.rgb, _DeepColor.rgb, depth01);
                s.normal = ToWorld(normalize(lerp(n, half3(0, 0, 1), cover * 0.6h)));
                s.albedo = lerp(water, _FoamColor.rgb * (0.6h + scum.r), cover);
                s.emission = _GlowColor.rgb * scum.g * cover * 1.4h;
                s.alpha = lerp(_Alpha.x, _Alpha.y, depth01);
                s.alpha = max(s.alpha, cover);
                s.smoothness = lerp(_Smoothness.x, _Smoothness.y * scum.b, cover);
                return s;
            }

            Surface Quicksand(float2 xz, float shore, float time)
            {
                Surface s;
                float2 uv = xz / _Tiles.x;
#if defined(_LIQUID_LOW)
                half4 albedo = SAMPLE_TEXTURE2D(_AlbedoA, sampler_trilinear_repeat, uv);
                half3 n = SampleNormal(TEXTURE2D_ARGS(_NormalA, sampler_trilinear_repeat), uv);
#else
                // Two-phase flow map: the bands creep along the swirl and the two samples hand over without a jump.
                float2 flow = (SAMPLE_TEXTURE2D(_FlowMap, sampler_trilinear_repeat, uv).rg * 2.0 - 1.0) * 0.1;
                float p0 = frac(time * 0.35);
                float p1 = frac(p0 + 0.5);
                half w = (half)abs(1.0 - 2.0 * p0);
                half4 albedo = lerp(SAMPLE_TEXTURE2D(_AlbedoA, sampler_trilinear_repeat, uv + flow * p1), SAMPLE_TEXTURE2D(_AlbedoA, sampler_trilinear_repeat, uv + flow * p0), w);
                half3 n = normalize(lerp(SampleNormal(TEXTURE2D_ARGS(_NormalA, sampler_trilinear_repeat), uv + flow * p1), SampleNormal(TEXTURE2D_ARGS(_NormalA, sampler_trilinear_repeat), uv + flow * p0), w));
#endif
                half damp = 1.0h - 0.1575h * (half)smoothstep(0.2, 1.6, shore);
                s.normal = ToWorld(n);
                s.albedo = albedo.rgb * lerp(_ShallowColor.rgb, _FoamColor.rgb, albedo.a * 0.5h) * damp;
                s.emission = half3(0, 0, 0);
                s.alpha = 1.0h;
                s.smoothness = lerp(_Smoothness.y, _Smoothness.x, albedo.a);
                return s;
            }

            Surface Ice(float2 xz, float shore, float time, float3 view)
            {
                Surface s;
                float2 uv = xz / _Tiles.x;
                half4 albedo = SAMPLE_TEXTURE2D(_AlbedoA, sampler_trilinear_repeat, uv);
                half4 cracks = SAMPLE_TEXTURE2D(_MaskMap, sampler_trilinear_repeat, uv);
                half milky = albedo.a;
#if !defined(_LIQUID_LOW)
                // A second look a little deeper along the view: the fracture planes seem to lie under the surface.
                half below = SAMPLE_TEXTURE2D(_MaskMap, sampler_trilinear_repeat, uv + view.xz * 0.04).g;
                milky = saturate(milky + below * 0.35h);
#endif
                half3 colour = lerp(_DeepColor.rgb, albedo.rgb * _ShallowColor.rgb, milky * 0.85h + 0.1h);
                half4 frost = SAMPLE_TEXTURE2D(_FrostMap, sampler_trilinear_repeat, xz / _Tiles.y);
                half threshold = (half)saturate(shore * 2.6);
                half frosted = (half)smoothstep(threshold - 0.08h, threshold + 0.08h, frost.r);
                s.normal = ToWorld(SampleNormal(TEXTURE2D_ARGS(_NormalA, sampler_trilinear_repeat), uv));
                s.albedo = lerp(colour, _FoamColor.rgb, frosted);
#if defined(_LIQUID_LOW)
                s.emission = _GlowColor.rgb * cracks.r * 0.35h;
#else
                s.emission = _GlowColor.rgb * (cracks.r * 0.35h + frost.g * frosted * 0.3h);
#endif
                s.alpha = lerp(_Alpha.x, _Alpha.y, max(milky, frosted));
                s.smoothness = lerp(_Smoothness.x, _Smoothness.y, frosted);
                return s;
            }

            Surface Corruption(float2 xz, float shore, float time)
            {
                Surface s;
                half3 a = SampleNormal(TEXTURE2D_ARGS(_NormalA, sampler_trilinear_repeat), xz / _NormalTiles.x + _NormalScroll.xy * time);
#if defined(_LIQUID_LOW)
                half3 n = a;
#else
                half3 b = SampleNormal(TEXTURE2D_ARGS(_NormalA, sampler_trilinear_repeat), Rotate(xz, _NormalTiles.w) / _NormalTiles.y + _NormalScroll.zw * time);
                half3 n = Blend(a, b);
#endif
                half4 veins = SAMPLE_TEXTURE2D(_MaskMap, sampler_trilinear_repeat, xz / _Tiles.x);
                half pulse = 0.5h + 0.5h * (half)sin(6.2831853 * (veins.g * 3.0 - time * 0.35));
                s.normal = ToWorld(n);
                s.albedo = lerp(_DeepColor.rgb, _ShallowColor.rgb, 0.6h + 0.4h * veins.b);
                s.emission = _GlowColor.rgb * veins.r * pulse * (half)smoothstep(0.0, 0.25, shore) * 1.15h;
                s.alpha = 1.0h;
                s.smoothness = _Smoothness.x;
                return s;
            }

            half4 LiquidFragment(Varyings input) : SV_Target
            {
                float time = _Time.y;
                float2 xz = input.positionWS.xz;
                float shore = input.shore;
                float3 view = normalize(GetWorldSpaceViewDir(input.positionWS));
#if defined(_LIQUID_LAVA)
                Surface s = Lava(xz, shore, time);
#elif defined(_LIQUID_BOG)
                Surface s = Bog(xz, shore, time);
#elif defined(_LIQUID_QUICKSAND)
                Surface s = Quicksand(xz, shore, time);
#elif defined(_LIQUID_ICE)
                Surface s = Ice(xz, shore, time, view);
#elif defined(_LIQUID_CORRUPTION)
                Surface s = Corruption(xz, shore, time);
#else
                Surface s = Water(xz, shore, time);
#endif
                Light sun = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 n = s.normal;
                half ndl = saturate(dot(n, sun.direction));
                half3 lit = sun.color * (sun.shadowAttenuation * sun.distanceAttenuation);
                half3 halfway = normalize(sun.direction + view);
                half power = exp2(10.0h * s.smoothness + 1.0h);
                half specular = pow(saturate(dot(n, halfway)), power) * s.smoothness * s.smoothness * 2.0h;
                half fresnel = pow(1.0h - saturate(dot(n, view)), 4.0h) * s.smoothness;
                half3 sky = SampleSH(reflect(-view, n));
                half3 colour = s.albedo * (lit * ndl + SampleSH(n)) + lit * specular + sky * fresnel * 0.5h + s.emission * _EmissionStrength;
                half alpha = saturate(s.alpha + specular * 0.5h);
#if defined(_LIQUID_REFRACT)
                float2 screenUv = input.screenPos.xy / max(input.screenPos.w, 1e-5);
                half depth01 = (half)saturate(shore / _DepthRange);
                half3 under = SampleSceneColor(screenUv + n.xz * _Refraction * depth01);
                colour = lerp(under, colour, alpha);
                alpha = 1.0h;
#endif
                colour = MixFog(colour, input.fog);
                return half4(colour, alpha);
            }
            ENDHLSL
        }
    }
}
