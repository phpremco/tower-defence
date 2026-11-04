// The glow of a planet's air seen past its edge: drawn on the inside of a shell around the planet, so only the ring
// outside the disc shows (the planet hides the rest). Each pixel's view ray is measured by how close it passes to the
// planet; the glow falls off with that height like real air thins out, is bright over the day side, warm where the
// ray grazes the terminator and flares up when the camera looks towards the sun past the limb (forward scattering).
Shader "Game/PlanetAtmosphere"
{
    Properties
    {
        _SunDirection ("Towards the sun (world)", Vector) = (0, 0, -1, 0)
        [HDR] _SunColor ("Sun colour", Color) = (1, 1, 1, 1)
        [HDR] _AtmoColor ("Atmosphere", Color) = (0.35, 0.6, 1.0, 1)
        [HDR] _SunsetColor ("Twilight", Color) = (1.0, 0.45, 0.2, 1)
        _Strength ("Strength", Range(0, 6)) = 1.5
        _Falloff ("Falloff", Range(1, 20)) = 6
        _Lift ("Shell radius over planet radius", Float) = 1.06
        _Forward ("Forward scattering", Range(0, 6)) = 1.5
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        Cull Front

        Pass
        {
            Name "PlanetAtmosphere"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex AtmosphereVertex
            #pragma fragment AtmosphereFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _SunDirection;
            half4 _SunColor;
            half4 _AtmoColor;
            half4 _SunsetColor;
            half _Strength;
            half _Falloff;
            float _Lift;
            half _Forward;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings AtmosphereVertex(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                return output;
            }

            half4 AtmosphereFragment(Varyings input) : SV_Target
            {
                float3 centre = TransformObjectToWorld(float3(0, 0, 0));
                float shellRadius = length(GetObjectToWorldMatrix()._m00_m10_m20);
                float radius = shellRadius / _Lift;
                float3 eye = GetCameraPositionWS();
                float3 ray = normalize(input.positionWS - eye);
                float along = dot(centre - eye, ray);
                float3 closest = eye + ray * along;
                float3 outward = closest - centre;
                float height = saturate((length(outward) / radius - 1.0) / (_Lift - 1.0));
                float density = exp(-height * _Falloff) * (1.0 - height);

                float3 sunDirection = normalize(_SunDirection.xyz);
                half3 sunColour = _SunColor.rgb;
                float facingSun = dot(normalize(outward), sunDirection);
                float lit = smoothstep(-0.35, 0.3, facingSun);
                float forward = pow(saturate(dot(ray, sunDirection)), 8.0) * _Forward * smoothstep(-0.6, 0.0, facingSun);
                half3 colour = lerp(_SunsetColor.rgb, _AtmoColor.rgb, smoothstep(-0.1, 0.45, facingSun)) * sunColour;
                half3 glow = colour * density * (lit + forward) * _Strength;
                // Alpha follows the glow, so on a transparent target (the map globe) the faint edge fades instead of blacking out what lies behind.
                return half4(glow, saturate(max(glow.r, max(glow.g, glow.b))));
            }
            ENDHLSL
        }
    }
}
