using UnityEngine;

public class WalkThroughOnewayTrigger : MonoBehaviour
{
    [Header("Threshold Settings")]
    [SerializeField] private Transform checkpointTransform; // จุดที่ต้องเดินผ่านถึงจะ Block
    [SerializeField] private float checkpointOffset = 2f; // ระยะห่างจากจุดเริ่มต้น (ถ้าไม่ใส่ Transform)
    [SerializeField] private bool useWorldPosition = false; // ใช้ตำแหน่งโลกแทนการคำนวณ

    [Header("Collider Settings")]
    [SerializeField] private Collider triggerZone; // โซนที่จะเช็คว่าผู้เล่นอยู่ข้างใน
    [SerializeField] private Collider blockCollider; // Collider สำหรับบัง (ไม่ใช่ Trigger!)

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = Color.yellow;

    private bool hasBlocked = false;
    private Vector3 checkpointPosition;
    private PlayerController trackedPlayer;

    private void Start()
    {
        // ถ้าไม่ได้กำหนด Trigger Zone ให้ใช้ของ GameObject นี้
        if (triggerZone == null)
        {
            triggerZone = GetComponent<Collider>();
        }

        // ตรวจสอบว่าเป็น Trigger
        if (triggerZone != null)
        {
            triggerZone.isTrigger = true;
        }

        // คำนวณตำแหน่ง Checkpoint
        if (checkpointTransform != null)
        {
            checkpointPosition = checkpointTransform.position;
        }
        else if (useWorldPosition)
        {
            checkpointPosition = transform.position;
        }
        else
        {
            // ใช้ offset จากตำแหน่งปัจจุบัน (ไปทางขวา)
            checkpointPosition = transform.position + new Vector3(checkpointOffset, 0f, 0f);
        }

        // ปิด Block Collider ตอนเริ่มต้น
        if (blockCollider != null)
        {
            blockCollider.enabled = false;
            Debug.Log("?? Block Collider ปิดอยู่ (ยังเดินผ่านได้)");
        }
        else
        {
            Debug.LogWarning("?? ไม่ได้ใส่ Block Collider! ผู้เล่นจะเดินกลับมาได้");
        }
    }

    private void Update()
    {
        // ถ้ายัง Block ไม่โผล่ และมีผู้เล่นอยู่ในโซน
        if (!hasBlocked && trackedPlayer != null)
        {
            // เช็คว่าผู้เล่นเดินผ่าน Checkpoint แล้วหรือยัง
            float playerX = trackedPlayer.transform.position.x;
            float checkpointX = checkpointPosition.x;

            // ถ้าผู้เล่นเดินผ่านเส้น Checkpoint (ไปทางขวา)
            if (playerX >= checkpointX)
            {
                ActivateBlock();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // เมื่อผู้เล่นเข้ามาในโซน
        if (!hasBlocked && other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();

            if (player != null)
            {
                trackedPlayer = player;
                Debug.Log("?? ผู้เล่นเข้าโซน - เริ่มติดตามตำแหน่ง");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // ถ้าผู้เล่นออกจากโซนก่อนถึง Checkpoint
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();

            if (player == trackedPlayer && !hasBlocked)
            {
                trackedPlayer = null;
                Debug.Log("?? ผู้เล่นออกจากโซนก่อนถึง Checkpoint");
            }
        }
    }

    private void ActivateBlock()
    {
        hasBlocked = true;

        // **เปิด Block Collider เพื่อบังไม่ให้กลับมา**
        if (blockCollider != null)
        {
            blockCollider.enabled = true;
            Debug.Log("?? เปิด Block Collider - ไม่สามารถเดินกลับได้แล้ว!");
        }

        // ปิด Trigger Zone (ไม่ต้องเช็คอีกต่อไป)
        if (triggerZone != null)
        {
            triggerZone.enabled = false;
            Debug.Log("?? ปิด Trigger Zone แล้ว");
        }

        trackedPlayer = null;
    }

    // ฟังก์ชันสำหรับ Reset (ใช้สำหรับ Debug หรือ Checkpoint)
    public void ResetTrigger()
    {
        hasBlocked = false;
        trackedPlayer = null;

        if (triggerZone != null)
        {
            triggerZone.enabled = true;
        }

        if (blockCollider != null)
        {
            blockCollider.enabled = false;
        }

        Debug.Log("?? Reset Trigger แล้ว");
    }

    // Gizmos สำหรับแสดงตำแหน่ง Checkpoint ใน Scene View
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Vector3 checkPoint;

        if (checkpointTransform != null)
        {
            checkPoint = checkpointTransform.position;
        }
        else if (useWorldPosition)
        {
            checkPoint = transform.position;
        }
        else
        {
            checkPoint = transform.position + new Vector3(checkpointOffset, 0f, 0f);
        }

        // วาดเส้น Checkpoint
        Gizmos.color = gizmoColor;
        Vector3 top = checkPoint + new Vector3(0f, 3f, 0f);
        Vector3 bottom = checkPoint - new Vector3(0f, 3f, 0f);
        Gizmos.DrawLine(top, bottom);

        // วาดลูกศร
        Gizmos.DrawLine(top, top + new Vector3(-0.3f, -0.3f, 0f));
        Gizmos.DrawLine(top, top + new Vector3(0.3f, -0.3f, 0f));

        // วาดข้อความ (ถ้าใช้ใน Editor)
#if UNITY_EDITOR
        UnityEditor.Handles.Label(top + Vector3.up * 0.5f, "Checkpoint", new GUIStyle()
        {
            normal = new GUIStyleState() { textColor = gizmoColor },
            fontSize = 12,
            fontStyle = FontStyle.Bold
        });
#endif
    }
}