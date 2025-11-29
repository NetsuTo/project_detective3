using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem slideEffect;
    [SerializeField] private Vector3 effectOffset = new Vector3(0, 0.5f, 0);
    [SerializeField] private Vector3 effectRotation = new Vector3(0, 0, 0);
    [SerializeField] private bool followPlayer = true;
    [SerializeField] private bool rotateWithPlayer = false;

    [Header("Zone Settings")]
    [SerializeField] private bool startVisible = true;

    private HashSet<PlayerController> playersInZone = new HashSet<PlayerController>();
    private Dictionary<PlayerController, Coroutine> activeSlides = new Dictionary<PlayerController, Coroutine>();
    private Dictionary<PlayerController, ParticleSystem> playerEffects = new Dictionary<PlayerController, ParticleSystem>();
    private Collider zoneCollider;
    private bool isZoneActive = true;
    private AudioSource audioSource;

    private void Start()
    {
        zoneCollider = GetComponent<Collider>();
        if (zoneCollider != null)
        {
            zoneCollider.isTrigger = true;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            Debug.Log("?? สร้าง AudioSource อัตโนมัติสำหรับ SlideZone");
        }

        isZoneActive = startVisible;
        gameObject.SetActive(startVisible);
        Debug.Log($"?? SlideZone เริ่มต้น: {(startVisible ? "แสดง ?" : "ซ่อน ?")}");
    }

    private void OnEnable()
    {
        isZoneActive = true;
        Debug.Log("?? SlideZone โผล่! กำลังตรวจสอบผู้เล่นในโซน...");

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
        isZoneActive = false;
        Debug.Log("?? SlideZone ซ่อน! หยุดการสไลด์ทั้งหมด + ล้างรายชื่อผู้เล่น");

        StopAllSlides();
        StopAllEffects();
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

                if (isZoneActive)
                {
                    StartEffectForPlayer(player);
                    StartSlideForPlayer(player);
                }
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (isZoneActive && other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && playersInZone.Contains(player))
            {
                // ===== ใช้ Input System แทน Input Manager =====
                float horizontal = 0f;
                float vertical = 0f;

                // อ่านค่าจาก Keyboard
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.aKey.isPressed) horizontal = -1f;
                    else if (Keyboard.current.dKey.isPressed) horizontal = 1f;

                    if (Keyboard.current.wKey.isPressed) vertical = 1f;
                    else if (Keyboard.current.sKey.isPressed) vertical = -1f;
                }

                // อ่านค่าจาก Gamepad (ถ้ามี)
                if (Gamepad.current != null)
                {
                    Vector2 leftStick = Gamepad.current.leftStick.ReadValue();
                    if (Mathf.Abs(leftStick.x) > 0.1f) horizontal = leftStick.x;
                    if (Mathf.Abs(leftStick.y) > 0.1f) vertical = leftStick.y;
                }

                // เช็คว่ามีการกดปุ่มหรือไม่
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
                StopEffectForPlayer(player);
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

    private void StartEffectForPlayer(PlayerController player)
    {
        if (player == null || slideEffect == null) return;
        if (playerEffects.ContainsKey(player)) return;

        Quaternion rotation;
        if (rotateWithPlayer)
        {
            rotation = player.transform.rotation * Quaternion.Euler(effectRotation);
        }
        else
        {
            rotation = Quaternion.Euler(effectRotation);
        }

        ParticleSystem effectInstance = Instantiate(slideEffect, player.transform.position + effectOffset, rotation);

        if (followPlayer)
        {
            effectInstance.transform.SetParent(player.transform);
            effectInstance.transform.localPosition = effectOffset;

            if (rotateWithPlayer)
            {
                effectInstance.transform.localRotation = Quaternion.Euler(effectRotation);
            }
            else
            {
                effectInstance.transform.rotation = rotation;
            }
        }

        effectInstance.Play();
        playerEffects[player] = effectInstance;

        Debug.Log($"? เริ่มเอฟเฟกต์สไลด์สำหรับผู้เล่น (Rotation: {effectRotation})");
    }

    private void StopEffectForPlayer(PlayerController player)
    {
        if (player == null || !playerEffects.ContainsKey(player)) return;

        ParticleSystem effect = playerEffects[player];
        if (effect != null)
        {
            effect.Stop();
            Destroy(effect.gameObject, 2f);
        }

        playerEffects.Remove(player);
        Debug.Log($"?? หยุดเอฟเฟกต์สไลด์สำหรับผู้เล่น");
    }

    private void StopAllEffects()
    {
        foreach (var kvp in playerEffects)
        {
            if (kvp.Value != null)
            {
                kvp.Value.Stop();
                Destroy(kvp.Value.gameObject, 2f);
            }
        }
        playerEffects.Clear();
    }

    private IEnumerator PerformSlide(PlayerController player)
    {
        if (player == null) yield break;

        Vector3 slideDirection = player.transform.forward;
        slideDirection.y = 0;
        slideDirection.Normalize();

        Animator animator = player.GetComponent<Animator>();
        if (animator != null && !string.IsNullOrEmpty(slideAnimationTrigger))
        {
            animator.SetTrigger(slideAnimationTrigger);
        }

        PlaySlideSound();

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

        yield return new WaitForSeconds(0.1f);

        if (player != null && activeSlides.ContainsKey(player))
        {
            activeSlides.Remove(player);
        }
    }

    private void PlaySlideSound()
    {
        if (slideSound == null) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(slideSound, slideSoundVolume);
            Debug.Log("?? เล่นเสียงผ่าน AudioManager");
        }
        else if (audioSource != null)
        {
            audioSource.PlayOneShot(slideSound, slideSoundVolume);
            Debug.Log("?? เล่นเสียงผ่าน AudioSource");
        }
        else
        {
            Debug.LogWarning("?? ไม่พบ AudioManager และ AudioSource!");
        }
    }

    private void OnDestroy()
    {
        StopAllSlides();
        StopAllEffects();
        playersInZone.Clear();
    }
}