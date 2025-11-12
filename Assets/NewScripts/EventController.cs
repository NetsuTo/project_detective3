using UnityEngine;

public class EventController : MonoBehaviour
{
    [Header("Objects")]
    public GameObject objectA;           // วัตถุที่จะเคลื่อนที่ (ต้องมี Rigidbody)
    public Transform targetPoint;        // จุดเป้าหมาย
    public GameObject objectB;           // วัตถุที่จะหายไปเมื่อถึงจุด

    [Header("Settings")]
    public float moveSpeed = 3f;

    [Header("Effects & Sounds")]
    public GameObject collisionEffect;   // Prefab เอฟเฟคตอนชน (เช่น ParticeSystem)
    public AudioClip collisionSound;     // เสียงตอนชน
    [Range(0f, 1f)]
    public float soundVolume = 0.8f;     // ความดังเสียง
    public bool shakeOnCollision = true; // เขย่ากล้องตอนชน
    public float shakeIntensity = 0.3f;  // ความแรงการเขย่า
    public float shakeDuration = 0.3f;   // ระยะเวลาเขย่า

    private bool isMoving = false;
    private Rigidbody rb;
    private AudioSource audioSource;

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

        // สร้าง AudioSource สำหรับเล่นเสียง
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound
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

    private void FixedUpdate()
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

        // เล่นเอฟเฟค
        PlayCollisionEffect();

        // เล่นเสียง
        PlayCollisionSound();

        // เขย่ากล้อง
        if (shakeOnCollision && Camera.main != null)
        {
            StartCoroutine(ShakeCamera());
        }

        // ซ่อน ObjectB
        if (objectB != null)
        {
            objectB.SetActive(false);
            Debug.Log("[EventController] ObjectB ถูกปิดการแสดงผล");
        }
    }

    private void PlayCollisionEffect()
    {
        if (collisionEffect != null && targetPoint != null)
        {
            // สร้างเอฟเฟคที่ตำแหน่งชน
            GameObject fx = Instantiate(collisionEffect, targetPoint.position, Quaternion.identity);

            // ลบเอฟเฟคอัตโนมัติหลัง 3 วินาที
            Destroy(fx, 3f);

            Debug.Log("[EventController] เล่นเอฟเฟคชน!");
        }
    }

    private void PlayCollisionSound()
    {
        if (collisionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(collisionSound, soundVolume);
            Debug.Log("[EventController] เล่นเสียงชน!");
        }
    }

    private System.Collections.IEnumerator ShakeCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        Vector3 originalPos = cam.transform.localPosition;
        float elapsed = 0f;

        Debug.Log("[EventController] เขย่ากล้อง!");

        while (elapsed < shakeDuration)
        {
            // สุ่มตำแหน่งเขย่า
            float x = Random.Range(-1f, 1f) * shakeIntensity;
            float y = Random.Range(-1f, 1f) * shakeIntensity;

            cam.transform.localPosition = originalPos + new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // คืนตำแหน่งเดิม
        cam.transform.localPosition = originalPos;
    }
}