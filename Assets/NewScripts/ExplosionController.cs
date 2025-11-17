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

    [Header("?? Particle Effect")]
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

    [Header("?? Camera Shake")]
    [Tooltip("ระยะเวลาที่สั่น (วินาที)")]
    public float shakeDuration = 0.5f;

    [Tooltip("ความแรงของการสั่น")]
    public float shakeMagnitude = 0.3f;

    [Tooltip("ความถี่ในการสั่น")]
    public float shakeFrequency = 25f;

    [Header("?? Timing")]
    [Tooltip("ดีเลย์ก่อนเริ่มระเบิด (วินาที)")]
    public float explosionDelay = 0.2f;

    [Tooltip("ดีเลย์ก่อนทำลาย Object (วินาที) - ให้เวลา Effect เล่นก่อน")]
    public float destroyDelay = 0.3f;

    [Header("? Visual Effects")]
    [Tooltip("Flash สีขาวตอนระเบิด")]
    public bool enableFlash = true;

    [Tooltip("ระยะเวลา Flash")]
    public float flashDuration = 0.2f;

    private AudioSource audioSource;
    private Camera mainCamera;
    private bool hasExploded = false;

    void Start()
    {
        // สร้าง AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound

        // หา Main Camera
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("[ExplosionController] ไม่พบ Main Camera!");
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
            AudioManager.Instance.PlaySFX(explosionSound);
            Debug.Log("?? เล่นเสียงระเบิด");
        }
    }

    private void SpawnExplosionEffects()
    {
        if (explosionEffect == null)
        {
            Debug.LogWarning("?? ไม่ได้ตั้งค่า Explosion Effect!");
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

        Debug.Log($"?? Spawn Effect จำนวน {effectCount} ตัว");
    }

    private IEnumerator ShakeCamera()
    {
        if (mainCamera == null) yield break;

        Vector3 originalPos = mainCamera.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            // คำนวณ offset แบบสุ่ม
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;

            mainCamera.transform.localPosition = originalPos + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // คืนตำแหน่งเดิม
        mainCamera.transform.localPosition = originalPos;
        Debug.Log("?? Camera Shake เสร็จสิ้น");
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