using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// The teleprinter on the desk. Every so often it prints a new purchase order,
    /// which pops out of the top and lands on the desk, as long as there are not
    /// too many bills lying around already. The lamp blinks while it prints.
    /// The red button on the front prints one now; it has a few presses that
    /// recharge over time, so it cannot be spammed.
    /// </summary>
    public class OrderPrinter : MonoBehaviour
    {
        public WorkConfig config;
        public Material paperMaterial;
        public Material edgeMaterial;
        [Tooltip("Red edge for contracts, so they stand out on the desk.")]
        public Material contractEdgeMaterial;
        [Tooltip("Where a new bill appears, relative to the printer.")]
        public Vector3 spawnOffset = new Vector3(0f, 0.8f, 0f);
        public Vector3 ejectVelocity = new Vector3(1.4f, 1.6f, -0.6f);
        public Light lamp;
        [Tooltip("The red button, relative to the printer.")]
        public Vector3 buttonLocal;
        public float buttonRadius = 0.35f;

        /// <summary>Button presses left.</summary>
        public int Charges { get; private set; }
        float recharge;

        float timer;
        float blink;

        void Start()
        {
            timer = 3f;   // the first order comes quickly
            Charges = config.buttonCharges;
        }

        void Update()
        {
            UpdateButton();
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                timer = Random.Range(config.printEvery.x, config.printEvery.y);
                if (FindObjectsByType<Bill>(FindObjectsSortMode.None).Length < config.maxBillsOnDesk) Print();
            }
            if (lamp != null)
            {
                blink = Mathf.MoveTowards(blink, 0f, Time.deltaTime);
                lamp.intensity = blink > 0f && Mathf.Repeat(blink * 8f, 1f) > 0.5f ? 2f : 0f;
            }
        }

        void UpdateButton()
        {
            if (Charges < config.buttonCharges)
            {
                recharge += Time.deltaTime;
                if (recharge >= config.buttonRecharge) { recharge = 0f; Charges++; }
            }
            var pointer = UnityEngine.InputSystem.Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame) return;
            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
            if (ItemGrabber.Instance != null && ItemGrabber.Instance.Hovered != null) return;
            Ray ray = Camera.main.ScreenPointToRay(pointer.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f, ~0, QueryTriggerInteraction.Ignore) || !hit.collider.transform.IsChildOf(transform)) return;
            Vector3 button = transform.TransformPoint(buttonLocal);
            if (Vector3.Distance(hit.point, button) > buttonRadius) return;
            Vector3 at = button + Vector3.up * 0.8f;
            if (Charges <= 0)
            {
                FloatingText.Show("Recharging... " + Mathf.CeilToInt(config.buttonRecharge - recharge) + " s", at, new Color(1f, 0.6f, 0.45f));
                return;
            }
            if (FindObjectsByType<Bill>(FindObjectsSortMode.None).Length >= config.maxBillsOnDesk + 2)
            {
                FloatingText.Show("Desk is full", at, new Color(1f, 0.6f, 0.45f));
                return;
            }
            Charges--;
            Print();
            FloatingText.Show(Charges + " / " + config.buttonCharges + " left", at + Vector3.up * 0.4f, new Color(1f, 0.9f, 0.55f));
        }

        /// <summary>Prints a loan offer from the bank (see LoanOffice).</summary>
        public Bill PrintLoan(float amount) => Eject(Bill.CreateLoan(transform.position + spawnOffset, config, paperMaterial, edgeMaterial, amount), "Loan offer");

        public void Print()
        {
            var office = ContractOffice.Instance;
            if (office != null && office.OpenCount < config.maxOpenContracts && Random.value < config.contractChance)
            {
                var contract = Bill.CreateContract(transform.position + spawnOffset, config, paperMaterial,
                                                   contractEdgeMaterial != null ? contractEdgeMaterial : edgeMaterial);
                office.Register(contract);
                Eject(contract, "New contract! due " + contract.DueText);
                return;
            }
            Eject(Bill.Create(transform.position + spawnOffset, config, paperMaterial, edgeMaterial), "New order");
        }

        Bill Eject(Bill bill, string message)
        {
            var rb = bill.GetComponent<Rigidbody>();
            rb.linearVelocity = ejectVelocity;
            rb.angularVelocity = new Vector3(0f, Random.Range(-1f, 1f), 0f);
            if (PropViewportKeeper.Instance != null) PropViewportKeeper.Instance.Register(rb);
            blink = 1.2f;
            FloatingText.Show(message, transform.position + Vector3.up * 1.1f, new Color(1f, 0.9f, 0.55f));
            return bill;
        }
    }
}
