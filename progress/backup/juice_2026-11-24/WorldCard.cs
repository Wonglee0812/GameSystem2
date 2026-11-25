using DG.Tweening;
using UnityEngine;

namespace PokerTD
{
    /// <summary>
    /// A playing card in the 3D world (on a tower or above an enemy). It always faces the
    /// camera and uses the same sprites as the hand. Animations (DOTween):
    /// PopIn = appears with a bounce, Punch = small hit reaction, Shake = steady trembling
    /// (set every frame, 0..1), Burst = hard shake, short hold, then dissolve.
    /// Layers: root (position, faces the camera) > Body (scale, punch, shake) > sprite.
    /// </summary>
    public class WorldCard : MonoBehaviour
    {
        const float SpriteWidth = 2.64f;   // card sprites are 264 px wide at 100 px per unit

        public Card Card { get; private set; }
        public bool Busy { get; private set; }   // bursting or dissolving: do not reuse

        /// <summary>0..1, steady trembling (enemy marks shake harder when the burst comes near).</summary>
        public float Shake { get; set; }

        /// <summary>Extra angle around the view axis (a fan of cards above an enemy).</summary>
        public float Roll { get; set; }

        Transform body, wobble;
        SpriteRenderer sprite;
        MaterialPropertyBlock block;
        float dissolve, flash, grey, baseScale = 1f;

        static Material material;
        static readonly int DissolveId = Shader.PropertyToID("_Dissolve");
        static readonly int FlashId = Shader.PropertyToID("_Flash");
        static readonly int GreyId = Shader.PropertyToID("_Grey");

        public static WorldCard Create(Transform parent, Vector3 localPosition, float width, int sortingOrder)
        {
            var go = new GameObject("WorldCard");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var wc = go.AddComponent<WorldCard>();
            wc.Build(width, sortingOrder);
            return wc;
        }

        void Build(float width, int sortingOrder)
        {
            if (material == null) material = Resources.Load<Material>("M_CardDissolve");
            body = new GameObject("Body").transform;
            body.SetParent(transform, false);
            wobble = new GameObject("Sprite").transform;
            wobble.SetParent(body, false);
            sprite = wobble.gameObject.AddComponent<SpriteRenderer>();
            if (material != null) sprite.sharedMaterial = material;
            sprite.sortingOrder = sortingOrder;
            baseScale = width / SpriteWidth;
            wobble.localScale = Vector3.one * baseScale;
            block = new MaterialPropertyBlock();
            Apply();
        }

        public void SetCard(Card card)
        {
            Card = card;
            sprite.sprite = UI.CardArt.Face(card);
        }

        /// <summary>Any card face (a build card flying to its tile).</summary>
        public void SetSprite(Sprite face) => sprite.sprite = face;

        public void SetGrey(bool on)
        {
            float g = on ? 1f : 0f;
            if (Mathf.Approximately(g, grey)) return;
            grey = g;
            Apply();
        }

        /// <summary>Appears: grows from nothing with a bounce, a white flash and a small spin.</summary>
        public void PopIn()
        {
            Reset();
            body.localScale = Vector3.zero;
            body.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetLink(gameObject);
            body.DOPunchRotation(new Vector3(0f, 0f, 25f), 0.4f, 8, 0.8f).SetLink(gameObject);
            Flash(0.8f, 0.25f);
        }

        /// <summary>A hit: short punch in scale and angle (strength 1 = normal hit).</summary>
        public void Punch(float strength = 1f)
        {
            if (Busy) return;
            body.DOComplete();
            body.DOPunchScale(Vector3.one * 0.18f * strength, 0.22f, 6, 0.8f).SetLink(gameObject);
            body.DOPunchRotation(new Vector3(0f, 0f, 14f * strength * (Random.value < 0.5f ? -1f : 1f)), 0.22f, 8, 0.8f).SetLink(gameObject);
            Flash(0.45f * strength, 0.15f);
        }

        /// <summary>The burst: hard shake, a short hold, then the card burns away and is destroyed.</summary>
        public void Burst(float delay = 0f)
        {
            if (Busy) return;
            Busy = true;
            Shake = 0f;
            body.DOComplete();
            var s = DOTween.Sequence().SetTarget(this).SetLink(gameObject);
            s.AppendInterval(delay);
            s.Append(body.DOScale(1.35f, 0.12f).SetEase(Ease.OutBack));
            s.Join(DOTween.To(() => flash, v => { flash = v; Apply(); }, 1f, 0.12f).SetTarget(this));
            s.Append(body.DOShakePosition(0.3f, 0.12f, 40, 90f, false, false));
            s.Join(body.DOShakeRotation(0.3f, new Vector3(0f, 0f, 25f), 40, 90f, false));
            s.Join(DOTween.To(() => flash, v => { flash = v; Apply(); }, 0f, 0.3f).SetTarget(this));
            s.AppendInterval(0.12f);                                  // stay for a moment
            s.Append(DOTween.To(() => dissolve, v => { dissolve = v; Apply(); }, 1f, 0.45f).SetEase(Ease.InQuad));
            s.Join(body.DOScale(1.5f, 0.45f));
            s.OnComplete(() => Destroy(gameObject));
        }

        /// <summary>Burns away without the shake (a tower losing its card). destroy = false keeps it for reuse.</summary>
        public void DissolveOut(bool destroy, float time = 0.35f)
        {
            if (Busy) return;
            Busy = true;
            Shake = 0f;
            body.DOComplete();
            DOTween.To(() => dissolve, v => { dissolve = v; Apply(); }, 1f, time).SetEase(Ease.InQuad).SetTarget(this).SetLink(gameObject)
                .OnComplete(() =>
                {
                    if (destroy) { Destroy(gameObject); return; }
                    gameObject.SetActive(false);
                    Reset();
                });
        }

        void Reset()
        {
            Busy = false;
            DOTween.Kill(this);
            body.DOKill();
            body.localPosition = Vector3.zero;
            body.localRotation = Quaternion.identity;
            body.localScale = Vector3.one;
            dissolve = 0f; flash = 0f;
            Apply();
        }

        void Flash(float amount, float time)
        {
            flash = amount;
            Apply();
            DOTween.To(() => flash, v => { flash = v; Apply(); }, 0f, time).SetTarget(this).SetLink(gameObject);
        }

        void Apply()
        {
            if (sprite == null) return;
            sprite.GetPropertyBlock(block);
            block.SetFloat(DissolveId, dissolve);
            block.SetFloat(FlashId, flash);
            block.SetFloat(GreyId, grey);
            sprite.SetPropertyBlock(block);
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation * Quaternion.Euler(0f, 0f, Roll);

            // steady trembling on the inner layer, so it does not fight the DOTween punches on Body
            if (Shake > 0.001f && !Busy)
            {
                float a = Shake * Shake;
                wobble.localPosition = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * 0.035f * a;
                wobble.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-1f, 1f) * 9f * a);
            }
            else
            {
                wobble.localPosition = Vector3.zero;
                wobble.localRotation = Quaternion.identity;
            }
        }
    }
}
