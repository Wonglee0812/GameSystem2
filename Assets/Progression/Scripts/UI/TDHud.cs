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
        RectTransform calcPanel;
        TextMeshProUGUI calcText;

        void Start()
        {
            var game = TDGame.Instance;
            int towers = game.config.towers.Length;
            for (int i = 0; i < towerButtons.Length; i++)
            {
                int index = i;
                towerButtons[i].onClick.AddListener(() => BuildManager.Instance.Select(index));
                var t = game.config.towers[i];
                towerButtons[i].GetComponentInChildren<TMP_Text>().text = $"{i + 1}  {t.name}\n<size=70%>{t.cost} gold</size>";
            }
            // buttons for machines bought in the shop and not placed yet: copies of the first
            // tower button in a second column (one per machine slot, hidden when empty)
            var first = (RectTransform)towerButtons[0].transform;
            for (int i = 0; i < game.MachineLimit; i++) AddMachineButton(i);
            BuildCalcPanel();
            gameObject.AddComponent<ComboView>();
            startWave.onClick.AddListener(game.StartWave);
            gameOver.SetActive(false);
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
            status.text = $"Lives  <b>{game.Lives}</b>     Gold  <b>{game.Gold}</b>     Ante <b>{game.Ante}</b>  ·  Wave {game.WaveInAnte}/{game.config.wavesPerAnte}" +
                          $"     Towers <b>{Tower.All.Count}/{game.TowerLimit}</b>     Machines <b>{game.MachinesOwned}/{game.MachineLimit}</b>" +
                          $"\n<size=70%>{(game.Current == TDGame.Phase.Deploy ? "Build towers, place bought machines, drag a card onto each tower, then Start Wave (Space). Ring 1 is applied first. Hover a tower to see its burst. Right-click a tower or machine to sell it." : game.Current == TDGame.Phase.Wave ? "Wave in progress" : game.Current == TDGame.Phase.Shop ? "Shop turn" : "")}</size>";
            bool deploy = game.Current == TDGame.Phase.Deploy;
            for (int i = 0; i < towerButtons.Length; i++)
            {
                towerButtons[i].gameObject.SetActive(deploy);
                towerButtons[i].image.color = BuildManager.Instance.Selected == i ? new Color(1f, 0.85f, 0.4f) : Color.white;
            }
            while (machineButtons.Count < game.MachineLimit) AddMachineButton(machineButtons.Count);
            for (int i = 0; i < machineButtons.Count; i++)
            {
                bool has = deploy && i < game.MachineStock.Count;
                machineButtons[i].gameObject.SetActive(has);
                if (!has) continue;
                var buff = game.MachineStock[i];
                machineButtons[i].GetComponentInChildren<TMP_Text>().text = $"{towers + i + 1}  {buff.Short}\n<size=70%>place machine (free)</size>";
                machineButtons[i].image.color = BuildManager.Instance.Selected == towers + i ? new Color(1f, 0.85f, 0.4f) : Color.Lerp(buff.color, Color.white, 0.6f);
            }
            UpdateCalcPanel();
            startWave.interactable = deploy;
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
            if (calcPanel.gameObject.activeSelf != show) calcPanel.gameObject.SetActive(show);
            if (!show) return;
            calcText.text = DamageCalc.Explain(tower, HandType.Pair);
            Vector2 size = calcText.GetPreferredValues(calcText.text);
            calcPanel.sizeDelta = new Vector2(size.x + 36f, size.y + 28f);
            var canvas = GetComponent<Canvas>();
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            Vector2 mouse = Pointer.current.position.ReadValue();
            // keep the panel on screen
            float w = calcPanel.sizeDelta.x * scale, h = calcPanel.sizeDelta.y * scale;
            float x = mouse.x + 24f; if (x + w > Screen.width) x = mouse.x - 24f - w;
            float y = mouse.y - 24f; if (y - h < 0f) y = h;
            calcPanel.position = new Vector2(x, y);
        }
    }
}
