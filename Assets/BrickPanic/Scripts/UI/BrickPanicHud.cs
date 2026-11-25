using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BrickPanic
{
    /// <summary>The Brick Panic HUD (uGUI, built into the scene by "Brick Panic/Build HUD In Scene", so every part can be
    /// seen and changed in the Editor). It only reads the game state and calls RoundManager / BuildController; it has no rules.
    /// - Top left: round title and goal. Top right: money, people alive / goal, joint wear bar.
    /// - Build phase: forecast card (next disaster) + START at the top. Bottom left: SHOP button and the current tool.
    ///   SHOP (or B) pops up the materials and the person; a material pops up its sizes; a size closes the popup
    ///   (Wong, 2026-11-25: one step at a time instead of every card on screen). "?" pops up the controls.
    /// - Disaster: the shop slides away, a big banner names the disaster, a live counter shows alive / dead.
    /// - Result: a card counts up survivors, dead, rubble and the reward one line at a time, then CONTINUE.
    /// - Game over: a red card drops in with a shake, then RESTART.
    /// Motion follows "juice": small, fast animations that answer every action (Jonasson &amp; Purho, 2012).</summary>
    public class BrickPanicHud : MonoBehaviour
    {
        [Header("Game")]
        public RoundManager round;
        public BuildController build;
        public PhysicsActivator activator;
        [Tooltip("Fonts, sprites, icons and colours.")]
        public BPTheme theme;

        [Header("Top left")]
        public TextMeshProUGUI roundTitle;
        public TextMeshProUGUI goalText;

        [Header("Stats (top right)")]
        public RectTransform moneyPill;
        public TextMeshProUGUI moneyText;
        public RectTransform alivePill;
        public TextMeshProUGUI aliveText;
        public RectTransform wearPill;
        [Tooltip("Bar fill; its anchorMax.x is set to the average joint wear (0-1).")]
        public RectTransform wearFill;
        public TextMeshProUGUI wearText;

        [Header("Forecast (build phase)")]
        public RectTransform forecastRoot;
        public Image forecastIcon;
        public TextMeshProUGUI forecastName;
        public TextMeshProUGUI forecastDetail;
        public Button startButton;
        [Tooltip("One red line under START, only when there are fewer people than the goal.")]
        public TextMeshProUGUI peopleWarning;

        [Header("Live (during the disaster)")]
        public RectTransform liveRoot;
        public Image liveIcon;
        public TextMeshProUGUI liveName;
        public TextMeshProUGUI liveDetail;
        public TextMeshProUGUI liveAlive;
        public TextMeshProUGUI liveDead;

        [Header("Shop (build phase, bottom left)")]
        [Tooltip("Bottom-left group: SHOP button + the chip with the current tool. Slides away during a disaster.")]
        public RectTransform shopRoot;
        public Button shopButton;
        [Tooltip("Picture of the current tool (brick in its material, or the person).")]
        public Image toolIcon;
        public TextMeshProUGUI toolName;
        public TextMeshProUGUI toolPrice;
        [Tooltip("Card pictures per material (2x4 brick in that material), same order as BuildController.materials.")]
        public Sprite[] materialIcons;
        [Tooltip("Card pictures of every shape in every material: index = material x shape count + shape.")]
        public Sprite[] shapeIconsByMaterial;

        [Header("Popup 1: material or person")]
        public RectTransform categoryPopup;
        public BPCard[] materialCards;
        public BPCard personCard;

        [Header("Popup 2: size")]
        public RectTransform sizePopup;
        public TextMeshProUGUI sizeTitle;
        public BPCard[] shapeCards;
        public Button backButton;

        [Header("Tools (middle right): build / erase")]
        [Tooltip("Dark panel with the two tool buttons. Slides away during a disaster.")]
        public RectTransform toolPanel;
        public Button buildToolButton;
        public Button eraseToolButton;
        [Tooltip("Colour of the chosen tool button. Default green.")]
        public Color toolOnColor = new Color(0.3f, 0.85f, 0.5f);
        [Tooltip("Colour of the other tool button. Default dark grey.")]
        public Color toolOffColor = new Color(0.32f, 0.3f, 0.38f);

        [Header("Help")]
        public Button helpButton;
        public RectTransform helpPopup;

        [Header("Banner")]
        public TextMeshProUGUI banner;

        [Header("Round result")]
        public RectTransform resultRoot;
        [Tooltip("Big heading: ROUND X.")]
        public TextMeshProUGUI resultTitle;
        [Tooltip("Smaller heading under it: SURVIVED!")]
        public TextMeshProUGUI resultSubtitle;
        public TextMeshProUGUI resultSurvivors;
        public TextMeshProUGUI resultDead;
        public TextMeshProUGUI resultRubble;
        public TextMeshProUGUI resultReward;
        public Button continueButton;

        [Header("Game over")]
        public RectTransform gameOverRoot;
        public TextMeshProUGUI gameOverTitle;
        public TextMeshProUGUI gameOverStats;
        public Button restartButton;

        [Header("Motion")]
        [Tooltip("Seconds for panels to slide in or out. Default 0.45.")]
        public float slideTime = 0.45f;
        [Tooltip("Seconds per counted line on the result card. Default 0.45.")]
        public float countTime = 0.45f;

        enum Popup { None, Category, Size, Help }
        Popup popup = Popup.None;
        /// <summary>True while a popup is open: BuildController does not build or sell then.</summary>
        public static bool PopupOpen { get; private set; }

        RoundManager.State shownState = (RoundManager.State)(-1);
        Disaster shownNext;
        int shownSide = -1, shownRound = -1, shownAlive = -1, shownGoal = -1;
        float shownMoney = float.NaN;
        Vector2 shopHome, forecastHome, liveHome, toolHome;
        BuildController.Tool shownTool = (BuildController.Tool)(-1);
        Sequence countSeq;
        bool counting;

        void OnEnable() { Person.TypeSet += PersonPlaced; }
        void OnDisable() { Person.TypeSet -= PersonPlaced; PopupOpen = false; }

        /// <summary>A small label pops up over a newly placed person with its random type.</summary>
        void PersonPlaced(Person p)
        {
            var cam = Camera.main;
            if (cam == null || theme == null || p == null) return;
            Vector3 sp = cam.WorldToScreenPoint(p.transform.position + Vector3.up * 2.2f);
            if (sp.z < 0f) return;
            Color c = p.Type == Person.Kind.Kid ? theme.yellow : p.Type == Person.Kind.Elder ? theme.blue : p.Type == Person.Kind.Athlete ? theme.green : theme.cream;
            var t = new GameObject("TypeLabel", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            t.rectTransform.SetParent(transform, false);
            t.font = theme.title;
            if (theme.titleOutline != null) t.fontSharedMaterial = theme.titleOutline;
            t.fontSize = 30f;
            t.color = c;
            t.text = p.Type.ToString().ToUpper() + "!";
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            t.rectTransform.sizeDelta = new Vector2(240f, 50f);
            t.rectTransform.position = sp;
            t.rectTransform.localScale = Vector3.zero;
            var s = DOTween.Sequence().SetLink(t.gameObject);
            s.Append(t.rectTransform.DOScale(1f, 0.25f).SetEase(Ease.OutBack));
            s.Join(t.rectTransform.DOMoveY(sp.y + 50f, 0.9f).SetEase(Ease.OutQuad));
            s.Insert(0.55f, t.DOFade(0f, 0.35f));
            s.OnComplete(() => Destroy(t.gameObject));
        }

        void Awake()
        {
            if (shopRoot != null) shopHome = shopRoot.anchoredPosition;
            if (toolPanel != null) toolHome = toolPanel.anchoredPosition;
            if (forecastRoot != null) forecastHome = forecastRoot.anchoredPosition;
            if (liveRoot != null) liveHome = liveRoot.anchoredPosition;
            if (banner != null) banner.alpha = 0f;
        }

        void Start()
        {
            if (round == null) round = FindFirstObjectByType<RoundManager>();
            if (build == null) build = FindFirstObjectByType<BuildController>();
            if (activator == null) activator = FindFirstObjectByType<PhysicsActivator>();
            if (round != null) round.showDebugHud = false;
            WireCards();
            if (startButton != null) startButton.onClick.AddListener(() => round.StartDisaster());
            if (continueButton != null) continueButton.onClick.AddListener(() => { if (!counting) round.Continue(); else FinishCount(); });
            if (restartButton != null) restartButton.onClick.AddListener(() => round.Restart());
            if (buildToolButton != null) buildToolButton.onClick.AddListener(() => build.SetTool(BuildController.Tool.Build));
            if (eraseToolButton != null) eraseToolButton.onClick.AddListener(() => build.SetTool(BuildController.Tool.Erase));
            if (toolPanel != null) toolPanel.anchoredPosition = toolHome + Vector2.right * 200f;
            if (shopButton != null) shopButton.onClick.AddListener(() => Open(popup == Popup.None ? Popup.Category : Popup.None));
            if (backButton != null) backButton.onClick.AddListener(() => Open(Popup.Category));
            if (helpButton != null) helpButton.onClick.AddListener(() => Open(popup == Popup.Help ? Popup.None : Popup.Help));
            foreach (var p in new[] { categoryPopup, sizePopup, helpPopup }) if (p != null) p.gameObject.SetActive(false);
            if (resultRoot != null) resultRoot.gameObject.SetActive(false);
            if (gameOverRoot != null) gameOverRoot.gameObject.SetActive(false);
            if (liveRoot != null) liveRoot.anchoredPosition = liveHome + Vector2.up * 260f;
            if (shopRoot != null) shopRoot.anchoredPosition = shopHome + Vector2.down * 320f;
            if (forecastRoot != null) forecastRoot.anchoredPosition = forecastHome + Vector2.up * 420f;
        }

        // ------------------------------------------------------------ cards

        void WireCards()
        {
            if (build == null) return;
            for (int i = 0; i < materialCards.Length && i < build.materials.Length; i++)
            {
                int k = i;
                var m = build.materials[i];
                var c = materialCards[i];
                c.Clicked = () => { if (!build.buildEnabled) return; build.SelectMaterial(k); Open(Popup.Size); };
                c.TipTitle = () => m.displayName.ToUpper();
                c.TipBody = () => MaterialTip(m);
            }
            for (int i = 0; i < shapeCards.Length && i < build.shapes.Length; i++)
            {
                int k = i;
                var s = build.shapes[i];
                var c = shapeCards[i];
                c.Clicked = () => { if (!build.buildEnabled) return; build.SelectShape(k); Open(Popup.None); ToolPop(); };
                c.TipTitle = () => s.displayName.ToUpper();
                c.TipBody = () => $"Size: {s.studs.x} x {s.studs.y} studs, {(s.IsPlate ? "plate (1/3 high)" : "full brick")}\n" +
                                  $"In {build.Material.displayName.ToLower()}: <color=#FFC933>{Money(s.Price(build.Material))}</color>, {s.Mass(build.Material):0.#} kg\n" +
                                  "<color=#A99FB5>Z / X to switch, R to rotate</color>";
            }
            if (personCard != null)
            {
                personCard.Clicked = () => { if (!build.buildEnabled) return; build.SetMode(BuildController.Mode.People); Open(Popup.None); ToolPop(); };
                personCard.TipTitle = () => "PERSON";
                personCard.TipBody = () => $"Price: <color=#FFC933>{Money(build.personPrice)}</color>\n" +
                                           "A random type:\n<color=#FFC933>KID</color> small, fast, reckless\n<color=#7FB2FF>ELDER</color> slow, calm, fragile\n<color=#6FE09A>ATHLETE</color> fast, high jump, tough\nor an ADULT.\nPlace on any flat spot.\n" +
                                           "<color=#A99FB5>Right-click a person: full price back. TAB to switch.</color>";
            }
        }

        string MaterialTip(MaterialDef m)
        {
            string heat = m.heatSeconds > 0f ? $"burns after {m.heatSeconds:0.#} s in lava" : "does not burn";
            return $"Price: <color=#FFC933>{Money(m.pricePerStud * 8f)}</color> for a 2x4 (this shape <color=#FFC933>{Money(build.Shape.Price(m))}</color>)\n" +
                   $"Durability: {m.durability:0} s of full shaking\n" +
                   $"Joint strength: {m.breakForcePerStud:0} N per stud\n" +
                   $"Weight: {m.massPerStud:0.#} kg per stud\n" +
                   $"{(m.floats ? "Floats" : "Sinks")}, {heat}\n" +
                   "<color=#A99FB5>Stronger only buys time: every disaster can break every material.</color>";
        }

        void UpdateCards()
        {
            if (build == null) return;
            bool bricks = build.CurrentMode == BuildController.Mode.Bricks;
            for (int i = 0; i < materialCards.Length && i < build.materials.Length; i++)
            {
                var c = materialCards[i];
                var m = build.materials[i];
                float price = build.Shape.Price(m);
                c.Selected = bricks && build.MaterialIndex == i;
                c.Affordable = build.money >= price;
                if (c.priceText != null) c.priceText.text = Money(m.pricePerStud * 8f) + " / 2X4";
            }
            for (int i = 0; i < shapeCards.Length && i < build.shapes.Length; i++)
            {
                var c = shapeCards[i];
                float price = build.shapes[i].Price(build.Material);
                c.Selected = bricks && build.ShapeIndex == i;
                int si = build.MaterialIndex * build.shapes.Length + i;
                if (c.icon != null && shapeIconsByMaterial != null && si < shapeIconsByMaterial.Length && c.icon.sprite != shapeIconsByMaterial[si]) c.icon.sprite = shapeIconsByMaterial[si];
                c.Affordable = build.money >= price;
                if (c.priceText != null) c.priceText.text = Money(price);
            }
            if (toolIcon != null)
            {
                toolIcon.sprite = bricks ? (materialIcons != null && build.MaterialIndex < materialIcons.Length ? materialIcons[build.MaterialIndex] : null) : theme.person;
                toolIcon.color = bricks ? Color.white : theme.ink;
            }
            if (toolName != null) toolName.text = bricks ? build.Material.displayName.ToUpper() + "  " + ShapeLabel(build.Shape) : "PERSON";
            if (toolPrice != null) toolPrice.text = Money(bricks ? build.Shape.Price(build.Material) : build.personPrice);
            if (sizeTitle != null) sizeTitle.text = build.Material.displayName.ToUpper() + ": PICK A SIZE";
            if (personCard != null)
            {
                personCard.Selected = !bricks;
                personCard.Affordable = build.money >= build.personPrice;
                if (personCard.priceText != null) personCard.priceText.text = Money(build.personPrice);
            }
        }

        // ------------------------------------------------------------ every frame

        void Update()
        {
            if (round == null || build == null) return;
            BPTooltip.Suppressed = round.Current != RoundManager.State.Build;
            if (round.Current != shownState) { PhaseChanged(shownState, round.Current); shownState = round.Current; }
            if (round.Current == RoundManager.State.Build && (round.Next != shownNext || round.NextSide != shownSide || round.Round != shownRound))
            {
                bool first = shownNext == null;
                shownNext = round.Next; shownSide = round.NextSide; shownRound = round.Round;
                ShowForecast(!first);
            }
            UpdateStats();
            if (round.Current == RoundManager.State.Build) { UpdateCards(); PopupKeys(); UpdateTools(); }
            if (round.Current == RoundManager.State.Disaster || round.Current == RoundManager.State.Settle) UpdateLive();
        }

        void UpdateStats()
        {
            if (roundTitle != null) roundTitle.text = "ROUND " + round.Round;
            if (goalText != null) goalText.text = $"GOAL: {round.Goal} SURVIVORS";
            if (round.Goal != shownGoal && shownGoal >= 0 && goalText != null) goalText.rectTransform.DOPunchScale(Vector3.one * 0.25f, 0.4f, 6, 0.6f);
            shownGoal = round.Goal;

            float money = build.money;
            if (!Mathf.Approximately(money, shownMoney))
            {
                if (!float.IsNaN(shownMoney))
                {
                    float d = money - shownMoney;
                    Pop(moneyPill, d > 0f ? 0.18f : 0.1f);
                    FloatText(moneyPill, (d > 0f ? "+" : "-") + Money(Mathf.Abs(d)), d > 0f ? theme.green : theme.red);
                }
                shownMoney = money;
                if (moneyText != null) moneyText.text = Money(money);
            }

            int alive = Person.AliveCount;
            if (alive != shownAlive)
            {
                if (shownAlive >= 0) Pop(alivePill, 0.15f);
                shownAlive = alive;
            }
            if (aliveText != null)
            {
                aliveText.text = $"{alive} / {round.Goal}";
                aliveText.color = alive >= round.Goal ? theme.green : theme.red;
            }

            float wear = activator != null ? Mathf.Clamp01(activator.AverageWear) : 0f;
            if (wearFill != null)
            {
                wearFill.anchorMax = new Vector2(Mathf.Lerp(wearFill.anchorMax.x, Mathf.Max(0.04f, wear), 0.15f), 1f);
                var img = wearFill.GetComponent<Image>();
                if (img != null) img.color = Color.Lerp(theme.green, theme.red, wear * 1.4f);
            }
            if (wearText != null) wearText.text = $"WEAR {wear * 100f:0}%";

            bool few = round.Current == RoundManager.State.Build && alive < round.Goal;
            if (peopleWarning != null)
            {
                if (few != peopleWarning.gameObject.activeSelf) peopleWarning.gameObject.SetActive(few);
                if (few) peopleWarning.text = $"NEED {round.Goal - alive} MORE PEOPLE";
            }
        }

        void UpdateLive()
        {
            if (liveAlive != null) { liveAlive.text = Person.AliveCount.ToString(); liveAlive.color = Person.AliveCount >= round.Goal ? theme.green : theme.red; }
            int dead = Mathf.Max(0, round.AliveAtStart - Person.AliveCount);
            if (liveDead != null)
            {
                string t = dead.ToString();
                if (liveDead.text != t && liveDead.text != "") liveDead.rectTransform.DOPunchScale(Vector3.one * 0.5f, 0.3f, 6, 0.6f);
                liveDead.text = t;
            }
        }

        // ------------------------------------------------------------ phases

        void PhaseChanged(RoundManager.State from, RoundManager.State to)
        {
            switch (to)
            {
                case RoundManager.State.Build:
                    Hide(resultRoot); Hide(gameOverRoot);
                    Slide(liveRoot, liveHome + Vector2.up * 260f, Ease.InBack);
                    Slide(shopRoot, shopHome, Ease.OutBack, 0.15f);
                    Slide(toolPanel, toolHome, Ease.OutBack, 0.2f);
                    Slide(forecastRoot, forecastHome, Ease.OutBack, 0.05f);
                    if (startButton != null)
                    {
                        var r = (RectTransform)startButton.transform;
                        r.localScale = Vector3.zero;
                        r.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetDelay(0.35f);
                    }
                    if ((int)from >= 0) Banner("ROUND " + round.Round, theme.yellow);
                    break;
                case RoundManager.State.Disaster:
                    Hide(resultRoot); Hide(gameOverRoot);   // in case Build was skipped in the same frame
                    Open(Popup.None);
                    Slide(shopRoot, shopHome + Vector2.down * 320f, Ease.InBack);
                    Slide(toolPanel, toolHome + Vector2.right * 200f, Ease.InBack);
                    Slide(forecastRoot, forecastHome + Vector2.up * 420f, Ease.InBack);
                    SetDisaster(liveIcon, liveName, round.Next);
                    if (liveName != null) liveName.text = round.Next != null ? round.Next.displayName.ToUpper() : "";
                    if (liveDetail != null) liveDetail.text = (round.HasSide ? "FROM THE " + Tsunami.SideNames[round.NextSide].ToUpper() + "  -  " : "") + "LEVEL " + round.Round;
                    if (liveDead != null) liveDead.text = "0";
                    Slide(liveRoot, liveHome, Ease.OutBack, 0.25f);
                    Banner(round.Next != null ? round.Next.displayName.ToUpper() + "!" : "", DisasterColor(round.Next), 1.4f);
                    break;
                case RoundManager.State.Settle:
                    Banner("COUNTING", theme.cream, 0.9f);
                    break;
                case RoundManager.State.Result:
                    Slide(liveRoot, liveHome + Vector2.up * 260f, Ease.InBack);
                    ShowResult();
                    break;
                case RoundManager.State.GameOver:
                    Slide(liveRoot, liveHome + Vector2.up * 260f, Ease.InBack);
                    ShowGameOver();
                    break;
            }
        }

        void ShowForecast(bool flip)
        {
            if (forecastRoot == null) return;
            SetDisaster(forecastIcon, forecastName, round.Next);
            if (forecastName != null) forecastName.text = round.Next != null ? round.Next.displayName.ToUpper() : "";
            if (forecastDetail != null)
            {
                string side = round.HasSide ? "FROM THE " + Tsunami.SideNames[round.NextSide].ToUpper() + "  -  " : "";
                forecastDetail.text = side + "LEVEL " + round.Round + Hint(round.Next);
            }
            if (!flip) return;
            forecastRoot.DOKill(true);
            var s = DOTween.Sequence().SetLink(forecastRoot.gameObject);
            s.Append(forecastRoot.DOScaleX(0f, 0.12f).SetEase(Ease.InQuad));
            s.Append(forecastRoot.DOScaleX(1f, 0.25f).SetEase(Ease.OutBack));
            if (forecastIcon != null) s.Join(forecastIcon.rectTransform.DOPunchRotation(new Vector3(0f, 0f, 15f), 0.5f, 6, 0.5f));
        }

        void SetDisaster(Image icon, TextMeshProUGUI label, Disaster d)
        {
            Color c = DisasterColor(d);
            if (icon != null)
            {
                icon.sprite = d is Tsunami ? theme.tsunami : d is Eruption ? theme.volcano : d is Tornado ? theme.tornado : d is LightningStorm ? theme.lightning : theme.quake;
                icon.color = c;
            }
            if (label != null) label.color = c;
        }

        Color DisasterColor(Disaster d) => d is Tsunami ? theme.tsunamiColor : d is Eruption ? theme.volcanoColor :
            d is Tornado ? theme.tornadoColor : d is LightningStorm ? theme.lightningColor : theme.quakeColor;

        /// <summary>One short hint per disaster: what to build against it (shown after the level in the forecast).</summary>
        static string Hint(Disaster d) => d is Tsunami ? "" : d is Tornado ? "  -  HEAVY BRICKS STAY!" :
            d is LightningStorm ? "  -  STEEL DRAWS LIGHTNING!" : "  -  BUILD ROOFS!";

        void ShowResult()
        {
            if (resultRoot == null) return;
            PopIn(resultRoot);
            if (resultTitle != null) resultTitle.text = $"ROUND {round.Round}";
            if (resultSubtitle != null)
            {
                resultSubtitle.text = "SURVIVED!";
                var sr = resultSubtitle.rectTransform;
                sr.DOKill(true);
                sr.localScale = Vector3.zero;
                sr.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetDelay(0.25f);
            }
            var lines = new[] { resultSurvivors, resultDead, resultRubble, resultReward };
            foreach (var l in lines) if (l != null) l.alpha = 0f;
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            counting = true;
            countSeq?.Kill();
            countSeq = DOTween.Sequence().SetLink(resultRoot.gameObject);
            countSeq.AppendInterval(0.35f);
            CountLine(countSeq, resultSurvivors, round.LastSurvivors, v => $"SURVIVORS  <color=#{Hex(theme.green)}>{v}</color> / {round.Goal}");
            CountLine(countSeq, resultDead, round.LastDead, v => $"LOST  <color=#{Hex(theme.red)}>{v}</color>");
            CountLine(countSeq, resultRubble, round.LastRubble, v => $"RUBBLE CLEARED  {v}");
            int extra = Mathf.Max(0, round.LastSurvivors - round.Goal);
            CountLine(countSeq, resultReward, Mathf.RoundToInt(round.LastReward),
                      v => $"REWARD  <color=#{Hex(theme.yellow)}>+${v}</color>\n<size=60%>{round.baseReward:0} + {round.bonusPerExtraSurvivor:0} x {extra} extra</size>");
            countSeq.AppendCallback(() => { if (resultReward != null) resultReward.rectTransform.DOPunchScale(Vector3.one * 0.3f, 0.45f, 6, 0.5f); });
            countSeq.AppendCallback(FinishCount);
        }

        void CountLine(Sequence s, TextMeshProUGUI t, int target, System.Func<int, string> fmt)
        {
            if (t == null) return;
            s.AppendCallback(() => { t.text = fmt(0); t.alpha = 1f; t.rectTransform.localScale = Vector3.one * 1.3f; t.rectTransform.DOScale(1f, 0.25f).SetEase(Ease.OutBack); });
            int shown = 0;
            s.Append(DOTween.To(() => shown, v => { shown = v; t.text = fmt(v); }, target, target == 0 ? 0.1f : countTime).SetEase(Ease.OutQuad));
            s.AppendInterval(0.1f);
        }

        /// <summary>Skips the count (click CONTINUE early) or ends it.</summary>
        void FinishCount()
        {
            if (countSeq != null && countSeq.IsActive() && countSeq.IsPlaying()) { counting = false; countSeq.Complete(true); return; }
            counting = false;
            if (continueButton != null && !continueButton.gameObject.activeSelf)
            {
                continueButton.gameObject.SetActive(true);
                var r = (RectTransform)continueButton.transform;
                r.localScale = Vector3.zero;
                r.DOScale(1f, 0.35f).SetEase(Ease.OutBack);
            }
        }

        void ShowGameOver()
        {
            if (gameOverRoot == null) return;
            gameOverRoot.gameObject.SetActive(true);
            gameOverRoot.DOKill(true);
            Vector2 home = gameOverRoot.anchoredPosition;
            gameOverRoot.localScale = Vector3.one;
            gameOverRoot.anchoredPosition = home + Vector2.up * 900f;
            var s = DOTween.Sequence().SetLink(gameOverRoot.gameObject);
            s.Append(gameOverRoot.DOAnchorPos(home, 0.55f).SetEase(Ease.OutBounce));
            s.Append(gameOverRoot.DOShakeRotation(0.4f, new Vector3(0f, 0f, 6f), 14));
            if (gameOverStats != null)
                gameOverStats.text = $"SURVIVORS  <color=#{Hex(theme.red)}>{round.LastSurvivors}</color> / {round.Goal}\n" +
                                     $"ROUNDS SURVIVED  {round.Round - 1}\nPEOPLE LOST  {Person.Dead}";
            if (gameOverTitle != null) gameOverTitle.rectTransform.DOScale(1.06f, 0.6f).From(Vector3.one).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(gameOverTitle.gameObject);
        }

        // ------------------------------------------------------------ tools

        /// <summary>The chosen tool's button is green; a change gives it a small pop.</summary>
        void UpdateTools()
        {
            if (build.CurrentTool == shownTool) return;
            shownTool = build.CurrentTool;
            SetToolLook(buildToolButton, shownTool == BuildController.Tool.Build);
            SetToolLook(eraseToolButton, shownTool == BuildController.Tool.Erase);
        }

        void SetToolLook(Button b, bool on)
        {
            if (b == null) return;
            var vis = b.GetComponent<BPButton>() != null ? b.GetComponent<BPButton>().visual : (RectTransform)b.transform;
            var img = vis.GetComponent<Image>();
            if (img != null) img.color = on ? toolOnColor : toolOffColor;
            if (on) { vis.DOComplete(); vis.DOPunchScale(Vector3.one * 0.2f, 0.3f, 6, 0.5f); }
        }

        // ------------------------------------------------------------ popups

        static string ShapeLabel(ShapeDef s) =>
            s.kind == ShapeKind.Slope ? "SLOPE" : s.kind == ShapeKind.Round ? "ROUND" :
            s.IsPlate ? (s.StudCount > 8 ? "BIG PLATE" : "PLATE") : $"{s.studs.y}X{s.studs.x}";

        void PopupKeys()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (kb != null && kb.bKey.wasPressedThisFrame) Open(popup == Popup.None ? Popup.Category : Popup.None);
            if (kb != null && kb.escapeKey.wasPressedThisFrame) Open(popup == Popup.Size ? Popup.Category : Popup.None);
            // a click in the world while a popup is open only closes it
            if (popup != Popup.None && mouse != null && mouse.leftButton.wasPressedThisFrame &&
                (UnityEngine.EventSystems.EventSystem.current == null || !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()))
                Open(Popup.None);
        }

        /// <summary>Opens one popup (or none) with a pop animation; the cards inside pop in one after the other.</summary>
        void Open(Popup next)
        {
            if (next == popup) return;
            Close(PopupRoot(popup), next == Popup.Size || popup == Popup.Size);
            popup = next;
            PopupOpen = next != Popup.None;
            var root = PopupRoot(next);
            if (root == null) return;
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            if (BPTooltip.I != null) BPTooltip.I.transform.SetAsLastSibling();
            root.DOKill();
            var cg = root.GetComponent<CanvasGroup>();
            if (cg != null) { cg.DOKill(); cg.alpha = 0f; DOTween.To(() => cg.alpha, a => cg.alpha = a, 1f, 0.15f).SetTarget(cg); }
            root.localScale = Vector3.one * 0.7f;
            root.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
            BPCard[] cards = next == Popup.Category ? materialCards : next == Popup.Size ? shapeCards : null;
            if (cards != null)
            {
                var all = new System.Collections.Generic.List<BPCard>(cards);
                if (next == Popup.Category && personCard != null) all.Add(personCard);
                for (int i = 0; i < all.Count; i++)
                {
                    var v = all[i].transform as RectTransform;
                    v.DOKill(true);
                    v.localScale = Vector3.zero;
                    v.DOScale(1f, 0.28f).SetEase(Ease.OutBack).SetDelay(0.05f + i * 0.04f);
                }
            }
        }

        RectTransform PopupRoot(Popup p) => p == Popup.Category ? categoryPopup : p == Popup.Size ? sizePopup : p == Popup.Help ? helpPopup : null;

        void Close(RectTransform root, bool sideways)
        {
            if (root == null || !root.gameObject.activeSelf) return;
            root.DOKill();
            var cg = root.GetComponent<CanvasGroup>();
            if (cg != null) { cg.DOKill(); DOTween.To(() => cg.alpha, a => cg.alpha = a, 0f, 0.12f).SetTarget(cg); }
            root.DOScale(sideways ? 0.9f : 0.75f, 0.12f).SetEase(Ease.InQuad).OnComplete(() => root.gameObject.SetActive(false));
        }

        /// <summary>The tool chip bounces when the tool changes.</summary>
        void ToolPop()
        {
            if (toolIcon == null) return;
            var r = toolIcon.rectTransform;
            r.DOComplete();
            r.DOPunchScale(Vector3.one * 0.35f, 0.35f, 6, 0.5f);
        }

        // ------------------------------------------------------------ motion helpers

        void Slide(RectTransform rt, Vector2 to, Ease ease, float delay = 0f)
        {
            if (rt == null) return;
            rt.DOKill();
            rt.DOAnchorPos(to, slideTime).SetEase(ease).SetDelay(delay);
        }

        void PopIn(RectTransform rt)
        {
            rt.gameObject.SetActive(true);
            rt.DOKill();
            rt.localScale = Vector3.zero;
            rt.localRotation = Quaternion.Euler(0f, 0f, -6f);
            rt.DOScale(1f, 0.45f).SetEase(Ease.OutBack);
            rt.DOLocalRotate(Vector3.zero, 0.45f).SetEase(Ease.OutBack);
        }

        void Hide(RectTransform rt)
        {
            if (rt == null || !rt.gameObject.activeSelf) return;
            rt.DOKill();
            rt.DOScale(0f, 0.2f).SetEase(Ease.InBack).OnComplete(() => rt.gameObject.SetActive(false));
        }

        static void Pop(RectTransform rt, float amount)
        {
            if (rt == null) return;
            rt.DOComplete();
            rt.DOPunchScale(Vector3.one * amount, 0.3f, 6, 0.6f);
        }

        void FloatText(RectTransform at, string text, Color color)
        {
            if (at == null || theme == null) return;
            var t = new GameObject("Float", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            t.rectTransform.SetParent(transform, false);
            t.font = theme.number;
            if (theme.numberOutline != null) t.fontSharedMaterial = theme.numberOutline;
            t.fontSize = 38f;
            t.color = color;
            t.text = text;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            t.rectTransform.sizeDelta = new Vector2(240f, 60f);
            t.rectTransform.position = at.position + Vector3.down * at.rect.height * 0.8f * at.lossyScale.y;
            var s = DOTween.Sequence().SetLink(t.gameObject);
            s.Append(t.rectTransform.DOMoveY(t.rectTransform.position.y - 60f * at.lossyScale.y, 0.8f).SetEase(Ease.OutQuad));
            s.Join(t.DOFade(0f, 0.8f).SetEase(Ease.InQuad));
            s.OnComplete(() => Destroy(t.gameObject));
        }

        /// <summary>Big centre text: pops in, holds, fades out.</summary>
        public void Banner(string text, Color color, float hold = 0.7f)
        {
            if (banner == null || string.IsNullOrEmpty(text)) return;
            banner.DOKill();
            banner.rectTransform.DOKill();
            banner.text = text;
            banner.color = color;
            banner.alpha = 1f;
            banner.rectTransform.localScale = Vector3.one * 2.2f;
            banner.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -4f);
            var s = DOTween.Sequence().SetLink(banner.gameObject);
            s.Append(banner.rectTransform.DOScale(1f, 0.35f).SetEase(Ease.OutBack));
            s.Join(banner.rectTransform.DOLocalRotate(Vector3.zero, 0.35f).SetEase(Ease.OutBack));
            s.AppendInterval(hold);
            s.Append(banner.DOFade(0f, 0.3f));
            s.Join(banner.rectTransform.DOScale(0.8f, 0.3f).SetEase(Ease.InQuad));
        }

        /// <summary>Money with one decimal when needed: $0.5, $12, $3.8 (bricks cost less than 1 since 2026-11-25).</summary>
        public static string Money(float v) => "$" + (Mathf.Round(v * 10f) / 10f).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);

        static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);
    }
}
