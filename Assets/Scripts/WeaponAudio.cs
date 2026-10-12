using UnityEngine;

/// <summary>
/// Small procedural sound set so the prototype has audible feedback without external audio assets.
/// Replace these generated clips with authored audio during the art/audio pass.
/// </summary>
public sealed class WeaponAudio : MonoBehaviour
{
    private AudioSource effects;
    private AudioClip shotClip;
    private AudioClip reloadClip;
    private AudioClip stepClip;
    private AudioClip hitClip;
    private float nextStepAllowed;

    void Awake()
    {
        effects = gameObject.AddComponent<AudioSource>();
        effects.playOnAwake = false;
        effects.spatialBlend = 0f;
        effects.volume = .55f;
        shotClip = MakeNoise("Procedural Shot", .14f, 170f, .06f, .85f);
        reloadClip = MakeNoise("Procedural Reload", .22f, 780f, .025f, .45f);
        stepClip = MakeNoise("Procedural Footstep", .09f, 95f, .018f, .38f);
        hitClip = MakeNoise("Procedural Hit", .08f, 420f, .02f, .5f);
    }

    AudioClip MakeNoise(string clipName, float duration, float frequency, float toneMix, float volume)
    {
        int sampleRate = 22050;
        int count = Mathf.CeilToInt(sampleRate * duration);
        var samples = new float[count];
        float phase = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = Mathf.Exp(-t * (clipName.Contains("Shot") ? 26f : 18f));
            phase += 2f * Mathf.PI * frequency / sampleRate;
            float tone = Mathf.Sin(phase);
            float noise = Random.Range(-1f, 1f);
            samples[i] = (tone * toneMix + noise * (1f - toneMix)) * envelope * volume;
        }
        var clip = AudioClip.Create(clipName, count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public void PlayShot() { Play(shotClip, .85f, Random.Range(.94f, 1.04f)); }
    public void PlayReload() { Play(reloadClip, .55f, 1f); }
    public void PlayStep()
    {
        if (Time.time < nextStepAllowed) return;
        nextStepAllowed = Time.time + .28f;
        Play(stepClip, .34f, Random.Range(.9f, 1.1f));
    }
    public void PlayHit() { Play(hitClip, .45f, 1f); }

    void Play(AudioClip clip, float volume, float pitch)
    {
        if (effects == null || clip == null) return;
        effects.pitch = pitch;
        effects.PlayOneShot(clip, volume);
    }

    void OnDestroy()
    {
        if (shotClip) Destroy(shotClip);
        if (reloadClip) Destroy(reloadClip);
        if (stepClip) Destroy(stepClip);
        if (hitClip) Destroy(hitClip);
    }
}
