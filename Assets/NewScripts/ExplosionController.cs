using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ระบบระเบิด: ทำลาย Object, สั่นกล้อง, เล่นเสียง, เล่น Effect
/// </summary>
public class ExplosionController : MonoBehaviour
{
    [Header("?? วัตถุที่จะถูกทำลาย")]
    [Tooltip("วัตถุที่จะหายไปเมื่อระเบิด (เช่น กำแพง, หิน)")]
    public List<GameObject> objectsToDestroy = new List<GameObject>();

    [Header("? Particle Effect")]
    [Tooltip("เอฟเฟกต์การระเบิด (Particle System)")]
    public ParticleSystem explosionEffect;

    [Tooltip("ตำแหน่งที่จะ Spawn Effect (ถ้าไม่ใส่ จะใช้ตำแหน่งของ Script นี้)")]
    public Transform explosionPoint;

    [Tooltip("จำนวน Effect ที่จะ Spawn (สำหรับระเบิดใหญ่)")]
    public int effectCount = 1;

    [Tooltip("รัศมีการกระจาย Effect (ถ้า effectCount > 1)")]
    public float effectRadius = 2f;

    [Header("?? เสียงระเบิด")]
    [Tooltip("เสียงระเบิด")]
    public AudioClip explosionSound;

    [Range(0f, 1f)]
    public float explosionVolume = 1f;

    [Header("?? เสียงถ้ำถล่มต่อเนื่อง")]
    [Tooltip("เสียงถ้ำถล่มหลังระเบิด (เล่นลูปต่อเนื่องยาวๆ จนย้ายซีน)")]
    public AudioClip caveCollapseSound;

    [Range(0f, 1f)]
    public float caveCollapseVolume = 0.7f;

    [Tooltip("ดีเลย์ก่อนเล่นเสียงถ้ำถล่ม (วินาที)")]
    public float caveCollapseDelay = 0.5f;

    [Header("?? Camera Shake")]
    [Tooltip("ระยะเวลาที่สั่นแรง (วินาที)")]
    public float shakeDuration = 0.5f;

    [Tooltip("ความแรงของการสั่นแรก")]
    public float shakeMagnitude = 0.3f;

    [Tooltip("ความถี่ในการสั่น")]
    public float shakeFrequency = 25f;

    [Header("?? Continuous Shake (หลังระเบิด)")]
    [Tooltip("เปิดใช้การสั่นต่อเนื่องหลังระเบิด")]
    public bool enableContinuousShake = true;

    [Tooltip("ความแรงการสั่นต่อเนื่อง (เบากว่าการสั่นแรก)")]
    public float continuousShakeMagnitude = 0.05f;

    [Header("? Timing")]
    [Tooltip("ดีเลย์ก่อนเริ่มระเบิด (วินาที)")]
    public float explosionDelay = 0.2f;

    [Tooltip("ดีเลย์ก่อนทำลาย Object (วินาที) - ให้เวลา Effect เล่นก่อน")]
    public float destroyDelay = 0.3f;

    [Header("? Visual Effects")]
    [Tooltip("Flash สีขาวตอนระเบิด")]
    public bool enableFlash = true;

    [Tooltip("ระยะเวลา Flash")]
    public float flashDuration = 0.2f;

    [Header("?? Falling Debris (หินตก)")]
    [Tooltip("Prefab ของหินที่จะตกลงมา")]
    public GameObject debrisPrefab;

    [Tooltip("เปิดใช้การตกหินต่อเนื่อง")]
    public bool enableContinuousDebris = true;

    [Tooltip("จำนวนหินที่จะตกในช่วงแรก")]
    public int initialDebrisCount = 10;

    [Tooltip("จำนวนหินที่ตกต่อเนื่องต่อครั้ง")]
    public int continuousDebrisPerSpawn = 2;

    [Tooltip("ดีเลย์ระหว่างการ Spawn หินต่อเนื่อง (วินาที)")]
    public float continuousDebrisInterval = 0.5f;

    [Tooltip("ความสูงที่หิน Spawn (เหนือกล้อง)")]
    public float spawnHeight = 10f;

    [Tooltip("ระยะกว้างของการสุ่มตำแหน่ง X")]
    public float spawnRangeX = 15f;

    [Tooltip("ระยะห่างด้านหน้ากล้อง (Z)")]
    public float spawnDistanceInFront = 20f;

    [Tooltip("ดีเลย์ระหว่างการ Spawn แต่ละก้อน (วินาที)")]
    public float debrisSpawnInterval = 0.05f;

    [Tooltip("ระยะเวลาก่อนหินหายไป (วินาที)")]
    public float debrisLifetime = 5f;

    private AudioSource audioSource;
    private AudioSource caveCollapseAudioSource;
    private Camera mainCamera;
    private bool hasExploded = false;
    private bool isContinuousShaking = false;
    private Coroutine continuousShakeCoroutine;
    private Vector3 shakeOffset = Vector3.zero;
    private bool isSpawningDebris = false;
    private Coroutine continuousDebrisCoroutine;

    void Start()
    {
        // สร้าง AudioSource สำหรับเสียงระเบิด
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound

        // สร้าง AudioSource สำหรับเสียงถ้ำถล่ม (แยกเพื่อเล่นพร้อมกัน)
        caveCollapseAudioSource = gameObject.AddComponent<AudioSource>();
        caveCollapseAudioSource.playOnAwake = false;
        caveCollapseAudioSource.spatialBlend = 0f; // 2D sound
        caveCollapseAudioSource.loop = true; // ลูปเสียงจนกว่าจะย้ายซีน

        // หา Main Camera
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("[ExplosionController] ไม่พบ Main Camera!");
        }
    }

    void LateUpdate()
    {
        // ใช้ LateUpdate เพื่อให้กล้องทำงานหลังจาก Camera Follow เสร็จแล้ว
        if (mainCamera != null && (isContinuousShaking || shakeOffset != Vector3.zero))
        {
            mainCamera.transform.position += shakeOffset;
        }
    }

    /// <summary>
    /// เรียกฟังก์ชันนี้เพื่อเริ่มระเบิด (ใช้ใน Event)
    /// </summary>
    public void Explode()
    {
        if (hasExploded)
        {
            Debug.Log("?? ระเบิดไปแล้ว!");
            return;
        }

        hasExploded = true;
        Debug.Log("?? เริ่มระเบิด!");

        StartCoroutine(ExplosionSequence());
    }

    private IEnumerator ExplosionSequence()
    {
        // รอดีเลย์ก่อนระเบิด
        if (explosionDelay > 0)
        {
            yield return new WaitForSeconds(explosionDelay);
        }

        // เล่นเสียงระเบิด
        PlayExplosionSound();

        // เล่นเสียงถ้ำถล่ม (หลังจากดีเลย์)
        if (caveCollapseSound != null)
        {
            StartCoroutine(PlayCaveCollapseSound());
        }

        // สั่นกล้อง
        if (mainCamera != null)
        {
            StartCoroutine(ShakeCamera());
        }

        // Flash ตอนระเบิด
        if (enableFlash)
        {
            StartCoroutine(FlashEffect());
        }

        // เล่น Particle Effect
        SpawnExplosionEffects();

        // Spawn หินตก
        if (debrisPrefab != null)
        {
            StartCoroutine(SpawnInitialDebris());

            // เริ่มสั่นต่อเนื่อง
            if (enableContinuousDebris)
            {
                continuousDebrisCoroutine = StartCoroutine(SpawnContinuousDebris());
            }
        }

        // รอให้ Effect เล่นสักหน่อย แล้วค่อยทำลาย Object
        yield return new WaitForSeconds(destroyDelay);

        // ทำลาย/ซ่อน Object
        DestroyObjects();

        Debug.Log("? ระเบิดเสร็จสิ้น!");
    }

    private void PlayExplosionSound()
    {
        if (explosionSound != null)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(explosionSound, explosionVolume);
            else if (audioSource != null)
                audioSource.PlayOneShot(explosionSound, explosionVolume);

            Debug.Log("?? เล่นเสียงระเบิด");
        }
    }

    private IEnumerator PlayCaveCollapseSound()
    {
        // รอดีเลย์ก่อนเล่นเสียงถ้ำถล่ม
        if (caveCollapseDelay > 0)
        {
            yield return new WaitForSeconds(caveCollapseDelay);
        }

        // เล่นเสียงถ้ำถล่มต่อเนื่อง (ลูปจนกว่าจะย้ายซีน)
        if (AudioManager.Instance != null)
        {
            // ถ้าใช้ AudioManager ให้ส่งเสียงไปเล่น
            AudioManager.Instance.PlaySFX(caveCollapseSound, caveCollapseVolume);
        }
        else if (caveCollapseAudioSource != null)
        {
            // ถ้าไม่มี AudioManager ให้ใช้ AudioSource ของตัวเอง (ลูป)
            caveCollapseAudioSource.clip = caveCollapseSound;
            caveCollapseAudioSource.volume = caveCollapseVolume;
            caveCollapseAudioSource.loop = true;
            caveCollapseAudioSource.Play();
        }

        Debug.Log("?? เล่นเสียงถ้ำถล่มต่อเนื่อง (ลูป)");
    }

    /// <summary>
    /// หยุดเสียงถ้ำถล่ม (เรียกก่อนย้ายซีนถ้าต้องการ)
    /// </summary>
    public void StopCaveCollapseSound()
    {
        if (caveCollapseAudioSource != null && caveCollapseAudioSource.isPlaying)
        {
            caveCollapseAudioSource.Stop();
            Debug.Log("?? หยุดเสียงถ้ำถล่ม");
        }
    }

    private void SpawnExplosionEffects()
    {
        if (explosionEffect == null)
        {
            Debug.LogWarning("? ไม่ได้ตั้งค่า Explosion Effect!");
            return;
        }

        Vector3 centerPos = explosionPoint != null ? explosionPoint.position : transform.position;

        for (int i = 0; i < effectCount; i++)
        {
            Vector3 spawnPos = centerPos;

            // ถ้ามีหลาย Effect ให้กระจายออกไป
            if (effectCount > 1)
            {
                Vector2 randomOffset = Random.insideUnitCircle * effectRadius;
                spawnPos += new Vector3(randomOffset.x, 0, randomOffset.y);
            }

            // Spawn Effect
            ParticleSystem effect = Instantiate(explosionEffect, spawnPos, Quaternion.identity);
            effect.Play();

            // ทำลาย Effect หลังจากเล่นเสร็จ
            Destroy(effect.gameObject, effect.main.duration + effect.main.startLifetime.constantMax);
        }

        Debug.Log($"? Spawn Effect จำนวน {effectCount} ตัว");
    }

    private IEnumerator ShakeCamera()
    {
        if (mainCamera == null) yield break;

        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            // คำนวณ offset แบบสุ่ม
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;

            shakeOffset = new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // รีเซ็ต offset
        shakeOffset = Vector3.zero;
        Debug.Log("?? Camera Shake แรงเสร็จสิ้น");

        // เริ่มสั่นต่อเนื่องเบาๆ
        if (enableContinuousShake)
        {
            continuousShakeCoroutine = StartCoroutine(ContinuousShake());
        }
    }

    private IEnumerator ContinuousShake()
    {
        if (mainCamera == null) yield break;

        isContinuousShaking = true;

        Debug.Log("?? เริ่มสั่นจอแบบต่อเนื่อง");

        while (isContinuousShaking)
        {
            // สั่นเบาๆแบบสุ่ม
            float x = Random.Range(-1f, 1f) * continuousShakeMagnitude;
            float y = Random.Range(-1f, 1f) * continuousShakeMagnitude;

            shakeOffset = new Vector3(x, y, 0);

            yield return null;
        }

        // รีเซ็ต offset เมื่อหยุด
        shakeOffset = Vector3.zero;
    }

    /// <summary>
    /// หยุดการสั่นต่อเนื่อง (เรียกก่อนเปลี่ยนซีนถ้าต้องการ)
    /// </summary>
    public void StopContinuousShake()
    {
        isContinuousShaking = false;
        shakeOffset = Vector3.zero;
        if (continuousShakeCoroutine != null)
        {
            StopCoroutine(continuousShakeCoroutine);
        }
        Debug.Log("?? หยุดการสั่นต่อเนื่อง");
    }

    void OnDestroy()
    {
        // หยุดสั่นและเสียงเมื่อ Object ถูกทำลาย
        StopContinuousShake();
        StopContinuousDebris();
        StopCaveCollapseSound();
    }

    private IEnumerator FlashEffect()
    {
        // สร้าง Flash UI (ถ้ามี Canvas)
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) yield break;

        // สร้าง White Panel
        GameObject flashObj = new GameObject("Flash");
        flashObj.transform.SetParent(canvas.transform, false);

        UnityEngine.UI.Image flashImage = flashObj.AddComponent<UnityEngine.UI.Image>();
        flashImage.color = Color.white;

        RectTransform rect = flashObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        // Fade out
        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            float alpha = 1f - (elapsed / flashDuration);
            flashImage.color = new Color(1, 1, 1, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(flashObj);
    }

    private IEnumerator SpawnInitialDebris()
    {
        if (mainCamera == null || debrisPrefab == null) yield break;

        Debug.Log("?? เริ่ม Spawn หินตกช่วงแรก");

        for (int i = 0; i < initialDebrisCount; i++)
        {
            SpawnSingleDebris();
            yield return new WaitForSeconds(debrisSpawnInterval);
        }
    }

    private IEnumerator SpawnContinuousDebris()
    {
        if (mainCamera == null || debrisPrefab == null) yield break;

        isSpawningDebris = true;
        Debug.Log("?? เริ่มตกหินต่อเนื่อง");

        while (isSpawningDebris)
        {
            // Spawn หลายก้อนพร้อมกัน
            for (int i = 0; i < continuousDebrisPerSpawn; i++)
            {
                SpawnSingleDebris();
            }

            yield return new WaitForSeconds(continuousDebrisInterval);
        }
    }

    private void SpawnSingleDebris()
    {
        if (mainCamera == null || debrisPrefab == null) return;

        // ตำแหน่งกล้อง
        Vector3 cameraPos = mainCamera.transform.position;
        Vector3 cameraForward = mainCamera.transform.forward;

        // สุ่มตำแหน่ง X (ซ้าย-ขวา) และ Z (หน้ากล้อง)
        float randomX = Random.Range(-spawnRangeX, spawnRangeX);
        float randomZ = Random.Range(spawnDistanceInFront * 0.5f, spawnDistanceInFront);

        // คำนวณตำแหน่ง Spawn (ด้านหน้ากล้อง + เหนือขึ้นไป)
        Vector3 spawnPos = cameraPos
            + mainCamera.transform.right * randomX
            + Vector3.up * spawnHeight
            + cameraForward * randomZ;

        // Spawn หิน
        GameObject debris = Instantiate(debrisPrefab, spawnPos, Random.rotation);

        // แก้ Mesh Collider ให้เป็น Convex (ถ้ามี)
        MeshCollider meshCol = debris.GetComponent<MeshCollider>();
        if (meshCol != null)
        {
            meshCol.convex = true;
        }

        // เพิ่ม Rigidbody (3D)
        Rigidbody rb = debris.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = debris.AddComponent<Rigidbody>();
        }

        // ตั้งค่า Rigidbody
        rb.mass = Random.Range(0.5f, 2f);
        rb.angularVelocity = Random.insideUnitSphere * 5f;

        // เพิ่มแรงเล็กน้อย
        rb.velocity = new Vector3(Random.Range(-1f, 1f), Random.Range(-2f, 0f), Random.Range(-1f, 1f));

        // ทำลายหินหลังจากเวลาที่กำหนด
        Destroy(debris, debrisLifetime);
    }

    /// <summary>
    /// หยุดการตกหินต่อเนื่อง
    /// </summary>
    public void StopContinuousDebris()
    {
        isSpawningDebris = false;
        if (continuousDebrisCoroutine != null)
        {
            StopCoroutine(continuousDebrisCoroutine);
        }
        Debug.Log("?? หยุดการตกหินต่อเนื่อง");
    }

    private void DestroyObjects()
    {
        foreach (GameObject obj in objectsToDestroy)
        {
            if (obj != null)
            {
                Debug.Log($"?? ทำลาย: {obj.name}");

                // เลือกว่าจะ Destroy หรือแค่ Disable
                // แบบที่ 1: ทำลายทิ้งเลย
                Destroy(obj);

                // แบบที่ 2: แค่ซ่อน (ใช้บรรทัดนี้แทน)
                // obj.SetActive(false);
            }
        }
    }

    /// <summary>
    /// รีเซ็ตเพื่อระเบิดใหม่ (สำหรับ Debug)
    /// </summary>
    public void ResetExplosion()
    {
        hasExploded = false;
        StopContinuousShake();
        StopContinuousDebris();
        StopCaveCollapseSound();
        Debug.Log("?? รีเซ็ต Explosion");
    }

    // Gizmos สำหรับดูรัศมี Effect ใน Scene View
    void OnDrawGizmosSelected()
    {
        if (explosionPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(explosionPoint.position, effectRadius);
    }
}