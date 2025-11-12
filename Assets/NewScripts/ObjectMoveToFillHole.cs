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

    [Header("Effect ตอนกลบหลุม (ถ้ามี)")]
    public ParticleSystem fillEffect;

    [Header("เสียงตอนวาง (ถ้ามี)")]
    public AudioClip impactSound;
    [Range(0f, 1f)] public float soundVolume = 0.8f;

    [Header("Collider ที่จะเปิดหลังกลบหลุม (เช่น ทางเดิน)")]
    public Collider pathColliderToEnable;

    private bool isMoving = false;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (fillEffect != null)
            fillEffect.Stop();
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

        while (isMoving && objectA != null)
        {
            objectA.transform.position = Vector3.MoveTowards(
                objectA.transform.position,
                targetHole.position,
                moveSpeed * Time.deltaTime
            );

            if (Vector3.Distance(objectA.transform.position, targetHole.position) <= arriveDistance)
            {
                OnArriveHole();
                yield break;
            }

            yield return null;
        }
    }

    private void OnArriveHole()
    {
        isMoving = false;
        if (objectA == null) return;

        // snap ให้ตรงพอดี
        objectA.transform.position = targetHole.position;
        objectA.transform.rotation = targetHole.rotation;

        // เล่น Effect
        if (fillEffect != null)
        {
            ParticleSystem fx = Instantiate(fillEffect, targetHole.position, Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject, 3f);
        }

        // เล่นเสียง
        if (impactSound != null && audioSource != null)
            audioSource.PlayOneShot(impactSound, soundVolume);

        // เปิดทางเดิน (เช่น สะพาน)
        if (pathColliderToEnable != null)
            pathColliderToEnable.enabled = true;

        Debug.Log("[ObjectMoveToFillHole] กลบหลุมเสร็จแล้ว! ผู้เล่นสามารถเดินต่อได้");
    }
}
