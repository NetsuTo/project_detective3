using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trigger ที่ทำให้วัตถุตกลงมาเป็น Platform จากหลายจุดที่กำหนด
/// เมื่อ Player เหยียบ Trigger ? วัตถุตกมายังตำแหน่งที่ Set ไว้
/// มีเอฟเฟคและเสียง พร้อมระบบเสียงที่เชื่อมกับ AudioManager
/// </summary>
public class FallingPlatformTrigger : MonoBehaviour
{
    [Header("?? วัตถุที่จะตก (Platform)")]
    [Tooltip("Prefab ของ Platform ที่จะตกลงมา")]
    public GameObject platformPrefab;

    [Tooltip("จุดที่ Platform จะตกลงมา (ตำแหน่งสุดท้าย)")]
    public Transform[] dropPoints;

    [Header("?? การตกของ Platform")]
    [Tooltip("ดีเลย์หลังเหยียบ Trigger กี่วินาที Platform ถึงตก")]
    public float dropDelay = 1f;

    [Tooltip("ความสูงที่ Platform จะ Spawn (เหนือ Drop Point)")]
    public float spawnHeight = 20f;

    [Tooltip("ความเร็วในการตก (ยิ่งมากยิ่งเร็ว)")]
    public float fallSpeed = 5f;

    [Tooltip("สุ่มตำแหน่งรอบๆ Drop Point เล็กน้อย (รัศมี)")]
    public float randomOffset = 0.5f;

    [Header("?? การหมุนของ Platform")]
    [Tooltip("ใช้ Rotation ของ Drop Point")]
    public bool useDropPointRotation = true;

    [Tooltip("มุมหมุนเพิ่มเติมของ Platform (X, Y, Z)")]
    public Vector3 platformRotationOffset = new Vector3(-90, 0, 0);

    [Header("?? การสุ่ม")]
    [Tooltip("จำนวน Platform ที่จะตก (สุ่มจาก Drop Points)")]
    public int numberOfPlatforms = 1;

    [Tooltip("ระยะห่างระหว่างการตกของแต่ละชิ้น (วินาที)")]
    public float platformDropInterval = 0.5f;

    [Header("? พฤติกรรม")]
    [Tooltip("ระยะเวลาก่อน Platform หายไป (วินาที) - ถ้าเป็น 0 จะไม่หาย")]
    public float platformLifetime = 0f;

    [Tooltip("ทริกเกอร์ได้แค่ครั้งเดียว")]
    public bool triggerOnce = true;

    [Header("? เอฟเฟคตอนตกถึง")]
    [Tooltip("Particle Effect ที่จะเล่นตอน Platform ตกถึง")]
    public GameObject landingEffect;

    [Tooltip("ระยะเวลาก่อน Effect หายไป (วินาที)")]
    public float effectDuration = 2f;

    [Header("?? เสียง")]
    [Tooltip("เสียงตอน Platform ตกถึง")]
    public AudioClip landingSound;

    [Range(0f, 1f)]
    public float soundVolume = 0.7f;

    [Header("?? Tag ของ Player")]
    [Tooltip("Tag ที่จะทริกเกอร์ (เช่น 'Player')")]
    public string playerTag = "Player";

    private bool hasTriggered = false;
    private AudioSource localAudioSource;
    private List<GameObject> activePlatforms = new List<GameObject>();

    void Start()
    {
        // สร้าง AudioSource สำหรับใช้เอง (ถ้าไม่มี AudioManager)
        localAudioSource = gameObject.AddComponent<AudioSource>();
        localAudioSource.playOnAwake = false;
        localAudioSource.spatialBlend = 0f;

        // ตรวจสอบ Collider
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        else
        {
            Debug.LogWarning("?? [FallingPlatformTrigger] ไม่มี Collider! กรุณาเพิ่ม Collider และเปิด 'Is Trigger'");
        }

        // ตรวจสอบว่ามี Drop Points หรือไม่
        if (dropPoints == null || dropPoints.Length == 0)
        {
            Debug.LogError("? [FallingPlatformTrigger] ไม่ได้ใส่ Drop Points! Platform จะไม่รู้ว่าจะตกที่ไหน!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // เช็คว่าเป็น Player หรือไม่
        if (other.CompareTag(playerTag))
        {
            TriggerFallingPlatform();
        }
    }

    /// <summary>
    /// เริ่มให้ Platform ตก (เรียกจาก Trigger หรือ Script อื่นก็ได้)
    /// </summary>
    public void TriggerFallingPlatform()
    {
        if (triggerOnce && hasTriggered)
        {
            Debug.Log("?? Trigger นี้ถูกใช้ไปแล้ว!");
            return;
        }

        if (dropPoints == null || dropPoints.Length == 0)
        {
            Debug.LogError("? ไม่มี Drop Points! ไม่สามารถตก Platform ได้!");
            return;
        }

        hasTriggered = true;

        Debug.Log($"? Player เหยียบ Trigger! Platform {numberOfPlatforms} ชิ้นจะตกในอีก {dropDelay} วินาที!");

        // เริ่ม Coroutine ตก Platform
        StartCoroutine(DropPlatformSequence());
    }

    private IEnumerator DropPlatformSequence()
    {
        // รอตาม delay
        yield return new WaitForSeconds(dropDelay);

        // ตก Platform ตามจำนวนที่กำหนด
        for (int i = 0; i < numberOfPlatforms; i++)
        {
            // สุ่มเลือก Drop Point
            Transform selectedDropPoint = GetRandomDropPoint();

            if (selectedDropPoint != null)
            {
                SpawnFallingPlatform(selectedDropPoint);
            }

            // รอก่อนตกชิ้นถัดไป (ถ้ามีหลายชิ้น)
            if (i < numberOfPlatforms - 1)
            {
                yield return new WaitForSeconds(platformDropInterval);
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

    private void SpawnFallingPlatform(Transform dropPoint)
    {
        if (platformPrefab == null)
        {
            Debug.LogWarning("?? ไม่ได้ใส่ Platform Prefab!");
            return;
        }

        // ตำแหน่งที่ Platform จะไป (มีการสุ่มเล็กน้อย)
        Vector3 targetPos = dropPoint.position;

        if (randomOffset > 0)
        {
            // สุ่มในระนาบ Local ของ Drop Point
            Vector2 randomCircle = Random.insideUnitCircle * randomOffset;
            Vector3 randomInLocal = new Vector3(randomCircle.x, 0, randomCircle.y);

            if (useDropPointRotation)
            {
                targetPos += dropPoint.rotation * randomInLocal;
            }
            else
            {
                targetPos += randomInLocal;
            }
        }

        // คำนวณตำแหน่ง Spawn - ตกแนวตั้งเสมอ แต่ Platform หมุนได้
        Vector3 spawnPos = targetPos + Vector3.up * spawnHeight;

        // Spawn Platform พร้อม Rotation (ปรับแต่งได้เอง)
        Quaternion spawnRotation;
        if (useDropPointRotation)
        {
            // เอาการหมุนจาก Drop Point + บวกกับ Offset ที่ตั้งค่าเอง
            Vector3 finalRotation = dropPoint.eulerAngles + platformRotationOffset;
            spawnRotation = Quaternion.Euler(finalRotation);
        }
        else
        {
            // ใช้แค่ Offset ที่ตั้งค่าเอง
            spawnRotation = Quaternion.Euler(platformRotationOffset);
        }

        GameObject platform = Instantiate(platformPrefab, spawnPos, spawnRotation);
        activePlatforms.Add(platform);

        // ลบ Rigidbody ถ้ามี (เพราะเราไม่ใช้ Physics)
        Rigidbody rb = platform.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Destroy(rb);
        }

        Debug.Log($"?? Platform ตก! จาก {spawnPos} ? {targetPos} (จาก {dropPoint.name})");

        // เริ่ม Animation ตก
        StartCoroutine(AnimatePlatformFalling(platform, targetPos));

        // ทำลาย Platform หลังจากเวลาที่กำหนด (ถ้าตั้งค่าไว้)
        if (platformLifetime > 0)
        {
            Destroy(platform, platformLifetime);
        }
    }

    /// <summary>
    /// Animation ให้ Platform ตกลงมาแบบ Smooth
    /// </summary>
    private IEnumerator AnimatePlatformFalling(GameObject platform, Vector3 targetPos)
    {
        if (platform == null) yield break;

        Vector3 startPos = platform.transform.position;
        float elapsedTime = 0f;
        float duration = Vector3.Distance(startPos, targetPos) / fallSpeed;

        while (elapsedTime < duration && platform != null)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // ใช้ Lerp เพื่อความ Smooth
            platform.transform.position = Vector3.Lerp(startPos, targetPos, t);

            yield return null;
        }

        // ตรวจสอบว่า Platform ยังอยู่หรือไม่
        if (platform != null)
        {
            // ตั้งตำแหน่งให้แน่นอน
            platform.transform.position = targetPos;

            // เรียก Landing Effect
            OnPlatformLanded(platform, targetPos);
        }
    }

    /// <summary>
    /// เมื่อ Platform ตกถึง - เล่นเอฟเฟคและเสียง
    /// </summary>
    private void OnPlatformLanded(GameObject platform, Vector3 landingPos)
    {
        if (platform == null) return;

        Debug.Log($"?? Platform ตกถึงแล้ว! ตำแหน่ง: {landingPos}");

        // สร้างเอฟเฟค
        if (landingEffect != null)
        {
            GameObject effect = Instantiate(landingEffect, landingPos, Quaternion.identity);
            Destroy(effect, effectDuration);
        }

        // เล่นเสียงตกถึง
        if (landingSound != null)
        {
            PlaySound(landingSound);
        }
    }

    /// <summary>
    /// เล่นเสียง - ใช้ AudioManager.PlaySFX ก่อน ถ้าไม่มีใช้ Local AudioSource
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;

        // ลองหา AudioManager ก่อน
        AudioManager audioManager = FindObjectOfType<AudioManager>();

        if (audioManager != null)
        {
            // ใช้ PlaySFX จาก AudioManager
            audioManager.PlaySFX(clip);
            Debug.Log($"?? เล่นเสียงผ่าน AudioManager.PlaySFX: {clip.name}");
        }
        // ถ้าไม่มี AudioManager ใช้ AudioSource ของตัวเอง
        else if (localAudioSource != null)
        {
            localAudioSource.PlayOneShot(clip, soundVolume);
            Debug.Log($"?? เล่นเสียงผ่าน Local AudioSource: {clip.name}");
        }
        else
        {
            Debug.LogWarning("?? ไม่มี AudioManager และ Local AudioSource - ไม่สามารถเล่นเสียงได้");
        }
    }

    /// <summary>
    /// รีเซ็ต Trigger (สำหรับ Debug หรือใช้ซ้ำ)
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;

        // ลบ Platform ที่ตกทั้งหมด
        foreach (GameObject platform in activePlatforms)
        {
            if (platform != null)
            {
                Destroy(platform);
            }
        }
        activePlatforms.Clear();

        Debug.Log("?? รีเซ็ต Falling Platform Trigger");
    }

    // Gizmos แสดงตำแหน่งที่ Platform จะตก
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

            // Cube แสดงตำแหน่งที่ Platform จะอยู่
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(dropPoints[i].position, Vector3.one * 2f);

            // แสดงหมายเลข
#if UNITY_EDITOR
            UnityEditor.Handles.Label(dropPoints[i].position + Vector3.up * 0.5f, $"Platform {i + 1}");
#endif
        }
    }
}