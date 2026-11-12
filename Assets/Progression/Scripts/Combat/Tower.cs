using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PokerTD
{
    /// <summary>
    /// A tower holds one card for the whole wave and marks enemies with it.
    /// - Cannon: a slow shell that marks every enemy in a blast circle (AOE).
    /// - Gatling: fast shots, one enemy each.
    /// - Trap: lies on the path and marks every enemy inside its circle each pulse.
    /// Towers target the enemy closest to the base. A stunned tower does nothing.
    /// Graybox: built from primitives.
    /// </summary>
    public class Tower : MonoBehaviour
    {
        public static readonly List<Tower> All = new List<Tower>();

        public TDConfig.TowerType Type { get; private set; }
        public bool HasCard { get; private set; }
        public Card Card { get; private set; }
        public bool Stunned => Time.time < stunnedUntil;

        Transform turret;
        TextMeshPro label;
        float cooldown;
        float stunnedUntil;
        Material material;
        RangeRing ring;
        static readonly Color RingColor = new Color(1f, 1f, 1f, 0.45f);
        static readonly Color RingHover = new Color(1f, 0.9f, 0.4f, 0.9f);

        public static Tower Build(TDConfig.TowerType type, Vector3 position, Material material)
        {
            var go = new GameObject("Tower_" + type.name);
            go.transform.position = position;
            var t = go.AddComponent<Tower>();
            t.Type = type;
            t.material = material;
            t.BuildBody();
            All.Add(t);
            return t;
        }

        void BuildBody()
        {
            Color c = Type.color;
            switch (Type.kind)
            {
                case TDConfig.TowerKind.Cannon:
                    Part(PrimitiveType.Cylinder, transform, new Vector3(0f, 0.35f, 0f), new Vector3(1.3f, 0.35f, 1.3f), c * 0.8f);
                    turret = new GameObject("Turret").transform; turret.SetParent(transform, false); turret.localPosition = Vector3.up * 0.9f;
                    Part(PrimitiveType.Sphere, turret, Vector3.zero, Vector3.one * 0.9f, c);
                    Part(PrimitiveType.Cylinder, turret, new Vector3(0f, 0f, 0.55f), new Vector3(0.35f, 0.4f, 0.35f), c * 0.6f, Quaternion.Euler(90f, 0f, 0f));
                    break;
                case TDConfig.TowerKind.Gatling:
                    Part(PrimitiveType.Cube, transform, new Vector3(0f, 0.4f, 0f), new Vector3(1.1f, 0.8f, 1.1f), c * 0.8f);
                    turret = new GameObject("Turret").transform; turret.SetParent(transform, false); turret.localPosition = Vector3.up * 1.0f;
                    Part(PrimitiveType.Cube, turret, Vector3.zero, new Vector3(0.6f, 0.4f, 0.6f), c);
                    for (int i = -1; i <= 1; i += 2)
                        Part(PrimitiveType.Cylinder, turret, new Vector3(i * 0.13f, 0f, 0.5f), new Vector3(0.1f, 0.35f, 0.1f), c * 0.5f, Quaternion.Euler(90f, 0f, 0f));
                    break;
                default:   // trap: a flat disc on the path, as wide as its zone
                    var disc = Part(PrimitiveType.Cylinder, transform, new Vector3(0f, 0.03f, 0f), new Vector3(Type.areaRadius * 2f, 0.03f, Type.areaRadius * 2f), new Color(c.r, c.g, c.b, 1f));
                    Part(PrimitiveType.Cube, transform, new Vector3(0f, 0.15f, 0f), new Vector3(0.5f, 0.25f, 0.5f), c * 0.7f);
                    break;
            }
            // dashed circle on the ground: where this tower can shoot (trap: its zone)
            ring = RangeRing.Create(transform, Type.kind == TDConfig.TowerKind.Trap ? Type.areaRadius : Type.range, RingColor);

            var lab = new GameObject("CardLabel");
            lab.transform.SetParent(transform, false);
            lab.transform.localPosition = Vector3.up * (Type.kind == TDConfig.TowerKind.Trap ? 1.0f : 2.0f);
            label = lab.AddComponent<TextMeshPro>();
            label.fontSize = 6f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(3f, 1f);
            UpdateLabel();
        }

        GameObject Part(PrimitiveType shape, Transform parent, Vector3 pos, Vector3 scale, Color color, Quaternion? rot = null)
        {
            var p = GameObject.CreatePrimitive(shape);
            Destroy(p.GetComponent<Collider>());
            p.transform.SetParent(parent, false);
            p.transform.localPosition = pos;
            p.transform.localRotation = rot ?? Quaternion.identity;
            p.transform.localScale = scale;
            var r = p.GetComponent<Renderer>();
            r.sharedMaterial = material;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            r.SetPropertyBlock(block);
            return p;
        }

        public void SetCard(Card card)
        {
            Card = card;
            HasCard = true;
            UpdateLabel();
        }

        public void ClearCard()
        {
            HasCard = false;
            UpdateLabel();
        }

        public void Stun(float seconds)
        {
            stunnedUntil = Mathf.Max(stunnedUntil, Time.time + seconds);
            UpdateLabel();
        }

        void UpdateLabel()
        {
            if (label == null) return;
            label.text = Stunned ? "<color=#FF8040>zzz</color>" : HasCard ? Card.Rich : "<color=#888888>?</color>";
        }

        /// <summary>Brighter ring while the pointer is over this tower's tile.</summary>
        public void Highlight(bool on) => ring.SetColor(on ? RingHover : RingColor);

        void Update()
        {
            if (Camera.main != null) label.transform.rotation = Camera.main.transform.rotation;
            UpdateLabel();
            cooldown -= Time.deltaTime;
            if (!HasCard || Stunned || TDGame.Instance.Current != TDGame.Phase.Wave) return;

            if (Type.kind == TDConfig.TowerKind.Trap)
            {
                if (cooldown > 0f) return;
                cooldown = 1f / Type.fireRate;
                bool any = false;
                foreach (var e in Enemy.All.ToArray())
                    if (Flat(e.transform.position - transform.position) <= Type.areaRadius) { e.Marks.AddMark(Card, this); any = true; }
                if (any) Pulse();
                return;
            }

            var target = Target();
            if (target == null) return;
            Vector3 dir = target.transform.position - turret.position; dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f) turret.rotation = Quaternion.Slerp(turret.rotation, Quaternion.LookRotation(dir), 12f * Time.deltaTime);
            if (cooldown > 0f) return;
            cooldown = 1f / Type.fireRate;
            Projectile.Fire(this, target, material);
        }

        /// <summary>The enemy in range that is furthest along the path.</summary>
        Enemy Target()
        {
            Enemy best = null;
            foreach (var e in Enemy.All)
                if (Flat(e.transform.position - transform.position) <= Type.range && (best == null || e.Progress > best.Progress)) best = e;
            return best;
        }

        void Pulse() => FloatText.Show(Card.ToString(), transform.position + Vector3.up * 0.6f, new Color(0.85f, 0.6f, 1f, 0.8f));

        static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }

        void OnDestroy() => All.Remove(this);
    }
}
