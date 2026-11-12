using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WindPet.UI
{
    /// <summary>
    /// A big, readable copy of the bill under the pointer (or the one being carried),
    /// shown on screen like the documents in Papers, Please. The bill on the desk is
    /// small and seen at an angle, so the player reads the numbers here.
    /// </summary>
    public class BillViewer : MonoBehaviour
    {
        public GameObject panel;
        public TMP_Text orderNo;
        public TMP_Text buyer;
        public TMP_Text kwh;
        public TMP_Text euro;
        public GameObject stamp;
        [Tooltip("Small captions above the two numbers; loan offers change them.")]
        public TMP_Text wantsLabel;
        public TMP_Text paysLabel;
        [Tooltip("The small print at the bottom; contracts and loans show their own rules here.")]
        public TMP_Text fine;

        const string OrderRules = "Stamp to approve, then post it in the pachinko slot. Unstamped orders go back unpaid.";
        public LayerMask propLayers;

        Camera cam;

        void Start()
        {
            cam = Camera.main;
            if (panel != null) panel.SetActive(false);
        }

        void Update()
        {
            Bill bill = null;
            var grabber = ItemGrabber.Instance;
            if (grabber != null && grabber.Held != null) bill = grabber.Held.GetComponent<Bill>();
            var pointer = Pointer.current;
            if (bill == null && pointer != null && cam != null)
            {
                Ray ray = cam.ScreenPointToRay(pointer.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit, 100f, propLayers, QueryTriggerInteraction.Ignore))
                    bill = hit.collider.GetComponentInParent<Bill>();
            }
            if (panel != null) panel.SetActive(bill != null);
            if (bill == null) return;
            orderNo.text = bill.IsContract ? "<color=#B03A2E>CONTRACT " + bill.OrderNo + "  ·  due " + bill.DueText + "</color>"
                         : (bill.IsLoan ? "Loan No. " : "Order No. ") + bill.OrderNo;
            if (fine != null)
            {
                var office = ContractOffice.Instance;
                fine.text = bill.IsContract
                    ? $"<color=#B03A2E>Must be delivered before {bill.DueText}. Missed or unstamped: fine €{(office != null ? office.NextFine(bill) : bill.Fine):0.00}" +
                      (office != null ? $" (fines {office.Strikes}/{office.config.strikesBeforeBigFine})" : "") + "</color>"
                    : bill.IsLoan ? "Stamp and post it to get the money. Part of every later bill pays it back."
                    : OrderRules;
            }
            buyer.text = bill.Buyer + "\n<size=75%>" + bill.Business + "</size>";
            if (bill.IsLoan)
            {
                kwh.text = "€" + bill.Euro.ToString("0.00");
                euro.text = "€" + bill.Repay.ToString("0.00");
            }
            else
            {
                kwh.text = bill.Kwh + " <size=50%>kWh</size>";
                euro.text = "€" + bill.Euro.ToString("0.00");
            }
            if (wantsLabel != null) wantsLabel.text = bill.IsLoan ? "YOU GET NOW" : "WANTS TO BUY";
            if (paysLabel != null) paysLabel.text = bill.IsLoan ? "PAID BACK FROM BILLS" : "OFFERS TO PAY";
            stamp.SetActive(bill.Stamped);
        }
    }
}
