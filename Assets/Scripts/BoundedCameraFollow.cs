using UnityEngine;
using UnityEngine.InputSystem;

public class BoundedCameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 6f;
    public float leftBound = -8f;
    public float rightBound = 8f;
    public bool followY;

    private Camera cameraComponent;

    private void Awake()
    {
        cameraComponent = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (target == null || cameraComponent == null) return;

        float halfWidth = cameraComponent.orthographicSize * cameraComponent.aspect;
        float minX = leftBound + halfWidth;
        float maxX = rightBound - halfWidth;
        float x = minX <= maxX ? Mathf.Clamp(target.position.x, minX, maxX) : (leftBound + rightBound) * 0.5f;
        float y = followY ? target.position.y : transform.position.y;
        Vector3 destination = new Vector3(x, y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, destination, smoothSpeed * Time.deltaTime);
    }
}
