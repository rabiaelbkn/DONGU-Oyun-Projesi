#if UNITY_EDITOR
using System.Collections.Generic;

/// <summary>
/// Bilim insanı Rabiş'in diyalog ağacı. Kurulumda Assets/PastScene/Data/Rabis_Dialogue.asset dosyasına yazılır;
/// sonra metinleri Inspector'dan da düzenleyebilirsin.
///
/// Bayraklar: rabis_tanisti → gorev_alindi → ornek_alindi → kanit_teslim
/// </summary>
public static class RabisDialogue
{
    static DialogueData.Choice C(string text, string next, string setFlag = null, string requireFlag = null, string hideIfFlag = null) =>
        new DialogueData.Choice { text = text, next = next, setFlag = setFlag, requireFlag = requireFlag, hideIfFlag = hideIfFlag };

    static DialogueData.Node N(string id, string text, params DialogueData.Choice[] choices) =>
        new DialogueData.Node { id = id, text = text, choices = new List<DialogueData.Choice>(choices) };

    public static void Fill(DialogueData d)
    {
        d.speakerName = "Rabiş — Çevre Araştırmacısı";

        d.startRules = new List<DialogueData.StartRule>
        {
            new DialogueData.StartRule { requireFlag = "kanit_teslim", nodeId = "after" },
            new DialogueData.StartRule { requireFlag = "ornek_alindi", nodeId = "return" },
            new DialogueData.StartRule { requireFlag = "gorev_alindi", nodeId = "reminder" },
            new DialogueData.StartRule { requireFlag = "rabis_tanisti", nodeId = "again" },
            new DialogueData.StartRule { requireFlag = "", nodeId = "start" },
        };

        var nodes = new List<DialogueData.Node>();

        // ---------- İlk karşılaşma ----------
        var start = N("start",
            "Hey, oraya fazla yaklaşma! Bu suya dokunmak bile tehlikeli olabilir.",
            C("Sen kimsin?", "who"),
            C("Nehre ne oldu böyle?", "river"),
            C("Burada ne yapıyorsun?", "research"),
            C("Kusura bakma, gitmem lazım.", "bye"));
        start.setFlag = "rabis_tanisti";
        nodes.Add(start);

        nodes.Add(N("again",
            "Yine geldin. Nehir hâlâ aynı... Ne konuşmak istersin?",
            C("Nehre ne oldu?", "river"),
            C("Kanıt konusunu konuşalım.", "proof"),
            C("Sonra konuşuruz.", "bye")));

        nodes.Add(N("who",
            "Ben Rabiş, çevre araştırmacısıyım. Aylardır bu nehirden su örnekleri alıp laboratuvarda inceliyorum.",
            C("Nehre ne oldu böyle?", "river"),
            C("Tam olarak ne araştırıyorsun?", "research"),
            C("Görüşürüz.", "bye")));

        nodes.Add(N("research",
            "Fabrikanın atıklarının bu nehri öldürdüğünü bilimsel olarak kanıtlamaya çalışıyorum. Ama elimdeki veriler henüz yeterli değil.",
            C("Neden yeterli değil?", "proof"),
            C("Nehre ne oldu?", "river")));

        nodes.Add(N("river",
            "Şu boruyu görüyor musun? Fabrika kimyasal atıklarını hiç arıtmadan doğrudan nehre boşaltıyor. Suyun rengi, bu koku... hepsi o atıklar yüzünden.",
            C("Canlılara ne oluyor?", "life"),
            C("Bunun insanlara zararı var mı?", "people"),
            C("Bunu kanıtlayabilir misin?", "proof")));

        nodes.Add(N("life",
            "Sudaki oksijen azaldı, zehirli maddeler birikti. Balıklar, kurbağalar birer birer ölüyor. Kıyıya vuran ölü balıkları sen de gördün.",
            C("İnsanlar nasıl etkileniyor?", "people"),
            C("Kanıtın var mı?", "proof")));

        nodes.Add(N("people",
            "Bu nehir şehrin temiz su kaynaklarını besliyor. Kirlilik böyle sürerse içilebilir su azalacak, insanlar hastalanacak, su için kuyruklara girecekler.",
            C("Ben gelecekten geliyorum... Söylediklerin gerçek oluyor.", "future"),
            C("Kanıtın var mı?", "proof", hideIfFlag: "gorev_alindi"),
            C("Ne yapabiliriz?", "help", hideIfFlag: "gorev_alindi"),
            C("Anladım, örneği almaya gidiyorum.", "", requireFlag: "gorev_alindi")));

        nodes.Add(N("future",
            "Gelecekten mi?! ...Yüzündeki ifade şaka yapmadığını söylüyor. Demek gazeteler su kriziyle dolacak. O zaman bunu şimdi durdurmak zorundayız!",
            C("Nasıl yardım edebilirim?", "help", hideIfFlag: "gorev_alindi"),
            C("Örneği almaya gidiyorum!", "", requireFlag: "gorev_alindi")));

        nodes.Add(N("proof",
            "Eski örneklerde ağır metal oranı normalin çok üstünde çıktı. Ama fabrika 'o örnekler başka yerden alındı' diyor. Bana borunun tam döküldüğü yerden yeni bir örnek lazım.",
            C("Ben getiririm!", "accept"),
            C("Neden kendin almıyorsun?", "why"),
            C("Fabrikayla konuşsana.", "factory")));

        nodes.Add(N("why",
            "Fabrikanın güvenlik görevlileri beni tanıyor; borunun yanına her yaklaştığımda kovalıyorlar. Ama seni tanımıyorlar...",
            C("Tamam, ben alırım.", "accept"),
            C("Biraz düşüneyim.", "bye_think")));

        nodes.Add(N("factory",
            "Denedim. 'Kanıtın yoksa konuşma' dediler. Orada çalışan Emirhan bile işini kaybetmekten korktuğu için susuyor.",
            C("O zaman kanıtı ben getiririm.", "accept"),
            C("Anlıyorum.", "bye_think")));

        nodes.Add(N("help",
            "Fabrikanın suçunu kanıtlarsak yetkililer onu durdurmak zorunda kalır. Borunun döküldüğü yerden bir su örneğine ihtiyacım var.",
            C("Ben getiririm!", "accept"),
            C("Neden kendin almıyorsun?", "why")));

        var accept = N("accept",
            "Harika! Kıyıda, sarı uyarı tabelasının yanında boş bir numune şişesi bıraktım. Borunun altından suyu doldurup bana getir. Ama dikkat et: fabrika işçisi Emirhan vananın başında çalışıyor. O oradayken örneği alamazsın... Belki onunla konuşursan seni anlar.");
        accept.setFlag = "gorev_alindi";
        accept.toast = "GÖREV: Emirhan'la konuşup vananın başından ayrılmasını sağla, su örneğini al ve Rabiş'e getir.";
        nodes.Add(accept);

        nodes.Add(N("bye", "Dikkatli ol. Ve sakın o sudan içme!"));
        nodes.Add(N("bye_think", "Acele et ama... Nehir her gün biraz daha ölüyor."));

        // ---------- Görev sürerken ----------
        nodes.Add(N("reminder",
            "Şişe sarı uyarı tabelasının yanında. Emirhan vananın başındayken örneği alamazsın. Onunla konuş; kötü biri değil, sadece korkuyor. Etrafta bulduğun ipuçları da işine yarayabilir.",
            C("Tamam, hemen gidiyorum.", "ok"),
            C("Bu kirlilik insanlara tam olarak ne yapıyor?", "people")));
        nodes.Add(N("ok", "Sana güveniyorum."));

        // ---------- Örnek getirildi ----------
        var ret = N("return",
            "Örneği getirdin! Hemen ölçüm yapayım... Bak! Ağır metal ve kimyasal oranı güvenli sınırın kat kat üstünde. Bu, atıkların doğrudan fabrikadan geldiğinin kanıtı!",
            C("Emirhan'dan vana kayıt sayfasını da aldım.", "logbook", requireFlag: "emirhan_muttefik", hideIfFlag: "kayit_verildi"),
            C("Şimdi ne olacak?", "next_steps"),
            C("Bu kanıt geleceği değiştirebilir mi?", "change"));
        ret.setFlag = "kanit_teslim";
        ret.toast = "Kanıt Rabiş'e teslim edildi!";
        nodes.Add(ret);
        var logbook = N("logbook",
            "Vana kayıtları mı?! Bu, atığın bilerek ve her gece düzenli verildiğini kanıtlıyor! Emirhan da arıtma için tanıklık ederse fabrika artık kaçamaz. İşçileri de karşımıza almamış oluruz.",
            C("Şimdi ne olacak?", "next_steps"),
            C("Bu kanıt geleceği değiştirebilir mi?", "change"));
        logbook.setFlag = "kayit_verildi";
        logbook.toast = "Vana kayıt sayfası Rabiş'e teslim edildi!";
        nodes.Add(logbook);

        nodes.Add(N("next_steps",
            "Bu raporu yetkililere ve gazetelere götüreceğim. Fabrika atıklarını arıtmak zorunda kalacak. Nehir yeniden canlanabilir!",
            C("Geleceğe dönüp ne değiştiğini görmeliyim.", "go_back"),
            C("Teşekkürler Rabiş.", "thanks")));

        nodes.Add(N("change",
            "Eğer gerçekten gelecekten geldiysen... bugün attığımız her adım, oradaki insanların temiz suya ulaşmasını sağlayabilir.",
            C("Geleceğe dönmeliyim.", "go_back"),
            C("Şimdi ne olacak?", "next_steps")));

        var goBack = N("go_back",
            "Zaman kapısı geldiğin yerde, batı tarafında. Git ve gör... Belki gazeteler artık farklı şeyler yazar.");
        goBack.toast = "Zaman kapısından (batıda) geleceğe dönebilirsin.";
        nodes.Add(goBack);

        nodes.Add(N("thanks", "Asıl ben teşekkür ederim. Sen olmasan bu kanıtı asla toplayamazdım."));

        nodes.Add(N("after",
            "Raporu hazırlıyorum. Zaman kapısı batı tarafında, geleceğe dönebilirsin!",
            C("Emirhan'dan vana kayıt sayfasını da aldım.", "logbook", requireFlag: "emirhan_muttefik", hideIfFlag: "kayit_verildi"),
            C("Nehir kurtulacak mı?", "change"),
            C("Hoşça kal, Rabiş.", "thanks")));

        d.nodes = nodes;
    }
}
#endif
