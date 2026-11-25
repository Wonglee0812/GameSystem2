// Ink splat background (Splatoon-like), a code port of Wong's SplatoonGraph shader graph
// (Game-Design repo, OpenScene/ShaderGraph/SplatoonGraph.shadergraph):
//   offset = time * 0.01; warp = GradientNoise(uv * (1.5, 1) + offset, 4.43) * 0.1
//   n = GradientNoise(uv + warp, 15); t = smoothstep(0.4, 0.37, n); colour = lerp(A, B, t)
// The noise is Unity's Gradient Noise node code, so the blobs look the same as in the graph.
// _Tiling repeats the uv on the big quad under the table, so the visible part shows about one uv.
Shader "PokerTD/InkBackground"
{
    Properties
    {
        _ColorA ("Base colour", Color) = (0.145, 0.0, 1.0, 1)
        _ColorB ("Ink colour", Color) = (1.0, 1.0, 0.0, 1)
        _Darken ("Darken (0 = colours as set)", Range(0, 1)) = 0.6
        _Tiling ("Tiling on the quad", Float) = 3.7
        _Stretch ("UV stretch (graph: 1.5, 1)", Vector) = (1.5, 1, 0, 0)
        _WarpScale ("Warp noise scale", Float) = 4.43
        _WarpAmount ("Warp amount", Float) = 0.1
        _InkScale ("Ink noise scale", Float) = 15
        _Edge1 ("Smoothstep edge 1", Float) = 0.4
        _Edge2 ("Smoothstep edge 2", Float) = 0.37
        _Speed ("Flow speed", Float) = 0.01
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-100" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorA, _ColorB, _Stretch;
                float _Darken, _Tiling, _WarpScale, _WarpAmount, _InkScale, _Edge1, _Edge2, _Speed;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            // Unity Shader Graph "Gradient Noise" node (same hash and fade curve)
            float2 GradientDir(float2 p)
            {
                p = p % 289;
                float x = (34 * p.x + 1) * p.x % 289 + p.y;
                x = (34 * x + 1) * x % 289;
                x = frac(x / 41) * 2 - 1;
                return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
            }
            float GradientNoise(float2 uv, float scale)
            {
                float2 p = uv * scale;
                float2 ip = floor(p), fp = frac(p);
                float d00 = dot(GradientDir(ip), fp);
                float d01 = dot(GradientDir(ip + float2(0, 1)), fp - float2(0, 1));
                float d10 = dot(GradientDir(ip + float2(1, 0)), fp - float2(1, 0));
                float d11 = dot(GradientDir(ip + float2(1, 1)), fp - float2(1, 1));
                fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
                return lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv * _Tiling;
                float t = _Time.y * _Speed;
                float warp = GradientNoise(uv * _Stretch.xy + t, _WarpScale) * _WarpAmount;
                float n = GradientNoise(uv + warp, _InkScale);
                float k = smoothstep(_Edge1, _Edge2, n);
                float3 c = lerp(_ColorA.rgb, _ColorB.rgb, k) * (1 - _Darken);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
