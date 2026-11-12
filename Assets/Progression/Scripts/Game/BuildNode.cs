using UnityEngine;

namespace PokerTD
{
    /// <summary>One tile of the map where a tower or a buff machine can stand. Path tiles only take traps.</summary>
    public class BuildNode : MonoBehaviour
    {
        public bool isPath;
        public Tower tower;
        public BuffMachine machine;
        public bool IsFree => tower == null && machine == null;

        Renderer rend;
        MaterialPropertyBlock block;
        Color baseColor;

        void Awake()
        {
            rend = GetComponent<Renderer>();
            block = new MaterialPropertyBlock();
            baseColor = rend.sharedMaterial.HasProperty("_BaseColor") ? rend.sharedMaterial.GetColor("_BaseColor") : Color.white;
            rend.GetPropertyBlock(block);
            if (block.isEmpty) block.SetColor("_BaseColor", baseColor);
            baseColor = block.GetColor("_BaseColor");
        }

        public void Highlight(bool on, bool ok)
        {
            block.SetColor("_BaseColor", !on ? baseColor : ok ? Color.Lerp(baseColor, new Color(0.5f, 1f, 0.5f), 0.5f) : Color.Lerp(baseColor, new Color(1f, 0.4f, 0.4f), 0.5f));
            rend.SetPropertyBlock(block);
        }

        public Vector3 Top => transform.position + Vector3.up * (transform.localScale.y / 2f);
    }
}
