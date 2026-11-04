// The ground of a level with an imported environment set: the Blender tiling sets (ground, a second ground layer,
// road, plaza, cliff and the element layer under rifts) sampled in world space and blended by a splat map the level
// builds from its grid (R road, G plaza, B second layer, A element terrain) plus the slope (cliff), with a height
// blend from each set's mask (B), so the road's grit fills the ground's joints first and edges break up naturally.
// The baked level painting stays on top as a broad tint (patches, shores, far darkening) and its glow (veins, seams)
// off the road. Mask: R occlusion, G smoothness, B height, A dust that sparkles. The Low profile skips the detail
// normal, the sparkle and the normals of the minor layers.
Shader "Game/EnvironmentGround"
{
    Properties
    {
        _PaintMap ("Baked painting", 2D) = "grey" {}
        _PaintGlow ("Baked glow", 2D) = "black" {}
        _SplatMap ("Splat (R road, G plaza, B alt, A element)", 2D) = "black" {}
        _FieldRect ("Field rect (min x, min z, 1/width, 1/depth)", Vector) = (0, 0, 1, 1)

        _GroundAlbedo ("Ground albedo", 2D) = "grey" {}
        _GroundNormal ("Ground normal", 2D) = "bump" {}
        _GroundMask ("Ground mask", 2D) = "white" {}
        _GroundEmission ("Ground emission", 2D) = "black" {}
        _AltAlbedo ("Alt albedo", 2D) = "grey" {}
        _AltNormal ("Alt normal", 2D) = "bump" {}
        _AltMask ("Alt mask", 2D) = "white" {}
        _AltEmission ("Alt emission", 2D) = "black" {}
        _RoadAlbedo ("Road albedo", 2D) = "grey" {}
        _RoadNormal ("Road normal", 2D) = "bump" {}
        _RoadMask ("Road mask", 2D) = "white" {}
        _PlazaAlbedo ("Plaza albedo", 2D) = "grey" {}
        _PlazaNormal ("Plaza normal", 2D) = "bump" {}
        _PlazaMask ("Plaza mask", 2D) = "white" {}
        _PlazaEmission ("Plaza emission", 2D) = "black" {}
        _CliffAlbedo ("Cliff albedo", 2D) = "grey" {}
        _CliffNormal ("Cliff normal", 2D) = "bump" {}
        _CliffMask ("Cliff mask", 2D) = "white" {}
        _CliffEmission ("Cliff emission", 2D) = "black" {}
        _ElementAlbedo ("Element albedo", 2D) = "grey" {}
        _ElementNormal ("Element normal", 2D) = "bump" {}
        _ElementMask ("Element mask", 2D) = "white" {}
        _ElementEmission ("Element emission", 2D) = "black" {}
        _DetailNormal ("Detail normal", 2D) = "bump" {}

        _TileA ("Tile size: ground, alt, road, plaza", Vector) = (3, 2, 2, 3)
        _TileB ("Tile size: cliff, element, detail", Vector) = (3, 2, 0.6, 0)
        _GlowA ("Glow: ground, alt, plaza, cliff", Vector) = (1, 1, 1, 1)
        _GlowB ("Glow: element, painting, sparkle", Vector) = (1, 0.8, 0.6, 0)
        _Sharpness ("Edge sharpness: road, plaza, alt, element", Vector) = (7, 7, 4, 5)
        _Blend ("Height blend, cliff slope start, full, detail strength", Vector) = (0.35, 0.28, 0.5, 0.5)
        _Paint ("Painting: influence, blur, occlusion, smoothness", Vector) = (0.6, 2.5, 0.6, 1)
        _PaintReference ("Painting's neutral colour", Color) = (0.2, 0.2, 0.25, 1)
        _SparkleColour ("Sparkle colour", Color) = (1, 0.7, 0.95, 1)
        _DetailFade ("Detail fade (tiles per pixel: full, gone)", Vector) = (0.004, 0.02, 0, 0)
        _PlazaCount ("Plaza count", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Lit" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        #define MAX_PLAZAS 8

        CBUFFER_START(UnityPerMaterial)
        float4 _FieldRect;
        float4 _TileA;
        float4 _TileB;
        float4 _GlowA;
        float4 _GlowB;
        float4 _Sharpness;
        float4 _Blend;
        float4 _Paint;
        half4 _PaintReference;
        half4 _SparkleColour;
        float4 _DetailFade;
        float _PlazaCount;
        float4 _PlazaCentres[MAX_PLAZAS];
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex GroundVertex
            #pragma fragment GroundFragment

            #pragma multi_compile_local _ _ENV_LOW

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_PaintMap); SAMPLER(sampler_PaintMap);
            TEXTURE2D(_PaintGlow);
            TEXTURE2D(_SplatMap); SAMPLER(sampler_SplatMap);
            TEXTURE2D(_GroundAlbedo); SAMPLER(sampler_GroundAlbedo);
            TEXTURE2D(_GroundNormal);
            TEXTURE2D(_GroundMask);
            TEXTURE2D(_GroundEmission);
            TEXTURE2D(_AltAlbedo);
            TEXTURE2D(_AltNormal);
            TEXTURE2D(_AltMask);
            TEXTURE2D(_AltEmission);
            TEXTURE2D(_RoadAlbedo);
            TEXTURE2D(_RoadNormal);
            TEXTURE2D(_RoadMask);
            TEXTURE2D(_PlazaAlbedo);
            TEXTURE2D(_PlazaNormal);
            TEXTURE2D(_PlazaMask);
            TEXTURE2D(_PlazaEmission);
            TEXTURE2D(_CliffAlbedo);
            TEXTURE2D(_CliffNormal);
            TEXTURE2D(_CliffMask);
            TEXTURE2D(_CliffEmission);
            TEXTURE2D(_ElementAlbedo);
            TEXTURE2D(_ElementNormal);
            TEXTURE2D(_ElementMask);
            TEXTURE2D(_ElementEmission);
            TEXTURE2D(_DetailNormal);

            // Every tiling map repeats and shares the ground albedo's filtering (trilinear, anisotropic from its importer).
            #define TILE_SAMPLER sampler_GroundAlbedo

            struct Layer
            {
                half3 albedo;
                half3 normalTS;
                half4 mask;
                half3 emission;
            };

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 tangentWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
            };

            Varyings GroundVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normals = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = normals.normalWS;
                output.tangentWS = half4(normals.tangentWS, input.tangentOS.w);
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            // The gradients come from outside any branch, so a layer can be skipped where its weight is zero.
            #define SAMPLE_TILE(map, uv, gx, gy) SAMPLE_TEXTURE2D_GRAD(map, TILE_SAMPLER, uv, gx, gy)

            Layer SampleLayer(TEXTURE2D_PARAM(albedoMap, unusedA), TEXTURE2D_PARAM(normalMap, unusedN), TEXTURE2D_PARAM(maskMap, unusedM), float2 uv, float2 gx, float2 gy, bool withNormal)
            {
                Layer layer;
                layer.albedo = SAMPLE_TILE(albedoMap, uv, gx, gy).rgb;
                layer.mask = SAMPLE_TILE(maskMap, uv, gx, gy);
                layer.normalTS = withNormal ? UnpackNormal(SAMPLE_TILE(normalMap, uv, gx, gy)) : half3(0, 0, 1);
                layer.emission = half3(0, 0, 0);
                return layer;
            }

            // A layer's weight from its splat value and how much higher its surface stands than what lies under it.
            half HeightWeight(half splat, half layerHeight, half baseHeight, half sharpness)
            {
                return saturate((splat + (layerHeight - baseHeight) * _Blend.x - 0.5h) * sharpness + 0.5h);
            }

            void Mix(inout Layer result, inout half height, Layer layer, half weight)
            {
                result.albedo = lerp(result.albedo, layer.albedo, weight);
                result.normalTS = lerp(result.normalTS, layer.normalTS, weight);
                result.mask = lerp(result.mask, layer.mask, weight);
                result.emission = lerp(result.emission, layer.emission, weight);
                height = lerp(height, layer.mask.b, weight);
            }

            // The plaza's tiles are laid from the middle of its own plaza, so its sigil sits in the centre.
            float2 PlazaOrigin(float2 xz)
            {
                float2 best = float2(0, 0);
                float bestDistance = 1e9;
                int count = (int)_PlazaCount;
                for (int i = 0; i < MAX_PLAZAS; i++)
                {
                    if (i >= count)
                    {
                        break;
                    }

                    float2 d = xz - _PlazaCentres[i].xy;
                    float distance = dot(d, d);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = _PlazaCentres[i].xy;
                    }
                }

                return best;
            }

            half4 GroundFragment(Varyings input) : SV_Target
            {
                float2 xz = input.positionWS.xz;
                float2 gx = ddx(xz);
                float2 gy = ddy(xz);
                float2 fieldUV = (xz - _FieldRect.xy) * _FieldRect.zw;
                half4 splat = SAMPLE_TEXTURE2D(_SplatMap, sampler_SplatMap, fieldUV);
                half3 normalWS = normalize(input.normalWS);
                half slope = 1.0h - normalWS.y;

                #if defined(_ENV_LOW)
                    bool minorNormals = false;
                #else
                    bool minorNormals = true;
                #endif

                // The plain ground under everything.
                float tile = _TileA.x;
                Layer result = SampleLayer(TEXTURE2D_ARGS(_GroundAlbedo, TILE_SAMPLER), TEXTURE2D_ARGS(_GroundNormal, TILE_SAMPLER), TEXTURE2D_ARGS(_GroundMask, TILE_SAMPLER), xz / tile, gx / tile, gy / tile, true);
                result.emission = SAMPLE_TILE(_GroundEmission, xz / tile, gx / tile, gy / tile).rgb * _GlowA.x;
                half height = result.mask.b;

                // The second ground layer in patches.
                UNITY_BRANCH
                if (splat.b > 0.01h)
                {
                    tile = _TileA.y;
                    Layer alt = SampleLayer(TEXTURE2D_ARGS(_AltAlbedo, TILE_SAMPLER), TEXTURE2D_ARGS(_AltNormal, TILE_SAMPLER), TEXTURE2D_ARGS(_AltMask, TILE_SAMPLER), xz / tile, gx / tile, gy / tile, minorNormals);
                    alt.emission = SAMPLE_TILE(_AltEmission, xz / tile, gx / tile, gy / tile).rgb * _GlowA.y;
                    Mix(result, height, alt, HeightWeight(splat.b, alt.mask.b, height, _Sharpness.z));
                }

                // The element terrain (rift rock).
                UNITY_BRANCH
                if (splat.a > 0.01h)
                {
                    tile = _TileB.y;
                    Layer element = SampleLayer(TEXTURE2D_ARGS(_ElementAlbedo, TILE_SAMPLER), TEXTURE2D_ARGS(_ElementNormal, TILE_SAMPLER), TEXTURE2D_ARGS(_ElementMask, TILE_SAMPLER), xz / tile, gx / tile, gy / tile, minorNormals);
                    element.emission = SAMPLE_TILE(_ElementEmission, xz / tile, gx / tile, gy / tile).rgb * _GlowB.x;
                    Mix(result, height, element, HeightWeight(splat.a, element.mask.b, height, _Sharpness.w));
                }

                // Rock on steep ground: projected from the side the slope faces, so the strata run level.
                half cliffWeight = smoothstep(_Blend.y, _Blend.z, slope) * (1.0h - splat.r) * (1.0h - splat.g);
                UNITY_BRANCH
                if (cliffWeight > 0.01h)
                {
                    tile = _TileB.x;
                    bool alongX = abs(normalWS.x) > abs(normalWS.z);
                    float2 uv = alongX ? input.positionWS.zy : input.positionWS.xy;
                    float2 cgx = alongX ? ddx(input.positionWS.zy) : ddx(input.positionWS.xy);
                    float2 cgy = alongX ? ddy(input.positionWS.zy) : ddy(input.positionWS.xy);
                    Layer cliff = SampleLayer(TEXTURE2D_ARGS(_CliffAlbedo, TILE_SAMPLER), TEXTURE2D_ARGS(_CliffNormal, TILE_SAMPLER), TEXTURE2D_ARGS(_CliffMask, TILE_SAMPLER), uv / tile, cgx / tile, cgy / tile, minorNormals);
                    cliff.emission = SAMPLE_TILE(_CliffEmission, uv / tile, cgx / tile, cgy / tile).rgb * _GlowA.w;
                    Mix(result, height, cliff, cliffWeight);
                }

                // The painting tints the natural ground only: patches, wet shores, rock on the hills, the dark far land.
                half3 paint = SAMPLE_TEXTURE2D_BIAS(_PaintMap, sampler_PaintMap, fieldUV, _Paint.y).rgb;
                half3 tint = clamp(paint / max(_PaintReference.rgb, 0.02h), 0.2h, 2.2h);
                result.albedo *= lerp(half3(1, 1, 1), tint, _Paint.x);
                half natural = 1.0h;

                // The road and the plazas on top: the sharpest edges, laid where the grid says.
                UNITY_BRANCH
                if (splat.r > 0.01h)
                {
                    tile = _TileA.z;
                    Layer road = SampleLayer(TEXTURE2D_ARGS(_RoadAlbedo, TILE_SAMPLER), TEXTURE2D_ARGS(_RoadNormal, TILE_SAMPLER), TEXTURE2D_ARGS(_RoadMask, TILE_SAMPLER), xz / tile, gx / tile, gy / tile, true);
                    half weight = HeightWeight(splat.r, road.mask.b, height, _Sharpness.x);
                    Mix(result, height, road, weight);
                    natural -= weight * natural;
                }

                UNITY_BRANCH
                if (splat.g > 0.01h && _PlazaCount > 0)
                {
                    tile = _TileA.w;
                    float2 uv = (xz - PlazaOrigin(xz)) / tile + 0.5;
                    Layer plaza = SampleLayer(TEXTURE2D_ARGS(_PlazaAlbedo, TILE_SAMPLER), TEXTURE2D_ARGS(_PlazaNormal, TILE_SAMPLER), TEXTURE2D_ARGS(_PlazaMask, TILE_SAMPLER), uv, gx / tile, gy / tile, minorNormals);
                    plaza.emission = SAMPLE_TILE(_PlazaEmission, uv, gx / tile, gy / tile).rgb * _GlowA.z;
                    half weight = HeightWeight(splat.g, plaza.mask.b, height, _Sharpness.y);
                    Mix(result, height, plaza, weight);
                    natural -= weight * natural;
                }

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = result.albedo;
                surfaceData.alpha = 1;
                surfaceData.smoothness = saturate(result.mask.g * _Paint.w);
                surfaceData.occlusion = lerp(1.0h, result.mask.r, _Paint.z);
                surfaceData.normalTS = normalize(result.normalTS);
                surfaceData.emission = result.emission + SAMPLE_TEXTURE2D(_PaintGlow, sampler_PaintMap, fieldUV).rgb * (_GlowB.y * natural);

                #if !defined(_ENV_LOW)
                    // Grit that glints now and then, and a tiling detail normal for close-ups that fades at play zoom.
                    float2 cell = floor(xz * 23.0);
                    half seed = frac(sin(dot(cell, float2(12.9898, 78.233))) * 43758.5453);
                    half twinkle = saturate(sin(_Time.y * 2.3 + seed * 40.0) * 5.0 - 4.0);
                    surfaceData.emission += _SparkleColour.rgb * (result.mask.a * twinkle * _GlowB.z);

                    float detailTile = _TileB.z;
                    float2 detailUV = xz / detailTile;
                    float footprint = max(length(gx), length(gy)) / detailTile;
                    half fade = 1.0h - smoothstep(_DetailFade.x, _DetailFade.y, footprint);
                    UNITY_BRANCH
                    if (fade > 0.01h)
                    {
                        half3 detail = UnpackNormalScale(SAMPLE_TILE(_DetailNormal, detailUV, gx / detailTile, gy / detailTile), _Blend.w * fade);
                        surfaceData.normalTS = normalize(half3(surfaceData.normalTS.xy + detail.xy, surfaceData.normalTS.z * detail.z));
                    }
                #endif

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                half3 bitangent = input.tangentWS.w * cross(input.normalWS, input.tangentWS.xyz);
                inputData.normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(surfaceData.normalTS, half3x3(input.tangentWS.xyz, bitangent, input.normalWS)));
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
                inputData.vertexLighting = half3(0, 0, 0);
                inputData.bakedGI = SampleSH(inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = 1;
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex GroundDepthVertex
            #pragma fragment GroundDepthFragment

            float4 GroundDepthVertex(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }

            half GroundDepthFragment(float4 positionCS : SV_POSITION) : SV_Target
            {
                return positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex GroundNormalsVertex
            #pragma fragment GroundNormalsFragment
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            struct NormalsVaryings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
            };

            NormalsVaryings GroundNormalsVertex(float4 positionOS : POSITION, float3 normalOS : NORMAL)
            {
                NormalsVaryings output;
                output.positionCS = TransformObjectToHClip(positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(normalOS);
                return output;
            }

            half4 GroundNormalsFragment(NormalsVaryings input) : SV_Target
            {
                #if defined(_GBUFFER_NORMALS_OCT)
                    float3 normalWS = normalize(input.normalWS);
                    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                    float2 remapped = saturate(octNormalWS * 0.5 + 0.5);
                    return half4(PackFloat2To888(remapped), 0.0);
                #else
                    return half4(NormalizeNormalPerPixel(input.normalWS), 0.0);
                #endif
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
