using UnityEngine;

/// <summary>
/// Trigger Zone สำหรับปลดล็อคหน้าหนังสือ (รองรับหลายหน้าพร้อมกัน)
/// เมื่อชนจะทำให้ไอคอนหนังสือ UI สั่น จนกว่าจะเปิดหนังสือดู
/// </summary>
[RequireComponent(typeof(Collider))]
public class PageUnlockTrigger : MonoBehaviour
{
    [Header("?? การตั้งค่าปลดล็อค")]
    [Tooltip("หน้าที่จะปลดล็อคเมื่อผู้เล่นเข้ามาในพื้นที่นี้ (สามารถใส่หลายหน้าได้)")]
    [SerializeField]
    private int[] pageNumbersToUnlock = new int[] { 1 }; // ? เปลี่ยนเป็น Array

    [Tooltip("ปลดล็อคครั้งเดียวแล้วทำลาย Trigger?")]
    [SerializeField]
    private bool destroyAfterUnlock = true;

    [Tooltip("ระ?ะเวลารอก่อนทำลาย (วินาที)")]
    [SerializeField]
    private float destroyDelay = 0.5f;

    [Header("?? Debug")]
    [SerializeField]
    private bool showDebugLogs = true;

    [SerializeField]
    private Color gizmoColor = new Color(0f, 1f, 0.5f, 0.3f);

    private TutorialBook tutorialBook;
    private bool hasTriggered = false;

    void Start()
    {
        // ค้นหา TutorialBook ในฉาก
        tutorialBook = FindObjectOfType<TutorialBook>();

        if (tutorialBook == null)
        {
            Debug.LogError("? PageUnlockTrigger: ไม่พบ TutorialBook ในฉาก!");
            enabled = false;
            return;
        }

        // ตรวจสอบว่า Collider เป็น Trigger หรือไม่
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning("?? PageUnlockTrigger: Collider ไม่ได้เซ็ตเป็น 'Is Trigger' - กำลังเซ็ตให้อัตโนมัติ");
            col.isTrigger = true;
        }

        if (showDebugLogs)
        {
            string pageList = string.Join(", ", pageNumbersToUnlock);
            Debug.Log($"? PageUnlockTrigger พร้อม: จะปลดล็อคหน้า [{pageList}]");
        }
    }

    void Update()
    {
        // เช็คว่าผู้เล่นเปิดหนังสือหรือยัง ?? ถ้าเปิดแล้วให้หยุดสั่นและทำลาย Trigger
        if (hasTriggered && tutorialBook != null && tutorialBook.IsBookOpen())
        {
            StopShakingAndDestroy();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // ตรวจสอบว่าเป็น Player หรือไม่
        if (!other.CompareTag("Player") || hasTriggered)
        {
            return;
        }

        // ปลดล็อคหน้า
        UnlockPages();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // รองรับ 2D Game
        if (!other.CompareTag("Player") || hasTriggered)
        {
            return;
        }

        UnlockPages();
    }

    private void UnlockPages() // ? เปลี่ยนชื่อฟังก์ชัน
    {
        if (tutorialBook == null || hasTriggered)
        {
            return;
        }

        hasTriggered = true;

        // ปลดล็อคทุกหน้าที่กำหนดไว้
        foreach (int pageNumber in pageNumbersToUnlock)
        {
            if (pageNumber > 0)
            {
                tutorialBook.UnlockPageWithShake(pageNumber);

                if (showDebugLogs)
                {
                    Debug.Log($"?? ปลดล็อคหน้า {pageNumber}");
                }
            }
        }

        if (showDebugLogs)
        {
            string pageList = string.Join(", ", pageNumbersToUnlock);
            Debug.Log($"?? Trigger ถูกเรียกใช้: ปลดล็อคหน้า [{pageList}] + เริ่มสั่นไอคอน UI");
        }
    }

    private void StopShakingAndDestroy()
    {
        if (tutorialBook == null) return;

        // สั่งให้หยุดสั่นไอคอน
        tutorialBook.StopIconShake();

        if (showDebugLogs)
        {
            Debug.Log($"?? ผู้เล่นเปิดหนังสือแล้ว - หยุดสั่นไอคอน");
        }

        // ทำลาย Trigger ถ้าตั้งค่าไว้
        if (destroyAfterUnlock)
        {
            Destroy(gameObject, destroyDelay);

            if (showDebugLogs)
            {
                Debug.Log($"?? Trigger จะถูกทำลายใน {destroyDelay} วินาที");
            }
        }

        // ปิดการทำงานของ Script นี้
        enabled = false;
    }

    // แสดง Gizmo ใน Scene View
    void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = gizmoColor;

            if (col is BoxCollider)
            {
                BoxCollider boxCol = (BoxCollider)col;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(boxCol.center, boxCol.size);
            }
            else if (col is SphereCollider)
            {
                SphereCollider sphereCol = (SphereCollider)col;
                Gizmos.DrawSphere(transform.position + sphereCol.center, sphereCol.radius);
            }
        }

        // แสดงข้อความ
#if UNITY_EDITOR
        string pageList = string.Join(", ", pageNumbersToUnlock);
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.5f,
            $"?? Unlock Pages [{pageList}]",
            new GUIStyle()
            {
                normal = new GUIStyleState() { textColor = Color.green },
                fontSize = 12,
                fontStyle = FontStyle.Bold
            }
        );
#endif
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Collider col = GetComponent<Collider>();

        if (col is BoxCollider)
        {
            BoxCollider boxCol = (BoxCollider)col;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(boxCol.center, boxCol.size);
        }
    }
}