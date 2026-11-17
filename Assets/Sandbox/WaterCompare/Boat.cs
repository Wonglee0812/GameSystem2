using UnityEngine;

namespace DamRush.WaterCompare
{
    /// <summary>
    /// A small boat that floats on one lane's water: it sits on the surface, is carried by the
    /// current, slides down the slope of the surface, tilts with the waves, runs aground in
    /// shallow water and bounces off walls and blocks. It also pushes the water a little (wake).
    /// It reads the water through WaterSimBase.Sample, so it works on all versions.
    /// </summary>
    public class Boat : MonoBehaviour
    {
        public WaterSimBase sim;
        Vector2 pos, vel;
        float y, yaw, wakeTimer;
        const float Draft = 0.12f; // water depth needed to float (m)

        public void Init(WaterSimBase s, Vector2 p) { sim = s; pos = p; y = 10f; yaw = Random.Range(0f, 360f); }

        void Update()
        {
            float dt = Time.deltaTime;
            if (sim == null || dt <= 0f) return;
            if (!sim.Sample(pos, out float g, out float d, out Vector2 wv)) return;
            float surf = g + d;
            bool afloat = d > Draft;

            // slope of the water surface around the boat
            Vector2 grad = Vector2.zero;
            if (sim.Sample(pos + new Vector2(0.5f, 0f), out float g1, out float d1, out _) && sim.Sample(pos - new Vector2(0.5f, 0f), out float g2, out float d2, out _)
                && sim.Sample(pos + new Vector2(0f, 0.5f), out float g3, out float d3, out _) && sim.Sample(pos - new Vector2(0f, 0.5f), out float g4, out float d4, out _))
                grad = new Vector2((g1 + d1) - (g2 + d2), (g3 + d3) - (g4 + d4));
            grad = Vector2.ClampMagnitude(grad, 0.6f);

            if (afloat)
            {
                vel += (wv - vel) * Mathf.Min(1f, 2.5f * dt); // carried by the current
                vel -= grad * 9.81f * 0.35f * dt;             // slides down the surface
            }
            else vel *= Mathf.Max(0f, 1f - 5f * dt);         // aground

            Vector2 next = pos + vel * dt;
            // walls, blocks and banks higher than the water stop the boat
            if (sim.Sample(next, out float gn, out float dn, out _) && dn < Draft && gn > surf - 0.05f) { vel = -vel * 0.3f; next = pos; }
            next.x = Mathf.Clamp(next.x, 0.5f, CompareShape.LengthX - 0.5f);
            next.y = Mathf.Clamp(next.y, 0.5f, CompareShape.WidthZ - 0.5f);
            pos = next;

            float targetY = afloat ? surf - 0.05f : g;
            y = y > targetY + 0.3f ? y - 12f * dt : Mathf.Lerp(y, targetY, Mathf.Min(1f, 10f * dt)); // falls in when dropped
            if (vel.sqrMagnitude > 0.05f) yaw = Mathf.LerpAngle(yaw, Mathf.Atan2(vel.x, vel.y) * Mathf.Rad2Deg, 2f * dt);
            var tilt = Quaternion.Euler(Mathf.Atan(grad.y) * Mathf.Rad2Deg * 1.5f, 0f, -Mathf.Atan(grad.x) * Mathf.Rad2Deg * 1.5f);
            transform.localPosition = new Vector3(pos.x, y, pos.y);
            transform.localRotation = tilt * Quaternion.Euler(0f, yaw, 0f);

            // a small wake behind a moving boat
            wakeTimer -= dt;
            if (afloat && wakeTimer <= 0f && (vel - wv).sqrMagnitude > 0.1f) { wakeTimer = 0.15f; sim.Push(pos, 0.7f, vel); }
        }

        /// <summary>Builds a simple boat out of boxes (hull, cabin, mast).</summary>
        public static Boat Create(Transform lane, WaterSimBase sim, Vector2 at, Material hull, Material cabin)
        {
            var root = new GameObject("Boat");
            root.transform.SetParent(lane, false);
            Part(root.transform, new Vector3(0f, 0.12f, 0f), new Vector3(0.7f, 0.3f, 1.6f), hull);
            Part(root.transform, new Vector3(0f, 0.12f, 0.85f), new Vector3(0.45f, 0.26f, 0.25f), hull);
            Part(root.transform, new Vector3(0f, 0.42f, -0.25f), new Vector3(0.45f, 0.3f, 0.5f), cabin);
            Part(root.transform, new Vector3(0f, 0.8f, 0.25f), new Vector3(0.05f, 1.0f, 0.05f), cabin);
            var b = root.AddComponent<Boat>();
            b.Init(sim, at);
            return b;
        }

        static void Part(Transform parent, Vector3 p, Vector3 size, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = p; go.transform.localScale = size;
            if (m != null) go.GetComponent<Renderer>().sharedMaterial = m;
        }
    }
}
