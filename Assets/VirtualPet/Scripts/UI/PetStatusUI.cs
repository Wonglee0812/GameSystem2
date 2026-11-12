using TMPro;
using UnityEngine;

namespace WindPet.UI
{
    /// <summary>
    /// Connects the pet's four bars to the status icons and the money counter.
    /// </summary>
    public class PetStatusUI : MonoBehaviour
    {
        public PetStats stats;
        public PetStateMachine fsm;
        public Wallet wallet;

        public StatusIcon hungerIcon;
        public StatusIcon energyIcon;
        [Tooltip("Mood icon. Fill shows the Mood bar, the face shows the mood band.")]
        public StatusIcon funIcon;
        public UnityEngine.UI.Image moodFace;
        public Sprite happyFace;
        public Sprite neutralFace;
        public Sprite sadFace;
        [Tooltip("Optional. Falls back to the sad face when empty.")]
        public Sprite angryFace;
        public TemperatureIcon temperatureIcon;
        public TMP_Text moneyText;
        public string currencySymbol = "\u20AC";

        void Update()
        {
            if (stats == null) return;

            // a full icon always means good: the hunger icon shows fullness
            if (hungerIcon != null) hungerIcon.SetValue(stats.Fullness / 100f);
            if (energyIcon != null) energyIcon.SetValue(stats.Energy / 100f);
            if (funIcon != null) funIcon.SetValue(stats.Mood / 100f);
            if (temperatureIcon != null) temperatureIcon.SetValue(stats.Temperature / 100f);
            if (moneyText != null && wallet != null) {
                moneyText.textWrappingMode = TMPro.TextWrappingModes.NoWrap;   // "€ 20.00" stays on one line
                moneyText.text = currencySymbol + " " + wallet.Money.ToString("0.00");
                var bank = LoanOffice.Instance;
                if (bank != null && bank.Debt > 0f) moneyText.text += "\n<size=55%><color=#E07A5F>loan -" + bank.Debt.ToString("0.00") + "</color></size>";
            }
            if (moodFace != null && fsm != null) moodFace.sprite = FaceFor(fsm.Mood);
        }

        Sprite FaceFor(MoodState mood)
        {
            switch (mood)
            {
                case MoodState.Happy: return happyFace;
                case MoodState.Sad: return sadFace;
                case MoodState.Angry: return angryFace != null ? angryFace : sadFace;
                default: return neutralFace;
            }
        }
    }
}
