// Slow swirling background under the table (Balatro-like mood, dark so the board stays readable).
// The uv is twisted around the centre (more twist far out), then warped a few times with sine waves
// (domain warping); the result picks between three colours. URP unlit, no lighting.
Shader "PokerTD/Swirl"
{
    Properties
    {
        _ColorA ("Dark", Color) = (0.09, 0.07, 0.07, 1)
        _ColorB ("Mid", Color) = (0.16, 0.11, 0.10, 1)
        _ColorC ("Light", Color) = (0.25, 0.12, 0.10, 1)
        _Scale ("Scale", Float) = 6
        _Twist ("Twist", Float) = 0.6
        _Speed ("Speed", Float) = 0.08
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-100" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="UniversalForward" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorA, _ColorB, _ColorC;
                float _Scale, _Twist, _Speed;
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

            half4 frag(Varyings i) : SV_Target
            {
                float t = _Time.y * _Speed;
                float2 p = (i.uv - 0.5) * _Scale;
                float len = length(p);
                float ang = atan2(p.y, p.x) + _Twist * len - t * 2.0;
                p = float2(cos(ang), sin(ang)) * len;
                for (int k = 0; k < 4; k++)
                    p += 0.45 * float2(sin(p.y * 1.3 + t * 3.0 + k), cos(p.x * 1.1 - t * 2.0 + k * 1.7));
                float v = 0.5 + 0.5 * sin(length(p) * 1.4 + p.x * 0.6);
                float3 c = lerp(_ColorA.rgb, _ColorB.rgb, smoothstep(0.25, 0.65, v));
                c = lerp(c, _ColorC.rgb, smoothstep(0.75, 0.98, v));
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
