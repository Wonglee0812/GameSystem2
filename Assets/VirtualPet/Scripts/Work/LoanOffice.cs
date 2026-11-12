using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// Safety net, because the game has no game over. When the money runs out, the
    /// printer prints a loan offer from the bank. Stamp it and post it in the pachinko
    /// slot to get the money (enough to feed the pet and start earning again). The loan
    /// plus a little interest is then paid back with part of every bill. Only one loan
    /// at a time, and the size grows with the turbine level.
    /// </summary>
    public class LoanOffice : MonoBehaviour
    {
        public static LoanOffice Instance { get; private set; }

        public WorkConfig config;
        public Wallet wallet;
        public OrderPrinter printer;

        /// <summary>Money still owed to the bank.</summary>
        public float Debt { get; private set; }

        Bill offer;
        float wait;

        void Awake() => Instance = this;

        void Update()
        {
            if (wait > 0f) { wait -= Time.deltaTime; return; }
            if (wallet == null || printer == null || Debt > 0f || offer != null) return;
            if (wallet.Money > 0.001f) return;
            offer = printer.PrintLoan(config.loanBase * Turbine.CurrentMultiplier);
        }

        /// <summary>A stamped loan bill was posted: pay out and start the debt.</summary>
        public void Accept(Bill bill)
        {
            wallet.Add(bill.Euro);
            Debt += bill.Repay;
            offer = null;
        }

        /// <summary>An unstamped loan bill was posted: no loan, ask again later.</summary>
        public void Refuse()
        {
            offer = null;
            wait = config.loanCooldown;
        }

        /// <summary>Takes the bank's share from a bill payment. Returns what the player keeps.</summary>
        public float TakeRepayment(float payment, out float paid)
        {
            paid = 0f;
            if (Debt <= 0f) return payment;
            paid = Mathf.Min(Debt, Mathf.Round(payment * config.loanRepayShare * 100f) / 100f);
            Debt = Mathf.Max(0f, Mathf.Round((Debt - paid) * 100f) / 100f);
            return payment - paid;
        }
    }
}
