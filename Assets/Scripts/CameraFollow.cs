using UnityEngine;

public class FixedAngleFollowCamera : MonoBehaviour
{
    public Transform target;                // ตัวละคร
    public Vector3 offset = new Vector3(2.5f, 3.5f, -20f); // ระยะห่างกล้อง
    public float smoothSpeed = 5f;

    [Header("Fixed Camera Rotation")]
    public Vector3 fixedRotation = new Vector3(10f, -13f, 0f); // มุมกล้องที่ต้องการคงไว้

    void LateUpdate()
    {
        if (target == null) return;

        // คำนวณตำแหน่งกล้องตามตำแหน่งตัวละคร
        Vector3 desiredPosition = target.position + Quaternion.Euler(fixedRotation) * offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // ตั้งมุมกล้องตายตัว ไม่เปลี่ยนตาม Player
        transform.rotation = Quaternion.Euler(fixedRotation);
    }
}
