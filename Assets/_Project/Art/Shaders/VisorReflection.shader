// The boss mirrored in Mercer's visor on the intro cut-in (Docs/BOSS_INTROS.md, round 13b): a small render of the boss
// seen from his head, bent as on a curved glass (barrel warp, mirrored), tinted in the boss's colour and added over the
// visor inside a soft oval. Brighter towards the rim of the glass, like a real reflection; dark parts add nothing.
Shader "Game/UI/VisorReflection"
{
    Properties
    {
        [PerRendererData] _MainTex ("Reflection", 2D) = "black" {}
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Strength ("Strength", Range(0, 2)) = 0.6
        _Bulge ("Curvature", Range(0, 1.5)) = 0.55
        _Edge ("Rim softness", Range(0.01, 0.5)) = 0.22
        _RimBoost ("Brighter towards the rim", Range(0, 2)) = 0.6
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "CanUseSpriteAtlas" = "True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _Tint;
            float _Strength;
            float _Bulge;
            float _Edge;
            float _RimBoost;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 c = i.uv * 2.0 - 1.0;
                float r2 = dot(c, c);
                // A convex mirror shrinks the middle and stretches the rim; the picture is mirrored left to right.
                float2 warped = c / (1.0 + _Bulge * (1.0 - r2));
                float2 uv = float2(0.5 - warped.x * 0.5, 0.5 + warped.y * 0.5);
                float3 seen = tex2D(_MainTex, uv).rgb;
                float oval = 1.0 - smoothstep(1.0 - _Edge, 1.0, sqrt(r2));
                float rim = 1.0 + _RimBoost * smoothstep(0.2, 0.9, r2);
                float3 colour = seen * _Tint.rgb * (_Strength * oval * rim * i.color.a);
                return fixed4(colour, 0);
            }
            ENDCG
        }
    }
}
