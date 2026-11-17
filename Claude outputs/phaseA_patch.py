# Phase A patch for Dam Rush: beaver food/forage/health/winter, HUD, 5x speed, ice shader, 128x96 map.
# Run from the GameSystem2 project root. Each edit checks its anchor; a file is only written when all its edits match.
import sys, os
root = sys.argv[1] if len(sys.argv) > 1 else "."
S = os.path.join(root, "Assets/Sandbox")
report = []

def patch(rel, edits):
    p = os.path.join(S, rel)
    s = open(p, encoding="utf-8").read()
    for a, b in edits:
        if a not in s:
            if b in s:
                report.append(f"{rel}: already applied: {a[:50]!r}")
                continue
            report.append(f"{rel}: ANCHOR MISSING, file not written: {a[:60]!r}")
            return
        s = s.replace(a, b, 1)
    open(p, "w", encoding="utf-8").write(s)
    report.append(f"{rel}: ok")

# ------------------------------------------------------------------ Beaver
patch("Scripts/Colony/Beaver.cs", [
("""        public float food = 100f;
""", """        public float food = 100f;
        [Tooltip("Drops while the stomach is empty; the beaver dies at 0.")]
        public float health = 100f;
        bool dead;
"""),
("""            UpdateNeeds(dt);
            switch (state)""", """            UpdateNeeds(dt);
            if (dead) return;
            switch (state)"""),
("""            food = Mathf.Max(0f, food - n.hungerPerSecond * (age == BeaverAge.Kit ? 0.5f : 1f) * dt);
""", """            food = Mathf.Max(0f, food - n.hungerPerSecond * (age == BeaverAge.Kit ? 0.5f : 1f) * dt);
            // an empty stomach costs health; a fed beaver heals again
            float day = colony.dayNight != null ? colony.dayNight.dayLength : 240f;
            if (food <= 0f) health -= 100f / Mathf.Max(0.1f, n.starveDays * day) * dt;
            else if (food > n.hungryBelow) health = Mathf.Min(100f, health + 100f / day * dt);
            if (health <= 0f && !dead) { dead = true; colony.Die(this, "hunger"); return; }
"""),
("""            if (colony.AlarmActive) { Hide("Hiding: tail slap alarm", true); return; }
            if (Tired) { Hide("Sleeping in the lodge", false); return; }
            if (Hungry && carrying == Item.None) { Eat(); return; }
            if (Hungry && carrying == Item.Wood) { EatCarried(); return; }
""", """            if (colony.GameOver) return;
            if (colony.AlarmActive) { Hide("Hiding: tail slap alarm", true); return; }
            if (colony.IsWinter) { WinterThink(); return; }
            if (Tired) { Hide("Sleeping in the lodge", false); return; }
            if (Hungry && carrying == Item.None) { Eat(); return; }
"""),
("""            System.Predicate<Item> wants = group == WorkGroup.Wood ? (i => i == Item.Wood)""",
 """            System.Predicate<Item> wants = group == WorkGroup.Forage ? (i => i == Item.Food) : group == WorkGroup.Wood ? (i => i == Item.Wood)"""),
("""            if (CanWork && group == WorkGroup.Wood && colony.FreeSpace(Item.Wood) <= 0) { Wander("Storage full of wood"); return; }""",
 """            if (CanWork && group == WorkGroup.Forage) { Forage(); return; }
            if (CanWork && group == WorkGroup.Wood && colony.FreeSpace(Item.Wood) <= 0) { Wander("Storage full of wood"); return; }"""),
("""            energy = Mathf.Min(100f, energy + colony.needs.sleepEnergyPerSecond * Time.deltaTime);
""", """            energy = Mathf.Min(100f, energy + colony.needs.sleepEnergyPerSecond * Time.deltaTime);
            if (colony.IsWinter)
            {
                // winter: stay in the lodge; go out only to eat, and only when the entrance is not frozen in
                if (Hungry && !colony.LodgeFrozenIn && colony.NearestFood(transform.position) != null) ComeOut();
                return;
            }
"""),
])

# replace Eat + EatCarried with the new food methods
p = os.path.join(S, "Scripts/Colony/Beaver.cs")
s = open(p, encoding="utf-8").read()
a = s.find("        /// <summary>Beavers eat bark. Wood in a storage is the food cache; without it, gnaw a tree.</summary>")
b = s.find("        /// <summary>Go into the lodge: to sleep, or to hide after a tail slap.</summary>")
if a >= 0 and b > a:
    new = '''        /// <summary>
        /// Eat from the nearest food storage (in winter only one with deep water next to it).
        /// Without stored food, gnaw bark from a tree: slow and only half a meal. No trees in winter.
        /// </summary>
        void Eat()
        {
            var store = colony.NearestFood(transform.position);
            if (store != null)
            {
                task = "Going to eat";
                GoTo(store.hex, store.Door(transform.position), () =>
                {
                    if (!colony.TakeFrom(store, Item.Food)) { GoThink(0.2f); return; }
                    colony.Stat("food eaten");
                    task = "Eating";
                    StartWork(colony.needs.eatTime, () => { food = Mathf.Min(100f, food + colony.needs.foodPerMeal); GoThink(0.2f); });
                });
                return;
            }
            if (!colony.IsWinter && colony.PickTree(this, out Vector3 treePos, out HexCell treeHex))
            {
                task = "No stored food: going to gnaw a tree";
                GoTo(treeHex, treePos + (transform.position - treePos).normalized * 0.7f, () =>
                {
                    colony.Release(this);
                    task = "Gnawing bark (half a meal)";
                    colony.Stat("meals from trees");
                    StartWork(colony.needs.eatTime * 2f, () => { food = Mathf.Min(100f, food + colony.needs.foodPerMeal * 0.5f); GoThink(0.2f); });
                });
                return;
            }
            if (colony.IsWinter) { Hide("Hungry: no food under the ice", false); return; }
            Wander("Hungry: no food in the storages");
        }

        /// <summary>Forager: water plants in spring and summer, branches from trees in autumn, then to a food storage.</summary>
        void Forage()
        {
            if (colony.FreeSpace(Item.Food) <= 0) { Wander("Food storages are full"); return; }
            if (!colony.PickForageSpot(this, out Vector3 pos, out HexCell hex, out bool tree)) { Wander("No food to find here now"); return; }
            task = tree ? "Going to cut branches (food)" : "Going to pick water plants";
            Vector3 target = tree ? pos + (transform.position - pos).normalized * 0.7f : pos;
            GoTo(hex, target, () =>
            {
                task = tree ? "Cutting branches" : "Picking water plants";
                StartWork(colony.forageTime, () =>
                {
                    colony.Release(this);
                    carrying = Item.Food;
                    carryCount = CarryCapacity + (colony.seasons != null ? colony.seasons.ForageBonus : 0);
                    colony.Stat("food foraged", carryCount);
                    UpdateCarryVisual();
                    Deliver();
                });
            });
        }

        /// <summary>Winter: no outside work. Bring home what it carries, eat when hungry, else stay in the lodge.</summary>
        void WinterThink()
        {
            if (carrying != Item.None) { Deliver(); return; }
            if (Hungry) { Eat(); return; }
            Hide("Waiting out the winter in the lodge", false);
        }

'''
    s = s[:a] + new + s[b:]
    open(p, "w", encoding="utf-8").write(s)
    report.append("Beaver.cs: Eat/Forage/WinterThink ok")
elif "void WinterThink()" in s:
    report.append("Beaver.cs: Eat/Forage already applied")
else:
    report.append("Beaver.cs: EAT ANCHORS MISSING")

# ------------------------------------------------------------------ GameSpeed
patch("Scripts/Game/GameSpeed.cs", [
("public static readonly float[] Speeds = { 0f, 1f, 2f, 3f };", "public static readonly float[] Speeds = { 0f, 1f, 2f, 3f, 5f };"),
("    /// Game speed for the leader: pause, normal, fast and very fast.", "    /// Game speed for the leader: pause, 1x, 2x, 3x and 5x (5x is mainly for playtests over many seasons)."),
])

# ------------------------------------------------------------------ HUD
patch("Scripts/Colony/ColonyHud.cs", [
("""            if (dn != null) GUI.Label(new Rect(170, 36, 260, 20), dn.IsNight ? "beavers work best at night" : "work by day tires them faster", tiny);

            float x = 440;
            x = StockChip(x, "Wood", Item.Wood, WoodCol);
            x = StockChip(x, "Mud", Item.Mud, MudCol);
            x = StockChip(x, "Stone", Item.Stone, StoneCol);""",
"""            var se = colony.seasons;
            if (se != null)
            {
                string sc = se.Current == Season.Spring ? "9fe08a" : se.Current == Season.Summer ? "ffe08a" : se.Current == Season.Autumn ? "ffb070" : "bfe6ff";
                GUI.Label(new Rect(170, 36, 270, 20), $"<color=#{sc}><b>{se.Current.ToString().ToUpper()}</b></color> day {se.Day}/{se.daysPerSeason:0} · year {se.Year}", tiny);
            }

            float x = 440;
            foreach (var it in ItemInfo.All)
                if (colony.Capacity(it) > 0 || colony.PileCount(it) > 0 || colony.Stock(it) > 0) x = StockChip(x, ItemInfo.Name(it), it, ItemInfo.Colour(it));"""),
("""            return x + 135;""", """            return x + 118;"""),
("""            SpeedButtons(w - 650);""", """            SpeedButtons(w - 700);"""),
("""        static readonly string[] SpeedNames = { "II", "1x", "2x", "3x" };""", """        static readonly string[] SpeedNames = { "II", "1x", "2x", "3x", "5x" };"""),
("""            if (GameSpeed.Instance != null && GameSpeed.Instance.Paused) msg = "<b>PAUSED</b>  -  press Space or 1x to continue. You can still give orders";""",
 """            if (GameSpeed.Instance != null && GameSpeed.Instance.Paused) msg = "<b>PAUSED</b>  -  press Space or 1x to continue. You can still give orders";
            else if (colony.LodgeFrozenIn) msg = "<b>FROZEN IN</b>  -  the lodge entrance is under the ice. Nobody can reach the food";"""),
("""            Tabs();
            switch (tab)""", """            SeasonGoal(w);
            if (colony.GameOver) { GameOverScreen(w); return; }
            Tabs();
            switch (tab)"""),
("""        // ------------------------------------------------------------------ tabs""", """        /// <summary>Goal of the season, top right under the bar.</summary>
        void SeasonGoal(float w)
        {
            var se = colony.seasons;
            if (se == null) return;
            string body = $"<b>{se.Current.ToString().ToUpper()}</b>  <size=13>day {se.Day}/{se.daysPerSeason:0}, year {se.Year}</size>\\n{se.GoalText()}";
            var r = new Rect(w - 470, 66, 460, 0);
            r.height = small.CalcHeight(new GUIContent(body), r.width - 24) + 22;
            GUI.Box(r, GUIContent.none, toast); Block(r);
            GUI.Label(new Rect(r.x + 12, r.y + 10, r.width - 24, r.height - 16), body, small);
            Meter(new Rect(r.x + 12, r.yMax - 6, r.width - 24, 3), se.SeasonProgress, Accent);
        }

        void GameOverScreen(float w)
        {
            var r = new Rect(w * 0.5f - 300, 380, 600, 190);
            GUI.Box(r, GUIContent.none, banner); Block(r);
            var se = colony.seasons;
            GUI.Label(new Rect(r.x, r.y + 20, r.width, 40), "<b>THE FAMILY IS GONE</b>", new GUIStyle(title) { alignment = TextAnchor.MiddleCenter });
            GUI.Label(new Rect(r.x, r.y + 64, r.width, 30), se != null ? $"You lasted until {se.Current.ToString().ToLower()} of year {se.Year}." : "", center);
            if (GUI.Button(new Rect(r.x + 200, r.y + 116, 200, 46), "Play again", btn))
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        // ------------------------------------------------------------------ tabs"""),
("""            GUILayout.Space(4);
            string rule = orders.Tool switch""", """            if (orders.Tool == BuildTool.Storage)
            {
                GUILayout.Label("Storage type:", small);
                GUILayout.BeginHorizontal();
                foreach (var it in ItemInfo.StorageTypes)
                    if (GUILayout.Button($"<color=#{Hex(ItemInfo.Colour(it))}>■</color> {ItemInfo.Name(it)}", orders.storageType == it ? tabOn : btn, GUILayout.Height(30)))
                        orders.storageType = it;
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(4);
            string rule = orders.Tool switch"""),
("""            var a = Panel(330);
            GUILayout.BeginArea(a);
            GUILayout.Label("WORK GROUPS", title);""", """            var a = Panel(390);
            GUILayout.BeginArea(a);
            GUILayout.Label("WORK GROUPS", title);"""),
("""            GroupRow("Wood cutters", "cut trees, bring wood to storage", WoodCol, ref colony.woodWorkers);""",
 """            GroupRow("Foragers", colony.IsWinter ? "winter: nothing to find, the water is frozen" : colony.seasons != null && colony.seasons.Current == Season.Autumn ? "cut branches (food) for winter" : "pick water plants (food)", ItemInfo.Colour(Item.Food), ref colony.forageWorkers);
            GroupRow("Wood cutters", "cut trees, bring wood to storage", WoodCol, ref colony.woodWorkers);"""),
("""A nursery makes {colony.family.litterSize} kits per {colony.family.litterDays:0.#} day for {colony.family.litterWood} wood""",
 """A nursery makes {colony.family.litterSize} kits per {colony.family.litterDays:0.#} day for {colony.family.litterFood} food"""),
("""                Meter(new Rect(r.x + 320, r.y + 5, 60, 6), b.food / 100f, b.Hungry ? new Color(1f, 0.45f, 0.35f) : new Color(0.55f, 0.85f, 0.45f));""",
 """                Meter(new Rect(r.x + 320, r.y + 5, 60, 6), b.food / 100f, b.Hungry ? new Color(1f, 0.45f, 0.35f) : new Color(0.55f, 0.85f, 0.45f));
                if (b.health < 99f) Meter(new Rect(r.x + 320, r.y + 13, 60, 4), b.health / 100f, new Color(1f, 0.3f, 0.3f));"""),
("""1-4 build tools, 5 flag, 6/R repair""", """1-4 build tools, 5 flag, 6/R repair  ·  5x speed for tests"""),
])

# ------------------------------------------------------------------ ice in the water shader
patch("Shaders/DR_StylizedWater.shader", [
("""            float _DR_RippleCount;
""", """            float _DR_RippleCount;
            float _DR_Ice; // 0 = open water, 1 = thickest winter ice (set by Seasons.cs)
"""),
("""                col = lerp(col, _LineColor.rgb, white);
""", """                col = lerp(col, _LineColor.rgb, white);

                // winter ice: a pale, cracked cover that grows with the ice thickness
                float ice = saturate(_DR_Ice);
                if (ice > 0.001)
                {
                    float2 cr = Voronoi(xz * 0.35 + 11.3, 0.0);
                    float crack = 1.0 - smoothstep(0.02, 0.05, cr.y);
                    float3 iceCol = float3(0.80, 0.89, 0.95) * (0.92 + 0.08 * Noise(xz * 0.8));
                    iceCol = lerp(iceCol, float3(1, 1, 1), crack * 0.6);
                    col = lerp(col, iceCol, ice * 0.85);
                }
"""),
("""                float alpha = i.color.a;
""", """                float alpha = i.color.a;
                alpha = lerp(alpha, max(alpha, 0.92 * step(0.001, alpha)), saturate(_DR_Ice));
"""),
])

# ------------------------------------------------------------------ bigger map
patch("Editor/MainMapBuilder.cs", [
("""                AssetDatabase.CreateAsset(config, Root + "/Data/MainMapConfig.asset");
            }
""", """                AssetDatabase.CreateAsset(config, Root + "/Data/MainMapConfig.asset");
            }
            // plan v2: bigger map for the new buildings (128 x 96 m); stream points are scaled with it
            config.cellsX = 128; config.cellsZ = 96;
            EditorUtility.SetDirty(config);
"""),
("""        static StreamPath Stream(string name, float halfWidth, float valley, float depth, int joins, params Vector2[] pts) =>
            new StreamPath { name = name, points = pts, halfWidth = halfWidth, valleyHalfWidth = valley, sourceDepth = depth, joins = joins };""",
 """        /// <summary>The stream and source points were drawn for a 96 x 72 m map; plan v2 scales them to 128 x 96 m.</summary>
        const float MapScale = 128f / 96f;

        static StreamPath Stream(string name, float halfWidth, float valley, float depth, int joins, params Vector2[] pts)
        {
            for (int i = 0; i < pts.Length; i++) pts[i] *= MapScale;
            return new StreamPath { name = name, points = pts, halfWidth = halfWidth, valleyHalfWidth = valley, sourceDepth = depth, joins = joins };
        }"""),
("""            var s = new GameObject(name).AddComponent<WaterSource>();
            s.transform.SetParent(parent);""", """            p *= MapScale;
            var s = new GameObject(name).AddComponent<WaterSource>();
            s.transform.SetParent(parent);"""),
("""            colony.grid = hex; colony.water = water; colony.ground = terrain; colony.forestRoot = forest;""",
 """            colony.grid = hex; colony.water = water; colony.ground = terrain; colony.forestRoot = forest;
            colony.preferredLodgeSpot = new Vector2(46f, 35f) * MapScale;"""),
("""            ctrl.colony = colony;
            var hud""", """            ctrl.colony = colony;
            var seasons = colony.gameObject.AddComponent<Seasons>();
            seasons.colony = colony; seasons.map = ctrl;
            colony.seasons = seasons;
            var hud"""),
])

print("\n".join(report))
