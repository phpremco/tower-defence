// A planet seen from space, lit by the set's sun alone: a soft day/night terminator with a warm twilight band,
// coastlines and relief broken up by sphere noise so the painted map never shows its blobs, glinting seas, the
// shadows of the cloud deck, an atmospheric haze that thickens towards the limb, and on the night side either city
// lights (Earth) or glowing crystal fields (an alien world). Unlit by URP's lighting loop on purpose: one hard sun,
// no ambient probe, so the night side stays night. Noise octaves come from the quality profile.
Shader "Game/Planet"
{
    Properties
    {
        _SunDirection ("Towards the sun (world)", Vector) = (0, 0, -1, 0)
        [HDR] _SunColor ("Sun colour", Color) = (1, 1, 1, 1)
        _AlbedoMap ("Albedo", 2D) = "grey" {}
        _GlossMap ("Sea mask (gloss)", 2D) = "black" {}
        _NormalMap ("Normal", 2D) = "bump" {}
        _LightsMap ("Night lights", 2D) = "black" {}
        _CloudMap ("Clouds", 2D) = "black" {}
        _Alien ("Alien biomes", Range(0, 1)) = 0
        _DeepSea ("Deep sea", Color) = (0.01, 0.03, 0.08, 1)
        _ShallowSea ("Shallow sea", Color) = (0.03, 0.15, 0.2, 1)
        _LandA ("Lowland A", Color) = (0.2, 0.3, 0.2, 1)
        _LandB ("Lowland B", Color) = (0.3, 0.25, 0.2, 1)
        _Highland ("Highland", Color) = (0.35, 0.3, 0.25, 1)
        _Peak ("Peaks and ice", Color) = (0.85, 0.88, 0.9, 1)
        [HDR] _CrystalColor ("Crystal glow", Color) = (0.3, 1.2, 1.5, 1)
        [HDR] _CityColor ("City lights", Color) = (2.0, 1.3, 0.6, 1)
        [HDR] _AtmoColor ("Atmosphere", Color) = (0.35, 0.6, 1.0, 1)
        [HDR] _SunsetColor ("Twilight", Color) = (1.0, 0.45, 0.2, 1)
        _TextureTint ("Map tint on land", Range(0, 1)) = 0.35
        _CoastWarp ("Coast warp", Range(0, 0.05)) = 0.012
        _CoastBlur ("Coast blur (mip)", Range(0, 6)) = 4
        _CrystalDensity ("Crystal fields", Range(0, 1)) = 0.5
        _CloudShadow ("Cloud shadow", Range(0, 1)) = 0.5
        _SpecStrength ("Sea glint", Range(0, 4)) = 1.2
        _SpecPower ("Sea glint tightness", Range(8, 400)) = 120
        _RimStrength ("Limb haze", Range(0, 3)) = 1.0
        _RimPower ("Limb haze falloff", Range(0.5, 8)) = 3.0
        _DayHaze ("Haze over the day side", Range(0, 0.5)) = 0.15
        _Ambient ("Night ambient", Range(0, 1)) = 0.015
        _BumpStrength ("Relief", Range(0, 4)) = 1.0
        _MapRelief ("Painted relief", Range(0, 1)) = 0.35
        _DayLights ("Lights by day", Range(0, 1)) = 0
        _Octaves ("Noise octaves", Range(1, 8)) = 6
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Planet"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex PlanetVertex
            #pragma fragment PlanetFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "PlanetCommon.hlsl"

            TEXTURE2D(_AlbedoMap); SAMPLER(sampler_AlbedoMap);
            TEXTURE2D(_GlossMap); SAMPLER(sampler_GlossMap);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            TEXTURE2D(_LightsMap); SAMPLER(sampler_LightsMap);
            TEXTURE2D(_CloudMap); SAMPLER(sampler_CloudMap);

            CBUFFER_START(UnityPerMaterial)
            float4 _SunDirection;
            half4 _SunColor;
            float4 _AlbedoMap_ST;
            half _Alien;
            half4 _DeepSea;
            half4 _ShallowSea;
            half4 _LandA;
            half4 _LandB;
            half4 _Highland;
            half4 _Peak;
            half4 _CrystalColor;
            half4 _CityColor;
            half4 _AtmoColor;
            half4 _SunsetColor;
            half _TextureTint;
            half _CoastWarp;
            half _CoastBlur;
            half _CrystalDensity;
            half _CloudShadow;
            half _SpecStrength;
            half _SpecPower;
            half _RimStrength;
            half _RimPower;
            half _DayHaze;
            half _Ambient;
            half _BumpStrength;
            half _MapRelief;
            half _DayLights;
            half _Octaves;
            float4x4 _WorldToCloud;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
                float3 positionOS : TEXCOORD3;
                float2 uv : TEXCOORD4;
            };

            Varyings PlanetVertex(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normals = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = normals.normalWS;
                output.tangentWS = float4(normals.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                output.positionOS = input.positionOS.xyz;
                output.uv = input.uv;
                return output;
            }

            // Tilts the normal by the slope of a height field across the screen (surface gradient), so the noise
            // relief catches the sun without a second set of noise samples.
            float3 BumpFromHeight(float3 normal, float3 positionWS, float height)
            {
                float3 dpdx = ddx(positionWS);
                float3 dpdy = ddy(positionWS);
                float3 r1 = cross(dpdy, normal);
                float3 r2 = cross(normal, dpdx);
                float det = dot(dpdx, r1);
                float3 gradient = sign(det) * (ddx(height) * r1 + ddy(height) * r2);
                return normalize(abs(det) * normal - gradient);
            }

            half4 PlanetFragment(Varyings input) : SV_Target
            {
                int octaves = (int)_Octaves;
                float3 p = normalize(input.positionOS);
                float3 geometric = normalize(input.normalWS);
                float3 view = normalize(GetCameraPositionWS() - input.positionWS);
                float3 sunDirection = normalize(_SunDirection.xyz);
                half3 sunColour = _SunColor.rgb;
                float3 toSun = sunDirection;

                // Coasts: the painted sea mask, blurred and pushed around by noise, then cut at a ragged threshold.
                float warp = PlanetFbm(p * 5.0, 3) - 0.5;
                float2 uv = input.uv + float2(warp, PlanetFbm(p * 5.0 + 11.0, 3) - 0.5) * _CoastWarp;
                float seaField = SAMPLE_TEXTURE2D_LOD(_GlossMap, sampler_GlossMap, uv, _CoastBlur).a;
                float coastNoise = PlanetFbm(p * 26.0, octaves) - 0.5;
                float sea = smoothstep(0.66, 0.72, seaField + coastNoise * 0.42);
                float shallow = 1.0 - smoothstep(0.69, 0.86, seaField + coastNoise * 0.2);

                half3 painted = SAMPLE_TEXTURE2D(_AlbedoMap, sampler_AlbedoMap, uv).rgb;
                float ice = smoothstep(0.6, 0.85, dot(painted, float3(0.3, 0.4, 0.3)) + (PlanetFbm(p * 30.0, 3) - 0.5) * 0.3) * (1.0 - sea) * 0.5;

                // Relief and biomes from noise: broad plains, ridged mountain chains, pale peaks.
                float broad = PlanetFbm(p * 3.2, octaves);
                float ridges = PlanetRidged(p * 7.0 + broad * 1.5, octaves);
                float elevation = saturate(broad * 0.55 + ridges * 0.65 - 0.25);
                float biome = smoothstep(0.38, 0.62, PlanetFbm(p * 4.5 + 7.3, octaves));
                half3 lowland = lerp(_LandA.rgb, _LandB.rgb, biome);
                half3 land = lerp(lowland, _Highland.rgb, smoothstep(0.35, 0.65, elevation));
                land = lerp(land, _Peak.rgb, smoothstep(0.72, 0.9, elevation) * 0.8);
                half3 mapLand = painted * (0.75 + 0.5 * PlanetFbm(p * 18.0, octaves));
                land = lerp(land, painted, _TextureTint) * (0.8 + 0.4 * PlanetFbm(p * 60.0, max(1, octaves - 2)));
                land = lerp(mapLand, land, _Alien);
                land = lerp(land, _Peak.rgb, ice);
                half3 water = lerp(_DeepSea.rgb, _ShallowSea.rgb, shallow * shallow);
                water = lerp(water, painted * 0.6, (1.0 - _Alien) * 0.35);
                half3 albedo = lerp(land, water, sea);

                // Relief: the painted normal map plus the noise height on land.
                float3 tangent = normalize(input.tangentWS.xyz);
                float3 bitangent = cross(geometric, tangent) * input.tangentWS.w;
                float3 mapNormal = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv));
                mapNormal.xy *= _MapRelief;
                float3 normal = normalize(mapNormal.x * tangent + mapNormal.y * bitangent + mapNormal.z * geometric);
                float height = (elevation + PlanetFbm(p * 40.0, max(1, octaves - 2)) * 0.3) * (1.0 - sea) * _BumpStrength * 0.004;
                normal = BumpFromHeight(normal, input.positionWS, height * length(GetObjectToWorldMatrix()._m00_m10_m20));

                float facingSun = dot(geometric, toSun);
                float day = smoothstep(-0.1, 0.2, facingSun);
                float night = 1.0 - smoothstep(-0.18, 0.06, facingSun);

                // The cloud deck's shadow, cast a little away from the sun.
                float3 cloudDirection = normalize(mul((float3x3)_WorldToCloud, normalize(geometric + toSun * 0.018)));
                float cloud = SAMPLE_TEXTURE2D_LOD(_CloudMap, sampler_CloudMap, PlanetDirectionToUv(cloudDirection), 1.0).a;
                float shade = 1.0 - cloud * _CloudShadow;

                half3 light = sunColour;
                half3 colour = albedo * light * saturate(dot(normal, toSun)) * day * shade;
                float twilight = exp(-pow((facingSun - 0.02) / 0.13, 2.0));
                colour += albedo * _SunsetColor.rgb * twilight * 0.35;
                colour += albedo * _Ambient;

                // Sun glint on the sea: a tight hot spot and a broad sheen.
                float3 halfway = normalize(toSun + view);
                float glint = pow(saturate(dot(geometric, halfway)), _SpecPower) * _SpecStrength
                    + pow(saturate(dot(geometric, halfway)), _SpecPower * 0.08) * _SpecStrength * 0.025;
                colour += light * glint * sea * day * shade;

                // Air: a thin daylight haze over the face that thickens to the limb, warm along the terminator.
                float limb = pow(1.0 - saturate(dot(geometric, view)), _RimPower);
                half3 air = lerp(_SunsetColor.rgb, _AtmoColor.rgb, smoothstep(-0.05, 0.35, facingSun));
                float airLight = smoothstep(-0.3, 0.3, facingSun);
                colour = lerp(colour, air * light * 0.35 * airLight, saturate(_DayHaze + limb * 0.75) * airLight * _RimStrength * 0.6);
                colour += air * limb * _RimStrength * airLight * 0.6;

                // Night: city lights or crystal fields under the clouds.
                half3 lights = SAMPLE_TEXTURE2D(_LightsMap, sampler_LightsMap, input.uv).rgb * _CityColor.rgb;
                float fields = smoothstep(1.0 - _CrystalDensity * 0.45, 1.0 - _CrystalDensity * 0.3, PlanetFbm(p * 11.0 + 3.1, octaves)) * (1.0 - sea) * (1.0 - ice);
                float sparkle = 0.35 + PlanetFbm(p * 70.0, 2) * 1.3;
                half3 crystals = _CrystalColor.rgb * fields * sparkle * _Alien;
                colour += (lights * max(night, _DayLights) + crystals * lerp(0.06, 1.0, night)) * (1.0 - cloud * 0.75);

                return half4(colour, 1.0);
            }
            ENDHLSL
        }
    }
}
