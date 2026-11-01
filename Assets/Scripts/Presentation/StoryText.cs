using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Videodaki ekran yazıları:
///  - Bölüm etiketi (sağ üst, altın şeritli koyu kutu): "BÖLÜM 1 · GELECEK" vb.
///  - Altyazı (üst orta, kalın italik, konturlu): "2087. Musluklardan artık su akmıyor." vb.
/// Her yazı oyun başına bir kez gösterilir (GameFlags ile hatırlanır).
/// Sahneye bağlı olanlar burada, olaya bağlı olanlar ilgili scriptlerden çağrılır.
/// </summary>
public class StoryText : MonoBehaviour
{
    static StoryText instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        if (instance != null) return;
        var go = new GameObject("StoryText");
        instance = go.AddComponent<StoryText>();
        try { instance.Build(); } catch (System.Exception e) { Debug.LogError("[StoryText] " + e); }
    }

    // ---- Bölüm etiketleri (bir kez) ----
    public const string Ch1 = "BÖLÜM 1 · GELECEK";
    public const string ChNews = "GAZETE: 2087'NİN HABERLERİ";
    public const string Ch2 = "BÖLÜM 2 · KOMŞULAR VE SEÇİMLER";
    public const string Ch3 = "BÖLÜM 3 · ZAMAN GEÇİDİ";
    public const string Ch4 = "BÖLÜM 4 · GEÇMİŞ";
    public const string ChClues = "İPUÇLARI";
    public const string ChEmirhan = "EMİRHAN: KORKU VE VİCDAN";
    public const string Ch5 = "BÖLÜM 5 · DEĞİŞEN GELECEK";

    CanvasGroup chapterGroup, captionGroup;
    TMP_Text chapterText, captionText;
    RectTransform chapterBox;
    Coroutine chapterRoutine, captionRoutine;

    /// <summary>Bölüm etiketini oyun başına bir kez gösterir.</summary>
    public static void ChapterOnce(string text, float delay = 0.3f)
    {
        string flag = "yazi_" + text;
        if (GameFlags.Has(flag) || instance == null || instance.chapterText == null) return;
        GameFlags.Set(flag);
        if (instance.chapterRoutine != null) instance.StopCoroutine(instance.chapterRoutine);
        instance.chapterRoutine = instance.StartCoroutine(instance.ShowChapter(text, delay));
    }

    /// <summary>Altyazıyı oyun başına bir kez gösterir.</summary>
    public static void CaptionOnce(string key, string text, float delay = 0.4f, float seconds = 4f)
    {
        string flag = "altyazi_" + key;
        if (GameFlags.Has(flag) || instance == null || instance.captionText == null) return;
        GameFlags.Set(flag);
        if (instance.captionRoutine != null) instance.StopCoroutine(instance.captionRoutine);
        instance.captionRoutine = instance.StartCoroutine(instance.ShowCaption(text, delay, seconds));
    }

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        if (TitleScreen.IsOpen) return; // başlık ekranı kapanınca tekrar denenir
        SceneBeats(s.name);
    }

    public static void SceneBeats(string scene)
    {
        if (scene.StartsWith("Past"))
        {
            CaptionOnce("gecmis", "Geçmiş. Ardıç Nehri, fabrikanın atıklarıyla zehirleniyor.", 0.3f, 3.2f);
            ChapterOnce(Ch4, 1.8f);
        }
        else if (scene.StartsWith("Future"))
        {
            if (NewspaperUI.HistoryChanged) ChapterOnce(Ch5, 0.4f);
            else
            {
                CaptionOnce("gelecek", "2087. Musluklardan artık su akmıyor.", 0.5f, 4f);
                ChapterOnce(Ch1, 0.3f);
            }
        }
    }

    IEnumerator ShowChapter(string text, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        chapterText.text = text;
        chapterBox.sizeDelta = new Vector2(chapterText.GetPreferredValues(text).x + 70f, 50f);
        yield return Fade(chapterGroup, 0f, 1f, 0.5f);
        yield return new WaitForSecondsRealtime(4f);
        yield return Fade(chapterGroup, 1f, 0f, 0.6f);
    }

    IEnumerator ShowCaption(string text, float delay, float seconds)
    {
        yield return new WaitForSecondsRealtime(delay);
        captionText.text = text;
        yield return Fade(captionGroup, 0f, 1f, 0.5f);
        yield return new WaitForSecondsRealtime(seconds);
        yield return Fade(captionGroup, 1f, 0f, 0.5f);
    }

    static IEnumerator Fade(CanvasGroup g, float a, float b, float time)
    {
        for (float t = 0f; t < time; t += Time.unscaledDeltaTime) { g.alpha = Mathf.SmoothStep(a, b, t / time); yield return null; }
        g.alpha = b;
    }

    void Update()
    {
        // Diyalog, gazete, final vb. açıkken yazılar gizlenir
        bool hide = EndingCinematic.IsPlaying || TitleScreen.IsOpen || StoryCard.IsOpen;
        if (chapterGroup) chapterGroup.gameObject.SetActive(!hide);
        if (captionGroup) captionGroup.gameObject.SetActive(!hide);
    }

    void Build()
    {
        DontDestroyOnLoad(gameObject);
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 880;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Bölüm etiketi
        var box = new GameObject("Chapter", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        box.transform.SetParent(transform, false);
        chapterBox = (RectTransform)box.transform;
        chapterBox.anchorMin = chapterBox.anchorMax = chapterBox.pivot = new Vector2(1f, 1f);
        chapterBox.anchoredPosition = new Vector2(-36f, -32f);
        box.GetComponent<Image>().color = new Color(0.08f, 0.067f, 0.059f, 0.78f);
        chapterGroup = box.GetComponent<CanvasGroup>(); chapterGroup.alpha = 0f; chapterGroup.blocksRaycasts = false;
        var bar = new GameObject("Bar", typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(box.transform, false);
        var br = (RectTransform)bar.transform; br.anchorMin = new Vector2(0, 0); br.anchorMax = new Vector2(0, 1);
        br.pivot = new Vector2(0, 0.5f); br.sizeDelta = new Vector2(6, 0); br.anchoredPosition = Vector2.zero;
        bar.GetComponent<Image>().color = new Color(1f, 0.83f, 0.42f);
        chapterText = Text("Text", box.transform, 26, new Color(0.96f, 0.93f, 0.86f), FontStyles.Bold);
        var cr = chapterText.rectTransform; cr.anchorMin = Vector2.zero; cr.anchorMax = Vector2.one;
        cr.offsetMin = new Vector2(26, 0); cr.offsetMax = new Vector2(-10, 0);
        chapterText.alignment = TextAlignmentOptions.MidlineLeft;

        // Altyazı
        var cap = new GameObject("Caption", typeof(RectTransform), typeof(CanvasGroup));
        cap.transform.SetParent(transform, false);
        var capr = (RectTransform)cap.transform;
        capr.anchorMin = capr.anchorMax = capr.pivot = new Vector2(0.5f, 1f);
        capr.anchoredPosition = new Vector2(0, -170f); capr.sizeDelta = new Vector2(1500, 120);
        captionGroup = cap.GetComponent<CanvasGroup>(); captionGroup.alpha = 0f; captionGroup.blocksRaycasts = false;
        captionText = Text("Text", cap.transform, 40, new Color(0.98f, 0.96f, 0.9f), FontStyles.Bold | FontStyles.Italic);
        var tr = captionText.rectTransform; tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = tr.offsetMax = Vector2.zero;
        captionText.alignment = TextAlignmentOptions.Top;
        captionText.textWrappingMode = TextWrappingModes.Normal;
        if (captionText.font != null) { try { captionText.outlineWidth = 0.22f; captionText.outlineColor = new Color32(0, 0, 0, 255); } catch { } }
    }

    static TMP_Text Text(string name, Transform parent, float size, Color c, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size; t.color = c; t.fontStyle = style; t.raycastTarget = false;
        return t;
    }
}
