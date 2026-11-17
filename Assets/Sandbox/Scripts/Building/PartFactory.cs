using System.Collections.Generic;
using UnityEngine;

namespace DamRush
{
    /// <summary>
    /// Modular building-block architecture: every part is put together from one MaterialDef
    /// and one ShapeDef. Mass, cost, look and special rules all come from the two assets,
    /// so a new material or shape is a new asset, not new code.
    /// </summary>
    public static class PartFactory
    {
        public static SandboxConfig Config;
        public static WaterGrid Water;
        public static Transform PartsRoot, DebrisRoot;

        static readonly Dictionary<MaterialDef, PhysicsMaterial> physicsMats = new Dictionary<MaterialDef, PhysicsMaterial>();
        static readonly Queue<GameObject> debris = new Queue<GameObject>();

        public static int Cost(ShapeDef shape, MaterialDef mat) =>
            Mathf.Max(1, Mathf.RoundToInt(shape.Volume * mat.costPerCubicMetre));

        static PhysicsMaterial PhysicsMat(MaterialDef mat)
        {
            if (!physicsMats.TryGetValue(mat, out var pm))
            {
                pm = new PhysicsMaterial(mat.displayName)
                {
                    dynamicFriction = mat.friction,
                    staticFriction = mat.friction * 1.1f,
                    bounciness = 0f,
                    frictionCombine = PhysicsMaterialCombine.Average,
                };
                physicsMats[mat] = pm;
            }
            return pm;
        }

        /// <summary>Makes the visual + collider only (used by the ghost preview too).</summary>
        public static GameObject CreateShell(ShapeDef shape, string name)
        {
            GameObject go;
            switch (shape.kind)
            {
                case ShapeKind.Log:
                {
                    go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
                    go.GetComponent<MeshFilter>().sharedMesh = ShapeMeshes.Log(shape.size);
                    var cap = go.AddComponent<CapsuleCollider>();
                    cap.direction = 0;
                    cap.radius = Mathf.Min(shape.size.y, shape.size.z) * 0.5f;
                    cap.height = shape.size.x;
                    break;
                }
                case ShapeKind.Ramp:
                {
                    go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
                    var mesh = ShapeMeshes.Ramp(shape.size);
                    go.GetComponent<MeshFilter>().sharedMesh = mesh;
                    var mc = go.AddComponent<MeshCollider>();
                    mc.sharedMesh = mesh;
                    mc.convex = true;
                    break;
                }
                default:
                {
                    go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = name;
                    go.transform.localScale = shape.size;
                    break;
                }
            }
            return go;
        }

        public static Part Create(ShapeDef shape, MaterialDef mat, Vector3 position, Quaternion rotation)
        {
            var go = CreateShell(shape, mat.displayName + " " + shape.displayName);
            go.transform.SetPositionAndRotation(position, rotation);
            if (PartsRoot != null) go.transform.SetParent(PartsRoot, true);
            go.GetComponent<Renderer>().sharedMaterial = mat.renderMaterial;
            go.GetComponent<Collider>().sharedMaterial = PhysicsMat(mat);

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = mat.density * shape.Volume;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.isKinematic = true;

            var wb = go.AddComponent<WaterBody>();
            wb.config = Config; wb.water = Water; wb.volume = shape.Volume; wb.sealedBottom = mat.gluesToGround;

            var part = go.AddComponent<Part>();
            part.material = mat; part.shape = shape; part.cost = Cost(shape, mat);
            go.AddComponent<Breakable>();
            if (mat.erodes) go.AddComponent<Erosion>();
            return part;
        }

        /// <summary>
        /// Breaks a part into 0.5 m cubes of the same material. The cubes keep the part's speed,
        /// so a broken dam sends a wave of debris downstream (chain reaction).
        /// </summary>
        public static void Shatter(Part part)
        {
            if (part == null || !part.gameObject.activeInHierarchy) return; // already broken this frame
            var shape = part.shape;
            var t = part.transform;
            // blocks are scaled cubes; logs and ramps have a real-size mesh (scale = erosion only)
            Vector3 size = shape.kind == ShapeKind.Block ? t.localScale : Vector3.Scale(shape.size, t.localScale);
            const float piece = 0.5f;
            int cx = Mathf.Max(1, Mathf.RoundToInt(size.x / piece));
            int cy = Mathf.Max(1, Mathf.RoundToInt(size.y / piece));
            int cz = Mathf.Max(1, Mathf.RoundToInt(size.z / piece));
            Vector3 step = new Vector3(size.x / cx, size.y / cy, size.z / cz);
            Vector3 v = part.Body.linearVelocity, w = part.Body.angularVelocity;

            for (int x = 0; x < cx; x++)
                for (int y = 0; y < cy; y++)
                    for (int z = 0; z < cz; z++)
                    {
                        var local = new Vector3((x + 0.5f) * step.x - size.x * 0.5f, (y + 0.5f) * step.y - size.y * 0.5f, (z + 0.5f) * step.z - size.z * 0.5f);
                        if (shape.kind == ShapeKind.Ramp)
                        {
                            float nx = (local.x + size.x * 0.5f) / size.x, ny = (local.y + size.y * 0.5f) / size.y;
                            if (ny > 1f - nx) continue; // above the slope
                        }
                        Vector3 world = t.position + t.rotation * local;
                        SpawnDebris(part.material, world, t.rotation, step * 0.95f, v + Random.insideUnitSphere, w);
                    }
            part.gameObject.SetActive(false);
            Object.Destroy(part.gameObject);
        }

        static void SpawnDebris(MaterialDef mat, Vector3 pos, Quaternion rot, Vector3 size, Vector3 velocity, Vector3 spin)
        {
            while (debris.Count >= Config.maxDebris)
            {
                var old = debris.Dequeue();
                if (old != null) Object.Destroy(old);
            }
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = mat.displayName + " debris";
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = size;
            if (DebrisRoot != null) go.transform.SetParent(DebrisRoot, true);
            go.GetComponent<Renderer>().sharedMaterial = mat.renderMaterial;
            go.GetComponent<Collider>().sharedMaterial = PhysicsMat(mat);
            var rb = go.AddComponent<Rigidbody>();
            float vol = size.x * size.y * size.z;
            rb.mass = mat.density * vol;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.linearVelocity = velocity;
            rb.angularVelocity = spin;
            var wb = go.AddComponent<WaterBody>();
            wb.config = Config; wb.water = Water; wb.volume = vol;
            Object.Destroy(go, Config.debrisLifetime);
            debris.Enqueue(go);
        }

        public static void ClearDebris()
        {
            while (debris.Count > 0) { var d = debris.Dequeue(); if (d != null) Object.Destroy(d); }
        }
    }
}
