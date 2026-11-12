using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// A potion bought in the pachinko shop. It goes straight into the item slot.
    /// Click the pet to make it drink: an instant potion adds to a bar at once, a
    /// timed potion multiplies every gain of that bar for a while (PetStats buffs).
    /// Clicking anywhere else does nothing; right click throws the bottle as usual.
    /// </summary>
    public class PotionItem : HoldableItem
    {
        public PotionDef def;

        public override bool Use(ToolBelt belt, Vector2 screen)
        {
            var pet = belt.pet;
            Ray ray = belt.cam.ScreenPointToRay(screen);
            if (pet == null || pet.grabCollider == null || !pet.grabCollider.Raycast(ray, out _, 200f))
            {
                FloatingText.Show("Click the pet to drink", ray.GetPoint(6f), new Color(1f, 0.9f, 0.7f));
                return true;
            }
            Drink(belt.stats, pet.transform.position + Vector3.up * 1.8f);
            var face = pet.GetComponent<PetFace>();
            if (face != null) face.Happy();
            belt.ConsumeCurrent();
            return true;
        }

        void Drink(PetStats stats, Vector3 at)
        {
            if (stats == null || def == null) return;
            if (def.Timed) stats.AddBuff(def.bar, def.amount, def.duration);
            else if (def.bar == PetBar.Energy) stats.ChangeEnergy(def.amount);
            else if (def.bar == PetBar.Fullness) stats.ChangeFullness(def.amount);
            else stats.ChangeMood(def.amount);
            FloatingText.Show(def.name + "\n<size=70%>" + def.Effect + "</size>", at, new Color(0.85f, 0.7f, 1f));
        }
    }
}
