using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Gazete okuyucu. Arayüzünü kendisi kurar, sahneye bir şey eklemek gerekmez (NewspaperUI.Get()).
///
/// Sayfalar Resources klasöründen yüklenir:
///   Normal gazete    : Resources/Newspaper/Gazete_Sayfa_1.png, _2, _3 ...
///   Değişmiş gazete  : Resources/Newspaper/Changed/ChangedNewspaper_1.png, _2, _3 ...
/// Geçmişte fabrika görevi tamamlanınca (Rabiş'e kanıt teslim edilince) gazete değişmiş sayfaları gösterir.
/// Kontroller: A / D (ok tuşları) veya ekrandaki oklar = sayfa çevir, E / Esc = kapat.
/// </summary>
public class NewspaperUI : MonoBehaviour
{
    static NewspaperUI instance;
    public static NewspaperUI Instance => instance;
    public bool IsOpen => isOpen;

    public static NewspaperUI Get()
    {
        if (instance == null)
        {
            var go = new GameObject("NewspaperUI");
            builtByCode = true;
            instance = go.AddComponent<NewspaperUI>();
            builtByCode = false;
            try { instance.Build(); }
            catch (System.Exception e)
            {
                Debug.LogError("[Gazete] Arayüz kurulamadı. TextMesh Pro dosyaları eksik olabilir.\n" + e);
                if (instance.root) instance.root.SetActive(false);
            }
        }
        return instance;
    }

    /// <summary>Geçmiş değişti mi? (fabrika görevi tamamlandı ve kirlilik durduruldu)</summary>
    public static bool HistoryChanged =>
        GameFlags.Has("kanit_teslim") ||
        (GameManager.Instance != null && GameManager.Instance.factoryMissionCompleted && GameManager.Instance.factoryPollutionStopped);

    [Min(0.1f)] public float turnDuration = 0.34f;

    static bool builtByCode;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { instance = null; builtByCode = false; }
    const string Hint = "<color=#FFD36B>[A / D]</color> Sayfa çevir     <color=#FFD36B>[E / Esc]</color> Kapat";

    readonly List<Sprite> pages = new List<Sprite>();
    readonly List<Sprite> changedPages = new List<Sprite>();
    List<Sprite> active;

    GameObject root;
    Image pageImage;
    RectTransform pageRect;
    Button prevButton, nextButton;
    TMP_Text counterText, headerText;

    int currentPage;
    bool reachedLastPage;
    bool isOpen, isTurning;
    int openedFrame = -10;

    void Awake()
    {
        // Eski sahnelerde elle yerleştirilmiş NewspaperUI kalmışsa devre dışı bırak (yenisi kodla kurulur).
        if (!builtByCode)
        {
            var oldPanel = transform.Find("NewspaperPanel");
            if (oldPanel) oldPanel.gameObject.SetActive(false);
            Destroy(this);
            return;
        }
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    void OnDestroy() { if (instance == this) instance = null; }

    // ================= Genel API =================
    public void Open() => OpenNewspaper();

    public void OpenNewspaper()
    {
        if (isOpen) return;
        if (pages.Count == 0) LoadPages();
        active = HistoryChanged && changedPages.Count > 0 ? changedPages : pages;
        if (active.Count == 0)
        {
            Debug.LogWarning("[Gazete] Sayfa bulunamadı: Assets/Resources/Newspaper/Gazete_Sayfa_1.png ...");
            return;
        }

        currentPage = 0;
        reachedLastPage = false;
        isOpen = true;
        isTurning = false;
        openedFrame = Time.frameCount;
        headerText.text = active == changedPages
            ? "<color=#9BE37B>Geçmiş değişti — gazete artık farklı yazıyor!</color>"
            : string.Empty;
        root.SetActive(true);
        ShowPage();
        DialogueUI.ShowHint(this, Hint);
        AudioManager.Sfx("sfx_page");
        if (active != changedPages) StoryText.ChapterOnce(StoryText.ChNews, 0.2f);
    }

    public void CloseNewspaper()
    {
        if (!isOpen) return;
        isOpen = false;
        isTurning = false;
        StopAllCoroutines();
        pageRect.localScale = Vector3.one;
        root.SetActive(false);
        DialogueUI.HideHint(this);
        GameFlags.Set(HomeStoryFlags.NewspaperRead); // ev hikâyesi: gazete okununca Zeynep gelir

        // FİNAL: geçmiş değiştikten sonra değişmiş gazetenin son sayfası okunup kapatılınca sinematik başlar
        if (active == changedPages && reachedLastPage && !GameFlags.Has(EndingCinematic.SeenFlag))
            EndingCinematic.Play();
    }

    public void NextPage()
    {
        if (!isOpen || isTurning || currentPage >= active.Count - 1) return;
        AudioManager.Sfx("sfx_page", 0.8f);
        StartCoroutine(TurnTo(currentPage + 1));
    }

    public void PreviousPage()
    {
        if (!isOpen || isTurning || currentPage <= 0) return;
        AudioManager.Sfx("sfx_page", 0.8f);
        StartCoroutine(TurnTo(currentPage - 1));
    }

    // ================= İç işleyiş =================
    void Update()
    {
        if (isOpen && (root == null || !root.activeSelf)) { isOpen = false; isTurning = false; DialogueUI.HideHint(this); } // kendini onar
        if (!isOpen || isTurning || Time.frameCount == openedFrame) return;
        var k = Keyboard.current;
        if (k == null) return;

        if (k.eKey.wasPressedThisFrame || k.escapeKey.wasPressedThisFrame) CloseNewspaper();
        else if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) NextPage();
        else if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) PreviousPage();
    }

    IEnumerator TurnTo(int target)
    {
        isTurning = true;
        UpdateButtons();
        float half = turnDuration * 0.5f;
        for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
        {
            float e = Smooth(t / half);
            pageRect.localScale = new Vector3(Mathf.Lerp(1f, 0.035f, e), 1f, 1f);
            yield return null;
        }
        currentPage = target;
        ShowPage();
        for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
        {
            float e = Smooth(t / half);
            pageRect.localScale = new Vector3(Mathf.Lerp(0.035f, 1f, e), 1f, 1f);
            yield return null;
        }
        pageRect.localScale = Vector3.one;
        isTurning = false;
        UpdateButtons();
    }

    static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

    void ShowPage()
    {
        pageImage.sprite = active[currentPage];
        counterText.text = $"{currentPage + 1} / {active.Count}";
        if (currentPage >= active.Count - 1) reachedLastPage = true;
        UpdateButtons();
    }

    void UpdateButtons()
    {
        prevButton.interactable = !isTurning && currentPage > 0;
        nextButton.interactable = !isTurning && active != null && currentPage < active.Count - 1;
    }

    void LoadPages()
    {
        pages.Clear(); changedPages.Clear();
        for (int i = 1; i <= 30; i++) { var s = LoadSprite($"Newspaper/Gazete_Sayfa_{i}"); if (s == null) break; pages.Add(s); }
        for (int i = 1; i <= 30; i++) { var s = LoadSprite($"Newspaper/Changed/ChangedNewspaper_{i}"); if (s == null) break; changedPages.Add(s); }
    }

    static Sprite LoadSprite(string path)
    {
        var sprite = Resources.Load<Sprite>(path);
        if (sprite != null) return sprite;
        var tex = Resources.Load<Texture2D>(path); // görsel Sprite olarak içe aktarılmadıysa da çalışsın
        return tex == null ? null : Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
    }

    // ================= Arayüzü kur =================
    void Build()
    {
        DontDestroyOnLoad(gameObject);
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400; // DialogueUI (500) ipucu ve bildirimleri üstte kalsın
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        root = new GameObject("Root", typeof(RectTransform));
        root.transform.SetParent(transform, false);
        Stretch((RectTransform)root.transform);

        var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        dim.transform.SetParent(root.transform, false);
        Stretch((RectTransform)dim.transform);
        dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

        var page = new GameObject("Page", typeof(RectTransform), typeof(Image));
        page.transform.SetParent(root.transform, false);
        pageRect = (RectTransform)page.transform;
        pageRect.anchorMin = pageRect.anchorMax = pageRect.pivot = new Vector2(0.5f, 0.5f);
        pageRect.sizeDelta = new Vector2(1100, 800);
        pageRect.anchoredPosition = new Vector2(0, 40);
        pageImage = page.GetComponent<Image>();
        pageImage.preserveAspect = true;
        pageImage.raycastTarget = false;

        prevButton = ArrowButton("Previous", "<", new Vector2(-640, 40), PreviousPage);
        nextButton = ArrowButton("Next", ">", new Vector2(640, 40), NextPage);

        counterText = Text("Counter", root.transform, 30, new Color(1f, 1f, 1f, 0.85f));
        var cr = counterText.rectTransform;
        cr.anchorMin = cr.anchorMax = cr.pivot = new Vector2(0.5f, 0.5f);
        cr.sizeDelta = new Vector2(300, 44); cr.anchoredPosition = new Vector2(0, -385);

        headerText = Text("Header", root.transform, 34, Color.white);
        var hr = headerText.rectTransform;
        hr.anchorMin = hr.anchorMax = hr.pivot = new Vector2(0.5f, 1f);
        hr.sizeDelta = new Vector2(1400, 60); hr.anchoredPosition = new Vector2(0, -30);

        root.SetActive(false);
    }

    Button ArrowButton(string name, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(root.transform, false);
        var r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(96, 96); r.anchoredPosition = pos;
        go.GetComponent<Image>().color = new Color(0.2f, 0.17f, 0.14f, 0.96f);
        go.AddComponent<Outline>().effectColor = new Color(1f, 0.83f, 0.42f, 0.8f);
        var b = go.GetComponent<Button>();
        b.onClick.AddListener(onClick);
        var t = Text("Label", go.transform, 56, new Color(1f, 0.83f, 0.42f));
        Stretch(t.rectTransform);
        t.text = label;
        return b;
    }

    static TMP_Text Text(string name, Transform parent, float size, Color c)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size; t.color = c; t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        if (t.font != null)
        {
            try { t.outlineWidth = 0.2f; t.outlineColor = new Color32(0, 0, 0, 255); }
            catch (System.Exception) { }
        }
        return t;
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
    }
}
