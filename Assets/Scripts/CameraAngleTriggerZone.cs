using UnityEngine;

public class CameraAngleTriggerZone : MonoBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("กล้องที่ใช้ FixedAngleFollowCamera script")]
    public FixedAngleFollowCamera cameraScript;

    [Header("New Camera Angle")]
    [Tooltip("มุมกล้องใหม่ที่ต้องการ (X, Y, Z)")]
    public Vector3 newFixedRotation = new Vector3(10f, 0f, 0f);

    [Header("Offset Settings (Optional)")]
    [Tooltip("เปลี่ยน offset ด้วยหรือไม่")]
    public bool changeOffset = false;
    public Vector3 newOffset = new Vector3(2.5f, 3.5f, -20f);

    [Header("Transition Settings")]
    [Tooltip("ความเร็วในการเปลี่ยนมุม (0 = ทันที)")]
    [Range(0f, 10f)]
    public float transitionSpeed = 3f;

    [Header("Trigger Settings")]
    [Tooltip("Tag ของ GameObject ที่จะทริกเกอร์")]
    public string triggerTag = "Player";

    [Tooltip("ย้อนกลับเมื่อออกจาก Trigger")]
    public bool revertOnExit = false;

    private Vector3 originalRotation;
    private Vector3 originalOffset;
    private bool hasEntered = false;

    void Start()
    {
        // ถ้าไม่ได้กำหนดกล้อง ค้นหาอัตโนมัติ
        if (cameraScript == null)
        {
            cameraScript = Camera.main?.GetComponent<FixedAngleFollowCamera>();
        }

        // เก็บค่าเดิมของกล้อง
        if (cameraScript != null)
        {
            originalRotation = cameraScript.fixedRotation;
            originalOffset = cameraScript.offset;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(triggerTag) && cameraScript != null && !hasEntered)
        {
            hasEntered = true;

            if (transitionSpeed > 0)
            {
                // เปลี่ยนแบบค่อยๆ
                StartCoroutine(SmoothTransition(newFixedRotation, changeOffset ? newOffset : cameraScript.offset));
            }
            else
            {
                // เปลี่ยนทันที
                cameraScript.fixedRotation = newFixedRotation;
                if (changeOffset)
                {
                    cameraScript.offset = newOffset;
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (revertOnExit && other.CompareTag(triggerTag) && cameraScript != null && hasEntered)
        {
            hasEntered = false;

            if (transitionSpeed > 0)
            {
                StartCoroutine(SmoothTransition(originalRotation, originalOffset));
            }
            else
            {
                cameraScript.fixedRotation = originalRotation;
                cameraScript.offset = originalOffset;
            }
        }
    }

    System.Collections.IEnumerator SmoothTransition(Vector3 targetRotation, Vector3 targetOffset)
    {
        Vector3 startRotation = cameraScript.fixedRotation;
        Vector3 startOffset = cameraScript.offset;
        float elapsed = 0f;
        float duration = 1f / transitionSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            cameraScript.fixedRotation = Vector3.Lerp(startRotation, targetRotation, t);

            if (changeOffset)
            {
                cameraScript.offset = Vector3.Lerp(startOffset, targetOffset, t);
            }

            yield return null;
        }

        cameraScript.fixedRotation = targetRotation;
        if (changeOffset)
        {
            cameraScript.offset = targetOffset;
        }
    }

    // แสดง Gizmo เพื่อดูพื้นที่ Trigger
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0.5f, 0, 0.3f);

        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
        else
        {
            Gizmos.DrawCube(transform.position, transform.localScale);
        }

        // แสดงข้อความมุมกล้อง
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up,
            $"Camera Angle: {newFixedRotation}");
#endif
    }
}