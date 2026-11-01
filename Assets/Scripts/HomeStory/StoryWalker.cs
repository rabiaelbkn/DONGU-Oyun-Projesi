using UnityEngine;

/// <summary>
/// Hikâye için sahneye gelen NPC'yi yürütür (kapıdan girer, oyuncuya yaklaşır, çıkıp gider).
/// Konuşma yarıda kapatılırsa "[E] Konuş" ile tekrar açılabilir.
/// </summary>
public class StoryWalker : MonoBehaviour, IInteractable
{
    public float speed = 1.6f;
    public string prompt = "Konuş";

    /// <summary>E ile konuşulunca çağrılır. null ise NPC etkileşime kapalıdır.</summary>
    public System.Action onTalk;

    public bool Arrived { get; private set; }
    public bool Moving => moving;

    static readonly int SpeedHash = Animator.StringToHash("Speed");

    Animator anim;
    SpriteRenderer sr;
    NPCInteract2D card;
    Transform follow;
    float followGap, targetX;
    bool moving, destroyOnArrive;
    System.Action arriveCallback;

    public string InteractPrompt => onTalk != null && !moving ? prompt : null;
    public void Interact() => onTalk?.Invoke();

    void Awake()
    {
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        card = GetComponent<NPCInteract2D>();
        var wander = GetComponent<NPCWander2D>();
        if (wander) wander.enabled = false; // hareketi bu bileşen yönetir
    }

    /// <summary>Oyuncuya "gap" mesafesine kadar yaklaşır (oyuncu hareket ederse takip eder).</summary>
    public void Approach(Transform target, float gap, System.Action arrived = null)
    {
        follow = target; followGap = gap; destroyOnArrive = false;
        arriveCallback = arrived; Arrived = false; moving = true;
    }

    /// <summary>Belirli bir x noktasına yürür; leave=true ise varınca sahneden çıkar.</summary>
    public void WalkTo(float x, bool leave, System.Action arrived = null)
    {
        follow = null; targetX = x; destroyOnArrive = leave;
        arriveCallback = arrived; Arrived = false; moving = true;
        if (leave)
        {
            onTalk = null;
            if (card)
            {
                card.enabled = false;
                if (card.card) card.card.gameObject.SetActive(false);
            }
        }
    }

    public void Face(float worldX) { if (sr) sr.flipX = worldX < transform.position.x; }

    void Update()
    {
        float speedNow = 0f;
        if (moving && !GameUI.InteractionBlocked)
        {
            if (follow != null)
            {
                float side = transform.position.x <= follow.position.x ? -1f : 1f;
                targetX = follow.position.x + side * followGap;
            }

            Vector3 p = transform.position;
            float dx = targetX - p.x;
            if (Mathf.Abs(dx) <= 0.05f)
            {
                moving = false;
                Arrived = true;
                if (follow != null) Face(follow.position.x);
                var cb = arriveCallback; arriveCallback = null;
                cb?.Invoke();
                if (destroyOnArrive) { Destroy(gameObject); return; }
            }
            else
            {
                p.x = Mathf.MoveTowards(p.x, targetX, speed * Time.deltaTime);
                transform.position = p;
                if (sr) sr.flipX = dx < 0f;
                speedNow = speed;
            }
        }
        else if (!moving && follow != null)
        {
            Face(follow.position.x);
            // oyuncu uzaklaşırsa tekrar yaklaş
            if (Mathf.Abs(follow.position.x - transform.position.x) > followGap + 1.5f) { moving = true; Arrived = false; }
        }
        if (anim) anim.SetFloat(SpeedHash, speedNow);
    }
}
