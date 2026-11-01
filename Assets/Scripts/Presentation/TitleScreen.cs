using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Başlık ekranı (videonun açılışı): dönen zaman girdabı, "DÖNGÜ", alt başlık.
/// Oyun açılınca bir kez gösterilir; tıklayınca / E / Space / Enter ile kapanır.
/// </summary>
public class TitleScreen : MonoBehaviour
{
    static TitleScreen instance;
    public static bool IsOpen => instance != null && instance.open;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create() => Show();

    public static void Show()
    {
        if (instance == null)
        {
            var go = new GameObject("TitleScreen");
            instance = go.AddComponent<TitleScreen>();
            try { instance.Build(); } catch (System.Exception e) { Debug.LogError("[TitleScreen] " + e); Destroy(go); instance = null; return; }
        }
        instance.open = true;
        instance.openedAt = Time.unscaledTime;
        instance.root.SetActive(true);
        instance.group.alpha = 1f;
        instance.closing = false;
        AudioManager.Sfx("sfx_chime");
    }

    bool open, closing;
    float openedAt, closeAt;
    GameObject root;
    CanvasGroup group, titleGroup, subGroup, smallGroup, hintGroup, portalGroup;
    RectTransform portal;

    void Update()
    {
        if (!open) return;
        float t = Time.unscaledTime - openedAt;
        portalGroup.alpha = Smooth(t / 2.2f);
        float s = Mathf.Lerp(380f, 460f, Smooth(t / 2.2f)) * (1f + Mathf.Sin(t * 2.2f) * 0.03f);
        portal.sizeDelta = new Vector2(s, s);
        portal.localRotation = Quaternion.Euler(0, 0, -t * 8f);
        titleGroup.alpha = Smooth((t - 1.0f) / 1.2f);
        subGroup.alpha = Smooth((t - 2.2f) / 1.0f);
        smallGroup.alpha = Smooth((t - 3.4f) / 0.8f);
        hintGroup.alpha = t > 4f ? 0.55f + 0.45f * Mathf.Sin((t - 4f) * 3f) : 0f;

        if (closing)
        {
            float k = (Time.unscaledTime - closeAt) / 0.7f;
            group.alpha = 1f - Smooth(k);
            if (k >= 1f)
            {
                open = false; root.SetActive(false);
                StoryText.SceneBeats(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
            return;
        }

        if (t < 1.0f) return;
        var kb = Keyboard.current;
        bool go = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                  (kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame));
        if (go) { closing = true; closeAt = Time.unscaledTime; }
    }

    static float Smooth(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }

    void Build()
    {
        DontDestroyOnLoad(gameObject);
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 990;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        root = new GameObject("Root", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        root.transform.SetParent(transform, false);
        var rr = (RectTransform)root.transform; rr.anchorMin = Vector2.zero; rr.anchorMax = Vector2.one; rr.offsetMin = rr.offsetMax = Vector2.zero;
        root.GetComponent<Image>().color = new Color(0.031f, 0.027f, 0.024f, 1f);
        group = root.GetComponent<CanvasGroup>();

        var p = new GameObject("Portal", typeof(RectTransform), typeof(RawImage), typeof(CanvasGroup));
        p.transform.SetParent(root.transform, false);
        portal = (RectTransform)p.transform;
        portal.anchorMin = portal.anchorMax = portal.pivot = new Vector2(0.5f, 0.5f);
        portal.anchoredPosition = new Vector2(0, 240);
        var raw = p.GetComponent<RawImage>(); raw.texture = Resources.Load<Texture2D>("Portal/TimePortal"); raw.raycastTarget = false;
        portalGroup = p.GetComponent<CanvasGroup>();

        titleGroup = Line("Title", "DÖNGÜ", 170, new Color(1f, 0.83f, 0.42f), FontStyles.Bold, -110);
        subGroup = Line("Sub", "<i>Geçmişi değiştir. Geleceği kurtar... ama ne kadar süreliğine?</i>", 42, new Color(0.94f, 0.91f, 0.84f), FontStyles.Normal, -270);
        smallGroup = Line("Small", "Su kıtlığı üzerine 2D bir hikâye oyunu", 30, new Color(0.75f, 0.71f, 0.65f), FontStyles.Normal, -340);
        hintGroup = Line("Hint", "<color=#FFD36B>[Tıkla / E]</color> Başla", 30, Color.white, FontStyles.Bold, -460);
        root.SetActive(false);
    }

    CanvasGroup Line(string name, string text, float size, Color c, FontStyles style, float y)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(root.transform, false);
        var r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = new Vector2(0, y); r.sizeDelta = new Vector2(1600, size * 1.4f);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = c; t.fontStyle = style; t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false;
        if (t.font != null) { try { t.outlineWidth = 0.15f; t.outlineColor = new Color32(0, 0, 0, 255); } catch { } }
        var g = go.GetComponent<CanvasGroup>(); g.alpha = 0f;
        return g;
    }
}
