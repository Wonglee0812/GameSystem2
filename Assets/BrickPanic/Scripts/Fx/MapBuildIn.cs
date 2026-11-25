using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace BrickPanic
{
    /// <summary>A new island builds itself brick by brick, like the tile-by-tile board change in Poker TD (Wong, 2026-11-24):
    /// the ground columns fly up from below, starting in the centre and moving out, with a small overshoot; then the map
    /// pieces (trees, rocks, huts) drop in and pop. The real ground (merged meshes and colliders) is hidden while it runs;
    /// the flying bricks are only drawn (GPU instancing), so 3000+ bricks stay cheap. Building is blocked until it ends.
    /// Plays on every new island: game start, F5 new island and RESTART.</summary>
    [RequireComponent(typeof(MapGenerator))]
    public class MapBuildIn : MonoBehaviour
    {
        [Tooltip("Off = the island just appears (for the juice ON / OFF play test).")]
        public bool play = true;
        [Tooltip("How far below its place a ground brick starts, in metres. Default 14.")]
        public float depth = 14f;
        [Tooltip("Seconds between the centre column and the outer columns starting (the wave from the centre out). Default 1.6.")]
        public float spread = 1.6f;
        [Tooltip("Seconds one column takes to rise. Default 0.55.")]
        public float pieceTime = 0.55f;
        [Tooltip("Random extra delay per column in seconds, so the wave is not a perfect circle. Default 0.2.")]
        public float jitter = 0.2f;
        [Tooltip("Map pieces start this many seconds after the ground under them. Default 0.35.")]
        public float pieceDelay = 0.35f;
        [Tooltip("Chance that a landing column puffs a little dust. Default 0.05.")]
        [Range(0f, 1f)] public float dustChance = 0.05f;

        /// <summary>True while the animation runs (BuildController does not build then).</summary>
        public static bool Busy { get; private set; }

        MapGenerator map;

        class Group
        {
            public Mesh mesh;
            public Material mat;
            public readonly List<Matrix4x4> local = new List<Matrix4x4>();
            public readonly List<float> delay = new List<float>();
            public bool[] landed;
        }

        void Awake() { map = GetComponent<MapGenerator>(); map.Generated += Play; }
        void OnDestroy() { if (map != null) map.Generated -= Play; Busy = false; }

        public void Play()
        {
            if (!play || !BPJuice.On || !isActiveAndEnabled) return;
            StopAllCoroutines();
            StartCoroutine(Run());
        }

        float Delay(Vector3 localPos, float maxR)
        {
            float r = new Vector2(localPos.x, localPos.z).magnitude / Mathf.Max(1f, maxR);
            float h = Mathf.Abs(Mathf.Sin(localPos.x * 12.9898f + localPos.z * 78.233f) * 43758.5453f) % 1f;   // same jitter for a column's top and body
            return r * spread + h * jitter;
        }

        IEnumerator Run()
        {
            Busy = true;
            float maxR = map.size * 0.5f * MapGenerator.Cell;
            var groups = new List<Group>();
            foreach (var kv in map.TopCells)
            {
                var g = new Group { mesh = map.topBrick, mat = map.GroundMaterial(kv.Key) };
                foreach (var m in kv.Value) { g.local.Add(m); g.delay.Add(Delay(m.GetColumn(3), maxR)); }
                groups.Add(g);
            }
            var under = new Group { mesh = MapGenerator.BoxMesh(), mat = map.underMat };
            foreach (var m in map.UnderCells) { under.local.Add(m); under.delay.Add(Delay(m.GetColumn(3), maxR)); }
            groups.Add(under);
            foreach (var g in groups)
            {
                g.landed = new bool[g.local.Count];
                if (g.mat != null && !g.mat.enableInstancing) g.mat.enableInstancing = true;
            }
            foreach (var r in map.GroundRenderers) if (r != null) r.enabled = false;

            // map pieces: hidden until their turn
            var pieces = new List<(Transform t, Vector3 pos, float delay)>();
            if (map.dresser != null && map.dresser.Root != null)
                foreach (Transform t in map.dresser.Root)
                {
                    Vector3 lp = transform.InverseTransformPoint(t.position);
                    pieces.Add((t, t.position, Delay(lp, maxR) + pieceDelay));
                    t.localScale = Vector3.zero;
                }
            pieces.Sort((a, b) => a.delay.CompareTo(b.delay));
            int nextPiece = 0;

            float end = spread + jitter + pieceDelay + pieceTime + 0.6f;
            var batch = new Matrix4x4[1023];
            for (float t = 0f; t < end; t += Time.deltaTime)
            {
                var toWorld = transform.localToWorldMatrix;
                foreach (var g in groups)
                {
                    if (g.mat == null) continue;
                    var rp = new RenderParams(g.mat)
                    {
                        shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                        receiveShadows = true,
                        worldBounds = new Bounds(transform.position, Vector3.one * (maxR * 2f + depth * 2f + 40f)),
                        layer = gameObject.layer
                    };
                    int n = 0;
                    for (int i = 0; i < g.local.Count; i++)
                    {
                        float p = (t - g.delay[i]) / pieceTime;
                        if (p <= 0f) continue;
                        if (p >= 1f)
                        {
                            p = 1f;
                            if (!g.landed[i])
                            {
                                g.landed[i] = true;
                                if (g != under && Random.value < dustChance) Dust(toWorld.MultiplyPoint(g.local[i].GetColumn(3)) + Vector3.up * 0.6f);
                            }
                        }
                        float y = -(1f - OutBack(p)) * depth;
                        batch[n++] = toWorld * Matrix4x4.Translate(new Vector3(0f, y, 0f)) * g.local[i];
                        if (n == batch.Length) { Graphics.RenderMeshInstanced(rp, g.mesh, 0, batch, n); n = 0; }
                    }
                    if (n > 0) Graphics.RenderMeshInstanced(rp, g.mesh, 0, batch, n);
                }

                while (nextPiece < pieces.Count && t >= pieces[nextPiece].delay)
                {
                    var (pt, pos, _) = pieces[nextPiece++];
                    if (pt == null) continue;
                    pt.position = pos + Vector3.up * 3f;
                    pt.DOMove(pos, 0.35f).SetEase(Ease.InQuad).SetLink(pt.gameObject);
                    pt.DOScale(1f, 0.45f).SetEase(Ease.OutBack).SetLink(pt.gameObject);
                }
                yield return null;
            }

            foreach (var (pt, pos, _) in pieces) if (pt != null) { pt.DOKill(); pt.position = pos; pt.localScale = Vector3.one; }
            foreach (var r in map.GroundRenderers) if (r != null) r.enabled = true;
            Busy = false;
        }

        static float OutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        static void Dust(Vector3 at)
        {
            var c = new Color(1f, 0.93f, 0.75f);
            for (int k = 0; k < 3; k++)
            {
                Vector3 d = Random.insideUnitCircle.normalized;
                BrickFx.Emit(at, new Vector3(d.x, 0.6f, d.y) * Random.Range(1.5f, 3f), Random.Range(0.4f, 0.7f), 0.3f, c, c * 0.8f, 0f, 0.3f, 2f);
            }
        }
    }
}
