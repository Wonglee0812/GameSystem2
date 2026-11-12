using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// The ORDERS IN slot of the pachinko machine (a trigger zone). A posted bill:
    /// - stamped and enough electricity: the kWh leave the store and the money comes out as coins;
    /// - stamped but not enough electricity: the machine spits the bill back out;
    /// - not stamped: the order is returned to the buyer unpaid (the bill is gone).
    /// Loan offers: stamped = the bank pays out; not stamped = refused.
    /// While a loan is open, part of every payment goes to the bank (LoanOffice).
    /// </summary>
    public class PachinkoReceiver : MonoBehaviour
    {
        public Wallet wallet;
        public Transform coinTray;
        public Material coinMaterial;

        void OnTriggerEnter(Collider other)
        {
            var bill = other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<Bill>() : null;
            if (bill == null) return;
            var grid = PowerGrid.Instance;
            Vector3 at = transform.position + Vector3.up * 0.6f;
            var bank = LoanOffice.Instance;

            if (bill.IsLoan)
            {
                if (bill.Stamped && bank != null)
                {
                    bank.Accept(bill);
                    FloatingText.Show("Loan +€" + bill.Euro.ToString("0.00"), at, new Color(1f, 0.85f, 0.3f));
                    Coins(Mathf.Clamp(Mathf.RoundToInt(bill.Euro), 3, 20));
                }
                else
                {
                    bank?.Refuse();
                    FloatingText.Show("Loan refused", at, new Color(0.85f, 0.85f, 0.85f));
                }
                Remove(bill);
                return;
            }

            var office = ContractOffice.Instance;
            if (bill.IsContract && !bill.Stamped)
            {
                office?.Fine(bill, "Contract refused");
                Remove(bill);
                return;
            }
            if (!bill.Stamped)
            {
                FloatingText.Show("Returned unpaid", at, new Color(0.85f, 0.85f, 0.85f));
                Remove(bill);
                return;
            }
            if (grid == null || !grid.TryUse(bill.Kwh))
            {
                FloatingText.Show("Not enough power (" + bill.Kwh + " kWh)", at, new Color(1f, 0.45f, 0.35f));
                var rb = bill.GetComponent<Rigidbody>();
                if (ItemGrabber.Instance != null && ItemGrabber.Instance.Held == rb) ItemGrabber.Instance.ForceRelease();
                // spit it back out gently, towards the player
                Vector3 toCam = Camera.main != null ? Camera.main.transform.position - transform.position : Vector3.back;
                toCam.y = 0f; toCam.Normalize();
                rb.position += toCam * 0.6f;
                rb.linearVelocity = toCam * 1.2f + Vector3.up * 1.5f;
                return;
            }
            float paid = 0f;
            float keep = bank != null ? bank.TakeRepayment(bill.Euro, out paid) : bill.Euro;
            wallet.Add(keep);
            FloatingText.Show("+€" + keep.ToString("0.00") + "   -" + bill.Kwh + " kWh", at, new Color(1f, 0.85f, 0.3f));
            if (paid > 0f) FloatingText.Show("Loan -€" + paid.ToString("0.00"), at + Vector3.up * 0.5f, new Color(1f, 0.6f, 0.45f));
            Coins(Mathf.Clamp(Mathf.RoundToInt(bill.Euro), 3, 20));
            if (bill.IsContract) office?.Complete(bill);
            Remove(bill);
        }

        static void Remove(Bill bill)
        {
            var rb = bill.GetComponent<Rigidbody>();
            if (ItemGrabber.Instance != null && ItemGrabber.Instance.Held == rb) ItemGrabber.Instance.ForceRelease();
            Destroy(bill.gameObject);
        }

        void Coins(int count)
        {
            Vector3 at = coinTray != null ? coinTray.position : transform.position;
            var go = new GameObject("Coins");
            go.transform.position = at + Vector3.up * 0.4f;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = ps.main;
            m.loop = false;
            m.duration = 0.4f;
            m.startLifetime = 1.1f;
            m.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.16f);
            m.startColor = new Color(1f, 0.82f, 0.3f);
            m.gravityModifier = 1.5f;
            var e = ps.emission; e.rateOverTime = 0f; e.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 25f; sh.radius = 0.1f;
            sh.rotation = new Vector3(-90f, 0f, 0f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = coinMaterial;
            ps.Play();
            Destroy(go, 2f);
        }
    }
}
