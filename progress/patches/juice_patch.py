"""Poker TD juice hooks (2026-11-24): game scripts call the static Juice methods
(Scripts/Juice/Juice.cs). Run from the project root. Safe to run twice."""
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

patch(C + 'Tower.cs', [
    ("        /// <summary>Where shots start: the front of the weapon.</summary>",
     "        /// <summary>The weapon model on the turret (null for the trap and the graybox).</summary>\n        public Transform Weapon => turret != null && turret.childCount > 0 ? turret.GetChild(0) : null;\n\n        /// <summary>Where shots start: the front of the weapon.</summary>",
     "public Transform Weapon =>"),
    ("            Projectile.Fire(this, target, material);\n",
     "            Projectile.Fire(this, target, material);\n            Juice.TowerFired(this);\n",
     "Juice.TowerFired(this);"),
    ("            spikeTime = Time.time;\n",
     "            spikeTime = Time.time;\n            Juice.TrapPulse(this);\n",
     "Juice.TrapPulse(this);"),
    ("            All.Add(t);\n",
     "            All.Add(t);\n            Juice.Built(go.transform);\n",
     "Juice.Built(go.transform);"),
])
patch(C + 'BuffMachine.cs', [
    ("            All.Add(m);\n",
     "            All.Add(m);\n            Juice.Built(go.transform);\n",
     "Juice.Built(go.transform);"),
])
patch(C + 'Enemy.cs', [
    ("        public Material BarMaterial { get; private set; }\n",
     "        public Material BarMaterial { get; private set; }\n        /// <summary>The visible model (Kenney UFO); null in graybox.</summary>\n        public Transform Body => body;\n",
     "public Transform Body => body;"),
    ("            if (Health <= 0f) return;\n            Health -= amount;\n",
     "            if (Health <= 0f) return;\n            Health -= amount;\n            Juice.EnemyHit(this);\n",
     "Juice.EnemyHit(this);"),
    ("        void Die()\n        {\n",
     "        void Die()\n        {\n            Juice.EnemyDied(this);\n",
     "Juice.EnemyDied(this);"),
])
patch(C + 'CardMarks.cs', [
    ("            enemy.TakeDamage(card.Value);              // the hit itself\n",
     "            Juice.CardHit(enemy, card);\n            enemy.TakeDamage(card.Value);              // the hit itself\n",
     "Juice.CardHit(enemy, card);"),
    ("""            FloatText.Show($"{HandEvaluator.Name(result.hand)}\\n<size=80%>{result.damage:#,0}</size>",
                           transform.position + Vector3.up * 2.4f, HandColor(result.hand));
""",
     """            if (!Juice.Burst(transform.position, result, cards))   // juice shows chips x mult itself
                FloatText.Show($"{HandEvaluator.Name(result.hand)}\\n<size=80%>{result.damage:#,0}</size>",
                               transform.position + Vector3.up * 2.4f, HandColor(result.hand));
""",
     "if (!Juice.Burst(transform.position, result, cards))"),
])
patch(C + 'Projectile.cs', [
    ("        bool spin;",
     "        Vector3 flat;        // position on the straight line; the arc is added on top\n        float travelled, arc;\n        bool spin;",
     "Vector3 flat;"),
    ("            p.speed = tower.Type.projectileSpeed;\n",
     "            p.speed = tower.Type.projectileSpeed;\n            p.flat = go.transform.position;\n            if (cannon) p.arc = Juice.ArcHeight(Vector3.Distance(p.flat, p.lastPos));\n",
     "p.flat = go.transform.position;"),
    ("""            Vector3 to = lastPos - transform.position;
            float step = speed * Time.deltaTime;
            if (spin) transform.Rotate(0f, 720f * Time.deltaTime, 0f, Space.World);
            else if (to.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(to);
            if (to.magnitude > step) { transform.position += to.normalized * step; return; }
            Hit();
""",
     """            Vector3 to = lastPos - flat;
            float step = speed * Time.deltaTime;
            if (spin) transform.Rotate(0f, 720f * Time.deltaTime, 0f, Space.World);
            else if (to.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(to);
            if (to.magnitude > step)
            {
                flat += to.normalized * step;
                travelled += step;
                float k = travelled / (travelled + to.magnitude);   // 0 at the tower, 1 at the target
                transform.position = flat + Vector3.up * (arc * 4f * k * (1f - k));   // cannonball arc (juice)
                return;
            }
            Hit();
""",
     "cannonball arc (juice)"),
    ("                Blast.Show(lastPos, from.Type.areaRadius);\n",
     "                Blast.Show(lastPos, from.Type.areaRadius);\n                Juice.CannonLanded(lastPos, from.Type.areaRadius);\n",
     "Juice.CannonLanded(lastPos"),
])
print('done')

# ---------- round 2 (Wong's playtest notes, 2026-11-24) ----------
U = 'Assets/Progression/Scripts/UI/'
patch(C + 'Tower.cs', [
    ("            Juice.Built(go.transform);\n",
     "            Juice.Built(go.transform, UI.UiKit.TowerFace(type.name));\n",
     "Juice.Built(go.transform, UI.UiKit.TowerFace"),
])
patch(C + 'BuffMachine.cs', [
    ("            Juice.Built(go.transform);\n",
     "            Juice.Built(go.transform, UI.UiKit.MachineFace(System.Array.IndexOf(TDGame.Instance.config.buffs, type)));\n",
     "Juice.Built(go.transform, UI.UiKit.MachineFace"),
])
patch(C + 'Projectile.cs', [
    ("                PTDArt.Place(ammo, go.transform, Vector3.zero, 1.6f);\n",
     "                PTDArt.Place(ammo, go.transform, Vector3.zero, cannon ? 2.4f : 2.0f);   // bigger, easier to follow\n                Juice.DecorateShot(go, cannon);\n",
     "Juice.DecorateShot(go, cannon);"),
])
# no more "burst at best hand" button (Wong): early burst stays on (config.burstAtBestHand)
patch(U + 'TDHud.cs', [
    ("            BuildEarlyBurstButton();\n",
     "            // the \"burst at best hand\" test button is gone (Wong, 2026-11-24); early burst stays on\n            var oldToggle = startWave.transform.parent.Find(\"EarlyBurstToggle\");\n            if (oldToggle != null) oldToggle.gameObject.SetActive(false);\n",
     "the \"burst at best hand\" test button is gone"),
    ("            earlyBurst.gameObject.SetActive(fight);\n            earlyBurst.GetComponentInChildren<TMP_Text>().text",
     "            if (earlyBurst != null) earlyBurst.gameObject.SetActive(fight);\n            if (earlyBurst != null) earlyBurst.GetComponentInChildren<TMP_Text>().text",
     "FadeZones(deploy);"),   # round 3 removed the button code completely
])
# gold: only the number (Wong); lives are shown as hearts (HeartBar)
patch(U + 'WorldHud.cs', [
    ('            Set(gold, ref lastGold, $"GOLD  {game.Gold}");\n',
     '            Set(gold, ref lastGold, $"{game.Gold}");   // number only (Wong, 2026-11-24)\n',
     'Set(gold, ref lastGold, $"{game.Gold}");'),
])
print('done round 2')

# ---------- round 4 (2026-11-24): tower card waits on the tile for a poker card; one selection at a time ----------
G = 'Assets/Progression/Scripts/Game/'
patch(C + 'WorldCard.cs', [
    ("        public float Roll { get; set; }\n",
     "        public float Roll { get; set; }\n        /// <summary>Faces the camera (default). Off = keeps its own rotation (a card lying on a tile).</summary>\n        public bool Billboard { get; set; } = true;\n",
     "public bool Billboard { get; set; } = true;"),
    ("            if (cam != null) transform.rotation = cam.transform.rotation * Quaternion.Euler(0f, 0f, Roll);\n",
     "            if (cam != null && Billboard) transform.rotation = cam.transform.rotation * Quaternion.Euler(0f, 0f, Roll);\n",
     "if (cam != null && Billboard)"),
])
patch(C + 'Tower.cs', [
    ("            Juice.Built(go.transform, UI.UiKit.TowerFace(type.name));\n",
     "            Juice.Built(go.transform, UI.UiKit.TowerFace(type.name), t);   // the tower card waits on the tile for a poker card\n",
     "UI.UiKit.TowerFace(type.name), t);"),
    ("        public void SetCard(Card card)\n        {\n",
     "        /// <summary>Juice: the tower card lying on the tile until the first poker card arrives (then the model burns in).</summary>\n        public GameObject Pending { get; set; }\n\n        public void SetCard(Card card)\n        {\n            Juice.TowerArmed(this);\n",
     "Juice.TowerArmed(this);"),
])
patch(G + 'BuildManager.cs', [
    ("        public void Select(int index) => Selected = Selected == index ? -1 : index;\n",
     "        public void Select(int index)\n        {\n            Selected = Selected == index ? -1 : index;\n            // one selection at a time: picking a tower/machine card drops the selected poker cards (Wong, 2026-11-24)\n            if (Selected >= 0 && TDGame.Instance != null) TDGame.Instance.Hand.Selected.Clear();\n        }\n\n        /// <summary>No tower/machine card selected (a poker card was picked).</summary>\n        public void Deselect() => Selected = -1;\n",
     "public void Deselect() => Selected = -1;"),
])
patch('Assets/Progression/Scripts/UI/HandView.cs', [
    ("        public void Click(int index) => Hand.Toggle(index);\n",
     "        public void Click(int index)\n        {\n            Hand.Toggle(index);\n            // one selection at a time: a poker card drops the selected tower/machine card (Wong, 2026-11-24)\n            if (Hand.Selected.Count > 0 && BuildManager.Instance != null) BuildManager.Instance.Deselect();\n        }\n",
     "a poker card drops the selected tower/machine card"),
])
print('done round 4')

# ---------- round 5: tower card higher and a bit behind the model (did not cover the weapon) ----------
patch(C + 'Tower.cs', [
    ("            cardView = WorldCard.Create(transform, lab.transform.localPosition, 0.9f, 10);\n",
     "            // a little behind the tower (away from the camera), so from the tilted view the card does not cover the weapon (Wong, 2026-11-24)\n            lab.transform.localPosition += Vector3.forward * cardBack;\n            cardView = WorldCard.Create(transform, lab.transform.localPosition, 0.9f, 10);\n",
     "lab.transform.localPosition += Vector3.forward * cardBack;"),
    ("        RangeRing ring;\n", "        RangeRing ring;\n        const float cardBack = 0.9f;   // card offset behind the tower\n", "const float cardBack"),
])
