using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;          // Player ที่จะตาม
    public Vector3 offset = new Vector3(0, 5, -10); // ระยะห่างจาก Player
    public float smoothSpeed = 5f;    // ความเร็วในการตาม

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;

        transform.LookAt(target); // มองไปที่ Player
    }
}
