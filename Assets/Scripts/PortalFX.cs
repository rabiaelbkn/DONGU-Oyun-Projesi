using UnityEngine;

/// <summary>Zaman portalı için hafif nefes alma (ölçek) ve parıltı efekti. Tüm portallarda aynı.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PortalFX : MonoBehaviour
{
    public float pulseAmount = 0.035f;
    public float pulseSpeed = 2.2f;
    [Range(0f, 0.5f)] public float glowAmount = 0.18f;

    SpriteRenderer sr;
    Vector3 baseScale;
    Color baseColor;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        baseColor = sr.color;
    }

    void Update()
    {
        float t = Mathf.Sin(Time.time * pulseSpeed);
        transform.localScale = baseScale * (1f + t * pulseAmount);
        Color dim = baseColor * (1f - glowAmount); dim.a = baseColor.a;
        sr.color = Color.Lerp(dim, baseColor, t * 0.5f + 0.5f);
    }
}
