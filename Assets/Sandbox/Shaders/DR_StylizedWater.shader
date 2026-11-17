// Stylised water for Dam Rush (URP).
// - Painted base colour from the vertex colours of the water grid (depth, shore, flood tint).
// - Soft white "caustic" lines from the edges of a cellular (Voronoi) pattern, F2 - F1 (Worley, 1996), carried by the
//   flow velocity of the water grid with the two-phase flow-map method (Vlachos, 2010).
//   Still water (a pond behind a dam) keeps slowly breathing cells; running water moves the
//   cells downstream and draws a short white trail behind each line in the flow direction.
// - Foam from the water grid (shore, water against or over a dam, fast water).
// - Ripple rings around swimming beavers and splashes (positions set by WaterRipples.cs).
// - Small sparkles in daylight.
// - Flow streaks: long thin white streaks stretched along the current. They get brighter and
//   move faster in fast water, so the player sees where the water goes and how strong it is
//   (look inspired by the running water of Sandcastle, Bubblebird Studio). The cell lines fade
//   out in fast water and stay in still ponds.
// - Time comes from _DR_Time (real seconds, set by WaterGrid.cs), so the look does not
//   speed up at 3x or 5x game speed.
// - Clear water (2026-11-14, from the water compare scene): the ground under the water is seen
//   through it, bent a little by the waves (refraction from the URP opaque texture). Light is
//   absorbed per colour, red first, so shallow water is clear and deep water turns blue-green
//   (Beer-Lambert law); flood water absorbs much more, so it turns muddy. Moving caustics (cell
//   lines, Worley, 1996) light up the bed of shallow water. _Clarity 0 = the old painted look.
// UV1 = flow velocity (m/s) on x/z, UV2 = (foam 0-1, depth m).
Shader "DamRush/StylizedWater"
{
    Properties
    {
        _CellScale ("Pattern cells per metre", Float) = 0.27
        _LineWidth ("Line width", Range(0.005, 0.3)) = 0.035
        _LineColor ("Line colour", Color) = (1, 1, 1, 1)
        _LineStill ("Line strength in still water", Range(0, 1)) = 0.5
        _LineFast ("Extra line strength in fast water", Range(0, 1)) = 0.2
        _FlowSpeed ("Flow cycles per second", Float) = 0.35
        _FlowScale ("Flow advection (m per m/s per cycle)", Float) = 0.6
        _Trail ("Trail length behind lines in running water", Float) = 0.35
        _Wobble ("Cell wobble speed", Float) = 0.5
        _Warp ("Painted warp", Range(0, 2)) = 1.6
        _Tone ("Painted tone variation", Range(0, 0.4)) = 0.14
        _Pillow ("Light cell centres", Range(0, 0.3)) = 0.1
        _Sparkle ("Sparkles", Range(0, 2)) = 0.8
        _RippleStrength ("Ripple strength", Range(0, 2)) = 0.9
        _Brightness ("Brightness", Range(0.5, 2)) = 1.0
        _StreakStrength ("Flow streak strength", Range(0, 1.5)) = 1.1
        _StreakLength ("Flow streak length (m)", Float) = 6.0
        _StreakWidth ("Flow streak spacing across (m)", Float) = 0.9
        _StreakSpeed ("Streak speed per m/s of water", Float) = 0.9
        _StreakFrom ("Water speed where streaks start (m/s)", Float) = 0.15
        _StreakFull ("Water speed with full streaks (m/s)", Float) = 1.2
        _CalmLines ("Cell lines kept in fast water", Range(0, 1)) = 0.25
        _MaxVisualSpeed ("Highest water speed the look uses (m/s)", Float) = 3.0
        _Clarity ("Clear water (0 = painted, 1 = see-through)", Range(0, 1)) = 1
        _AbsorbRGB ("Clear water: absorption per metre (r, g, b)", Vector) = (2.8, 1.1, 0.8, 0)
        _Refract ("Refraction strength", Range(0, 0.1)) = 0.03
        _Caustics ("Caustics strength", Range(0, 2)) = 0.45
        _CausticScale ("Caustics size (cells per metre)", Float) = 0.9
        _ShoreFade ("Shore fade depth (m)", Float) = 0.06
        _ClearLines ("Cell lines kept in clear water", Range(0, 1)) = 0.3
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
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
            #pragma multi_compile_fog
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _CellScale, _LineWidth, _LineStill, _LineFast, _FlowSpeed, _FlowScale, _Trail;
                float _Wobble, _Warp, _Tone, _Pillow, _Sparkle, _RippleStrength, _Brightness;
                float4 _LineColor;
                float _StreakStrength, _StreakLength, _StreakWidth, _StreakSpeed, _StreakFrom, _StreakFull, _CalmLines, _MaxVisualSpeed;
                float4 _AbsorbRGB;
                float _Clarity, _Refract, _Caustics, _CausticScale, _ShoreFade, _ClearLines;
            CBUFFER_END
            float _DR_Flood; // 0-1 flood strength (set by WaterGrid.cs): flood water is muddy
            float _DR_Time; // real seconds (unscaled), set by WaterGrid.cs

            #define MAX_RIPPLES 24
            float4 _DR_Ripples[MAX_RIPPLES]; // x, z, strength, radius
            float _DR_RippleCount;
            float _DR_Ice; // 0 = open water, 1 = thickest winter ice (set by Seasons.cs)

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                float2 flow : TEXCOORD1;
                float2 foam : TEXCOORD2;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 color : COLOR;
                float2 flow : TEXCOORD2;
                float2 foam : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.color = v.color;
                o.flow = v.flow;
                o.foam = v.foam;
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            float2 Hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash2(i).x, b = Hash2(i + float2(1, 0)).x, c = Hash2(i + float2(0, 1)).x, d = Hash2(i + float2(1, 1)).x;
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // x = distance to the nearest cell point (F1), y = F2 - F1 (small on the cell edges)
            float2 Voronoi(float2 x, float t)
            {
                float2 n = floor(x), f = frac(x);
                float f1 = 8.0, f2 = 8.0;
                [unroll] for (int j = -1; j <= 1; j++)
                [unroll] for (int i = -1; i <= 1; i++)
                {
                    float2 g = float2(i, j);
                    float2 o = Hash2(n + g);
                    o = 0.5 + 0.45 * sin(t + 6.2831 * o); // the cell points drift slowly
                    float d = length(g + o - f);
                    if (d < f1) { f2 = f1; f1 = d; }
                    else if (d < f2) f2 = d;
                }
                return float2(f1, f2 - f1);
            }

            // white lines on the cell edges, anti-aliased with the screen-space derivative
            float Lines(float2 p, float t, float width)
            {
                float2 v = Voronoi(p, t);
                float aa = max(fwidth(v.y), 0.002);
                return 1.0 - smoothstep(width, width + aa * 1.5, v.y);
            }

            float4 frag(Varyings i) : SV_Target
            {
                float t = _DR_Time > 0.0 ? _DR_Time : _Time.y;
                float2 xz = i.positionWS.xz;
                float2 vel = i.flow;
                float speed = length(vel);
                // very fast flood water would stretch the pattern into noise: the look uses at most _MaxVisualSpeed
                if (speed > _MaxVisualSpeed) { vel *= _MaxVisualSpeed / speed; speed = _MaxVisualSpeed; }
                float2 dir = speed > 0.001 ? vel / speed : float2(1, 0);
                float foam = saturate(i.foam.x);

                // painted warp: soft, slow distortion so the cells are not perfect polygons
                float2 warp = (float2(Noise(xz * 0.22 + t * 0.03), Noise(xz * 0.22 + 31.7 - t * 0.025)) - 0.5) * _Warp;
                float2 p = (xz + warp) * _CellScale;

                // flow map: two layers half a cycle apart, each pushed downstream and reset (Vlachos, 2010)
                float phase = t * _FlowSpeed;
                float ph0 = frac(phase), ph1 = frac(phase + 0.5);
                float blend = abs(1.0 - 2.0 * ph0); // weight of layer 1
                float2 adv = vel * _FlowScale * _CellScale;
                float2 pA = p - adv * ph0;
                float2 pB = p - adv * ph1 + float2(0.37, 0.71);
                float wob = t * _Wobble;
                float width = _LineWidth * (1.0 + 0.25 * saturate(speed / 2.0));

                float lineA = Lines(pA, wob, width);
                float lineB = Lines(pB, wob + 1.7, width);
                // running water: a trail upstream of each line, so the white ripples point along the flow
                if (speed > 0.25)
                {
                    float2 back = dir * _Trail * saturate(speed / 2.5) * _CellScale * 2.0;
                    lineA = max(lineA, Lines(pA - back, wob, width * 0.7) * 0.55);
                    lineB = max(lineB, Lines(pB - back, wob + 1.7, width * 0.7) * 0.55);
                }
                float lines = lerp(lineA, lineB, blend);

                // flow streaks in a frame turned to the current: long along it, thin across it, moving downstream
                float flowAmount = smoothstep(_StreakFrom, _StreakFull, speed);
                float streaks = 0.0;
                if (flowAmount > 0.001)
                {
                    float2 side = float2(-dir.y, dir.x);
                    float along = dot(xz, dir), across = dot(xz, side);
                    float2 s1 = float2(along / _StreakLength - t * _StreakSpeed * speed / _StreakLength, across / _StreakWidth);
                    float n1 = Noise(s1 * float2(1.0, 1.0)) * 0.65 + Noise(s1 * float2(2.1, 2.3) + 5.3) * 0.35;
                    float band = 1.0 - abs(frac(across / _StreakWidth + Noise(float2(along * 0.15, 3.1)) * 0.6) * 2.0 - 1.0); // thin lines across
                    streaks = smoothstep(0.52, 0.72, n1) * smoothstep(0.45, 0.9, band);
                    streaks *= flowAmount * _StreakStrength * (0.6 + 0.4 * saturate(speed / 2.0));
                }
                // still water keeps its cells; in running water the cells fade and the streaks take over
                lines *= lerp(1.0, _CalmLines, flowAmount);

                // pillow look: cell centres a little lighter than the edges
                float2 cellA = Voronoi(pA, wob);
                float pillow = 1.0 + _Pillow * (0.5 - saturate(cellA.x * 1.3));

                float3 col = i.color.rgb;
                float tone = Noise(xz * 0.13 + t * 0.015) * 0.6 + Noise(xz * 0.47 - t * 0.02) * 0.4;
                col *= (1.0 + (tone - 0.5) * 2.0 * _Tone) * pillow * _Brightness;

                // foam: broken white patches where the grid says the water is rough
                float foamNoise = Noise(xz * 1.6 - vel * t * 0.5) * 0.6 + Noise(xz * 3.3 + t * 0.4) * 0.4;
                float foamMask = smoothstep(0.25, 0.55, foam * 1.2 - foamNoise * 0.55);

                float lineAmount = lines * saturate(_LineStill + _LineFast * saturate(speed / 2.5) + foam * 0.5);

                // ripple rings around swimmers and splashes
                float ripple = 0.0;
                int count = (int)_DR_RippleCount;
                [loop] for (int k = 0; k < MAX_RIPPLES; k++)
                {
                    if (k >= count) break;
                    float4 r = _DR_Ripples[k];
                    float d = distance(xz, r.xy);
                    if (d > r.w) continue;
                    float wave = sin(d * 7.0 - t * 5.0);
                    float ring = smoothstep(0.82, 0.97, wave);
                    float fade = 1.0 - d / r.w;
                    ripple = max(ripple, ring * fade * fade * r.z);
                }
                ripple *= _RippleStrength;

                // clear water: the painted cell lines are mostly replaced by the caustics on the bed
                lineAmount *= lerp(1.0, _ClearLines, _Clarity);
                float white = saturate(lineAmount + streaks * lerp(1.0, 0.7, _Clarity) + foamMask * 0.85 + ripple);
                col = lerp(col, _LineColor.rgb, white);

                // winter ice: a pale, cracked cover that grows with the ice thickness
                float ice = saturate(_DR_Ice);
                if (ice > 0.001)
                {
                    float2 cr = Voronoi(xz * 0.35 + 11.3, 0.0);
                    float crack = 1.0 - smoothstep(0.02, 0.05, cr.y);
                    float3 iceCol = float3(0.80, 0.89, 0.95) * (0.92 + 0.08 * Noise(xz * 0.8));
                    iceCol = lerp(iceCol, float3(1, 1, 1), crack * 0.6);
                    col = lerp(col, iceCol, ice * 0.85);
                }

                // lighting: follows the day/night cycle of the sun and the ambient light
                Light sun = GetMainLight();
                float3 n = normalize(i.normalWS);
                float ndl = saturate(dot(n, sun.direction));
                float3 light = sun.color * (0.55 + 0.45 * ndl) + SampleSH(n) * 0.45;
                col *= light;

                // clear water: the lit ground under the water, bent by the waves, minus what the water absorbs
                float clarity = _Clarity * (1.0 - ice);
                if (clarity > 0.001)
                {
                    float depthW = max(0.0, i.foam.y); // shore points carry a depth below zero
                    float2 suv = i.positionCS.xy / _ScaledScreenParams.xy;
                    float3 under = SampleSceneColor(suv + n.xz * _Refract * saturate(depthW * 2.0));
                    // caustics: two layers of moving cell lines, strongest in shallow water, gone in deep water
                    float2 cp = (xz + n.xz * 0.6) * _CausticScale;
                    float cl = min(Voronoi(cp, t * 1.1).y, Voronoi(cp * 1.37 + 4.1, t * 0.8 + 2.0).y);
                    float caust = (1.0 - smoothstep(0.0, 0.11, cl)) * _Caustics * saturate(depthW * 3.0 - 0.15) * exp(-depthW * 1.2);
                    under += under * caust * sun.color * (1.0 - white) * (1.0 - _DR_Flood);
                    float3 trans = exp(-depthW * _AbsorbRGB.rgb * (1.0 + _DR_Flood * 6.0));
                    float3 body = i.color.rgb * light;       // light scattered back by the water itself
                    // at least a light tint even in very shallow water, so it still reads as water
                    float3 clear = lerp(under * trans + body * (1.0 - trans), body, 0.18);
                    clear = lerp(clear, _LineColor.rgb * light, white);
                    col = lerp(col, clear, clarity);
                }

                // sparkles: tiny twinkling points, only in daylight
                float2 sp = xz * 2.2;
                float2 cellId = floor(sp);
                float2 h = Hash2(cellId + 7.1);
                float2 local = frac(sp) - 0.5 - (h - 0.5) * 0.6;
                float twinkle = pow(saturate(sin(t * (1.5 + h.x * 2.0) + h.y * 40.0)), 24.0) * step(0.9, h.x);
                float star = saturate(0.04 / (abs(local.x) * 40.0 + abs(local.y) * 3.0 + 0.04) + 0.04 / (abs(local.y) * 40.0 + abs(local.x) * 3.0 + 0.04)) ;
                star *= smoothstep(0.25, 0.0, length(local));
                col += twinkle * star * _Sparkle * saturate(Luminance(sun.color) - 0.4) * (1.0 - foamMask);

                float alpha = i.color.a;
                alpha = lerp(alpha, max(alpha, 0.92 * step(0.001, alpha)), saturate(_DR_Ice));
                alpha = saturate(max(alpha, alpha * 0.5 + white * 0.6 * step(0.001, alpha)));
                // clear water already holds the ground colour, so it only fades out at the shore line
                alpha = lerp(alpha, step(0.001, i.color.a) * smoothstep(0.0, _ShoreFade, i.foam.y), _Clarity * (1.0 - saturate(_DR_Ice)));

                col = MixFog(col, i.fogFactor);
                return float4(col, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
