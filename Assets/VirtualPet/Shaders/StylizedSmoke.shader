// Stylized (cartoon) smoke for particles, inspired by the erosion technique in
// Gabriel Aguiar's "Unity VFX Graph - Stylized Smoke Tutorial" (2021), rebuilt
// for the normal Particle System so it also runs in WebGL.
//
// How it works:
// - Particle alpha (Color over Lifetime) controls erosion: alpha 1 = full puff,
//   alpha 0 = eaten away. Pixels below the noise threshold are clipped.
// - Lighting is stepped (toon) between a shadow colour and a light colour.
// - Mesh mode uses real normals. Billboard mode fakes a sphere from the UVs.
Shader "WindPet/StylizedSmoke"
{
    Properties
    {
        _BaseColor ("Light Colour", Color) = (0.92, 0.98, 1, 1)
        _ShadowColor ("Shadow Colour", Color) = (0.55, 0.72, 0.88, 1)
        _EdgeColor ("Edge Colour", Color) = (1, 1, 1, 1)
        _NoiseTex ("Noise", 2D) = "gray" {}
        _NoiseScale ("Noise Scale", Float) = 1.5
        _NoiseScroll ("Noise Scroll (xy)", Vector) = (0.05, 0.25, 0, 0)
        _EdgeWidth ("Edge Width", Range(0, 0.3)) = 0.06
        _LightSteps ("Light Steps", Range(1, 6)) = 3
        [Toggle] _Billboard ("Billboard Mode", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 color : COLOR;
            };

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _ShadowColor;
                float4 _EdgeColor;
                float4 _NoiseTex_ST;
                float4 _NoiseScroll;
                float _NoiseScale;
                float _EdgeWidth;
                float _LightSteps;
                float _Billboard;
            CBUFFER_END

            Varyings vert (Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            float SampleNoise (float2 coords)
            {
                float2 scroll = _NoiseScroll.xy * _Time.y;
                return SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, coords * _NoiseScale + scroll).r;
            }

            half4 frag (Varyings input) : SV_Target
            {
                float erosion = 1.0 - input.color.a;
                float lightAmount;
                float value;

                if (_Billboard > 0.5)
                {
                    // Fake a sphere: centre of the quad is the front, the edge is the side.
                    float2 p = input.uv * 2.0 - 1.0;
                    float shape = saturate(1.0 - dot(p, p));
                    float noise = SampleNoise(input.uv);
                    value = shape * 0.75 + noise * 0.55 - 0.45 - erosion;
                    lightAmount = saturate(p.y * 0.5 + 0.5 + sqrt(shape) * 0.35);
                }
                else
                {
                    // Two planes of world-space noise, so moving puffs seem to churn.
                    float3 n = normalize(input.normalWS);
                    float noise = lerp(SampleNoise(input.positionWS.xy), SampleNoise(input.positionWS.zy + 0.37), abs(n.x));
                    value = noise - erosion;
                    Light mainLight = GetMainLight();
                    lightAmount = saturate(dot(n, mainLight.direction) * 0.5 + 0.5);
                }

                clip(value);

                float stepped = floor(lightAmount * _LightSteps) / max(_LightSteps - 1.0, 1.0);
                float3 colour = lerp(_ShadowColor.rgb, _BaseColor.rgb, saturate(stepped)) * input.color.rgb;

                // Bright rim where the puff is being eaten away.
                if (value < _EdgeWidth) colour = _EdgeColor.rgb;

                return half4(colour, 1.0);
            }
            ENDHLSL
        }
    }
}
