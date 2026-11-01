using System.Collections.Generic;
using UnityEngine;

/// <summary>Fabrika bacası dumanı: yükselen, büyüyen ve solan duman bulutları üretir.</summary>
public class SmokeEmitter : MonoBehaviour
{
    public Sprite[] puffs;
    public float interval = 0.45f;
    public float riseSpeed = 0.55f;
    public Vector2 wind = new Vector2(0.35f, 0f);
    public float life = 5f;
    public float startScale = 0.6f, endScale = 2.2f;
    public int sortingOrder = -42;
    public Material material;

    class Puff { public Transform t; public SpriteRenderer sr; public float age; public float spin; }
    readonly List<Puff> active = new List<Puff>();
    readonly Stack<Puff> pool = new Stack<Puff>();
    float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= interval && puffs != null && puffs.Length > 0) { timer = 0f; Spawn(); }

        for (int i = active.Count - 1; i >= 0; i--)
        {
            var p = active[i];
            p.age += Time.deltaTime;
            float k = p.age / life;
            if (k >= 1f) { p.t.gameObject.SetActive(false); pool.Push(p); active.RemoveAt(i); continue; }
            p.t.position += (Vector3)((Vector2.up * riseSpeed + wind * k) * Time.deltaTime);
            p.t.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, k);
            p.t.Rotate(0, 0, p.spin * Time.deltaTime);
            var c = p.sr.color; c.a = Mathf.Clamp01(1f - k) * 0.85f; p.sr.color = c;
        }
    }

    void Spawn()
    {
        Puff p = pool.Count > 0 ? pool.Pop() : Create();
        p.age = 0f;
        p.spin = Random.Range(-20f, 20f);
        p.sr.sprite = puffs[Random.Range(0, puffs.Length)];
        p.t.position = transform.position + new Vector3(Random.Range(-0.1f, 0.1f), 0f, 0f);
        p.t.gameObject.SetActive(true);
        active.Add(p);
    }

    Puff Create()
    {
        var go = new GameObject("Puff");
        go.transform.SetParent(transform, true);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = sortingOrder;
        if (material) sr.sharedMaterial = material;
        return new Puff { t = go.transform, sr = sr };
    }
}
