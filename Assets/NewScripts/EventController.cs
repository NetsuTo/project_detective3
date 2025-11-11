using UnityEngine;

public class EventController : MonoBehaviour
{
    [Header("Objects")]
    public GameObject objectA;           // วัตถุที่จะเคลื่อนที่ (ต้องมี Rigidbody)
    public Transform targetPoint;        // จุดเป้าหมาย
    public GameObject objectB;           // วัตถุที่จะหายไปเมื่อถึงจุด

    [Header("Settings")]
    public float moveSpeed = 3f;

    private bool isMoving = false;
    private Rigidbody rb;

    private void Start()
    {
        // ดึง Rigidbody จาก ObjectA
        if (objectA != null)
        {
            rb = objectA.GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogError("[EventController] ObjectA ต้องมี Rigidbody!");
            }
        }
    }

    public void StartObjectMovement()
    {
        if (objectA == null || targetPoint == null)
        {
            Debug.LogWarning("[EventController] โปรดตั้งค่า ObjectA และ TargetPoint ให้ครบ");
            return;
        }

        if (rb == null)
        {
            Debug.LogError("[EventController] ไม่พบ Rigidbody บน ObjectA!");
            return;
        }

        isMoving = true;
        Debug.Log("[EventController] เริ่มเคลื่อนที่ ObjectA...");
    }

    private void FixedUpdate() // เปลี่ยนจาก Update เป็น FixedUpdate
    {
        if (!isMoving || objectA == null || targetPoint == null || rb == null)
            return;

        // คำนวณตำแหน่งใหม่
        Vector3 newPosition = Vector3.MoveTowards(
            rb.position,
            targetPoint.position,
            moveSpeed * Time.fixedDeltaTime
        );

        // ใช้ Rigidbody.MovePosition แทนการเปลี่ยน transform โดยตรง
        rb.MovePosition(newPosition);

        // เช็คว่าถึงเป้าหมายหรือยัง
        if (Vector3.Distance(rb.position, targetPoint.position) < 0.05f)
        {
            isMoving = false;
            OnObjectAReachTarget();
        }
    }

    private void OnObjectAReachTarget()
    {
        Debug.Log("[EventController] ObjectA ถึงจุดเป้าหมายแล้ว!");

        if (objectB != null)
        {
            objectB.SetActive(false);
            Debug.Log("[EventController] ObjectB ถูกปิดการแสดงผล");
        }
    }
}