using System.Collections.Generic;
using UnityEngine;

namespace DamRush
{
    /// <summary>Simple meshes for the shapes Unity has no primitive for (log along x, ramp).</summary>
    public static class ShapeMeshes
    {
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

        /// <summary>Cylinder along the local x axis.</summary>
        public static Mesh Log(Vector3 size, int segments = 16)
        {
            string key = "log" + size;
            if (cache.TryGetValue(key, out var cached)) return cached;
            float hx = size.x * 0.5f, ry = size.y * 0.5f, rz = size.z * 0.5f;
            var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
            // side
            for (int s = 0; s <= segments; s++)
            {
                float a = s * Mathf.PI * 2f / segments;
                var dir = new Vector3(0f, Mathf.Cos(a), Mathf.Sin(a));
                var p = new Vector3(0f, dir.y * ry, dir.z * rz);
                v.Add(p + Vector3.left * hx); n.Add(dir);
                v.Add(p + Vector3.right * hx); n.Add(dir);
            }
            for (int s = 0; s < segments; s++)
            {
                int a = s * 2;
                t.Add(a); t.Add(a + 2); t.Add(a + 1);
                t.Add(a + 1); t.Add(a + 2); t.Add(a + 3);
            }
            // caps
            for (int side = -1; side <= 1; side += 2)
            {
                int centre = v.Count;
                v.Add(new Vector3(side * hx, 0f, 0f)); n.Add(new Vector3(side, 0f, 0f));
                for (int s = 0; s <= segments; s++)
                {
                    float a = s * Mathf.PI * 2f / segments;
                    v.Add(new Vector3(side * hx, Mathf.Cos(a) * ry, Mathf.Sin(a) * rz)); n.Add(new Vector3(side, 0f, 0f));
                }
                for (int s = 0; s < segments; s++)
                {
                    if (side > 0) { t.Add(centre); t.Add(centre + 1 + s); t.Add(centre + 2 + s); }
                    else { t.Add(centre); t.Add(centre + 2 + s); t.Add(centre + 1 + s); }
                }
            }
            var mesh = new Mesh { name = "Log" };
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
            cache[key] = mesh;
            return mesh;
        }

        /// <summary>Wedge: full height at -x, zero height at +x, flat bottom.</summary>
        public static Mesh Ramp(Vector3 size)
        {
            string key = "ramp" + size;
            if (cache.TryGetValue(key, out var cached)) return cached;
            float hx = size.x * 0.5f, hy = size.y * 0.5f, hz = size.z * 0.5f;
            var p = new[]
            {
                new Vector3(-hx, -hy, -hz), new Vector3(hx, -hy, -hz), new Vector3(hx, -hy, hz), new Vector3(-hx, -hy, hz), // bottom 0-3
                new Vector3(-hx, hy, -hz), new Vector3(-hx, hy, hz), // top edge 4-5
            };
            var v = new List<Vector3>(); var t = new List<int>();
            void Quad(int a, int b, int c, int d) { int s = v.Count; v.Add(p[a]); v.Add(p[b]); v.Add(p[c]); v.Add(p[d]); t.Add(s); t.Add(s + 1); t.Add(s + 2); t.Add(s); t.Add(s + 2); t.Add(s + 3); }
            void Tri(int a, int b, int c) { int s = v.Count; v.Add(p[a]); v.Add(p[b]); v.Add(p[c]); t.Add(s); t.Add(s + 1); t.Add(s + 2); }
            Quad(0, 1, 2, 3);       // bottom
            Quad(0, 3, 5, 4);       // back (tall side)
            Quad(4, 5, 2, 1);       // slope
            Tri(0, 4, 1);           // side -z
            Tri(3, 2, 5);           // side +z
            var mesh = new Mesh { name = "Ramp" };
            mesh.SetVertices(v); mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            // flip if the winding came out inside-out
            if (Vector3.Dot(mesh.normals[0], Vector3.down) < 0f)
            {
                for (int i = 0; i < t.Count; i += 3) { int tmp = t[i + 1]; t[i + 1] = t[i + 2]; t[i + 2] = tmp; }
                mesh.SetTriangles(t, 0);
                mesh.RecalculateNormals();
            }
            cache[key] = mesh;
            return mesh;
        }
    }
}
