// 3D model dissolve, the partner of PokerTD/CardDissolve: the same value-noise burn with the same
// glowing orange edge, so cards, towers, machines and enemies all appear and vanish the same way.
// Simple lighting (main light + ambient), enough for the short time an object is dissolving.
// The noise uses the object-space position (Kenney models share one colour-map texture, so their uv is not usable).
// _Dissolve 0 = whole model, 1 = gone.
Shader "PokerTD/ModelDissolve"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Colour", Color) = (1, 1, 1, 1)
        _Dissolve ("Dissolve", Range(0, 1)) = 0
        _EdgeColor ("Edge Color", Color) = (1, 0.62, 0.15, 1)
        _EdgeWidth ("Edge Width", Range(0, 0.3)) = 0.06
        _NoiseScale ("Noise Scale", Float) = 4
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Lit"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Dissolve;
                float4 _EdgeColor;
                float _EdgeWidth;
                float _NoiseScale;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 normalWS : TEXCOORD1; float3 posOS : TEXCOORD2; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.posOS = v.positionOS.xyz;
                return o;
            }

            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float ValueNoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), u.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), u.x), u.y);
            }
            float Fbm(float2 p) { return ValueNoise(p) * 0.6 + ValueNoise(p * 2.1 + 3.7) * 0.3 + ValueNoise(p * 4.3 + 9.1) * 0.1; }

            half4 frag(Varyings i, bool front : SV_IsFrontFace) : SV_Target
            {
                // noise from two side views of the object, so it wraps around the model
                float n = 0.5 * Fbm(i.posOS.xy * _NoiseScale) + 0.5 * Fbm(i.posOS.zy * _NoiseScale + 7.3);
                n = saturate((n - 0.5) * 2.2 + 0.5);   // averaging two noises makes them flat: stretch back to 0..1
                float threshold = _Dissolve * (1.0 + _EdgeWidth);
                clip(n - threshold + 0.0001);

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                float3 nrm = normalize(i.normalWS) * (front ? 1 : -1);
                Light main = GetMainLight();
                float3 lit = albedo.rgb * (main.color * saturate(dot(nrm, main.direction)) * 0.8 + SampleSH(nrm) + 0.1);
                float edge = _Dissolve > 0.0 ? 1.0 - saturate((n - threshold) / max(_EdgeWidth, 0.0001)) : 0.0;
                lit = lerp(lit, _EdgeColor.rgb * 2.0, edge);
                return half4(lit, 1);
            }
            ENDHLSL
        }
    }
}
