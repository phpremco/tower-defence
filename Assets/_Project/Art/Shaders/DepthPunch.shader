// The opening of Gravemaw's fissure at ground level (ArtSource README round 34, material slot 1 of the kit), drawn in
// two stages after the opaque ground and before the fissure rock, so the walls and the molten core below the ground
// show through it. A fragment that writes its own depth is also depth-tested with that depth, so one pass cannot both
// test against the ground and write the far plane:
// 1. Mark (no _FAR_DEPTH): depth-tested against what is already drawn, it only sets a stencil bit where the opening is
//    in view. Anything standing over the gap was drawn before it and is nearer, so the test keeps it.
// 2. Clear (_FAR_DEPTH): where the bit is set it writes the far-plane depth and resets the bit.
// It opens and closes with the rock (the same reveal data), and stays off a footprint the rock fades from (vertex
// alpha). Both stages pull the opening a little towards the camera along the view ray (the same pixels), so it wins the
// depth test over road tiles and flat decor that lie a hair above the bare ground.
Shader "Game/DepthPunch"
{
    Properties
    {
        _Front ("Open front (m along the crack)", Float) = 1000
        _OpenLength ("Opening ramp behind the front (m)", Float) = 2
        _Close ("Closed share of the whole crack", Range(0, 1)) = 0
        _Bias ("Pulled towards the camera along the view ray (m)", Float) = 0.3
        [Toggle(_FAR_DEPTH)] _FarDepth ("Clear stage (writes the far depth)", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Depth test", Float) = 4
        [Enum(Off, 0, On, 1)] _ZWrite ("Depth write", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil compare", Float) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPass ("Stencil pass", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry+1" }

        Pass
        {
            Name "Punch"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            ColorMask 0
            Cull Off
            Stencil
            {
                Ref 64
                ReadMask 64
                WriteMask 64
                Comp [_StencilComp]
                Pass [_StencilPass]
            }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex PunchVertex
            #pragma fragment PunchFragment
            #pragma shader_feature_local_fragment _FAR_DEPTH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float _Front;
            float _OpenLength;
            float _Close;
            float _Bias;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 reveal : TEXCOORD1;
                float2 closeXY : TEXCOORD2;
                float2 closeZ : TEXCOORD3;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half alpha : TEXCOORD0;
            };

            Varyings PunchVertex(Attributes input)
            {
                float closed = 1.0 - saturate((_Front - input.reveal.x) / max(_OpenLength, 0.001));
                closed = smoothstep(0.0, 1.0, max(closed, _Close));
                float3 position = input.positionOS.xyz + float3(input.closeXY.x, input.closeXY.y, input.closeZ.x) * closed;
                float3 positionWS = TransformObjectToWorld(position);
                positionWS += GetWorldSpaceNormalizeViewDir(positionWS) * _Bias;
                Varyings output;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.alpha = input.color.a;
                return output;
            }

            #if defined(_FAR_DEPTH)
            half4 PunchFragment(Varyings input, out float depth : SV_Depth) : SV_Target
            {
                clip(input.alpha - 0.5h);
                #if UNITY_REVERSED_Z
                    depth = 0.0;
                #else
                    depth = 1.0;
                #endif
                return 0;
            }
            #else
            half4 PunchFragment(Varyings input) : SV_Target
            {
                clip(input.alpha - 0.5h);
                return 0;
            }
            #endif
            ENDHLSL
        }
    }

    FallBack Off
}
