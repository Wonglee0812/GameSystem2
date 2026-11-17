using UnityEngine;

namespace DamRush
{
    /// <summary>
    /// Builds a beaver from Unity primitives: round body, head with orange front teeth,
    /// flat scaly tail. Kits and yearlings are smaller copies. Placeholder art for the prototype.
    /// </summary>
    public static class BeaverLook
    {
        public static Beaver Create(ColonyManager colony, string name, BeaverAge age, Vector3 position)
        {
            float s = age == BeaverAge.Adult ? 1f : age == BeaverAge.Yearling ? 0.82f : 0.6f;
            var root = new GameObject(name);
            root.transform.SetParent(colony.transform);
            root.transform.position = position;

            // child 0 = body pivot (it nods while the beaver works)
            var body = new GameObject("Body").transform;
            body.SetParent(root.transform, false);
            body.localScale = Vector3.one * s;

            Piece(body, PrimitiveType.Sphere, colony.fur, new Vector3(0f, 0.28f, 0f), new Vector3(0.55f, 0.48f, 0.9f));     // body
            Piece(body, PrimitiveType.Sphere, colony.fur, new Vector3(0f, 0.38f, 0.5f), new Vector3(0.38f, 0.34f, 0.4f));   // head
            Piece(body, PrimitiveType.Sphere, colony.dark, new Vector3(0f, 0.38f, 0.71f), new Vector3(0.1f, 0.08f, 0.06f)); // nose
            Piece(body, PrimitiveType.Cube, colony.teeth, new Vector3(0f, 0.27f, 0.67f), new Vector3(0.09f, 0.08f, 0.03f));  // teeth
            Piece(body, PrimitiveType.Sphere, colony.dark, new Vector3(0.14f, 0.52f, 0.46f), new Vector3(0.07f, 0.07f, 0.04f)); // ears
            Piece(body, PrimitiveType.Sphere, colony.dark, new Vector3(-0.14f, 0.52f, 0.46f), new Vector3(0.07f, 0.07f, 0.04f));
            Piece(body, PrimitiveType.Sphere, colony.dark, new Vector3(0.12f, 0.44f, 0.64f), new Vector3(0.05f, 0.05f, 0.03f)); // eyes
            Piece(body, PrimitiveType.Sphere, colony.dark, new Vector3(-0.12f, 0.44f, 0.64f), new Vector3(0.05f, 0.05f, 0.03f));
            Piece(body, PrimitiveType.Cube, colony.tail, new Vector3(0f, 0.12f, -0.62f), new Vector3(0.32f, 0.06f, 0.5f));   // tail

            var b = root.AddComponent<Beaver>();
            b.beaverName = name;
            b.age = age;
            b.colony = colony;
            b.walkSpeed = 1.1f * Mathf.Lerp(0.8f, 1f, s);
            b.swimSpeed = 2f * Mathf.Lerp(0.8f, 1f, s);
            // what it carries sits on its back
            b.carryWood = Piece(body, PrimitiveType.Cylinder, colony.woodMat, new Vector3(0f, 0.6f, 0f), new Vector3(0.14f, 0.55f, 0.14f), new Vector3(0f, 0f, 90f));
            b.carryMud = Piece(body, PrimitiveType.Sphere, colony.mudMat, new Vector3(0f, 0.58f, 0.05f), new Vector3(0.3f, 0.22f, 0.3f));
            b.carryStone = Piece(body, PrimitiveType.Cube, colony.stoneMat, new Vector3(0f, 0.6f, 0.05f), new Vector3(0.24f, 0.2f, 0.26f), new Vector3(10f, 25f, 5f));
            return b;
        }

        static GameObject Piece(Transform parent, PrimitiveType type, Material m, Vector3 pos, Vector3 scale, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = m;
            return go;
        }
    }
}
