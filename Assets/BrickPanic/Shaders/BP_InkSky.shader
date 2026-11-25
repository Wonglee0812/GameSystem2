// Ink splat sky for Brick Panic. Same idea as Poker TD's ink background (a port of Wong's SplatoonGraph shader graph:
// warp noise -> ink noise -> smoothstep -> two colours), but as a skybox: the noise is 3D and is sampled with the view
// direction, so the splats wrap around the free-fly camera without seams or a pinch at the top and bottom.
// _Tint / _TintColor let the game colour the sky during a disaster (DisasterSky).
Shader "BrickPanic/InkSky"
{
    Properties
    {
        _ColorA ("Base colour", Color) = (0.4, 0.78, 1, 1)
        _ColorB ("Ink colour", Color) = (0.64, 0.9, 1, 1)
        _BottomColor ("Colour below the horizon", Color) = (0.24, 0.42, 0.8, 1)
        _BottomAmount ("Below-horizon blend (0-1)", Range(0, 1)) = 0.7
        _InkScale ("Ink noise scale", Float) = 4
        _WarpScale ("Warp noise scale", Float) = 2
        _WarpAmount ("Warp amount", Float) = 0.35
        _Edge1 ("Smoothstep edge 1", Float) = 0.42
        _Edge2 ("Smoothstep edge 2", Float) = 0.39
        _Speed ("Flow speed", Float) = 0.02
        _TintColor ("Disaster tint colour", Color) = (1, 0.4, 0.2, 1)
        _Tint ("Disaster tint amount (set by script)", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorA, _ColorB, _BottomColor, _TintColor;
                float _BottomAmount, _InkScale, _WarpScale, _WarpAmount, _Edge1, _Edge2, _Speed, _Tint;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.dir = v.positionOS.xyz;
                return o;
            }

            float3 hash3(float3 p)
            {
                p = float3(dot(p, float3(127.1, 311.7, 74.7)), dot(p, float3(269.5, 183.3, 246.1)), dot(p, float3(113.5, 271.9, 124.6)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }

            // 3D gradient noise, about -0.5..0.5
            float gnoise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float3 u = f * f * (3.0 - 2.0 * f);
                float a = dot(hash3(i + float3(0, 0, 0)), f - float3(0, 0, 0));
                float b = dot(hash3(i + float3(1, 0, 0)), f - float3(1, 0, 0));
                float c = dot(hash3(i + float3(0, 1, 0)), f - float3(0, 1, 0));
                float d = dot(hash3(i + float3(1, 1, 0)), f - float3(1, 1, 0));
                float e = dot(hash3(i + float3(0, 0, 1)), f - float3(0, 0, 1));
                float g = dot(hash3(i + float3(1, 0, 1)), f - float3(1, 0, 1));
                float h = dot(hash3(i + float3(0, 1, 1)), f - float3(0, 1, 1));
                float k = dot(hash3(i + float3(1, 1, 1)), f - float3(1, 1, 1));
                return lerp(lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y), lerp(lerp(e, g, u.x), lerp(h, k, u.x), u.y), u.z);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 dir = normalize(i.dir);
                float t = _Time.y * _Speed;
                float warp = gnoise(dir * _WarpScale + t) * _WarpAmount;
                float n = gnoise(dir * _InkScale + warp + float3(t, t * 0.5, 0)) + 0.5;
                float ink = smoothstep(_Edge1, _Edge2, n);
                float3 col = lerp(_ColorA.rgb, _ColorB.rgb, ink);
                col = lerp(col, _BottomColor.rgb, saturate(-dir.y * 1.5) * _BottomAmount);
                col = lerp(col, col * _TintColor.rgb * 1.4, _Tint);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
