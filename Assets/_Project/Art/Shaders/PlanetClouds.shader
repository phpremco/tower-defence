// The cloud deck of a planet: the painted cloud map torn into wisps by sphere noise, lit by the sun with the same
// terminator as the ground (white by day, a warm edge at dusk, nearly black at night) and thinned at the limb so the
// atmosphere glow shows through. A painted deck can tint itself from its RGB (the Earth's brown smog). Alpha blended over the ground; the ground shader casts the matching shadows.
Shader "Game/PlanetClouds"
{
    Properties
    {
        _SunDirection ("Towards the sun (world)", Vector) = (0, 0, -1, 0)
        [HDR] _SunColor ("Sun colour", Color) = (1, 1, 1, 1)
        _CloudMap ("Clouds", 2D) = "black" {}
        _CloudColor ("Colour", Color) = (1, 1, 1, 1)
        _MapColour ("Deck colour from the map (RGB)", Range(0, 1)) = 0
        _MapSaturation ("Saturation of the map's colour", Range(0, 1)) = 1
        [HDR] _SunsetColor ("Twilight", Color) = (1.0, 0.45, 0.2, 1)
        _Alpha ("Alpha", Range(0, 1)) = 1
        _Detail ("Wisps", Range(0, 1)) = 0.6
        _Octaves ("Noise octaves", Range(1, 8)) = 5
        _ClearCentre ("Thinned spot: direction (object), cos of the outer radius", Vector) = (0, 0, 1, 2)
        _ClearInner ("Thinned spot: cos of the inner radius", Float) = 3
        _ClearShare ("Thinned spot: how much thinner", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-10" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "PlanetClouds"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CloudVertex
            #pragma fragment CloudFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "PlanetCommon.hlsl"

            TEXTURE2D(_CloudMap); SAMPLER(sampler_CloudMap);

            CBUFFER_START(UnityPerMaterial)
            float4 _SunDirection;
            half4 _SunColor;
            float4 _CloudMap_ST;
            half4 _CloudColor;
            half _MapColour;
            half _MapSaturation;
            half4 _SunsetColor;
            half _Alpha;
            half _Detail;
            half _Octaves;
            float4 _ClearCentre;
            float _ClearInner;
            half _ClearShare;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
                float2 uv : TEXCOORD3;
            };

            Varyings CloudVertex(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionOS = input.positionOS.xyz;
                output.uv = input.uv;
                return output;
            }

            half4 CloudFragment(Varyings input) : SV_Target
            {
                int octaves = (int)_Octaves;
                float3 p = normalize(input.positionOS);
                float3 normal = normalize(input.normalWS);
                float3 view = normalize(GetCameraPositionWS() - input.positionWS);
                float3 sunDirection = normalize(_SunDirection.xyz);
                half3 sunColour = _SunColor.rgb;

                float swirl = PlanetFbm(p * 4.0, 3) - 0.5;
                float2 uv = input.uv + float2(swirl * 0.02, swirl * 0.01);
                half4 deck = SAMPLE_TEXTURE2D(_CloudMap, sampler_CloudMap, uv);
                float cover = deck.a;
                float wisps = PlanetFbm(p * 14.0 + swirl * 2.0, octaves);
                float alpha = saturate(cover * lerp(1.0, wisps * 2.2 - 0.25, _Detail));
                alpha = saturate(alpha * 1.3) * _Alpha;
                // The chosen region shows through: the deck thins over it, softly towards its rim.
                float inside = smoothstep(_ClearCentre.w, _ClearInner, dot(p, normalize(_ClearCentre.xyz)));
                alpha *= 1.0 - _ClearShare * inside;

                float facingSun = dot(normal, sunDirection);
                float day = smoothstep(-0.12, 0.25, facingSun);
                float twilight = exp(-pow((facingSun - 0.04) / 0.14, 2.0));
                float facing = saturate(dot(normal, view));
                // Lit tops read brighter where the deck is thick; the limb thins out under the haze.
                // A painted smog deck (Earth) carries its own tint in RGB; a white cloud map keeps the style colour.
                half3 mapTint = lerp(dot(deck.rgb, half3(0.3h, 0.59h, 0.11h)).xxx, deck.rgb, _MapSaturation);
                half3 tint = lerp(_CloudColor.rgb, _CloudColor.rgb * mapTint, _MapColour);
                half3 colour = tint * sunColour * (0.25 + 0.75 * saturate(facingSun + 0.15)) * day * (0.8 + 0.4 * wisps);
                colour += _SunsetColor.rgb * twilight * 0.5;
                alpha *= smoothstep(0.0, 0.35, facing) * lerp(0.75, 1.0, day);
                return half4(colour, alpha);
            }
            ENDHLSL
        }
    }
}
