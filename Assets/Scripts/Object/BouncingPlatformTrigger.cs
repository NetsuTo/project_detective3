using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trigger ที่ทำให้วัตถุตกลงมาเป็น Platform จากหลายจุดที่กำหนด (ไม่ซ้ำจุด) + มีการเด้ง
/// เมื่อ Player เหยียบ Trigger ? วัตถุตกมายังตำแหน่งที่ Set ไว้ แล้วเด้งกระดอนเล็กน้อย
/// มีเอฟเฟคและเสียงแค่ตอนถึงพื้นครั้งแรก พร้อมระบบเสียงที่เชื่อมกับ AudioManager
/// รองรับการใส่ Platform หลายแบบ (สุ่มเลือกไม่ซ้ำในแต่ละรอบ)
/// </summary>
public class BouncingPlatformTrigger : MonoBehaviour
{
    [Header("?? วัตถุที่จะตก (Platform)")]
    [Tooltip("Prefab ของ Platform ที่จะตกลงมา (หลายแบบ - จะสุ่มเลือกไม่ซ้ำ)")]
    public GameObject[] platformPrefabs;

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

    [Header("?? การเด้ง")]
    [Tooltip("จำนวนครั้งที่จะเด้ง")]
    public int bounceCount = 2;

    [Tooltip("ความสูงของการเด้งครั้งแรก")]
    public float firstBounceHeight = 1.5f;

    [Tooltip("เปอร์เซ็นต์ที่ความสูงลดลงในแต่ละครั้ง (0-1)")]
    [Range(0f, 1f)]
    public float bounceDecay = 0.6f;

    [Tooltip("ความเร็วในการเด้ง (ยิ่งมากยิ่งเร็ว)")]
    public float bounceSpeed = 8f;

    [Header("?? การหมุนของ Platform")]
    [Tooltip("ใช้ Rotation ของ Drop Point")]
    public bool useDropPointRotation = true;

    [Tooltip("มุมหมุนเพิ่มเติมของ Platform (X, Y, Z)")]
    public Vector3 platformRotationOffset = new Vector3(-90, 0, 0);

    [Tooltip("ความเร็วในการหมุน Platform ขณะเด้ง (องศา/วินาที) - ตั้ง 0 ถ้าไม่ต้องการหมุน")]
    public float rotationSpeed = 180f;

    [Tooltip("แกนที่จะหมุน (X=pitch, Y=yaw, Z=roll)")]
    public Vector3 rotationAxis = new Vector3(1, 0, 0);

    [Header("?? การสุ่ม")]
    [Tooltip("จำนวน Platform ที่จะตก (สุ่มจาก Drop Points แบบไม่ซ้ำ)")]
    public int numberOfPlatforms = 1;

    [Tooltip("ระยะห่างระหว่างการตกของแต่ละชิ้น (วินาที)")]
    public float platformDropInterval = 0.5f;

    [Tooltip("สุ่ม Prefab ไม่ซ้ำกันในแต่ละรอบที่ trigger")]
    public bool uniquePrefabsPerTrigger = true;

    [Header("? พฤติกรรม")]
    [Tooltip("ระยะเวลาก่อน Platform หายไป (วินาที) - ถ้าเป็น 0 จะไม่หาย")]
    public float platformLifetime = 0f;

    [Tooltip("ทริกเกอร์ได้แค่ครั้งเดียว")]
    public bool triggerOnce = true;

    [Header("? เอฟเฟคตอนตกถึง")]
    [Tooltip("Particle Effect ที่จะเล่นตอน Platform ตกถึงครั้งแรก")]
    public GameObject landingEffect;

    [Tooltip("ระยะเวลาก่อน Effect หายไป (วินาที)")]
    public float effectDuration = 2f;

    [Header("?? เสียง")]
    [Tooltip("เสียงตอน Platform ตกถึงครั้งแรก")]
    public AudioClip landingSound;

    [Range(0f, 1f)]
    [Tooltip("ระดับเสียง (0 = เงียบ, 1 = เต็ม)")]
    public float soundVolume = 0.3f;

    [Range(0f, 1f)]
    [Tooltip("Spatial Blend (0 = 2D, 1 = 3D) - แนะนำ 0.5-1 เพื่อลดเสียงดัง")]
    public float spatialBlend = 0.8f;

    [Tooltip("ระยะที่เสียงได้ยินสูงสุด (3D Sound)")]
    public float maxSoundDistance = 50f;

    [Header("??? Tag ของ Player")]
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
        localAudioSource.spatialBlend = spatialBlend;
        localAudioSource.maxDistance = maxSoundDistance;
        localAudioSource.rolloffMode = AudioRolloffMode.Linear;

        // ตรวจสอบ Collider
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        else
        {
            Debug.LogWarning("?? [BouncingPlatformTrigger] ไม่มี Collider! กรุณาเพิ่ม Collider และเปิด 'Is Trigger'");
        }

        // ตรวจสอบว่ามี Drop Points หรือไม่
        if (dropPoints == null || dropPoints.Length == 0)
        {
            Debug.LogError("? [BouncingPlatformTrigger] ไม่ได้ใส่ Drop Points! Platform จะไม่รู้ว่าจะตกที่ไหน!");
        }

        // ตรวจสอบว่ามี Platform Prefabs หรือไม่
        if (platformPrefabs == null || platformPrefabs.Length == 0)
        {
            Debug.LogError("? [BouncingPlatformTrigger] ไม่ได้ใส่ Platform Prefabs! Platform จะไม่มีอะไรตกลงมา!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // เช็คว่าเป็น Player หรือไม่
        if (other.CompareTag(playerTag))
        {
            TriggerBouncingPlatform();
        }
    }

    /// <summary>
    /// เริ่มให้ Platform ตก (เรียกจาก Trigger หรือ Script อื่นก็ได้)
    /// </summary>
    public void TriggerBouncingPlatform()
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

        // สุ่ม Drop Points แบบไม่ซ้ำ
        List<Transform> shuffledPoints = GetShuffledDropPoints();

        if (shuffledPoints.Count == 0)
        {
            Debug.LogError("? ไม่มี Drop Points ที่ใช้งานได้!");
            yield break;
        }

        // จำกัดจำนวน Platform ไม่ให้เกินจำนวน Drop Points
        int actualPlatformCount = Mathf.Min(numberOfPlatforms, shuffledPoints.Count);

        if (actualPlatformCount < numberOfPlatforms)
        {
            Debug.LogWarning($"?? มี Drop Points เพียง {shuffledPoints.Count} จุด แต่ตั้งค่าให้ตก {numberOfPlatforms} ชิ้น - จะตกเพียง {actualPlatformCount} ชิ้น");
        }

        // สุ่ม Prefabs แบบไม่ซ้ำ (ถ้าเปิดใช้งาน)
        List<GameObject> selectedPrefabs = GetShuffledPrefabs(actualPlatformCount);

        // ตก Platform ตามจำนวนที่กำหนด (แต่ละจุดไม่ซ้ำ)
        for (int i = 0; i < actualPlatformCount; i++)
        {
            Transform selectedDropPoint = shuffledPoints[i];
            GameObject selectedPrefab = selectedPrefabs[i];

            if (selectedDropPoint != null && selectedPrefab != null)
            {
                SpawnBouncingPlatform(selectedDropPoint, selectedPrefab);
            }

            // รอก่อนตกชิ้นถัดไป (ถ้ามีหลายชิ้น)
            if (i < actualPlatformCount - 1)
            {
                yield return new WaitForSeconds(platformDropInterval);
            }
        }
    }

    /// <summary>
    /// สุ่ม Drop Points แบบไม่ซ้ำ (Fisher-Yates Shuffle)
    /// </summary>
    private List<Transform> GetShuffledDropPoints()
    {
        if (dropPoints == null || dropPoints.Length == 0)
            return new List<Transform>();

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
            return new List<Transform>();
        }

        // สุ่มลำดับแบบ Fisher-Yates Shuffle
        for (int i = validPoints.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            Transform temp = validPoints[i];
            validPoints[i] = validPoints[randomIndex];
            validPoints[randomIndex] = temp;
        }

        return validPoints;
    }

    /// <summary>
    /// สุ่ม Prefabs แบบไม่ซ้ำ (ถ้าเปิดใช้งาน uniquePrefabsPerTrigger)
    /// </summary>
    private List<GameObject> GetShuffledPrefabs(int count)
    {
        List<GameObject> result = new List<GameObject>();

        if (platformPrefabs == null || platformPrefabs.Length == 0)
        {
            Debug.LogWarning("?? ไม่ได้ใส่ Platform Prefabs!");
            return result;
        }

        // กรองเอาเฉพาะ Prefab ที่ไม่เป็น null
        List<GameObject> validPrefabs = new List<GameObject>();
        foreach (GameObject prefab in platformPrefabs)
        {
            if (prefab != null)
            {
                validPrefabs.Add(prefab);
            }
        }

        if (validPrefabs.Count == 0)
        {
            Debug.LogWarning("?? ไม่มี Platform Prefab ที่ใช้งานได้!");
            return result;
        }

        // ถ้าเปิด uniquePrefabsPerTrigger = สุ่มไม่ซ้ำ
        if (uniquePrefabsPerTrigger)
        {
            // ถ้าต้องการ Platform มากกว่าจำนวน Prefab ที่มี
            if (count > validPrefabs.Count)
            {
                Debug.LogWarning($"?? ต้องการ Platform {count} ชิ้น แต่มี Prefab แค่ {validPrefabs.Count} แบบ - จะวนซ้ำ Prefab");

                // สุ่ม Prefabs แล้ววนซ้ำจนครบจำนวน
                List<GameObject> shuffled = new List<GameObject>(validPrefabs);

                // Shuffle
                for (int i = shuffled.Count - 1; i > 0; i--)
                {
                    int randomIndex = Random.Range(0, i + 1);
                    GameObject temp = shuffled[i];
                    shuffled[i] = shuffled[randomIndex];
                    shuffled[randomIndex] = temp;
                }

                // วนซ้ำจนครบจำนวน
                for (int i = 0; i < count; i++)
                {
                    result.Add(shuffled[i % shuffled.Count]);
                }
            }
            else
            {
                // สุ่ม Prefabs แบบไม่ซ้ำ (Fisher-Yates Shuffle)
                List<GameObject> shuffled = new List<GameObject>(validPrefabs);

                for (int i = shuffled.Count - 1; i > 0; i--)
                {
                    int randomIndex = Random.Range(0, i + 1);
                    GameObject temp = shuffled[i];
                    shuffled[i] = shuffled[randomIndex];
                    shuffled[randomIndex] = temp;
                }

                // เอาเฉพาะจำนวนที่ต้องการ
                for (int i = 0; i < count; i++)
                {
                    result.Add(shuffled[i]);
                }

                Debug.Log($"?? สุ่ม Prefab ไม่ซ้ำ: {string.Join(", ", result.ConvertAll(p => p.name))}");
            }
        }
        else
        {
            // สุ่มแบบธรรมดา (อาจซ้ำได้)
            for (int i = 0; i < count; i++)
            {
                GameObject randomPrefab = validPrefabs[Random.Range(0, validPrefabs.Count)];
                result.Add(randomPrefab);
            }

            Debug.Log($"?? สุ่ม Prefab: {string.Join(", ", result.ConvertAll(p => p.name))}");
        }

        return result;
    }

    private void SpawnBouncingPlatform(Transform dropPoint, GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("?? Prefab เป็น null!");
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

        GameObject platform = Instantiate(prefab, spawnPos, spawnRotation);
        activePlatforms.Add(platform);

        // ลบ Rigidbody ถ้ามี (เพราะเราไม่ใช้ Physics)
        Rigidbody rb = platform.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Destroy(rb);
        }

        Debug.Log($"?? Platform ตก! [{prefab.name}] จาก {spawnPos} ? {targetPos} (จาก {dropPoint.name})");

        // เริ่ม Animation ตกและเด้ง
        StartCoroutine(AnimatePlatformFallingAndBouncing(platform, targetPos));

        // ทำลาย Platform หลังจากเวลาที่กำหนด (ถ้าตั้งค่าไว้)
        if (platformLifetime > 0)
        {
            Destroy(platform, platformLifetime);
        }
    }

    /// <summary>
    /// Animation ให้ Platform ตกลงมาแบบ Smooth แล้วเด้งขึ้นลง
    /// </summary>
    private IEnumerator AnimatePlatformFallingAndBouncing(GameObject platform, Vector3 targetPos)
    {
        if (platform == null) yield break;

        // ========== ตอนที่ 1: ตกลงมา ==========
        Vector3 startPos = platform.transform.position;
        float elapsedTime = 0f;
        float fallDuration = Vector3.Distance(startPos, targetPos) / fallSpeed;

        while (elapsedTime < fallDuration && platform != null)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fallDuration;

            // ใช้ Lerp เพื่อความ Smooth
            platform.transform.position = Vector3.Lerp(startPos, targetPos, t);

            yield return null;
        }

        // ตรวจสอบว่า Platform ยังอยู่หรือไม่
        if (platform == null) yield break;

        // ตั้งตำแหน่งให้แน่นอน
        platform.transform.position = targetPos;

        // เรียก Landing Effect และเสียง (เฉพาะครั้งแรก)
        OnPlatformLanded(platform, targetPos);

        // ========== ตอนที่ 2: เด้งขึ้นลง ==========
        float currentBounceHeight = firstBounceHeight;

        for (int i = 0; i < bounceCount; i++)
        {
            if (platform == null) yield break;

            // คำนวณความสูงของการเด้งในรอบนี้
            Vector3 bounceTarget = targetPos + Vector3.up * currentBounceHeight;

            // เด้งขึ้น
            float bounceUpTime = 0f;
            float bounceUpDuration = currentBounceHeight / bounceSpeed;

            while (bounceUpTime < bounceUpDuration && platform != null)
            {
                bounceUpTime += Time.deltaTime;
                float t = bounceUpTime / bounceUpDuration;

                // ใช้ SmoothStep เพื่อความนุ่มนวล
                float easedT = Mathf.SmoothStep(0f, 1f, t);
                platform.transform.position = Vector3.Lerp(targetPos, bounceTarget, easedT);

                // หมุน Platform ขณะเด้งขึ้น
                if (rotationSpeed > 0)
                {
                    float rotationAmount = rotationSpeed * Time.deltaTime;
                    platform.transform.Rotate(rotationAxis.normalized * rotationAmount, Space.Self);
                }

                yield return null;
            }

            if (platform == null) yield break;

            // เด้งลง
            float bounceDownTime = 0f;
            float bounceDownDuration = currentBounceHeight / bounceSpeed;

            Vector3 bounceStartPos = platform.transform.position;

            while (bounceDownTime < bounceDownDuration && platform != null)
            {
                bounceDownTime += Time.deltaTime;
                float t = bounceDownTime / bounceDownDuration;

                // ใช้ SmoothStep เพื่อความนุ่มนวล
                float easedT = Mathf.SmoothStep(0f, 1f, t);
                platform.transform.position = Vector3.Lerp(bounceStartPos, targetPos, easedT);

                // หมุน Platform ขณะเด้งลง
                if (rotationSpeed > 0)
                {
                    float rotationAmount = rotationSpeed * Time.deltaTime;
                    platform.transform.Rotate(rotationAxis.normalized * rotationAmount, Space.Self);
                }

                yield return null;
            }

            if (platform == null) yield break;

            // ตั้งตำแหน่งกลับไปที่พื้น
            platform.transform.position = targetPos;

            // ลดความสูงของการเด้งในรอบถัดไป
            currentBounceHeight *= bounceDecay;

            Debug.Log($"?? Platform เด้งครั้งที่ {i + 1}/{bounceCount}");
        }

        Debug.Log($"? Platform เด้งเสร็จแล้ว!");
    }

    /// <summary>
    /// เมื่อ Platform ตกถึงครั้งแรก - เล่นเอฟเฟคและเสียง
    /// </summary>
    private void OnPlatformLanded(GameObject platform, Vector3 landingPos)
    {
        if (platform == null) return;

        Debug.Log($"? Platform ตกถึงแล้ว! ตำแหน่ง: {landingPos}");

        // สร้างเอฟเฟค
        if (landingEffect != null)
        {
            GameObject effect = Instantiate(landingEffect, landingPos, Quaternion.identity);
            Destroy(effect, effectDuration);
        }

        // เล่นเสียงตกถึง (แค่ครั้งแรก)
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
            // ลด volume ลงเพราะ AudioManager มักจะมี Master Volume อยู่แล้ว
            float adjustedVolume = soundVolume * 0.4f; // ลด 60% เพื่อไม่ให้ดังเกิน
            audioManager.PlaySFX(clip, adjustedVolume);
            Debug.Log($"?? เล่นเสียงผ่าน AudioManager.PlaySFX: {clip.name} (Volume: {adjustedVolume:F2})");
        }
        // ถ้าไม่มี AudioManager ใช้ AudioSource ของตัวเอง
        else if (localAudioSource != null)
        {
            localAudioSource.volume = soundVolume;
            localAudioSource.PlayOneShot(clip, soundVolume);
            Debug.Log($"?? เล่นเสียงผ่าน Local AudioSource: {clip.name} (Volume: {soundVolume}, Spatial: {spatialBlend})");
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

        Debug.Log("?? รีเซ็ต Bouncing Platform Trigger");
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

            // แสดงความสูงของการเด้ง
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(dropPoints[i].position + Vector3.up * firstBounceHeight, 0.5f);

            // แสดงหมายเลข
#if UNITY_EDITOR
            UnityEditor.Handles.Label(dropPoints[i].position + Vector3.up * 0.5f, $"Platform {i + 1}\nBounce: {bounceCount}x");
#endif
        }
    }
}