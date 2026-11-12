// Ink outline for the inverted-hull meshes exported from Blender.
// The hull's normals already point inwards, so moving each vertex against its
// normal makes the outline thicker. _Width is in world units (metres).
// Unlit, no lighting, so the line colour stays flat like a drawn line. WebGL safe.
Shader "WindPet/InkOutline"
{
    Properties
    {
        _BaseColor ("Colour", Color) = (0.17, 0.1, 0.06, 1)
        _Width ("Extra Width (m)", Range(0, 0.08)) = 0.015
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Width;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert (Attributes input)
            {
                Varyings o;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = normalize(TransformObjectToWorldNormal(input.normalOS));
                positionWS -= normalWS * _Width;       // normals point inwards: minus = grow outwards
                o.positionCS = TransformWorldToHClip(positionWS);
                return o;
            }

            half4 frag (Varyings i) : SV_Target { return half4(_BaseColor.rgb, 1); }
            ENDHLSL
        }
    }
}
