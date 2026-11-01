using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Dolap: ilk etkileşimde açılır ve zaman portalını gösterir, ikincisinde portala girer.</summary>
public class CabinetPortalInteraction : MonoBehaviour, IInteractable
{
    public static bool AnyOpen { get; private set; }
    public GameObject portalVisual;
    [Tooltip("Arka plandaki dolabın kapakları (açılış animasyonu + içinde portal).")]
    public CabinetDoors doors;
    public TimeDoorInteraction timeDoor;
    public string openText = "Dolabı aç";
    public string enterText = "Zaman geçidine gir    <color=#FFD36B>[Esc]</color> Kapat";

    private bool isOpen;
    private int openedFrame;

    public bool IsOpen => isOpen;
    public string InteractPrompt => isOpen ? enterText : openText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() => AnyOpen = false;

    public void Interact()
    {
        if (!isOpen)
        {
            isOpen = true;
            AnyOpen = true;
            openedFrame = Time.frameCount;
            if (portalVisual != null) portalVisual.SetActive(true);
            if (doors != null) doors.Open();
            AudioManager.Sfx("sfx_cabinet");
            StoryText.ChapterOnce(StoryText.Ch3, 0.6f);
            var body = GameObject.Find("Player")?.GetComponent<Rigidbody2D>();
            if (body != null) body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            return;
        }

        if (Time.frameCount > openedFrame && timeDoor != null)
        {
            AnyOpen = false;
            timeDoor.Interact();
        }
    }

    private void Update()
    {
        if (isOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    public void Close()
    {
        isOpen = false;
        AnyOpen = false;
        if (portalVisual != null) portalVisual.SetActive(false);
        if (doors != null) doors.Close();
        AudioManager.Sfx("sfx_cabinet", 0.7f);
    }

    private void OnDisable()
    {
        if (isOpen) AnyOpen = false;
    }
}
