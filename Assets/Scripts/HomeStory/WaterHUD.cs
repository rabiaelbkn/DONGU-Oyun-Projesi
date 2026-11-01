using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sol üstte temiz su stoğunu gösterir. Zeynep geldikten sonra (bayrak "su_stogu") görünür.
/// Geçmiş sahnesi (adı "Past" ile başlayan) dışındaki tüm sahnelerde görünür; sahne adına veya
/// arka plana bağlı değildir. Kendi Canvas'ı yüksek sıralamada olduğu için arka plan/UI altında kalmaz.
/// </summary>
public class WaterHUD : MonoBehaviour
{
    static WaterHUD instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        if (instance != null) return;
        var go = new GameObject("WaterHUD");
        instance = go.AddComponent<WaterHUD>();
        try { instance.Build(); } catch (System.Exception e) { Debug.LogError("[WaterHUD] " + e); }
    }

    [Tooltip("Açıksa su göstergesi oyunun başından itibaren görünür (Zeynep'i beklemez).")]
    public bool alwaysVisible = false;

    GameObject root;
    TMP_Text label;
    RectTransform box;
    int shownValue = -1;
    float pulse;

    void Update()
    {
        if (root == null) return;
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool visible = (alwaysVisible || GameFlags.Has(HomeStoryFlags.WaterShown)) &&
                       !scene.StartsWith("Past", System.StringComparison.OrdinalIgnoreCase) &&
                       !EndingCinematic.IsPlaying && !StoryCard.IsOpen &&
                       !(NewspaperUI.Instance != null && NewspaperUI.Instance.IsOpen);
        if (root.activeSelf != visible) root.SetActive(visible);
        if (!visible) return;

        int w = StoryProgress.CleanWater;
        if (w != shownValue)
        {
            if (shownValue >= 0) pulse = 1f;
            shownValue = w;
            string drops = "";
            for (int i = 0; i < Mathf.Max(3, w); i++)
                drops += i < w ? "<color=#7EC8FF>■</color> " : "<color=#555555>■</color> ";
            label.text = $"Temiz Su: <b>{w}</b>   {drops}";
        }
        pulse = Mathf.MoveTowards(pulse, 0f, Time.unscaledDeltaTime * 2f);
        box.localScale = Vector3.one * (1f + pulse * 0.15f);
    }

    void Build()
    {
        DontDestroyOnLoad(gameObject);
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900; // sahnedeki arka plan / UI Canvas'larının üstünde (final sinematiği 1000)
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        root = new GameObject("Box", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(transform, false);
        box = (RectTransform)root.transform;
        box.anchorMin = box.anchorMax = box.pivot = new Vector2(0f, 1f);
        box.anchoredPosition = new Vector2(30, -30);
        box.sizeDelta = new Vector2(420, 70);
        root.GetComponent<Image>().color = new Color(0.13f, 0.11f, 0.10f, 0.88f);
        root.AddComponent<Outline>().effectColor = new Color(1f, 0.83f, 0.42f, 0.7f);

        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(root.transform, false);
        label = go.AddComponent<TextMeshProUGUI>();
        label.fontSize = 32; label.color = Color.white;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;
        var r = label.rectTransform;
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(20, 0); r.offsetMax = new Vector2(-10, 0);

        root.SetActive(false);
    }
}
