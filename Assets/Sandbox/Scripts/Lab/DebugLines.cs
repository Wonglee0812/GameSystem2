using System.Collections.Generic;
using UnityEngine;

namespace DamRush
{
    /// <summary>
    /// Draws coloured lines in the Game view (Debug.DrawLine only shows in the Scene view).
    /// Call Line() any time during the frame; the lines are drawn once and then cleared.
    /// </summary>
    public class DebugLines : MonoBehaviour
    {
        static DebugLines instance;
        public Material material; // an unlit material that uses vertex colours

        readonly List<Vector3> points = new List<Vector3>();
        readonly List<Color> colors = new List<Color>();
        readonly List<int> indices = new List<int>();
        Mesh mesh;

        void Awake()
        {
            instance = this;
            mesh = new Mesh { name = "Debug lines" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.MarkDynamic();
        }

        public static void Line(Vector3 a, Vector3 b, Color c)
        {
            if (instance == null) return;
            int i = instance.points.Count;
            instance.points.Add(a); instance.points.Add(b);
            instance.colors.Add(c); instance.colors.Add(c);
            instance.indices.Add(i); instance.indices.Add(i + 1);
        }

        /// <summary>Line with a small arrow head at the end.</summary>
        public static void Arrow(Vector3 from, Vector3 to, Color c)
        {
            Line(from, to, c);
            Vector3 d = to - from;
            if (d.sqrMagnitude < 1e-6f) return;
            Vector3 side = Vector3.Cross(d, Mathf.Abs(d.normalized.y) > 0.9f ? Vector3.right : Vector3.up).normalized * d.magnitude * 0.12f;
            Line(to, to - d * 0.2f + side, c);
            Line(to, to - d * 0.2f - side, c);
        }

        void LateUpdate()
        {
            mesh.Clear();
            if (points.Count > 0 && material != null)
            {
                mesh.SetVertices(points);
                mesh.SetColors(colors);
                mesh.SetIndices(indices, MeshTopology.Lines, 0);
                mesh.RecalculateBounds();
                Graphics.RenderMesh(new RenderParams(material), mesh, 0, Matrix4x4.identity);
            }
            points.Clear(); colors.Clear(); indices.Clear();
        }
    }
}
