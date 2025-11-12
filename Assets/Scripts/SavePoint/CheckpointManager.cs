// CheckpointManager.cs
// ติดสคริปต์นี้กับ Player (หรือจะถูกสร้างอัตโนมัติ)
// ทำหน้าที่เก็บตำแหน่ง Checkpoint และทำงานร่วมกับ RespawnTrigger
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    [Header("Current Checkpoint")]
    [Tooltip("จุด Checkpoint ปัจจุบัน")]
    public Vector3 checkpointPosition;
    public Quaternion checkpointRotation;

    [Header("Settings")]
    [Tooltip("มี Checkpoint ที่บันทึกไว้หรือไม่")]
    public bool hasCheckpoint = false;

    [Header("Auto Respawn")]
    [Tooltip("ความสูงที่ต่ำกว่านี้จะ Respawn อัตโนมัติ")]
    public float fallThreshold = -10f;

    [Tooltip("เปิดใช้ Auto Respawn เมื่อตกต่ำเกินไป")]
    public bool autoRespawnOnFall = true;

    [Header("Visual Feedback")]
    public GameObject checkpointIndicator; // ไอคอนแสดงว่ามี Checkpoint

    private CharacterController characterController;
    private PlayerController playerController;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        playerController = GetComponent<PlayerController>();

        // ตั้งค่า Checkpoint เริ่มต้นเป็นตำแหน่งปัจจุบัน
        if (!hasCheckpoint)
        {
            checkpointPosition = transform.position;
            checkpointRotation = transform.rotation;
        }

        // ซ่อนไอคอนถ้ายังไม่มี Checkpoint
        if (checkpointIndicator != null)
        {
            checkpointIndicator.SetActive(hasCheckpoint);
        }
    }

    void Update()
    {
        // ตรวจสอบว่าตกต่ำเกินไปหรือไม่
        if (autoRespawnOnFall && transform.position.y < fallThreshold)
        {
            Debug.Log("?? ผู้เล่นตกต่ำเกินไป! กำลัง Respawn...");
            Respawn();
        }
    }

    // บันทึก Checkpoint ใหม่
    public void SetCheckpoint(Vector3 position, Quaternion rotation)
    {
        checkpointPosition = position;
        checkpointRotation = rotation;
        hasCheckpoint = true;

        // แสดงไอคอน
        if (checkpointIndicator != null)
        {
            checkpointIndicator.SetActive(true);
        }

        Debug.Log($"?? บันทึก Checkpoint: {position}");
    }

    // Respawn ผู้เล่นไปยัง Checkpoint
    public void Respawn()
    {
        if (!hasCheckpoint)
        {
            Debug.LogWarning("?? ไม่มี Checkpoint ที่จะ Respawn ไป!");
            return;
        }

        Debug.Log($"?? กำลัง Respawn ไปยัง: {checkpointPosition}");

        // รีเซ็ต PlayerController ถ้ามี
        if (playerController != null)
        {
            playerController.HideSuccessSymbol();
            playerController.StopCastingAnimation();
        }

        // จัดการกับ CharacterController
        if (characterController != null)
        {
            // ปิด CharacterController ก่อนเปลี่ยนตำแหน่ง
            characterController.enabled = false;

            // วาปผู้เล่น
            transform.position = checkpointPosition;
            transform.rotation = checkpointRotation;

            // เปิด CharacterController กลับ
            characterController.enabled = true;
        }
        else
        {
            // ถ้าไม่มี CharacterController
            transform.position = checkpointPosition;
            transform.rotation = checkpointRotation;
        }

        // รีเซ็ต Rigidbody ถ้ามี
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log("? Respawn สำเร็จ!");
    }

    // ล้าง Checkpoint
    public void ClearCheckpoint()
    {
        hasCheckpoint = false;

        if (checkpointIndicator != null)
        {
            checkpointIndicator.SetActive(false);
        }

        Debug.Log("??? ล้าง Checkpoint แล้ว");
    }

    // ฟังก์ชันสำหรับเรียกจาก RespawnTrigger
    public Vector3 GetCheckpointPosition()
    {
        return checkpointPosition;
    }

    public Quaternion GetCheckpointRotation()
    {
        return checkpointRotation;
    }
}