using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PokerTD.UI
{
    /// <summary>
    /// Fake 3D tilt for a flat UI image. The HUD canvas has no perspective, so a rotation
    /// around X or Y only makes the card thinner. This effect splits the image into a small
    /// grid, rotates every vertex by 'tilt' and divides it by its depth (a pinhole
    /// perspective projection), so the near edge grows and the far edge shrinks, like
    /// the cards in Balatro. A grid (not one quad) keeps the texture from bending along the diagonal.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public class CardPerspective : BaseMeshEffect
    {
        [Tooltip("Rotation in degrees: x = top/bottom, y = left/right.")]
        public Vector2 tilt;
        [Tooltip("Distance of the eye in UI pixels; smaller = stronger perspective.")]
        public float focal = 520f;
        public int columns = 4;
        public int rows = 6;

        static readonly List<UIVertex> quad = new List<UIVertex>();
        Vector2 lastTilt;

        /// <summary>Sets the tilt and rebuilds the mesh only when it really changed.</summary>
        public void SetTilt(Vector2 value)
        {
            if ((value - lastTilt).sqrMagnitude < 0.0004f) return;
            tilt = lastTilt = value;
            if (graphic != null) graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount < 4) return;

            quad.Clear();
            vh.GetUIVertexStream(quad);
            Vector2 pMin = new Vector2(float.MaxValue, float.MaxValue), pMax = -pMin;
            Vector2 uMin = pMin, uMax = pMax;
            foreach (var v in quad)
            {
                pMin = Vector2.Min(pMin, v.position); pMax = Vector2.Max(pMax, v.position);
                uMin = Vector2.Min(uMin, v.uv0); uMax = Vector2.Max(uMax, v.uv0);
            }
            UIVertex template = quad[0];
            Vector2 centre = (pMin + pMax) * 0.5f;
            Quaternion rot = Quaternion.Euler(tilt.x, tilt.y, 0f);

            vh.Clear();
            for (int r = 0; r <= rows; r++)
            {
                for (int c = 0; c <= columns; c++)
                {
                    var t = new Vector2(c / (float)columns, r / (float)rows);
                    Vector2 p = new Vector2(Mathf.Lerp(pMin.x, pMax.x, t.x), Mathf.Lerp(pMin.y, pMax.y, t.y)) - centre;
                    Vector3 q = rot * new Vector3(p.x, p.y, 0f);
                    float s = focal / Mathf.Max(1f, focal + q.z);
                    var v = template;
                    v.position = new Vector3(centre.x + q.x * s, centre.y + q.y * s, 0f);
                    v.uv0 = new Vector2(Mathf.Lerp(uMin.x, uMax.x, t.x), Mathf.Lerp(uMin.y, uMax.y, t.y));
                    vh.AddVert(v);
                }
            }
            int stride = columns + 1;
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < columns; c++)
                {
                    int i = r * stride + c;
                    vh.AddTriangle(i, i + stride, i + stride + 1);
                    vh.AddTriangle(i, i + stride + 1, i + 1);
                }
        }
    }
}
