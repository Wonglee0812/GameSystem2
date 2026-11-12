using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PokerTD.UI
{
    /// <summary>Graybox HUD: lives, gold, ante and wave, tower buttons, start wave, game over.</summary>
    public class TDHud : MonoBehaviour
    {
        public TMP_Text status;
        public Button[] towerButtons;
        public Button startWave;
        public GameObject gameOver;
        public TMP_Text gameOverText;

        void Start()
        {
            var game = TDGame.Instance;
            for (int i = 0; i < towerButtons.Length; i++)
            {
                int index = i;
                towerButtons[i].onClick.AddListener(() => BuildManager.Instance.Select(index));
                var t = game.config.towers[i];
                towerButtons[i].GetComponentInChildren<TMP_Text>().text = $"{i + 1}  {t.name}\n<size=70%>{t.cost} gold</size>";
            }
            startWave.onClick.AddListener(game.StartWave);
            gameOver.SetActive(false);
        }

        void Update()
        {
            var game = TDGame.Instance;
            status.text = $"Lives  <b>{game.Lives}</b>     Gold  <b>{game.Gold}</b>     Ante <b>{game.Ante}</b>  ·  Wave {game.WaveInAnte}/{game.config.wavesPerAnte}" +
                          $"     Towers <b>{Tower.All.Count}/{game.TowerLimit}</b>" +
                          $"\n<size=70%>{(game.Current == TDGame.Phase.Deploy ? "Build towers, drag a card onto each tower, then Start Wave (Space). Click a tower to take its card back." : game.Current == TDGame.Phase.Wave ? "Wave in progress" : game.Current == TDGame.Phase.Shop ? "Shop turn" : "")}</size>";
            for (int i = 0; i < towerButtons.Length; i++)
            {
                towerButtons[i].gameObject.SetActive(game.Current == TDGame.Phase.Deploy);
                towerButtons[i].image.color = BuildManager.Instance.Selected == i ? new Color(1f, 0.85f, 0.4f) : Color.white;
            }
            startWave.interactable = game.Current == TDGame.Phase.Deploy;
            startWave.gameObject.SetActive(game.Current == TDGame.Phase.Deploy || game.Current == TDGame.Phase.Wave);
            var kb = Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame) game.StartWave();
            if (game.Current == TDGame.Phase.GameOver && !gameOver.activeSelf)
            {
                gameOver.SetActive(true);
                gameOverText.text = $"GAME OVER\n<size=50%>Reached ante {game.Ante}, wave {game.WaveInAnte}\nPress R to restart</size>";
            }
            if (gameOver.activeSelf && kb != null && kb.rKey.wasPressedThisFrame)
            {
                Enemy.All.Clear(); Tower.All.Clear();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex >= 0 ? SceneManager.GetActiveScene().name : "PokerTD");
            }
        }
    }
}
