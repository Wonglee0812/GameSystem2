using System.Collections.Generic;
using UnityEngine;

namespace DamRush
{
    /// <summary>
    /// White streaks that drift with the current, so the player can see where the water flows
    /// and how fast. Each streak is a small particle carried by the flow velocity of the water
    /// grid (a simple form of particle tracing of a flow field, the same idea as dye or foam
    /// floating on a river). Fast water gives long, bright streaks; still water (a pond behind
    /// a dam) has almost none. Drawn as one dynamic mesh of thin flat quads.
    /// </summary>
    [RequireComponent(typeof(WaterGrid))]
    public class FlowStreaks : MonoBehaviour
    {
        public WaterGrid water;
        public int count = 900;
        [Tooltip("Streaks move at this share of the real water speed (real speed looks too fast from far away).")]
        public float speedScale = 0.45f;
        public float maxVisualSpeed = 3f;
        public float minLife = 1.5f, maxLife = 3.5f;
        public float width = 0.07f;
        [Tooltip("Water slower than this shows no streaks (m/s).")]
        public float minSpeed = 0.25f;

        struct Streak { public Vector2 pos; public float age, life; }
        Streak[] streaks;
        readonly List<int> wetCells = new List<int>();
        float wetRefresh;
        Mesh mesh;
        Vector3[] verts;
        Color[] cols;
        GameObject drawer;

        void Start()
        {
            if (water == null) water = GetComponent<WaterGrid>();
            streaks = new Streak[count];
            verts = new Vector3[count * 4];
            cols = new Color[count * 4];
            var tris = new int[count * 6];
            for (int i = 0; i < count; i++)
            {
                int v = i * 4, t = i * 6;
                tris[t] = v; tris[t + 1] = v + 2; tris[t + 2] = v + 1;
                tris[t + 3] = v + 1; tris[t + 4] = v + 2; tris[t + 5] = v + 3;
                streaks[i].life = -1f; // spawn on the first update
            }
            mesh = new Mesh { name = "Flow streaks" };
            mesh.MarkDynamic();
            mesh.vertices = verts; mesh.colors = cols; mesh.triangles = tris;

            drawer = new GameObject("Flow Streaks", typeof(MeshFilter), typeof(MeshRenderer));
            drawer.GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = drawer.GetComponent<MeshRenderer>();
            var m = new Material(Shader.Find("Sprites/Default")) { renderQueue = 3050 }; // after the water
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        void RefreshWetCells()
        {
            wetCells.Clear();
            for (int i = 0; i < water.CellsX; i++)
                for (int j = 0; j < water.CellsZ; j++)
                {
                    int id = i * water.CellsZ + j;
                    if (!water.CellDeeper(id, 0.08f)) continue;
                    Vector3 c = water.CellCenter(id);
                    if (water.VelocityAt(c.x, c.z).magnitude > minSpeed) wetCells.Add(id);
                }
        }

        void Spawn(ref Streak s)
        {
            s.age = 0f;
            s.life = Random.Range(minLife, maxLife);
            if (wetCells.Count == 0) { s.pos = new Vector2(-100f, -100f); return; }
            Vector3 c = water.CellCenter(wetCells[Random.Range(0, wetCells.Count)]);
            float h = water.CellSize * 0.5f;
            s.pos = new Vector2(c.x + Random.Range(-h, h), c.z + Random.Range(-h, h));
        }

        void LateUpdate()
        {
            if (streaks == null || water.CellsX == 0) return;
            wetRefresh -= Time.deltaTime;
            if (wetRefresh <= 0f) { wetRefresh = 1f; RefreshWetCells(); }

            float dt = water.paused ? 0f : Time.deltaTime;
            for (int i = 0; i < streaks.Length; i++)
            {
                ref var s = ref streaks[i];
                s.age += dt;
                if (s.life < 0f || s.age > s.life) Spawn(ref s);

                Vector2 v = water.VelocityAt(s.pos.x, s.pos.y);
                float speed = v.magnitude;
                Vector2 vis = Vector2.ClampMagnitude(v * speedScale, maxVisualSpeed);
                s.pos += vis * dt;
                bool wet = water.DepthAt(s.pos.x, s.pos.y) > 0.05f && speed > minSpeed;
                if (!wet && s.age > 0.1f) s.age = s.life; // left the water: respawn next frame

                // fade in and out over the life, brighter in fast water
                float t = s.life > 0f ? s.age / s.life : 1f;
                float fade = Mathf.Clamp01(t * 4f) * Mathf.Clamp01((1f - t) * 3f);
                float alpha = wet ? fade * Mathf.Lerp(0.25f, 0.9f, Mathf.InverseLerp(minSpeed, 3f, speed)) : 0f;

                Vector2 dir = speed > 0.001f ? v / speed : Vector2.right;
                float len = 0.25f + Mathf.Min(vis.magnitude, maxVisualSpeed) * 0.35f;
                Vector2 side = new Vector2(-dir.y, dir.x) * width;
                Vector2 tail = s.pos - dir * len;
                float y = water.SurfaceAt(s.pos.x, s.pos.y, out _) + 0.03f;
                int k = i * 4;
                verts[k] = new Vector3(tail.x - side.x, y, tail.y - side.y);
                verts[k + 1] = new Vector3(tail.x + side.x, y, tail.y + side.y);
                verts[k + 2] = new Vector3(s.pos.x - side.x, y, s.pos.y - side.y);
                verts[k + 3] = new Vector3(s.pos.x + side.x, y, s.pos.y + side.y);
                var head = new Color(1f, 1f, 1f, alpha);
                var tailCol = new Color(1f, 1f, 1f, 0f);
                cols[k] = tailCol; cols[k + 1] = tailCol; cols[k + 2] = head; cols[k + 3] = head;
            }
            mesh.vertices = verts;
            mesh.colors = cols;
            mesh.RecalculateBounds();
        }

        void OnDestroy() { if (drawer != null) Destroy(drawer); }
    }
}
