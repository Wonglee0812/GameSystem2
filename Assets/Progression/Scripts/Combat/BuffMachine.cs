using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PokerTD
{
    /// <summary>
    /// A buff machine stands on a tile. The ground around it is split into rings, one
    /// ring per tower the player may own (TowerLimit). A tower inside ring k gets this
    /// machine's buff as step k of its burst: ring 1 first, then ring 2, and so on.
    /// So the place of a tower decides the order of its buffs, and the order matters:
    /// +mult before xmult gives more damage than the other way round (as in Balatro).
    /// Two machines that put the same ring number on one tower: the machine built first goes first.
    /// Graybox: built from primitives.
    /// </summary>
    public class BuffMachine : MonoBehaviour
    {
        public static readonly List<BuffMachine> All = new List<BuffMachine>();
        static int built;

        public TDConfig.BuffType Type { get; private set; }
        public int BuildOrder { get; private set; }

        Material material;
        RingGuide guide;
        TextMeshPro label;
        static readonly Color RingFaint = new Color(1f, 1f, 1f, 0.35f);
        static readonly Color RingBright = new Color(1f, 0.9f, 0.4f, 0.95f);

        public static BuffMachine Build(TDConfig.BuffType type, Vector3 position, Material material)
        {
            var go = new GameObject("Buff_" + type.name);
            go.transform.position = position;
            var m = go.AddComponent<BuffMachine>();
            m.Type = type;
            m.material = material;
            m.BuildOrder = ++built;
            m.BuildBody();
            All.Add(m);
            return m;
        }

        /// <summary>Called when the scene restarts.</summary>
        public static void ResetAll() { All.Clear(); built = 0; }

        void BuildBody()
        {
            Color c = Type.color;
            Part(PrimitiveType.Cylinder, new Vector3(0f, 0.25f, 0f), new Vector3(1.2f, 0.25f, 1.2f), c * 0.6f);
            Part(PrimitiveType.Cube, new Vector3(0f, 0.85f, 0f), new Vector3(0.7f, 0.7f, 0.7f), c);
            Part(PrimitiveType.Sphere, new Vector3(0f, 1.45f, 0f), Vector3.one * 0.45f, Color.Lerp(c, Color.white, 0.5f));

            guide = RingGuide.Create(transform, RingFaint);
            guide.Show(false);

            var lab = new GameObject("BuffLabel");
            lab.transform.SetParent(transform, false);
            lab.transform.localPosition = Vector3.up * 2.2f;
            label = lab.AddComponent<TextMeshPro>();
            label.text = Type.Short;
            label.fontSize = 5f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.Lerp(c, Color.white, 0.4f);
            label.rectTransform.sizeDelta = new Vector2(3f, 1f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }

        void Part(PrimitiveType shape, Vector3 pos, Vector3 scale, Color color)
        {
            var p = GameObject.CreatePrimitive(shape);
            Destroy(p.GetComponent<Collider>());
            p.transform.SetParent(transform, false);
            p.transform.localPosition = pos;
            p.transform.localScale = scale;
            var r = p.GetComponent<Renderer>();
            r.sharedMaterial = material;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            r.SetPropertyBlock(block);
        }

        /// <summary>The ring (buff order) of a point around this machine, 0 when outside.</summary>
        public int RingOf(Vector3 point) => RingAt(transform.position, point);

        public static int RingAt(Vector3 centre, Vector3 point)
        {
            var game = TDGame.Instance;
            int n = game.TowerLimit;
            Vector3 d = point - centre; d.y = 0f;
            int k = Mathf.CeilToInt(d.magnitude / game.config.ringWidth);
            if (k < 1 || k > n) return 0;
            return game.config.ringOneIsInner ? k : n + 1 - k;
        }

        /// <summary>Show the rings (bright when this machine is the one under the pointer).</summary>
        public void ShowRings(bool show, bool bright)
        {
            guide.Show(show);
            if (!show) return;
            var game = TDGame.Instance;
            guide.Refresh(game.TowerLimit, game.config.ringWidth, game.config.ringOneIsInner);
            guide.SetColor(bright ? RingBright : RingFaint);
            guide.SetFillAlpha(bright ? 1f : 0.45f);
        }

        void Update()
        {
            if (Camera.main != null) label.transform.rotation = Camera.main.transform.rotation;
        }

        void OnDestroy() => All.Remove(this);
    }
}
