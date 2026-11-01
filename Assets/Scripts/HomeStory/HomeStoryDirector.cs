using UnityEngine;

/// <summary>Ev hikâyesinin bayrakları (GameFlags'te tutulur, sahneler arasında kalır).</summary>
public static class HomeStoryFlags
{
    public const string NewspaperRead   = "gazete_okundu";
    public const string WaterShown      = "su_stogu";
    public const string ZeynepArrived   = "zeynep_geldi";
    public const string ZeynepDecided   = "zeynep_karar";
    public const string ZeynepGaveWater = "zeynep_su_verildi";
    public const string ZeynepTank      = "zeynep_depo_gorevi";
    public const string TankFound       = "zeynep_depo_bulundu";
    public const string ZeynepRefused   = "zeynep_reddedildi";
    public const string ZeynepNotePending = "zeynep_not_bekliyor";
    public const string ZeynepNoteShown = "zeynep_not_geldi";
    public const string ZeynepGone      = "zeynep_gitti";
    public const string MotherArrived   = "anne_geldi";
    public const string MotherDecided   = "anne_karar";
    public const string MotherGone      = "anne_gitti";

    // Diğer sistemlerle ortak güven bayrakları
    public const string ZeynepTrust     = "ZeynepGuvendi";
    public const string ZeynepTrustLost = "ZeynepGuvenKaybi";
    public const string FamilyHelped    = "AileyeYardimEdildi";
    public const string FamilyTrustLost = "AileGuvenKaybi";
}

/// <summary>
/// Ev içi / ev dışı sahnelerindeki hikâye akışı:
///  1) Gazete okununca Zeynep kapıdan gelir, su stoğu (3) görünür, Zeynep su ister:
///     a) Su ver   -> su -1, güven kazanılır, Zeynep gider, "Birkaç gün sonra..." notu gelir
///     b) Birlikte ara -> evin dışındaki su deposuna gidilir, su bulunur (+1), Zeynep gider
///     c) Reddet   -> güven kaybedilir, Zeynep gider
///  2) Zeynep gittikten sonra Anne ve çocuğu Deniz gelir; çocuk hasta, su verilir ya da verilmez.
/// Her sahnede bir tane bulunur (kurulum scripti yerleştirir). Durum GameFlags'te tutulduğu için
/// oyuncu sahne değiştirse de hikâye kaldığı yerden devam eder.
/// </summary>
public class HomeStoryDirector : MonoBehaviour
{
    public enum Place { Interior, Exterior }

    public Place place = Place.Interior;
    public GameObject zeynepPrefab, annePrefab, denizPrefab;
    [Tooltip("Ziyaretçilerin girip çıktığı x noktası (ev içinde kapı).")]
    public float entryX = 5.4f;
    [Tooltip("Ziyaretçilerin çıkıp gittiği x noktası.")]
    public float exitX = 6.2f;
    [Tooltip("Ev dışında Anne ve Deniz'in geldiği x noktası.")]
    public float arriveFromX = -7.8f;
    [Tooltip("Ev dışında Zeynep'in depoda beklediği x noktası.")]
    public float tankWaitX = 4.4f;
    public float floorY = -2.1f;
    public float motherDelay = 2.5f;
    [Header("Ölçek (ev içinde karakterler gerçek insan boyunda)")]
    public float npcScale = 1f;
    public float npcSpeed = 1.6f;
    [Tooltip("NPC'nin oyuncuya yaklaştığı mesafe.")]
    public float approachGap = 1.1f;
    [Tooltip("Çocuğun annesine olan mesafesi.")]
    public float childGap = 0.55f;
    [Tooltip("Başlarının üstündeki isim kartlarını gizle (büyük ölçekte ekranı kaplamasın).")]
    public bool hideCards = false;

    StoryWalker zeynep, anne, deniz;
    StoryWalker leaving; // sahneden çıkmakta olan Zeynep
    bool ZeynepPresent => zeynep != null || leaving != null;
    Transform player;
    bool zeynepAutoTalk, motherAutoTalk;
    float motherAt = -1f;
    float zeynepAt = -1f;

    static bool Has(string f) => GameFlags.Has(f);
    static void Set(string f) => GameFlags.Set(f);

    bool Busy => GameUI.InteractionBlocked;

    void Start()
    {
        var p = GameObject.Find("Player");
        if (p) player = p.transform;
        motherAt = Time.time + 1.2f;
    }

    void Update()
    {
        if (player == null) { var p = GameObject.Find("Player"); if (p) player = p.transform; else return; }

        UpdateZeynep();
        UpdateNote();
        UpdateMother();
    }

    // ===================== ZEYNEP =====================
    void UpdateZeynep()
    {
        if (place == Place.Interior && Has(HomeStoryFlags.NewspaperRead) && !Has(HomeStoryFlags.ZeynepDecided) && zeynep == null)
        {
            // gazete kapandıktan kısa bir süre sonra kapı çalsın
            if (Busy) { zeynepAt = -1f; return; }
            if (zeynepAt < 0f) zeynepAt = Time.time + (Has(HomeStoryFlags.ZeynepArrived) ? 0.5f : 1.5f);
            if (Time.time < zeynepAt) return;
            zeynepAt = -1f;

            bool first = !Has(HomeStoryFlags.ZeynepArrived);
            zeynep = Spawn(zeynepPrefab, entryX);
            if (zeynep == null) return;
            zeynep.onTalk = OpenZeynep;
            zeynep.Approach(player, approachGap);
            zeynepAutoTalk = true;
            if (first)
            {
                Set(HomeStoryFlags.ZeynepArrived);
                Set(HomeStoryFlags.WaterShown);
                StoryText.ChapterOnce(StoryText.Ch2, 0.3f);
                DialogueUI.Toast("Kapı çalıyor... Komşun Zeynep geldi.\nEvde " + StoryProgress.CleanWater + " şişe temiz suyun var.", 5f);
            }
        }

        if (place == Place.Exterior && Has(HomeStoryFlags.ZeynepTank) && !Has(HomeStoryFlags.TankFound) && zeynep == null)
        {
            zeynep = Spawn(zeynepPrefab, tankWaitX);
            if (zeynep == null) return;
            zeynep.onTalk = () => OpenSimple("Zeynep", Portrait(zeynep),
                "Depo şurada, tepenin üstünde. Merdivenden çıkıp kapağına bakalım!");
            zeynep.Face(player.position.x);
        }

        if (zeynep != null && zeynepAutoTalk && zeynep.Arrived && !Busy)
        {
            zeynepAutoTalk = false;
            OpenZeynep();
        }
    }

    void OpenZeynep()
    {
        if (Has(HomeStoryFlags.ZeynepDecided) || zeynep == null) return;
        zeynepAutoTalk = false;
        var d = new StoryDialogueBuilder("Zeynep");
        d.Node("start",
            "Merhaba komşu... Habersiz geldim, kusura bakma. İki gündür musluklardan tek damla su akmıyor, evde hiç suyum kalmadı. Senin biraz suyun var mı?");
        d.WaterChoice("start", "Al, suyumdan sana vereyim.", "give", () =>
        {
            Set(HomeStoryFlags.ZeynepDecided); Set(HomeStoryFlags.ZeynepGaveWater);
            Set(HomeStoryFlags.ZeynepTrust); Set(HomeStoryFlags.ZeynepNotePending);
            DialogueUI.Toast("Zeynep'in güvenini kazandın.", 4f);
        });
        d.Choice("start", "Birlikte su arayalım. Evin dışında eski bir su deposu var.", "tank", () =>
        {
            Set(HomeStoryFlags.ZeynepDecided); Set(HomeStoryFlags.ZeynepTank);
            DialogueUI.Toast("GÖREV: Evin dışındaki su deposuna Zeynep'le birlikte bak.", 6f);
        });
        d.Choice("start", "Üzgünüm, veremem. Bana ancak yetiyor.", "refuse", () =>
        {
            Set(HomeStoryFlags.ZeynepDecided); Set(HomeStoryFlags.ZeynepRefused);
            Set(HomeStoryFlags.ZeynepTrustLost); Set(HomeStoryFlags.ZeynepGone);
            DialogueUI.Toast("Zeynep'in güvenini kaybettin.", 4f);
        });
        d.Node("give", "Gerçekten mi? ...Çok teşekkür ederim. Bu iyiliğini unutmayacağım, söz veriyorum.");
        d.Node("tank", "Eski depo mu? Hiç aklıma gelmemişti! Hadi, dışarıda buluşalım.");
        d.Node("refuse", "...Anlıyorum. Artık herkes sadece kendini düşünüyor.");

        DialogueUI.Get().Open(d.Data, Portrait(zeynep), () =>
        {
            if (!Has(HomeStoryFlags.ZeynepDecided) || zeynep == null) return; // seçim yapılmadan kapandı: "[E] Konuş" ile tekrar açılır
            leaving = zeynep;
            leaving.WalkTo(exitX, true);
            zeynep = null;
        });
    }

    /// <summary>Ev dışındaki su deposu (WaterTank) çağırır.</summary>
    public void OnTankChecked()
    {
        if (!Has(HomeStoryFlags.ZeynepTank) || Has(HomeStoryFlags.TankFound))
        {
            DialogueUI.Toast(Has(HomeStoryFlags.TankFound)
                ? "Depoda başka su kalmadı."
                : "Eski, paslı bir su deposu. Kimse uzun zamandır bakmamış gibi.", 4f);
            return;
        }

        Set(HomeStoryFlags.TankFound);
        Set("DepoSuyuPaylasildi");
        StoryProgress.AddWater(1);
        DialogueUI.Toast("Depoda temiz su buldun! (+1 su)", 4f);

        var d = new StoryDialogueBuilder("Zeynep");
        d.Node("look", "Paslı merdivenden çıkıp kapağı açıyorsun... Deponun dibinde hâlâ biraz temiz su var!", speaker: "Su Deposu", next: "z");
        d.Node("z", "İnanamıyorum, buldun! Yarısını alıp eve götürüyorum, yarısı senin. Birlikte başardık, teşekkür ederim!");
        DialogueUI.Get().Open(d.Data, zeynep ? Portrait(zeynep) : null, () =>
        {
            Set(HomeStoryFlags.ZeynepTrust);
            Set(HomeStoryFlags.ZeynepGone);
            if (zeynep != null) { leaving = zeynep; leaving.WalkTo(exitX, true); zeynep = null; }
        });
    }

    // ===================== NOT (Birkaç gün sonra...) =====================
    void UpdateNote()
    {
        if (!Has(HomeStoryFlags.ZeynepNotePending) || Has(HomeStoryFlags.ZeynepNoteShown)) return;
        if (ZeynepPresent || Busy || StoryCard.IsOpen) return;

        Set(HomeStoryFlags.ZeynepNoteShown);
        StoryCard.Show("Birkaç gün sonra...",
            "Kapının altından katlanmış bir not buluyorsun:\n\n" +
            "<i>“Komşu, o gün bana suyunu paylaştığın için teşekkür ederim. Mahallenin yukarısında hâlâ temiz su veren bir kaynak buldum. " +
            "Birkaç gün içinde sana da su getirebilirim.\n— Zeynep”</i>",
            () =>
            {
                Set(HomeStoryFlags.ZeynepGone);
                motherAt = Time.time + motherDelay;
            });
    }

    // ===================== ANNE ve DENİZ =====================
    void UpdateMother()
    {
        bool due = Has(HomeStoryFlags.ZeynepGone) && !Has(HomeStoryFlags.MotherDecided);
        if (!due) return;

        if (anne == null)
        {
            if (ZeynepPresent || Busy || StoryCard.IsOpen) { motherAt = Mathf.Max(motherAt, Time.time + motherDelay); return; }
            if (Time.time < motherAt) return;

            float from = place == Place.Interior ? entryX : arriveFromX;
            float dir = place == Place.Interior ? 1f : -1f; // çocuk annenin arkasında
            anne = Spawn(annePrefab, from);
            deniz = Spawn(denizPrefab, from + dir * childGap);
            if (anne == null) return;
            anne.onTalk = OpenMother;
            anne.Approach(player, approachGap);
            if (deniz) deniz.Approach(anne.transform, childGap);
            motherAutoTalk = true;
            if (!Has(HomeStoryFlags.MotherArrived))
            {
                Set(HomeStoryFlags.MotherArrived);
                DialogueUI.Toast("Bir anne ve küçük oğlu sana doğru geliyor...", 4f);
            }
        }

        if (anne != null && motherAutoTalk && anne.Arrived && !Busy)
        {
            motherAutoTalk = false;
            OpenMother();
        }
    }

    void OpenMother()
    {
        if (Has(HomeStoryFlags.MotherDecided) || anne == null) return;
        motherAutoTalk = false;
        var d = new StoryDialogueBuilder("Anne");
        d.Node("start",
            "Lütfen, yardım et! Oğlum Deniz hasta... Musluklardan su gelmeyince nehirden su içti, o günden beri ateşi düşmüyor.", next: "deniz");
        d.Node("deniz", "Anne... karnım ağrıyor... çok susadım...", speaker: "Deniz", next: "ask");
        d.Node("ask", "Ne olur... Biraz temiz su verebilir misin?");
        d.WaterChoice("ask", "Al, bu suyu Deniz'e ver.", "give", () =>
        {
            Set(HomeStoryFlags.MotherDecided); Set(HomeStoryFlags.FamilyHelped);
            DialogueUI.Toast("Anne ve Deniz'in güvenini kazandın.", 4f);
        });
        d.Choice("ask", "Üzgünüm, verecek suyum yok.", "refuse", () =>
        {
            Set(HomeStoryFlags.MotherDecided); Set(HomeStoryFlags.FamilyTrustLost);
            DialogueUI.Toast("Anne ve Deniz yardımsız ayrıldı.", 4f);
        });
        d.Node("give", "Allah razı olsun... Bak, Deniz biraz kendine geldi. Bu iyiliğini asla unutmayacağız.");
        d.Node("refuse", "...Anlıyorum. Hadi Deniz, başka bir kapı çalalım.");

        DialogueUI.Get().Open(d.Data, Portrait(anne), () =>
        {
            if (!Has(HomeStoryFlags.MotherDecided) || anne == null) return;
            float exit = place == Place.Interior ? exitX : arriveFromX;
            anne.WalkTo(exit, true);
            if (deniz) deniz.WalkTo(exit, true);
            anne = null; deniz = null;
            Set(HomeStoryFlags.MotherGone);
        });
    }

    // ===================== yardımcılar =====================
    StoryWalker Spawn(GameObject prefab, float x)
    {
        if (prefab == null) { Debug.LogWarning("[HomeStory] NPC prefabı atanmamış. Tools > DÖNGÜ > Oyunu Kur'u çalıştır."); return null; }
        var go = Instantiate(prefab, new Vector3(x, floorY, 0f), Quaternion.identity);
        go.layer = 6;
        go.transform.localScale = new Vector3(npcScale, npcScale, 1f);
        var w = go.GetComponent<StoryWalker>();
        if (w == null) w = go.AddComponent<StoryWalker>();
        w.speed = npcSpeed;
        if (hideCards)
        {
            var card = go.GetComponent<NPCInteract2D>();
            if (card) { if (card.card) card.card.gameObject.SetActive(false); card.enabled = false; }
        }
        return w;
    }

    static Sprite Portrait(StoryWalker w)
    {
        if (w == null) return null;
        var sr = w.GetComponent<SpriteRenderer>();
        return sr ? sr.sprite : null;
    }

    void OpenSimple(string speaker, Sprite face, string line)
    {
        var d = new StoryDialogueBuilder(speaker);
        d.Node("only", line);
        DialogueUI.Get().Open(d.Data, face);
    }
}

/// <summary>Kodla kısa diyalog ağacı kurmak için yardımcı (DialogueUI ile kullanılır).</summary>
public class StoryDialogueBuilder
{
    public readonly DialogueData Data;

    public StoryDialogueBuilder(string speaker)
    {
        Data = ScriptableObject.CreateInstance<DialogueData>();
        Data.speakerName = speaker;
    }

    public DialogueData.Node Node(string id, string text, string speaker = null, string next = null)
    {
        var n = new DialogueData.Node { id = id, text = text, speaker = speaker, next = next };
        Data.nodes.Add(n);
        return n;
    }

    public void Choice(string nodeId, string text, string next, System.Action apply)
    {
        Data.Find(nodeId).choices.Add(new DialogueData.Choice
        {
            text = text, next = next,
            onPick = () => { apply?.Invoke(); return true; }
        });
    }

    /// <summary>1 su harcayan seçenek. Su yoksa seçilemez ve uyarı gösterilir.</summary>
    public void WaterChoice(string nodeId, string text, string next, System.Action apply)
    {
        int w = StoryProgress.CleanWater;
        string tag = w > 0 ? $"  <color=#7EC8FF>(Su -1, kalan {w})</color>" : "  <color=#FF8A7A>(Suyun kalmadı)</color>";
        Data.Find(nodeId).choices.Add(new DialogueData.Choice
        {
            text = text + tag, next = next,
            onPick = () =>
            {
                if (!StoryProgress.SpendWater())
                {
                    DialogueUI.Toast("Hiç temiz suyun kalmadı; başka bir seçenek seç.", 3f);
                    return false;
                }
                apply?.Invoke();
                return true;
            }
        });
    }
}
