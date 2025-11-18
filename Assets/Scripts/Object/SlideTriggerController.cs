using UnityEngine;

/// <summary>
/// ควบคุมการแสดง/ซ่อน SlideZone เมื่อผู้เล่นเดินผ่าน Trigger
/// </summary>
public class SlideTriggerController : MonoBehaviour
{
    [Header("Target Slide Zone")]
    [SerializeField] private GameObject targetSlideZone; // โซนสไลด์ที่จะแสดง/ซ่อน

    [Header("Trigger Behavior")]
    [SerializeField] private TriggerAction actionOnEnter = TriggerAction.ShowZone;

    [Header("Visual Feedback (Optional)")]
    [SerializeField] private ParticleSystem spawnEffect; // เอฟเฟกต์เมื่อโผล่
    [SerializeField] private AudioClip triggerSound;
    [SerializeField, Range(0f, 1f)] private float triggerSoundVolume = 0.5f;

    public enum TriggerAction
    {
        ShowZone,  // แสดงโซนสไลด์ (เปิด)
        HideZone   // ซ่อนโซนสไลด์ (ปิด)
    }

    private Collider triggerCollider;

    private void Start()
    {
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }

        if (targetSlideZone == null)
        {
            Debug.LogError("? ไม่ได้ใส่ Target Slide Zone!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && targetSlideZone != null)
        {
            bool shouldShow = (actionOnEnter == TriggerAction.ShowZone);

            // แสดง/ซ่อน SlideZone
            targetSlideZone.SetActive(shouldShow);

            Debug.Log($"?? Trigger: {gameObject.name} ? {(shouldShow ? "แสดง ???" : "ซ่อน ??")} SlideZone");

            // เล่นเอฟเฟกต์เมื่อโผล่
            if (shouldShow && spawnEffect != null)
            {
                spawnEffect.Play();
            }

            // เล่นเสียง
            PlayTriggerSound();
        }
    }

    private void PlayTriggerSound()
    {
        if (triggerSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(triggerSound, triggerSoundVolume);
        }
    }

    // สำหรับ Debug ใน Editor
    private void OnDrawGizmos()
    {
        if (targetSlideZone != null)
        {
            Gizmos.color = (actionOnEnter == TriggerAction.ShowZone) ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, targetSlideZone.transform.position);

            // วาดไอคอน
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        }
    }
}