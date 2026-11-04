// The lit surface of the imported environment kit and decor (kerbs, pads, plinths, banks, cliffs, rocks, crystals,
// plants, scatter). URP physically based shading with the Blender maps (albedo, normal, emission and the packed mask:
// R occlusion, G smoothness, B where the tiling detail normal applies), drawn instanced in large numbers, without the
// units' rim light so the scenery stays back. Flora bends in the wind from its "Sway" vertex colour (R bend weight from
// root to tip, G phase, B flutter) in world space, so it keeps swaying when thousands are drawn in one call; the
// shadow, depth and normals passes bend the same way.
Shader "Game/EnvironmentLit"
{
    Properties
    {
        _BaseMap ("Albedo", 2D) = "white" {}
        _BaseColor ("Colour", Color) = (1, 1, 1, 1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _BumpScale ("Normal strength", Float) = 1
        _BumpMap ("Normal map", 2D) = "bump" {}
        _EmissionColor ("Emission", Color) = (0, 0, 0, 1)
        _EmissionMap ("Emission map", 2D) = "white" {}
        _MaskMap ("Mask (R occlusion, G smoothness, B detail)", 2D) = "white" {}
        _OcclusionStrength ("Occlusion strength", Range(0, 1)) = 0.5
        _DetailNormalMap ("Detail normal", 2D) = "bump" {}
        _DetailNormalScale ("Detail strength", Range(0, 2)) = 0.6
        _DetailFade ("Detail fade (tiles per pixel: full, gone)", Vector) = (0.004, 0.02, 0, 0)
        _SwayScale ("Sway scale", Float) = 1
        _PadCalmGlow ("Pad: glow share while not placing", Range(0, 1)) = 0.12
        _PadCalmAlbedo ("Pad: albedo share while not placing", Range(0, 1)) = 0.7
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Lit" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        half4 _BaseColor;
        half4 _EmissionColor;
        half _Smoothness;
        half _BumpScale;
        half _OcclusionStrength;
        half _DetailNormalScale;
        float4 _DetailNormalMap_ST;
        float4 _DetailFade;
        float _SwayScale;
        half _PadCalmGlow;
        half _PadCalmAlbedo;
        CBUFFER_END

        // Set once per level from the environment set (EnvironmentInstancer): direction (xyz) and tip amplitude (w);
        // speed (x) and flutter amplitude (y).
        float4 _EnvWind;
        float4 _EnvWindParams;

        // Bends a vertex in world space: the tip follows the wind, a little flutter along the normal. The instance's own
        // position shifts the phase, so neighbouring plants never move in step.
        float3 ApplySway(float3 positionWS, float3 normalWS, half4 sway)
        {
            #if defined(_SWAY)
                float3 pivot = float3(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13, UNITY_MATRIX_M._m23);
                float phase = sway.g * 6.2831853 + dot(pivot.xz, float2(0.73, 1.37));
                float t = _Time.y * _EnvWindParams.x;
                float bend = sway.r * _EnvWind.w * _SwayScale * sin(t + phase);
                float flutter = sway.b * _EnvWindParams.y * _SwayScale * sin(_Time.y * 7.0 + phase * 2.1);
                positionWS += _EnvWind.xyz * bend + normalWS * flutter;
            #endif
            return positionWS;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex EnvVertex
            #pragma fragment EnvFragment

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _MASKMAP
            #pragma shader_feature_local_fragment _DETAIL
            #pragma shader_feature_local_vertex _SWAY
            #pragma shader_feature_local_fragment _PAD_HIGHLIGHT

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MaskMap);
            SAMPLER(sampler_MaskMap);
            TEXTURE2D(_DetailNormalMap);
            SAMPLER(sampler_DetailNormalMap);

            // Build pads (BuildPadHighlight): one texel per cell, red where a tower may go; the field rect; and how far
            // the placing highlight is up (0 calm, 1 placing).
            TEXTURE2D(_EnvPadMask);
            SAMPLER(sampler_EnvPadMask);
            float4 _EnvPadRect;
            float _EnvPadHighlight;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 texcoord : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half4 tangentWS : TEXCOORD3;
                half fogFactor : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings EnvVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexNormalInputs normals = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                float3 positionWS = ApplySway(TransformObjectToWorld(input.positionOS.xyz), normals.normalWS, input.color);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                output.normalWS = normals.normalWS;
                half sign = input.tangentOS.w * GetOddNegativeScale();
                output.tangentWS = half4(normals.tangentWS, sign);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 EnvFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;
                surfaceData.alpha = 1;
                surfaceData.smoothness = _Smoothness;
                surfaceData.occlusion = 1;
                surfaceData.emission = SampleEmission(input.uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));
                surfaceData.normalTS = SampleNormal(input.uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);

                half4 mask = half4(1.0h, 1.0h, 1.0h, 1.0h);
                #if defined(_MASKMAP)
                    mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, input.uv);
                    surfaceData.smoothness = mask.g * _Smoothness;
                    surfaceData.occlusion = lerp(1.0h, mask.r, _OcclusionStrength);
                #endif
                #if defined(_DETAIL)
                    // Fades out as a tile shrinks towards a few pixels, so the detail only shows in close-ups.
                    float2 detailUV = input.uv * _DetailNormalMap_ST.xy + _DetailNormalMap_ST.zw;
                    float footprint = max(length(ddx(detailUV)), length(ddy(detailUV)));
                    half fade = 1.0h - smoothstep(_DetailFade.x, _DetailFade.y, footprint);
                    half3 detail = UnpackNormalScale(SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailNormalMap, detailUV), _DetailNormalScale * mask.b * fade);
                    surfaceData.normalTS = normalize(half3(surfaceData.normalTS.xy + detail.xy, surfaceData.normalTS.z * detail.z));
                #endif

                #if defined(_PAD_HIGHLIGHT)
                    // Calm by default: a dark tile that sits in the ground. Its outline lights up only while the player
                    // is choosing a build spot, and only on free cells (a pad under a tower stays calm).
                    float2 padUV = (input.positionWS.xz - _EnvPadRect.xy) * _EnvPadRect.zw;
                    half free = SAMPLE_TEXTURE2D(_EnvPadMask, sampler_EnvPadMask, padUV).r;
                    half lit = saturate(_EnvPadHighlight) * free;
                    surfaceData.emission *= lerp(_PadCalmGlow, 1.0h, lit);
                    surfaceData.albedo *= lerp(_PadCalmAlbedo, 1.0h, lit);
                #endif

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                #if defined(_NORMALMAP)
                    half3 bitangent = input.tangentWS.w * cross(input.normalWS, input.tangentWS.xyz);
                    inputData.normalWS = TransformTangentToWorld(surfaceData.normalTS, half3x3(input.tangentWS.xyz, bitangent, input.normalWS));
                #else
                    inputData.normalWS = input.normalWS;
                #endif
                inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
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
            #pragma vertex EnvShadowVertex
            #pragma fragment EnvShadowFragment
            #pragma shader_feature_local_vertex _SWAY
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 EnvShadowVertex(ShadowAttributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 positionWS = ApplySway(TransformObjectToWorld(input.positionOS.xyz), normalWS, input.color);
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

            half4 EnvShadowFragment() : SV_Target
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
            #pragma vertex EnvDepthVertex
            #pragma fragment EnvDepthFragment
            #pragma shader_feature_local_vertex _SWAY
            #pragma multi_compile_instancing

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 EnvDepthVertex(DepthAttributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                return TransformWorldToHClip(ApplySway(TransformObjectToWorld(input.positionOS.xyz), normalWS, input.color));
            }

            half EnvDepthFragment(float4 positionCS : SV_POSITION) : SV_Target
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
            #pragma vertex EnvNormalsVertex
            #pragma fragment EnvNormalsFragment
            #pragma shader_feature_local_vertex _SWAY
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing

            struct NormalsAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct NormalsVaryings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
            };

            NormalsVaryings EnvNormalsVertex(NormalsAttributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                NormalsVaryings output;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplySway(TransformObjectToWorld(input.positionOS.xyz), output.normalWS, input.color));
                return output;
            }

            half4 EnvNormalsFragment(NormalsVaryings input) : SV_Target
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
