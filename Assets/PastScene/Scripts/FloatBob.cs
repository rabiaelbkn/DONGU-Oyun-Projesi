using UnityEngine;

/// <summary>Suda yüzen nesneler için hafif aşağı-yukarı salınım ve sallanma.</summary>
public class FloatBob : MonoBehaviour
{
    public float amplitude = 0.05f;
    public float speed = 1.5f;
    public float tiltDegrees = 6f;
    public float driftSpeed = 0f;

    Vector3 start; float phase;

    void Start() { start = transform.localPosition; phase = Random.value * 10f; }

    void Update()
    {
        float t = Time.time * speed + phase;
        start.x += driftSpeed * Time.deltaTime;
        transform.localPosition = start + new Vector3(0f, Mathf.Sin(t) * amplitude, 0f);
        transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 0.7f) * tiltDegrees);
    }
}
