// Gravemaw's 3D fissure (ArtSource README round 34): the rock of the modular kit, bent along the crack path by the view.
// It tears open behind the boss: every vertex carries its distance along the crack (UV1.x) and its offset to the closed
// state, where the lips lie flat and the gap is shut on the centre line (UV2 = dx, dy; UV3.x = dz, in the mesh's own
// space). A vertex is closed ahead of the open front, opens over _OpenLength behind it, and _Close folds the whole crack
// shut again as it heals. URP physically based shading with the kit's maps (albedo, normal, emission, mask: R occlusion,
// G smoothness); the molten core glows from the emission map while the crack is hot and cools from the top down (vertex
// colour R is the depth, so the very bottom glows longest). Vertex alpha fades the skirt into the ground (dithered).
// Drawn after the two depth punch stages (Game/DepthPunch), which clears the ground's depth over the gap so the walls below show.
Shader "Game/FissureRock"
{
    Properties
    {
        _BaseMap ("Albedo", 2D) = "white" {}
        _BaseColor ("Colour", Color) = (1, 1, 1, 1)
        _LipTint ("Lip and skirt tint (V above 0.4)", Color) = (1, 1, 1, 1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _BumpScale ("Normal strength", Float) = 1
        _BumpMap ("Normal map", 2D) = "bump" {}
        [HDR] _EmissionColor ("Emission", Color) = (2.6, 1.1, 0.26, 1)
        _EmissionMap ("Emission map", 2D) = "black" {}
        _MaskMap ("Mask (R occlusion, G smoothness)", 2D) = "white" {}
        _OcclusionStrength ("Occlusion strength", Range(0, 1)) = 0.8
        _Front ("Open front (m along the crack)", Float) = 1000
        _OpenLength ("Opening ramp behind the front (m)", Float) = 2
        _Close ("Closed share of the whole crack", Range(0, 1)) = 0
        _Heat ("Heat left after the front passed", Range(0, 1)) = 1
        _HotLength ("Fresh heat behind the front (m)", Float) = 6
        _DepthHold ("How much longer the bottom keeps its heat", Range(0, 1)) = 0.6
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Lit" "Queue" = "Geometry+3" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        half4 _BaseColor;
        half4 _LipTint;
        half4 _EmissionColor;
        half _Smoothness;
        half _BumpScale;
        half _OcclusionStrength;
        float _Front;
        float _OpenLength;
        float _Close;
        half _Heat;
        float _HotLength;
        half _DepthHold;
        CBUFFER_END

        // 0 open, 1 closed: shut ahead of the front, tearing open behind it, everything folding shut as the crack heals.
        float ClosedAt(float along)
        {
            float closed = 1.0 - saturate((_Front - along) / max(_OpenLength, 0.001));
            return smoothstep(0.0, 1.0, max(closed, _Close));
        }

        float3 Revealed(float3 positionOS, float along, float2 closeXY, float closeZ)
        {
            return positionOS + float3(closeXY.x, closeXY.y, closeZ) * ClosedAt(along);
        }

        // A stable screen-space pattern for the dithered fade of the skirt into the ground.
        half Dither(float2 pixel)
        {
            return (half)frac(52.9829189 * frac(dot(pixel, float2(0.06711056, 0.00583715))));
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex RockVertex
            #pragma fragment RockFragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MaskMap);
            SAMPLER(sampler_MaskMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 texcoord : TEXCOORD0;
                float2 reveal : TEXCOORD1;
                float2 closeXY : TEXCOORD2;
                float2 closeZ : TEXCOORD3;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half4 tangentWS : TEXCOORD3;
                half4 color : TEXCOORD4;
                float along : TEXCOORD5;
                half fogFactor : TEXCOORD6;
            };

            Varyings RockVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexNormalInputs normals = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                float3 positionWS = TransformObjectToWorld(Revealed(input.positionOS.xyz, input.reveal.x, input.closeXY, input.closeZ.x));
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                output.normalWS = normals.normalWS;
                output.tangentWS = half4(normals.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                output.color = input.color;
                output.along = input.reveal.x;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 RockFragment(Varyings input) : SV_Target
            {
                clip(input.color.a - Dither(input.positionCS.xy) * 0.999h);

                half4 mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, input.uv);
                half lip = smoothstep(0.38h, 0.45h, (half)input.uv.y);
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb * lerp(half3(1, 1, 1), _LipTint.rgb, lip);
                surfaceData.alpha = 1;
                surfaceData.smoothness = mask.g * _Smoothness;
                surfaceData.occlusion = lerp(1.0h, mask.r, _OcclusionStrength);
                surfaceData.normalTS = SampleNormal(input.uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);

                // Fresh heat just behind the front, the left-over heat everywhere, and the bottom of the gap cooling last.
                half fresh = (half)saturate(1.0 - (_Front - input.along) / max(_HotLength, 0.001));
                half heat = max(_Heat, fresh);
                heat = lerp(heat, sqrt(heat), input.color.r * _DepthHold);
                half3 glow = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb * _EmissionColor.rgb;
                surfaceData.emission = glow * heat * (1.0h - (half)ClosedAt(input.along));

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
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex RockShadowVertex
            #pragma fragment RockShadowFragment
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 reveal : TEXCOORD1;
                float2 closeXY : TEXCOORD2;
                float2 closeZ : TEXCOORD3;
            };

            float4 RockShadowVertex(ShadowAttributes input) : SV_POSITION
            {
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 positionWS = TransformObjectToWorld(Revealed(input.positionOS.xyz, input.reveal.x, input.closeXY, input.closeZ.x));
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return positionCS;
            }

            half4 RockShadowFragment() : SV_Target
            {
                return 0;
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
            #pragma vertex RockDepthVertex
            #pragma fragment RockDepthFragment

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                float2 reveal : TEXCOORD1;
                float2 closeXY : TEXCOORD2;
                float2 closeZ : TEXCOORD3;
            };

            float4 RockDepthVertex(DepthAttributes input) : SV_POSITION
            {
                return TransformObjectToHClip(Revealed(input.positionOS.xyz, input.reveal.x, input.closeXY, input.closeZ.x));
            }

            half RockDepthFragment(float4 positionCS : SV_POSITION) : SV_Target
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
            #pragma vertex RockNormalsVertex
            #pragma fragment RockNormalsFragment
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            struct NormalsAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 reveal : TEXCOORD1;
                float2 closeXY : TEXCOORD2;
                float2 closeZ : TEXCOORD3;
            };

            struct NormalsVaryings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
            };

            NormalsVaryings RockNormalsVertex(NormalsAttributes input)
            {
                NormalsVaryings output;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformObjectToHClip(Revealed(input.positionOS.xyz, input.reveal.x, input.closeXY, input.closeZ.x));
                return output;
            }

            half4 RockNormalsFragment(NormalsVaryings input) : SV_Target
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

    FallBack Off
}
