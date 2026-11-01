using UnityEngine;

/// <summary>Sprite karelerini sırayla döndürür (nehir dalgası, akan atık vb.). Tiled çizim boyutunu korur.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFrameLoop : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 6f;
    SpriteRenderer sr;
    float t;

    void Awake() { sr = GetComponent<SpriteRenderer>(); t = Random.value; }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;
        t += Time.deltaTime * fps;
        var s = frames[(int)t % frames.Length];
        if (sr.sprite != s) sr.sprite = s;
    }
}
