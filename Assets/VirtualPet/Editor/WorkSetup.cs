using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace WindPet.EditorTools
{
    /// <summary>
    /// Wires the electricity job into the lab after it is built (called by the lab
    /// builder): the power store, the turbine, the order printer, the pachinko slot
    /// and the on-screen bill viewer. Menu: Wind Pet/Setup Work Desk.
    /// </summary>
    public static class WorkSetup
    {
        /// <summary>How far the pachinko machine was moved towards the desk after export.</summary>
        const float PrinterScale = 1.4f;
        public static readonly Vector3 PachinkoOffset = new Vector3(-0.7f, 0f, -1.3f);
        // positions of the slot and the coin tray in the exported model (world, before the move)
        static readonly Vector3 SlotWorld = new Vector3(5.9f, -0.42f, 3.41f);
        static readonly Vector3 TrayWorld = new Vector3(4.7f, -0.55f, 3.33f);

        const string DataDir = "Assets/VirtualPet/Data";
        const string MatDir = "Assets/VirtualPet/Art/Lab/Materials";

        public static WorkConfig Config()
        {
            string path = DataDir + "/WorkConfig.asset";
            var cfg = AssetDatabase.LoadAssetAtPath<WorkConfig>(path);
            if (cfg == null) { cfg = ScriptableObject.CreateInstance<WorkConfig>(); AssetDatabase.CreateAsset(cfg, path); }
            return cfg;
        }

        [MenuItem("Wind Pet/Setup Work Desk")]
        static void Menu()
        {
            var lab = GameObject.Find("Lab");
            if (lab != null) Setup(lab.transform);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(lab.scene);
        }

        public static void Setup(Transform lab)
        {
            var cfg = Config();
            var pet = Object.FindAnyObjectByType<PetDrag>();
            var fsm = Object.FindAnyObjectByType<PetStateMachine>();
            var wallet = Object.FindAnyObjectByType<Wallet>();
            var look = pet != null ? pet.GetComponent<PetLook>() : null;
            var smoke = AssetDatabase.LoadAssetAtPath<Material>("Assets/VirtualPet/Materials/Smoke_Billboard.mat");

            // power store
            var eco = GameObject.Find("Economy");
            if (eco == null) eco = new GameObject("Economy");
            var grid = eco.GetComponent<PowerGrid>();
            if (grid == null) grid = eco.AddComponent<PowerGrid>();
            grid.config = cfg;
            grid.wallet = wallet;
            var bank = eco.GetComponent<LoanOffice>();
            if (bank == null) bank = eco.AddComponent<LoanOffice>();
            bank.config = cfg;
            bank.wallet = wallet;
            var office = eco.GetComponent<ContractOffice>();
            if (office == null) office = eco.AddComponent<ContractOffice>();
            office.config = cfg;
            office.wallet = wallet;
            var clock = eco.GetComponent<GameClock>();
            if (clock == null) clock = eco.AddComponent<GameClock>();
            clock.config = cfg;
            clock.display = ClockText();

            // turbine
            var turbineT = lab.Find("Lab_Work/Item_Turbine");
            if (turbineT != null)
            {
                var t = turbineT.gameObject.AddComponent<Turbine>();
                t.config = cfg; t.pet = pet; t.fsm = fsm; t.look = look; t.wallet = wallet;
                t.rotor = lab.Find("Lab_Work/Item_TurbineRotor");
                t.smokeMaterial = smoke;
                Bounds b = turbineT.GetComponent<Renderer>().bounds;   // main mesh: the glass dips below the desk
                // the counter number sits on the dark window at the front of the base
                Physics.SyncTransforms();
                Vector3 probe = new Vector3(b.center.x, b.min.y + 0.24f, b.min.z - 1f);
                float frontZ = Physics.Raycast(probe, Vector3.forward, out RaycastHit hit, 3f) ? hit.point.z : b.min.z;
                t.counter = Text3D(turbineT, "Counter", new Vector3(b.center.x, b.min.y + 0.24f, frontZ - 0.01f), 2.4f,
                                   new Color(1f, 0.9f, 0.55f), new Vector2(0.9f, 0.26f));
                t.label = Text3D(turbineT, "BrokenLabel", new Vector3(b.center.x, b.max.y + 0.5f, b.center.z), 3f,
                                 new Color(1f, 0.55f, 0.45f), new Vector2(4f, 1.2f));
                t.label.gameObject.SetActive(false);
                var bulbGo = new GameObject("BulbLight");
                bulbGo.transform.SetParent(turbineT, false);
                bulbGo.transform.position = new Vector3(b.center.x, b.max.y - 0.15f, b.center.z);
                t.bulb = bulbGo.AddComponent<Light>();
                t.bulb.type = LightType.Point;
                t.bulb.color = new Color(1f, 0.85f, 0.5f);
                t.bulb.range = 4f;
                t.bulb.intensity = 0f;
            }

            // order printer
            var printerT = lab.Find("Lab_Work/Item_OrderPrinter");
            if (printerT != null)
            {
                // a bigger printer, grown from the middle of its bottom so it stays on the desk
                Bounds before = WorldBounds(printerT);
                Vector3 foot = new Vector3(before.center.x, before.min.y, before.center.z);
                printerT.localScale *= PrinterScale;
                printerT.position = foot + (printerT.position - foot) * PrinterScale;
                printerT.position += new Vector3(0.6f, 0f, 0f);   // the bigger body would be cut off by the left edge
                var p = printerT.gameObject.AddComponent<OrderPrinter>();
                p.config = cfg;
                p.paperMaterial = LitMaterial("M_BillPaper", new Color(0.953f, 0.894f, 0.745f));
                p.edgeMaterial = LitMaterial("M_BillEdge", new Color(0.42f, 0.25f, 0.11f));
                p.contractEdgeMaterial = LitMaterial("M_ContractEdge", new Color(0.69f, 0.23f, 0.18f));
                Bounds b = WorldBounds(printerT);
                // bills come out flat on the desk to the right of the printer, where
                // there is room, and slide a little towards the player
                p.spawnOffset = new Vector3(b.max.x + 0.75f, b.min.y + 0.35f, b.center.z - 0.1f) - printerT.position;
                p.ejectVelocity = new Vector3(0.6f, 0f, -0.8f);
                var lampGo = new GameObject("Lamp");
                lampGo.transform.SetParent(printerT, false);
                lampGo.transform.position = new Vector3(b.center.x, b.max.y + 0.2f, b.min.z - 0.2f);
                p.lamp = lampGo.AddComponent<Light>();
                p.lamp.type = LightType.Point;
                p.lamp.color = new Color(1f, 0.85f, 0.45f);
                p.lamp.range = 2f;
                p.lamp.intensity = 0f;
                // the red button: front, left, low (Blender: PX-0.3, PY-0.42, DESK+0.16 on a 1.0 x 0.8 x 0.62 body)
                Vector3 button = new Vector3(b.center.x - 0.3f * PrinterScale, b.min.y + 0.16f * PrinterScale, b.min.z + 0.02f);
                p.buttonLocal = printerT.InverseTransformPoint(button);
                bank.printer = p;
            }

            // pachinko ORDERS IN slot: a trigger zone in front of the slot
            var pachinko = lab.Find("Lab_Pachinko");
            if (pachinko != null)
            {
                var slot = new GameObject("OrdersInSlot");
                slot.transform.SetParent(pachinko, true);
                slot.transform.position = SlotWorld + PachinkoOffset + new Vector3(0f, 0.1f, -0.35f);
                slot.transform.rotation = Quaternion.identity;
                var trigger = slot.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.size = new Vector3(1.3f, 1.1f, 1.1f);
                var recv = slot.AddComponent<PachinkoReceiver>();
                recv.wallet = wallet;
                recv.coinMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/VirtualPet/Art/FX/M_Bubble.mat");
                var tray = new GameObject("CoinTray");
                tray.transform.SetParent(pachinko, true);
                tray.transform.position = TrayWorld + PachinkoOffset;
                recv.coinTray = tray.transform;
                SetupShop(cfg, wallet, pachinko, tray.transform);
            }

            SetupBillViewer();
            EditorUtility.SetDirty(cfg);
        }

        // ---------- on-screen bill ----------

        static void SetupBillViewer()
        {
            var canvas = GameObject.Find("StatusCanvas");
            if (canvas == null || canvas.transform.Find("BillView") != null)
            {
                // already there: only make sure the viewer is wired
                var existing = canvas != null ? canvas.GetComponentInChildren<WindPet.UI.BillViewer>(true) : null;
                if (existing != null)
                {
                    existing.propLayers = 1 << LayerMask.NameToLayer("Props");
                    var view = canvas.transform.Find("BillView");
                    existing.wantsLabel = view.Find("WantsLabel").GetComponent<TMP_Text>();
                    existing.paysLabel = view.Find("PaysLabel").GetComponent<TMP_Text>();
                    existing.fine = view.Find("Fine").GetComponent<TMP_Text>();
                }
                return;
            }
            var paper = new Color(0.953f, 0.894f, 0.745f);
            var ink = new Color(0.23f, 0.16f, 0.11f);
            var panel = Box("BillView", canvas.transform, paper);
            var rt = (RectTransform)panel.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(40f, -60f);
            rt.sizeDelta = new Vector2(380f, 500f);
            var outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(0.17f, 0.1f, 0.06f);
            outline.effectDistance = new Vector2(3f, -3f);

            Label(panel.transform, "Title", "BREEZE & BOLT", 30, ink, new Vector2(0, -24), new Vector2(340, 40), FontStyles.Bold);
            Label(panel.transform, "Sub", "ELECTRICITY EXCHANGE · PURCHASE ORDER", 13, ink, new Vector2(0, -64), new Vector2(340, 22), FontStyles.Normal);
            var order = Label(panel.transform, "OrderNo", "Order No. PO-0412", 16, ink, new Vector2(0, -100), new Vector2(340, 24), FontStyles.Normal);
            Label(panel.transform, "BuyerLabel", "BUYER", 12, ink, new Vector2(0, -136), new Vector2(340, 18), FontStyles.Normal);
            var buyer = Label(panel.transform, "Buyer", "Mrs. H. Vermeer", 22, ink, new Vector2(0, -158), new Vector2(340, 60), FontStyles.Normal);
            var wants = Label(panel.transform, "WantsLabel", "WANTS TO BUY", 12, ink, new Vector2(-85, -240), new Vector2(160, 18), FontStyles.Normal);
            var kwh = Label(panel.transform, "Kwh", "3 kWh", 44, ink, new Vector2(-85, -262), new Vector2(160, 60), FontStyles.Bold);
            var pays = Label(panel.transform, "PaysLabel", "OFFERS TO PAY", 12, ink, new Vector2(85, -240), new Vector2(160, 18), FontStyles.Normal);
            var euro = Label(panel.transform, "Euro", "€12.00", 40, ink, new Vector2(85, -262), new Vector2(160, 60), FontStyles.Bold);
            var fineText = Label(panel.transform, "Fine", "Stamp to approve, then post it in the pachinko slot. Unstamped orders go back unpaid.", 13, ink,
                  new Vector2(0, -350), new Vector2(330, 50), FontStyles.Italic);
            var stamp = Label(panel.transform, "Stamp", "APPROVED", 46, new Color(0.75f, 0.22f, 0.17f), new Vector2(40, -420), new Vector2(300, 64), FontStyles.Bold);
            stamp.transform.localRotation = Quaternion.Euler(0f, 0f, 12f);

            var viewer = canvas.AddComponent<WindPet.UI.BillViewer>();
            viewer.panel = panel;
            viewer.orderNo = order;
            viewer.buyer = buyer;
            viewer.kwh = kwh;
            viewer.euro = euro;
            viewer.stamp = stamp.gameObject;
            viewer.wantsLabel = wants;
            viewer.paysLabel = pays;
            viewer.fine = fineText;
            viewer.propLayers = 1 << LayerMask.NameToLayer("Props");
            panel.SetActive(false);
        }

        // ---------- clock ----------

        /// <summary>A plain time display under the money counter (the UI pass restyles it).</summary>
        static TMP_Text ClockText()
        {
            var canvas = GameObject.Find("StatusCanvas");
            if (canvas == null) return null;
            var old = canvas.transform.Find("Clock");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var t = Label(canvas.transform, "Clock", "Day 1  08:00", 30, new Color(1f, 0.95f, 0.85f), Vector2.zero, new Vector2(320, 80), FontStyles.Bold);
            var rt = (RectTransform)t.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -24f);
            t.alignment = TextAlignmentOptions.Top;
            var cursor = canvas.transform.Find("ToolCursor");
            if (cursor != null) t.transform.SetSiblingIndex(cursor.GetSiblingIndex());
            return t;
        }

        // ---------- pachinko shop ----------

        /// <summary>
        /// A "SHOP" sign on the pachinko screen, and the shop window on the status canvas
        /// (a plain frame in the bill colours; the UI pass restyles it).
        /// </summary>
        static void SetupShop(WorkConfig cfg, Wallet wallet, Transform pachinko, Transform tray)
        {
            var glass = pachinko.Find("Lab_Pachinko_Glass");
            Bounds g = glass != null ? glass.GetComponent<Renderer>().bounds : WorldBounds(pachinko);
            var sign = Text3D(pachinko, "ShopSign", new Vector3(g.center.x, g.center.y, g.min.z - 0.05f), 4f,
                              new Color(1f, 0.85f, 0.35f), new Vector2(1.6f, 0.8f));
            sign.text = "SHOP\n<size=45%>click me</size>";

            var canvas = GameObject.Find("StatusCanvas");
            if (canvas == null) return;
            var old = canvas.transform.Find("Shop");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var paper = new Color(0.953f, 0.894f, 0.745f);
            var ink = new Color(0.23f, 0.16f, 0.11f);
            var brass = new Color(0.88f, 0.69f, 0.3f);

            // full-screen dim layer: blocks clicks to the room, a click on it closes the shop
            var root = new GameObject("Shop", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            var cursor = canvas.transform.Find("ToolCursor");
            if (cursor != null) root.transform.SetSiblingIndex(cursor.GetSiblingIndex());   // the tool cursor stays on top
            Stretch((RectTransform)root.transform);
            var holder = new GameObject("Panel", typeof(RectTransform));   // hidden and shown; the root keeps the script running
            holder.transform.SetParent(root.transform, false);
            Stretch((RectTransform)holder.transform);
            var dim = Box("Blocker", holder.transform, new Color(0.1f, 0.06f, 0.03f, 0.55f));
            dim.GetComponent<Image>().raycastTarget = true;
            Stretch((RectTransform)dim.transform);
            var blocker = dim.AddComponent<Button>();
            blocker.transition = Selectable.Transition.None;

            var win = Box("Window", holder.transform, paper);
            win.GetComponent<Image>().raycastTarget = true;
            var wr = (RectTransform)win.transform;
            wr.anchorMin = wr.anchorMax = wr.pivot = new Vector2(0.5f, 0.5f);
            wr.sizeDelta = new Vector2(820f, 640f);
            var ol = win.AddComponent<Outline>();
            ol.effectColor = ink; ol.effectDistance = new Vector2(4f, -4f);

            Label(win.transform, "Title", "BREEZE & BOLT SUPPLY", 34, ink, new Vector2(0, -22), new Vector2(700, 44), FontStyles.Bold);
            var close = MakeButton(win.transform, "Close", "X", new Vector2(370, -20), new Vector2(48, 48), new Color(0.75f, 0.22f, 0.17f), Color.white);

            // turbine upgrade
            var tBox = Box("TurbineRow", win.transform, new Color(1f, 0.97f, 0.88f));
            Place((RectTransform)tBox.transform, new Vector2(0, -84), new Vector2(760, 90));
            var tInfo = Label(tBox.transform, "Info", "Turbine Lv 1/5", 24, ink, new Vector2(-150, -14), new Vector2(440, 64), FontStyles.Normal);
            tInfo.alignment = TextAlignmentOptions.Left;
            var up = MakeButton(tBox.transform, "Upgrade", "Upgrade €20", new Vector2(250, -18), new Vector2(220, 56), brass, ink);

            // potions: 2 columns x 3 rows
            Label(win.transform, "PotionsTitle", "POTIONS", 16, ink, new Vector2(0, -190), new Vector2(700, 24), FontStyles.Bold);
            var rows = new WindPet.UI.ShopUI.PotionRow[cfg.potions.Length];
            for (int i = 0; i < rows.Length; i++)
            {
                var def = cfg.potions[i];
                var card = Box("Potion" + i, win.transform, new Color(1f, 0.97f, 0.88f));
                Place((RectTransform)card.transform, new Vector2(i % 2 == 0 ? -192 : 192, -222 - (i / 2) * 116), new Vector2(370, 104));
                var icon = Box("Icon", card.transform, Color.white).GetComponent<Image>();
                Place((RectTransform)icon.transform, new Vector2(-140, -12), new Vector2(80, 80));
                icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/VirtualPet/UI/Icons/Tools/" + def.model + ".png");
                icon.preserveAspect = true;
                var title = Label(card.transform, "Name", def.name, 20, ink, new Vector2(10, -12), new Vector2(200, 28), FontStyles.Bold);
                title.alignment = TextAlignmentOptions.Left;
                var effect = Label(card.transform, "Effect", def.Effect, 14, ink, new Vector2(10, -42), new Vector2(200, 50), FontStyles.Normal);
                effect.alignment = TextAlignmentOptions.TopLeft;
                var buy = MakeButton(card.transform, "Buy", "€" + def.price, new Vector2(140, -24), new Vector2(76, 56), brass, ink);
                rows[i] = new WindPet.UI.ShopUI.PotionRow { icon = icon, title = title, effect = effect, buy = buy, price = buy.GetComponentInChildren<TMP_Text>() };
            }
            var footer = Label(win.transform, "Footer", "No potions active", 16, ink, new Vector2(0, -590), new Vector2(760, 36), FontStyles.Italic);

            var shop = root.AddComponent<WindPet.UI.ShopUI>();
            shop.config = cfg;
            shop.wallet = wallet;
            shop.stats = Object.FindAnyObjectByType<PetStats>();
            shop.panel = holder;
            shop.blocker = blocker;
            shop.closeButton = close;
            shop.turbineInfo = tInfo;
            shop.upgradeButton = up;
            shop.upgradeLabel = up.GetComponentInChildren<TMP_Text>();
            shop.rows = rows;
            shop.footer = footer;
            shop.pachinko = pachinko;
            shop.screenCentre = g.center;
            shop.screenRadius = Mathf.Max(g.extents.x, g.extents.y);
            shop.tray = tray;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static void Place(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        static Button MakeButton(Transform parent, string name, string text, Vector2 pos, Vector2 size, Color color, Color textColor)
        {
            var go = Box(name, parent, color);
            go.GetComponent<Image>().raycastTarget = true;
            Place((RectTransform)go.transform, pos, size);
            var ol = go.AddComponent<Outline>();
            ol.effectColor = new Color(0.17f, 0.1f, 0.06f); ol.effectDistance = new Vector2(2f, -2f);
            var b = go.AddComponent<Button>();
            var colors = b.colors;
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
            b.colors = colors;
            var t = Label(go.transform, "Text", text, 22, textColor, Vector2.zero, size, FontStyles.Bold);
            t.alignment = TextAlignmentOptions.Center;
            var tr = (RectTransform)t.transform;
            tr.anchorMin = tr.anchorMax = tr.pivot = new Vector2(0.5f, 0.5f);
            return b;
        }

        static GameObject Box(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return go;
        }

        static TextMeshProUGUI Label(Transform parent, string name, string text, float size, Color color, Vector2 pos, Vector2 box, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = box;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.fontStyle = style;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            return t;
        }

        // ---------- helpers ----------

        static TextMeshPro Text3D(Transform parent, string name, Vector3 worldPos, float size, Color color, Vector2 box)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, true);
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.identity;   // faces the camera (-Z)
            var t = go.AddComponent<TextMeshPro>();
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.fontStyle = FontStyles.Bold;
            t.rectTransform.sizeDelta = box;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.text = "0000.0";
            return t;
        }

        static Material LitMaterial(string name, Color color)
        {
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", 0.1f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Bounds WorldBounds(Transform t)
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            Bounds b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
