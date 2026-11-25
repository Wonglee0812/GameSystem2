// Card sprite for the 3D board (towers and enemy marks), URP unlit.
// _Dissolve 0..1 burns the card away along a value-noise pattern with a glowing edge
// (the usual noise-threshold dissolve: clip where noise < threshold, glow just above it).
// _Flash pushes the colour to white (hit flash), _Grey makes it grey (stunned tower).
Shader "PokerTD/CardDissolve"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Dissolve ("Dissolve", Range(0, 1)) = 0
        _EdgeColor ("Edge Color", Color) = (1, 0.62, 0.15, 1)
        _EdgeWidth ("Edge Width", Range(0, 0.3)) = 0.08
        _NoiseScale ("Noise Scale", Float) = 7
        _Flash ("Flash", Range(0, 1)) = 0
        _Grey ("Grey", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Dissolve;
                float4 _EdgeColor;
                float _EdgeWidth;
                float _NoiseScale;
                float _Flash;
                float _Grey;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings  { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float Hash (float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }

            float ValueNoise (float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), u.x),
                            lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), u.x), u.y);
            }

            float Fbm (float2 p)
            {
                return ValueNoise(p) * 0.6 + ValueNoise(p * 2.1 + 3.7) * 0.3 + ValueNoise(p * 4.3 + 9.1) * 0.1;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
                float grey = dot(col.rgb, float3(0.299, 0.587, 0.114)) * 0.65;
                col.rgb = lerp(col.rgb, grey.xxx, _Grey);
                col.rgb = lerp(col.rgb, 1.0.xxx, _Flash);

                // threshold slightly above 1 at full dissolve, so the whole card is gone
                float threshold = _Dissolve * (1.0 + _EdgeWidth);
                float n = Fbm(i.uv * _NoiseScale);
                clip(n - threshold + 0.0001);
                float edge = _Dissolve > 0.0 ? 1.0 - saturate((n - threshold) / max(_EdgeWidth, 0.0001)) : 0.0;
                col.rgb = lerp(col.rgb, _EdgeColor.rgb * 2.0, edge);
                return col;
            }
            ENDHLSL
        }
    }
}
