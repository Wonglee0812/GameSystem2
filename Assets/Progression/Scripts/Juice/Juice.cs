using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PokerTD.UI;

namespace PokerTD
{
    /// <summary>
    /// All "juice" of Poker TD in one place: small audio-visual effects that make actions feel
    /// satisfying without changing the rules ("maximum output for minimum input", Jonasson &amp; Purho, 2012;
    /// one small layer at a time, Nijman, 2013). Every effect has its own switch and the master switch
    /// turns all of them off, so the game can be play-tested with juice ON and OFF.
    /// Game scripts call the static methods (Juice.Burst, Juice.EnemyHit...); they do nothing when the
    /// Juice object is missing or switched off. Phase changes and lost lives are watched here, so
    /// TDGame needs no hooks. Menu "Poker TD/Build Juice In Scene" makes the object, the banner,
    /// the red vignette, the ink background and the model dissolve material.
    /// Camera shake uses "trauma" (Eiserloh, 2016): hits add trauma, shake = trauma squared, trauma fades.
    /// The shake only rotates the camera, so it never fights CameraDragPan (which moves it).
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class Juice : MonoBehaviour
    {
        public static Juice I { get; private set; }

        [Header("Master")]
        [Tooltip("Off = no juice at all (for the juice ON / OFF play test).")]
        public bool juiceOn = true;
        [Range(0f, 2f)] public float strength = 1f;

        [Header("Camera shake (trauma)")]
        public bool screenShake = true;
        [Tooltip("Largest camera tilt in degrees at full trauma.")]
        public float shakeMaxAngle = 1.2f;
        [Tooltip("Trauma lost per second.")]
        public float traumaDecay = 1.8f;
        public float shakeFrequency = 22f;

        [Header("Hit stop (short freeze on big hands)")]
        public bool hitStop = true;
        [Tooltip("Freeze length for the best hand; smaller hands freeze shorter. High Card never freezes.")]
        public float hitStopMax = 0.1f;
        [Tooltip("Time speed during the freeze (0 = full stop).")]
        public float hitStopTimeScale = 0.05f;

        [Header("Burst")]
        [Tooltip("Balatro-style count: chips x mult, then the damage.")]
        public bool burstScore = true;
        public bool burstConfetti = true;

        [Header("Hits")]
        public bool hitFlash = true;
        [Tooltip("Seconds the UFO stays white after a hit.")]
        public float hitFlashTime = 0.12f;
        [Tooltip("Small numbers for every card hit.")]
        public bool hitNumbers = true;
        public bool weaponRecoil = true;
        public bool muzzleFlash = true;
        public bool cannonArc = true;
        public float cannonArcHeight = 2.2f;
        public bool impactDust = true;

        [Header("Enemies")]
        [Tooltip("A dead UFO burns away with the same dissolve as the cards.")]
        public bool deathDissolve = true;
        [Tooltip("Seconds a dead UFO takes to burn away.")]
        public float deathBurnTime = 1.1f;
        [Tooltip("Orange embers (and the bigger blast of Boom UFOs).")]
        public bool deathExplosion = true;
        [Tooltip("A coin flies from a dead UFO to the GOLD text.")]
        public bool coinFly = true;

        [Header("Building and base")]
        [Tooltip("The build card flies to the tile and burns away while the tower burns in.")]
        public bool buildDissolve = true;
        public float buildCardFlight = 0.35f;
        public float buildDissolveTime = 0.6f;
        public bool baseHit = true;

        [Header("Banners")]
        public bool banners = true;
        public TextMeshProUGUI banner;
        [Tooltip("Red screen edge when the castle is hit.")]
        public Image vignette;

        [Header("Background")]
        [Tooltip("Ink splat background under the table (port of Wong's Splatoon shader graph).")]
        public bool inkBackground = true;
        public Renderer background;

        [Header("Scene links (found by name when empty)")]
        public Transform castle;
        public WorldHud worldHud;

        // ---------- state ----------
        float trauma;
        Quaternion camBase;
        Transform cam;
        float noiseSeed;
        float stopUntil, stopCooldown;
        bool stopping;
        TDGame.Phase lastPhase;
        int lastLives = -1, lastAnte = -1;
        ParticleSystem sparks, dust, smoke;
        Material particleMat, flashMat, trailMat;
        static readonly Color Edge = new Color(1f, 0.62f, 0.15f);   // same orange as the dissolve edge
        Sequence bannerSeq;

        bool On(bool flag) => juiceOn && flag;

        void Awake()
        {
            I = this;
            noiseSeed = Random.value * 100f;
        }

        void Start()
        {
            if (Camera.main != null) { cam = Camera.main.transform; camBase = cam.localRotation; }
            if (worldHud == null) worldHud = FindFirstObjectByType<WorldHud>();
            if (castle == null) { var c = GameObject.Find("Castle"); if (c != null) castle = c.transform; }
            if (banner != null) { banner.alpha = 0f; banner.raycastTarget = false; }
            if (vignette != null) { var col = vignette.color; col.a = 0f; vignette.color = col; vignette.raycastTarget = false; }
            MakeParticles();
            flashMat = new Material(Shader.Find("Sprites/Default")) { color = Color.white };
            var g = TDGame.Instance;
            if (g != null) { lastPhase = g.Current; lastLives = g.Lives; lastAnte = g.Ante; }
        }

        void OnDisable()
        {
            if (stopping) Time.timeScale = 1f;
            stopping = false;
            if (cam != null) cam.localRotation = camBase;
        }

        void OnDestroy() { if (I == this) I = null; }

        // ================= every frame =================
        void Update()
        {
            if (background != null && background.gameObject.activeSelf != (juiceOn && inkBackground)) background.gameObject.SetActive(juiceOn && inkBackground);

            // hit stop ends on real time, so it also ends while time is slowed
            if (stopping && Time.unscaledTime >= stopUntil) { Time.timeScale = 1f; stopping = false; }

            var g = TDGame.Instance;
            if (g == null) return;
            if (g.Current != lastPhase) { PhaseChanged(lastPhase, g.Current, g); lastPhase = g.Current; }
            if (lastLives >= 0 && g.Lives < lastLives) BaseHit(lastLives - g.Lives);
            lastLives = g.Lives;
            lastAnte = g.Ante;
        }

        void LateUpdate()
        {
            if (cam == null) return;
            trauma = Mathf.Max(0f, trauma - traumaDecay * Time.unscaledDeltaTime);
            if (!On(screenShake) || trauma <= 0f) { cam.localRotation = camBase; return; }
            float s = trauma * trauma;
            float t = Time.unscaledTime * shakeFrequency;
            float pitch = shakeMaxAngle * s * (Mathf.PerlinNoise(noiseSeed, t) * 2f - 1f);
            float yaw = shakeMaxAngle * s * (Mathf.PerlinNoise(noiseSeed + 1f, t) * 2f - 1f);
            float roll = shakeMaxAngle * 0.5f * s * (Mathf.PerlinNoise(noiseSeed + 2f, t) * 2f - 1f);
            cam.localRotation = camBase * Quaternion.Euler(pitch, yaw, roll);
        }

        // ================= public hooks (static, safe when there is no Juice) =================

        public static void Shake(float amount)
        {
            if (I == null || !I.On(I.screenShake)) return;
            I.trauma = Mathf.Clamp01(I.trauma + amount * I.strength);
        }

        /// <summary>Short freeze. Not again within 0.25 s, so many bursts do not freeze the game all the time.</summary>
        public static void Stop(float seconds)
        {
            if (I == null || !I.On(I.hitStop) || seconds <= 0f) return;
            if (Time.unscaledTime < I.stopCooldown) return;
            I.stopping = true;
            I.stopUntil = Time.unscaledTime + seconds * Mathf.Min(1f, I.strength);
            I.stopCooldown = I.stopUntil + 0.25f;
            Time.timeScale = I.hitStopTimeScale;
        }

        /// <summary>A hand burst on an enemy. Returns true when the juice shows the score (then the plain float text is skipped).</summary>
        public static bool Burst(Vector3 at, DamageCalc.Result r, List<Card> cards)
        {
            if (I == null || !I.juiceOn) return false;
            int rank = (int)r.hand;
            if (rank > 0)
            {
                Shake(0.12f + 0.07f * rank);
                Stop(Mathf.Min(I.hitStopMax, 0.025f + 0.012f * rank));
            }
            else Shake(0.06f);
            if (I.On(I.burstConfetti) && cards != null)
                foreach (var c in cards)
                    I.Emit(I.sparks, at + Vector3.up * 1.2f, SuitColor(c.suit), 4 + rank * 2, 5f + rank * 0.6f);
            if (!I.On(I.burstScore)) return false;
            BurstScore.Show(at + Vector3.up * 2.3f, r);
            return true;
        }

        /// <summary>One card hit an enemy (before the damage): a small number.</summary>
        public static void CardHit(Enemy e, Card card)
        {
            if (I == null || !I.On(I.hitNumbers) || e == null) return;
            HitNumber.Show(e.transform.position + Vector3.up * 1.2f + Random.insideUnitSphere * 0.3f, card.Value.ToString(), SuitColor(card.suit));
        }

        /// <summary>Any damage: white flash and a squash of the UFO.</summary>
        public static void EnemyHit(Enemy e)
        {
            if (I == null || !I.juiceOn || e == null) return;
            var body = e.Body;
            if (body == null) return;
            if (I.hitFlash) Flasher.For(body.gameObject, I.flashMat).Flash(I.hitFlashTime);
            body.DOComplete();
            body.DOPunchScale(new Vector3(0.25f, -0.25f, 0.25f) * body.localScale.x, 0.2f, 6, 0.6f).SetLink(body.gameObject);
        }

        public static void EnemyDied(Enemy e)
        {
            if (I == null || !I.juiceOn || e == null) return;
            Vector3 p = e.transform.position;
            bool boom = e.Type.stunRadius > 0f;
            var body = e.Body;
            if (I.deathDissolve && body != null)
            {
                // a copy of the UFO stays for a moment, rises a little and burns away (the enemy itself is gone)
                var f = body.GetComponent<Flasher>();
                if (f != null) f.Off();
                var ghost = Object.Instantiate(body.gameObject, body.position, body.rotation);
                ghost.name = "DeadUfo";
                ghost.transform.localScale = body.lossyScale;
                var gf = ghost.GetComponent<Flasher>();
                if (gf != null) DestroyImmediate(gf);
                float burn = boom ? I.deathBurnTime * 0.75f : I.deathBurnTime;
                ghost.transform.DOMoveY(body.position.y + 0.9f, burn).SetEase(Ease.OutQuad).SetLink(ghost);
                ghost.transform.DORotate(new Vector3(0f, 300f, 0f), burn, RotateMode.LocalAxisAdd).SetLink(ghost);
                ModelDissolve.Run(ghost, 0f, 1f, burn, true);
            }
            if (I.deathExplosion)
            {
                I.Emit(I.sparks, p, Edge, boom ? 40 : 14, boom ? 9f : 4f);
                if (boom)
                {
                    I.EmitOne(I.sparks, p, new Color(1f, 0.8f, 0.4f), Vector3.zero, 4f, 0.12f);
                    I.Emit(I.smoke, p, new Color(0.35f, 0.32f, 0.35f, 0.8f), 14, 1.5f);
                }
            }
            Shake(boom ? 0.4f : 0.06f);
            if (I.On(I.coinFly) && I.worldHud != null && I.worldHud.gold != null)
                Coin.Fly(p + Vector3.up * 0.5f, I.worldHud.gold.transform, e.Type.gold);
        }

        public static void TowerFired(Tower t)
        {
            if (I == null || !I.juiceOn || t == null) return;
            var w = t.Weapon;
            if (I.weaponRecoil && w != null)
            {
                w.DOComplete();
                float k = t.Type.kind == TDConfig.TowerKind.Cannon ? 0.35f : 0.12f;
                w.DOPunchPosition(new Vector3(0f, 0f, -k), 0.18f, 4, 0.5f).SetLink(w.gameObject);
            }
            if (I.muzzleFlash)
            {
                bool cannon = t.Type.kind == TDConfig.TowerKind.Cannon;
                var dir = w != null ? w.forward : Vector3.forward;
                Vector3 m = t.Muzzle;
                I.EmitOne(I.sparks, m, new Color(1f, 0.95f, 0.7f), dir * 0.5f, cannon ? 1.4f : 0.8f, 0.07f);   // the flash
                int n = cannon ? 18 : 6;
                for (int i = 0; i < n; i++)
                    I.EmitOne(I.sparks, m, new Color(1f, 0.85f, 0.4f), dir * (cannon ? 6f : 5f) + Random.insideUnitSphere * 2f, cannon ? 0.3f : 0.22f, 0.22f);
                if (cannon) I.Emit(I.smoke, m, new Color(0.85f, 0.85f, 0.85f, 0.7f), 6, 1f);
            }
            if (t.Type.kind == TDConfig.TowerKind.Cannon) Shake(0.04f);
        }

        /// <summary>The spike trap pulsed: a ring of dust.</summary>
        public static void TrapPulse(Tower t)
        {
            if (I == null || !I.On(I.impactDust) || t == null) return;
            I.Emit(I.dust, t.transform.position + Vector3.up * 0.3f, new Color(0.75f, 0.6f, 0.45f, 0.8f), 10, 2.5f);
        }

        /// <summary>Height of the cannonball arc for a shot of this length (0 = straight).</summary>
        public static float ArcHeight(float distance) =>
            I == null || !I.On(I.cannonArc) ? 0f : I.cannonArcHeight * Mathf.Clamp01(distance / 5f);

        /// <summary>A shot was fired: a trail behind it, so the player can follow it.</summary>
        public static void DecorateShot(GameObject shot, bool cannon)
        {
            if (I == null || !I.On(I.muzzleFlash) || shot == null) return;
            if (I.trailMat == null) I.trailMat = new Material(Shader.Find("Sprites/Default"));
            var tr = shot.AddComponent<TrailRenderer>();
            tr.sharedMaterial = I.trailMat;
            tr.time = cannon ? 0.3f : 0.14f;
            tr.minVertexDistance = 0.05f;
            tr.widthCurve = new AnimationCurve(new Keyframe(0f, cannon ? 0.4f : 0.18f), new Keyframe(1f, 0f));
            var g = new Gradient();
            Color c0 = cannon ? new Color(1f, 0.75f, 0.35f) : new Color(1f, 0.95f, 0.6f);
            g.SetKeys(new[] { new GradientColorKey(c0, 0f), new GradientColorKey(Edge, 1f) },
                      new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
            tr.colorGradient = g;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.receiveShadows = false;
        }

        /// <summary>Cannonball landed: flash, fireball, sparks, smoke and dust, and a bump of the camera.</summary>
        public static void CannonLanded(Vector3 at, float radius)
        {
            if (I == null || !I.juiceOn) return;
            if (I.deathExplosion)
            {
                I.EmitOne(I.sparks, at + Vector3.up * 0.3f, new Color(1f, 0.9f, 0.6f), Vector3.zero, radius * 1.4f, 0.1f);   // flash
                I.Emit(I.sparks, at + Vector3.up * 0.3f, new Color(1f, 0.55f, 0.15f), 12, 2.5f);                             // fireball
                I.Emit(I.sparks, at, new Color(1f, 0.85f, 0.4f), 24, 8f);                                                       // sparks
                I.Emit(I.smoke, at + Vector3.up * 0.4f, new Color(0.3f, 0.28f, 0.3f, 0.85f), 10, 1.8f);
            }
            if (I.impactDust) I.Emit(I.dust, at, new Color(0.7f, 0.55f, 0.4f, 0.9f), 22, radius * 1.6f);
            Shake(0.16f);
        }

        /// <summary>A tower or machine was just built. Its card flies from the build cards to the tile.
        /// Machine: the card burns away while the model burns in.
        /// Tower (waitFor set): the card lies down on the tile and waits, with a hint, until the first poker
        /// card is put on the tower (TowerArmed); only then the card burns away and the tower burns in
        /// (Wong, 2026-11-24: it shows the player that a tower needs a poker card).</summary>
        public static void Built(Transform t, Sprite face = null, Tower waitFor = null)
        {
            if (I == null || !I.On(I.buildDissolve) || t == null) return;
            Vector3 end = t.position;
            float flight = face != null ? I.buildCardFlight : 0f;
            WorldCard card = null;
            if (face != null && Camera.main != null)
            {
                var ray = Camera.main.ScreenPointToRay(new Vector2(Screen.width * 0.1f, Screen.height * 0.15f));
                card = WorldCard.Create(null, ray.GetPoint(10f), waitFor != null ? 1.6f : 1.2f, 30);
                card.SetSprite(face);
            }

            if (waitFor != null && card != null)
            {
                // the model stays hidden; the card lands flat on the tile
                ModelDissolve.Run(t.gameObject, 1f, 1f, 1e6f, false);
                waitFor.Pending = card.gameObject;
                card.transform.SetParent(t, true);
                Vector3 rest = end + Vector3.up * 0.04f;
                var flat = Quaternion.Euler(90f, Random.Range(-8f, 8f), 0f);
                var s = DOTween.Sequence().SetLink(card.gameObject);
                s.Append(card.transform.DOMove(rest + Vector3.up * 1.2f, flight).SetEase(Ease.OutQuad));
                s.AppendCallback(() => { if (card != null) { card.Billboard = false; card.transform.rotation = Quaternion.Euler(60f, 0f, 0f); } });
                s.Append(card.transform.DOMove(rest, 0.18f).SetEase(Ease.InQuad));
                s.Join(card.transform.DORotateQuaternion(flat, 0.18f));
                s.AppendCallback(() =>
                {
                    if (I == null || card == null) return;
                    I.Emit(I.dust, rest + Vector3.up * 0.1f, new Color(0.8f, 0.75f, 0.65f, 0.8f), 8, 1.6f);
                    Shake(0.04f);
                    Hint(card.transform, "ADD A POKER CARD");
                    card.transform.DOScale(1.06f, 0.6f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(card.gameObject);
                });
                return;
            }

            if (card != null)
                card.transform.DOMove(end + Vector3.up * 1.6f, flight).SetEase(Ease.OutQuad).SetLink(card.gameObject)
                    .OnComplete(() => { if (card != null) card.DissolveOut(true, I != null ? I.buildDissolveTime : 0.5f); });
            Reveal(t, flight);
        }

        /// <summary>The first poker card reached a tower whose card is still waiting on the tile.</summary>
        public static void TowerArmed(Tower t)
        {
            if (t == null || t.Pending == null) return;
            var go = t.Pending;
            t.Pending = null;
            var card = go.GetComponent<WorldCard>();
            go.transform.DOKill();
            var hint = go.transform.Find("Hint");
            if (hint != null) Object.Destroy(hint.gameObject);
            if (card != null) card.DissolveOut(true, I != null ? I.buildDissolveTime : 0.5f);
            else Object.Destroy(go);
            if (I == null || !I.juiceOn) { var md = t.GetComponent<ModelDissolve>(); if (md != null) ModelDissolve.Run(t.gameObject, 0f, 0f, 0.01f, false); return; }
            Reveal(t.transform, 0f);
        }

        /// <summary>The model burns in (with a small scale pop), then dust and a bump.</summary>
        static void Reveal(Transform t, float delay)
        {
            Vector3 end = t.position;
            t.localScale = Vector3.one * 0.85f;
            t.DOScale(1f, 0.5f).SetDelay(delay).SetEase(Ease.OutBack).SetLink(t.gameObject);
            ModelDissolve.Run(t.gameObject, 1f, 0f, I.buildDissolveTime, false, delay, () =>
            {
                if (I == null || t == null) return;
                I.Emit(I.dust, end + Vector3.up * 0.25f, new Color(0.8f, 0.75f, 0.65f, 0.9f), 14, 3f);
                Shake(0.08f);
            });
        }

        /// <summary>A small pulsing line of text lying on the table above a card (player guidance).</summary>
        static void Hint(Transform card, string text)
        {
            var go = new GameObject("Hint");
            go.transform.SetParent(card, false);
            go.transform.localPosition = new Vector3(0f, -1.25f, -0.01f);   // under the card (card space: y = up on the card)
            var t = go.AddComponent<TextMeshPro>();
            UiKit.SetFont(t, UiKit.Number);
            t.text = text;
            t.fontSize = 3.6f;
            t.color = new Color(1f, 0.85f, 0.3f);
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.rectTransform.sizeDelta = new Vector2(5f, 0.8f);
            t.sortingOrder = 31;
            DOTween.To(() => t.alpha, a => t.alpha = a, 0.35f, 0.7f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(go);
        }

        // ================= watched events =================

        void PhaseChanged(TDGame.Phase from, TDGame.Phase to, TDGame g)
        {
            // wave over: the camera glides back to its start view (Wong, 2026-11-24)
            if (from == TDGame.Phase.Wave && cam != null) { var pan = cam.GetComponent<CameraDragPan>(); if (pan != null) pan.ResetView(); }
            if (!On(banners) || banner == null) return;
            if (to == TDGame.Phase.Wave) ShowBanner($"WAVE {g.WaveInAnte}", new Color(1f, 0.85f, 0.3f));
            else if (from == TDGame.Phase.Wave && to == TDGame.Phase.Shop) ShowBanner("WAVE CLEARED!", new Color(0.45f, 0.95f, 0.5f));
            else if (to == TDGame.Phase.Deploy && from == TDGame.Phase.Shop && g.Ante != lastAnte) ShowBanner($"LEVEL {g.Ante}", new Color(1f, 0.6f, 0.25f));
        }

        void BaseHit(int lost)
        {
            if (!On(baseHit)) return;
            Shake(0.25f + 0.1f * lost);
            if (castle != null)
            {
                castle.DOComplete();
                castle.DOShakePosition(0.4f, new Vector3(0.25f, 0.05f, 0.25f), 25, 90f).SetLink(castle.gameObject);
                Flasher.For(castle.gameObject, flashMat).Flash(0.08f);
            }
            if (vignette != null)
            {
                vignette.DOKill();
                var c = vignette.color; c.a = 0.55f; vignette.color = c;
                var v = vignette;
                DOTween.To(() => v.color.a, a => { var k = v.color; k.a = a; v.color = k; }, 0f, 0.6f).SetUpdate(true).SetTarget(v).SetLink(v.gameObject);
            }
            HeartBar.Hit();
            if (worldHud != null && worldHud.lives != null && worldHud.lives.gameObject.activeInHierarchy)
            {
                var txt = worldHud.lives;
                Color baseCol = txt.color;
                txt.color = new Color(1f, 0.3f, 0.25f);
                DOTween.To(() => txt.color, x => txt.color = x, baseCol, 0.6f).SetLink(txt.gameObject);
            }
        }

        void ShowBanner(string text, Color color)
        {
            bannerSeq?.Kill();
            banner.text = text;
            banner.color = color;
            banner.alpha = 0f;
            var tr = banner.rectTransform;
            tr.localScale = Vector3.one * 1.8f;
            bannerSeq = DOTween.Sequence().SetUpdate(true).SetLink(banner.gameObject)
                .Append(DOTween.To(() => banner.alpha, a => banner.alpha = a, 1f, 0.15f))
                .Join(tr.DOScale(1f, 0.3f).SetEase(Ease.OutBack))
                .AppendInterval(0.8f)
                .Append(DOTween.To(() => banner.alpha, a => banner.alpha = a, 0f, 0.3f))
                .Join(tr.DOScale(0.9f, 0.3f));
        }

        // ================= particles =================

        void MakeParticles()
        {
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(16f, 16f)) / 16f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d * d)));
                }
            tex.Apply();
            particleMat = new Material(Shader.Find("Sprites/Default")) { mainTexture = tex };
            sparks = MakeSystem("JuiceSparks", 0.4f, 0.8f, 0.2f, 0.4f, 1.2f, 0.05f);
            dust = MakeSystem("JuiceDust", 0.5f, 0.9f, 0.5f, 1.0f, 0.1f, 0.2f);
            smoke = MakeSystem("JuiceSmoke", 0.6f, 1.1f, 0.6f, 1.2f, -0.15f, 0.15f);
        }

        ParticleSystem MakeSystem(string name, float lifeMin, float lifeMax, float sizeMin, float sizeMax, float gravity, float radius)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.maxParticles = 800;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startSpeed = 1f;
            main.gravityModifier = gravity;
            var em = ps.emission; em.enabled = false;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = radius;
            var size = ps.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            var col = ps.colorOverLifetime; col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            var limit = ps.limitVelocityOverLifetime; limit.enabled = true; limit.dampen = 0.12f; limit.limit = 2f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = particleMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
            return ps;
        }

        /// <summary>Burst of n particles flying out in all directions (flattened a bit, so they spread over the table).</summary>
        void Emit(ParticleSystem ps, Vector3 at, Color color, int n, float speed)
        {
            if (ps == null) return;
            n = Mathf.Max(1, Mathf.RoundToInt(n * Mathf.Clamp(strength, 0.3f, 2f)));
            for (int i = 0; i < n; i++)
            {
                Vector3 v = Random.onUnitSphere; v.y = Mathf.Abs(v.y) * 0.8f + 0.2f;
                EmitOne(ps, at, color, v * speed * Random.Range(0.4f, 1f), -1f, -1f);
            }
        }

        void EmitOne(ParticleSystem ps, Vector3 at, Color color, Vector3 velocity, float size, float life)
        {
            var ep = new ParticleSystem.EmitParams { position = at, velocity = velocity, startColor = color, applyShapeToPosition = false };
            if (size > 0f) ep.startSize = size;
            if (life > 0f) ep.startLifetime = life;
            ps.Emit(ep, 1);
        }

        static Color SuitColor(Suit s) =>
            s == Suit.Hearts || s == Suit.Diamonds ? new Color(1f, 0.3f, 0.3f) : new Color(1f, 0.92f, 0.7f);
    }
}
