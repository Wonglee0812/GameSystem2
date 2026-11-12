using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// The pet notices food puffs and flies to the nearest one to eat it. Eating
    /// fills the Fullness bar, cheers the pet up and changes its temperature (fan
    /// puffs cool, dryer puffs warm). A full pet still eats, but then the puff only
    /// changes its temperature. A sleeping pet ignores food.
    /// </summary>
    public class PetFeeding : MonoBehaviour
    {
        public PetDrag drag;
        public PetWander wander;
        public PetStateMachine fsm;
        [Header("Eating effect")]
        public PetLook look;
        public PetFace face;
        [Tooltip("How close the puff must come to the pet's body (on top of the body's own radius).")]
        public float eatMargin = 0.25f;
        [Tooltip("Above this fullness a puff only changes the temperature (no fullness, no mood).")]
        public float fullAbove = 95f;

        void Update()
        {
            if (drag == null || wander == null || fsm == null) return;
            Vector3 centre = drag.body != null ? drag.body.transform.TransformPoint(drag.body.center) : transform.position;
            Vector3 feetToCentre = centre - transform.position;
            float half = drag.body != null ? Mathf.Max(0f, drag.body.height * 0.5f - drag.body.radius) : 0f;
            float reach = (drag.body != null ? drag.body.radius : 0.5f) + eatMargin;

            bool wantsFood = fsm.Energy != EnergyState.Sleep && !drag.IsHeld && !drag.Anchor.HasValue;
            FoodPuff nearest = null;
            float best = float.MaxValue;
            if (wantsFood)
                foreach (var puff in FoodPuff.All)
                {
                    float dist = DistanceToBody(puff.transform.position, centre, half);
                    if (dist < best) { best = dist; nearest = puff; }
                }

            if (nearest == null) { wander.FoodTarget = null; return; }

            // aim the feet so that the body's middle reaches the puff
            Vector3 goal = nearest.transform.position - feetToCentre;
            goal.z = nearest.transform.position.z;               // fly to the puff's depth too
            wander.FoodTarget = goal;

            bool sameDepth = Mathf.Abs(nearest.transform.position.z - transform.position.z) < 0.8f;
            if (best < reach && sameDepth && nearest.Eat(drag.visual != null ? drag.visual : transform))
            {
                bool full = fsm.stats.Fullness >= fullAbove;
                fsm.stats.ChangeTemperature(nearest.TemperatureChange);   // always: wind food is warm or cold air
                if (!full)
                {
                    fsm.stats.ChangeFullness(nearest.Food);
                    fsm.stats.ChangeMood(fsm.stats.config.eatMoodBonus);
                }
                // a happy gulp: wobble, glow in the food's colour, squint, small burst
                drag.Wobble(1.5f);
                if (look != null) look.Flash(nearest.PuffColor);
                if (face != null) face.Happy();
                nearest.Burst(centre);
            }
        }

        /// <summary>Distance (in X and Y) from a point to the pet's body, a vertical line from feet to head.</summary>
        static float DistanceToBody(Vector3 point, Vector3 centre, float half)
        {
            Vector2 p = new Vector2(point.x, point.y);
            float y = Mathf.Clamp(p.y, centre.y - half, centre.y + half);
            return Vector2.Distance(p, new Vector2(centre.x, y));
        }
    }
}
