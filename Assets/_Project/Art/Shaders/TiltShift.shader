// Tilt-shift depth of field for the fixed orthographic camera: the band around the middle of the screen stays
// sharp and the picture softens towards the top and bottom, like a photo of a model landscape. A screen mask
// does the work, because the pipeline's depth of field cannot read depth from an orthographic camera. One pass,
// thirteen taps, run by the full-screen pass renderer feature before post-processing.
Shader "Game/TiltShift"
{
    Properties
    {
        _Band ("Sharp half height", Range(0, 0.5)) = 0.22
        _Feather ("Feather", Range(0.01, 0.5)) = 0.3
        _Radius ("Blur radius in texels", Range(0, 12)) = 3
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        Cull Off
        ZTest Always

        Pass
        {
            Name "TiltShift"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment TiltShiftFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Band;
            float _Feather;
            float _Radius;

            static const float2 Taps[12] =
            {
                float2(-0.326, -0.406), float2(-0.840, -0.074), float2(-0.696, 0.457), float2(-0.203, 0.621),
                float2(0.962, -0.195), float2(0.473, -0.480), float2(0.519, 0.767), float2(0.185, -0.893),
                float2(0.507, 0.064), float2(0.896, 0.412), float2(-0.322, -0.933), float2(-0.792, -0.598)
            };

            half4 TiltShiftFragment(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float mask = smoothstep(_Band, _Band + _Feather, abs(uv.y - 0.5));
                half4 centre = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                if (mask <= 0.001)
                {
                    return centre;
                }

                float2 spread = _BlitTexture_TexelSize.xy * (_Radius * mask);
                half4 sum = centre;
                [unroll]
                for (int i = 0; i < 12; i++)
                {
                    sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + Taps[i] * spread);
                }

                return sum / 13.0h;
            }
            ENDHLSL
        }
    }
}
