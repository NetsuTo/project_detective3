// CheckpointWall.cs (เวอร์ชันสั่งให้ RespawnTrigger โผล่)
// เดินผ่านกำแพง ? RespawnTrigger โผล่ขึ้นมาที่ตำแหน่งที่กำหนด
using UnityEngine;

public class CheckpointWall : MonoBehaviour
{
    [Header("Respawn Trigger Settings")]
    [Tooltip("แผ่น RespawnTrigger ที่จะเปิดใช้งาน")]
    public RespawnTrigger respawnTrigger;

    [Tooltip("ตำแหน่งที่แผ่นจะโผล่ขึ้นมา")]
    public Transform spawnPosition;

    [Header("Behavior")]
    [Tooltip("ย้ายแผ่นไปตำแหน่งใหม่ หรือแค่เปิด-ปิด")]
    public TriggerBehavior behavior = TriggerBehavior.MoveAndActivate;

    [Tooltip("ใช้ได้แค่ครั้งเดียว")]
    public bool oneTimeUse = false;

    [Tooltip("แสดง Debug Gizmos")]
    public bool showGizmos = true;

    private bool hasBeenUsed = false;

    public enum TriggerBehavior
    {
        MoveAndActivate,  // ย้ายแผ่นไปตำแหน่งใหม่และเปิด
        ActivateOnly      // เปิดแผ่น (ไม่ย้าย)
    }

    void Start()
    {
        // ตรวจสอบ Collider
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        else
        {
            Debug.LogError("? CheckpointWall ต้องมี Collider component!");
        }

        // เช็คว่ามี RespawnTrigger หรือไม่
        if (respawnTrigger == null)
        {
            Debug.LogWarning("?? กรุณากำหนด RespawnTrigger ให้กับกำแพงนี้!");
        }

        // เช็คว่ามี Spawn Position หรือไม่
        if (behavior == TriggerBehavior.MoveAndActivate && spawnPosition == null)
        {
            Debug.LogWarning("?? กรุณากำหนด Spawn Position สำหรับกำแพงนี้!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // ถ้าใช้ไปแล้วและเป็นแบบใช้ครั้งเดียว ก็ไม่ทำอะไร
        if (oneTimeUse && hasBeenUsed)
        {
            Debug.Log("?? กำแพงนี้ใช้ไปแล้ว");
            return;
        }

        // ตรวจสอบว่าเป็นผู้เล่นหรือไม่
        if (other.CompareTag("Player"))
        {
            if (respawnTrigger == null)
            {
                Debug.LogError("? ไม่มี RespawnTrigger ที่จะเปิดใช้งาน!");
                return;
            }

            // ทำงานตาม Behavior ที่เลือก
            if (behavior == TriggerBehavior.MoveAndActivate)
            {
                // ย้ายแผ่นไปตำแหน่งใหม่
                if (spawnPosition != null)
                {
                    respawnTrigger.transform.position = spawnPosition.position;
                    respawnTrigger.transform.rotation = spawnPosition.rotation;
                    Debug.Log($"?? ย้าย RespawnTrigger ไปที่: {spawnPosition.position}");
                }
            }

            // เปิดใช้งานแผ่น
            respawnTrigger.gameObject.SetActive(true);
            Debug.Log($"? [{gameObject.name}] เปิด RespawnTrigger: {respawnTrigger.name}");

            // ทำเครื่องหมายว่าใช้ไปแล้ว
            if (oneTimeUse)
            {
                hasBeenUsed = true;
            }
        }
    }

    // แสดง Gizmos ใน Scene View
    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // วาดกำแพง
        Gizmos.color = hasBeenUsed ? Color.gray : Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

        // วาด Spawn Position
        if (behavior == TriggerBehavior.MoveAndActivate && spawnPosition != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(spawnPosition.position, 0.7f);
            Gizmos.DrawLine(transform.position, spawnPosition.position);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(spawnPosition.position + Vector3.up, "TRIGGER SPAWN");
#endif
        }

        // วาดเส้นไปหา RespawnTrigger
        if (respawnTrigger != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, respawnTrigger.transform.position);
        }
    }

    // ฟังก์ชันสำหรับรีเซ็ต
    public void ResetWall()
    {
        hasBeenUsed = false;

        // ปิด RespawnTrigger
        if (respawnTrigger != null)
        {
            respawnTrigger.gameObject.SetActive(false);
        }

        Debug.Log("?? รีเซ็ต CheckpointWall: " + gameObject.name);
    }
}