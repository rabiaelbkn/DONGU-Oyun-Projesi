/// <summary>
/// Hikâye bayrakları. Geçmiş sahnesinin GameFlags'i ile aynı depoyu kullanır,
/// böylece Rabiş'in nehir görevi (kanıt teslimi) gelecekteki gazeteyi de etkiler.
/// </summary>
public static class StoryProgress
{
    public static int CleanWater { get; private set; } = 3;

    // Run even when Unity's Enter Play Mode options disable domain reload.
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void BeginPlaySession()
    {
        CleanWater = 3;
    }

    public static bool Has(string flag)
    {
        if (GameFlags.Has(flag)) return true;
        // Geçmişte Rabiş'e su örneği teslim edildiyse nehir kanıtı bulunmuş sayılır.
        return flag == "NehirKanitiBulundu" && GameFlags.Has("kanit_teslim");
    }

    public static void Set(string flag) => GameFlags.Set(flag);

    public static void AddWater(int amount = 1) => CleanWater += amount;

    public static bool SpendWater(int amount = 1)
    {
        if (CleanWater < amount) return false;
        CleanWater -= amount;
        return true;
    }

    public static string NewspaperHeadline(int page)
    {
        switch (page)
        {
            case 1:
                return Has("DepoSuyuPaylasildi") && Has("AileyeYardimEdildi")
                    ? "İçme Suyu Kısıtlamaları Hafifletildi"
                    : "Son Temiz Su Kaynakları da Tükeniyor!";
            case 2:
                return Has("NehirKanitiBulundu") && Has("AritmaDesteklendi") && Has("HalkTemizligeKatildi")
                    ? "Ardıç Nehri’nde İlk İyileşme"
                    : "Nehirler Kirlilikten Kurtulamıyor!";
            case 3:
                return Has("DepoSuyuPaylasildi") && Has("AileyeYardimEdildi") && Has("TarimSuPlaniOlustu")
                    ? "Barajlarda Toparlanma, Çiftçilere Nefes"
                    : "Barajlar Kuruyor, Tarım Alarmda!";
            default:
                return string.Empty;
        }
    }

    public static string NewspaperBody(int page)
    {
        bool improved = !string.IsNullOrEmpty(NewspaperHeadline(page)) &&
                        NewspaperHeadline(page) != InitialHeadline(page);
        if (!improved)
        {
            switch (page)
            {
                case 1: return "Uzmanlar, artan kuraklık ve su kaynaklarının bilinçsiz kullanımı nedeniyle temiz su miktarının hızla azaldığını söylüyor. Bazı mahallelerde kesintiler başladı.";
                case 2: return "Fabrika atıkları ve yetersiz denetimler nedeniyle Ardıç Nehri’ndeki kirlilik artıyor. Bölge halkı önlem bekliyor.";
                case 3: return "Artan sıcaklıklar ve yetersiz yağışlar baraj seviyelerini kritik düzeye indirdi. Çiftçiler tarlalarını sulamakta zorlanıyor.";
            }
        }
        switch (page)
        {
            case 1: return "Mahallede başlatılan su paylaşımı ve destek çalışmaları ilk sonuçlarını verdi. Yetkililer bazı bölgelerde kesintilerin azaltılacağını duyurdu.";
            case 2: return "Arıtma talebi, nehir kirliliğine dair kanıtlar ve kıyı temizliği ilk iyileşmeyi sağladı. Uzmanlar denetimlerin sürmesi gerektiğini belirtiyor.";
            case 3: return "Su paylaşımı ve tarlalarda tasarruf planı, çiftçilerin kayıplarını azaltmaya başladı. Uzmanlar çalışmaların sürdürülmesini istiyor.";
            default: return string.Empty;
        }
    }

    private static string InitialHeadline(int page)
    {
        switch (page)
        {
            case 1: return "Son Temiz Su Kaynakları da Tükeniyor!";
            case 2: return "Nehirler Kirlilikten Kurtulamıyor!";
            case 3: return "Barajlar Kuruyor, Tarım Alarmda!";
            default: return string.Empty;
        }
    }

    public static void ResetForNewGame()
    {
        GameFlags.ClearAll();
        CleanWater = 3;
    }
}
