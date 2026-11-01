using System.Collections;
using UnityEngine;

/// <summary>
/// Ev içindeki dolabın kapakları (arka plandaki dolabın kendi kapakları).
/// Açılınca kapaklar menteşelerinden dışarı doğru döner, içi kararır ve içinde zaman girdabı belirir.
/// Kapak sprite'larının pivotu menteşe tarafındadır (sol kapak: sol kenar, sağ kapak: sağ kenar);
/// x ölçeği 1 -> -0.47 arasında değişince kapak dışarıya doğru katlanır.
/// </summary>
public class CabinetDoors : MonoBehaviour
{
    public SpriteRenderer leftDoor, rightDoor;
    public GameObject inside;
    public SpriteRenderer vortex, glow;
    public float openTime = 0.6f;
    [Tooltip("Kapakların döndüğü en büyük açı (derece).")]
    public float maxAngle = 118f;

    float k;            // 0 kapalı .. 1 açık
    Coroutine routine;

    Vector3 vortexScale = Vector3.one;

    void Awake()
    {
        if (vortex) vortexScale = vortex.transform.localScale;
        Apply(0f);
    }

    void Update()
    {
        if (vortex && k > 0f) vortex.transform.localScale = vortexScale * (1f + Mathf.Sin(Time.time * 2.2f) * 0.03f);
    }

    public void Open() => Animate(1f);
    public void Close() => Animate(0f);

    void Animate(float target)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Run(target));
    }

    IEnumerator Run(float target)
    {
        float from = k;
        float time = openTime * Mathf.Abs(target - from);
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            float x = t / time;
            Apply(Mathf.Lerp(from, target, x * x * (3f - 2f * x)));
            yield return null;
        }
        Apply(target);
    }

    void Apply(float value)
    {
        k = value;
        float ang = k * maxAngle * Mathf.Deg2Rad;
        float s = Mathf.Cos(ang);
        float grow = 1f + Mathf.Abs(Mathf.Sin(ang)) * 0.05f;   // kapak bize doğru açılırken hafif büyür
        float shade = 1f - 0.24f * (1f - Mathf.Abs(s));
        Color c = s > 0f ? new Color(shade, shade, shade, 1f) : new Color(0.62f * shade, 0.62f * shade, 0.62f * shade, 1f);
        foreach (var d in new[] { leftDoor, rightDoor })
        {
            if (!d) continue;
            d.transform.localScale = new Vector3(Mathf.Abs(s) < 0.02f ? 0.02f * Mathf.Sign(s == 0 ? 1 : s) : s, grow, 1f);
            d.color = c;
        }
        if (inside) inside.SetActive(k > 0.001f);
        float va = Mathf.Clamp01((k - 0.25f) / 0.6f);
        if (vortex) vortex.color = new Color(1f, 1f, 1f, va);
        if (glow) glow.color = new Color(1f, 1f, 1f, va);
    }
}
