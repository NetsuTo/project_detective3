// RespawnTrigger.cs
// ติดสคริปต์นี้กับแผ่นที่ต้องการให้เป็นจุด Respawn
using UnityEngine;

public class RespawnTrigger : MonoBehaviour
{
    [Header("Respawn Settings")]
    [Tooltip("จุดที่ผู้เล่นจะวาปกลับไป")]
    public Transform respawnPoint;

    [Tooltip("เปิดใช้เอฟเฟกต์เมื่อ Respawn")]
    public bool useEffect = true;

    [Header("Optional Effects")]
    public ParticleSystem respawnEffect;
    public AudioClip respawnSound;

    [Header("Player Reset Options")]
    [Tooltip("รีเซ็ตความเร็วเมื่อ Respawn")]
    public bool resetVelocity = true;

    [Tooltip("หน่วงเวลาก่อน Respawn (วินาที)")]
    public float respawnDelay = 0f;

    private AudioSource audioSource;

    void Start()
    {
        // ตรวจสอบว่ามี Collider และตั้งเป็น Trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        else
        {
            Debug.LogError("? RespawnTrigger ต้องมี Collider component!");
        }

        // ตั้งค่า AudioSource ถ้ามีเสียง
        if (respawnSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = respawnSound;
            audioSource.playOnAwake = false;
        }

        // เช็คว่ามี Respawn Point หรือไม่
        if (respawnPoint == null)
        {
            Debug.LogWarning("?? ไม่ได้กำหนด Respawn Point! กรุณาลาก GameObject มาใส่");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // ตรวจสอบว่าเป็นผู้เล่นหรือไม่
        if (other.CompareTag("Player"))
        {
            Debug.Log("?? ผู้เล่นเหยียบแผ่น Respawn!");

            if (respawnDelay > 0f)
            {
                Invoke(nameof(DelayedRespawn), respawnDelay);
                lastPlayerToRespawn = other.gameObject;
            }
            else
            {
                RespawnPlayer(other.gameObject);
            }
        }
    }

    private GameObject lastPlayerToRespawn;

    private void DelayedRespawn()
    {
        if (lastPlayerToRespawn != null)
        {
            RespawnPlayer(lastPlayerToRespawn);
            lastPlayerToRespawn = null;
        }
    }

    void RespawnPlayer(GameObject player)
    {
        // ลองใช้ CheckpointManager ก่อน (จากกำแพง)
        CheckpointManager checkpointManager = player.GetComponent<CheckpointManager>();

        Vector3 targetPosition;
        Quaternion targetRotation;

        if (checkpointManager != null && checkpointManager.hasCheckpoint)
        {
            // ใช้ Checkpoint จากกำแพง
            targetPosition = checkpointManager.GetCheckpointPosition();
            targetRotation = checkpointManager.GetCheckpointRotation();
            Debug.Log("?? ใช้ Checkpoint จากกำแพง: " + targetPosition);
        }
        else if (respawnPoint != null)
        {
            // ใช้ Respawn Point ปกติ
            targetPosition = respawnPoint.position;
            targetRotation = respawnPoint.rotation;
            Debug.Log("?? ใช้ Respawn Point ปกติ: " + targetPosition);
        }
        else
        {
            Debug.LogError("? ไม่สามารถ Respawn ได้ ไม่มีทั้ง Checkpoint และ Respawn Point!");
            return;
        }

        Debug.Log("?? กำลัง Respawn ผู้เล่นจาก " + player.transform.position + " ไปยัง " + targetPosition);

        // รีเซ็ต PlayerController ถ้ามี
        PlayerController playerController = player.GetComponent<PlayerController>();

        // รีเซ็ต Rigidbody (ถ้ามี)
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null && resetVelocity)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // จัดการกับ CharacterController (สำคัญมาก!)
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            // ต้องปิด CharacterController ก่อนเปลี่ยนตำแหน่ง
            cc.enabled = false;

            // วาปผู้เล่น
            player.transform.position = respawnPoint.position;
            player.transform.rotation = respawnPoint.rotation;

            // เปิด CharacterController กลับ
            cc.enabled = true;

            Debug.Log("? Respawn สำเร็จ (CharacterController)");
        }
        else
        {
            // ถ้าไม่มี CharacterController ก็เปลี่ยนตำแหน่งตรงๆ
            player.transform.position = respawnPoint.position;
            player.transform.rotation = respawnPoint.rotation;

            Debug.Log("? Respawn สำเร็จ (Transform)");
        }

        // รีเซ็ตค่าใน PlayerController ถ้ามี
        if (playerController != null)
        {
            // ซ่อน Success Symbol ถ้ามี
            playerController.HideSuccessSymbol();

            // หยุด Animation Casting ถ้ากำลังทำอยู่
            playerController.StopCastingAnimation();

            Debug.Log("? รีเซ็ต PlayerController สำเร็จ");
        }

        // เล่นเอฟเฟกต์และเสียง
        if (useEffect)
        {
            if (respawnEffect != null)
            {
                respawnEffect.transform.position = respawnPoint.position;
                respawnEffect.Play();
            }

            if (audioSource != null && respawnSound != null)
            {
                audioSource.Play();
            }
        }

        Debug.Log("?? ผู้เล่น Respawn เรียบร้อยที่ตำแหน่ง: " + player.transform.position);
    }

    // แสดง Gizmo ใน Scene View เพื่อดูตำแหน่ง Respawn Point
    void OnDrawGizmos()
    {
        if (respawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(respawnPoint.position, 0.5f);
            Gizmos.DrawLine(transform.position, respawnPoint.position);
        }
    }
}