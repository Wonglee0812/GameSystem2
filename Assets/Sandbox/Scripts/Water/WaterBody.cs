using System.Collections.Generic;
using UnityEngine;

namespace DamRush
{
    /// <summary>
    /// Shared rule for every part and every piece of debris: the water pushes it.
    ///
    /// The surface of the part is cut into small square patches (a voxel skin). Every physics
    /// step, each patch under the water gets the water pressure p = rho * g * depth, pushing
    /// into the patch. This follows the idea of Kerner (2015): add up the pressure on the
    /// submerged surface of the hull instead of using one buoyancy force.
    /// From this one rule we get:
    /// - buoyancy (bottom patches are deeper than top patches, so the sum points up = rho*g*V),
    /// - the side push on a dam (deep water on one side, shallow or none on the other),
    /// - a correct turning force, because each patch pushes at its own position.
    /// On top of that, each patch facing into the current gets pressure drag (0.5*rho*Cd*A*v^2).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class WaterBody : MonoBehaviour
    {
        public static readonly List<WaterBody> All = new List<WaterBody>();

        public SandboxConfig config;
        public WaterGrid water;
        [Tooltip("Real volume in m3 (for info and erosion; buoyancy comes from the patches).")]
        public float volume = 1f;
        [Tooltip("Counted as extra ground by the water grid when it stands on the ground.")]
        public bool blocksWater = true;
        [Tooltip("Water cannot get under the part when it stands on the ground (mud seals the bottom).")]
        public bool sealedBottom;

        public Collider Col { get; private set; }
        public Rigidbody Body { get; private set; }

        // ---- debug read-outs (last physics step)
        /// <summary>Share of the surface patches under water (0-1).</summary>
        public float Submerged { get; private set; }
        /// <summary>Speed of the water around the part (m/s).</summary>
        public float FlowSpeed { get; private set; }
        public Vector3 PressureForce { get; private set; }   // total pressure force (vertical part = buoyancy)
        public Vector3 DragForce { get; private set; }
        public float WaterLevelAround { get; private set; }
        public int PatchCount => patches?.Count ?? 0;

        struct Patch { public Vector3 localPos, localNormal; public float area; }
        List<Patch> patches;
        float totalArea;
        Vector3 builtScale;
        float baseDamping, baseAngularDamping;

        void Awake()
        {
            Col = GetComponent<Collider>();
            Body = GetComponent<Rigidbody>();
            baseDamping = Body.linearDamping;
            baseAngularDamping = Body.angularDamping;
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        /// <summary>
        /// Voxelises the collider in its own axes and keeps the outer faces as patches.
        /// Runs once (and again if erosion changes the size).
        /// </summary>
        void BuildPatches()
        {
            patches = new List<Patch>();
            totalArea = 0f;
            builtScale = transform.lossyScale;
            var mf = GetComponent<MeshFilter>();
            Bounds lb = mf != null && mf.sharedMesh != null ? mf.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one);
            Vector3 scale = transform.lossyScale;
            Vector3 worldSize = Vector3.Scale(lb.size, scale);
            // at least 4 voxels across the thinnest side, so round logs are not counted as square
            float step = Mathf.Clamp(Mathf.Min(worldSize.x, Mathf.Min(worldSize.y, worldSize.z)) * 0.25f, 0.05f, config.probeStep);
            int cx = Mathf.Max(1, Mathf.RoundToInt(worldSize.x / step));
            int cy = Mathf.Max(1, Mathf.RoundToInt(worldSize.y / step));
            int cz = Mathf.Max(1, Mathf.RoundToInt(worldSize.z / step));
            Vector3 ls = new Vector3(lb.size.x / cx, lb.size.y / cy, lb.size.z / cz); // local voxel size
            Vector3 ws = Vector3.Scale(ls, scale);                                       // world voxel size

            var inside = new bool[cx, cy, cz];
            Physics.SyncTransforms();
            for (int x = 0; x < cx; x++)
                for (int y = 0; y < cy; y++)
                    for (int z = 0; z < cz; z++)
                    {
                        Vector3 lp = lb.min + new Vector3((x + 0.5f) * ls.x, (y + 0.5f) * ls.y, (z + 0.5f) * ls.z);
                        Vector3 wp = transform.TransformPoint(lp);
                        inside[x, y, z] = (Col.ClosestPoint(wp) - wp).sqrMagnitude < 1e-6f;
                    }

            var dirs = new[] { Vector3Int.left, Vector3Int.right, Vector3Int.down, Vector3Int.up, new Vector3Int(0, 0, -1), new Vector3Int(0, 0, 1) };
            for (int x = 0; x < cx; x++)
                for (int y = 0; y < cy; y++)
                    for (int z = 0; z < cz; z++)
                    {
                        if (!inside[x, y, z]) continue;
                        Vector3 centre = lb.min + new Vector3((x + 0.5f) * ls.x, (y + 0.5f) * ls.y, (z + 0.5f) * ls.z);
                        foreach (var d in dirs)
                        {
                            int nx = x + d.x, ny = y + d.y, nz = z + d.z;
                            bool open = nx < 0 || ny < 0 || nz < 0 || nx >= cx || ny >= cy || nz >= cz || !inside[nx, ny, nz];
                            if (!open) continue;
                            float a = d.x != 0 ? ws.y * ws.z : d.y != 0 ? ws.x * ws.z : ws.x * ws.y;
                            patches.Add(new Patch
                            {
                                localPos = centre + Vector3.Scale((Vector3)d, ls) * 0.5f,
                                localNormal = d,
                                area = a,
                            });
                            totalArea += a;
                        }
                    }
        }

        /// <summary>Mean water surface on a ring just outside the part (used under parts that sit on the ground).</summary>
        float RingSurface(Bounds b, out bool any)
        {
            float r = Mathf.Max(b.extents.x, b.extents.z) + config.cellSize * 0.75f;
            float sum = 0f; int n = 0;
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI * 0.25f;
                float s = water.SurfaceAt(b.center.x + Mathf.Cos(a) * r, b.center.z + Mathf.Sin(a) * r, out bool wet);
                if (wet) { sum += s; n++; }
            }
            any = n > 0;
            return n > 0 ? sum / n : float.NegativeInfinity;
        }

        void FixedUpdate()
        {
            if (water == null || Col == null || config == null) return;
            if (patches == null || (transform.lossyScale - builtScale).sqrMagnitude > 0.0025f) BuildPatches();

            float rho = config.waterDensity, g = config.gravity;
            Bounds b = Col.bounds;
            float ring = RingSurface(b, out bool ringWet);
            WaterLevelAround = ringWet ? ring : float.NaN;
            float offset = config.cellSize * 0.6f;
            Vector3 com = Body.worldCenterOfMass;
            Vector3 pressureSum = Vector3.zero, dragSum = Vector3.zero, torque = Vector3.zero;
            float wetArea = 0f, flowSum = 0f; int flowCount = 0;

            foreach (var p in patches)
            {
                Vector3 pos = transform.TransformPoint(p.localPos);
                Vector3 n = transform.TransformDirection(p.localNormal);
                // read the water just outside the patch (sideways only; bottom and top read straight below/above)
                Vector3 side = new Vector3(n.x, 0f, n.z);
                Vector3 q = pos + side * offset;
                float s = water.SurfaceAt(q.x, q.z, out bool wet);
                if (!wet && n.y < -0.5f && !sealedBottom && ringWet)
                {
                    // part sits on the ground: water seeps under it from the sides (uplift)
                    s = ring; wet = true;
                }
                if (!wet || s <= pos.y) continue;

                float pressure = rho * g * (s - pos.y) * config.pressureScale;
                Vector3 f = -n * pressure * p.area;
                pressureSum += f;
                wetArea += p.area;

                // pressure drag on patches facing into the current
                Vector2 v2 = water.VelocityAt(q.x, q.z);
                Vector3 rel = new Vector3(v2.x, 0f, v2.y) - Body.GetPointVelocity(pos);
                rel.y = 0f;
                float into = Vector3.Dot(rel, -n);
                if (into > 0f)
                {
                    Vector3 fd = -n * (0.5f * rho * config.dragCoefficient * p.area * into * into);
                    dragSum += fd;
                    f += fd;
                }
                flowSum += v2.magnitude; flowCount++;
                torque += Vector3.Cross(pos - com, f);
            }

            Submerged = totalArea > 0f ? wetArea / totalArea : 0f;
            FlowSpeed = flowCount > 0 ? flowSum / flowCount : 0f;
            PressureForce = pressureSum;
            DragForce = dragSum;

            if (Body.isKinematic) return; // walls still report their forces for the lab
            Vector3 total = pressureSum + dragSum;
            float limit = Body.mass * 60f;
            if (total.sqrMagnitude > limit * limit)
            {
                float k = limit / total.magnitude;
                total *= k; torque *= k;
            }
            Body.AddForce(total);
            Body.AddTorque(torque);
            Body.linearDamping = baseDamping + config.waterLinearDamping * Submerged;
            Body.angularDamping = baseAngularDamping + config.waterAngularDamping * Submerged;
        }
    }
}
