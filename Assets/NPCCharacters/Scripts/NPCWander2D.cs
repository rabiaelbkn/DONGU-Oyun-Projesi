using UnityEngine;

/// <summary>
/// NPC'yi başlangıç noktası etrafında sağa-sola dolaştırır: yürür, durur, bekler, tekrar yürür.
/// Animator'daki "Speed" parametresini günceller (0 = Idle, &gt;0 = Walk).
/// </summary>
[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public class NPCWander2D : MonoBehaviour
{
    public NPCData data;

    [Header("Data atanmamışsa kullanılır")]
    public float walkSpeed = 0.8f;
    public float wanderRadius = 2.5f;
    public Vector2 idleTimeRange = new Vector2(1.5f, 4f);

    [Tooltip("Kapalıysa NPC hep yerinde bekler.")]
    public bool wander = true;

    /// <summary>Oyuncuyla konuşurken true olur; NPC durur.</summary>
    [HideInInspector] public bool paused;

    static readonly int SpeedHash = Animator.StringToHash("Speed");

    Animator anim;
    SpriteRenderer sr;
    float homeX, targetX, waitUntil;
    bool walking;

    float Speed => data ? data.walkSpeed : walkSpeed;
    float Radius => data ? data.wanderRadius : wanderRadius;
    Vector2 IdleRange => data ? data.idleTimeRange : idleTimeRange;

    void Awake()
    {
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        homeX = transform.position.x;
        waitUntil = Time.time + Random.Range(0.2f, IdleRange.y);
        // Aynı anda yürümesinler diye animasyonu rastgele bir yerden başlat
        anim.Play(0, 0, Random.value);
    }

    void Update()
    {
        float speedNow = 0f;

        if (!paused && wander)
        {
            if (!walking && Time.time >= waitUntil)
            {
                targetX = homeX + Random.Range(-Radius, Radius);
                walking = Mathf.Abs(targetX - transform.position.x) > 0.2f;
                if (!walking) waitUntil = Time.time + Random.Range(IdleRange.x, IdleRange.y);
            }

            if (walking)
            {
                Vector3 p = transform.position;
                float dir = Mathf.Sign(targetX - p.x);
                p.x = Mathf.MoveTowards(p.x, targetX, Speed * Time.deltaTime);
                transform.position = p;
                sr.flipX = dir < 0f;
                speedNow = Speed;

                if (Mathf.Abs(p.x - targetX) < 0.01f)
                {
                    walking = false;
                    waitUntil = Time.time + Random.Range(IdleRange.x, IdleRange.y);
                }
            }
        }

        anim.SetFloat(SpeedHash, speedNow);
    }

    /// <summary>NPC'yi belirli bir yöne baktırır (konuşurken oyuncuya dönmek için).</summary>
    public void Face(float worldX)
    {
        sr.flipX = worldX < transform.position.x;
    }

    void OnDrawGizmosSelected()
    {
        float r = data ? data.wanderRadius : wanderRadius;
        float cx = Application.isPlaying ? homeX : transform.position.x;
        Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.8f);
        Vector3 a = new Vector3(cx - r, transform.position.y, 0), b = new Vector3(cx + r, transform.position.y, 0);
        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(a + Vector3.up * 0.2f, a - Vector3.up * 0.2f);
        Gizmos.DrawLine(b + Vector3.up * 0.2f, b - Vector3.up * 0.2f);
    }
}
