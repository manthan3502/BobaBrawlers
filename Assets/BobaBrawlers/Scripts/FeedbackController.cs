using System.Collections;
using UnityEngine;

namespace BobaBrawlers
{
    public sealed class FeedbackController : MonoBehaviour
    {
        public AudioClip playerFallVocal;
        public Material sparkMaterial;
        public float SfxVolume { get; private set; }
        public float MusicVolume { get; private set; }
        public bool MusicPlaying => music && music.isPlaying;
        public AudioSource MusicSource => music;
        public AudioSource FallSource => vocal;
        public int PlayerFallPlayCount { get; private set; }
        AudioSource source, vocal, music, uiSound, charge;
        AudioClip dash, bump, fall, ko, click, victory, defeat, menuMusic, gameplayMusic;
        readonly ParticleSystem[] pool = new ParticleSystem[16];
        int nextParticle;
        Coroutine hitStop;
        float lastImpact;
        void Awake()
        {
            SfxVolume = PlayerPrefs.GetFloat("Boba.SFX", .65f); MusicVolume = PlayerPrefs.GetFloat("Boba.Music", .5f);
            source = Source(false); vocal = Source(true); music = Source(false); uiSound = Source(true); charge = Source(false);
            vocal.priority = 0;
            dash = Load("Dash"); bump = Load("Impact"); fall = Load("ComicFall"); ko = Load("KO"); click = Load("Button"); victory = Load("Victory"); defeat = Load("Defeat");
            menuMusic = Load("MenuLoop"); gameplayMusic = Load("BrawlLoop");
            music.loop = true; music.volume = MusicVolume * .42f;
            charge.clip = Load("Charge"); charge.loop = true; charge.volume = SfxVolume * .22f;
            for (int i = 0; i < pool.Length; i++) pool[i] = CreateParticles(i);
            SetMusicContext(true);
        }
        AudioClip Load(string name) { return Resources.Load<AudioClip>("Audio/" + name); }
        AudioSource Source(bool ignorePause)
        { var result = gameObject.AddComponent<AudioSource>(); result.playOnAwake = false; result.ignoreListenerPause = ignorePause; return result; }
        public void SetMusicContext(bool menu)
        {
            var clip = menu ? menuMusic : gameplayMusic;
            if (music.clip != clip) { music.clip = clip; if (clip) music.Play(); }
        }
        public void SetSfx(float value)
        { SfxVolume = Mathf.Clamp01(value); vocal.volume = SfxVolume; charge.volume = SfxVolume * .22f; PlayerPrefs.SetFloat("Boba.SFX", SfxVolume); }
        public void SetMusic(float value)
        { MusicVolume = Mathf.Clamp01(value); music.volume = MusicVolume * .42f; PlayerPrefs.SetFloat("Boba.Music", MusicVolume); }
        void Update()
        {
            music.volume = Mathf.MoveTowards(music.volume, MusicVolume * .42f * (vocal.isPlaying ? .32f : 1), Time.unscaledDeltaTime * .7f);
            var game = GameManager.Instance; bool charging = game && game.Running && game.Player && game.Player.Alive && game.Player.Dash.IsCharging;
            if (charging) { if (!charge.isPlaying && charge.clip) charge.Play(); charge.pitch = Mathf.Lerp(.8f, 1.45f, game.Player.Dash.Charge); }
            else if (charge.isPlaying) charge.Stop();
        }
        public void Button() { if (click) uiSound.PlayOneShot(click, SfxVolume * .5f); }
        public void RoundStart() { if (ko) source.PlayOneShot(ko, SfxVolume * .4f); }
        public void Impact(Vector3 at)
        {
            if (Time.time - lastImpact > .035f && bump) source.PlayOneShot(bump, SfxVolume); lastImpact = Time.time;
            Burst(at, Palette.Orange, 22, .13f, 4.3f);
            Burst(at, Color.white, 8, .07f, 5.5f);
            var game = GameManager.Instance; if (game && game.Presentation) game.Presentation.Shake();
            if (hitStop == null && game.Running && !GamePreferences.ReducedMotion) hitStop = StartCoroutine(HitStop());
        }
        IEnumerator HitStop()
        {
            Time.timeScale = .08f; yield return new WaitForSecondsRealtime(.035f);
            if (GameManager.Instance && GameManager.Instance.Running) Time.timeScale = 1; hitStop = null;
        }
        public void Dash(Vector3 at, Vector3 direction)
        { if (dash) source.PlayOneShot(dash, SfxVolume); Burst(at - direction * .4f, Palette.Blue, 12, .08f, 2.8f); }
        public void Knockout(Vector3 at, bool player, Transform model = null)
        {
            if (player) { vocal.Stop(); vocal.clip = playerFallVocal; vocal.volume = SfxVolume; if (vocal.clip) { vocal.Play(); PlayerFallPlayCount++; } }
            else if (ko) source.PlayOneShot(ko, SfxVolume);
            Burst(new Vector3(at.x, .3f, at.z), Palette.Cream, 35, .16f, 4.5f);
            if (model)
            {
                var ghost = Instantiate(model.gameObject, new Vector3(at.x, .25f, at.z), model.rotation);
                ghost.name = "Comic falling echo"; ghost.transform.localScale = model.lossyScale;
                foreach (var renderer in ghost.GetComponentsInChildren<Renderer>()) renderer.enabled = true;
                ghost.AddComponent<FallingEcho>();
            }
            if (GameManager.Instance.Presentation) GameManager.Instance.Presentation.Shake(.12f);
        }
        public void Result(bool won, bool champion)
        {
            if (charge.isPlaying) charge.Stop();
            var clip = won ? victory : defeat; if (clip) source.PlayOneShot(clip, SfxVolume);
            if (won) StartCoroutine(Confetti(champion));
        }
        IEnumerator Confetti(bool champion)
        {
            for (int i = 0; i < (champion ? 12 : 5); i++)
            {
                Burst(new Vector3(-4.5f + i % 3 * 4.5f, 3, 0), i % 3 == 0 ? Palette.Lime : i % 3 == 1 ? Palette.Orange : Palette.Blue, 35, .1f, 4);
                yield return new WaitForSeconds(.16f);
                if (!GameManager.Instance || GameManager.Instance.State != MatchState.Victory) yield break;
            }
        }
        ParticleSystem CreateParticles(int index)
        {
            var go = new GameObject("Pooled comic burst " + index); go.transform.SetParent(transform);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.duration = 1; main.loop = false; main.startLifetime = .65f; main.startSpeed = 4; main.startSize = .1f; main.gravityModifier = .8f; main.maxParticles = 64;
            var emission = ps.emission; emission.rateOverTime = 0;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .18f;
            var rotation = ps.rotationOverLifetime; rotation.enabled = true; rotation.z = 5;
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0));
            if (sparkMaterial) ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = sparkMaterial;
            return ps;
        }
        void Burst(Vector3 at, Color color, int count, float size, float speed)
        {
            var ps = pool[nextParticle++ % pool.Length]; ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); ps.transform.position = at;
            var main = ps.main; main.startColor = color; main.startSize = size; main.startSpeed = speed;
            ps.Play(); ps.Emit(count);
        }
    }
}
