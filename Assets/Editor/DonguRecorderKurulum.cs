#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

/// <summary>
/// Video kayıt aracının ihtiyaç duyduğu Unity Recorder paketini tek tıkla kurar.
/// Tools > DÖNGÜ > Video Kaydı > Recorder Paketini Kur
/// Paket kurulunca "Kaydı Başlat / Durdur (F9)" menüsü kendiliğinden görünür.
/// </summary>
public static class DonguRecorderKurulum
{
    [MenuItem("Tools/DÖNGÜ/Video Kaydı/Recorder Paketini Kur (ilk sefer)", false, 20)]
    static void Install()
    {
        Debug.Log("[DÖNGÜ] Unity Recorder paketi kuruluyor... Bitince 'Tools > DÖNGÜ > Video Kaydı > Kaydı Başlat / Durdur (F9)' görünecek.");
        Client.Add("com.unity.recorder");
    }
}
#endif
