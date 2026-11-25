using UnityEngine;

namespace BrickPanic
{
    /// <summary>Removes bricks and people that fell off the floating island. A person who falls off is dead.</summary>
    public class KillZone : MonoBehaviour
    {
        [Tooltip("Anything below this height in metres is lost. Default -25.")]
        public float killHeight = -25f;
        [Tooltip("Seconds between checks. Default 0.5.")]
        public float checkEvery = 0.5f;

        float next;

        void Update()
        {
            if (Time.time < next) return;
            next = Time.time + checkEvery;
            for (int i = Block.All.Count - 1; i >= 0; i--)
            {
                var b = Block.All[i];
                if (b.transform.position.y < killHeight)
                {
                    Block.Lost++;
                    Destroy(b.gameObject);
                }
            }
            for (int i = Person.All.Count - 1; i >= 0; i--)
            {
                var p = Person.All[i];
                if (p.transform.position.y < killHeight)
                {
                    p.Die("fell off");
                    Destroy(p.gameObject);
                }
            }
        }
    }
}
