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
        /// <summary>How many buff machines may stand on the map.</summary>
        public int MachineLimit { get; private set; }
        public void RaiseMachineLimit(int amount = 1) { MachineLimit += amount; Changed?.Invoke(); }

        /// <summary>Machines bought in the shop and not placed yet (placed in the Deploy phase).</summary>
        public readonly List<TDConfig.BuffType> MachineStock = new List<TDConfig.BuffType>();
        /// <summary>Random offers for sale in the current shop turn (mixed kinds, see ShopOffer).</summary>
        public readonly List<ShopOffer> Offers = new List<ShopOffer>();
        public int MachinesOwned => MachineStock.Count + BuffMachine.All.Count;
        /// <summary>The machine limit counts only machines placed on the board. The stock has no limit.</summary>
        public bool CanPlaceMachine => BuffMachine.All.Count < MachineLimit;
        /// <summary>Sells a machine that waits in the stock (right-click its button in Deploy). Same price as a placed one.</summary>
        public bool SellStock(int index)
        {
            if (Current != Phase.Deploy || index < 0 || index >= MachineStock.Count) return false;
            AddGold(SellValue(MachineStock[index]));
            MachineStock.RemoveAt(index);
            return true;
        }

        public int SellValue(TDConfig.BuffType buff) => Mathf.Max(1, Mathf.FloorToInt(buff.cost * config.sellBack));

        // ---------- early burst (test option) ----------

        /// <summary>Test switch for config.burstAtBestHand (starts with the config value, the HUD button flips it).</summary>
        public bool BurstAtBestHand { get; set; }

        /// <summary>Hand that really counts: a locked hand bursts as High Card.</summary>
        public HandType Effective(HandType hand) => IsUnlocked(hand) ? hand : HandType.HighCard;

        int bestFrame = -1;
        HandType bestHand;
        /// <summary>
        /// Best hand (after locks) that one enemy can get from the cards on the towers. An enemy holds at
        /// most 5 different cards, so every group of 1-5 tower cards is checked. Worked out once per frame.
        /// </summary>
        public HandType BestReachableHand
        {
            get
            {
                if (bestFrame == Time.frameCount) return bestHand;
                bestFrame = Time.frameCount;
                var cards = new List<Card>();
                foreach (var t in Tower.All) if (t.HasCard && !cards.Contains(t.Card)) cards.Add(t.Card);
                bestHand = HandType.HighCard;
                BestOf(cards, 0, new List<Card>(5));
                return bestHand;
            }
        }

        void BestOf(List<Card> cards, int from, List<Card> pick)
        {
            if (pick.Count > 0)
            {
                var h = Effective(HandEvaluator.Evaluate(pick));
                if (h > bestHand) bestHand = h;
            }
            if (pick.Count == 5) return;
            for (int i = from; i < cards.Count; i++)
            {
                pick.Add(cards[i]);
                BestOf(cards, i + 1, pick);
                pick.RemoveAt(pick.Count - 1);
            }
        }

        /// <summary>A wave may only start when at least one tower holds a card (towers without a card never shoot).</summary>
        public bool CanStartWave => Current == Phase.Deploy && Tower.All.Exists(t => t.HasCard);

        // ---------- shop (step 5) ----------

        public int TowerSlotsBought { get; private set; }
        public int MachineSlotsBought { get; private set; }
        /// <summary>Price of the next reroll. Back to config.rerollCost at every new shop turn.</summary>
        public int RerollCost { get; private set; }

        /// <summary>One-use items held by the player (at most config.itemSlots).</summary>
        public readonly List<TDConfig.ItemType> Items = new List<TDConfig.ItemType>();
        public bool ItemSlotsFull => Items.Count >= config.itemSlots;

        public bool CanBuy(ShopOffer offer) =>
            Current == Phase.Shop && Gold >= offer.Cost(this) && !(offer.kind == OfferKind.Item && ItemSlotsFull);

        public bool BuyOffer(int index)
        {
            if (index < 0 || index >= Offers.Count) return false;
            var offer = Offers[index];
            if (!CanBuy(offer) || !TrySpend(offer.Cost(this))) return false;
            switch (offer.kind)
            {
                case OfferKind.Machine: MachineStock.Add(offer.machine); break;
                case OfferKind.HandLevel: levels[offer.hand] = Level(offer.hand) + 1; break;
                case OfferKind.TowerSlot: TowerSlotsBought++; TowerLimit++; break;
                case OfferKind.MachineSlot: MachineSlotsBought++; MachineLimit++; break;
                case OfferKind.Item: Items.Add(offer.item); break;
                case OfferKind.Unlock: unlocked.Add(offer.hand); break;
            }
            Offers.RemoveAt(index);
            Changed?.Invoke();
            return true;
        }

        public bool Reroll()
        {
            if (Current != Phase.Shop || !TrySpend(RerollCost)) return false;
            RerollCost += config.rerollCostStep;
            RollOffers();
            Changed?.Invoke();
            return true;
        }

        void RollOffers()
        {
            Offers.Clear();
            var locked = new List<HandType>();
            var open = new List<HandType>();
            foreach (var h in config.hands) (IsUnlocked(h.hand) ? open : locked).Add(h.hand);
            // one zone each in the 3D shop: machines, items, then upgrades (hand levels, slots, unlocks by weight)
            for (int i = 0; i < config.shopMachineOffers; i++)
                Offers.Add(new ShopOffer { kind = OfferKind.Machine, machine = config.buffs[UnityEngine.Random.Range(0, config.buffs.Length)] });
            for (int i = 0; i < config.shopItemOffers; i++)
                Offers.Add(new ShopOffer { kind = OfferKind.Item, item = config.items[UnityEngine.Random.Range(0, config.items.Length)] });
            for (int i = 0; i < config.shopUpgradeOffers; i++)
            {
                float unlockW = locked.Count > 0 && !Offers.Exists(o => o.kind == OfferKind.Unlock) ? config.unlockWeight : 0f;
                float[] w = { 0f, open.Count > 0 ? config.handLevelWeight : 0f, 0f, config.towerSlotWeight, config.machineSlotWeight, unlockW };
                float total = 0f; foreach (float x in w) total += x;
                float roll = UnityEngine.Random.value * total;
                int k = 0;
                while (k < w.Length - 1 && roll >= w[k]) { roll -= w[k]; k++; }
                var o = new ShopOffer();
                switch (k)
                {
                    case 0: o.kind = OfferKind.Machine; o.machine = config.buffs[UnityEngine.Random.Range(0, config.buffs.Length)]; break;
                    case 1: o.kind = OfferKind.HandLevel; o.hand = open[UnityEngine.Random.Range(0, open.Count)]; break;
                    case 2: o.kind = OfferKind.Item; o.item = config.items[UnityEngine.Random.Range(0, config.items.Length)]; break;
                    case 3: o.kind = OfferKind.TowerSlot; break;
                    case 4: o.kind = OfferKind.MachineSlot; break;
                    default: o.kind = OfferKind.Unlock; o.hand = locked[UnityEngine.Random.Range(0, locked.Count)]; break;
                }
                Offers.Add(o);
            }
        }

        // ---------- items ----------

        /// <summary>Set while a Hand manual waits for the player to click a hand in the COMBOS panel.</summary>
        public int PickingHandForItem { get; private set; } = -1;

        /// <summary>Why the item can not be used now, or null when it can.</summary>
        public string ItemBlock(int index)
        {
            if (index < 0 || index >= Items.Count) return "empty";
            bool board = Current == Phase.Deploy || Current == Phase.Wave;
            if (!board) return "use on the board";
            switch (Items[index].kind)
            {
                case ItemKind.Slow:
                case ItemKind.Freeze: return Enemy.All.Count > 0 ? null : "no enemies";
                case ItemKind.BurstMarked: return Enemy.All.Exists(e => e.Marks != null && e.Marks.Cards.Count > 0) ? null : "no marked enemy";
                case ItemKind.ExtraDiscard: return Current == Phase.Deploy ? null : "deploy only";
                default: return null;
            }
        }

        public void UseItem(int index)
        {
            if (ItemBlock(index) != null) return;
            var item = Items[index];
            switch (item.kind)
            {
                case ItemKind.Slow:
                    foreach (var e in Enemy.All) e.Slow(item.value, item.seconds);
                    break;
                case ItemKind.Freeze:
                    Enemy front = null;
                    foreach (var e in Enemy.All) if (front == null || e.Progress > front.Progress) front = e;
                    front.Freeze(item.seconds);
                    break;
                case ItemKind.BurstMarked:
                    foreach (var e in Enemy.All.ToArray()) if (e != null && e.Marks != null) e.Marks.BurstNow();
                    break;
                case ItemKind.ExtraDiscard: Hand.AddDiscards(Mathf.RoundToInt(item.value)); break;
                case ItemKind.TowerSlot: TowerLimit++; break;
                case ItemKind.MachineSlot: MachineLimit++; break;
                case ItemKind.HandLevel:
                    PickingHandForItem = PickingHandForItem == index ? -1 : index;   // click again to cancel
                    Changed?.Invoke();
                    return;   // used up only when a hand is picked
            }
            Items.RemoveAt(index);
            PickingHandForItem = -1;
            Changed?.Invoke();
        }

        /// <summary>Finishes a Hand manual: +1 level to the hand clicked in the COMBOS panel.</summary>
        public bool PickHandForItem(HandType hand)
        {
            if (PickingHandForItem < 0 || PickingHandForItem >= Items.Count || !IsUnlocked(hand)) return false;
            Items.RemoveAt(PickingHandForItem);
            PickingHandForItem = -1;
            LevelUp(hand);
            return true;
        }

        public void CancelHandPick() { PickingHandForItem = -1; Changed?.Invoke(); }

        readonly Dictionary<HandType, int> levels = new Dictionary<HandType, int>();
        readonly HashSet<HandType> unlocked = new HashSet<HandType>();

        void Awake()
        {
            Instance = this;
            Lives = config.startLives;
            Gold = config.startGold;
            TowerLimit = config.startTowerLimit;
            MachineLimit = config.startMachineLimit;
            BurstAtBestHand = config.burstAtBestHand;
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
            Hand.Deal(config.HandSize(TowerLimit), config.discardsPerTurn);
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
            if (!CanStartWave) return;
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
            RerollCost = config.rerollCost;
            PickingHandForItem = -1;
            RollOffers();
            Changed?.Invoke();
            BoardTransition.Instance.Sink();
        }

        /// <summary>Enemy health for the current ante and wave (sheet Run Sim, column Normal HP).</summary>
        public float WaveHealth(int ante, int waveInAnte) =>
            config.baseHealth * Mathf.Pow(config.healthGrowthPerAnte, ante - 1) * config.waveHealth[Mathf.Clamp(waveInAnte - 1, 0, config.waveHealth.Length - 1)];
    }
}
