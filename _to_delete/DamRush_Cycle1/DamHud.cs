using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DamRush
{
    /// <summary>Plain HUD for the prototype: phase, timer, budget, village health, part palette.</summary>
    public class DamHud : MonoBehaviour
    {
        public DamGame game;
        public BuildTool tool;
        public SandboxConfig config;

        public TMP_Text phaseText, budgetText, villageText, infoText, hintText, statsText;
        public Image villageFill;
        public Button floodButton;
        public Button[] shapeButtons, materialButtons;
        public GameObject gameOverPanel;
        public TMP_Text gameOverText;
        public Button restartButton;

        static readonly Color Selected = new Color(1f, 0.85f, 0.3f), Normal = new Color(0.9f, 0.9f, 0.9f);

        void Start()
        {
            for (int i = 0; i < shapeButtons.Length; i++)
            {
                int k = i;
                shapeButtons[i].onClick.AddListener(() => tool.SelectShape(k));
                shapeButtons[i].GetComponentInChildren<TMP_Text>().text = (i + 1) + "  " + tool.shapes[i].displayName;
            }
            for (int i = 0; i < materialButtons.Length; i++)
            {
                int k = i;
                materialButtons[i].onClick.AddListener(() => tool.SelectMaterial(k));
                materialButtons[i].GetComponentInChildren<TMP_Text>().text = (i + 4) + "  " + tool.materials[i].displayName;
            }
            floodButton.onClick.AddListener(game.StartFlood);
            restartButton.onClick.AddListener(game.Restart);
            gameOverPanel.SetActive(false);
            hintText.text = "LMB place   RMB remove   Q/E turn   R stand up   G snap   WASD move   MMB rotate   Wheel zoom";
        }

        void Update()
        {
            switch (game.Phase)
            {
                case Phase.Build:
                    phaseText.text = $"WAVE {game.Wave}  -  BUILD  {Mathf.CeilToInt(game.TimeLeft)}s\n<size=70%>Next flood peak: {game.PeakInflow(game.Wave):0.#} m3/s</size>";
                    break;
                case Phase.Flood:
                    phaseText.text = $"WAVE {game.Wave}  -  FLOOD!  {Mathf.CeilToInt(game.TimeLeft)}s\n<size=70%>Inflow: {config.baseInflow + game.water.extraInflow:0.0} m3/s</size>";
                    break;
                default:
                    phaseText.text = "VILLAGE FLOODED";
                    break;
            }
            floodButton.gameObject.SetActive(game.Phase == Phase.Build);
            budgetText.text = $"Budget: {game.Budget}";

            float hp = game.VillageHealth / config.villageMaxHealth;
            villageFill.rectTransform.localScale = new Vector3(Mathf.Clamp01(hp), 1f, 1f);
            villageFill.color = Color.Lerp(new Color(0.9f, 0.25f, 0.2f), new Color(0.3f, 0.8f, 0.35f), hp);
            villageText.text = $"Village {Mathf.CeilToInt(game.VillageHealth)}   water {game.VillageDepth:0.00} m";

            var m = tool.Mat; var s = tool.Shape;
            string rules = "";
            if (m.erodes) rules += "  washes away";
            if (m.gluesToGround) rules += "  sticks to ground + glue";
            string floats = m.density < 1000f ? "floats" : "sinks";
            infoText.text = $"<b>{m.displayName} {s.displayName}</b>   cost {tool.CurrentCost}   mass {m.density * s.Volume:0} kg   {floats}{rules}" +
                            (tool.GhostValid || string.IsNullOrEmpty(tool.Problem) ? "" : $"\n<color=#ff8070>{tool.Problem}</color>") +
                            $"\nSnap: {(tool.Snap ? "ON" : "OFF")}";
            statsText.text = $"Joints snapped: {Breakable.JointsSnapped}   Parts broken: {Breakable.BrokenCount}";

            for (int i = 0; i < shapeButtons.Length; i++) shapeButtons[i].image.color = i == tool.ShapeIndex ? Selected : Normal;
            for (int i = 0; i < materialButtons.Length; i++)
                materialButtons[i].image.color = i == tool.MaterialIndex ? Selected : Color.Lerp(tool.materials[i].color, Color.white, 0.35f);

            if (game.Phase == Phase.GameOver && !gameOverPanel.activeSelf)
            {
                gameOverPanel.SetActive(true);
                gameOverText.text = $"The village is flooded.\nYou held out until wave {game.Wave}.\nJoints snapped: {Breakable.JointsSnapped}, parts broken: {Breakable.BrokenCount}";
            }
        }
    }
}
