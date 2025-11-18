using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trigger ที่ทำให้หินก้อนใหญ่ตกลงมาสุ่มจากหลายจุด
/// เมื่อ Player เหยียบ Trigger ? หินตกจากจุดใดจุดหนึ่งที่กำหนดไว้
/// </summary>
public class FallingRockTrigger : MonoBehaviour
{
    [Header("?? หินก้อนใหญ่")]
    [Tooltip("Prefab ของหินก้อนใหญ่")]
    public GameObject bigRockPrefab;

    [Tooltip("จุดที่หินสามารถตกได้ (จะสุ่มเลือก 1 จุด)")]
    public Transform[] dropPoints;

    [Header("?? การตกของหิน")]
    [Tooltip("ดีเลย์หลังเหยียบ Trigger กี่วินาทีหินถึงตก")]
    public float dropDelay = 1f;

    [Tooltip("ความสูงที่หินจะ Spawn (เหนือ Drop Point)")]
    public float spawnHeight = 20f;

    [Tooltip("สุ่มตำแหน่งรอบๆ Drop Point เล็กน้อย (รัศมี)")]
    public float randomOffset = 0.5f;

    [Header("?? การสุ่ม")]
    [Tooltip("จำนวนหินที่จะตก (สุ่มจาก Drop Points)")]
    public int numberOfRocks = 1;

    [Tooltip("ระยะห่างระหว่างการตกของแต่ละก้อน (วินาที)")]
    public float rockDropInterval = 0.5f;

    [Header("?? พฤติกรรม")]
    [Tooltip("ระยะเวลาก่อนหินหายไป (วินาที)")]
    public float rockLifetime = 10f;

    [Tooltip("ทริกเกอร์ได้แค่ครั้งเดียว")]
    public bool triggerOnce = true;

    [Header("?? เสียง")]
    [Tooltip("เสียงเตือนตอนเหยียบ Trigger")]
    public AudioClip warningSound;

    [Tooltip("เสียงหินตก")]
    public AudioClip rockFallSound;

    [Range(0f, 1f)]
    public float soundVolume = 0.7f;

    [Header("?? Tag ของ Player")]
    [Tooltip("Tag ที่จะทริกเกอร์ (เช่น 'Player')")]
    public string playerTag = "Player";

    private bool hasTriggered = false;
    private AudioSource audioSource;

    void Start()
    {
        // สร้าง AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        // ตรวจสอบ Collider
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        else
        {
            Debug.LogWarning("?? [FallingRockTrigger] ไม่มี Collider! กรุณาเพิ่ม Collider และเปิด 'Is Trigger'");
        }

        // ตรวจสอบว่ามี Drop Points หรือไม่
        if (dropPoints == null || dropPoints.Length == 0)
        {
            Debug.LogError("? [FallingRockTrigger] ไม่ได้ใส่ Drop Points! หินจะไม่รู้ว่าจะตกที่ไหน!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // เช็คว่าเป็น Player หรือไม่
        if (other.CompareTag(playerTag))
        {
            TriggerRockFall();
        }
    }

    /// <summary>
    /// เริ่มให้หินตก (เรียกจาก Trigger หรือ Script อื่นก็ได้)
    /// </summary>
    public void TriggerRockFall()
    {
        if (triggerOnce && hasTriggered)
        {
            Debug.Log("?? Trigger นี้ถูกใช้ไปแล้ว!");
            return;
        }

        if (dropPoints == null || dropPoints.Length == 0)
        {
            Debug.LogError("? ไม่มี Drop Points! ไม่สามารถตกหินได้!");
            return;
        }

        hasTriggered = true;

        Debug.Log($"?? Player เหยียบ Trigger! หิน {numberOfRocks} ก้อนจะตกในอีก {dropDelay} วินาที!");

        // เล่นเสียงเตือน
        if (warningSound != null)
        {
            PlaySound(warningSound);
        }

        // เริ่ม Coroutine ตกหิน
        StartCoroutine(DropRockSequence());
    }

    private IEnumerator DropRockSequence()
    {
        // รอตาม delay
        yield return new WaitForSeconds(dropDelay);

        // ตกหินตามจำนวนที่กำหนด
        for (int i = 0; i < numberOfRocks; i++)
        {
            // สุ่มเลือก Drop Point
            Transform selectedDropPoint = GetRandomDropPoint();

            if (selectedDropPoint != null)
            {
                SpawnBigRock(selectedDropPoint);
            }

            // รอก่อนตกก้อนถัดไป (ถ้ามีหลายก้อน)
            if (i < numberOfRocks - 1)
            {
                yield return new WaitForSeconds(rockDropInterval);
            }
        }
    }

    /// <summary>
    /// สุ่มเลือก Drop Point จากรายการ
    /// </summary>
    private Transform GetRandomDropPoint()
    {
        if (dropPoints == null || dropPoints.Length == 0)
            return null;

        // กรองเอาเฉพาะ Transform ที่ไม่เป็น null
        List<Transform> validPoints = new List<Transform>();
        foreach (Transform point in dropPoints)
        {
            if (point != null)
            {
                validPoints.Add(point);
            }
        }

        if (validPoints.Count == 0)
        {
            Debug.LogWarning("?? ไม่มี Drop Point ที่ใช้งานได้!");
            return null;
        }

        // สุ่มเลือกจุดหนึ่ง
        int randomIndex = Random.Range(0, validPoints.Count);
        return validPoints[randomIndex];
    }

    private void SpawnBigRock(Transform dropPoint)
    {
        if (bigRockPrefab == null)
        {
            Debug.LogWarning("?? ไม่ได้ใส่ Big Rock Prefab!");
            return;
        }

        // ตำแหน่งที่หินจะตก (มีการสุ่มเล็กน้อย)
        Vector3 targetPos = dropPoint.position;

        if (randomOffset > 0)
        {
            Vector2 randomCircle = Random.insideUnitCircle * randomOffset;
            targetPos += new Vector3(randomCircle.x, 0, randomCircle.y);
        }

        // คำนวณตำแหน่ง Spawn (สูงขึ้นไป)
        Vector3 spawnPos = targetPos + Vector3.up * spawnHeight;

        // Spawn หินก้อนใหญ่
        GameObject rock = Instantiate(bigRockPrefab, spawnPos, Random.rotation);

        // ตรวจสอบว่ามี Rigidbody หรือยัง
        Rigidbody rb = rock.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = rock.AddComponent<Rigidbody>();
        }

        // ตั้งค่า Rigidbody
        rb.mass = Random.Range(5f, 15f); // มวลหนัก
        rb.angularVelocity = Random.insideUnitSphere * 2f;

        // แก้ Mesh Collider (ถ้ามี)
        MeshCollider meshCol = rock.GetComponent<MeshCollider>();
        if (meshCol != null)
        {
            meshCol.convex = true;
        }

        // เล่นเสียงหินตก
        if (rockFallSound != null)
        {
            PlaySound(rockFallSound);
        }

        Debug.Log($"?? หินก้อนใหญ่ตก! ตำแหน่ง: {targetPos} (จาก {dropPoint.name})");

        // ทำลายหินหลังจากเวลาที่กำหนด
        Destroy(rock, rockLifetime);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, soundVolume);
        }
    }

    /// <summary>
    /// รีเซ็ต Trigger (สำหรับ Debug หรือใช้ซ้ำ)
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
        Debug.Log("?? รีเซ็ต Rock Trigger");
    }

    // Gizmos แสดงตำแหน่งที่หินจะตก
    void OnDrawGizmosSelected()
    {
        if (dropPoints == null || dropPoints.Length == 0) return;

        for (int i = 0; i < dropPoints.Length; i++)
        {
            if (dropPoints[i] == null) continue;

            // สีต่างกันแต่ละจุด
            Gizmos.color = Color.HSVToRGB((float)i / dropPoints.Length, 0.8f, 1f);

            // วงกลมแสดงรัศมีการสุ่ม
            Gizmos.DrawWireSphere(dropPoints[i].position, randomOffset);

            // เส้นแสดงความสูง
            Gizmos.DrawLine(dropPoints[i].position, dropPoints[i].position + Vector3.up * spawnHeight);

            // วงกลมที่ตำแหน่ง Spawn
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(dropPoints[i].position + Vector3.up * spawnHeight, 1f);

            // แสดงหมายเลข
#if UNITY_EDITOR
            UnityEditor.Handles.Label(dropPoints[i].position + Vector3.up * 0.5f, $"Point {i + 1}");
#endif
        }
    }
}