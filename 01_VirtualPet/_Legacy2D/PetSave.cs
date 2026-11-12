using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// Saves the pet between sessions with PlayerPrefs (works in WebGL), so the
    /// Bond keeps growing over several play sessions. Saves every few seconds.
    /// </summary>
    public class PetSave : MonoBehaviour
    {
        public PetStats stats;
        public PetStateMachine fsm;
        public Wallet wallet;
        public float saveInterval = 5f;

        const string Key = "windpet_save_v1";
        float timer;

        [System.Serializable]
        class SaveData
        {
            public float hunger, temperature, energy, fun, bond;
            public int money;
            public int state;
        }

        void Start() => Load();

        void Update()
        {
            timer += Time.deltaTime;
            if (timer < saveInterval) return;
            timer = 0f;
            Save();
        }

        void OnApplicationQuit() => Save();

        public void Save()
        {
            var data = new SaveData
            {
                hunger = stats.Hunger, temperature = stats.Temperature, energy = stats.Energy,
                fun = stats.Fun, bond = stats.Bond, money = wallet.Money, state = (int)fsm.State
            };
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        void Load()
        {
            if (!PlayerPrefs.HasKey(Key)) return;
            var data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(Key));
            stats.SetAll(data.hunger, data.temperature, data.energy, data.fun, data.bond);
            wallet.SetMoney(data.money);
            // Activities are short, so only restore Sleeping; other states are re-checked by the FSM.
            if ((PetState)data.state == PetState.Sleeping) fsm.ForceState(PetState.Sleeping);
        }

        /// <summary>Debug: start a fresh pet.</summary>
        public void ResetSave()
        {
            PlayerPrefs.DeleteKey(Key);
            stats.ResetToStart();
            wallet.SetMoney(stats.config.startMoney);
            fsm.ForceState(PetState.Idle);
        }
    }
}
