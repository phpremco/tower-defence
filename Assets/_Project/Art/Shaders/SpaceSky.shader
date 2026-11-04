// The sky behind the stars of the space sets: a faint band of galaxy light along one great circle, two colours of
// nebula gas mixed by noise, dark dust lanes through the middle of the band and a very dim haze elsewhere. Drawn on
// the inside of a sphere that follows the camera, before everything else, so it always lies at infinity.
Shader "Game/SpaceSky"
{
    Properties
    {
        [HDR] _NebulaA ("Nebula A", Color) = (0.25, 0.12, 0.4, 1)
        [HDR] _NebulaB ("Nebula B", Color) = (0.08, 0.3, 0.4, 1)
        [HDR] _BandColor ("Galaxy band", Color) = (0.6, 0.55, 0.5, 1)
        _BandNormal ("Band plane normal", Vector) = (0.3, 0.9, 0.2, 0)
        _BandWidth ("Band width", Range(0.02, 1)) = 0.22
        _Strength ("Strength", Range(0, 2)) = 0.35
        _Haze ("Haze elsewhere", Range(0, 1)) = 0.15
        _StarScale ("Star grid", Float) = 70
        _StarBrightness ("Star brightness", Range(0, 6)) = 2
        _BandStars ("Stars crowding the band", Range(0, 1)) = 0.45
        _Octaves ("Noise octaves", Range(1, 8)) = 6
    }

    SubShader
    {
        Tags { "RenderType" = "Background" "Queue" = "Background" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        ZWrite Off
        Cull Front

        Pass
        {
            Name "SpaceSky"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex SkyVertex
            #pragma fragment SkyFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "PlanetCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
            half4 _NebulaA;
            half4 _NebulaB;
            half4 _BandColor;
            float4 _BandNormal;
            half _BandWidth;
            half _Strength;
            half _Haze;
            half _StarScale;
            half _StarBrightness;
            half _BandStars;
            half _Octaves;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            Varyings SkyVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            // One star per lit cell of a 3D grid over the sky, drawn a pixel or two wide whatever the field of view (fwidth),
            // so the stars stay crisp and steady as the camera moves; colour and brightness come from the cell's hash.
            half3 StarLayer(float3 d, float scale, float density, float seed)
            {
                float3 q = d * scale;
                float3 cell = floor(q);
                float roll = PlanetHash(cell + seed);
                float3 spot = float3(PlanetHash(cell + seed + 1.3), PlanetHash(cell + seed + 2.7), PlanetHash(cell + seed + 5.1)) * 0.8 + 0.1;
                float distance = length(frac(q) - spot);
                float pixel = length(fwidth(q));
                float bright = pow(PlanetHash(cell + seed + 7.9), 4.0);
                float size = pixel * (0.7 + bright * 1.8);
                float shape = saturate(1.0 - distance / size);
                float temperature = PlanetHash(cell + seed + 9.4);
                half3 colour = temperature < 0.08 ? half3(1.0, 0.55, 0.4) : temperature < 0.3 ? half3(1.0, 0.88, 0.7) : temperature < 0.75 ? half3(0.95, 0.97, 1.0) : half3(0.62, 0.75, 1.0);
                return colour * (shape * shape * step(roll, density) * (0.35 + bright * 3.0));
            }

            half4 SkyFragment(Varyings input) : SV_Target
            {
                int octaves = (int)_Octaves;
                float3 d = normalize(input.direction);
                float across = dot(d, normalize(_BandNormal.xyz));
                float wobble = (PlanetFbm(d * 2.0, 3) - 0.5) * _BandWidth * 1.2;
                float band = exp(-pow((across + wobble) / _BandWidth, 2.0));
                float core = exp(-pow((across + wobble) / (_BandWidth * 0.35), 2.0));

                float gas = PlanetFbm(d * 3.0 + 4.0, octaves);
                float mixing = smoothstep(0.35, 0.65, PlanetFbm(d * 1.6 + 9.0, octaves));
                half3 nebula = lerp(_NebulaA.rgb, _NebulaB.rgb, mixing) * pow(gas, 2.2) * 2.5;
                float dust = smoothstep(0.45, 0.75, PlanetRidged(d * 5.0 + gas, octaves));
                float stars = PlanetFbm(d * 24.0, max(1, octaves - 2));

                half3 colour = (_BandColor.rgb * (core * 0.8 + band * 0.4) * (0.5 + stars) + nebula * band) * (1.0 - dust * core * 0.85);
                colour += nebula * pow(gas, 3.0) * _Haze;
                colour *= _Strength;
                float crowd = 1.0 + band * _BandStars * 4.0;
                colour += StarLayer(d, _StarScale, 0.05 * crowd, 0.0) * _StarBrightness;
                colour += StarLayer(d, _StarScale * 2.6, 0.08 * crowd, 31.0) * (_StarBrightness * 0.6);
                return half4(colour, 1.0);
            }
            ENDHLSL
        }
    }
}
