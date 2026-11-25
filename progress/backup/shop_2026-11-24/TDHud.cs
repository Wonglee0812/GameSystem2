using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PokerTD.UI
{
    /// <summary>
    /// Graybox HUD: lives, gold, ante and wave, tower and buff machine buttons, start wave,
    /// game over, and the calculation panel that follows the pointer over a tower.
    /// The machine buttons and the panel are made at runtime, so the scene does not need a rebuild.
    /// </summary>
    public class TDHud : MonoBehaviour
    {
        public TMP_Text status;
        public Button[] towerButtons;
        public Button startWave;
        public GameObject gameOver;
        public TMP_Text gameOverText;

        readonly List<Button> machineButtons = new List<Button>();
        Image[] zones;
        float[] zoneAlpha;
        bool calcShown;
        readonly List<Button> itemButtons = new List<Button>();
        RectTransform calcPanel;
        TextMeshProUGUI calcText;

        void Start()
        {
            var game = TDGame.Instance;
            int towers = game.config.towers.Length;
            // old tower buttons: optional (the new HUD uses BuildCardBar; Wong may delete them)
            for (int i = 0; towerButtons != null && i < towerButtons.Length; i++)
            {
                if (towerButtons[i] == null) continue;
                int index = i;
                towerButtons[i].onClick.AddListener(() => BuildManager.Instance.Select(index));
                var t = game.config.towers[i];
                towerButtons[i].GetComponentInChildren<TMP_Text>().text = $"{i + 1}  {t.name}\n<size=70%>{t.cost} gold</size>";
            }
            // buttons for machines bought in the shop and not placed yet: copies of the first
            // tower button in a second column (one per machine slot, hidden when empty)
            // (old machine buttons are not made any more: BuildCardBar shows bought machines)
            BuildCalcPanel();
            gameObject.AddComponent<ComboView>();
            startWave.onClick.AddListener(game.StartWave);
            // the "burst at best hand" test button is gone (Wong, 2026-11-24); early burst stays on (config)
            // (old item buttons are not made any more: ItemTable shows the items on the table)
            gameOver.SetActive(false);
            // new layout: 3D boards, build cards, item cards (the old status line and buttons are hidden)
            if (status != null) status.gameObject.SetActive(false);
            if (GetComponent<HudLayout>() == null) gameObject.AddComponent<HudLayout>();
        }

        void AddMachineButton(int i)
        {
            var game = TDGame.Instance;
            var first = (RectTransform)towerButtons[0].transform;
            int index = game.config.towers.Length + i;
            var b = Instantiate(towerButtons[0], first.parent);
            b.name = "Machine" + i;
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() => BuildManager.Instance.Select(index));
            var rt = (RectTransform)b.transform;
            rt.anchoredPosition = new Vector2(first.anchoredPosition.x + first.sizeDelta.x + 20f, 40f + i * 105f);
            machineButtons.Add(b);
            b.gameObject.AddComponent<RightClickRelay>().onRightClick = () => TDGame.Instance.SellStock(i);
        }

        /// <summary>Item slots in a row under the status line (top left). Click to use an item.</summary>
        void AddItemButton(int i)
        {
            var b = Instantiate(towerButtons[0], towerButtons[0].transform.parent);
            b.name = "Item" + i;
            b.onClick.RemoveAllListeners();
            int index = i;
            b.onClick.AddListener(() =>
            {
                TDGame.Instance.UseItem(index);
                if (UnityEngine.EventSystems.EventSystem.current != null) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            });
            var rt = (RectTransform)b.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(200f, 80f);
            rt.anchoredPosition = new Vector2(40f + i * 215f, -150f);
            var t = b.GetComponentInChildren<TMP_Text>();
            t.fontSize = 20f;
            ((RectTransform)t.transform).sizeDelta = rt.sizeDelta - new Vector2(10f, 6f);
            itemButtons.Add(b);
        }

        void UpdateItems()
        {
            var game = TDGame.Instance;
            bool board = game.Current == TDGame.Phase.Deploy || game.Current == TDGame.Phase.Wave;   // hidden in the shop (the shop title shows the item count)
            while (itemButtons.Count < game.config.itemSlots) AddItemButton(itemButtons.Count);
            for (int i = 0; i < itemButtons.Count; i++)
            {
                var b = itemButtons[i];
                b.gameObject.SetActive(false);   // replaced by ItemCardBar
                continue;
                var t = b.GetComponentInChildren<TMP_Text>();
                if (i >= game.Items.Count)
                {
                    b.interactable = false;
                    b.image.color = new Color(1f, 1f, 1f, 0.35f);
                    t.text = "<color=#888888>empty item slot</color>";
                    continue;
                }
                var item = game.Items[i];
                string block = game.ItemBlock(i);
                bool picking = game.PickingHandForItem == i;
                b.interactable = block == null;
                b.image.color = picking ? new Color(1f, 0.85f, 0.4f) : new Color(1f, 0.95f, 0.8f);
                string sub = picking ? "click a hand in COMBOS (Esc cancels)" : block != null ? $"<color=#C03020>{block}</color>" : item.description;
                t.text = $"<b>{item.name}</b>\n<size=75%>{sub}</size>";
            }
            var kb = Keyboard.current;
            if (game.PickingHandForItem >= 0 && kb != null && kb.escapeKey.wasPressedThisFrame) game.CancelHandPick();
        }

        void BuildCalcPanel()
        {
            var go = new GameObject("CalcPanel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            calcPanel = (RectTransform)go.transform;
            calcPanel.anchorMin = calcPanel.anchorMax = Vector2.zero;
            calcPanel.pivot = new Vector2(0f, 1f);
            go.GetComponent<Image>().color = new Color(0.08f, 0.06f, 0.05f, 0.88f);
            go.GetComponent<Image>().raycastTarget = false;
            var tgo = new GameObject("Text", typeof(RectTransform));
            tgo.transform.SetParent(go.transform, false);
            var trt = (RectTransform)tgo.transform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(18f, 14f); trt.offsetMax = new Vector2(-18f, -14f);
            calcText = tgo.AddComponent<TextMeshProUGUI>();
            calcText.fontSize = 24f;
            calcText.color = Color.white;
            calcText.alignment = TextAlignmentOptions.TopLeft;
            calcText.textWrappingMode = TextWrappingModes.NoWrap;
            calcText.raycastTarget = false;
            go.SetActive(false);
        }

        void Update()
        {
            var game = TDGame.Instance;
            int towers = game.config.towers.Length;
            if (status != null && status.gameObject.activeSelf) status.text = $"Lives  <b>{game.Lives}</b>     Gold  <b>{game.Gold}</b>     Ante <b>{game.Ante}</b>  ·  Wave {game.WaveInAnte}/{game.config.wavesPerAnte}" +
                          $"     Towers <b>{Tower.All.Count}/{game.TowerLimit}</b>     Machines <b>{BuffMachine.All.Count}/{game.MachineLimit}</b>{(game.MachineStock.Count > 0 ? $" +{game.MachineStock.Count} in stock" : "")}" +
                          $"\n<size=70%>{(game.Current == TDGame.Phase.Deploy && !game.CanStartWave ? "<color=#FF8060>Drag a card onto at least one tower to start the wave.</color> " : "")}{(game.Current == TDGame.Phase.Deploy ? "Build towers, place bought machines, drag a card onto each tower, then Start Wave (Space). Ring 1 is applied first. Hover a tower to see its burst. Right-click a tower or machine to sell it." : game.Current == TDGame.Phase.Wave ? "Wave in progress" : game.Current == TDGame.Phase.Shop ? "Shop turn" : "")}</size>";
            bool deploy = game.Current == TDGame.Phase.Deploy;
            for (int i = 0; towerButtons != null && i < towerButtons.Length; i++)
            {
                if (towerButtons[i] == null) continue;
                towerButtons[i].gameObject.SetActive(false);   // replaced by BuildCardBar
                towerButtons[i].image.color = BuildManager.Instance.Selected == i ? new Color(1f, 0.85f, 0.4f) : Color.white;
            }
            for (int i = 0; i < machineButtons.Count; i++)
            {
                bool has = false;   // replaced by BuildCardBar
                machineButtons[i].gameObject.SetActive(has);
                if (!has) continue;
                var buff = game.MachineStock[i];
                machineButtons[i].GetComponentInChildren<TMP_Text>().text = $"{towers + i + 1}  {buff.Short}\n<size=70%>{(game.CanPlaceMachine ? "place (free)" : "<color=#FF6040>limit full</color>")} · right-click sells {game.SellValue(buff)}g</size>";
                machineButtons[i].image.color = BuildManager.Instance.Selected == towers + i ? new Color(1f, 0.85f, 0.4f) : Color.Lerp(buff.color, Color.white, 0.6f);
            }
            UpdateCalcPanel();
            startWave.interactable = game.CanStartWave;
            bool fight = deploy || game.Current == TDGame.Phase.Wave;
            FadeZones(deploy);
            startWave.gameObject.SetActive(deploy || game.Current == TDGame.Phase.Wave);
            var kb = Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame) game.StartWave();
            if (game.Current == TDGame.Phase.GameOver && !gameOver.activeSelf)
            {
                gameOver.SetActive(true);
                gameOverText.text = $"GAME OVER\n<size=50%>Reached ante {game.Ante}, wave {game.WaveInAnte}\nPress R to restart</size>";
            }
            if (gameOver.activeSelf && kb != null && kb.rKey.wasPressedThisFrame)
            {
                Enemy.All.Clear(); Tower.All.Clear(); BuffMachine.ResetAll();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex >= 0 ? SceneManager.GetActiveScene().name : "PokerTD");
            }
        }

        /// <summary>Hovering a tower: its burst step by step (example hand: Pair), next to the pointer.</summary>
        void UpdateCalcPanel()
        {
            var tower = BuildManager.Instance.HoveredTower;
            var game = TDGame.Instance;
            bool show = tower != null && (game.Current == TDGame.Phase.Deploy || game.Current == TDGame.Phase.Wave) && Pointer.current != null;
            if (calcPanel.gameObject.activeSelf) calcPanel.gameObject.SetActive(false);   // old panel: replaced by the shared tooltip
            if (!show) { if (calcShown) { UiKit.HideTip(); calcShown = false; } return; }
            // the same tooltip as the build cards (UiKit), Wong 2026-11-24
            UiKit.ShowTip(DamageCalc.Explain(tower, HandType.Pair), Pointer.current.position.ReadValue());
            calcShown = true;
        }

        /// <summary>The dark zones behind the tower cards and the hand fade out when the cards are hidden (wave, shop).</summary>
        void FadeZones(bool show)
        {
            if (zones == null)
            {
                var list = new System.Collections.Generic.List<Image>();
                foreach (var n in new[] { "TowerArea", "CardArea" })
                {
                    var t = transform.Find(n);
                    var img = t != null ? t.GetComponent<Image>() : null;
                    if (img != null) list.Add(img);
                }
                zones = list.ToArray();
                zoneAlpha = new float[zones.Length];
                for (int i = 0; i < zones.Length; i++) zoneAlpha[i] = zones[i].color.a;
            }
            for (int i = 0; i < zones.Length; i++)
            {
                var c = zones[i].color;
                float goal = show ? zoneAlpha[i] : 0f;
                if (Mathf.Approximately(c.a, goal)) continue;
                c.a = Mathf.MoveTowards(c.a, goal, Time.unscaledDeltaTime * 3f * Mathf.Max(0.05f, zoneAlpha[i]));
                zones[i].color = c;
            }
        }
    }

    /// <summary>Calls onRightClick when the UI element is right-clicked (used to sell stock machines).</summary>
    public class RightClickRelay : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler
    {
        public System.Action onRightClick;

        public void OnPointerClick(UnityEngine.EventSystems.PointerEventData e)
        {
            if (e.button == UnityEngine.EventSystems.PointerEventData.InputButton.Right) onRightClick?.Invoke();
        }
    }
}
