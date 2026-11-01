using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Tam ekran anlatım kartı (ör. "Birkaç gün sonra..." + not). Kodla kurulur.
/// Açıkken etkileşim ve hareket kilitlidir. E / Enter / Space / tıklama ile kapanır.
/// </summary>
public class StoryCard : MonoBehaviour
{
    static StoryCard instance;
    public static bool IsOpen => instance != null && instance.open;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => instance = null;

    CanvasGroup group;
    TMP_Text titleText, bodyText, hintText;
    bool open;
    float openedAt;
    System.Action onClosed;

    public static void Show(string title, string body, System.Action closed = null)
    {
        if (instance == null)
        {
            var go = new GameObject("StoryCard");
            instance = go.AddComponent<StoryCard>();
            instance.Build();
        }
        instance.Open(title, body, closed);
    }

    void Open(string title, string body, System.Action closed)
    {
        titleText.text = title;
        bodyText.text = body;
        onClosed = closed;
        open = true;
        openedAt = Time.unscaledTime;
        group.alpha = 0f;
        group.gameObject.SetActive(true);
        hintText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!open) return;
        float t = Time.unscaledTime - openedAt;
        group.alpha = Mathf.Clamp01(t / 0.8f);
        bool canClose = t > 1.2f;
        hintText.gameObject.SetActive(canClose);
        if (!canClose) return;

        var k = Keyboard.current;
        bool next = (k != null && (k.eKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame)) ||
                    (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        if (!next) return;

        open = false;
        group.gameObject.SetActive(false);
        var cb = onClosed; onClosed = null;
        cb?.Invoke();
    }

    void OnEnable() { UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded; }
    void OnSceneLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m)
    {
        if (!open) return;
        open = false; onClosed = null;
        if (group) group.gameObject.SetActive(false);
    }

    void Build()
    {
        DontDestroyOnLoad(gameObject);
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 450;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        var root = new GameObject("Root", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        root.transform.SetParent(transform, false);
        Stretch((RectTransform)root.transform);
        root.GetComponent<Image>().color = new Color(0.04f, 0.035f, 0.03f, 0.97f);
        group = root.GetComponent<CanvasGroup>();

        titleText = Text("Title", root.transform, 64, new Color(1f, 0.83f, 0.42f), FontStyles.Italic);
        Place(titleText.rectTransform, new Vector2(0, 260), new Vector2(1500, 100));

        bodyText = Text("Body", root.transform, 38, new Color(0.95f, 0.92f, 0.85f), FontStyles.Normal);
        bodyText.textWrappingMode = TextWrappingModes.Normal;
        Place(bodyText.rectTransform, new Vector2(0, -20), new Vector2(1300, 440));

        hintText = Text("Hint", root.transform, 30, new Color(1f, 1f, 1f, 0.7f), FontStyles.Italic);
        hintText.text = "<color=#FFD36B>[E]</color> Devam";
        Place(hintText.rectTransform, new Vector2(0, -400), new Vector2(600, 50));

        root.SetActive(false);
    }

    static TMP_Text Text(string name, Transform parent, float size, Color c, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size; t.color = c; t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    static void Place(RectTransform r, Vector2 pos, Vector2 size)
    {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos; r.sizeDelta = size;
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
    }
}
