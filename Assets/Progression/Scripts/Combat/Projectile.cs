using TMPro;
using UnityEngine;

namespace PokerTD
{
    /// <summary>
    /// A flying card. It follows its target; on arrival it marks the target (gatling)
    /// or every enemy in the blast circle (cannon). If the target dies first, it
    /// still flies to the last known spot (a cannon shell can still hit others there).
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        Tower from;
        Card card;
        Enemy target;
        Vector3 lastPos;
        float speed;

        public static void Fire(Tower tower, Enemy target, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.name = "CardShot";
            go.transform.position = tower.transform.position + Vector3.up * 1f;
            go.transform.localScale = new Vector3(0.3f, 0.04f, 0.42f);   // a little playing card
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = material;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", new Color(0.98f, 0.95f, 0.88f));
            r.SetPropertyBlock(block);
            var p = go.AddComponent<Projectile>();
            p.from = tower;
            p.card = tower.Card;
            p.target = target;
            p.lastPos = target.transform.position;
            p.speed = tower.Type.projectileSpeed;
        }

        void Update()
        {
            if (target != null) lastPos = target.transform.position;
            Vector3 to = lastPos - transform.position;
            float step = speed * Time.deltaTime;
            transform.Rotate(0f, 720f * Time.deltaTime, 0f, Space.World);
            if (to.magnitude > step) { transform.position += to.normalized * step; return; }
            Hit();
        }

        void Hit()
        {
            if (from != null && from.Type.kind == TDConfig.TowerKind.Cannon)
            {
                foreach (var e in Enemy.All.ToArray())
                {
                    Vector3 d = e.transform.position - lastPos; d.y = 0f;
                    if (d.magnitude <= from.Type.areaRadius) e.Marks.AddMark(card, from);
                }
                FloatText.Show("*", lastPos + Vector3.up, new Color(1f, 0.8f, 0.4f));
            }
            else if (target != null) target.Marks.AddMark(card, from);
            Destroy(gameObject);
        }
    }
}
