// The lit surface of everything that acts on the field: towers, gates, the hero, drones and creatures. Standard URP
// physically based shading plus a rim of light along the silhouette, in a colour per side (cool for the colony, warm
// for the aliens), so a unit lifts off the dark ground at game zoom. Scenery keeps the plain Lit shader and stays back.
// Imported (Blender) models add a packed mask (R occlusion, G smoothness, B and A where the two tiling detail normals
// of their material families apply) and those detail normals, which fade out as their tiles shrink below a few
// pixels so pores never shimmer at game zoom. An overlay colour (hit flash, frost) lerps the whole surface towards
// one colour without swapping materials. Old procedural models set none of it and render as before.
Shader "Game/UnitLit"
{
    Properties
    {
        _BaseMap ("Albedo", 2D) = "white" {}
        _BaseColor ("Colour", Color) = (1, 1, 1, 1)
        _Cutoff ("Alpha cutoff", Range(0, 1)) = 0.5
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0
        _BumpScale ("Normal strength", Float) = 1
        _BumpMap ("Normal map", 2D) = "bump" {}
        _EmissionColor ("Emission", Color) = (0, 0, 0, 1)
        _EmissionMap ("Emission map", 2D) = "white" {}
        _RimColor ("Rim colour", Color) = (0.55, 0.8, 1, 1)
        _RimPower ("Rim power", Range(0.5, 8)) = 2.4
        _RimStrength ("Rim strength", Range(0, 3)) = 0.55
        _WaveAmplitude ("Wave amplitude", Float) = 0
        _WaveVector ("Wave vector (xyz) and speed (w)", Vector) = (0, 0, 0, 0)
        _WaveRamp ("Wave envelope ramp", Vector) = (0, 0, 0, 1)
        _WaveDirection ("Wave direction (xyz) and phase (w)", Vector) = (0, 1, 0, 0)
        _MaskMap ("Mask (R occlusion, G smoothness, B detail, A detail 2)", 2D) = "white" {}
        _OcclusionStrength ("Occlusion strength", Range(0, 1)) = 0.5
        _DetailNormalMap ("Detail normal", 2D) = "bump" {}
        _DetailNormalScale ("Detail strength", Range(0, 2)) = 0.8
        _DetailNormalMap2 ("Detail normal 2", 2D) = "bump" {}
        _DetailNormalScale2 ("Detail strength 2", Range(0, 2)) = 0.8
        _DetailFade ("Detail fade (tiles per pixel: full, gone)", Vector) = (0.004, 0.02, 0, 0)
        [HDR] _OverlayColor ("Overlay colour (a: amount)", Color) = (1, 1, 1, 0)
        _RimAlpha ("Rim adds coverage (see-through surfaces)", Range(0, 2)) = 0
        [HideInInspector] _SrcBlend ("Source blend", Float) = 1
        [HideInInspector] _DstBlend ("Destination blend", Float) = 0
        [HideInInspector] _ZWrite ("Depth write", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Lit" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        half4 _BaseColor;
        half4 _EmissionColor;
        half4 _RimColor;
        half _Cutoff;
        half _Smoothness;
        half _Metallic;
        half _BumpScale;
        half _RimPower;
        half _RimStrength;
        half _RimAlpha;
        float _WaveAmplitude;
        float4 _WaveVector;
        float4 _WaveRamp;
        float4 _WaveDirection;
        float4 _DetailNormalMap_ST;
        float4 _DetailNormalMap2_ST;
        float4 _DetailFade;
        half4 _OverlayColor;
        half _OcclusionStrength;
        half _DetailNormalScale;
        half _DetailNormalScale2;
        CBUFFER_END

        TEXTURE2D(_MaskMap);
        SAMPLER(sampler_MaskMap);
        TEXTURE2D(_DetailNormalMap);
        SAMPLER(sampler_DetailNormalMap);
        TEXTURE2D(_DetailNormalMap2);
        SAMPLER(sampler_DetailNormalMap2);

        // The surface wave (see SurfaceWave.cs): a sine travelling along the wave vector, scaled by a squared ramp
        // over the part, moving vertices along the wave direction. The normal tilts with the slope so the ripple
        // catches light. Off (amplitude zero) for every material that does not ask for it.
        void ApplyWave(inout float3 positionOS, inout float3 normalOS)
        {
            if (_WaveAmplitude <= 0.0)
            {
                return;
            }

            float ramp = saturate(dot(positionOS, _WaveRamp.xyz) + _WaveRamp.w);
            float envelope = ramp * ramp;
            float angle = dot(positionOS, _WaveVector.xyz) - _Time.y * _WaveVector.w + _WaveDirection.w;
            float s;
            float c;
            sincos(angle, s, c);
            float3 direction = _WaveDirection.xyz;
            positionOS += direction * (_WaveAmplitude * envelope * s);
            float3 gradient = _WaveVector.xyz * (_WaveAmplitude * envelope * c);
            normalOS = normalize(normalOS - gradient * dot(normalOS, direction));
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex UnitVertex
            #pragma fragment UnitFragment

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _MASKMAP
            #pragma shader_feature_local_fragment _DETAIL
            #pragma shader_feature_local_fragment _DETAIL2

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // A tiling detail normal on top of the model's own, by whiteout blending, weighted by its mask channel and
            // faded out as a tile shrinks towards a few pixels: at game zoom the mips would only average it into noise.
            half3 BlendDetail(half3 normalTS, TEXTURE2D_PARAM(detailMap, detailSampler), float2 uv, float4 tiling, half strength)
            {
                float2 detailUV = uv * tiling.xy + tiling.zw;
                float footprint = max(length(ddx(detailUV)), length(ddy(detailUV)));
                half fade = 1.0h - smoothstep(_DetailFade.x, _DetailFade.y, footprint);
                half3 detail = UnpackNormalScale(SAMPLE_TEXTURE2D(detailMap, detailSampler, detailUV), strength * fade);
                return normalize(half3(normalTS.xy + detail.xy, normalTS.z * detail.z));
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 texcoord : TEXCOORD0;
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

            Varyings UnitVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                ApplyWave(input.positionOS.xyz, input.normalOS);
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normals = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                output.normalWS = normals.normalWS;
                half sign = input.tangentOS.w * GetOddNegativeScale();
                output.tangentWS = half4(normals.tangentWS, sign);
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 UnitFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 albedoAlpha = SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
                half alpha = Alpha(albedoAlpha.a, _BaseColor, _Cutoff);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb;
                surfaceData.alpha = alpha;
                surfaceData.metallic = _Metallic;
                surfaceData.specular = half3(0, 0, 0);
                surfaceData.smoothness = _Smoothness;
                surfaceData.occlusion = 1;
                surfaceData.emission = SampleEmission(input.uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));
                surfaceData.normalTS = SampleNormal(input.uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);

                // The albedo already carries the baked occlusion, so the mask's share only shapes the ambient light.
                half4 mask = half4(1.0h, 1.0h, 1.0h, 1.0h);
                #if defined(_MASKMAP)
                    mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, input.uv);
                    surfaceData.smoothness = mask.g * _Smoothness;
                    surfaceData.occlusion = lerp(1.0h, mask.r, _OcclusionStrength);
                #endif
                #if defined(_DETAIL)
                    surfaceData.normalTS = BlendDetail(surfaceData.normalTS, TEXTURE2D_ARGS(_DetailNormalMap, sampler_DetailNormalMap), input.uv, _DetailNormalMap_ST, _DetailNormalScale * mask.b);
                #endif
                #if defined(_DETAIL2)
                    surfaceData.normalTS = BlendDetail(surfaceData.normalTS, TEXTURE2D_ARGS(_DetailNormalMap2, sampler_DetailNormalMap2), input.uv, _DetailNormalMap2_ST, _DetailNormalScale2 * mask.a);
                #endif

                // A hit flash or a frost: the whole surface leans towards one colour and glows in it.
                surfaceData.albedo = lerp(surfaceData.albedo, saturate(_OverlayColor.rgb), _OverlayColor.a);
                surfaceData.emission += _OverlayColor.rgb * _OverlayColor.a;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                #if defined(_NORMALMAP)
                    half3 bitangent = input.tangentWS.w * cross(input.normalWS, input.tangentWS.xyz);
                    inputData.normalWS = TransformTangentToWorld(surfaceData.normalTS, half3x3(input.tangentWS.xyz, bitangent, input.normalWS));
                #else
                    inputData.normalWS = input.normalWS;
                #endif
                inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
                inputData.viewDirectionWS = viewDirWS;
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
                inputData.vertexLighting = half3(0, 0, 0);
                inputData.bakedGI = SampleSH(inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);

                // The rim: strongest where the surface turns away from the camera, and a little more on the upper
                // half of a shape, as if the sky lit it, so the top of every unit carries a bright edge.
                half facing = saturate(dot(inputData.normalWS, viewDirWS));
                half rim = pow(1.0h - facing, _RimPower) * _RimStrength;
                half fromAbove = 0.5h + 0.5h * saturate(inputData.normalWS.y * 0.5h + 0.5h);
                color.rgb += _RimColor.rgb * rim * fromAbove;

                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                // A see-through surface (ice) is clear face-on and dense along its silhouette, as real ice reads.
                color.a = saturate(alpha + rim * fromAbove * _RimAlpha);
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
            #pragma target 3.0
            #pragma vertex ShadowPassVertexWave
            #pragma fragment ShadowPassFragment
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"

            Varyings ShadowPassVertexWave(Attributes input)
            {
                ApplyWave(input.positionOS.xyz, input.normalOS);
                return ShadowPassVertex(input);
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
            #pragma target 3.0
            #pragma vertex DepthOnlyVertexWave
            #pragma fragment DepthOnlyFragment
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"

            Varyings DepthOnlyVertexWave(Attributes input)
            {
                float3 normalOS = float3(0.0, 1.0, 0.0);
                ApplyWave(input.position.xyz, normalOS);
                return DepthOnlyVertex(input);
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
            #pragma target 3.0
            #pragma vertex DepthNormalsVertexWave
            #pragma fragment DepthNormalsFragment
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"

            Varyings DepthNormalsVertexWave(Attributes input)
            {
                ApplyWave(input.positionOS.xyz, input.normal);
                return DepthNormalsVertex(input);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
