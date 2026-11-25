using UnityEngine;

namespace PokerTD
{
    /// <summary>One tile of the map where a tower or a buff machine can stand. Path tiles only take traps.
    /// The cube keeps the collider for clicks; the Kenney tile model (art) is what the player sees.
    /// A small decoration on the tile (deco) hides while something is built on it.</summary>
    public class BuildNode : MonoBehaviour
    {
        public bool isPath;
        public Tower tower;
        public BuffMachine machine;
        [Tooltip("Visible tile model (Kenney). Empty = use this object's own renderer (graybox).")]
        public Renderer[] art;
        [Tooltip("Optional decoration on the tile, hidden while a tower or machine stands here.")]
        public GameObject deco;
        public bool IsFree => tower == null && machine == null;

        Renderer[] rends;
        MaterialPropertyBlock block;
        Color[] baseColors;

        void Awake()
        {
            rends = art != null && art.Length > 0 ? art : new[] { GetComponent<Renderer>() };
            block = new MaterialPropertyBlock();
            baseColors = new Color[rends.Length];
            for (int i = 0; i < rends.Length; i++)
            {
                var r = rends[i];
                Color c = r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor") ? r.sharedMaterial.GetColor("_BaseColor") : Color.white;
                r.GetPropertyBlock(block);
                if (!block.isEmpty && block.HasColor("_BaseColor")) c = block.GetColor("_BaseColor");
                baseColors[i] = c;
            }
        }

        public void Highlight(bool on, bool ok)
        {
            for (int i = 0; i < rends.Length; i++)
            {
                Color b = baseColors[i];
                rends[i].GetPropertyBlock(block);
                block.SetColor("_BaseColor", !on ? b : ok ? Color.Lerp(b, new Color(0.5f, 1f, 0.5f), 0.5f) : Color.Lerp(b, new Color(1f, 0.4f, 0.4f), 0.5f));
                rends[i].SetPropertyBlock(block);
            }
        }

        void LateUpdate()
        {
            if (deco != null && deco.activeSelf != IsFree) deco.SetActive(IsFree);
        }

        public Vector3 Top => transform.position + Vector3.up * (transform.localScale.y / 2f);
    }
}
