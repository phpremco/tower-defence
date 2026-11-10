// Camera-facing sprites drawn as one mesh: each quad's four corners share the sprite's centre (POSITION, world space),
// UV0 says which corner, UV1 holds (size in metres, turn in radians, twinkle phase, unused). The vertex shader spreads
// the corners in view space, so the batch needs no per-sprite objects. Additive (dust, near stars, sparks, glints) or
// alpha-blended (smoke and steam) by the blend factors the code sets. Twinkle runs on the cutscene clock (_Clock).
Shader "Game/CutsceneSprites"
{
    Properties
    {
        _MainTex ("Sprite", 2D) = "white" {}
        [HDR] _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Twinkle ("Twinkle", Range(0, 1)) = 0
        _TwinkleRate ("Twinkle rate", Float) = 3
        _Clock ("Cutscene clock", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination blend", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend [_SrcBlend] [_DstBlend]
        ZWrite Off
        Cull Off

        Pass
        {
            Name "CutsceneSprites"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex SpriteVertex
            #pragma fragment SpriteFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half4 _Tint;
            half _Twinkle;
            half _TwinkleRate;
            float _Clock;
            half _SrcBlend;
            half _DstBlend;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 corner : TEXCOORD0;
                float4 shape : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            Varyings SpriteVertex(Attributes input)
            {
                Varyings output;
                float3 centre = TransformObjectToWorld(input.positionOS.xyz);
                float3 viewCentre = TransformWorldToView(centre);
                float s, c;
                sincos(input.shape.y, s, c);
                float2 corner = float2(input.corner.x * c - input.corner.y * s, input.corner.x * s + input.corner.y * c);
                viewCentre.xy += corner * input.shape.x;
                output.positionCS = TransformWViewToHClip(viewCentre);
                half twinkle = 1.0h - _Twinkle * (half)(0.5 + 0.5 * sin(_Clock * _TwinkleRate * (0.6 + input.shape.z) + input.shape.z * 40.0));
                output.color = input.color * _Tint * twinkle;
                output.uv = input.corner * 0.5 + 0.5;
                return output;
            }

            half4 SpriteFragment(Varyings input) : SV_Target
            {
                half4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                return texel * input.color;
            }
            ENDHLSL
        }
    }
}
