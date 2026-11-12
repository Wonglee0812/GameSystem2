using System;
using System.Collections.Generic;
using UnityEngine;

namespace PokerTD
{
    /// <summary>
    /// The run: lives, gold, ante and wave, hand levels and unlocks, and the phase.
    /// Two turns, like Unrailed 2: after a wave the board sinks and the shop comes up
    /// (Shop); Continue brings the board back, the cards are dealt and the player builds
    /// and arranges towers (Deploy); Start Wave sends the enemies (Wave). The run is
    /// endless and ends when the base has no lives left.
    /// </summary>
    public class TDGame : MonoBehaviour
    {
        public enum Phase { Shop, Deploy, Wave, GameOver }

        public static TDGame Instance { get; private set; }
        public TDConfig config;

        public int Lives { get; private set; }
        public int Gold { get; private set; }
        /// <summary>Waves finished or started so far, 1-based for the current one.</summary>
        public int WaveNumber { get; private set; } = 1;
        public int Ante => (WaveNumber - 1) / config.wavesPerAnte + 1;
        public int WaveInAnte => (WaveNumber - 1) % config.wavesPerAnte + 1;
        public Phase Current { get; private set; } = Phase.Deploy;

        public event Action Changed;
        public CardHand Hand { get; } = new CardHand();
        /// <summary>How many towers may stand on the map.</summary>
        public int TowerLimit { get; private set; }
        public void RaiseTowerLimit(int amount = 1) { TowerLimit += amount; Changed?.Invoke(); }

        readonly Dictionary<HandType, int> levels = new Dictionary<HandType, int>();
        readonly HashSet<HandType> unlocked = new HashSet<HandType>();

        void Awake()
        {
            Instance = this;
            Lives = config.startLives;
            Gold = config.startGold;
            TowerLimit = config.startTowerLimit;
            foreach (var h in config.hands) if (!h.startsLocked) unlocked.Add(h.hand);
        }

        // ---------- hands ----------

        public int Level(HandType hand) => levels.TryGetValue(hand, out int l) ? l : 1;
        public void LevelUp(HandType hand) { levels[hand] = Level(hand) + 1; Changed?.Invoke(); }
        public bool IsUnlocked(HandType hand) => !config.useLocks || unlocked.Contains(hand);
        public void Unlock(HandType hand) { unlocked.Add(hand); Changed?.Invoke(); }

        // ---------- gold and lives ----------

        public void AddGold(int amount) { Gold += amount; Changed?.Invoke(); }

        public bool TrySpend(int amount)
        {
            if (Gold < amount) return false;
            Gold -= amount;
            Changed?.Invoke();
            return true;
        }

        public void LoseLives(int amount)
        {
            if (Current == Phase.GameOver) return;
            Lives = Mathf.Max(0, Lives - amount);
            if (Lives == 0) Current = Phase.GameOver;
            Changed?.Invoke();
        }

        // ---------- phases ----------

        void Start() => BeginDeploy();

        void BeginDeploy()
        {
            Current = Phase.Deploy;
            Hand.Deal(Tower.All.Count, config.discardsPerTurn);
            Changed?.Invoke();
        }

        /// <summary>Shop -> board rises -> new deal.</summary>
        public void LeaveShop()
        {
            if (Current != Phase.Shop) return;
            BoardTransition.Instance.Rise(BeginDeploy);
        }

        public void StartWave()
        {
            if (Current != Phase.Deploy) return;
            Current = Phase.Wave;
            WaveSpawner.Instance.Begin(Ante, WaveInAnte);
            Changed?.Invoke();
        }

        /// <summary>Called by the spawner when the last enemy of the wave is gone.</summary>
        public void WaveCleared()
        {
            if (Current != Phase.Wave) return;
            AddGold(config.waveGold + config.waveGoldPerAnte * (Ante - 1));
            WaveNumber++;
            Current = Phase.Shop;
            foreach (var t in Tower.All) t.ClearCard();
            Changed?.Invoke();
            BoardTransition.Instance.Sink();
        }

        /// <summary>Enemy health for the current ante and wave (sheet Run Sim, column Normal HP).</summary>
        public float WaveHealth(int ante, int waveInAnte) =>
            config.baseHealth * Mathf.Pow(config.healthGrowthPerAnte, ante - 1) * config.waveHealth[Mathf.Clamp(waveInAnte - 1, 0, config.waveHealth.Length - 1)];
    }
}
