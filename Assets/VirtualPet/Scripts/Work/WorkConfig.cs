using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// Tuning numbers for the electricity job: turbine, bills, lights and prices.
    /// First guesses, to be balanced in WindPet_Balance.xlsx. Rates are per second.
    /// </summary>
    /// <summary>
    /// A potion for sale in the pachinko shop. Duration 0: adds 'amount' to the bar at once.
    /// Duration above 0: every gain of that bar is multiplied by 'amount' for that many seconds.
    /// </summary>
    [System.Serializable]
    public class PotionDef
    {
        public string name;
        [Tooltip("Shelf bottle used as the model and icon.")]
        public string model;
        public PetBar bar;
        public float amount;
        public float duration;
        public float price;
        public bool Timed => duration > 0f;
        public string Effect => Timed ? $"{bar} gains x{amount:0.#} for {duration:0} s" : $"+{amount:0} {bar}";
    }

    [CreateAssetMenu(menuName = "Wind Pet/Work Config", fileName = "WorkConfig")]
    public class WorkConfig : ScriptableObject
    {
        [Header("Electricity")]
        public float startElectricity = 5f;
        [Tooltip("Electricity the room lights use every second.")]
        public float lightsKwhPerSecond = 0.01f;
        [Tooltip("When there is no electricity, the lights cost this much money per second instead.")]
        public float lightsEuroPerSecond = 0.02f;
        [Tooltip("How dark the room gets when the lights go out (0 = black, 1 = no change).")]
        [Range(0f, 1f)] public float darkLevel = 0.25f;

        [Header("Turbine")]
        [Tooltip("Electricity made per second by a normal pet.")]
        public float kwhPerSecond = 0.08f;
        public float tiredOutput = 0.5f;
        public float energeticOutput = 1.25f;
        [Tooltip("Output multiplier of a full storm inside the turbine (x2-x3).")]
        public float stormOutput = 3f;
        [Tooltip("Durability lost per second of normal work (starts at 100).")]
        public float wearPerSecond = 0.05f;
        [Tooltip("A storm inside wears the turbine this many times faster.")]
        public float stormWear = 8f;
        [Tooltip("Price of a new turbine when the old one is broken.")]
        public float turbinePrice = 15f;

        [Header("Turbine levels")]
        public int maxTurbineLevel = 5;
        [Tooltip("Price of going from level 1 to level 2.")]
        public float upgradeBaseCost = 20f;
        [Tooltip("Each upgrade costs this many times the last one (Kingdom Rush towers: about 1.5x).")]
        public float upgradeCostGrowth = 1.5f;
        [Tooltip("Each level makes bills (and potion prices) this many times bigger.")]
        public float levelGrowth = 1.5f;
        [Tooltip("Each level makes this many times more electricity. Higher than levelGrowth, so an upgrade really pays off.")]
        public float outputGrowth = 1.8f;

        [Header("Turbine: what work costs the pet (per second)")]
        public float workEnergy = 0.35f;
        public float workFullness = 0.1f;
        public float workMood = 0.15f;

        [Header("Bills")]
        public Vector2 printEvery = new Vector2(8f, 15f);
        public int maxBillsOnDesk = 6;
        [Tooltip("Presses of the red printer button before it needs to recharge.")]
        public int buttonCharges = 3;
        [Tooltip("Seconds to get one button press back.")]
        public float buttonRecharge = 20f;
        [Tooltip("Chance of a big order (x3 kWh) per printed bill, from turbine level 2.")]
        [Range(0f, 1f)] public float bigOrderChance = 0.15f;
        public Vector2Int kwhRange = new Vector2Int(1, 5);
        [Tooltip("Size of the bill paper on the desk (1 = 0.55 x 0.75 m).")]
        public float billScale = 1.5f;
        [Tooltip("A fair price for 1 kWh. Bills spread around it.")]
        public float fairEuroPerKwh = 1f;
        [Tooltip("How wide the prices spread (log scale). 1 = most bills between 0.37x and 2.7x fair.")]
        public float priceSpread = 1f;
        public Vector2 euroPerKwhLimits = new Vector2(0.07f, 15f);

        [Header("Loan (safety net when the money runs out)")]
        [Tooltip("Loan size at turbine level 1. It grows with the level like the bills.")]
        public float loanBase = 10f;
        [Tooltip("Extra to pay back: 0.1 = 10 %.")]
        public float loanInterest = 0.1f;
        [Tooltip("Part of every paid bill that goes to the loan until it is paid back.")]
        [Range(0f, 1f)] public float loanRepayShare = 0.5f;
        [Tooltip("Seconds before a new loan offer after one was refused.")]
        public float loanCooldown = 20f;

        [Header("Shop: potions")]
        public PotionDef[] potions =
        {
            new PotionDef { name = "Mood Tonic",    model = "Item_Potion2", bar = PetBar.Mood,     amount = 30f, price = 15f },
            new PotionDef { name = "Hearty Brew",   model = "Item_Potion0", bar = PetBar.Fullness, amount = 40f, price = 10f },
            new PotionDef { name = "Energy Elixir", model = "Item_Potion3", bar = PetBar.Energy,   amount = 40f, price = 18f },
            new PotionDef { name = "Joy Draught",   model = "Item_Potion5", bar = PetBar.Mood,     amount = 2f, duration = 90f, price = 30f },
            new PotionDef { name = "Feast Draught", model = "Item_Potion6", bar = PetBar.Fullness, amount = 2f, duration = 90f, price = 30f },
            new PotionDef { name = "Vigor Draught", model = "Item_Potion4", bar = PetBar.Energy,   amount = 2f, duration = 90f, price = 30f },
        };
        [Tooltip("Potion prices are multiplied by the turbine level multiplier, so they keep up with income.")]
        public bool potionPriceFollowsLevel = true;

        /// <summary>Potion price at the current turbine level, in whole euros.</summary>
        public float PotionPrice(PotionDef def) => Mathf.Round(def.price * (potionPriceFollowsLevel ? Turbine.CurrentMultiplier : 1f));

        [Header("Clock")]
        [Tooltip("The game clock starts at this hour on day 1.")]
        public float startHour = 8f;
        [Tooltip("Real seconds per game hour.")]
        public float secondsPerGameHour = 60f;

        [Header("Contracts (bills with a deadline and a fine)")]
        [Tooltip("Chance that a printed bill is a contract.")]
        [Range(0f, 1f)] public float contractChance = 0.25f;
        public int maxOpenContracts = 2;
        [Tooltip("Contracts ask this many times more kWh than a normal bill.")]
        public float contractKwhMultiplier = 2f;
        [Tooltip("Deadline: this many game hours after printing (random between x and y).")]
        public Vector2 contractHours = new Vector2(2f, 5f);
        [Tooltip("Fine per kWh, as a part of the fair price (random between x and y).")]
        public Vector2 finePerKwh = new Vector2(0.5f, 1.5f);
        [Tooltip("After this many fines, the next fine takes a big part of your money.")]
        public int strikesBeforeBigFine = 3;
        [Range(0f, 1f)] public float bigFineShare = 0.8f;

        [Header("Stamp")]
        [Tooltip("Stamps per press on the ink pad.")]
        public int inkPerPad = 5;
    }
}
