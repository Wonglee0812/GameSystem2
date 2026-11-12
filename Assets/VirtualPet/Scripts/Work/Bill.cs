using TMPro;
using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// A purchase order for electricity, printed by the order printer. A buyer wants
    /// some kWh for a price. The player reads it, stamps it (approve) or not, and posts
    /// it into the pachinko machine. Prices are random, so some bills are great deals
    /// and some are a loss.
    /// </summary>
    public class Bill : MonoBehaviour
    {
        static readonly string[] Buyers =
        {
            "Mrs. H. Vermeer|Corner Bakery", "Mr. B. Kuiper|Kuiper Ice Rink", "Ms. L. de Wit|Night Library",
            "Mr. T. Bos|Bos Bicycle Repair", "Mrs. A. Smit|Smit Laundry", "Dr. J. Mulder|Mulder Clinic",
            "Mr. K. Visser|Harbour Lighthouse", "Ms. E. Jansen|Jansen Flower Farm", "Mr. P. Dekker|Dekker Cinema",
            "Mrs. N. Peters|Peters Ice Cream"
        };
        static int nextNumber = 412;

        public string OrderNo { get; private set; }
        public string Buyer { get; private set; }
        public string Business { get; private set; }
        public int Kwh { get; private set; }
        public float Euro { get; private set; }
        public bool Stamped { get; private set; }
        /// <summary>A loan offer from the bank instead of a purchase order. Euro = money you get.</summary>
        public bool IsLoan { get; private set; }
        /// <summary>For a loan: what you pay back from later bills.</summary>
        public float Repay { get; private set; }
        /// <summary>A contract must be delivered before DueMinute (game clock), or Fine is charged.</summary>
        public bool IsContract { get; private set; }
        public int DueMinute { get; private set; }
        public float Fine { get; private set; }
        public float EuroPerKwh => Euro / Mathf.Max(1, Kwh);

        GameObject stampMark;

        /// <summary>Makes a new bill with random numbers from the config.</summary>
        public static Bill Create(Vector3 position, WorkConfig cfg, Material paper, Material edge) => Create(position, cfg, paper, edge, 0f, false);

        /// <summary>Makes a contract: a bigger order with a deadline and a fine (see ContractOffice).</summary>
        public static Bill CreateContract(Vector3 position, WorkConfig cfg, Material paper, Material edge) => Create(position, cfg, paper, edge, 0f, true);

        /// <summary>Makes a loan offer of 'amount' euros (see LoanOffice).</summary>
        public static Bill CreateLoan(Vector3 position, WorkConfig cfg, Material paper, Material edge, float amount) => Create(position, cfg, paper, edge, amount, false);

        static Bill Create(Vector3 position, WorkConfig cfg, Material paper, Material edge, float loan, bool contract)
        {
            var root = new GameObject("Bill");
            root.transform.position = position;
            root.layer = LayerMask.NameToLayer("Props");
            var bill = root.AddComponent<Bill>();
            if (loan > 0f) bill.MakeLoan(cfg, loan); else bill.Randomise(cfg);
            if (contract) bill.MakeContract(cfg);
            root.transform.localScale = Vector3.one * cfg.billScale;

            const float w = 0.55f, d = 0.75f;
            var sheet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(sheet.GetComponent<Collider>());
            sheet.name = "Paper";
            sheet.transform.SetParent(root.transform, false);
            sheet.transform.localScale = new Vector3(w, 0.012f, d);
            sheet.GetComponent<Renderer>().sharedMaterial = paper;
            var border = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(border.GetComponent<Collider>());
            border.name = "Edge";
            border.transform.SetParent(root.transform, false);
            border.transform.localPosition = new Vector3(0f, -0.003f, 0f);
            border.transform.localScale = new Vector3(w + 0.03f, 0.008f, d + 0.03f);
            border.GetComponent<Renderer>().sharedMaterial = edge;

            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(w, 0.02f, d);
            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 0.1f;
            rb.linearDamping = 1.2f;
            rb.angularDamping = 1.5f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            foreach (var t in root.GetComponentsInChildren<Transform>()) t.gameObject.layer = root.layer;

            // small text on the paper itself (the big readable copy is the bill viewer on screen)
            var text = Text(root.transform, "Text", new Vector3(0f, 0.008f, 0.02f), 0.7f, new Color32(58, 40, 28, 255));
            text.text = bill.IsContract
                ? "<b>BREEZE & BOLT</b>\n<size=60%><color=#B03A2E>CONTRACT " + bill.OrderNo + "</color></size>\n<size=150%>" + bill.Kwh + " kWh</size>\n<size=150%>€" + bill.Euro.ToString("0.00") + "</size>\n<size=70%><color=#B03A2E>due " + bill.DueText + "</color></size>"
                : bill.IsLoan
                ? "<b>BREEZE & BOLT BANK</b>\n<size=60%>EMERGENCY LOAN " + bill.OrderNo + "</size>\n\n<size=160%>+€" + bill.Euro.ToString("0.00") + "</size>\n<size=80%>pay back €" + bill.Repay.ToString("0.00") + "</size>"
                : "<b>BREEZE & BOLT</b>\n<size=60%>PURCHASE ORDER " + bill.OrderNo + "</size>\n\n<size=160%>" + bill.Kwh + " kWh</size>\n<size=160%>€" + bill.Euro.ToString("0.00") + "</size>";
            var mark = Text(root.transform, "StampMark", new Vector3(0.08f, 0.01f, -0.25f), 1.3f, new Color32(192, 57, 43, 230));
            mark.text = "<b>APPROVED</b>";
            mark.transform.localRotation = Quaternion.Euler(90f, -12f, 0f);
            bill.stampMark = mark.gameObject;
            bill.stampMark.SetActive(false);
            return bill;
        }

        static TextMeshPro Text(Transform parent, string name, Vector3 pos, float size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);   // lies flat, readable from the camera side
            var t = go.AddComponent<TextMeshPro>();
            t.fontSize = size;
            t.alignment = TextAlignmentOptions.Center;
            t.color = color;
            t.rectTransform.sizeDelta = new Vector2(0.5f, 0.68f);
            t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        void Randomise(WorkConfig cfg)
        {
            OrderNo = "PO-" + (nextNumber++).ToString("0000");
            var who = Buyers[Random.Range(0, Buyers.Length)].Split('|');
            Buyer = who[0];
            Business = who[1];
            // bigger turbine, bigger customers; from level 2 some orders are three times the size
            float size = Turbine.CurrentMultiplier;
            if (size > 1.01f && Random.value < cfg.bigOrderChance) size *= 3f;
            Kwh = Mathf.Max(1, Mathf.RoundToInt(Random.Range(cfg.kwhRange.x, cfg.kwhRange.y + 1) * size));
            // log-normal spread around the fair price: most bills are close to fair, a few are great or terrible
            Euro = Mathf.Max(0.05f, Mathf.Round(Kwh * EuroPerKwhRandom(cfg) * 20f) / 20f);   // rounded to 5 cents
        }

        void MakeContract(WorkConfig cfg)
        {
            IsContract = true;
            OrderNo = "CT" + OrderNo.Substring(2);
            Kwh = Mathf.Max(1, Mathf.RoundToInt(Kwh * cfg.contractKwhMultiplier));
            Euro = Mathf.Max(0.05f, Mathf.Round(Kwh * EuroPerKwhRandom(cfg) * 20f) / 20f);
            int now = GameClock.Instance != null ? GameClock.Instance.Now : 0;
            DueMinute = now + Mathf.RoundToInt(Random.Range(cfg.contractHours.x, cfg.contractHours.y) * 60f / 15f) * 15;   // on a quarter hour
            Fine = Mathf.Max(1f, Mathf.Round(Kwh * cfg.fairEuroPerKwh * Random.Range(cfg.finePerKwh.x, cfg.finePerKwh.y)));
        }

        public string DueText => GameClock.Instance != null ? GameClock.Instance.Deadline(DueMinute) : GameClock.Format(DueMinute, false);

        void MakeLoan(WorkConfig cfg, float amount)
        {
            IsLoan = true;
            OrderNo = "LN-" + (nextNumber++).ToString("0000");
            Buyer = "Breeze & Bolt Bank";
            Business = "Emergency loan";
            Kwh = 0;
            Euro = Mathf.Round(amount * 100f) / 100f;
            Repay = Mathf.Round(amount * (1f + cfg.loanInterest) * 100f) / 100f;
        }

        static float EuroPerKwhRandom(WorkConfig cfg) =>
            Mathf.Clamp(cfg.fairEuroPerKwh * Mathf.Exp(Gaussian() * cfg.priceSpread), cfg.euroPerKwhLimits.x, cfg.euroPerKwhLimits.y);

        static float Gaussian()
        {
            float u1 = 1f - Random.value, u2 = Random.value;
            return Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Sin(2f * Mathf.PI * u2);
        }

        public void Stamp()
        {
            if (Stamped) return;
            Stamped = true;
            stampMark.SetActive(true);
        }
    }
}
