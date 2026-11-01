using System;

[Serializable]
public class StoryChoice
{
    public string Text;
    public string Result;
    public Action Apply;
    public bool RequiresWater;

    public StoryChoice(string text, string result, Action apply, bool requiresWater = false)
    {
        Text = text;
        Result = result;
        Apply = apply;
        RequiresWater = requiresWater;
    }
}

public class StoryScenario
{
    public string Key;
    public string Speaker;
    public string Line;
    public StoryChoice[] Choices;

    public StoryScenario(string key, string speaker, string line, params StoryChoice[] choices)
    {
        Key = key;
        Speaker = speaker;
        Line = line;
        Choices = choices;
    }
}

public static class StoryDialogueCatalog
{
    public static StoryScenario Get(StoryCharacter character)
    {
        switch (character)
        {
            case StoryCharacter.Elif: return Elif();
            case StoryCharacter.AnneVeDeniz: return AnneVeDeniz();
            case StoryCharacter.Rabis: return Rabis();
            case StoryCharacter.Zeynep: return Zeynep();
            case StoryCharacter.Emirhan: return Emirhan();
            default: return null;
        }
    }

    private static StoryScenario Elif() => new StoryScenario(
        "Elif",
        "Elif",
        "Üç gündür doğru düzgün su içemedim. Depoda biraz su olduğunu biliyorum ama kimse paylaşmıyor.",
        new StoryChoice("Sana kendi suyumdan verebilirim.",
            "Elif sana güvenmeye başlıyor. Matarandaki temiz su azalıyor.",
            () => StoryProgress.Set("ElifGuvendi"), true),
        new StoryChoice("Depodaki suyu birlikte bulalım.",
            "Depoda kalan temiz suyu bulup paylaşıyorsunuz. Elif, mahallede suyu paylaşma çağrına yardım edecek.",
            () => { StoryProgress.Set("DepoSuyuPaylasildi"); StoryProgress.Set("ElifGuvendi"); }),
        new StoryChoice("Herkes kendi başının çaresine baksın.",
            "Elif konuşmayı kesip uzaklaşıyor. Depodaki suyun yerini öğrenemiyorsun.",
            () => StoryProgress.Set("ElifGuvenKaybi"))
    );

    private static StoryScenario AnneVeDeniz() => new StoryScenario(
        "AnneVeDeniz",
        "Anne ve Deniz",
        "Anne: Deniz susuz kalınca kirli Ardıç Nehri’nden su içti. Nehir suyu yüzünden hastalandı; şimdi karnı ağrıyor ve halsiz. Temiz suya ve yardıma ihtiyacımız var.\nDeniz: Anne, karnım çok ağrıyor...",
        new StoryChoice("Mataramdaki temiz suyu Deniz’e vereyim.",
            "Temiz su Deniz’in susuzluğunu gideriyor ama nehirden kaptığı hastalık geçmiyor. Aile sana güveniyor; Anne, tarlada suyu dikkatli kullanma yöntemini de paylaşıyor.",
            SetFamilyHelp, true),
        new StoryChoice("Sizi Rabiş’e götüreyim; hem yardım buluruz hem nehir suyunu gösteririz.",
            "Rabiş nehir suyundan numune alıp kirliliği belgeliyor ve aileyi yardım noktasına yönlendiriyor. Deniz’in hastalığının kirli sudan kaynaklandığı anlaşılır. Anne, tarladaki su tasarrufu yöntemini kayda geçiriyor.",
            () => { StoryProgress.Set("NehirKanitiBulundu"); SetFamilyHelp(); }),
        new StoryChoice("Yardım edemem, yoluma devam etmeliyim.",
            "Aile yardımsız kalıyor. Anne sana güvenmiyor; kirli nehir suyundan hastalanan Deniz’in durumu kötüleşiyor.",
            () => StoryProgress.Set("AileGuvenKaybi"))
    );

    private static void SetFamilyHelp()
    {
        StoryProgress.Set("AileyeYardimEdildi");
        StoryProgress.Set("TarimSuPlaniOlustu");
    }

    private static StoryScenario Rabis() => new StoryScenario(
        "Rabis",
        "Rabiş",
        "Nehirdeki kirliliği ölçtüm ama kaynağını kanıtlamam gerek. Numune almama yardım eder misin?",
        new StoryChoice("Birlikte nehirden numune alalım.",
            "Numune Rabiş’in araştırmasına kanıt sağlıyor.",
            () => StoryProgress.Set("NehirKanitiBulundu")),
        new StoryChoice("Fabrika çıkışını da belgeleyelim.",
            "Atık suyun nehre karıştığı yeri görüntülüyorsunuz. Kanıt Emirhan’la konuşurken kullanılabilir.",
            () => StoryProgress.Set("NehirKanitiBulundu")),
        new StoryChoice("Şimdi risk alamam.",
            "Rabiş araştırmaya tek başına devam ediyor; kanıt bulmak gecikiyor.",
            () => StoryProgress.Set("RabisYardimEdilmedi"))
    );

    private static StoryScenario Zeynep() => new StoryScenario(
        "Zeynep",
        "Zeynep",
        "Kıyıda çöpler ve ölü balıklar var. Herkes buradan uzaklaştı. Burayı tek başıma temizleyemiyorum.",
        new StoryChoice("Mahalleliyi çağıralım, birlikte temizleyelim.",
            "Mahalleli temizliğe katılıyor. Nehir kıyısındaki görünür çöpler azalıyor.",
            () => StoryProgress.Set("HalkTemizligeKatildi")),
        new StoryChoice("Önce atıkların nereden geldiğini araştıralım.",
            "Zeynep seni atık izlerinin başladığı yere götürüyor. Rabiş’in kanıtları güçleniyor.",
            () => StoryProgress.Set("AtikKaynaklariArastirildi")),
        new StoryChoice("Burası benim sorunum değil.",
            "Zeynep temizliği tek başına sürdürmeye çalışıyor. Mahalle desteği azalıyor.",
            () => StoryProgress.Set("ZeynepGuvenKaybi"))
    );

    private static StoryScenario Emirhan() => new StoryScenario(
        "Emirhan",
        "Emirhan",
        "Atık suyun nehre karıştığını gördüm. Patronlara karşı gelirsem işimi kaybedebilirim; sessiz kalırsam nehir kirlenmeye devam edecek.",
        new StoryChoice("Arıtma sistemi kurulmasını birlikte talep edelim.",
            "Emirhan işçilerle konuşmayı kabul ediyor. Fabrikada arıtma sistemi talebi başlıyor.",
            () => StoryProgress.Set("AritmaDesteklendi")),
        new StoryChoice("Elindeki bilgileri Rabiş’e verelim.",
            "Emirhan gördüklerini Rabiş’le paylaşıyor. Kirliliğin kaynağına dair kanıt güçleniyor.",
            () => StoryProgress.Set("NehirKanitiBulundu")),
        new StoryChoice("Şimdilik sessiz kal.",
            "Emirhan kısa vadede işini koruyor; fabrikadan nehre atık akışı sürüyor.",
            () => StoryProgress.Set("EmirhanSessizKaldi"))
    );
}
