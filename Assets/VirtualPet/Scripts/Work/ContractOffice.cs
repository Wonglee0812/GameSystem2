using System.Collections.Generic;
using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// Contracts are bills with a deadline on the game clock. The player must stamp
    /// and post them in time (with enough electricity), or pay a fine: posting one
    /// unstamped, or letting the deadline pass, both cost the fine. Sometimes the fine
    /// is cheaper than the electricity, so taking it can be the smart choice, but fines
    /// add up: after config.strikesBeforeBigFine fines, the next one takes a big part
    /// of the money (config.bigFineShare) and the count starts again.
    /// </summary>
    public class ContractOffice : MonoBehaviour
    {
        public static ContractOffice Instance { get; private set; }

        public WorkConfig config;
        public Wallet wallet;

        /// <summary>Fines since the last big fine.</summary>
        public int Strikes { get; private set; }
        public int OpenCount { get { open.RemoveAll(b => b == null); return open.Count; } }

        readonly List<Bill> open = new List<Bill>();

        void Awake() => Instance = this;

        public void Register(Bill contract) => open.Add(contract);

        void Update()
        {
            var clock = GameClock.Instance;
            if (clock == null) return;
            for (int i = open.Count - 1; i >= 0; i--)
            {
                var bill = open[i];
                if (bill == null) { open.RemoveAt(i); continue; }
                if (clock.Now < bill.DueMinute) continue;
                open.RemoveAt(i);
                Fine(bill, "Contract missed");
                if (ItemGrabber.Instance != null && ItemGrabber.Instance.Held == bill.GetComponent<Rigidbody>()) ItemGrabber.Instance.ForceRelease();
                Destroy(bill.gameObject);
            }
        }

        /// <summary>The fine the next missed contract would cost.</summary>
        public float NextFine(Bill bill) =>
            Strikes >= config.strikesBeforeBigFine ? Mathf.Max(bill.Fine, Mathf.Round(wallet.Money * config.bigFineShare * 100f) / 100f) : bill.Fine;

        /// <summary>Charges the fine for this contract (as much as the player has).</summary>
        public void Fine(Bill bill, string reason)
        {
            open.Remove(bill);
            float fine = NextFine(bill);
            bool big = Strikes >= config.strikesBeforeBigFine;
            Strikes = big ? 0 : Strikes + 1;
            float paid = Mathf.Min(fine, wallet.Money);
            wallet.SetMoney(Mathf.Round((wallet.Money - paid) * 100f) / 100f);
            FloatingText.Show(reason + "\n<size=80%>fine -€" + paid.ToString("0.00") + (big ? "  (big fine!)" : "") + "</size>",
                              bill.transform.position + Vector3.up * 0.8f, new Color(1f, 0.4f, 0.3f));
        }

        /// <summary>A contract was delivered in time.</summary>
        public void Complete(Bill bill) => open.Remove(bill);
    }
}
