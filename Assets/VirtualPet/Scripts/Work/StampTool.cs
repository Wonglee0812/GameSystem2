using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// The APPROVED stamp. Click it to hold it, then left click:
    /// on the ink pad to ink it, on a bill to stamp it. Every stamp uses ink.
    /// Right click throws it, like any held item.
    /// </summary>
    public class StampTool : HoldableItem
    {
        public WorkConfig config;
        public int Ink { get; private set; }

        public override bool Use(ToolBelt belt, Vector2 screen)
        {
            var cam = belt.cam;
            Ray ray = cam.ScreenPointToRay(screen);
            var hits = Physics.RaycastAll(ray, 100f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                var pad = h.collider.GetComponentInParent<InkPad>();
                if (pad != null)
                {
                    Ink = config != null ? config.inkPerPad : 5;
                    FloatingText.Show("Inked x" + Ink, h.point + Vector3.up * 0.4f, new Color(0.9f, 0.35f, 0.3f));
                    return true;
                }
                var bill = h.collider.GetComponentInParent<Bill>();
                if (bill != null)
                {
                    if (bill.Stamped) return true;
                    if (Ink <= 0)
                    {
                        belt.Shake();
                        FloatingText.Show("No ink: press the stamp on the pad", h.point + Vector3.up * 0.4f, new Color(1f, 0.8f, 0.6f));
                        return true;
                    }
                    Ink--;
                    bill.Stamp();
                    var rb = bill.GetComponent<Rigidbody>();
                    if (rb != null) rb.AddForce(Vector3.down * 2f, ForceMode.VelocityChange);   // a little thump
                    FloatingText.Show("APPROVED", h.point + Vector3.up * 0.4f, new Color(0.9f, 0.3f, 0.25f));
                    return true;
                }
            }
            return true;   // clicking empty space with the stamp does nothing (right click throws it)
        }
    }
}
