using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// Draws the outer wind layer as crescent-shaped arcs that wrap around the
    /// whirlwind and keep circling. Each arc is a LineRenderer that is thin and
    /// faded at both ends and thick in the middle, like a half moon.
    /// The arcs follow the height and radius curve of a WindVortex.
    /// </summary>
    [ExecuteAlways]
    public class WindArcs : MonoBehaviour
    {
        [Tooltip("Takes height and radius from this vortex so all layers match.")]
        public WindVortex shapeSource;
        [Tooltip("Optional. Bends the arcs when the pet moves.")]
        public PetBend bend;
        public Material material;
        public Color color = new Color(0.85f, 0.95f, 1f, 1f);

        [Header("Arcs")]
        public int arcCount = 7;
        [Tooltip("Length of each arc in degrees around the body.")]
        public float arcLength = 170f;
        public int segments = 28;
        public float width = 0.05f;
        [Tooltip("Arcs sit a bit outside the soft particles.")]
        public float radiusPadding = 1.15f;
        [Range(0f, 1f)] public float lowestHeight = 0.12f;
        [Range(0f, 1f)] public float highestHeight = 0.9f;

        [Header("Motion")]
        [Tooltip("Degrees per second.")]
        public float spinSpeed = 220f;
        [Tooltip("Arcs are tilted a little, so they look like a spiral.")]
        public float tilt = 0.12f;
        public int randomSeed = 7;

        [Header("Thinning (set by WindPhase)")]
        public float spread = 1f;
        [Range(0f, 1f)] public float fade = 1f;
        [Tooltip("Colour multiplier, set by PetLook (temperature and mood).")]
        public Color tint = Color.white;
        [Tooltip("Speed multiplier, set by the pet's behaviour (slow when asleep, fast in a storm).")]
        public float spinMultiplier = 1f;
        float spinAngle;
        float lastFade = -1f;
        Color lastTint = Color.white;
        Gradient gradient;

        LineRenderer[] lines;
        float[] heights, phases, speedFactors, tiltSigns;

        void OnEnable() => Build();

        void OnDisable() => Clear();

        bool needsRebuild;

        // Unity does not allow creating objects inside OnValidate, so only mark it here.
        void OnValidate() => needsRebuild = true;

        void Build()
        {
            Clear();
            var random = new System.Random(randomSeed);
            lines = new LineRenderer[arcCount];
            heights = new float[arcCount];
            phases = new float[arcCount];
            speedFactors = new float[arcCount];
            tiltSigns = new float[arcCount];

            var widthCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));
            gradient = MakeGradient(1f);
            lastFade = 1f;

            for (int i = 0; i < arcCount; i++)
            {
                var go = new GameObject("Arc_" + i);
                go.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
                go.transform.SetParent(transform, false);

                var line = go.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.positionCount = segments;
                line.sharedMaterial = material;
                line.widthCurve = widthCurve;
                line.widthMultiplier = width;
                line.colorGradient = gradient;
                line.numCapVertices = 2;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                lines[i] = line;

                // Spread arcs evenly over the height, with a little randomness.
                float t = arcCount == 1 ? 0.5f : i / (float)(arcCount - 1);
                heights[i] = Mathf.Lerp(lowestHeight, highestHeight, t) + ((float)random.NextDouble() - 0.5f) * 0.05f;
                phases[i] = (float)random.NextDouble() * 360f;
                speedFactors[i] = 0.8f + (float)random.NextDouble() * 0.4f;
                tiltSigns[i] = random.NextDouble() < 0.5 ? -1f : 1f;
            }
        }

        static bool Similar(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < 0.02f;

        Gradient MakeGradient(float f)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(color * tint, 0f), new GradientColorKey(color * tint, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(color.a * f, 0.5f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (!child.name.StartsWith("Arc_")) continue;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
            lines = null;
        }

        void LateUpdate()
        {
            if (needsRebuild && isActiveAndEnabled) { needsRebuild = false; Build(); }
            if (lines == null || shapeSource == null) return;
            float time = Application.isPlaying ? Time.time : (float)Time.realtimeSinceStartup;
            float height = shapeSource.height;
            spinAngle += spinSpeed * spinMultiplier * (Application.isPlaying ? Time.deltaTime : 1f / 60f);
            // only touch the colours when the fade really changed (avoids work every frame)
            if (Mathf.Abs(fade - lastFade) > 0.01f || !Similar(tint, lastTint))
            {
                lastFade = fade;
                lastTint = tint;
                gradient = MakeGradient(fade);
                foreach (var l in lines)
                    if (l != null) { l.colorGradient = gradient; l.widthMultiplier = width * Mathf.Lerp(0.5f, 1f, fade); }
            }

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == null) continue;
                float h01 = heights[i];
                float radius = shapeSource.radiusProfile.Evaluate(h01) * shapeSource.radiusScale * radiusPadding * spread;
                float start = phases[i] + spinAngle * speedFactors[i];

                for (int s = 0; s < segments; s++)
                {
                    float a = (start + s / (float)(segments - 1) * arcLength) * Mathf.Deg2Rad;
                    float y = h01 * height + Mathf.Sin(a) * tilt * tiltSigns[i];
                    Vector3 point = new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius);
                    if (bend != null) point += bend.OffsetAt(y / height);
                    lines[i].SetPosition(s, point);
                }
            }
        }
    }
}
