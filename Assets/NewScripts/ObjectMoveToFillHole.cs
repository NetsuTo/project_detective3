using UnityEngine;
using System.Collections;

public class ObjectMoveToFillHole : MonoBehaviour
{
    [Header("วัตถุที่จะเคลื่อน (เช่น ก้อนหิน, แผ่นหิน)")]
    public GameObject objectA;

    [Header("ตำแหน่งหลุมที่จะกลบ")]
    public Transform targetHole;

    [Header("ความเร็วในการเคลื่อน")]
    public float moveSpeed = 2f;

    [Header("ระยะที่จะถือว่าถึงจุดหมายแล้ว")]
    public float arriveDistance = 0.05f;

    [Header("Effect ตอนเคลื่อนที่ (ติดตามวัตถุ)")]
    public ParticleSystem movingEffect;
    [Tooltip("ตำแหน่ง Offset ของเอฟเฟคจากตัววัตถุ")]
    public Vector3 effectOffset = Vector3.zero;

    [Header("Effect ตอนกลบหลุม (แสดงครั้งเดียว)")]
    public ParticleSystem fillEffect;

    [Header("เสียงตอนขยับ (เล่นตลอดตอนเคลื่อนที่)")]
    public AudioClip movingSound;
    [Range(0f, 1f)] public float movingSoundVolume = 0.6f;

    [Header("เสียงตอนวาง (เล่นครั้งเดียวตอนถึง)")]
    public AudioClip impactSound;
    [Range(0f, 1f)] public float impactSoundVolume = 0.8f;

    [Header("Collider ที่จะเปิดหลังกลบหลุม (เช่น ทางเดิน)")]
    public Collider pathColliderToEnable;

    private bool isMoving = false;
    private AudioSource audioSource;
    private ParticleSystem activeMovingEffect; // เอฟเฟคที่กำลังเล่นอยู่

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true; // ตั้งให้เล่นวนตอนขยับ

        // ตรวจสอบว่ามี movingEffect หรือไม่
        if (movingEffect != null)
        {
            movingEffect.Stop();
        }

        if (fillEffect != null)
        {
            fillEffect.Stop();
        }
    }

    /// <summary>
    /// เริ่ม Event การกลบหลุม (เรียกใน onSuccessEvent)
    /// </summary>
    public void StartMoveToHole()
    {
        if (objectA == null || targetHole == null)
        {
            Debug.LogWarning("[ObjectMoveToFillHole] ยังไม่ได้กำหนด objectA หรือ targetHole");
            return;
        }

        if (isMoving) return;

        Debug.Log("[ObjectMoveToFillHole] เริ่มเคลื่อน ObjectA ไปยังหลุม...");
        StartCoroutine(MoveObject());
    }

    private IEnumerator MoveObject()
    {
        isMoving = true;

        // เริ่มเล่นเอฟเฟคตอนเคลื่อนที่
        StartMovingEffect();

        // เริ่มเล่นเสียงตอนขยับ
        StartMovingSound();

        while (isMoving && objectA != null)
        {
            objectA.transform.position = Vector3.MoveTowards(
                objectA.transform.position,
                targetHole.position,
                moveSpeed * Time.deltaTime
            );

            // อัปเดตตำแหน่งเอฟเฟคให้ติดตามวัตถุ
            if (activeMovingEffect != null)
            {
                activeMovingEffect.transform.position = objectA.transform.position + effectOffset;
            }

            if (Vector3.Distance(objectA.transform.position, targetHole.position) <= arriveDistance)
            {
                OnArriveHole();
                yield break;
            }

            yield return null;
        }
    }

    private void StartMovingEffect()
    {
        if (movingEffect == null) return;

        // สร้างเอฟเฟคใหม่ที่ติดกับวัตถุ
        Vector3 spawnPos = objectA.transform.position + effectOffset;
        activeMovingEffect = Instantiate(movingEffect, spawnPos, Quaternion.identity);

        // ทำให้เอฟเฟคเป็นลูกของวัตถุ (จะติดตามอัตโนมัติ)
        // แต่เราจะใช้วิธี manual update ใน MoveObject() แทน เพื่อความแม่นยำ

        activeMovingEffect.Play();
        Debug.Log("[ObjectMoveToFillHole] เริ่มเล่นเอฟเฟคการเคลื่อนที่");
    }

    private void StopMovingEffect()
    {
        if (activeMovingEffect != null)
        {
            activeMovingEffect.Stop();
            Destroy(activeMovingEffect.gameObject, 2f); // ให้เวลา particle ที่เหลืออยู่จางหาย
            activeMovingEffect = null;
            Debug.Log("[ObjectMoveToFillHole] หยุดเอฟเฟคการเคลื่อนที่");
        }
    }

    private void StartMovingSound()
    {
        if (movingSound != null && audioSource != null)
        {
            audioSource.clip = movingSound;
            audioSource.volume = movingSoundVolume;
            audioSource.loop = true;
            audioSource.Play();
            Debug.Log("[ObjectMoveToFillHole] เริ่มเล่นเสียงขยับ");
        }
    }

    private void StopMovingSound()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("[ObjectMoveToFillHole] หยุดเสียงขยับ");
        }
    }

    private void OnArriveHole()
    {
        isMoving = false;
        if (objectA == null) return;

        // หยุดเอฟเฟคการเคลื่อนที่
        StopMovingEffect();

        // หยุดเสียงขยับ
        StopMovingSound();

        // snap ให้ตรงพอดี
        objectA.transform.position = targetHole.position;
        objectA.transform.rotation = targetHole.rotation;

        // เล่น Effect ตอนกลบหลุม
        if (fillEffect != null)
        {
            ParticleSystem fx = Instantiate(fillEffect, targetHole.position, Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject, 3f);
        }

        // เล่นเสียงตอนวาง (PlayOneShot)
        if (impactSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(impactSound, impactSoundVolume);
            Debug.Log("[ObjectMoveToFillHole] เล่นเสียงตอนวาง");
        }

        // เปิดทางเดิน (เช่น สะพาน)
        if (pathColliderToEnable != null)
            pathColliderToEnable.enabled = true;

        Debug.Log("[ObjectMoveToFillHole] กลบหลุมเสร็จแล้ว! ผู้เล่นสามารถเดินต่อได้");
    }

    // เผื่อต้องการหยุดกลางคัน
    void OnDestroy()
    {
        if (activeMovingEffect != null)
        {
            Destroy(activeMovingEffect.gameObject);
        }

        // หยุดเสียงถ้ายังเล่นอยู่
        StopMovingSound();
    }
}