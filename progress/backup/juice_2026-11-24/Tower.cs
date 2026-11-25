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
    /// Look: Kenney Tower Defense Kit (CC0) body + weapon; graybox primitives when the art asset is missing.
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
        WorldCard cardView;   // the tower's card as a sprite above it
        float cooldown;
        readonly Dictionary<Enemy, float> lastShot = new Dictionary<Enemy, float>();
        float stunnedUntil;
        Material material;
        RangeRing ring;
        Transform spikes;          // spike trap: spikes that pop up on each pulse
        float spikeTime = -10f;
        static Mesh spikeMesh;
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
            if (BoardTransition.Instance != null) go.transform.SetParent(BoardTransition.Instance.board, true);   // sinks with the board in the shop turn
            All.Add(t);
            return t;
        }

        void BuildBody()
        {
            Color c = Type.color;
            float cardY = BuildKenney();
            if (cardY < 0f)
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
            lab.transform.localPosition = Vector3.up * (cardY > 0f ? cardY : Type.kind == TDConfig.TowerKind.Trap ? 1.0f : 2.0f);
            label = lab.AddComponent<TextMeshPro>();
            label.fontSize = 6f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(3f, 1f);
            cardView = WorldCard.Create(transform, lab.transform.localPosition, 0.9f, 10);
            cardView.gameObject.SetActive(false);
            lab.transform.localPosition += Vector3.up * 0.85f;   // "zzz" and "?" above the card
            UpdateLabel();
        }

        /// <summary>Kenney look: a stacked tower body with the weapon on a turning turret.
        /// Cannon = round tower + cannon, Gatling = square tower + turret, Trap = low base + catapult on the path.
        /// Returns the height for the card above it, or -1 when the art asset is missing (graybox then).</summary>
        float BuildKenney()
        {
            var art = PTDArt.I;
            if (art == null) return -1f;
            const float S = 1.5f;
            float y;
            GameObject weapon;
            float ws = S;
            switch (Type.kind)
            {
                case TDConfig.TowerKind.Cannon:
                    y = PTDArt.Stack(transform, 0f, S, art.cannonBase);
                    weapon = art.cannonWeapon;
                    break;
                case TDConfig.TowerKind.Gatling:
                    y = PTDArt.Stack(transform, 0f, S, art.gatlingBase, art.gatlingTop);
                    weapon = art.gatlingWeapon;
                    break;
                default:   // spike trap: a metal plate on the path with spikes in the hole
                    y = PTDArt.Stack(transform, 0f, 1.8f, art.trapBase);
                    spikes = BuildSpikes(art.spikeMaterial, y);
                    return y + 0.6f + 0.55f;
            }
            turret = new GameObject("Turret").transform;
            turret.SetParent(transform, false);
            turret.localPosition = Vector3.up * y;
            float top = PTDArt.Stack(turret, 0f, ws, weapon);
            return y + top + 0.55f;
        }

        /// <summary>3 x 3 metal spikes (square pyramids, flat shaded). Mostly hidden in the plate; they pop up on a pulse.</summary>
        Transform BuildSpikes(Material mat, float y)
        {
            if (spikeMesh == null)
            {
                const float r = 0.09f, h = 0.6f;
                Vector3[] c = { new Vector3(-r, 0f, -r), new Vector3(r, 0f, -r), new Vector3(r, 0f, r), new Vector3(-r, 0f, r) };
                Vector3 top = new Vector3(0f, h, 0f);
                var v = new List<Vector3>();
                for (int i = 0; i < 4; i++) { v.Add(c[i]); v.Add(top); v.Add(c[(i + 1) % 4]); }
                var t = new int[v.Count];
                for (int i = 0; i < t.Length; i++) t[i] = i;
                spikeMesh = new Mesh { name = "Spike" };
                spikeMesh.SetVertices(v);
                spikeMesh.SetTriangles(t, 0);
                spikeMesh.RecalculateNormals();
                spikeMesh.RecalculateBounds();
            }
            var holder = new GameObject("Spikes").transform;
            holder.SetParent(transform, false);
            holder.localPosition = Vector3.up * (y - 0.12f);
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    var s = new GameObject("Spike");
                    s.transform.SetParent(holder, false);
                    s.transform.localPosition = new Vector3(i * 0.22f, 0f, j * 0.22f);
                    s.transform.localScale = Vector3.one * ((i + j) % 2 == 0 ? 1f : 0.8f);
                    s.AddComponent<MeshFilter>().sharedMesh = spikeMesh;
                    var mr = s.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = mat != null ? mat : material;
                }
            holder.localScale = new Vector3(1f, 0.45f, 1f);
            return holder;
        }

        /// <summary>Where shots start: the front of the weapon.</summary>
        public Vector3 Muzzle => turret != null ? turret.position + turret.forward * 0.45f + Vector3.up * 0.4f : transform.position + Vector3.up;

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
            cardView.gameObject.SetActive(true);
            cardView.SetCard(card);
            cardView.PopIn();
            UpdateLabel();
        }

        public void ClearCard()
        {
            HasCard = false;
            if (cardView != null && cardView.gameObject.activeSelf) cardView.DissolveOut(false);
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
            label.text = Stunned ? "<color=#FF8040>zzz</color>" : HasCard ? "" : "<color=#888888>?</color>";
            if (cardView != null) cardView.SetGrey(Stunned);
        }

        /// <summary>Brighter ring while the pointer is over this tower's tile.</summary>
        public void Highlight(bool on) => ring.SetColor(on ? RingHover : RingColor);

        void Update()
        {
            if (Camera.main != null) label.transform.rotation = Camera.main.transform.rotation;
            UpdateLabel();
            cooldown -= Time.deltaTime;
            if (spikes != null)   // up fast, hold, slowly back down
            {
                float st = Time.time - spikeTime;
                float k = st < 0.08f ? st / 0.08f : Mathf.Clamp01(1f - (st - 0.3f) / 0.35f);
                spikes.localScale = new Vector3(1f, Mathf.Lerp(0.45f, 1f, k), 1f);
            }
            if (!HasCard || Stunned || TDGame.Instance.Current != TDGame.Phase.Wave) return;

            if (Type.kind == TDConfig.TowerKind.Trap)
            {
                if (cooldown > 0f) return;
                cooldown = 1f / Type.fireRate;
                bool any = false;
                foreach (var e in Enemy.All.ToArray())
                    if (Flat(e.transform.position - transform.position) <= Type.areaRadius) { e.Marks.AddMark(Card, this); any = true; }
                if (any) { Pulse(); cardView.Punch(0.6f); }
                return;
            }

            var target = Target();
            if (target == null) return;
            Vector3 dir = target.transform.position - turret.position; dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f) turret.rotation = Quaternion.Slerp(turret.rotation, Quaternion.LookRotation(dir), 12f * Time.deltaTime);
            if (cooldown > 0f) return;
            cooldown = 1f / Type.fireRate;
            lastShot[target] = Time.time;
            Projectile.Fire(this, target, material);
            cardView.Punch(0.35f);
        }

        /// <summary>
        /// The enemy in range that does not carry this tower's card yet, furthest along the path.
        /// When every enemy in range already has it: round robin, the enemy this tower shot
        /// longest ago (so it goes back to the first one, then the next...).
        /// </summary>
        Enemy Target()
        {
            Enemy furthest = null, fresh = null, oldest = null;
            float oldestTime = float.MaxValue;
            bool spread = TDGame.Instance.config.aimAtUnmarked;
            foreach (var e in Enemy.All)
            {
                if (Flat(e.transform.position - transform.position) > Type.range) continue;
                if (furthest == null || e.Progress > furthest.Progress) furthest = e;
                if (!spread) continue;
                if (!Carries(e) && (fresh == null || e.Progress > fresh.Progress)) fresh = e;
                float t = lastShot.TryGetValue(e, out float s) ? s : -1f;
                if (t < oldestTime || (Mathf.Approximately(t, oldestTime) && e.Progress > oldest.Progress)) { oldestTime = t; oldest = e; }
            }
            if (!spread) return furthest;
            if (lastShot.Count > 64) lastShot.Clear();
            return fresh != null ? fresh : oldest;
        }

        bool Carries(Enemy e)
        {
            foreach (var c in e.Marks.Cards) if (c.Equals(Card)) return true;
            return false;
        }

        void Pulse()
        {
            spikeTime = Time.time;
            FloatText.Show(Card.ToString(), transform.position + Vector3.up * 0.6f, new Color(0.85f, 0.6f, 1f, 0.8f));
        }

        static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }

        void OnDestroy() => All.Remove(this);
    }
}
