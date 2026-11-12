using UnityEngine;

namespace WindPet
{
    /// <summary>
    /// Facial expressions with the two eye spheres:
    /// - Angry: a dark slanted lid cuts the top of each eye (inner corners down).
    /// - Asleep: the eyes close into thin lines.
    /// - Happy gulp (after eating): a short squint.
    /// </summary>
    public class PetFace : MonoBehaviour
    {
        public PetStateMachine fsm;
        public PetLook look;
        public Transform eyes;
        public Transform eyeL, eyeR;
        public Color lidColor = new Color(0.05f, 0.05f, 0.08f);
        [Tooltip("Lid angle in degrees (inner corners point down).")]
        public float lidAngle = 24f;
        public float speed = 8f;

        Transform lidL, lidR;
        Vector3 baseL, baseR;
        float angry, closed, happy;
        float happyTimer;

        /// <summary>A short happy squint, for example after eating.</summary>
        public void Happy(float seconds = 0.6f) => happyTimer = seconds;

        void Start()
        {
            if (eyeL != null) baseL = eyeL.localScale;
            if (eyeR != null) baseR = eyeR.localScale;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.SetColor("_BaseColor", lidColor);
            lidL = MakeLid("AngryLid_L", eyeL, -lidAngle, mat);
            lidR = MakeLid("AngryLid_R", eyeR, lidAngle, mat);
        }

        Transform MakeLid(string name, Transform eye, float angle, Material mat)
        {
            if (eye == null || eyes == null) return null;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(eyes, false);
            go.transform.localPosition = new Vector3(eye.localPosition.x, 0.3f, eye.localPosition.z - 0.075f);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            go.transform.localScale = Vector3.zero;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        void Update()
        {
            float k = 1f - Mathf.Exp(-speed * Time.deltaTime);
            bool asleep = fsm != null && fsm.Energy == EnergyState.Sleep;
            float storm = look != null ? look.Storm : 0f;
            happyTimer -= Time.deltaTime;

            angry = Mathf.Lerp(angry, asleep ? 0f : Mathf.InverseLerp(0.3f, 0.8f, storm), k);
            closed = Mathf.Lerp(closed, asleep ? 1f : 0f, k * 0.5f);
            happy = Mathf.Lerp(happy, happyTimer > 0f ? 1f : 0f, k * 2f);

            float open = Mathf.Min(1f - 0.92f * closed, 1f - 0.5f * happy);
            if (eyeL != null) eyeL.localScale = new Vector3(baseL.x, baseL.y * open, baseL.z);
            if (eyeR != null) eyeR.localScale = new Vector3(baseR.x, baseR.y * open, baseR.z);

            // the lid slides down over the top of the eye as the pet gets angrier
            Lid(lidL, eyeL);
            Lid(lidR, eyeR);
        }

        void Lid(Transform lid, Transform eye)
        {
            if (lid == null || eye == null) return;
            float a = angry;
            lid.localScale = a < 0.01f ? Vector3.zero : new Vector3(0.32f, 0.14f, 0.02f);
            lid.localPosition = new Vector3(eye.localPosition.x, Mathf.Lerp(0.26f, 0.105f, a), eye.localPosition.z - 0.075f);
        }
    }
}
