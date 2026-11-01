using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Diyalog kutusu, seçenekler, "[E] Konuş" ipucu ve üst bildirimler.
/// Arayüz kod ile kurulur; sahneye bir şey eklemek gerekmez (DialogueUI.Get() ilk çağrıda kendini oluşturur).
/// Kontroller: E / Enter / Boşluk = devam veya yazıyı hızlandır, 1-4 = seçenek, fare ile tıklama.
/// </summary>
public class DialogueUI : MonoBehaviour
{
    static DialogueUI instance;
    public static bool IsOpen => instance != null && instance.open;
    /// <summary>Diyalog az önce (bu karede veya bir önceki karede) kapandıysa true. Aynı E tuşunun başka şeyi tetiklemesini önler.</summary>
    public static bool JustClosed
    {
        get { int d = Time.frameCount - lastClosedFrame; return d >= 0 && d <= 1; }
    }
    static int lastClosedFrame = -10;

    // Domain Reload kapalıyken (Enter Play Mode Options) statikler Play'ler arasında kalır: her Play'de sıfırla.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { lastClosedFrame = -10; instance = null; }

    public static DialogueUI Get()
    {
        if (instance == null)
        {
            var go = new GameObject("DialogueUI");
            instance = go.AddComponent<DialogueUI>();
            try { instance.Build(); }
            catch (System.Exception e)
            {
                Debug.LogError("[DialogueUI] Arayüz kurulamadı. TextMesh Pro dosyaları eksik olabilir (Window > TextMeshPro > Import TMP Essential Resources).\n" + e);
                instance.HideAll();
            }
        }
        return instance;
    }

    [Header("Ayarlar")]
    public float charsPerSecond = 45f;

    // UI parçaları
    GameObject panel, choiceBox, continueHint;
    Image portrait;
    TMP_Text nameText, bodyText, hintText, toastText;
    readonly List<Button> choiceButtons = new List<Button>();
    readonly List<TMP_Text> choiceLabels = new List<TMP_Text>();
    CanvasGroup toastGroup;

    // durum
    bool open;
    DialogueData data;
    DialogueData.Node node;
    readonly List<DialogueData.Choice> visibleChoices = new List<DialogueData.Choice>();
    float typed;
    int openFrame;
    System.Action onClosed;
    Object hintOwner;
    float toastUntil;
    HeroController2D lockedHero;
    bool unlockPending;

    static readonly Color PanelColor = new Color(0.13f, 0.11f, 0.10f, 0.94f);
    static readonly Color Gold = new Color(1f, 0.83f, 0.42f);

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    void OnEnable() { UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded; }
    void OnDestroy() { if (instance == this) instance = null; }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m)
    {
        if (open) { open = false; if (panel) panel.SetActive(false); if (choiceBox) choiceBox.SetActive(false); onClosed = null; }
        lockedHero = null;
        unlockPending = false;
        hintOwner = null;
        if (hintText) hintText.gameObject.SetActive(false);
        EnsureEventSystem();
    }

    // ================= Genel API =================

    public void Open(DialogueData d, Sprite face, System.Action closed = null)
    {
        if (d == null || panel == null || bodyText == null || choiceBox == null) return;
        data = d;
        onClosed = closed;
        unlockPending = false;
        open = true;
        openFrame = Time.frameCount;
        portrait.sprite = face;
        portrait.enabled = face != null;
        panel.SetActive(true);
        HideHint(hintOwner);
        LockPlayer(true);
        ShowNode(d.GetStartNode());
    }

    public void Close()
    {
        if (!open) return;
        open = false;
        lastClosedFrame = Time.frameCount;
        panel.SetActive(false);
        choiceBox.SetActive(false);
        unlockPending = true; // oyuncuyu bir sonraki karede serbest bırak (kapatan tuş zıplatmasın)
        var cb = onClosed; onClosed = null;
        cb?.Invoke();
    }

    public static void ShowHint(Object owner, string text)
    {
        var ui = Get();
        if (ui.open || ui.hintText == null) return;
        ui.hintOwner = owner;
        ui.hintText.text = text;
        ui.hintText.gameObject.SetActive(true);
    }

    public static void HideHint(Object owner)
    {
        if (instance == null || instance.hintOwner != owner || instance.hintText == null) return;
        instance.hintOwner = null;
        instance.hintText.gameObject.SetActive(false);
    }

    public static void Toast(string text, float seconds = 3.5f)
    {
        if (string.IsNullOrEmpty(text)) return;
        var ui = Get();
        if (ui.toastText == null || ui.toastGroup == null) return;
        AudioManager.Sfx("sfx_chime", 0.45f);
        ui.toastText.text = text;
        ui.toastUntil = Time.unscaledTime + seconds;
        ui.toastGroup.alpha = 1f;
    }

    // ================= Akış =================

    void ShowNode(DialogueData.Node n)
    {
        node = n;
        if (n == null) { Close(); return; }

        GameFlags.Set(n.setFlag);
        if (!string.IsNullOrEmpty(n.toast)) Toast(n.toast, 5f);

        nameText.text = string.IsNullOrEmpty(n.speaker) ? data.speakerName : n.speaker;
        bodyText.text = n.text;
        bodyText.maxVisibleCharacters = 0;
        typed = 0f;

        visibleChoices.Clear();
        foreach (var c in n.choices)
        {
            if (!string.IsNullOrEmpty(c.requireFlag) && !GameFlags.Has(c.requireFlag)) continue;
            if (!string.IsNullOrEmpty(c.hideIfFlag) && GameFlags.Has(c.hideIfFlag)) continue;
            visibleChoices.Add(c);
        }
        choiceBox.SetActive(false);
        continueHint.SetActive(false);
    }

    bool TypingDone => bodyText.maxVisibleCharacters >= bodyText.text.Length;

    void FinishTyping()
    {
        bodyText.maxVisibleCharacters = bodyText.text.Length;
        RevealOptions();
    }

    void RevealOptions()
    {
        if (visibleChoices.Count == 0) { continueHint.SetActive(true); return; }
        choiceBox.SetActive(true);
        for (int i = 0; i < choiceButtons.Count; i++)
        {
            bool on = i < visibleChoices.Count;
            choiceButtons[i].gameObject.SetActive(on);
            if (on) choiceLabels[i].text = $"<color=#FFD36B>{i + 1}.</color>  {visibleChoices[i].text}";
        }
        EventSystem.current?.SetSelectedGameObject(null);
    }

    void Pick(int index)
    {
        if (!open || index < 0 || index >= visibleChoices.Count || !TypingDone) return;
        var c = visibleChoices[index];
        AudioManager.Sfx("sfx_click");
        if (c.onPick != null && !c.onPick()) return;
        GameFlags.Set(c.setFlag);
        if (string.IsNullOrEmpty(c.next)) Close();
        else ShowNode(data.Find(c.next));
    }

    void Update()
    {
        if (panel == null || toastGroup == null || bodyText == null) return; // arayüz kurulamadıysa
        if (open && !panel.activeSelf) { open = false; LockPlayer(false); } // kendini onar: görünmeyen diyalog etkileşimi kilitlemesin
        // bildirim solması
        if (toastGroup.alpha > 0f && Time.unscaledTime > toastUntil)
            toastGroup.alpha = Mathf.MoveTowards(toastGroup.alpha, 0f, Time.unscaledDeltaTime * 2f);

        if (unlockPending && Time.frameCount > lastClosedFrame) { unlockPending = false; if (!open) LockPlayer(false); }

        if (!open || node == null) return;

        if (!TypingDone)
        {
            typed += Time.unscaledDeltaTime * charsPerSecond;
            bodyText.maxVisibleCharacters = Mathf.Min(bodyText.text.Length, Mathf.FloorToInt(typed));
            if (TypingDone) RevealOptions();
        }

        if (Time.frameCount == openFrame) return; // açan tuşla hemen geçmesin

        var k = Keyboard.current;
        bool advance = k != null && (k.eKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame);
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && visibleChoices.Count == 0) advance = true;

        if (advance)
        {
            if (!TypingDone) FinishTyping();
            else if (visibleChoices.Count == 0)
            {
                if (string.IsNullOrEmpty(node.next)) Close();
                else ShowNode(data.Find(node.next));
            }
        }

        if (k != null && TypingDone && visibleChoices.Count > 0)
        {
            if (k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame) Pick(0);
            else if (k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame) Pick(1);
            else if (k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame) Pick(2);
            else if (k.digit4Key.wasPressedThisFrame || k.numpad4Key.wasPressedThisFrame) Pick(3);
        }

        if (k != null && k.escapeKey.wasPressedThisFrame) Close();
    }

    void LockPlayer(bool locked)
    {
        if (locked)
        {
            lockedHero = FindFirstObjectByType<HeroController2D>();
            if (lockedHero) lockedHero.inputLocked = true;
        }
        else if (lockedHero)
        {
            lockedHero.inputLocked = false;
            lockedHero = null;
        }
    }

    // ================= Arayüzü kur =================

    void Build()
    {
        DontDestroyOnLoad(gameObject);

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        // Ana panel (alt)
        panel = Box("Panel", transform, PanelColor);
        var pr = (RectTransform)panel.transform;
        pr.anchorMin = new Vector2(0.5f, 0f); pr.anchorMax = new Vector2(0.5f, 0f); pr.pivot = new Vector2(0.5f, 0f);
        pr.sizeDelta = new Vector2(1500, 300); pr.anchoredPosition = new Vector2(0, 40);
        panel.AddComponent<Outline>().effectColor = Gold * 0.8f;

        var pgo = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        pgo.transform.SetParent(panel.transform, false);
        portrait = pgo.GetComponent<Image>();
        portrait.preserveAspect = true;
        var por = (RectTransform)pgo.transform;
        por.anchorMin = por.anchorMax = new Vector2(0, 0.5f); por.pivot = new Vector2(0, 0.5f);
        por.sizeDelta = new Vector2(200, 270); por.anchoredPosition = new Vector2(30, 0);

        nameText = Text("Name", panel.transform, 40, Gold, FontStyles.Bold);
        Place(nameText.rectTransform, new Vector2(260, -24), new Vector2(1200, 56));

        bodyText = Text("Body", panel.transform, 34, Color.white, FontStyles.Normal);
        Place(bodyText.rectTransform, new Vector2(260, -88), new Vector2(1200, 190));
        bodyText.textWrappingMode = TextWrappingModes.Normal;

        var ch = Text("Continue", panel.transform, 26, new Color(1, 1, 1, 0.7f), FontStyles.Italic);
        ch.text = "[E] Devam";
        ch.alignment = TextAlignmentOptions.BottomRight;
        var chr = ch.rectTransform; chr.anchorMin = chr.anchorMax = chr.pivot = new Vector2(1, 0);
        chr.sizeDelta = new Vector2(300, 40); chr.anchoredPosition = new Vector2(-24, 16);
        continueHint = ch.gameObject;

        // Seçenek kutusu (panelin üstünde, sağda)
        choiceBox = new GameObject("Choices", typeof(RectTransform));
        choiceBox.transform.SetParent(transform, false);
        var cr = (RectTransform)choiceBox.transform;
        cr.anchorMin = cr.anchorMax = new Vector2(0.5f, 0f); cr.pivot = new Vector2(1f, 0f);
        cr.anchoredPosition = new Vector2(750, 355); cr.sizeDelta = new Vector2(760, 0);
        var vl = choiceBox.AddComponent<VerticalLayoutGroup>();
        vl.spacing = 10; vl.childControlHeight = true; vl.childControlWidth = true; vl.childForceExpandHeight = false;
        vl.childAlignment = TextAnchor.LowerRight;
        var fit = choiceBox.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        for (int i = 0; i < 4; i++)
        {
            int idx = i;
            var b = Box("Choice" + (i + 1), choiceBox.transform, new Color(0.2f, 0.17f, 0.14f, 0.96f));
            var le = b.AddComponent<LayoutElement>(); le.minHeight = 64;
            b.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.6f);
            var btn = b.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.35f, 1.2f, 0.9f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            btn.colors = colors;
            btn.onClick.AddListener(() => Pick(idx));
            var t = Text("Label", b.transform, 30, Color.white, FontStyles.Normal);
            var tr = t.rectTransform; tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(22, 6); tr.offsetMax = new Vector2(-16, -6);
            t.alignment = TextAlignmentOptions.MidlineLeft;
            t.textWrappingMode = TextWrappingModes.Normal;
            choiceButtons.Add(btn); choiceLabels.Add(t);
        }

        // İpucu (alt orta)
        hintText = Text("Hint", transform, 32, Color.white, FontStyles.Bold);
        hintText.alignment = TextAlignmentOptions.Center;
        var hr = hintText.rectTransform; hr.anchorMin = hr.anchorMax = hr.pivot = new Vector2(0.5f, 0f);
        hr.sizeDelta = new Vector2(900, 60); hr.anchoredPosition = new Vector2(0, 60);
        SetOutline(hintText, 0.25f);

        // Bildirim (üst orta)
        toastText = Text("Toast", transform, 36, Gold, FontStyles.Bold);
        toastText.alignment = TextAlignmentOptions.Center;
        toastText.textWrappingMode = TextWrappingModes.Normal;
        var tor = toastText.rectTransform; tor.anchorMin = tor.anchorMax = tor.pivot = new Vector2(0.5f, 1f);
        tor.sizeDelta = new Vector2(1100, 160); tor.anchoredPosition = new Vector2(0, -40);
        SetOutline(toastText, 0.25f);
        toastGroup = toastText.gameObject.AddComponent<CanvasGroup>();
        toastGroup.alpha = 0f; toastGroup.blocksRaycasts = false;

        panel.SetActive(false);
        choiceBox.SetActive(false);
        hintText.gameObject.SetActive(false);

        EnsureEventSystem();
    }

    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    static GameObject Box(string name, Transform parent, Color c)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = c;
        return go;
    }

    void HideAll()
    {
        if (panel) panel.SetActive(false);
        if (choiceBox) choiceBox.SetActive(false);
        if (hintText) hintText.gameObject.SetActive(false);
        open = false;
    }

    static void SetOutline(TMP_Text t, float width)
    {
        if (t == null || t.font == null) return; // font yoksa materyal de yoktur
        try { t.outlineWidth = width; t.outlineColor = new Color32(0, 0, 0, 255); }
        catch (System.Exception) { }
    }

    static TMP_Text Text(string name, Transform parent, float size, Color c, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size; t.color = c; t.fontStyle = style;
        t.raycastTarget = false;
        return t;
    }

    static void Place(RectTransform r, Vector2 topLeft, Vector2 size)
    {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
        r.anchoredPosition = topLeft; r.sizeDelta = size;
    }
}
