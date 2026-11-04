// The Burrower eyes of the underground cutscene (README round 26): many eyes in one combined mesh, each opened, closed
// and turned on the GPU by its EyeRig vertex colours (R upper lid, G lower lid, B eyeball and cornea). Every vertex
// carries its eye's pivot and pair (TEXCOORD1), the lid hinge with the mirror sign of its instance (TEXCOORD2) and the
// eye's up axis (TEXCOORD3), all in the mesh's own space; the pair's blink, look and glow come from the global
// _BurrowerEyeState array that EyeField writes once a frame. A mirrored eye turns its lids and pitch the other way, so a
// pair closes and looks together. Lit like the units (URP PBR) with the iris glowing from the emission map; the
// cornea is the same shader alpha blended.
Shader "Game/BurrowerEye"
{
    Properties
    {
        _BaseMap ("Albedo", 2D) = "white" {}
        _BaseColor ("Colour (a: cornea opacity)", Color) = (1, 1, 1, 1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.6
        _BumpMap ("Normal map", 2D) = "bump" {}
        _BumpScale ("Normal strength", Float) = 1
        _MaskMap ("Mask (R occlusion, G smoothness)", 2D) = "white" {}
        _OcclusionStrength ("Occlusion strength", Range(0, 1)) = 0.6
        _EmissionMap ("Emission map", 2D) = "black" {}
        [HDR] _EmissionColor ("Iris glow", Color) = (4, 4, 4, 1)
        _UpperLid ("Upper lid degrees at a full blink", Float) = 32
        _LowerLid ("Lower lid degrees at a full blink", Float) = 22
        _LookYaw ("Look degrees left and right", Float) = 22
        _LookPitch ("Look degrees up and down", Float) = 15
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination blend", Float) = 0
        [Toggle] _ZWrite ("Depth write", Float) = 1
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
        float _UpperLid;
        float _LowerLid;
        float _LookYaw;
        float _LookPitch;
        half _SrcBlend;
        half _DstBlend;
        half _ZWrite;
        CBUFFER_END

        // One entry per eye pair: x blink (0 open, 1 shut), y look left/right and z up/down (-1..1), w glow share.
        #define EYE_PAIRS 512
        float4 _BurrowerEyeState[EYE_PAIRS];

        float3 Turn(float3 v, float3 axis, float degrees)
        {
            float s;
            float c;
            sincos(radians(degrees), s, c);
            return v * c + cross(axis, v) * s + axis * dot(axis, v) * (1.0 - c);
        }

        // Turns the lids about the hinge and the eyeball about its up axis and the hinge, all through the pivot.
        void ApplyEyeRig(inout float3 positionOS, inout float3 normalOS, half4 rig, float4 pivotPair, float4 hingeMirror, float3 up, out float glow)
        {
            float4 state = _BurrowerEyeState[(int)pivotPair.w];
            float3 hinge = normalize(hingeMirror.xyz);
            float mirror = hingeMirror.w;
            float3 local = positionOS - pivotPair.xyz;
            float3 normal = normalOS;
            float lid = (_UpperLid * rig.r - _LowerLid * rig.g) * state.x * mirror;
            local = Turn(local, hinge, lid);
            normal = Turn(normal, hinge, lid);
            float yaw = _LookYaw * state.y * rig.b;
            float pitch = _LookPitch * state.z * rig.b * mirror;
            float3 upAxis = normalize(up);
            local = Turn(Turn(local, upAxis, yaw), hinge, pitch);
            normal = Turn(Turn(normal, upAxis, yaw), hinge, pitch);
            positionOS = pivotPair.xyz + local;
            normalOS = normal;
            glow = state.w;
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
            #pragma target 3.5
            #pragma vertex EyeVertex
            #pragma fragment EyeFragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            TEXTURE2D(_MaskMap); SAMPLER(sampler_MaskMap);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 texcoord : TEXCOORD0;
                float4 pivotPair : TEXCOORD1;
                float4 hingeMirror : TEXCOORD2;
                float4 up : TEXCOORD3;
                half4 rig : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half4 tangentWS : TEXCOORD3;
                half fogFactor : TEXCOORD4;
                half glow : TEXCOORD5;
            };

            Varyings EyeVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                float glow;
                ApplyEyeRig(input.positionOS.xyz, input.normalOS, input.rig, input.pivotPair, input.hingeMirror, input.up.xyz, glow);
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normals = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                output.normalWS = normals.normalWS;
                output.tangentWS = half4(normals.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                output.glow = glow;
                return output;
            }

            half4 EyeFragment(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half4 mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, input.uv);
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = albedo.rgb;
                surface.alpha = _BaseColor.a;
                surface.smoothness = mask.g * _Smoothness;
                surface.occlusion = lerp(1.0h, mask.r, _OcclusionStrength);
                surface.normalTS = normalTS;
                surface.emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb * _EmissionColor.rgb * input.glow;

                InputData data = (InputData)0;
                data.positionWS = input.positionWS;
                data.positionCS = input.positionCS;
                half3 bitangent = input.tangentWS.w * cross(input.normalWS, input.tangentWS.xyz);
                data.normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent, input.normalWS)));
                data.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                data.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                data.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
                data.bakedGI = SampleSH(data.normalWS);
                data.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                data.shadowMask = half4(1, 1, 1, 1);

                half4 colour = UniversalFragmentPBR(data, surface);
                colour.rgb = MixFog(colour.rgb, data.fogCoord);
                colour.a = surface.alpha;
                return colour;
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
            #pragma vertex DepthVertex
            #pragma fragment DepthFragment

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 pivotPair : TEXCOORD1;
                float4 hingeMirror : TEXCOORD2;
                float4 up : TEXCOORD3;
                half4 rig : COLOR;
            };

            float4 DepthVertex(Attributes input) : SV_POSITION
            {
                float glow;
                ApplyEyeRig(input.positionOS.xyz, input.normalOS, input.rig, input.pivotPair, input.hingeMirror, input.up.xyz, glow);
                return TransformObjectToHClip(input.positionOS.xyz);
            }

            half DepthFragment() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
