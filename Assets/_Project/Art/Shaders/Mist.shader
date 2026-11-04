// A sheet of ground mist (ArtSource README round 24: Mist_Layer_A/B/C are grey density maps). Sampled in world space
// so the bank pattern does not stretch with the sheet, scrolled by the wind, tinted by the theme and faded softly where
// it touches the ground or a model (High: the scene depth; Low leaves the fade out). Unlit, alpha blended, no shadows.
Shader "Game/Mist"
{
    Properties
    {
        _MainTex ("Density (R)", 2D) = "black" {}
        _Color ("Tint", Color) = (0.55, 0.6, 0.75, 1)
        _Alpha ("Alpha", Range(0, 1)) = 0.16
        _Tile ("World size of one tile", Float) = 7
        _Scroll ("Scroll tiles/s", Vector) = (0.012, 0.003, 0, 0)
        _SoftDistance ("Soft fade against geometry", Float) = 0.3
        _Glow ("Self light", Range(0, 2)) = 0.6
        [Toggle(_MIST_SOFT)] _Soft ("Soft fade (depth texture)", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-50" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Mist"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex MistVertex
            #pragma fragment MistFragment
            #pragma multi_compile_local _ _MIST_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "VfxCommon.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half4 _Color;
            half _Alpha;
            float _Tile;
            float4 _Scroll;
            float _SoftDistance;
            half _Glow;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half3 ambient : TEXCOORD1;
#if defined(_MIST_SOFT)
                float4 screenPos : TEXCOORD2;
                float eyeDepth : TEXCOORD3;
#endif
            };

            Varyings MistVertex(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.uv = positions.positionWS.xz / max(_Tile, 0.01) + _Scroll.xy * _Time.y;
                output.ambient = SampleSH(half3(0, 1, 0));
#if defined(_MIST_SOFT)
                output.screenPos = ComputeScreenPos(positions.positionCS);
                output.eyeDepth = -positions.positionVS.z;
#endif
                return output;
            }

            half4 MistFragment(Varyings input) : SV_Target
            {
                half density = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).r;
                half alpha = density * _Alpha;
#if defined(_MIST_SOFT)
                alpha *= VfxSoftFade(input.screenPos, input.eyeDepth, _SoftDistance);
#endif
                Light sun = GetMainLight();
                half3 colour = _Color.rgb * (sun.color * 0.35h + input.ambient + _Glow);
                return half4(colour, alpha);
            }
            ENDHLSL
        }
    }
}
