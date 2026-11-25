"""Poker TD: Kenney Tower Defense Kit models for towers, UFO enemies, buff machines and ammo
(graybox stays as fallback when Art/Resources/PTDArt.asset is missing).
Run from the project root. Safe to run twice."""
import sys

def patch(path, edits):
    s = open(path, encoding='utf-8').read()
    for old, new, marker in edits:
        if marker in s:
            continue
        if old not in s:
            sys.exit(f'NOT FOUND in {path}: {old[:70]!r}')
        s = s.replace(old, new, 1)
    open(path, 'w', encoding='utf-8').write(s)
    print('patched', path)

C = 'Assets/Progression/Scripts/Combat/'

# ---------- Tower ----------
patch(C + 'Tower.cs', [
    ("    /// Graybox: built from primitives.\n    /// </summary>\n    public class Tower",
     "    /// Look: Kenney Tower Defense Kit (CC0) body + weapon; graybox primitives when the art asset is missing.\n    /// </summary>\n    public class Tower",
     "Kenney Tower Defense Kit (CC0) body"),
    ("            Color c = Type.color;\n            switch (Type.kind)\n",
     "            Color c = Type.color;\n            float cardY = BuildKenney();\n            if (cardY < 0f)\n            switch (Type.kind)\n",
     "float cardY = BuildKenney();"),
    ("            lab.transform.localPosition = Vector3.up * (Type.kind == TDConfig.TowerKind.Trap ? 1.0f : 2.0f);\n",
     "            lab.transform.localPosition = Vector3.up * (cardY > 0f ? cardY : Type.kind == TDConfig.TowerKind.Trap ? 1.0f : 2.0f);\n",
     "cardY > 0f ? cardY"),
    ("        GameObject Part(PrimitiveType shape, Transform parent,",
     """        /// <summary>Kenney look: a stacked tower body with the weapon on a turning turret.
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
                default:
                    ws = 1.3f;
                    y = PTDArt.Stack(transform, 0f, ws, art.trapBase);
                    weapon = art.trapWeapon;
                    break;
            }
            turret = new GameObject("Turret").transform;
            turret.SetParent(transform, false);
            turret.localPosition = Vector3.up * y;
            float top = PTDArt.Stack(turret, 0f, ws, weapon);
            return y + top + 0.95f;   // card (half height 0.68) fully above the weapon
        }

        /// <summary>Where shots start: the front of the weapon.</summary>
        public Vector3 Muzzle => turret != null ? turret.position + turret.forward * 0.45f + Vector3.up * 0.4f : transform.position + Vector3.up;

        GameObject Part(PrimitiveType shape, Transform parent,""",
     "float BuildKenney()"),
])

# ---------- Enemy ----------
patch(C + 'Enemy.cs', [
    ("    /// Graybox: the body is a capsule primitive, coloured by type.\n",
     "    /// Look: a Kenney UFO (CC0) that bobs and spins; graybox capsule when the art asset is missing.\n",
     "Look: a Kenney UFO"),
    ("        Transform head;\n        Transform barFill;\n",
     "        Transform head;\n        Transform barFill;\n        Transform body;      // Kenney model (null in graybox)\n        float bobPhase;\n        /// <summary>Plain material for the health bar and mark timer quads.</summary>\n        public Material BarMaterial { get; private set; }\n",
     "Transform body;      // Kenney model"),
    ("""            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(go.GetComponent<Collider>());
            go.name = "Enemy_" + type.name;
            go.transform.position = route.points[0].position + Vector3.up * type.size;
            go.transform.localScale = Vector3.one * type.size;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = material;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", type.color);
            r.SetPropertyBlock(block);

            var e = go.AddComponent<Enemy>();
""",
     """            var model = PTDArt.I != null ? PTDArt.I.Ufo(type.name) : null;
            GameObject go;
            Transform body = null;
            if (model != null)
            {
                go = new GameObject("Enemy_" + type.name);
                go.transform.position = route.points[0].position + Vector3.up * type.size;
                body = PTDArt.Place(model, go.transform, Vector3.down * 0.5f * type.size, type.size * 1.6f);
                PTDArt.Tint(body, Color.Lerp(Color.white, type.color, 0.35f));
                foreach (var mr in body.GetComponentsInChildren<Renderer>()) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Destroy(go.GetComponent<Collider>());
                go.name = "Enemy_" + type.name;
                go.transform.position = route.points[0].position + Vector3.up * type.size;
                go.transform.localScale = Vector3.one * type.size;
                var r = go.GetComponent<Renderer>();
                r.sharedMaterial = material;
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", type.color);
                r.SetPropertyBlock(block);
            }

            var e = go.AddComponent<Enemy>();
            e.body = body;
            e.bobPhase = Random.value * 6.28f;
            e.BarMaterial = material;
""",
     "e.BarMaterial = material;"),
    ("""            head.localPosition = Vector3.up * 1.6f;
            head.localScale = Vector3.one / Type.size;   // same size for every enemy
""",
     """            float k = transform.localScale.x;   // graybox: scaled by size; Kenney: 1
            head.localPosition = Vector3.up * (body != null ? 1.15f : 1.6f) * Type.size / k;
            head.localScale = Vector3.one / k;   // same size for every enemy
""",
     "float k = transform.localScale.x;"),
    ("            r.sharedMaterial = GetComponent<Renderer>().sharedMaterial;\n",
     "            r.sharedMaterial = BarMaterial;\n",
     "r.sharedMaterial = BarMaterial;"),
    ("""            var route = PathRoute.Instance;
            if (next >= route.points.Length) { ReachBase(); return; }
""",
     """            if (body != null)   // UFO hover: bob up and down, spin (not while frozen)
            {
                body.localPosition = Vector3.up * (-0.5f * Type.size + Mathf.Sin(Time.time * 3f + bobPhase) * 0.08f);
                if (freezeTime <= 0f) body.Rotate(0f, 120f * Time.deltaTime, 0f, Space.Self);
            }
            var route = PathRoute.Instance;
            if (next >= route.points.Length) { ReachBase(); return; }
""",
     "UFO hover: bob up and down"),
])

# ---------- CardMarks ----------
patch(C + 'CardMarks.cs', [
    ("            r.sharedMaterial = GetComponent<Renderer>().sharedMaterial;\n",
     "            r.sharedMaterial = enemy.BarMaterial;\n",
     "r.sharedMaterial = enemy.BarMaterial;"),
])

# ---------- BuffMachine ----------
patch(C + 'BuffMachine.cs', [
    ("    /// Graybox: built from primitives.\n    /// </summary>\n    public class BuffMachine",
     "    /// Look: Kenney round tower base with crystals in the buff colour; graybox primitives when the art asset is missing.\n    /// </summary>\n    public class BuffMachine",
     "Kenney round tower base with crystals"),
    ("""            Part(PrimitiveType.Cylinder, new Vector3(0f, 0.25f, 0f), new Vector3(1.2f, 0.25f, 1.2f), c * 0.6f);
            Part(PrimitiveType.Cube, new Vector3(0f, 0.85f, 0f), new Vector3(0.7f, 0.7f, 0.7f), c);
            Part(PrimitiveType.Sphere, new Vector3(0f, 1.45f, 0f), Vector3.one * 0.45f, Color.Lerp(c, Color.white, 0.5f));
""",
     """            float labelY = 2.2f;
            var art = PTDArt.I;
            if (art != null && art.machineBase != null)
            {
                float y = PTDArt.Stack(transform, 0f, 1.5f, art.machineBase);
                y = PTDArt.Stack(transform, y, 1.3f, art.machineCrystals, out Transform crystals);
                PTDArt.Tint(crystals, Color.Lerp(c, Color.white, 0.25f));
                labelY = y + 0.45f;
            }
            else
            {
                Part(PrimitiveType.Cylinder, new Vector3(0f, 0.25f, 0f), new Vector3(1.2f, 0.25f, 1.2f), c * 0.6f);
                Part(PrimitiveType.Cube, new Vector3(0f, 0.85f, 0f), new Vector3(0.7f, 0.7f, 0.7f), c);
                Part(PrimitiveType.Sphere, new Vector3(0f, 1.45f, 0f), Vector3.one * 0.45f, Color.Lerp(c, Color.white, 0.5f));
            }
""",
     "float labelY = 2.2f;"),
    ("            lab.transform.localPosition = Vector3.up * 2.2f;\n",
     "            lab.transform.localPosition = Vector3.up * labelY;\n",
     "Vector3.up * labelY;"),
])

# ---------- Projectile ----------
patch(C + 'Projectile.cs', [
    ("        float speed;\n",
     "        float speed;\n        bool spin;   // cannonball and graybox card spin; bullets point at the target\n",
     "bool spin;"),
    ("""            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.name = "CardShot";
            go.transform.position = tower.transform.position + Vector3.up * 1f;
            go.transform.localScale = new Vector3(0.3f, 0.04f, 0.42f);   // a little playing card
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = material;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", new Color(0.98f, 0.95f, 0.88f));
            r.SetPropertyBlock(block);
            var p = go.AddComponent<Projectile>();
""",
     """            var art = PTDArt.I;
            bool cannon = tower.Type.kind == TDConfig.TowerKind.Cannon;
            var ammo = art == null ? null : cannon ? art.cannonball : art.bullet;
            GameObject go;
            if (ammo != null)   // Kenney ammo
            {
                go = new GameObject(cannon ? "Cannonball" : "Bullet");
                go.transform.position = tower.Muzzle;
                PTDArt.Place(ammo, go.transform, Vector3.zero, 1.6f);
            }
            else   // graybox: a little playing card
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(go.GetComponent<Collider>());
                go.name = "CardShot";
                go.transform.position = tower.transform.position + Vector3.up * 1f;
                go.transform.localScale = new Vector3(0.3f, 0.04f, 0.42f);
                var r = go.GetComponent<Renderer>();
                r.sharedMaterial = material;
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", new Color(0.98f, 0.95f, 0.88f));
                r.SetPropertyBlock(block);
            }
            var p = go.AddComponent<Projectile>();
            p.spin = ammo == null || cannon;
""",
     "p.spin = ammo == null || cannon;"),
    ("            transform.Rotate(0f, 720f * Time.deltaTime, 0f, Space.World);\n",
     "            if (spin) transform.Rotate(0f, 720f * Time.deltaTime, 0f, Space.World);\n            else if (to.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(to);\n",
     "if (spin) transform.Rotate"),
])
print('done')

# ---------- towers and machines ride with the board (they floated over the shop floor before) ----------
patch(C + 'Tower.cs', [
    ("            t.BuildBody();\n            All.Add(t);\n",
     "            t.BuildBody();\n            if (BoardTransition.Instance != null) go.transform.SetParent(BoardTransition.Instance.board, true);   // sinks with the board in the shop turn\n            All.Add(t);\n",
     "sinks with the board in the shop turn"),
])
patch(C + 'BuffMachine.cs', [
    ("            m.BuildBody();\n            All.Add(m);\n",
     "            m.BuildBody();\n            if (BoardTransition.Instance != null) go.transform.SetParent(BoardTransition.Instance.board, true);   // sinks with the board in the shop turn\n            All.Add(m);\n",
     "sinks with the board in the shop turn"),
])
print('done 2')

# ---------- spike trap (Wong, 2026-11-24: the catapult looked like a cannon) ----------
patch('Assets/Progression/Scripts/Game/PTDArt.cs', [
    ("        public GameObject trapWeapon;\n",
     "        public GameObject trapWeapon;\n        [Tooltip(\"Material for the spikes of the trap (made in code).\")]\n        public Material spikeMaterial;\n",
     "public Material spikeMaterial;"),
])
patch(C + 'Tower.cs', [
    ("        RangeRing ring;\n",
     "        RangeRing ring;\n        Transform spikes;          // spike trap: spikes that pop up on each pulse\n        float spikeTime = -10f;\n        static Mesh spikeMesh;\n",
     "Transform spikes;          // spike trap"),
    ("""                default:
                    ws = 1.3f;
                    y = PTDArt.Stack(transform, 0f, ws, art.trapBase);
                    weapon = art.trapWeapon;
                    break;
""",
     """                default:   // spike trap: a metal plate on the path with spikes in the hole
                    y = PTDArt.Stack(transform, 0f, 1.8f, art.trapBase);
                    spikes = BuildSpikes(art.spikeMaterial, y);
                    return y + 0.6f + 0.95f;   // card fully above the spikes
""",
     "spikes = BuildSpikes(art.spikeMaterial, y);"),
    ("        /// <summary>Where shots start: the front of the weapon.</summary>",
     """        /// <summary>3 x 3 metal spikes (square pyramids, flat shaded). Mostly hidden in the plate; they pop up on a pulse.</summary>
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

        /// <summary>Where shots start: the front of the weapon.</summary>""",
     "Transform BuildSpikes(Material mat, float y)"),
    ("""            cooldown -= Time.deltaTime;
            if (!HasCard""",
     """            cooldown -= Time.deltaTime;
            if (spikes != null)   // up fast, hold, slowly back down
            {
                float st = Time.time - spikeTime;
                float k = st < 0.08f ? st / 0.08f : Mathf.Clamp01(1f - (st - 0.3f) / 0.35f);
                spikes.localScale = new Vector3(1f, Mathf.Lerp(0.45f, 1f, k), 1f);
            }
            if (!HasCard""",
     "up fast, hold, slowly back down"),
    ("        void Pulse() => FloatText.Show(",
     "        void Pulse()\n        {\n            spikeTime = Time.time;\n            FloatText.Show(",
     "spikeTime = Time.time;\n            FloatText"),
])
# close the Pulse block
s = open(C + 'Tower.cs', encoding='utf-8').read()
old = "new Color(0.85f, 0.6f, 1f, 0.8f));\n\n        static float Flat"
if old in s:
    s = s.replace(old, "new Color(0.85f, 0.6f, 1f, 0.8f));\n        }\n\n        static float Flat", 1)
    open(C + 'Tower.cs', 'w', encoding='utf-8').write(s)
print('done 3')
