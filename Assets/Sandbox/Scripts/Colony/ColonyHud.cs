using System.Collections.Generic;
using UnityEngine;

namespace DamRush
{
    /// <summary>
    /// Colony HUD (IMGUI). Look and layout follow two cosy builders the leader liked:
    /// Sandcastle (Bubblebird Studio) - white rounded counters at the top left and a row of
    /// big icon tiles at the bottom - and The Last Hive style research cards - dark cards with a
    /// thin gold outline and pill buttons.
    /// - Top left: materials as white pills (icon + amount; hover = name and room).
    /// - Top centre: season, day, clock and the water state; banners and short pop-ups under it.
    /// - Top right: game speed, family size, and the goal of the season (click to fold).
    /// - Bottom centre: the build bar - category tabs and a row of tool tiles (icon, name, cost).
    ///   A help card above it explains the chosen tool.
    /// - Bottom left: round buttons for Work, Family, Orders, Events and Help; each opens a card.
    /// - Right: pop-up of the clicked building (research tree of a lab, orders of a machine...).
    /// - Tooltip at the mouse, bars and names in the world.
    /// </summary>
    public class ColonyHud : MonoBehaviour
    {
        public ColonyManager colony;
        public Camera cam;
        public BuildOrders orders;
        public MapController map;

        enum Card { None, Work, Family, Orders, Events, Help }
        Card card = Card.None;
        bool showTasks, showDebug, trayOpen = true, goalOpen = true;
        Vector2 orderScroll, familyScroll, eventScroll, cardScroll;
        int buildCat;
        string hoverTip;     // tooltip of the HUD element under the mouse (drawn last)
        float rightTop;      // where the right column continues (under the season goal)

        // ---- clicks on the HUD must not reach the map
        static readonly List<Rect> hudRects = new List<Rect>();
        static float scale = 1f;

        /// <summary>True when the mouse (Input System coordinates) is over a HUD panel.</summary>
        public static bool PointerOverHud(Vector2 mouse)
        {
            var p = new Vector2(mouse.x, Screen.height - mouse.y);
            foreach (var r in hudRects) if (r.Contains(p)) return true;
            return false;
        }

        static void Block(Rect r) => hudRects.Add(new Rect(r.x * scale, r.y * scale, r.width * scale, r.height * scale));

        // ---- palette: warm cream tiles, dark charcoal cards with a gold line, one orange accent
        static readonly Color Cream = new Color(0.97f, 0.93f, 0.83f), CreamHover = new Color(1f, 0.97f, 0.9f), SandLine = new Color(0.82f, 0.7f, 0.48f);
        static readonly Color Charcoal = new Color(0.11f, 0.105f, 0.1f, 0.94f), Charcoal2 = new Color(0.17f, 0.16f, 0.15f, 0.96f), GoldLine = new Color(0.8f, 0.7f, 0.47f);
        static readonly Color Accent = new Color(0.91f, 0.59f, 0.23f), Ink = new Color(0.23f, 0.18f, 0.13f), InkSoft = new Color(0.45f, 0.37f, 0.27f);
        static readonly Color Paper = new Color(0.93f, 0.89f, 0.8f), PaperSoft = new Color(0.8f, 0.77f, 0.69f);
        static readonly Color RepairCol = new Color(0.4f, 0.8f, 1f), NurseryCol = new Color(0.85f, 0.6f, 1f);
        static readonly Color WoodCol = new Color(0.66f, 0.45f, 0.26f), MudCol = new Color(0.47f, 0.35f, 0.24f), StoneCol = new Color(0.66f, 0.68f, 0.72f);
        GUIStyle removeOn;
        GUIStyle panel, title, text, small, tiny, btn, tabOn, center, nameTag, toast, banner;
        GUIStyle pill, tile, tileHover, tileOn, catTile, catOn, roundBtn, roundOn, inkBig, inkName, inkSub, inkKey, shadowText, shadowBig;
        Texture2D white;

        void Start()
        {
            if (map == null) map = FindFirstObjectByType<MapController>();
            if (FindFirstObjectByType<GameSpeed>() == null) gameObject.AddComponent<GameSpeed>();
        }

        /// <summary>Rounded rectangle texture (9-sliced by the style border) with an optional outline.</summary>
        static Texture2D Rounded(Color fill, Color line, int radius, float lineWidth)
        {
            int size = radius * 2 + 4;
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    float dx = Mathf.Max(0f, Mathf.Max(radius - x, x - (size - 1 - radius)));
                    float dy = Mathf.Max(0f, Mathf.Max(radius - y, y - (size - 1 - radius)));
                    float d = Mathf.Sqrt(dx * dx + dy * dy);            // distance into the corner
                    float inside = Mathf.Clamp01(radius + 0.5f - d);
                    float edge = lineWidth > 0f ? Mathf.Clamp01(d - (radius - lineWidth) + 0.5f) : 0f;
                    Color c = Color.Lerp(fill, line, edge);
                    c.a = Mathf.Lerp(fill.a, line.a, edge) * inside;
                    t.SetPixel(x, y, c);
                }
            t.Apply();
            return t;
        }

        static GUIStyle Box(Color fill, Color line, int radius, float lineWidth, int pad)
        {
            var s = new GUIStyle { border = new RectOffset(radius + 2, radius + 2, radius + 2, radius + 2), padding = new RectOffset(pad, pad, pad, pad) };
            s.normal.background = Rounded(fill, line, radius, lineWidth);
            return s;
        }

        static GUIStyle Button(Color normal, Color hover, Color line, Color textCol, int size, int radius = 10)
        {
            var s = new GUIStyle(GUI.skin.button) { richText = true, fontSize = size, alignment = TextAnchor.MiddleCenter, border = new RectOffset(radius + 2, radius + 2, radius + 2, radius + 2) };
            s.normal.background = Rounded(normal, line, radius, 1.5f);
            s.hover.background = s.active.background = Rounded(hover, line, radius, 1.5f);
            s.normal.textColor = s.hover.textColor = s.active.textColor = textCol;
            return s;
        }

        void InitStyles()
        {
            if (panel != null) return;
            white = Texture2D.whiteTexture;
            panel = Box(Charcoal, GoldLine, 14, 1.5f, 16);
            toast = Box(Charcoal2, GoldLine, 12, 1.5f, 10);
            banner = Box(new Color(0.42f, 0.13f, 0.1f, 0.95f), GoldLine, 14, 1.5f, 10);
            pill = Box(new Color(1f, 1f, 1f, 0.97f), new Color(0.88f, 0.84f, 0.75f), 18, 1.5f, 6);
            tile = Box(Cream, SandLine, 14, 2f, 4);
            tileHover = Box(CreamHover, Accent, 14, 2f, 4);
            tileOn = Box(new Color(1f, 0.9f, 0.72f), Accent, 14, 4f, 4);
            catTile = Button(Charcoal2, new Color(0.25f, 0.23f, 0.2f, 0.97f), GoldLine, Paper, 15, 12);
            catOn = Button(Accent, new Color(0.97f, 0.66f, 0.3f), GoldLine, Ink, 15, 12);
            roundBtn = Button(new Color(0.13f, 0.12f, 0.11f, 0.9f), new Color(0.24f, 0.22f, 0.2f, 0.95f), new Color(0, 0, 0, 0), Paper, 16, 14);
            roundOn = Button(Accent, new Color(0.97f, 0.66f, 0.3f), new Color(0, 0, 0, 0), Ink, 16, 14);

            var lbl = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true };
            lbl.normal.textColor = Paper;
            title = new GUIStyle(lbl) { fontSize = 20, fontStyle = FontStyle.Bold };
            title.normal.textColor = new Color(0.95f, 0.85f, 0.6f);
            text = new GUIStyle(lbl) { fontSize = 17 };
            small = new GUIStyle(lbl) { fontSize = 15 };
            small.normal.textColor = PaperSoft;
            tiny = new GUIStyle(lbl) { fontSize = 13 };
            tiny.normal.textColor = new Color(0.68f, 0.64f, 0.56f);
            center = new GUIStyle(text) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
            nameTag = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, wordWrap = false, fontStyle = FontStyle.Bold };
            nameTag.normal.textColor = Color.white;
            btn = Button(Charcoal2, new Color(0.27f, 0.25f, 0.22f, 0.98f), GoldLine, Paper, 16);
            tabOn = Button(Accent, new Color(0.97f, 0.66f, 0.3f), GoldLine, Ink, 16);
            removeOn = Button(new Color(0.78f, 0.16f, 0.12f, 1f), new Color(0.92f, 0.22f, 0.16f, 1f), GoldLine, Color.white, 16);

            inkBig = new GUIStyle(lbl) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, wordWrap = false };
            inkBig.normal.textColor = Ink;
            inkName = new GUIStyle(lbl) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter, wordWrap = false, clipping = TextClipping.Clip };
            inkName.normal.textColor = Ink;
            inkSub = new GUIStyle(lbl) { fontSize = 11, alignment = TextAnchor.UpperCenter, wordWrap = true };
            inkSub.normal.textColor = InkSoft;
            inkKey = new GUIStyle(lbl) { fontSize = 11, fontStyle = FontStyle.Bold, wordWrap = false };
            inkKey.normal.textColor = InkSoft;
            shadowText = new GUIStyle(lbl) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false };
            shadowText.normal.textColor = Color.white;
            shadowBig = new GUIStyle(shadowText) { fontSize = 26 };
        }

        void Fill(Rect r, Color c) { var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, white); GUI.color = old; }

        void Meter(Rect r, float v, Color c)
        {
            Fill(r, new Color(1f, 1f, 1f, 0.12f));
            Fill(new Rect(r.x, r.y, r.width * Mathf.Clamp01(v), r.height), c);
        }

        /// <summary>White text with a soft dark shadow, for text straight on the map (like the wave timer in the references).</summary>
        void Shadowed(Rect r, string s, GUIStyle st)
        {
            var sh = new GUIStyle(st); sh.normal.textColor = new Color(0f, 0f, 0f, 0.55f);
            string plain = System.Text.RegularExpressions.Regex.Replace(s, "<color=[^>]+>|</color>", "");
            GUI.Label(new Rect(r.x + 1.5f, r.y + 2f, r.width, r.height), plain, sh);
            GUI.Label(r, s, st);
        }

        static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);

        // ------------------------------------------------------------------ main

        void OnGUI()
        {
            if (colony == null) return;
            InitStyles();
            if (Event.current.type == EventType.Layout) hudRects.Clear();
            scale = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale;
            hoverTip = null;

            WorldLabels();
            Stocks();
            Clock(w);
            Banners(w);
            TopRight(w);
            if (colony.GameOver) { GameOverScreen(w); return; }
            BuildBar(w);
            CardButtons();
            switch (card)
            {
                case Card.Work: WorkCard(); break;
                case Card.Family: FamilyCard(); break;
                case Card.Orders: OrdersCard(); break;
                case Card.Events: EventsCard(); break;
                case Card.Help: HelpCard(); break;
            }
            SelectionPanel(w);
            Tooltip();
            HudTip();
        }

        // ------------------------------------------------------------------ top left: materials

        void Stocks()
        {
            float x = 18;
            foreach (var it in new[] { Item.Food, Item.Wood, Item.Mud, Item.Stone })
                x = StockPill(x, 14, it, 50f);
            float x2 = 18;
            foreach (var it in new[] { Item.PressedWood, Item.Gravel, Item.Sand, Item.Cement })
                if (colony.Capacity(it) > 0 || colony.PileCount(it) > 0 || colony.Stock(it) > 0) x2 = StockPill(x2, 72, it, 38f);
        }

        /// <summary>A white pill: icon + amount in stock (red when the storages are full). Hover = name, room and piles on the ground.</summary>
        float StockPill(float x, float y, Item item, float h)
        {
            int have = colony.Stock(item), cap = colony.Capacity(item), ground = colony.PileCount(item);
            bool full = cap > 0 && have >= cap;
            var big = new GUIStyle(inkBig) { fontSize = h > 45f ? 22 : 17 };
            string num = full ? $"<color=#c0442a>{have}</color>" : have.ToString();
            float tw = big.CalcSize(new GUIContent(have.ToString())).x;
            float icon = h - 12f;
            var r = new Rect(x, y, Mathf.Max(h * 2.1f, icon + tw + 30f), h);
            GUI.Box(r, GUIContent.none, pill); Block(r);
            GUI.DrawTexture(new Rect(r.x + 8, r.y + 6, icon, icon), HudIcons.ForItem(item));
            GUI.Label(new Rect(r.x + icon + 14, r.y, tw + 12, h), num, big);
            if (ground > 0) Fill(new Rect(r.xMax - 9, r.y + 6, 6, 6), Accent); // a dot: some lies on the ground
            if (r.Contains(Event.current.mousePosition))
                hoverTip = $"<b>{ItemInfo.Name(item)}</b>\n{have} in storage{(cap > 0 ? $" (room {cap})" : " (no storage yet)")}" + (ground > 0 ? $"\n{ground} more on the ground" : "");
            return r.xMax + 8;
        }

        // ------------------------------------------------------------------ top centre: time and water

        void Clock(float w)
        {
            var se = colony.seasons; var dn = colony.dayNight;
            float cx = w * 0.5f;
            if (se != null)
            {
                string sc = se.Current == Season.Spring ? "b8f0a0" : se.Current == Season.Summer ? "ffe08a" : se.Current == Season.Autumn ? "ffbf80" : "cfeeff";
                Shadowed(new Rect(cx - 250, 10, 500, 22), $"<color=#{sc}>{se.Current.ToString().ToUpper()}</color>   DAY {se.Day}/{se.daysPerSeason:0}   ·   YEAR {se.Year}", shadowText);
                Fill(new Rect(cx - 90, 34, 180, 3), new Color(0f, 0f, 0f, 0.3f));
                Fill(new Rect(cx - 90, 34, 180 * Mathf.Clamp01(se.SeasonProgress), 3), new Color(1f, 1f, 1f, 0.85f));
            }
            if (dn != null)
            {
                GUI.DrawTexture(new Rect(cx - 62, 42, 26, 26), HudIcons.Get(dn.IsNight ? "Moon" : "Sun"));
                Shadowed(new Rect(cx - 30, 38, 110, 34), dn.Clock, shadowBig);
            }
            if (map != null)
            {
                string water;
                switch (map.Phase)
                {
                    case FloodPhase.Warning: water = $"<color=#ffc080>FLOOD FORECAST  x{map.floodStrength:0.#}</color>"; break;
                    case FloodPhase.Rising: water = $"<color=#ff9a70>FLOOD RISING  x{map.FlowFactor:0.#}</color>"; break;
                    case FloodPhase.Peak: water = map.WaveHigh ? $"<color=#ff7a60>FLOOD WAVE  x{map.FlowFactor:0.#}  ·  {map.DaysLeft:0.#} d left</color>" : $"<color=#ffd890>between waves  ·  {map.DaysLeft:0.#} d left</color>"; break;
                    case FloodPhase.Falling: water = "<color=#ffd890>flood falling</color>"; break;
                    default: water = "<color=#d8f2ff>water normal</color>"; break;
                }
                string rise = Mathf.Abs(map.LodgeRise) > 0.05f ? $"  ·  lodge {(map.LodgeRise > 0 ? "+" : "")}{map.LodgeRise:0.0} m" : "";
                Shadowed(new Rect(cx - 260, 72, 520, 20), water + rise, new GUIStyle(shadowText) { fontSize = 13 });
            }
        }

        void Banners(float w)
        {
            float y = 100;
            string msg = null;
            if (GameSpeed.Instance != null && GameSpeed.Instance.Paused) msg = "<b>PAUSED</b>  -  press Space or 1x to continue. You can still give orders";
            else if (colony.LodgeFrozenIn) msg = "<b>FROZEN IN</b>  -  the lodge entrance is under the ice. Nobody can reach the food";
            else if (colony.bellRinging) msg = "<b>ALARM BELL</b>  -  the family stays in the lodge. Click the bell for the all clear";
            else if (colony.AlarmActive) msg = $"<b>TAIL SLAP ALARM</b>  -  the family hides in the lodge ({colony.AlarmLeft:0} s)";
            else if (map != null && map.Phase == FloodPhase.Warning)
                msg = $"<b>FLOOD FORECAST</b>  -  waves in {map.PhaseLeft / Mathf.Max(1f, colony.dayNight != null ? colony.dayNight.dayLength : 240f) * 24f:0} h (x{map.floodStrength:0.#}). Red = may flood. Ring the bell when the waves come";
            else if (map != null && map.Flooding && map.WaveHigh)
                msg = $"<b>FLOOD WAVE</b>  -  x{map.FlowFactor:0.#} water, fast and deep: beavers outside can be swept away. " + (orders.Bell != null ? "Ring the bell!" : "Build an alarm bell!");
            else if (map != null && map.Flooding)
                msg = $"<b>FLOOD</b>  -  low water between waves ({map.DaysLeft:0.#} days left). Safe to work now";
            if (msg != null)
            {
                var r = new Rect(w * 0.5f - 340, y, 680, 44);
                GUI.Box(r, GUIContent.none, banner);
                GUI.Label(r, msg, new GUIStyle(center) { fontSize = 15 });
                y += 52;
            }
            // toasts: important events for 6 seconds
            for (int i = 0; i < colony.toasts.Count; i++)
            {
                var (t, at) = colony.toasts[i];
                float age = Time.time - at;
                if (age > 6f) continue;
                var old = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01((6f - age) * 1.5f));
                var r = new Rect(w * 0.5f - 300, y, 600, 34);
                GUI.Box(r, GUIContent.none, toast);
                GUI.Label(r, t, new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, wordWrap = false });
                GUI.color = old;
                y += 40;
            }
        }

        // ------------------------------------------------------------------ top right: speed, family, goal

        static readonly string[] SpeedNames = { "II", "1x", "2x", "3x", "5x" };

        void TopRight(float w)
        {
            // family pill
            int kits = colony.Count(BeaverAge.Kit);
            string fam = $"{colony.beavers.Count}/{colony.family.maxFamily}";
            float tw = inkBig.CalcSize(new GUIContent(fam)).x;
            var fr = new Rect(w - 18 - (tw + 74), 14, tw + 74, 50);
            GUI.Box(fr, GUIContent.none, pill); Block(fr);
            GUI.DrawTexture(new Rect(fr.x + 8, fr.y + 6, 38, 38), HudIcons.Get("Beaver"));
            GUI.Label(new Rect(fr.x + 52, fr.y, tw + 16, 50), fam, inkBig);
            if (fr.Contains(Event.current.mousePosition)) hoverTip = $"<b>Family</b>\n{colony.Workers} workers, {kits} kits (room for {colony.family.maxFamily})";

            // game speed: dark rounded buttons like the undo/redo buttons of Sandcastle
            var gs = GameSpeed.Instance;
            if (gs != null)
                for (int i = SpeedNames.Length - 1; i >= 0; i--)
                {
                    var r = new Rect(fr.x - 12 - (SpeedNames.Length - i) * 50, 17, 44, 44);
                    Block(r);
                    if (GUI.Button(r, $"<b>{SpeedNames[i]}</b>", gs.index == i ? roundOn : roundBtn)) gs.Set(i);
                }

            // season goal card (click the title to fold it)
            rightTop = 76;
            var se = colony.seasons;
            if (se == null) return;
            var gr = new Rect(w - 18 - 400, 76, 400, 34);
            string head = $"<b>{se.Current.ToString().ToUpper()} GOAL</b>  <size=12>{(goalOpen ? "▲" : "▼")}</size>";
            if (goalOpen)
            {
                string body = se.GoalText();
                gr.height = small.CalcHeight(new GUIContent(body), gr.width - 28) + 52;
                GUI.Box(gr, GUIContent.none, toast); Block(gr);
                GUI.Label(new Rect(gr.x + 14, gr.y + 8, gr.width - 28, 22), head, new GUIStyle(small) { normal = { textColor = title.normal.textColor } });
                GUI.Label(new Rect(gr.x + 14, gr.y + 32, gr.width - 28, gr.height - 40), body, small);
            }
            else
            {
                GUI.Box(gr, GUIContent.none, toast); Block(gr);
                GUI.Label(new Rect(gr.x + 14, gr.y + 6, gr.width - 28, 22), head, new GUIStyle(small) { normal = { textColor = title.normal.textColor } });
            }
            if (GUI.Button(new Rect(gr.x, gr.y, gr.width, 30), GUIContent.none, GUIStyle.none)) goalOpen = !goalOpen;
            rightTop = gr.yMax + 10;
        }

        void GameOverScreen(float w)
        {
            var r = new Rect(w * 0.5f - 300, 380, 600, 190);
            GUI.Box(r, GUIContent.none, panel); Block(r);
            var se = colony.seasons;
            GUI.Label(new Rect(r.x, r.y + 20, r.width, 40), "<b>THE FAMILY IS GONE</b>", new GUIStyle(title) { alignment = TextAnchor.MiddleCenter });
            GUI.Label(new Rect(r.x, r.y + 64, r.width, 30), se != null ? $"You lasted until {se.Current.ToString().ToLower()} of year {se.Year}." : "", center);
            if (GUI.Button(new Rect(r.x + 200, r.y + 116, 200, 46), "Play again", tabOn))
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        // ------------------------------------------------------------------ bottom centre: build bar

        struct ToolEntry { public BuildTool t; public string key, name, sub, machine, icon; }

        List<ToolEntry> ToolsOf(int cat)
        {
            int worn = orders.WornDams;
            var ind = colony.industry;
            var g = new List<ToolEntry>();
            void Add(BuildTool t, string key, string name, string sub, string icon, string machine = null) =>
                g.Add(new ToolEntry { t = t, key = key, name = name, sub = sub, icon = icon, machine = machine });
            switch (cat)
            {
                case 0:
                    Add(BuildTool.Dam, "1", "Dam Mk1", Cost(orders.damStages), "Dam");
                    Add(BuildTool.DamMk2, "7", "Dam Mk2", Cost(orders.damMk2.stages), "DamMk2");
                    Add(BuildTool.Spillway, "8", "Spillway", Cost(orders.spillway.stages), "Spillway");
                    Add(BuildTool.Levee, null, "Levee", Cost(orders.levee.stages), "Levee");
                    Add(BuildTool.Floodgate, null, "Floodgate", Cost(orders.floodgate.stages), "Floodgate");
                    Add(BuildTool.Sandbag, null, "Sandbags", orders.sandbagCost.CostText(), "Sandbag");
                    Add(BuildTool.Dig, "Z", "Dig", $"-{orders.digStep:0.00} m", "Dig");
                    break;
                case 1:
                    Add(BuildTool.Base, "2", "Base", Cost(orders.Def(BuildingKind.Base).stages), "Base");
                    Add(BuildTool.PumpBase, "9", "Pump base", Cost(orders.pumpBaseDef.stages), "PumpBase");
                    Add(BuildTool.Nursery, "3", "Nursery", Cost(orders.Def(BuildingKind.Nursery).stages), "Nursery");
                    Add(BuildTool.Storage, "4", "Storage", Cost(orders.Def(BuildingKind.Storage).stages), "Storage");
                    Add(BuildTool.FoodFarm, "0", "Food farm", Cost(orders.farmDef.stages), "FoodFarm");
                    break;
                case 2:
                    Add(BuildTool.MaterialLab, null, "Material lab", Cost(ind.materialLabDef.stages), "MaterialLab");
                    Add(BuildTool.BuildingLab, null, "Building lab", Cost(ind.buildingLabDef.stages), "BuildingLab");
                    Add(BuildTool.WaterWheel, null, "Water wheel", Cost(ind.wheelDef.stages), "WaterWheel");
                    foreach (var m in ind.machines) Add(BuildTool.Machine, null, m.name, Cost(m.stages), "Machine", m.id);
                    break;
                default:
                    Add(BuildTool.AlarmBell, null, "Alarm bell", Cost(orders.bellDef.stages), "AlarmBell");
                    Add(BuildTool.Fell, "X", "Fell trees", $"{colony.fellWood} wood each", "Fell");
                    Add(BuildTool.Priority, "5", "Priority flag", "builders first", "Priority");
                    Add(BuildTool.Repair, "6", "Repair", worn > 0 ? $"{worn} worn dam(s)" : "worn dams", "Repair");
                    break;
            }
            return g;
        }

        void BuildBar(float w)
        {
            string[] cats = { "Water", "Buildings", "Industry", "Tools" };
            const float T = 100f, TH = 112f, Gap = 8f, Cat = 54f;
            float bottom = 1080f - 16f;

            // category tabs (always visible) + fold button
            float tabsW = cats.Length * (Cat + 6f) + Cat;
            float ty = trayOpen ? bottom - TH - 24f - Cat - 8f : bottom - Cat;
            float tx = w * 0.5f - tabsW * 0.5f;
            for (int k = 0; k < cats.Length; k++)
            {
                var r = new Rect(tx + k * (Cat + 6f), ty, Cat, Cat);
                Block(r);
                bool on = trayOpen && buildCat == k;
                if (GUI.Button(r, GUIContent.none, on ? catOn : catTile))
                {
                    if (on) trayOpen = false; else { buildCat = k; trayOpen = true; }
                }
                GUI.DrawTexture(new Rect(r.x + 9, r.y + 9, Cat - 18, Cat - 18), HudIcons.Get(cats[k]));
                if (r.Contains(Event.current.mousePosition)) hoverTip = $"<b>{cats[k]}</b>";
            }
            var fold = new Rect(tx + cats.Length * (Cat + 6f), ty, Cat, Cat);
            Block(fold);
            if (GUI.Button(fold, trayOpen ? "▼" : "▲", catTile))
            {
                trayOpen = !trayOpen;
                if (!trayOpen) orders.SetTool(BuildTool.None);
            }
            if (!trayOpen) return;

            // the tray: one tile per tool
            var list = ToolsOf(buildCat);
            float trayW = list.Count * (T + Gap) - Gap + 24f;
            var tray = new Rect(w * 0.5f - trayW * 0.5f, bottom - TH - 24f, trayW, TH + 24f);
            GUI.Box(tray, GUIContent.none, panel); Block(tray);
            var ind = colony.industry;
            for (int k = 0; k < list.Count; k++)
            {
                var e = list[k];
                var r = new Rect(tray.x + 12 + k * (T + Gap), tray.y + 12, T, TH);
                bool on = orders.Tool == e.t && (e.machine == null || orders.machineType == e.machine);
                bool open = ind.ToolUnlocked(e.t, e.machine ?? orders.machineType, out string needs);
                string sub = open ? e.sub : $"<color=#b2492a>research {needs}</color>";
                if (Tile(r, HudIcons.Get(e.icon), e.key, e.name, sub, on, !open))
                {
                    if (e.machine != null)
                    {
                        bool same = orders.Tool == BuildTool.Machine && orders.machineType == e.machine;
                        orders.machineType = e.machine;
                        if (same || orders.Tool != BuildTool.Machine) orders.SetTool(BuildTool.Machine);
                    }
                    else orders.SetTool(e.t);
                }
                if (r.Contains(Event.current.mousePosition))
                    hoverTip = $"<b>{e.name}</b>{(e.key != null ? $"  <size=12>(key {e.key})</size>" : "")}\n{e.sub}" + (open ? "" : $"\n<color=#ffb070>locked: research {needs}</color>");
            }

            // help card for the chosen tool, above the tabs
            if (orders.Tool != BuildTool.None) ToolHelp(w, ty - 8f);
        }

        /// <summary>A cream tile with an icon, the name and a short cost line; the hotkey sits in the corner.</summary>
        bool Tile(Rect r, Texture2D icon, string key, string name, string sub, bool on, bool locked)
        {
            Block(r);
            bool hover = r.Contains(Event.current.mousePosition);
            GUI.Box(r, GUIContent.none, on ? tileOn : hover ? tileHover : tile);
            var old = GUI.color;
            if (locked) GUI.color = new Color(1f, 1f, 1f, 0.4f);
            GUI.DrawTexture(new Rect(r.x + (r.width - 54f) * 0.5f, r.y + 8, 54, 54), icon);
            GUI.color = old;
            GUI.Label(new Rect(r.x + 4, r.y + 64, r.width - 8, 18), name, inkName);
            GUI.Label(new Rect(r.x + 4, r.y + 81, r.width - 8, 30), $"<size=11>{sub}</size>", inkSub);
            if (key != null) GUI.Label(new Rect(r.x + 9, r.y + 5, 20, 16), key, inkKey);
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        /// <summary>What the chosen tool does, plus its extra choices (storage type, repair all).</summary>
        void ToolHelp(float w, float bottomY)
        {
            string rule = RuleFor(orders.Tool);
            float cw = 640f;
            float h = small.CalcHeight(new GUIContent(rule), cw - 32) + 24;
            bool storage = orders.Tool == BuildTool.Storage;
            int worn = orders.WornDams;
            if (storage) h += 40;
            if (worn > 0 && orders.Tool == BuildTool.Repair) h += 40;
            var r = new Rect(w * 0.5f - cw * 0.5f, bottomY - h, cw, h);
            GUI.Box(r, GUIContent.none, toast); Block(r);
            GUILayout.BeginArea(new Rect(r.x + 16, r.y + 10, r.width - 32, r.height - 16));
            GUILayout.Label($"<color=#{Hex(Accent)}>●</color> {rule}", small);
            if (storage)
            {
                GUILayout.BeginHorizontal();
                foreach (var it in ItemInfo.StorageTypes)
                {
                    var rect = GUILayoutUtility.GetRect(70, 32, GUILayout.Width(70), GUILayout.Height(32));
                    if (GUI.Button(rect, GUIContent.none, orders.storageType == it ? tabOn : btn)) orders.storageType = it;
                    GUI.DrawTexture(new Rect(rect.x + 6, rect.y + 4, 24, 24), HudIcons.ForItem(it));
                    GUI.Label(new Rect(rect.x + 30, rect.y, 40, 32), $"<size=11>{ItemInfo.Name(it)}</size>", new GUIStyle(tiny) { alignment = TextAnchor.MiddleLeft, wordWrap = false, normal = { textColor = orders.storageType == it ? Ink : PaperSoft } });
                }
                GUILayout.EndHorizontal();
            }
            if (worn > 0 && orders.Tool == BuildTool.Repair && GUILayout.Button($"Repair all worn dams ({worn})", btn, GUILayout.Height(30)))
                colony.Log($"Repair ordered on {orders.RepairAll()} dam(s)");
            GUILayout.EndArea();
        }

        string RuleFor(BuildTool tool)
        {
            return tool switch
            {
                BuildTool.Dam => "Blocks the stream so the water behind it rises. Built in 3 stages: stones + mud, sticks, mud seal.",
                BuildTool.DamMk2 => orders.damMk2.rule,
                BuildTool.Spillway => orders.spillway.rule,
                BuildTool.PumpBase => orders.pumpBaseDef.rule,
                BuildTool.FoodFarm => orders.farmDef.rule,
                BuildTool.Dig => $"Click a free hex: the diggers take out {orders.digLoadsPerStep} loads and the ground goes down {orders.digStep:0.00} m (max {orders.digMax:0.0} m). Click again to dig deeper. Deeper water stays open longer under the ice.",
                BuildTool.Levee => orders.levee.rule,
                BuildTool.Floodgate => orders.floodgate.rule,
                BuildTool.Sandbag => $"Click a finished dam, levee or floodgate: +{orders.sandbagHeight:0.0} m and less wear for the flood. Builders bring {orders.sandbagCost.CostText()}.",
                BuildTool.AlarmBell => orders.bellDef.rule,
                BuildTool.MaterialLab => colony.industry.materialLabDef.rule,
                BuildTool.BuildingLab => colony.industry.buildingLabDef.rule,
                BuildTool.WaterWheel => colony.industry.wheelDef.rule,
                BuildTool.Machine => $"On a base next to a water wheel. {colony.industry.Machine(orders.machineType)?.name}: {colony.industry.Machine(orders.machineType)?.recipe.Text()}. Needs one operator (Work tab). Orders and auto in the Industry tab.",
                BuildTool.Fell => $"Click or drag over trees to mark them. Wood cutters fell marked trees first: {colony.fellWood} wood per tree, and the tree is gone. Right-click takes the marks off.",
                BuildTool.Base => orders.Def(BuildingKind.Base).rule,
                BuildTool.Nursery => orders.Def(BuildingKind.Nursery).rule,
                BuildTool.Storage => orders.Def(BuildingKind.Storage).rule,
                BuildTool.Priority => "Click an order to put a flag on it (click again to take it off). Builders bring materials to flagged orders first.",
                BuildTool.Repair => $"Click a worn dam (blue) to repair it before the water washes a stage away. A repair costs about {orders.repairShare * 100f:0}% of the stage x the wear; rebuilding costs the full stage.",
                _ => "Every building stands on a base. Build the base first, then put the building on it.",
            };
        }

        static string Cost(BuildStage[] stages) => BuildStage.Sum(stages).CostText();

        // ------------------------------------------------------------------ bottom left: card buttons

        void CardButtons()
        {
            (Card c, string icon, string name)[] items = { (Card.Work, "Work", "Work"), (Card.Family, "Family", "Family"), (Card.Orders, "Orders", "Orders"), (Card.Events, "Events", "Events"), (Card.Help, "Help", "Help") };
            for (int i = 0; i < items.Length; i++)
            {
                var r = new Rect(18 + i * 64, 1080 - 16 - 58, 58, 58);
                Block(r);
                bool on = card == items[i].c;
                if (GUI.Button(r, GUIContent.none, on ? roundOn : roundBtn)) card = on ? Card.None : items[i].c;
                if (items[i].c == Card.Help) GUI.Label(r, "<b><size=26>?</size></b>", new GUIStyle(center) { normal = { textColor = on ? Ink : Paper } });
                else GUI.DrawTexture(new Rect(r.x + 11, r.y + 9, 36, 36), HudIcons.Get(items[i].icon));
                if (items[i].c == Card.Events && colony.AlarmActive) Fill(new Rect(r.xMax - 12, r.y + 4, 8, 8), new Color(1f, 0.35f, 0.3f));
                if (items[i].c == Card.Orders && orders.sites.Count > 0)
                    GUI.Label(new Rect(r.x, r.yMax - 18, r.width - 4, 16), $"<size=11><b>{orders.sites.Count}</b></size>", new GUIStyle(tiny) { alignment = TextAnchor.MiddleRight, wordWrap = false, normal = { textColor = on ? Ink : Paper } });
                if (r.Contains(Event.current.mousePosition)) hoverTip = $"<b>{items[i].name}</b>";
            }
        }

        /// <summary>A dark card at the bottom left, above the card buttons.</summary>
        Rect CardRect(float height)
        {
            height = Mathf.Min(height, 1080 - 16 - 58 - 12 - 120);
            var r = new Rect(18, 1080 - 16 - 58 - 12 - height, 440, height);
            GUI.Box(r, GUIContent.none, panel);
            Block(r);
            return new Rect(r.x + 18, r.y + 14, r.width - 36, r.height - 26);
        }

        // ------------------------------------------------------------------ work

        void WorkCard()
        {
            var a = CardRect(420);
            GUILayout.BeginArea(a);
            GUILayout.Label("WORK GROUPS", title);
            GUILayout.Label($"{colony.Workers} workers. Kits do not work, they play near the lodge.", small);
            GUILayout.Space(8);
            GroupRow("Foragers", colony.IsWinter ? "winter: nothing to find, the water is frozen" : colony.seasons != null && colony.seasons.Current == Season.Autumn ? "cut branches (food) for winter" : "pick water plants (food)", ItemInfo.Colour(Item.Food), ref colony.forageWorkers);
            GroupRow("Operators", "run the machines (one beaver per machine)", new Color(0.6f, 0.75f, 0.95f), ref colony.operateWorkers);
            GroupRow("Wood cutters", "cut trees, bring wood to storage", WoodCol, ref colony.woodWorkers);
            GroupRow("Diggers", "dig mud, every 3rd load a stone", MudCol, ref colony.earthWorkers);
            GroupRow("Builders", "bring materials to orders", Accent, ref colony.buildWorkers);
            GUILayout.Space(6);
            GUILayout.Label($"<b>Idle: {colony.IdleWorkers}</b>  <size=14>idle beavers repair leaking dams by instinct</size>", text);
            GUILayout.EndArea();
        }

        void GroupRow(string name, string what, Color c, ref int value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<color=#{Hex(c)}>■</color> <b>{name}</b>\n<size=13>{what}</size>", text, GUILayout.Width(250));
            if (GUILayout.Button("-", btn, GUILayout.Width(38), GUILayout.Height(34)) && value > 0) { value--; colony.ApplyGroups(); }
            GUILayout.Label($"<b>{value}</b>", center, GUILayout.Width(40), GUILayout.Height(34));
            if (GUILayout.Button("+", btn, GUILayout.Width(38), GUILayout.Height(34)) && colony.IdleWorkers > 0) { value++; colony.ApplyGroups(); }
            GUILayout.EndHorizontal();
            GUILayout.Space(6);
        }

        // ------------------------------------------------------------------ orders

        void OrdersCard()
        {
            var a = CardRect(640);
            GUILayout.BeginArea(a);
            GUILayout.Label($"ORDERS ({orders.sites.Count})", title);
            GUILayout.Label("Click an order on the map to open it. Right-click twice removes it.", tiny);
            orderScroll = GUILayout.BeginScrollView(orderScroll, GUILayout.Height(520));
            Site remove = null;
            foreach (var s in orders.sites)
            {
                GUILayout.BeginHorizontal();
                string state = !s.HasWork ? "<color=#8fe08f>done</color>" : s.stage < 0 && s is BuildingSite bs && bs.baseSite != null && !bs.baseSite.Finished ? "waiting for base" : s.Progress();
                if (s is DamSite d && d.stage >= 0) state = d.Status();
                if (s is BuildingSite n && n.Kind == BuildingKind.Nursery && n.Finished)
                    state = n.entranceUnderWater ? "<color=#8fe08f>making kits</color>" : "<color=#ff8a70>entrance dry</color>";
                if (s is BuildingSite nw && nw.Kind == BuildingKind.Nursery && nw.Finished && nw.waitReason.Length > 0 && nw.entranceUnderWater)
                    state = $"<color=#ff8a70>waiting: {nw.waitReason}</color>";
                string flagTag = s.priority && s.HasWork ? " <color=#ffd84a>[flag]</color>" : "";
                GUILayout.Label($"<b>{s.Title}</b>{flagTag} <size=13>{s.hex.axial.x},{s.hex.axial.y}</size>\n<size=13>{state}</size>", small, GUILayout.Width(250));
                if (s.HasWork && GUILayout.Button(s.priority ? "<color=#ffd84a>flag</color>" : "flag", btn, GUILayout.Width(52), GUILayout.Height(30))) orders.TogglePriority(s);
                else if (!s.HasWork && s is DamSite wd && orders.CanRepair(wd, out _))
                { if (GUILayout.Button("<color=#80d0ff>fix</color>", btn, GUILayout.Width(52), GUILayout.Height(30))) orders.OrderRepair(wd); }
                else if (!s.HasWork) GUILayout.Space(56);
                bool pending = orders.PendingRemove == s;
                bool blink = pending && Mathf.Repeat(Time.unscaledTime * 4f, 1f) < 0.6f;
                if (GUILayout.Button(pending ? "sure?" : "x", blink ? removeOn : btn, GUILayout.Width(pending ? 62 : 40), GUILayout.Height(30))) remove = s;
                GUILayout.EndHorizontal();
                var last = GUILayoutUtility.GetLastRect();
                if (s.HasWork) Meter(new Rect(last.x, last.yMax + 1, 250, 4), s.Progress01(), s.repair != null ? RepairCol : Accent);
                GUILayout.Space(8);
            }
            GUILayout.EndScrollView();
            if (remove != null) orders.RequestRemove(remove); // first click marks it (flashing red), second click removes
            GUILayout.EndArea();
        }

        // ------------------------------------------------------------------ selection pop-up

        Vector2 selectScroll;

        /// <summary>
        /// Pop-up for the building or order the leader clicked: what it is and does, and its own
        /// controls (research for a lab, orders for a machine, type for a storage), plus flag and remove.
        /// </summary>
        void SelectionPanel(float w)
        {
            var s = orders.Selected;
            if (s == null) return;
            var ind = colony.industry;
            var b = s as BuildingSite;
            bool lab = b != null && (b.Kind == BuildingKind.MaterialLab || b.Kind == BuildingKind.BuildingLab);
            float h = lab && b.Finished ? 640 : b != null && (b.Kind == BuildingKind.Machine || b.Kind == BuildingKind.Storage) ? 380 : 270;
            var r = new Rect(w - 18 - 460, rightTop, 460, Mathf.Min(h, 1080 - 30 - rightTop));
            GUI.Box(r, GUIContent.none, panel); Block(r);
            GUILayout.BeginArea(new Rect(r.x + 14, r.y + 10, r.width - 28, r.height - 20));
            string info = orders.SelectedInfo(out string head);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>{head}</b>", text);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("x", btn, GUILayout.Width(32), GUILayout.Height(28))) { orders.ClearSelection(); GUILayout.EndHorizontal(); GUILayout.EndArea(); return; }
            GUILayout.EndHorizontal();
            selectScroll = GUILayout.BeginScrollView(selectScroll);
            GUILayout.Label(info, small);

            if (b != null && b.Kind == BuildingKind.Storage)
            {
                GUILayout.Space(4);
                GUILayout.Label("Keeps (changing it puts the stock on the ground):", small);
                for (int row = 0; row * 4 < ItemInfo.StorageTypes.Length; row++)
                {
                    GUILayout.BeginHorizontal();
                    for (int k = row * 4; k < Mathf.Min(row * 4 + 4, ItemInfo.StorageTypes.Length); k++)
                    {
                        var it = ItemInfo.StorageTypes[k];
                        if (GUILayout.Button($"<color=#{Hex(ItemInfo.Colour(it))}>■</color> {ItemInfo.Name(it)}", b.storeType == it ? tabOn : btn, GUILayout.Height(28), GUILayout.Width(98)))
                            orders.ChangeStorageType(b, it);
                    }
                    GUILayout.EndHorizontal();
                }
            }
            else if (lab)
            {
                if (!b.Finished) GUILayout.Label("Research starts when the lab is finished.", small);
                else
                {
                    GUILayout.Label($"Food in storage: <b>{colony.Stock(Item.Food)}</b>. Research is paid with food when it starts.", small);
                    ResearchRows(b.Kind == BuildingKind.MaterialLab ? LabKind.Material : LabKind.Building);
                }
            }
            else if (b != null && b.Kind == BuildingKind.Machine && b.Finished)
            {
                GUILayout.Space(4);
                var op = ind.OperatorOf(b);
                GUILayout.Label(op != null ? $"Run by <b>{op.beaverName}</b>: {op.task}" : "No beaver here now: one comes when there is an order", small);
                GUILayout.Label($"Loaded: {ind.BatchesLoaded(b)} batch(es), waiting to carry home: {b.outBuffer}", small);
                GUILayout.BeginHorizontal();
                GUILayout.Label(b.auto && ind.Automation ? "<b>auto</b>" : $"order: <b>{b.ordered}</b>", text, GUILayout.Width(100));
                if (GUILayout.Button("+1", btn, GUILayout.Width(52), GUILayout.Height(32))) b.ordered += 1;
                if (GUILayout.Button("+5", btn, GUILayout.Width(52), GUILayout.Height(32))) b.ordered += 5;
                if (GUILayout.Button("clear", btn, GUILayout.Width(64), GUILayout.Height(32))) b.ordered = 0;
                if (ind.Automation) { if (GUILayout.Button(b.auto ? "auto ON" : "auto off", b.auto ? tabOn : btn, GUILayout.Width(90), GUILayout.Height(32))) b.auto = !b.auto; }
                else GUILayout.Label("<size=11>auto: research M7</size>", small, GUILayout.Width(90));
                GUILayout.EndHorizontal();
            }

            // flood defences
            if (s is DamSite dam && dam.stage >= 0)
            {
                GUILayout.BeginHorizontal();
                if (dam.kind == DamKind.Floodgate && dam.stage >= 1 && GUILayout.Button(dam.gateOpen ? "Close the gate" : "Open the gate", dam.gateOpen ? tabOn : btn, GUILayout.Height(32)))
                    orders.ToggleGate(dam);
                if (orders.CanSandbag(dam, out string bagWhy))
                {
                    bool bagOk = colony.industry.ToolUnlocked(BuildTool.Sandbag, null, out string bagNeeds);
                    if (bagOk && GUILayout.Button($"Add sandbags ({orders.sandbagCost.CostText()})", btn, GUILayout.Height(32))) orders.OrderSandbags(dam);
                    if (!bagOk) GUILayout.Label($"<size=12>Sandbags: research {bagNeeds}</size>", small);
                }
                GUILayout.EndHorizontal();
            }
            if (b != null && b.Kind == BuildingKind.AlarmBell && b.Finished)
            {
                if (GUILayout.Button(colony.bellRinging ? "<b>All clear</b> (back to work)" : "<b>RING THE BELL</b> (everybody to the lodge)", colony.bellRinging ? tabOn : removeOn, GUILayout.Height(40)))
                    colony.RingBell(!colony.bellRinging);
            }
            if (b != null && b.flooded) GUILayout.Label("<color=#ff6050><b>FLOODED</b>: water is above the floor</color>", small);

            // move between a platform and the building on it
            if (BuildOrders.IsPlatformSite(s))
            {
                var onIt = orders.BuildingOn(s);
                if (onIt != null && GUILayout.Button($"Open the {onIt.Title.ToLower()} on it", btn, GUILayout.Height(28))) orders.Select(onIt);
            }
            else if (b != null && b.baseSite != null && GUILayout.Button($"Open the {b.baseSite.Title.ToLower()} under it", btn, GUILayout.Height(28))) orders.Select(b.baseSite);
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            if (s.HasWork && GUILayout.Button(s.priority ? "Take the flag off" : "Priority flag", btn, GUILayout.Height(32))) orders.TogglePriority(s);
            bool pending = orders.PendingRemove == s;
            if (GUILayout.Button(pending ? "<b>Sure? Click to remove</b>" : "Remove", pending ? removeOn : btn, GUILayout.Height(32)))
                if (orders.RequestRemove(s)) orders.ClearSelection();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        /// <summary>The tech tree of one lab: done, researching (with a bar), can start, or why not.</summary>
        void ResearchRows(LabKind lab)
        {
            var ind = colony.industry;
            foreach (var n in ind.tech)
            {
                if (n.lab != lab) continue;
                GUILayout.BeginHorizontal(toast);
                string req = n.requires.Length > 0 ? $" <size=11>(after {string.Join(", ", n.requires)})</size>" : "";
                GUILayout.Label($"<b>{n.id} {n.name}</b>{req}\n<size=12>{n.effect}</size>", small, GUILayout.Width(250));
                if (ind.Unlocked(n.id)) GUILayout.Label("<color=#8fe08f><b>done</b></color>", center, GUILayout.Width(150), GUILayout.Height(40));
                else if (ind.Researching(n, out float p)) GUILayout.Label($"researching\n{p * 100f:0}%", center, GUILayout.Width(150), GUILayout.Height(40));
                else if (ind.CanResearch(n, out string why))
                {
                    if (GUILayout.Button($"Research\n<size=11>{n.food} food · {n.days:0.#} d</size>", btn, GUILayout.Width(150), GUILayout.Height(40))) ind.StartResearch(n);
                }
                else GUILayout.Label($"<size=11>{n.food} food · {n.days:0.#} d\n<color=#ffb070>{why}</color></size>", small, GUILayout.Width(150));
                GUILayout.EndHorizontal();
                if (ind.Researching(n, out float pr)) { var last = GUILayoutUtility.GetLastRect(); Meter(new Rect(last.x, last.yMax - 3, last.width, 3), pr, Accent); }
            }
        }

        // ------------------------------------------------------------------ family

        void FamilyCard()
        {
            var a = CardRect(Mathf.Min(170 + colony.beavers.Count * 46, 820));
            GUILayout.BeginArea(a);
            GUILayout.Label("FAMILY", title);
            int ad = colony.Count(BeaverAge.Adult), ye = colony.Count(BeaverAge.Yearling), ki = colony.Count(BeaverAge.Kit);
            GUILayout.Label($"{ad} adults · {ye} yearlings · {ki} kits", text);
            int room = colony.KitCapacity;
            string nurse = room == 0 ? "No nursery yet: no new kits" : $"Kits {ki}/{room}. A nursery makes {colony.family.litterSize} kits per {colony.family.litterDays:0.#} day for {colony.family.litterFood} food";
            GUILayout.Label(nurse, small);
            showTasks = GUILayout.Toggle(showTasks, " show what each beaver is doing", small);
            familyScroll = GUILayout.BeginScrollView(familyScroll);
            foreach (var b in colony.beavers)
            {
                string grp = b.CanWork ? b.group.ToString() : "plays";
                string carry = b.carrying == Item.None ? "" : $" · carries {Mathf.Max(1, b.carryCount)} {b.carrying.ToString().ToLower()}";
                GUILayout.Label($"<b>{b.beaverName}</b>  <size=13>{b.age} · {grp}{carry}</size>", small);
                var r = GUILayoutUtility.GetLastRect();
                Meter(new Rect(r.x + 250, r.y + 5, 60, 6), b.energy / 100f, b.Tired ? new Color(1f, 0.45f, 0.35f) : new Color(0.5f, 0.75f, 1f));
                Meter(new Rect(r.x + 320, r.y + 5, 60, 6), b.food / 100f, b.Hungry ? new Color(1f, 0.45f, 0.35f) : new Color(0.55f, 0.85f, 0.45f));
                if (b.health < 99f) Meter(new Rect(r.x + 320, r.y + 13, 60, 4), b.health / 100f, new Color(1f, 0.3f, 0.3f));
                if (showTasks) GUILayout.Label($"   {b.task}", tiny);
            }
            GUILayout.EndScrollView();
            GUILayout.Label("<size=13>bars: <color=#80c0ff>energy</color>  <color=#8cd973>food</color></size>", tiny);
            GUILayout.EndArea();
        }

        // ------------------------------------------------------------------ events and debug

        void EventsCard()
        {
            var a = CardRect(showDebug ? 820 : 560);
            GUILayout.BeginArea(a);
            GUILayout.Label("EVENTS", title);
            eventScroll = GUILayout.BeginScrollView(eventScroll, GUILayout.Height(380));
            if (colony.log.Count == 0) GUILayout.Label("Nothing happened yet.", small);
            foreach (var line in colony.log) GUILayout.Label(line, small);
            GUILayout.EndScrollView();
            GUILayout.Space(6);
            showDebug = GUILayout.Toggle(showDebug, " debug", small);
            if (showDebug && map != null)
            {
                var water = map.water;
                GUILayout.Label($"Water in {water.InflowRate:0.0} / out {water.OutflowRate:0.0} m3/s · step {water.StepMilliseconds:0.0} ms{(water.paused ? " · PAUSED" : "")}", small);
                var h = map.Hovered;
                if (h != null)
                {
                    float depth = water.DepthAt(h.center.x, h.center.z);
                    float speed = water.VelocityAt(h.center.x, h.center.z).magnitude;
                    GUILayout.Label($"Hex {h.axial.x},{h.axial.y} · ground {h.minGround:0.0}-{h.maxGround:0.0} m · {(depth > 0.02f ? $"water {depth:0.00} m, {speed:0.00} m/s" : "dry")}", small);
                }
                GUILayout.BeginHorizontal();
                if (GUILayout.Button($"Colour: {water.colorMode}", btn, GUILayout.Height(32))) water.colorMode = (WaterColorMode)(((int)water.colorMode + 1) % 3);
                if (GUILayout.Button(water.paused ? "Play water" : "Pause water", btn, GUILayout.Height(32))) water.paused = !water.paused;
                GUILayout.EndHorizontal();
                float days = Mathf.Max(0.05f, (Time.time - colony.StatsSince) / Mathf.Max(1f, colony.dayNight != null ? colony.dayNight.dayLength : 240f));
                var econ = new System.Text.StringBuilder($"<b>Economy</b> over {days:0.0} day(s), per day:\n");
                foreach (var kv in colony.stats) econ.Append($"{kv.Key}: {kv.Value / days:0.#}   ");
                GUILayout.Label(econ.ToString(), tiny);
                if (GUILayout.Button("Reset economy numbers", btn, GUILayout.Height(28))) colony.ResetStats();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Test flood (F)", btn, GUILayout.Height(32))) map.StartFlood();
                if (GUILayout.Button(orders.ShowGridAlways ? "Grid: always" : "Grid: build only", btn, GUILayout.Height(32))) orders.ShowGridAlways = !orders.ShowGridAlways;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
        }

        // ------------------------------------------------------------------ help

        void HelpCard()
        {
            var a = CardRect(430);
            GUILayout.BeginArea(a);
            GUILayout.Label("CONTROLS", title);
            GUILayout.Label("<b>Camera</b>  WASD move · mouse wheel zoom · middle mouse rotate\n" +
                "<b>Build</b>  pick a tile at the bottom, then click a hex (dams: drag a line)\n" +
                "<b>Keys</b>  1-4, 7-0, Z, X, M build tools · 5 flag · 6 / R repair · Esc close\n" +
                "<b>Remove</b>  right-click an order twice\n" +
                "<b>Open</b>  click a building to see what it does and its controls\n" +
                "<b>Speed</b>  Space pause · - / + slower or faster\n" +
                "<b>Other</b>  G grid · F test flood", small);
            GUILayout.Space(8);
            GUILayout.Label($"Map seed {MapRandomizer.CurrentSeed}: put it in MapRandomizer to play this map again.", tiny);
            GUILayout.EndArea();
        }

        // ------------------------------------------------------------------ tooltips

        /// <summary>Tooltip of the HUD element under the mouse (pills, tiles, buttons).</summary>
        void HudTip()
        {
            if (string.IsNullOrEmpty(hoverTip)) return;
            Vector2 m = Event.current.mousePosition;
            float tw = Mathf.Min(360f, small.CalcSize(new GUIContent(System.Text.RegularExpressions.Regex.Replace(hoverTip, "<[^>]+>", ""))).x + 30f);
            var r = new Rect(m.x + 18, m.y + 16, tw, small.CalcHeight(new GUIContent(hoverTip), tw - 20) + 18);
            float sw = Screen.width / scale;
            if (r.xMax > sw - 8) r.x = m.x - r.width - 10;
            if (r.yMax > 1080 - 8) r.y = m.y - r.height - 10;
            GUI.Box(r, GUIContent.none, toast);
            GUI.Label(new Rect(r.x + 10, r.y + 8, r.width - 20, r.height - 14), hoverTip, small);
        }

        // ------------------------------------------------------------------ in the world

        void Tooltip()
        {
            var hex = orders.HoverHex;
            if (hex == null || hoverTip != null) return;
            if (PointerOverHud(new Vector2(Event.current.mousePosition.x * scale, Screen.height - Event.current.mousePosition.y * scale))) return;
            string body;
            if (orders.Tool == BuildTool.None)
            {
                string info = orders.HexInfo(hex, out string name);
                body = $"<color=#ffd699><b>{name}</b></color>\n{info}";
                var top = orders.TopSiteOn(hex);
                if (top != null && top == orders.PendingRemove)
                    body += $"\n<color=#ff6050><b>Right-click again to remove ({orders.PendingLeft:0.0} s)</b></color>";
            }
            else
            {
                string head = orders.Tool == BuildTool.Priority
                    ? (orders.HoverOk ? "<color=#ffd84a><b>Priority flag</b></color>" : "<color=#ff8a70><b>Nothing to flag</b></color>")
                    : orders.Tool == BuildTool.Repair
                    ? (orders.HoverOk ? "<color=#80d0ff><b>Click to repair</b></color>" : "<color=#ff8a70><b>Cannot repair</b></color>")
                    : orders.HoverOk ? "<color=#8fe08f><b>Click to order</b></color>" : "<color=#ff8a70><b>Cannot build here</b></color>";
                body = $"{head}\n{orders.HoverText}";
            }
            Vector2 m = Event.current.mousePosition;
            var size = small.CalcSize(new GUIContent(body.Replace("<b>", "").Replace("</b>", "")));
            var r = new Rect(m.x + 22, m.y + 18, Mathf.Min(460, size.x + 30), 0);
            r.height = small.CalcHeight(new GUIContent(body), r.width - 20) + 20;
            // keep it on the screen
            float sw = Screen.width / scale;
            if (r.xMax > sw - 8) r.x = m.x - r.width - 12;
            if (r.yMax > 1080 - 36) r.y = 1080 - 36 - r.height;
            GUI.Box(r, GUIContent.none, toast);
            GUI.Label(new Rect(r.x + 10, r.y + 10, r.width - 20, r.height - 20), body, small);
        }

        /// <summary>Names above beavers (only when the camera is close) and labels on sites while building.</summary>
        void WorldLabels()
        {
            if (Event.current.type != EventType.Repaint) return;
            foreach (var b in colony.beavers)
            {
                if (!b.isActiveAndEnabled || !b.transform.GetChild(0).gameObject.activeSelf) continue;
                Vector3 wp = b.transform.position + Vector3.up * 1.1f;
                if ((wp - cam.transform.position).magnitude > 32f && card != Card.Family) continue;
                if (!ToGui(wp, out var p)) continue;
                GUI.Label(new Rect(p.x - 60, p.y - 12, 120, 24), b.beaverName, nameTag);
            }
            bool building = orders.Tool != BuildTool.None || trayOpen;
            bool blinkOn = Mathf.Repeat(Time.unscaledTime * 4f, 1f) < 0.6f;
            float dayLen = colony.dayNight != null ? colony.dayNight.dayLength : 240f;
            foreach (var s in orders.sites)
            {
                float top = s is DamSite ds ? Mathf.Max(ds.Crest, s.hex.maxGround) : s is BuildingSite bs && bs.stage >= 0 ? bs.Floor + (bs.Kind == BuildingKind.Base ? 0f : 1.6f) : s.hex.maxGround;
                Vector3 anchor = new Vector3(s.hex.center.x, top + 1.2f, s.hex.center.z);
                float dist = (anchor - cam.transform.position).magnitude;
                if (dist > 90f || !ToGui(anchor, out var p)) continue;
                bool close = dist < 38f || building;

                if (s == orders.PendingRemove)
                {
                    var r = new Rect(p.x - 110, p.y - 12, 220, 24);
                    Fill(new Rect(r.x + 4, r.y + 2, 212, 20), blinkOn ? new Color(0.6f, 0f, 0f, 0.75f) : new Color(0f, 0f, 0f, 0.45f));
                    GUI.Label(r, "<color=#ff5040>REMOVE? right-click again</color>", new GUIStyle(nameTag) { fontSize = 13 });
                    continue;
                }

                // pick one bar per site: repair, building, nursery or dam condition
                float value; Color col; string label;
                var dam = s as DamSite;
                var nursery = s as BuildingSite;
                bool leak = dam != null && dam.leakNoise > orders.repairInstinctNoise;
                if (s.repair != null) { value = s.Progress01(); col = RepairCol; label = $"repair {(int)(value * 100f)}%"; }
                else if (!s.Finished)
                {
                    value = s.Progress01(); col = dam != null && dam.damaged ? new Color(1f, 0.45f, 0.3f) : Accent;
                    label = (dam != null && dam.damaged ? "rebuild " : s.Title + " ") + $"{(int)(value * 100f)}%";
                }
                else if (nursery != null && nursery.Kind == BuildingKind.Nursery)
                {
                    value = Mathf.Clamp01(nursery.litterTimer / (colony.family.litterDays * dayLen));
                    bool waiting = nursery.waitReason.Length > 0;
                    col = waiting ? new Color(1f, 0.45f, 0.35f) : NurseryCol;
                    label = waiting ? nursery.waitReason : $"next kits {(int)(value * 100f)}%";
                }
                else if (dam != null && dam.condition < 0.995f)
                {
                    value = dam.condition;
                    col = Color.Lerp(new Color(1f, 0.3f, 0.2f), new Color(0.45f, 0.9f, 0.4f), Mathf.InverseLerp(0.2f, 0.9f, value));
                    label = $"condition {(int)(value * 100f)}%";
                    close |= value < 0.5f;
                }
                else if (leak) { value = 1f; col = new Color(1f, 0.3f, 0.2f); label = ""; }
                else continue;

                float bw = 64f, bh = 7f;
                var bar = new Rect(p.x - bw * 0.5f, p.y, bw, bh);
                Fill(new Rect(bar.x - 2, bar.y - 2, bw + 4, bh + 4), new Color(0f, 0f, 0f, 0.6f));
                Fill(bar, new Color(1f, 1f, 1f, 0.15f));
                Fill(new Rect(bar.x, bar.y, bw * Mathf.Clamp01(value), bh), col);
                if (s.priority && s.HasWork) Fill(new Rect(bar.xMax + 3, bar.y - 2, 4, bh + 4), new Color(1f, 0.85f, 0.2f));
                if (leak) label = "<color=#ff7060>LEAK!</color> " + label;
                if (close && label.Length > 0)
                    GUI.Label(new Rect(p.x - 80, p.y - 20, 160, 18), label, new GUIStyle(nameTag) { fontSize = 12 });
            }
        }

        bool ToGui(Vector3 world, out Vector2 p)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            p = new Vector2(sp.x / scale, (Screen.height - sp.y) / scale);
            return sp.z > 0f;
        }
    }
}
