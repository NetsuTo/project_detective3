using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// โซนสไลด์ - ผู้เล่นจะสไลด์ทันทีเมื่อโซนโผล่ขึ้นมา
/// </summary>
public class SlideZone : MonoBehaviour
{
    [Header("Slide Settings")]
    [SerializeField] private float slideSpeed = 10f;
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Animation")]
    [SerializeField] private string slideAnimationTrigger = "Slide";

    [Header("Audio")]
    [SerializeField] private AudioClip slideSound;
    [SerializeField, Range(0f, 1f)] private float slideSoundVolume = 0.7f;

    [Header("Zone Settings")]
    [SerializeField] private bool startVisible = true;

    private HashSet<PlayerController> playersInZone = new HashSet<PlayerController>(); // เก็บผู้เล่นที่อยู่ในโซน
    private Dictionary<PlayerController, Coroutine> activeSlides = new Dictionary<PlayerController, Coroutine>();
    private Collider zoneCollider;
    private bool isZoneActive = true;

    private void Start()
    {
        zoneCollider = GetComponent<Collider>();
        if (zoneCollider != null)
        {
            zoneCollider.isTrigger = true;
        }

        isZoneActive = startVisible;
        gameObject.SetActive(startVisible);
        Debug.Log($"?? SlideZone เริ่มต้น: {(startVisible ? "แสดง ???" : "ซ่อน ??")}");
    }

    private void OnEnable()
    {
        // เมื่อโซนถูกเปิด (โผล่)
        isZoneActive = true;
        Debug.Log("? SlideZone โผล่! กำลังตรวจสอบผู้เล่นในโซน...");

        // เริ่มสไลด์ทันทีสำหรับผู้เล่นที่อยู่ในโซนอยู่แล้ว
        foreach (var player in playersInZone)
        {
            if (player != null)
            {
                StartSlideForPlayer(player);
            }
        }
    }

    private void OnDisable()
    {
        // เมื่อโซนถูกปิด (ซ่อน)
        isZoneActive = false;
        Debug.Log("?? SlideZone ซ่อน! หยุดการสไลด์ทั้งหมด + ล้างรายชื่อผู้เล่น");

        // หยุดการสไลด์ทั้งหมด
        StopAllSlides();

        // ล้างรายชื่อผู้เล่นออกทั้งหมด (เพื่อไม่ให้สไลด์ตอนโผล่ใหม่)
        playersInZone.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                playersInZone.Add(player);
                Debug.Log($"?? ผู้เล่นเข้าโซนสไลด์ (โซน: {(isZoneActive ? "เปิด" : "ปิด")}) - จำนวนผู้เล่น: {playersInZone.Count}");

                // ถ้าโซนเปิดอยู่ ให้เริ่มสไลด์ทันที
                if (isZoneActive)
                {
                    StartSlideForPlayer(player);
                }
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // ตรวจสอบว่าผู้เล่นยังคงกดปุ่มเคลื่อนที่อยู่หรือไม่
        if (isZoneActive && other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && playersInZone.Contains(player))
            {
                float horizontal = Input.GetAxisRaw("Horizontal");
                float vertical = Input.GetAxisRaw("Vertical");

                // ถ้ากำลังกดปุ่มเคลื่อนที่และยังไม่ได้สไลด์ ให้เริ่มสไลด์
                if ((Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f) &&
                    !activeSlides.ContainsKey(player))
                {
                    StartSlideForPlayer(player);
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && playersInZone.Contains(player))
            {
                playersInZone.Remove(player);
                StopSlideForPlayer(player);
                Debug.Log("?? ผู้เล่นออกจากโซนสไลด์");
            }
        }
    }

    private void StartSlideForPlayer(PlayerController player)
    {
        if (player == null || activeSlides.ContainsKey(player)) return;

        Debug.Log($"?? เริ่มสไลด์สำหรับผู้เล่น!");
        Coroutine slideCoroutine = StartCoroutine(PerformSlide(player));
        activeSlides[player] = slideCoroutine;
    }

    private void StopSlideForPlayer(PlayerController player)
    {
        if (player == null || !activeSlides.ContainsKey(player)) return;

        StopCoroutine(activeSlides[player]);
        activeSlides.Remove(player);
    }

    private void StopAllSlides()
    {
        foreach (var kvp in activeSlides)
        {
            if (kvp.Value != null)
            {
                StopCoroutine(kvp.Value);
            }
        }
        activeSlides.Clear();
    }

    private IEnumerator PerformSlide(PlayerController player)
    {
        if (player == null) yield break;

        // คำนวณทิศทางจากการหันของผู้เล่น
        Vector3 slideDirection = player.transform.forward;
        slideDirection.y = 0;
        slideDirection.Normalize();

        // เล่นอนิเมชั่น
        Animator animator = player.GetComponent<Animator>();
        if (animator != null && !string.IsNullOrEmpty(slideAnimationTrigger))
        {
            animator.SetTrigger(slideAnimationTrigger);
        }

        // เล่นเสียง
        PlaySlideSound();

        // ทำการสไลด์
        CharacterController controller = player.GetComponent<CharacterController>();
        float slideTime = 0.3f;
        float elapsed = 0f;

        while (elapsed < slideTime)
        {
            if (player == null || !isZoneActive) break;

            elapsed += Time.deltaTime;
            float t = slideCurve.Evaluate(elapsed / slideTime);

            Vector3 moveVector = slideDirection * slideSpeed * t * Time.deltaTime;

            if (controller != null)
            {
                controller.Move(moveVector);
            }
            else
            {
                player.transform.position += moveVector;
            }

            yield return null;
        }

        // คูลดาวน์สั้นๆ
        yield return new WaitForSeconds(0.1f);

        // ลบออกจาก dictionary เมื่อเสร็จสิ้น
        if (player != null && activeSlides.ContainsKey(player))
        {
            activeSlides.Remove(player);
        }
    }

    private void PlaySlideSound()
    {
        if (slideSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(slideSound, slideSoundVolume);
        }
    }

    private void OnDestroy()
    {
        StopAllSlides();
        playersInZone.Clear();
    }
}