#if UNITY_EDITOR
using System.Collections.Generic;

/// <summary>
/// EMİRHAN — Fabrika işçisi (Assets/PastScene/Data/FactoryGuard_Dialogue.asset)
///
/// Karakter: Gece vanayı açıp atığı nehre vermesi emredilen bir işçi. Küçük bir kızı var,
/// işini kaybetmekten korkuyor; yaptığının yanlış olduğunu içten içe biliyor.
///
/// Kurallar:
///  - Rabiş görevi vermeden önce Emirhan sadece seni araziden kovar ("pre").
///  - Görevden sonra borunun vanasında çalışır; o oradayken su örneği alınamaz.
///  - Yalan ("yolumu kaybettim") işe yaramaz: yolu gösterir ama yerinden kıpırdamaz.
///  - Rabiş'in adını doğrudan söylersen panikler ve şüphelenir ("emirhan_suphelendi").
///  - Onu ikna etmenin iki yolu var: kızı üzerinden empati ya da bulduğun ipuçları
///    (fabrika borusu / uyarı tabelası). Sonunda iki teklif:
///      a) "Fabrika arıtma kurmak zorunda kalır, kimse işini kaybetmez" -> MÜTTEFİK olur
///         ("emirhan_muttefik", "AritmaDesteklendi"), vana kayıt sayfasını verir.
///      b) "Kimseye senden bahsetmem" -> göz yumar, ama yardım etmez.
///    Her iki durumda da "guvenlik_gecildi": vardiyasına girer, borunun başı boşalır.
/// Kurulum (Tools > DÖNGÜ > Oyunu Kur) her çalıştığında bu içerik asset'e yazılır.
/// </summary>
public static class FactoryGuardDialogue
{
    public const string QuestFlag = "gorev_alindi";
    public const string PassFlag = "guvenlik_gecildi";
    public const string SuspectFlag = "emirhan_suphelendi";
    public const string AllyFlag = "emirhan_muttefik";
    public const string PipeClue = "fabrika_borusu_incelendi";
    public const string SignClue = "uyari_tabelasi_incelendi";

    static DialogueData.Choice C(string text, string next, string setFlag = null, string requireFlag = null, string hideIfFlag = null) =>
        new DialogueData.Choice { text = text, next = next, setFlag = setFlag, requireFlag = requireFlag, hideIfFlag = hideIfFlag };

    static DialogueData.Node N(string id, string text, params DialogueData.Choice[] choices) =>
        new DialogueData.Node { id = id, text = text, choices = new List<DialogueData.Choice>(choices) };

    public static void Fill(DialogueData d)
    {
        d.speakerName = "Emirhan — Fabrika İşçisi";
        d.startRules = new List<DialogueData.StartRule>
        {
            new DialogueData.StartRule { requireFlag = PassFlag, nodeId = "done" },
            new DialogueData.StartRule { requireFlag = SuspectFlag, nodeId = "again" },
            new DialogueData.StartRule { requireFlag = QuestFlag, nodeId = "start" },
            new DialogueData.StartRule { requireFlag = "", nodeId = "pre" },
        };

        var nodes = new List<DialogueData.Node>();

        // ---------- Görevden önce ----------
        nodes.Add(N("pre",
            "Burası fabrika arazisi, yabancılar giremez. Hadi, benim işim var."));

        // ---------- İlk gerçek karşılaşma (görev alındıktan sonra) ----------
        nodes.Add(N("start",
            "Dur bakalım! Burası fabrika arazisi. Vananın başında ne arıyorsun?",
            C("Nehre bakıyordum. Bu koku da ne böyle?", "smell"),
            C("Rabiş beni gönderdi, borudan su örneği alacağım.", "rabis"),
            C("Yolumu kaybettim.", "lost")));

        nodes.Add(N("lost",
            "Köy şu tarafta, köprüyü geçince. Ama buralarda dolaşma, ben vardiya bitene kadar bu vananın başındayım."));

        var rabis = N("rabis",
            "Rabiş mi?! O adam yüzünden patron hepimizi sıkıştırıyor! Örnek falan yok. Borunun yanına yaklaşma, gözüm üzerinde!");
        rabis.setFlag = SuspectFlag;
        rabis.toast = "Emirhan şüphelendi. Vananın başında seni izliyor.";
        nodes.Add(rabis);

        nodes.Add(N("smell",
            "Koku mu? ...Alışırsın. Fabrika boyası falan işte.",
            C("Boya mı? Nehirde ölü balıklar var.", "fish"),
            C("Borudan nehre akan atığı gördüm. Bu boya değil.", "evidence", requireFlag: PipeClue),
            C("Peki, kolay gelsin.", "leave_now")));

        nodes.Add(N("leave_now", "Sana da. Hadi, burada durma."));

        // ---------- Asıl derdi ----------
        nodes.Add(N("fish",
            "...Bak, ben sadece bir işçiyim. Gece \"vanayı aç\" derler, açarım. Evde beni bekleyen bir kızım var. İşimi kaybedemem.",
            C("Kızın bu şehrin suyunu içiyor, değil mi?", "daughter"),
            C("Yani suç senin değil, patronların.", "blame"),
            C("Borudan nehre akan atığı kendi gözümle gördüm.", "evidence", requireFlag: PipeClue),
            C("Belediye bile tabela asmış: bu su tehlikeli.", "evidence", requireFlag: SignClue)));

        nodes.Add(N("blame",
            "Doğru... Ama bunu söylemek kolay. Biz sesimizi çıkarırsak ilk bizi kapı dışarı ederler.",
            C("Kızın bu şehrin suyunu içiyor, değil mi?", "daughter"),
            C("Borudan nehre akan atığı kendi gözümle gördüm.", "evidence", requireFlag: PipeClue),
            C("Haklısın. Seni daha fazla oyalamayayım.", "leave_now")));

        nodes.Add(N("daughter",
            "...Evet. Geçen hafta karnı ağrıdı, doktor sudan olabilir dedi. \"Bilmiyordum\" diyemem aslında... Ne yapmamı istiyorsun?",
            C("Rabiş kanıtı toplarsa fabrika arıtma kurmak zorunda kalır. Kimse işini kaybetmez, sadece atık temizlenir.", "ally"),
            C("Bırak örneği alayım. Kimseye senden bahsetmem.", "deal")));

        nodes.Add(N("evidence",
            "Gördün demek... Evet. Gece vanayı açıyoruz. Arıtma pahalıymış, patron öyle diyor. Ne yapmamı istiyorsun?",
            C("Rabiş kanıtı toplarsa fabrika arıtma kurmak zorunda kalır. Kimse işini kaybetmez, sadece atık temizlenir.", "ally"),
            C("Bırak örneği alayım. Kimseye senden bahsetmem.", "deal")));

        // ---------- Sonuçlar ----------
        var ally = N("ally",
            "Arıtma... Bunu yıllardır istiyoruz ama kimse sesimizi duymadı. Tamam, varım. Vardiyaya giriyorum, borunun başı boş kalacak. " +
            "Şunu da al: vana kayıt defterinden bir sayfa. Gece hangi saatlerde atık verdiğimiz yazıyor. Rabiş'e ver.");
        ally.setFlag = AllyFlag;
        ally.toast = "Emirhan'dan vana kayıt sayfasını aldın!";
        ally.next = "ally_pass";
        nodes.Add(ally);
        var allyPass = N("ally_pass", "Kızım için... Hadi, çabuk ol.");
        allyPass.setFlag = PassFlag;
        nodes.Add(allyPass);

        var deal = N("deal",
            "...Ben bir şey görmedim. Vardiyam başlıyor, içeri giriyorum. Döndüğümde burada olma.");
        deal.setFlag = PassFlag;
        nodes.Add(deal);

        // ---------- Şüphelendikten sonra ----------
        nodes.Add(N("again",
            "Yine mi sen? Söyledim, borudan uzak dur.",
            C("Haklısın, öyle pat diye söylememeliydim. Sadece suyun neden böyle olduğunu anlamak istiyorum.", "calm"),
            C("Borudan akan atığı kendi gözümle gördüm. Bunu benden saklayamazsın.", "evidence", requireFlag: PipeClue),
            C("Tamam, gidiyorum.", "leave_now")));

        nodes.Add(N("calm",
            "Anlamak mı? ...Anlayacak bir şey yok. Bana \"vanayı aç\" derler, açarım. Evde bir kızım var, işimi kaybedemem.",
            C("Kızın bu şehrin suyunu içiyor, değil mi?", "daughter"),
            C("Yani suç senin değil, patronların.", "blame"),
            C("Belediye bile tabela asmış: bu su tehlikeli.", "evidence", requireFlag: SignClue)));

        // ---------- İkna olduktan sonra (fabrikaya girmeden yakalarsan) ----------
        nodes.Add(N("done", "Vardiyam başladı. Çabuk ol, kimse görmeden."));

        d.nodes = nodes;
    }
}
#endif
