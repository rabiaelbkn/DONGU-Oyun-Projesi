using UnityEngine;

/// <summary>
/// Oyuncu yaklaşınca NPC durur, oyuncuya döner ve başının üstünde isim kartını gösterir.
/// Oyuncu şu yollardan biriyle tanınır: Tag'i "Player" olan obje, ya da üzerinde HeroController2D bulunan obje.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class NPCInteract2D : MonoBehaviour
{
    [Tooltip("Başın üstünde açılan kart (child obje).")]
    public Transform card;
    public float popTime = 0.15f;

    public event System.Action<NPCInteract2D> PlayerEntered;
    public event System.Action<NPCInteract2D> PlayerExited;

    NPCWander2D wanderer;
    Transform player;
    Vector3 cardScale = Vector3.one;
    float shown;   // 0..1
    bool show;

    void Awake()
    {
        wanderer = GetComponent<NPCWander2D>();
        GetComponent<Collider2D>().isTrigger = true;
        if (card != null)
        {
            cardScale = card.localScale;
            card.localScale = Vector3.zero;
            card.gameObject.SetActive(false);
        }
    }

    static bool IsPlayer(Collider2D other)
    {
        if (other.CompareTag("Player")) return true;
        var rb = other.attachedRigidbody;
        return rb != null && (rb.CompareTag("Player") || rb.GetComponent("HeroController2D") != null);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        player = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
        show = true;
        if (wanderer) wanderer.paused = true;
        if (card) card.gameObject.SetActive(true);
        PlayerEntered?.Invoke(this);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        show = false;
        player = null;
        if (wanderer) wanderer.paused = false;
        PlayerExited?.Invoke(this);
    }

    void Update()
    {
        if (player != null && wanderer) wanderer.Face(player.position.x);
        if (card == null) return;

        shown = Mathf.MoveTowards(shown, show ? 1f : 0f, Time.deltaTime / Mathf.Max(0.01f, popTime));
        float s = show ? EaseOutBack(shown) : shown;
        card.localScale = cardScale * s;
        if (!show && shown <= 0f && card.gameObject.activeSelf) card.gameObject.SetActive(false);
    }

    static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
