#if DONGU_RECORDER
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

/// <summary>
/// SUNUM İÇİN VİDEO KAYDI (Unity Recorder paketi gerekir)
///   Tools > DÖNGÜ > Video Kaydı > Kaydı Başlat / Durdur   (kısayol: F9)
/// Play modunda değilsen önce Play'e geçer, sonra kaydı başlatır.
/// Kayıt: Game görünümü, 1920x1080, 30 fps, sesli MP4 -> proje klasöründe "Kayitlar/DONGU_tarih_saat.mp4"
/// Play modundan çıkınca kayıt kendiliğinden durur ve klasör açılır.
/// </summary>
public static class DonguVideoRecorder
{
    const string MenuPath = "Tools/DÖNGÜ/Video Kaydı/Kaydı Başlat ∕ Durdur _F9";
    const string PendingKey = "DONGU.Recorder.StartOnPlay";

    static RecorderController controller;
    static string lastFile;

    public static bool IsRecording => controller != null && controller.IsRecording();

    static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Kayitlar"));

    [InitializeOnLoadMethod]
    static void Init()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem(MenuPath, false, 0)]
    static void Toggle()
    {
        if (IsRecording) { Stop(); return; }
        if (!EditorApplication.isPlaying)
        {
            SessionState.SetBool(PendingKey, true);
            EditorApplication.isPlaying = true; // Play başlayınca kayıt otomatik başlar
            return;
        }
        Start();
    }

    [MenuItem(MenuPath, true)]
    static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, IsRecording);
        return true;
    }

    [MenuItem("Tools/DÖNGÜ/Video Kaydı/Kayıt Klasörünü Aç", false, 1)]
    static void OpenFolder()
    {
        Directory.CreateDirectory(Folder);
        EditorUtility.RevealInFinder(Folder + Path.DirectorySeparatorChar);
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingKey, false))
        {
            SessionState.SetBool(PendingKey, false);
            Start();
        }
        else if (state == PlayModeStateChange.ExitingPlayMode && IsRecording)
        {
            Stop();
        }
    }

    static void Start()
    {
        Directory.CreateDirectory(Folder);
        lastFile = Path.Combine(Folder, "DONGU_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));

        var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movie.name = "DONGU Sunum Kaydı";
        movie.Enabled = true;
        movie.EncoderSettings = new CoreEncoderSettings
        {
            Codec = CoreEncoderSettings.OutputCodec.MP4,
            EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High
        };
        movie.ImageInputSettings = new GameViewInputSettings { OutputWidth = 1920, OutputHeight = 1080 };
        movie.AudioInputSettings.PreserveAudio = true;
        movie.OutputFile = lastFile; // uzantı (.mp4) otomatik eklenir

        settings.AddRecorderSettings(movie);
        settings.SetRecordModeToManual();
        settings.FrameRate = 30f;
        settings.CapFrameRate = true;

        controller = new RecorderController(settings);
        controller.PrepareRecording();
        controller.StartRecording();
        Debug.Log("<color=#ff5555>● KAYIT BAŞLADI</color> — durdurmak için F9 (veya Play'den çık). Dosya: " + lastFile + ".mp4");
    }

    static void Stop()
    {
        if (controller == null) return;
        controller.StopRecording();
        controller = null;
        Debug.Log("■ KAYIT BİTTİ: " + lastFile + ".mp4");
        EditorApplication.delayCall += OpenFolder;
    }
}
#endif
