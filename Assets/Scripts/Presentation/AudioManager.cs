using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Müzik ve ses efektleri (videodakiyle aynı sesler). Kendiliğinden oluşur, sahneler arasında kalır.
/// Sesler: Assets/Resources/Audio/
///   Müzik: music_future (gelecek), music_past (geçmiş), music_hope / music_dry / music_final (final)
///   Efekt: sfx_whoosh (portal), sfx_chime (bildirim), sfx_click (seçim), sfx_page (gazete),
///          sfx_door (kapı), sfx_cabinet (dolap kapağı)
/// </summary>
public class AudioManager : MonoBehaviour
{
    static AudioManager instance;

    [Range(0f, 1f)] public float musicVolume = 0.55f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    AudioSource musicA, musicB, sfx;
    bool aActive = true;
    string currentMusic;
    readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    Coroutine fadeRoutine;

    /// <summary>Sahneye göre müziği otomatik seçme kapalıysa (ör. final sinematiği) true.</summary>
    public static bool MusicOverride;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { instance = null; MusicOverride = false; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        if (instance != null) return;
        var go = new GameObject("AudioManager");
        instance = go.AddComponent<AudioManager>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        musicA = gameObject.AddComponent<AudioSource>();
        musicB = gameObject.AddComponent<AudioSource>();
        foreach (var m in new[] { musicA, musicB }) { m.loop = true; m.playOnAwake = false; m.volume = 0f; }
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        if (MusicOverride) return;
        PlayMusic(s.name.StartsWith("Past") ? "music_past" : "music_future");
    }

    AudioClip Clip(string name)
    {
        if (!cache.TryGetValue(name, out var c))
        {
            c = Resources.Load<AudioClip>("Audio/" + name);
            if (c == null) Debug.LogWarning("[Ses] Bulunamadı: Resources/Audio/" + name);
            cache[name] = c;
        }
        return c;
    }

    // ================= Genel API =================
    public static void Sfx(string name, float volume = 1f)
    {
        if (instance == null) return;
        var c = instance.Clip(name);
        if (c) instance.sfx.PlayOneShot(c, volume * instance.sfxVolume);
    }

    public static void PlayMusic(string name, float fade = 1.5f)
    {
        if (instance == null || instance.currentMusic == name) return;
        var c = instance.Clip(name);
        if (c == null) return;
        instance.currentMusic = name;
        var to = instance.aActive ? instance.musicB : instance.musicA;
        var from = instance.aActive ? instance.musicA : instance.musicB;
        instance.aActive = !instance.aActive;
        to.clip = c; to.volume = 0f; to.Play();
        if (instance.fadeRoutine != null) instance.StopCoroutine(instance.fadeRoutine);
        instance.fadeRoutine = instance.StartCoroutine(instance.Crossfade(from, to, fade));
    }

    public static void StopMusic(float fade = 1.5f)
    {
        if (instance == null) return;
        instance.currentMusic = null;
        if (instance.fadeRoutine != null) instance.StopCoroutine(instance.fadeRoutine);
        instance.fadeRoutine = instance.StartCoroutine(instance.Crossfade(instance.musicA, null, fade, instance.musicB));
    }

    IEnumerator Crossfade(AudioSource from, AudioSource to, float time, AudioSource from2 = null)
    {
        float f0 = from ? from.volume : 0f, f20 = from2 ? from2.volume : 0f;
        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            float k = t / time;
            if (from) from.volume = Mathf.Lerp(f0, 0f, k);
            if (from2) from2.volume = Mathf.Lerp(f20, 0f, k);
            if (to) to.volume = Mathf.Lerp(0f, musicVolume, k);
            yield return null;
        }
        if (from) { from.volume = 0f; from.Stop(); }
        if (from2) { from2.volume = 0f; from2.Stop(); }
        if (to) to.volume = musicVolume;
    }
}
