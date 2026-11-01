using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// FİNAL SİNEMATİĞİ
/// Geçmiş değiştirildikten sonra geleceğe dönülüp değişmiş gazetenin son sayfası okunup kapatılınca başlar.
///   1) Her şey bir süre düzeldi   (Resources/Ending/gelecek_duzeldi.png, yavaş kaydırma)
///   2) YILLAR SONRA...
///   3) Su kıtlığının geri dönüşü   (Resources/Ending/yillar_sonra_kuraklik.png, yakınlaşma + toz + renk değişimi)
///   4) Oyunun ana mesajı
///   5) OYUN SONU  (tıklayınca oyun baştan başlar)
/// Sahneler arası geçiş: sol fare tuşu (Space / Enter da çalışır). Yazı belirirken tıklamak yazıyı hemen tamamlar.
/// Arayüz kodla kurulur, sahneye bir şey eklemek gerekmez.
/// </summary>
public class EndingCinematic : MonoBehaviour
{
    public const string SeenFlag = "final_goruldu";

    static EndingCinematic instance;
    public static bool IsPlaying => instance != null && instance.playing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => instance = null;

    public static void Play()
    {
        if (IsPlaying) return;
        GameFlags.Set(SeenFlag);
        if (instance == null)
        {
            var go = new GameObject("EndingCinematic");
            instance = go.AddComponent<EndingCinematic>();
            instance.Build();
        }
        instance.StartCoroutine(instance.Run());
    }

    // ---- arayüz parçaları ----
    RectTransform canvasRect, imageRect;
    RawImage image, black;
    CanvasGroup imageGroup, shade;
    RectTransform textRoot;
    RectTransform dustRoot;
    readonly List<Dust> dust = new List<Dust>();
    bool playing;

    // ---- kamera hareketi ----
    enum Motion { None, Pan, ZoomDrift }
    Motion motion;
    float motionStart;
    float imageAspect = 3.58f;

    struct Dust { public RectTransform rt; public Image img; public Vector2 vel; public float phase; }

    static readonly Color Gold = new Color(1f, 0.83f, 0.42f);

    // ================= Akış =================
    IEnumerator Run()
    {
        playing = true;
        AudioManager.MusicOverride = true;
        AudioManager.PlayMusic("music_hope", 2.5f);
        ClearText();
        imageGroup.alpha = 0f;
        black.color = new Color(0, 0, 0, 0);
        transform.GetChild(0).gameObject.SetActive(true);

        // ekran yumuşakça kararır
        yield return Fade(black, 0f, 1f, 1.2f);

        // ---------- 1. Sahne: Her şey bir süre düzeldi ----------
        ShowImage("Ending/gelecek_duzeldi", Motion.Pan, Color.white);
        shade.alpha = 1f;
        yield return Fade(black, 1f, 0f, 1.6f);
        yield return Lines(new[]
        {
            Line("Bir süreliğine her şey düzeldi...", 54, Gold, FontStyles.Bold | FontStyles.Italic),
            Line("Nehirler yeniden akmaya, toprak yeniden canlanmaya başladı.", 38, Color.white),
            Line("Su kıtlığı azaldı. İnsanlık yeniden umut buldu.", 38, Color.white),
        }, -230f, 1.1f);
        yield return WaitClick();
        yield return Fade(black, 0f, 1f, 1.2f);
        ClearText(); HideImage(); Clear(black); // arka plan zaten siyah; perdeyi kaldır ki yazı görünsün

        // ---------- 2. Sahne: Yıllar sonra ----------
        AudioManager.StopMusic(1.5f);
        yield return Lines(new[] { Line("YILLAR SONRA...", 96, Color.white, FontStyles.Bold) }, 0f, 0f, fadeTime: 2.5f);
        yield return WaitClick();
        yield return FadeText(1f, 0f, 1.2f);
        ClearText();

        // ---------- 3. Sahne: Su kıtlığının geri dönüşü ----------
        AudioManager.PlayMusic("music_dry", 2f);
        ShowImage("Ending/yillar_sonra_kuraklik", Motion.ZoomDrift, Color.white);
        SpawnDust(true);
        shade.alpha = 1f;
        yield return Fade(black, 1f, 0f, 2f);
        yield return Lines(new[]
        {
            Line("Yıllar geçti...", 50, new Color(1f, 0.72f, 0.45f), FontStyles.Bold | FontStyles.Italic),
            Line("İnsanlık geçmişte yaptığı hataları unuttu.", 38, Color.white),
            Line("Su kaynakları yeniden tükendi.", 38, Color.white),
            Line("Ve su kıtlığı bir kez daha başladı.", 40, new Color(1f, 0.6f, 0.45f), FontStyles.Bold),
        }, -210f, 1.2f);
        yield return WaitClick();
        yield return Fade(black, 0f, 1f, 1.4f);
        ClearText(); HideImage(); SpawnDust(false); Clear(black);

        // ---------- 4. Sahne: Oyunun ana mesajı ----------
        AudioManager.PlayMusic("music_final", 2f);
        yield return Lines(new[]
        {
            Line("GELECEĞİ KURTARMAK YETMEZ.", 66, Color.white, FontStyles.Bold),
            Line("Geçmişi değiştirerek kendi anımızı ve belki yakın geleceği kurtarabiliriz. Ama ana kural değişmez.", 32, new Color(0.88f, 0.88f, 0.88f)),
            Line("Eğer tüketim alışkanlıklarımızı ve sistemimizi temelde dönüştürmezsek, zamanı ne kadar geri alırsak alalım, kaçınılmaz son sadece birkaç nesil ötelenir.", 32, new Color(0.88f, 0.88f, 0.88f)),
            Line("Doğayı korumak, geçmişte bir kez verilen bir karar değil; her gün yeniden üstlenilmesi gereken bir sorumluluktur.", 38, Gold, FontStyles.Bold),
            Line("Sürdürülebilirlik bir 'kurtarma' operasyonu değil, kalıcı bir yaşam biçimidir.", 42, Gold, FontStyles.Bold),
        }, 0f, 1.6f, fadeTime: 1.4f, spacing: 34f, width: 1500f);
        yield return WaitClick();
        yield return FadeText(1f, 0f, 1.4f);
        ClearText();

        // ---------- 5. Sahne: Oyun sonu ----------
        yield return Lines(new[] { Line("OYUN SONU", 110, Color.white, FontStyles.Bold) }, 0f, 0f, fadeTime: 2.5f);
        yield return new WaitForSecondsRealtime(1.5f);
        yield return FadeText(1f, 0f, 1.0f);
        ClearText();

        // ---------- Kapanış: DÖNGÜ logosu (videodaki gibi) ----------
        AudioManager.Sfx("sfx_chime");
        var logo = new GameObject("Logo", typeof(RectTransform), typeof(RawImage), typeof(CanvasGroup));
        logo.transform.SetParent(textRoot, false);
        var lr = (RectTransform)logo.transform;
        lr.anchorMin = lr.anchorMax = lr.pivot = new Vector2(0.5f, 0.5f);
        lr.anchoredPosition = new Vector2(0, 210); lr.sizeDelta = new Vector2(360, 360);
        logo.GetComponent<RawImage>().texture = Resources.Load<Texture2D>("Portal/TimePortal");
        logo.GetComponent<RawImage>().raycastTarget = false;
        var lg = logo.GetComponent<CanvasGroup>(); lg.alpha = 0f;
        var title = Line("DÖNGÜ", 150, Gold, FontStyles.Bold);
        Place(title.rectTransform, new Vector2(0, -60), 1200f);
        var motto = Line("Doğayı korumak her gün yeniden üstlenilen bir sorumluluktur.", 36, new Color(0.92f, 0.89f, 0.82f), FontStyles.Italic);
        Place(motto.rectTransform, new Vector2(0, -190), 1500f);
        for (float lt = 0f; lt < 1.8f; lt += Time.unscaledDeltaTime)
        {
            lg.alpha = Mathf.Clamp01(lt / 1.2f);
            title.GetComponent<CanvasGroup>().alpha = Mathf.Clamp01(lt / 1.2f);
            motto.GetComponent<CanvasGroup>().alpha = Mathf.Clamp01((lt - 0.8f) / 1.0f);
            lr.localRotation = Quaternion.Euler(0, 0, -Time.unscaledTime * 8f);
            yield return null;
        }
        motto.GetComponent<CanvasGroup>().alpha = 1f;
        var hint = Line("Yeniden başlamak için tıkla", 26, new Color(1f, 1f, 1f, 0.55f), FontStyles.Italic);
        Place(hint.rectTransform, new Vector2(0, -420), 900f);
        yield return FadeOne(hint, 1.2f);
        while (!Clicked()) { lr.localRotation = Quaternion.Euler(0, 0, -Time.unscaledTime * 8f); yield return null; }
        yield return null;

        RestartGame();
    }

    void RestartGame()
    {
        StoryProgress.ResetForNewGame();
        if (GameManager.Instance != null) GameManager.Instance.ResetState();
        playing = false;
        transform.GetChild(0).gameObject.SetActive(false);
        ClearText();
        AudioManager.MusicOverride = false;
        SceneManager.LoadScene(0);
        TitleScreen.Show();
    }

    // ================= Görsel ve hareket =================
    void ShowImage(string path, Motion m, Color tint)
    {
        var tex = Resources.Load<Texture2D>(path);
        if (tex == null) Debug.LogWarning("[Final] Görsel bulunamadı: Assets/Resources/" + path + ".png");
        image.texture = tex;
        image.color = tint;
        imageAspect = tex != null ? (float)tex.width / tex.height : 16f / 9f;
        motion = m;
        motionStart = Time.unscaledTime;
        imageGroup.alpha = 1f;
        UpdateMotion();
    }

    void HideImage() { imageGroup.alpha = 0f; motion = Motion.None; shade.alpha = 0f; }

    void Update()
    {
        if (!playing) return;
        UpdateMotion();
        UpdateDust();
    }

    void UpdateMotion()
    {
        if (motion == Motion.None || canvasRect == null) return;
        float t = Time.unscaledTime - motionStart;
        Vector2 screen = canvasRect.rect.size;

        float zoom = motion == Motion.ZoomDrift ? Mathf.Lerp(1.0f, 1.12f, Mathf.Clamp01(t / 30f)) : 1.03f;
        // ekranı tamamen kaplayacak boyut ("cover")
        float h = screen.y * zoom;
        float w = h * imageAspect;
        if (w < screen.x * zoom) { w = screen.x * zoom; h = w / imageAspect; }
        imageRect.sizeDelta = new Vector2(w, h);

        float overflowX = Mathf.Max(0f, (w - screen.x) * 0.5f);
        float overflowY = Mathf.Max(0f, (h - screen.y) * 0.5f);
        if (motion == Motion.Pan)
        {
            // görselin solundan sağına yavaşça kay (gidip gelir)
            float p = Mathf.SmoothStep(0f, 1f, Mathf.PingPong(t / 28f, 1f));
            imageRect.anchoredPosition = new Vector2(Mathf.Lerp(overflowX, -overflowX, p), 0f);
        }
        else
        {
            // kuraklık: sağdan sola ağır kayma, hafif dikey salınım ve sıcak renge kayma
            float p = Mathf.SmoothStep(0f, 1f, Mathf.PingPong(t / 34f, 1f));
            imageRect.anchoredPosition = new Vector2(Mathf.Lerp(-overflowX, overflowX * 0.6f, p), Mathf.Sin(t * 0.25f) * overflowY * 0.5f);
            float heat = Mathf.Clamp01(t / 12f);
            image.color = Color.Lerp(Color.white, new Color(1f, 0.86f, 0.68f), heat);
        }
    }

    void SpawnDust(bool on)
    {
        foreach (var d in dust) if (d.rt) Destroy(d.rt.gameObject);
        dust.Clear();
        if (!on) return;
        for (int i = 0; i < 70; i++)
        {
            var go = new GameObject("Dust", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(dustRoot, false);
            var rt = (RectTransform)go.transform;
            float s = Random.Range(3f, 9f);
            rt.sizeDelta = new Vector2(s, s);
            rt.anchoredPosition = new Vector2(Random.Range(-1000f, 1000f), Random.Range(-560f, 560f));
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.color = new Color(0.95f, 0.78f, 0.55f, 0f);
            dust.Add(new Dust { rt = rt, img = img, vel = new Vector2(Random.Range(25f, 80f), Random.Range(-8f, 18f)), phase = Random.value * 10f });
        }
    }

    void UpdateDust()
    {
        if (dust.Count == 0) return;
        Vector2 half = canvasRect.rect.size * 0.5f + new Vector2(20f, 20f);
        float dt = Time.unscaledDeltaTime, t = Time.unscaledTime;
        foreach (var d in dust)
        {
            var p = d.rt.anchoredPosition + d.vel * dt + new Vector2(0f, Mathf.Sin(t * 1.3f + d.phase) * 10f * dt);
            if (p.x > half.x) p.x = -half.x;
            if (p.y > half.y) p.y = -half.y; else if (p.y < -half.y) p.y = half.y;
            d.rt.anchoredPosition = p;
            var c = d.img.color; c.a = (0.25f + 0.25f * Mathf.Sin(t * 2f + d.phase)) * imageGroup.alpha;
            d.img.color = c;
        }
    }

    // ================= Yazılar =================
    TMP_Text Line(string text, float size, Color color, FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject("Line", typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(textRoot, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
        if (t.font != null) { try { t.outlineWidth = 0.18f; t.outlineColor = new Color32(0, 0, 0, 255); } catch { } }
        go.GetComponent<CanvasGroup>().alpha = 0f;
        return t;
    }

    static void Place(RectTransform r, Vector2 pos, float width)
    {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = new Vector2(width, 100f);
    }

    /// <summary>Satırları dikeyde ortalayarak yerleştirir ve sırayla yavaşça belirtir. Tıklamak hepsini hemen gösterir.</summary>
    IEnumerator Lines(TMP_Text[] lines, float centerY, float gap, float fadeTime = 1.2f, float spacing = 22f, float width = 1400f)
    {
        float total = 0f;
        var heights = new float[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            lines[i].rectTransform.sizeDelta = new Vector2(width, 100f);
            heights[i] = lines[i].GetPreferredValues(lines[i].text, width, 0f).y;
            total += heights[i] + (i > 0 ? spacing : 0f);
        }
        float y = centerY + total * 0.5f;
        for (int i = 0; i < lines.Length; i++)
        {
            var r = lines[i].rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 1f);
            r.sizeDelta = new Vector2(width, heights[i]);
            r.anchoredPosition = new Vector2(0f, y);
            y -= heights[i] + spacing;
        }

        bool skip = false;
        int startFrame = Time.frameCount;
        for (int i = 0; i < lines.Length && !skip; i++)
        {
            var g = lines[i].GetComponent<CanvasGroup>();
            for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
            {
                if (Time.frameCount > startFrame && Clicked()) { skip = true; break; }
                g.alpha = Mathf.SmoothStep(0f, 1f, t / fadeTime);
                yield return null;
            }
            g.alpha = 1f;
            for (float t = 0f; t < gap && !skip; t += Time.unscaledDeltaTime)
            {
                if (Clicked()) { skip = true; break; }
                yield return null;
            }
        }
        foreach (var l in lines) l.GetComponent<CanvasGroup>().alpha = 1f;
        if (skip) yield return null; // aynı tıklama sahneyi de geçmesin
    }

    IEnumerator FadeOne(TMP_Text line, float time)
    {
        var g = line.GetComponent<CanvasGroup>();
        for (float t = 0f; t < time; t += Time.unscaledDeltaTime) { g.alpha = t / time; yield return null; }
        g.alpha = 1f;
    }

    IEnumerator FadeText(float from, float to, float time)
    {
        var groups = textRoot.GetComponentsInChildren<CanvasGroup>();
        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            float a = Mathf.Lerp(from, to, t / time);
            foreach (var g in groups) g.alpha = Mathf.Min(g.alpha, a);
            yield return null;
        }
        foreach (var g in groups) g.alpha = to;
    }

    void ClearText()
    {
        if (textRoot == null) return;
        for (int i = textRoot.childCount - 1; i >= 0; i--) Destroy(textRoot.GetChild(i).gameObject);
    }

    // ================= Giriş ve geçişler =================
    static bool Clicked()
    {
        bool mouse = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        var k = Keyboard.current;
        bool key = k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame);
        return mouse || key;
    }

    IEnumerator WaitClick()
    {
        yield return new WaitForSecondsRealtime(0.4f);
        while (!Clicked()) yield return null;
        yield return null;
    }

    static void Clear(RawImage target) => target.color = new Color(0, 0, 0, 0);

    IEnumerator Fade(RawImage target, float from, float to, float time)
    {
        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            target.color = new Color(0, 0, 0, Mathf.SmoothStep(from, to, t / time));
            yield return null;
        }
        target.color = new Color(0, 0, 0, to);
    }

    // ================= Kurulum =================
    void Build()
    {
        DontDestroyOnLoad(gameObject);
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // her şeyin üstünde
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        canvasRect = (RectTransform)transform;

        var root = new GameObject("Root", typeof(RectTransform), typeof(RectMask2D));
        root.transform.SetParent(transform, false);
        Stretch((RectTransform)root.transform);

        // siyah zemin (görsel yokken)
        var bg = Raw("Background", root.transform); Stretch(bg.rectTransform); bg.color = Color.black;

        // görsel
        var imgHolder = new GameObject("ImageHolder", typeof(RectTransform), typeof(CanvasGroup));
        imgHolder.transform.SetParent(root.transform, false);
        Stretch((RectTransform)imgHolder.transform);
        imageGroup = imgHolder.GetComponent<CanvasGroup>();
        image = Raw("Image", imgHolder.transform);
        imageRect = image.rectTransform;
        imageRect.anchorMin = imageRect.anchorMax = imageRect.pivot = new Vector2(0.5f, 0.5f);

        // yazının okunması için alt tarafta koyu geçiş
        var shadeGo = new GameObject("Shade", typeof(RectTransform), typeof(CanvasGroup));
        shadeGo.transform.SetParent(root.transform, false);
        Stretch((RectTransform)shadeGo.transform);
        shade = shadeGo.GetComponent<CanvasGroup>();
        var grad = Raw("Gradient", shadeGo.transform);
        grad.texture = MakeGradient();
        var gr = grad.rectTransform;
        gr.anchorMin = new Vector2(0f, 0f); gr.anchorMax = new Vector2(1f, 0.62f);
        gr.offsetMin = gr.offsetMax = Vector2.zero;
        shade.alpha = 0f;

        // toz parçacıkları
        var dustGo = new GameObject("Dust", typeof(RectTransform));
        dustGo.transform.SetParent(root.transform, false);
        dustRoot = (RectTransform)dustGo.transform;
        dustRoot.anchorMin = dustRoot.anchorMax = new Vector2(0.5f, 0.5f);

        // yazılar
        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(root.transform, false);
        textRoot = (RectTransform)textGo.transform;
        textRoot.anchorMin = textRoot.anchorMax = new Vector2(0.5f, 0.5f);
        textRoot.sizeDelta = Vector2.zero;

        // geçişler için siyah perde (en üstte)
        black = Raw("Fade", root.transform); Stretch(black.rectTransform); black.color = new Color(0, 0, 0, 0);
        black.raycastTarget = true;

        root.SetActive(false);
    }

    static Texture2D MakeGradient()
    {
        var tex = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < 64; y++)
        {
            float a = Mathf.Pow(1f - y / 63f, 1.4f) * 0.88f;
            tex.SetPixel(0, y, new Color(0f, 0f, 0f, a));
        }
        tex.Apply();
        return tex;
    }

    static RawImage Raw(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(parent, false);
        var r = go.GetComponent<RawImage>();
        r.raycastTarget = false;
        return r;
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
    }
}
