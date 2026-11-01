using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Kapı / zaman kapısı: başka sahneye geçirir.</summary>
public class TimeDoorInteraction : MonoBehaviour, IInteractable
{
    public string sceneToLoad = "PastScene";
    [Tooltip("Açıksa, oyuncunun geldiği sahneye döner (yoksa sceneToLoad).")]
    public bool returnToPreviousScene;
    public string prompt = "Kapıdan geç";
    [Tooltip("Açıksa zaman portalı geçiş animasyonu (yakınlaşma + girdap + altın ışık), kapalıysa kısa karartma.")]
    public bool portalEffect;
    [Tooltip("Portal animasyonunda kameranın yakınlaşacağı nokta (boşsa bu nesnenin konumu).")]
    public Transform focus;

    /// <summary>En son kapıdan çıkılan sahne.</summary>
    public static string PreviousScene { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() => PreviousScene = null;

    public string InteractPrompt => prompt;

    public void Interact()
    {
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        string target = sceneToLoad;
        if (returnToPreviousScene && !string.IsNullOrEmpty(PreviousScene)) target = PreviousScene;
        PreviousScene = SceneManager.GetActiveScene().name;
        PortalTransition.Play(target, focus ? focus.position : transform.position, portalEffect);
    }
}
