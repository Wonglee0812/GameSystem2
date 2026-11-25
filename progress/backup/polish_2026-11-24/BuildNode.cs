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

        bool hiOn, hiOk;
        Color ring = Color.clear;     // buff machine ring colour on this tile (alpha = strength)
        TMPro.TextMeshPro ringLabel;
        Renderer ringFill;
        MaterialPropertyBlock fillBlock;
        static Material fillMat;

        /// <summary>Colours this tile for a buff machine ring (number 0 = no ring). The tile gets the ring colour
        /// and a small ring number, so the player sees which tiles give which buff order (Wong, 2026-11-24).</summary>
        public void SetRing(int number, Color color)
        {
            Color c = number > 0 ? color : Color.clear;
            if (c == ring && (ringLabel == null || ringLabel.gameObject.activeSelf == number > 0)) return;
            ring = c;
            if (number > 0)
            {
                if (ringLabel == null)
                {
                    ringLabel = new GameObject("RingNumber").AddComponent<TMPro.TextMeshPro>();
                    ringLabel.transform.SetParent(transform.parent, false);
                    ringLabel.transform.position = Top + Vector3.up * 0.04f;
                    ringLabel.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    UI.UiKit.SetFont(ringLabel, UI.UiKit.Number, 0.2f);
                    ringLabel.fontSize = 5f;
                    ringLabel.alignment = TMPro.TextAlignmentOptions.Center;
                    ringLabel.rectTransform.sizeDelta = new Vector2(1.6f, 1.2f);
                    ringLabel.sortingOrder = 1;
                }
                if (ringFill == null)
                {
                    // a see-through coloured square on the tile (a tint on the green tile turned every colour muddy)
                    var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    Destroy(q.GetComponent<Collider>());
                    q.name = "RingFill";
                    q.transform.SetParent(transform.parent, false);
                    q.transform.position = Top + Vector3.up * 0.025f;
                    q.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    q.transform.localScale = new Vector3(transform.localScale.x * 0.94f, transform.localScale.z * 0.94f, 1f);
                    if (fillMat == null) fillMat = new Material(Shader.Find("Sprites/Default"));
                    ringFill = q.GetComponent<Renderer>();
                    ringFill.sharedMaterial = fillMat;
                    ringFill.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    ringFill.sortingOrder = 0;
                    fillBlock = new MaterialPropertyBlock();
                }
                fillBlock.SetColor("_Color", color);
                ringFill.SetPropertyBlock(fillBlock);
                ringFill.gameObject.SetActive(true);
                ringLabel.text = number.ToString();
                ringLabel.color = new Color(1f, 1f, 1f, 0.85f);
                ringLabel.gameObject.SetActive(true);
            }
            else
            {
                if (ringLabel != null) ringLabel.gameObject.SetActive(false);
                if (ringFill != null) ringFill.gameObject.SetActive(false);
            }
            Apply();
        }

        public void Highlight(bool on, bool ok)
        {
            hiOn = on; hiOk = ok;
            Apply();
        }

        void Apply()
        {
            bool on = hiOn, ok = hiOk;
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
