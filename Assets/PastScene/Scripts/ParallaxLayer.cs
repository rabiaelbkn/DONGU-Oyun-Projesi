using UnityEngine;

/// <summary>Arka plan katmanını kameraya göre daha yavaş kaydırır (derinlik hissi). 0 = sabit, 1 = kamerayla birlikte.</summary>
public class ParallaxLayer : MonoBehaviour
{
    [Range(0f, 1f)] public float factor = 0.5f;
    Transform cam; Vector3 start; float camStartX;

    void Start()
    {
        if (Camera.main) { cam = Camera.main.transform; camStartX = cam.position.x; }
        start = transform.position;
    }

    void LateUpdate()
    {
        if (!cam) return;
        transform.position = new Vector3(start.x + (cam.position.x - camStartX) * factor, start.y, start.z);
    }
}
