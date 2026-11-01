using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Oyun genelindeki görev durumu. Kendiliğinden oluşur ve sahneler arasında kalır.
/// Geçmişte Rabiş'e kanıt teslim edilince (bayrak "kanit_teslim") fabrika görevi tamamlanmış ve
/// kirlilik durdurulmuş sayılır; geleceğe dönünce gazete değişir.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Fabrika görevinin sonucu
    public bool factoryPollutionStopped = false;

    // Fabrika görevi tamamlandı mı?
    public bool factoryMissionCompleted = false;

    bool newspaperNoticeShown;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CreateAtStartup()
    {
        if (Instance != null) return;
        new GameObject("GameManager").AddComponent<GameManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        GameFlags.FlagSet += OnFlagSet;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        GameFlags.FlagSet -= OnFlagSet;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

    void OnFlagSet(string flag)
    {
        if (flag == "kanit_teslim") CompleteFactoryMission(true);
    }

    // Geçmişte fabrika görevi tamamlandığında çağrılır
    public void CompleteFactoryMission(bool pollutionStopped)
    {
        factoryMissionCompleted = true;
        factoryPollutionStopped = pollutionStopped;
        Debug.Log("Fabrika görevi tamamlandı. Kirlilik durduruldu: " + pollutionStopped);
    }

    public void ResetState()
    {
        factoryMissionCompleted = false;
        factoryPollutionStopped = false;
        newspaperNoticeShown = false;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Geleceğe ilk dönüşte gazeteye bakmayı hatırlat
        if (newspaperNoticeShown || !NewspaperUI.HistoryChanged || !scene.name.StartsWith("Future")) return;
        newspaperNoticeShown = true;
        DialogueUI.Toast("Geleceğe döndün... Gazeteye bir bak, bir şeyler değişmiş olabilir!", 6f);
    }
}
