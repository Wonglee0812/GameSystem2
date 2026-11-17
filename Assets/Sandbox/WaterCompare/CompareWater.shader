// Water look for the water compare scene (and later the game).
// - Colour by depth: light turquoise in shallow water, dark blue in deep water (light is
//   absorbed with depth, so deep water looks darker).
// - Clear water (new): the ground under the water is seen through it, bent a little by the
//   waves (refraction from the URP opaque texture). Light is absorbed per colour, red first,
//   so shallow water is clear and deep water turns blue-green (Beer-Lambert law).
// - Soft shore: the water fades out over the last few centimetres instead of a hard edge.
// - Reflection of the sky at grazing angles (Fresnel, Schlick's approximation) and a sun highlight.
// - Foam where the water is fast or steep, and flow streaks that move with the current
//   (two-phase flow map, Vlachos, 2010).
// - Caustics: bright moving lines of light on the ground under shallow water (cellular
//   F2-F1 noise, Worley, 1996), like the turquoise shallows in Sandcastle's screenshots.
// - Foam on wave fronts: where the surface rises fast (GPU versions only).
// Two ways to get the water: mesh data from the CPU (versions A and B: uv1 = depth, uv2 = velocity)
// or the State texture from the GPU simulation (version C, keyword _USE_STATE).
Shader "DamRush/CompareWater"
{
    Properties
    {
        _Shallow ("Shallow colour", Color) = (0.36, 0.84, 0.82, 1)
        _Deep ("Deep colour", Color) = (0.03, 0.22, 0.42, 1)
        _Sky ("Sky reflection", Color) = (0.75, 0.88, 1, 1)
        _Absorb ("Absorption per metre", Float) = 1.3
        _AbsorbRGB ("Clear water: absorption per metre (r, g, b)", Vector) = (1.7, 0.55, 0.38, 0)
        _Clarity ("Clear water (0 = old look, 1 = see-through)", Range(0, 1)) = 1
        _Refract ("Refraction strength", Range(0, 0.1)) = 0.03
        _Caustics ("Caustics strength", Range(0, 2)) = 0.8
        _CausticScale ("Caustics size (lines per metre)", Float) = 0.9
        _FrontFoam ("Foam on rising wave fronts", Float) = 1.0
        _ShoreFade ("Shore fade depth (m)", Float) = 0.06
        _FoamColor ("Foam colour", Color) = (0.97, 0.99, 1, 1)
        _FoamSpeed ("Water speed with full foam (m/s)", Float) = 4.0
        _FoamSteep ("Foam on steep water", Float) = 2.5
        _StreakStrength ("Flow streaks", Range(0, 1.5)) = 0.7
        _FlowScale ("Flow advection", Float) = 0.35
        _Gloss ("Sun highlight sharpness", Float) = 120
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ _USE_STATE
            #pragma target 4.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Shallow, _Deep, _Sky, _FoamColor, _AbsorbRGB;
                float _Clarity, _Refract, _Caustics, _CausticScale, _FrontFoam;
                float _Alpha, _StepDt;
                float _Absorb, _ShoreFade, _FoamSpeed, _FoamSteep, _StreakStrength, _FlowScale, _Gloss;
                float _CellSize;
                float4 _GridSize; // x = cells x, y = cells z
            CBUFFER_END
            float _DR_Time;
            Texture2D<float4> _State;
            Texture2D<float4> _PrevState;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 depthSteep : TEXCOORD2; // x = depth, y = steepness
                float2 vel : TEXCOORD3;
                float rise : TEXCOORD4; // m/s the surface goes up (wave front)
            };

            #if defined(_USE_STATE)
            // the state blended between the step before and the last step (smooth motion)
            float4 StateAt(int2 c)
            {
                c = clamp(c, int2(0, 0), int2((int)_GridSize.x - 1, (int)_GridSize.y - 1));
                return lerp(_PrevState.Load(int3(c, 0)), _State.Load(int3(c, 0)), saturate(_Alpha));
            }
            float SurfaceAt(int2 c)
            {
                float4 s = StateAt(c);
                return s.y > 0.003 ? s.x + s.y : s.x - 0.05;
            }
            #endif

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 pos = v.positionOS.xyz;
                float3 nrm = v.normalOS;
                float depth = v.uv1.x;
                float2 vel = v.uv2;
                o.rise = 0.0;
                #if defined(_USE_STATE)
                    int2 c = int2(v.uv0);
                    float4 s = StateAt(c);
                    float4 sNow = _State.Load(int3(c, 0)), sBefore = _PrevState.Load(int3(c, 0));
                    o.rise = (sNow.x + sNow.y - sBefore.x - sBefore.y) / max(_StepDt, 0.001);
                    depth = s.y;
                    vel = s.zw;
                    pos.y = depth > 0.003 ? s.x + s.y : s.x - 0.05;
                    float hl = SurfaceAt(c - int2(1, 0)), hr = SurfaceAt(c + int2(1, 0));
                    float hb = SurfaceAt(c - int2(0, 1)), ht = SurfaceAt(c + int2(0, 1));
                    nrm = normalize(float3(hl - hr, 2.0 * _CellSize, hb - ht));
                #endif
                VertexPositionInputs p = GetVertexPositionInputs(pos);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(nrm);
                o.depthSteep = float2(depth, 1.0 - saturate(nrm.y));
                o.vel = vel;
                return o;
            }

            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), u.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), u.x), u.y);
            }
            float Fbm(float2 p) { return Noise(p) * 0.55 + Noise(p * 2.1 + 3.7) * 0.3 + Noise(p * 4.3 + 9.1) * 0.15; }

            // cellular noise: distance to the 2nd nearest point minus the nearest (Worley, 1996);
            // small values are the thin lines between cells, which look like caustics
            float CellLines(float2 p, float t)
            {
                float2 ip = floor(p), fp = frac(p);
                float d1 = 8.0, d2 = 8.0;
                for (int y = -1; y <= 1; y++)
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 g = float2(x, y);
                        float2 h = float2(Hash(ip + g), Hash(ip + g + 19.19));
                        float2 o = 0.5 + 0.4 * sin(t + 6.2831 * h);
                        float d = length(g + o - fp);
                        if (d < d1) { d2 = d1; d1 = d; } else if (d < d2) d2 = d;
                    }
                return d2 - d1;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float t = _DR_Time > 0.0 ? _DR_Time : _Time.y;
                float depth = i.depthSteep.x;
                if (depth < 0.003) discard;
                float2 xz = i.positionWS.xz;
                float2 vel = i.vel;
                float speed = min(length(vel), 4.0);

                // two-phase flow map: two noise layers pushed downstream, each reset half a cycle apart
                float phase = t * 0.4;
                float p0 = frac(phase), p1 = frac(phase + 0.5);
                float blend = abs(1.0 - 2.0 * p0);
                float2 adv = vel * _FlowScale;
                float n0 = Fbm((xz - adv * p0 * 2.5) * 1.3);
                float n1 = Fbm((xz - adv * p1 * 2.5) * 1.3 + 17.3);
                float flowNoise = lerp(n0, n1, blend);

                // small ripples bend the normal a little (stronger in fast water)
                float3 n = normalize(i.normalWS);
                float rip = 0.08 + 0.12 * saturate(speed / 2.0);
                n = normalize(n + float3(n0 - 0.5, 0, n1 - 0.5) * rip);

                // depth colour and soft shore
                float absorbed = 1.0 - exp(-depth * _Absorb);
                float3 col = lerp(_Shallow.rgb, _Deep.rgb, absorbed);
                float alpha = smoothstep(0.0, _ShoreFade, depth) * lerp(0.55, 0.93, absorbed);

                // foam: fast water, steep water (falls over a wall), and a thin line at the shore when the water moves
                // steepness counts only where the surface really drops (water over a wall, a fall), not on gentle slopes
                float foamAmount = saturate((i.rise - 0.6) * 0.3 * _FrontFoam) * saturate(depth * 8.0) + saturate(speed / _FoamSpeed) * 0.9 + saturate((i.depthSteep.y - 0.12) * _FoamSteep) + saturate(0.12 - depth) * 4.0 * saturate(speed - 0.3);
                float foam = smoothstep(0.55, 0.85, foamAmount * 0.7 + flowNoise * 0.5);
                // streaks along the current
                float2 dir = speed > 0.01 ? vel / max(length(vel), 1e-4) : float2(1, 0);
                float2 side = float2(-dir.y, dir.x);
                float streak = Noise(float2(dot(xz, dir) * 0.25 - t * speed * 0.35, dot(xz, side) * 1.6));
                streak = smoothstep(0.62, 0.85, streak) * saturate((speed - 0.15) / 1.2) * _StreakStrength;
                float white = saturate(foam + streak * 0.7);
                col = lerp(col, _FoamColor.rgb, white);

                // light: sun, sky reflection at grazing angles, highlight
                Light sun = GetMainLight();
                float3 viewDir = normalize(GetWorldSpaceViewDir(i.positionWS));
                float ndl = saturate(dot(n, sun.direction));
                float3 light = sun.color * (0.45 + 0.55 * ndl) + SampleSH(n) * 0.5;
                float3 lit = col * light;
                // clear water: the lit ground under the water, bent by the waves, minus what the water absorbs
                float2 suv = i.positionCS.xy / _ScaledScreenParams.xy;
                float3 under = SampleSceneColor(suv + n.xz * _Refract * saturate(depth * 2.0));
                // caustics: two layers of moving cell lines, strongest in shallow water, gone in deep water
                float2 cp = (xz + n.xz * 0.6) * _CausticScale;
                float cl = min(CellLines(cp, t * 1.1), CellLines(cp * 1.37 + 4.1, t * 0.8 + 2.0));
                float caust = (1.0 - smoothstep(0.0, 0.11, cl)) * _Caustics * saturate(depth * 3.0 - 0.15) * exp(-depth * 1.2);
                under += under * caust * sun.color * (1.0 - white);
                float3 trans = exp(-depth * _AbsorbRGB.rgb);
                float3 body = lerp(_Shallow.rgb, _Deep.rgb, absorbed) * light;   // light scattered back by the water itself
                float3 clear = under * trans + body * (1.0 - trans);
                clear = lerp(clear, _FoamColor.rgb * light, white);
                lit = lerp(lit, clear, _Clarity);
                float fresnel = pow(1.0 - saturate(dot(n, viewDir)), 5.0) * 0.9 + 0.04; // Schlick
                lit = lerp(lit, _Sky.rgb * (0.6 + 0.4 * sun.color), fresnel * (1.0 - white) * 0.6);
                float3 h = normalize(sun.direction + viewDir);
                lit += sun.color * pow(saturate(dot(n, h)), _Gloss) * 1.5 * (1.0 - white);

                alpha = saturate(max(alpha, white * 0.9 * smoothstep(0.0, _ShoreFade * 0.5, depth)) + fresnel * 0.2);
                // clear water already holds the ground colour, so it only fades out at the shore line
                alpha = lerp(alpha, smoothstep(0.0, _ShoreFade, depth), _Clarity);
                return float4(lit, alpha);
            }
            ENDHLSL
        }
    }
}
