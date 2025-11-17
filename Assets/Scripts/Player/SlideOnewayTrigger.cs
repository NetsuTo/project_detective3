using System.Collections;
using UnityEngine;

public class SlideOnewayTrigger : MonoBehaviour
{
    [Header("Slide Settings")]
    [SerializeField] private float slideDistance = 3f; // ระยะทางที่จะสไลด์
    [SerializeField] private float slideDuration = 0.5f; // เวลาที่ใช้สไลด์
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Animation")]
    [SerializeField] private string slideAnimationTrigger = "Slide"; // ชื่อ Trigger ใน Animator

    [Header("Collider Settings")]
    [SerializeField] private Collider triggerCollider; // Collider สำหรับเข้า
    [SerializeField] private Collider blockCollider; // Collider สำหรับบัง (ไม่ใช่ Trigger!)

    [Header("Audio")]
    [SerializeField] private AudioClip slideSound;
    [SerializeField, Range(0f, 1f)] private float slideSoundVolume = 0.7f;

    private bool hasTriggered = false;
    private bool isSliding = false;

    private void Start()
    {
        // ถ้าไม่ได้กำหนด Trigger Collider ให้ใช้ของ GameObject นี้
        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<Collider>();
        }

        // ตรวจสอบว่าเป็น Trigger
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }

        // ปิด Block Collider ตอนเริ่มต้น (จะเปิดหลังสไลด์)
        if (blockCollider != null)
        {
            blockCollider.enabled = false;
            Debug.Log("?? Block Collider ปิดอยู่ (ยังเดินผ่านได้)");
        }
        else
        {
            Debug.LogWarning("?? ไม่ได้ใส่ Block Collider! ผู้เล่นจะเดินกลับมาได้");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // ตรวจสอบว่าเป็น Player และยังไม่เคย Trigger
        if (!hasTriggered && !isSliding && other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();

            if (player != null)
            {
                hasTriggered = true;

                // ปิด Trigger ทันที
                if (triggerCollider != null)
                {
                    triggerCollider.enabled = false;
                }

                StartCoroutine(SlidePlayer(player));
            }
        }
    }

    private IEnumerator SlidePlayer(PlayerController player)
    {
        isSliding = true;

        // บอกทิศทางการสไลด์จากทิศหันของผู้เล่น
        float direction = Mathf.Sign(player.transform.forward.x);
        if (Mathf.Abs(player.transform.forward.x) < 0.1f)
        {
            // ถ้าหันแนวตั้ง ให้ดูจาก Rotation Y
            direction = player.transform.rotation.eulerAngles.y > 180f ? -1f : 1f;
        }

        Vector3 startPos = player.transform.position;
        Vector3 targetPos = startPos + new Vector3(direction * slideDistance, 0f, 0f);

        // เล่นอนิเมชั่นสไลด์
        Animator animator = player.GetComponent<Animator>();
        if (animator != null && !string.IsNullOrEmpty(slideAnimationTrigger))
        {
            animator.SetTrigger(slideAnimationTrigger);
            Debug.Log("?? เล่นอนิเมชั่น: " + slideAnimationTrigger);
        }

        // เล่นเสียงสไลด์
        PlaySlideSound();

        // ทำการสไลด์
        CharacterController controller = player.GetComponent<CharacterController>();
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideDuration;
            float curveValue = slideCurve.Evaluate(t);

            Vector3 newPos = Vector3.Lerp(startPos, targetPos, curveValue);

            if (controller != null)
            {
                // ใช้ CharacterController.Move
                Vector3 moveVector = newPos - player.transform.position;
                controller.Move(moveVector);
            }
            else
            {
                // ถ้าไม่มี CharacterController ใช้ Transform
                player.transform.position = newPos;
            }

            yield return null;
        }

        // ตรวจสอบให้แน่ใจว่าถึงตำแหน่งเป้าหมาย
        if (controller != null)
        {
            Vector3 finalMove = targetPos - player.transform.position;
            controller.Move(finalMove);
        }
        else
        {
            player.transform.position = targetPos;
        }

        isSliding = false;

        // **เปิด Block Collider เพื่อบังไม่ให้กลับมา**
        if (blockCollider != null)
        {
            blockCollider.enabled = true;
            Debug.Log("?? เปิด Block Collider - ไม่สามารถเดินกลับได้แล้ว!");
        }

        Debug.Log("? สไลด์เสร็จสิ้น");
    }

    private void PlaySlideSound()
    {
        if (slideSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(slideSound, slideSoundVolume);
        }
    }

    // ฟังก์ชันสำหรับ Reset (ใช้สำหรับ Debug หรือ Checkpoint)
    public void ResetTrigger()
    {
        hasTriggered = false;
        isSliding = false;

        if (triggerCollider != null)
        {
            triggerCollider.enabled = true;
        }

        if (blockCollider != null)
        {
            blockCollider.enabled = false;
        }

        Debug.Log("?? Reset Trigger แล้ว");
    }
}