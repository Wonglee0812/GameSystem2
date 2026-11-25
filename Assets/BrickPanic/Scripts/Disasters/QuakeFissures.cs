using System.Collections.Generic;
using UnityEngine;

namespace BrickPanic
{
    /// <summary>Ground hazard of the earthquake (Wong, 2026-11-25): cracks open in open ground while the island shakes.
    /// A crack is a zig-zag row of dark sunken bricks that widens with the quake strength. People standing on it take
    /// damage and fall; bricks standing on it wear faster. Cracks start only where nothing is built, so open ground is
    /// no longer the safe choice. They close (break apart) when the quake ends.</summary>
    public class QuakeFissures : MonoBehaviour
    {
        [Header("References")]
        public RoundManager round;
        public MapGenerator map;
        [Tooltip("Dark material of the crack bricks.")]
        public Material crackMaterial;
        [Tooltip("Mesh of a crack segment. Empty = a box, half sunk into the ground (looks like a dark gap).")]
        public Mesh segmentMesh;

        [Header("Cracks")]
        [Tooltip("Cracks in a level-1 quake. Default 4.")]
        public int cracks = 4;
        [Tooltip("Extra cracks per level above 1. Default 1.")]
        public int cracksPerLevel = 1;
        [Tooltip("Length of one crack in cells (random between x and y). Default 10 - 20.")]
        public Vector2Int length = new Vector2Int(10, 20);
        [Tooltip("Widest crack in metres at full quake strength. Default 1.4.")]
        public float maxWidth = 1.4f;
        [Tooltip("Damage per second to a person standing on a crack. Default 35.")]
        public float damagePerSecond = 35f;
        [Tooltip("Joint wear per second (seconds of wear) for a brick standing on a crack. Default 3.")]
        public float brickWear = 3f;
        [Tooltip("Intensity at which cracks start to open (0-1). Default 0.35.")]
        [Range(0f, 1f)] public float openAt = 0.35f;

        class Segment { public Vector3 pos; public Transform t; public float delay; }

        readonly List<Segment> segments = new List<Segment>();
        Earthquake quake;
        bool active;
        float openTime, width, nextTick;

        void Start()
        {
            if (round == null) round = FindFirstObjectByType<RoundManager>();
            if (map == null) map = FindFirstObjectByType<MapGenerator>();
            quake = FindFirstObjectByType<Earthquake>();
        }

        void Update()
        {
            if (round == null || map == null || quake == null || crackMaterial == null) return;
            bool running = round.Current == RoundManager.State.Disaster && round.Next == quake && quake.Running;
            if (running && !active && DisasterState.Intensity >= openAt) Open();
            if (!running && active) Close();
            if (!active) return;

            openTime += Time.deltaTime;
            width = Mathf.MoveTowards(width, maxWidth * Mathf.Clamp01(DisasterState.Intensity), Time.deltaTime * 0.8f);
            foreach (var s in segments)
            {
                if (s.t == null) continue;
                float k = Mathf.Clamp01((openTime - s.delay) * 2f);   // each crack runs along its length
                s.t.localScale = new Vector3(Mathf.Max(0.05f, width * k), 0.3f, 1.05f);
                if (k > 0.5f && Random.value < 0.004f)   // a little light dust from the crack
                {
                    var dust = new Color(0.9f, 0.85f, 0.72f);
                    BrickFx.Emit(s.pos + Vector3.up * 0.3f, new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(1.5f, 2.5f), Random.Range(-0.5f, 0.5f)),
                                 Random.Range(0.8f, 1.2f), 0.35f, dust, new Color(dust.r, dust.g, dust.b, 0f), 0f, -0.05f, 1.5f);
                }
            }
            if (Time.time >= nextTick) { nextTick = Time.time + 0.25f; Hurt(0.25f); }
        }

        void Open()
        {
            active = true;
            openTime = 0f;
            width = 0f;
            int level = Mathf.Max(1, round.Round);
            int count = cracks + cracksPerLevel * (level - 1);
            for (int c = 0; c < count; c++) MakeCrack();
            BPJuice.AddTrauma(0.3f);
        }

        void MakeCrack()
        {
            // start on an open cell (nothing built within 2 m) away from the coast
            for (int tries = 0; tries < 40; tries++)
            {
                int i = Random.Range(4, map.size - 4), j = Random.Range(4, map.size - 4);
                if (!map.Inside(i, j) || map.Zones[i, j] == MapGenerator.ZoneType.Volcano || map.Zones[i, j] == MapGenerator.ZoneType.Crater) continue;
                Vector3 p = CellTop(i, j);
                if (Built(p)) continue;
                float ang = Random.Range(0f, Mathf.PI * 2f);
                int len = Random.Range(length.x, length.y + 1);
                float x = i, z = j, delay = 0f;
                for (int k = 0; k < len; k++)
                {
                    ang += Random.Range(-0.5f, 0.5f);   // zig-zag
                    x += Mathf.Cos(ang);
                    z += Mathf.Sin(ang);
                    int ci = Mathf.RoundToInt(x), cj = Mathf.RoundToInt(z);
                    if (!map.Inside(ci, cj)) break;
                    if (map.Zones[ci, cj] == MapGenerator.ZoneType.Volcano || map.Zones[ci, cj] == MapGenerator.ZoneType.Crater) break;
                    Vector3 pos = CellTop(ci, cj);
                    var go = new GameObject("Crack");
                    go.transform.SetParent(map.transform, true);
                    go.transform.SetPositionAndRotation(pos + Vector3.up * 0.02f, Quaternion.LookRotation(new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang))));
                    go.AddComponent<MeshFilter>().sharedMesh = segmentMesh != null ? segmentMesh : MapGenerator.BoxMesh();
                    var r = go.AddComponent<MeshRenderer>();
                    r.sharedMaterial = crackMaterial;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    go.transform.localScale = new Vector3(0.05f, 0.3f, 1.05f);
                    segments.Add(new Segment { pos = pos, t = go.transform, delay = delay });
                    delay += 0.06f;
                }
                return;
            }
        }

        bool Built(Vector3 p)
        {
            foreach (var b in Block.All) if (b != null && b.placedByPlayer && (b.Centre - p).sqrMagnitude < 9f) return true;
            return false;
        }

        Vector3 CellTop(int i, int j)
        {
            var p = map.transform.TransformPoint(map.CellCentre(i, j));
            p.y = map.TopY(i, j);
            return p;
        }

        void Hurt(float dt)
        {
            float half = width * 0.5f + 0.15f;
            if (half < 0.25f) return;
            foreach (var p in Person.All)
            {
                if (p == null || !p.Alive) continue;
                Vector3 pos = p.transform.position;
                foreach (var s in segments)
                {
                    if (s.t == null || s.t.localScale.x < 0.3f) continue;
                    Vector3 local = s.t.InverseTransformPoint(pos);
                    if (Mathf.Abs(local.x * s.t.localScale.x) > half || Mathf.Abs(local.z) > 0.55f || Mathf.Abs(pos.y - s.pos.y) > 1.2f) continue;
                    p.Damage(damagePerSecond * dt, "crack");
                    if (p.Alive && Random.value < 0.3f) p.Swept(Vector3.down);
                    break;
                }
            }
            if (PhysicsActivator.Instance == null) return;
            foreach (var b in Block.All)
            {
                if (b == null || b.Body == null) continue;
                Vector3 c = b.transform.position;
                foreach (var s in segments)
                {
                    if (s.t == null) continue;
                    Vector3 d = c - s.pos;
                    if (Mathf.Abs(d.y) < 1f && new Vector2(d.x, d.z).sqrMagnitude < half * half + 0.25f) { PhysicsActivator.Instance.DamageBlock(b, brickWear * dt); break; }
                }
            }
        }

        void Close()
        {
            active = false;
            foreach (var s in segments)
            {
                if (s.t == null) continue;
                BrickFx.Break(s.pos + Vector3.up * 0.2f, new Vector3(0.8f, 0.3f, 0.8f), new Color(0.25f, 0.2f, 0.22f), 0.5f);
                Destroy(s.t.gameObject);
            }
            segments.Clear();
        }
    }
}
