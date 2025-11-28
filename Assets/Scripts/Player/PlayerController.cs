using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float acceleration = 10f;

    [Header("Jump / Gravity")]
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public float fallMultiplier = 2.5f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundMask;

    [Header("Visual Effects")]
    public GameObject jumpDustEffectPrefab;
    public Transform effectSpawnPoint;

    [Header("Use Zone")]
    public UseZone currentUseZone;
    private readonly HashSet<UseZone> zonesIn = new HashSet<UseZone>();

    [Header("Success Symbol")]
    public GameObject successSymbol;

    [Header("Sound Effects")]
    public AudioClip[] footstepSounds;
    public AudioClip jumpSound;
    public AudioClip landSound;
    [Range(0f, 1f)] public float footstepVolume = 0.5f;
    [Range(0f, 1f)] public float jumpVolume = 0.7f;
    [Range(0f, 1f)] public float landVolume = 0.6f;
    public float footstepInterval = 0.4f;

    private AudioSource audioSource;
    private float footstepTimer = 0f;

    private CharacterController controller;
    private Animator animator;
    private Vector3 velocity;
    private float currentSpeed;
    private bool isGrounded;
    private bool wasGroundedLastFrame;
    private bool isPickingUp = false;
    private Action pickupCallback;

    // ===== Input System - รองรับทั้ง Keyboard + Gamepad =====
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction interactAction;

    // ===== Movement Lock System =====
    private bool isMovementLocked = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        // ===== สร้าง Input Actions รองรับทั้ง Keyboard + Gamepad =====

        // Move - รองรับ A/D และ Left Stick
        moveAction = new InputAction("Move", type: InputActionType.Value);
        moveAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/a")
            .With("Positive", "<Keyboard>/d");
        moveAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Gamepad>/leftStick/left")
            .With("Positive", "<Gamepad>/leftStick/right");
        moveAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Gamepad>/dpad/left")
            .With("Positive", "<Gamepad>/dpad/right");

        // Jump - รองรับ Space และ Button South (A/Cross)
        jumpAction = new InputAction("Jump", type: InputActionType.Button);
        jumpAction.AddBinding("<Keyboard>/space");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");  // Xbox: A, PS: Cross

        // Interact - รองรับ E และ Button North (Y/Triangle)
        interactAction = new InputAction("Interact", type: InputActionType.Button);
        interactAction.AddBinding("<Keyboard>/e");
        interactAction.AddBinding("<Gamepad>/buttonNorth");  // Xbox: Y, PS: Triangle

        // Enable Actions
        moveAction.Enable();
        jumpAction.Enable();
        interactAction.Enable();

        // Subscribe to Interact
        interactAction.performed += OnInteractPerformed;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        ScanZonesAtStart();

        Debug.Log("✅ PlayerController Started - Keyboard + Gamepad Ready!");
        Debug.Log("📋 Controls: Move (A/D or Left Stick), Jump (Space or A), Interact (E or Y)");
    }

    private void OnEnable()
    {
        moveAction?.Enable();
        jumpAction?.Enable();
        interactAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        jumpAction?.Disable();
        interactAction?.Disable();
    }

    private void OnDestroy()
    {
        if (interactAction != null)
        {
            interactAction.performed -= OnInteractPerformed;
        }

        moveAction?.Dispose();
        jumpAction?.Dispose();
        interactAction?.Dispose();
    }

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        TryUseSelectedInZone();
    }

    private void ScanZonesAtStart()
    {
        zonesIn.Clear();

        var allZones = FindObjectsOfType<UseZone>(true);
        Vector3 p = transform.position;
        foreach (var z in allZones)
        {
            var col = z.GetComponent<Collider>();
            if (col == null) continue;

            if (col.bounds.Contains(p))
                zonesIn.Add(z);
        }

        RecomputeCurrentZone();
    }

    void Update()
    {
        wasGroundedLastFrame = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        // ===== อ่าน Input ทุกเฟรม =====
        float moveInput = 0f;
        bool jumpInput = false;

        if (!isMovementLocked)
        {
            // อ่านค่าการเคลื่อนที่ (รองรับทั้ง Keyboard + Gamepad)
            moveInput = moveAction.ReadValue<float>();

            // อ่านค่ากระโดด
            jumpInput = jumpAction.WasPressedThisFrame();
        }

        // ===== Movement System =====
        if (!isMovementLocked)
        {
            float targetSpeed = moveInput * moveSpeed;
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * acceleration);

            Vector3 move = new Vector3(currentSpeed, 0f, 0f);
            controller.Move(move * Time.deltaTime);

            animator.SetFloat("Speed", Mathf.Abs(currentSpeed));

            // เสียงเดิน
            HandleFootstepSounds();

            // --- หันตัว ---
            if (moveInput > 0.05f)
                transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            else if (moveInput < -0.05f)
                transform.rotation = Quaternion.Euler(0f, -90f, 0f);

            // --- กระโดด ---
            if (jumpInput && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                animator.SetTrigger("Jump");
                PlayJumpSound();
                PlayJumpEffect();
            }
        }
        else
        {
            // ===== ถ้าล็อคอยู่: หยุดการเคลื่อนที่ =====
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.deltaTime * acceleration);
            animator.SetFloat("Speed", 0f);
            footstepTimer = 0f;
        }

        // แรงโน้มถ่วง
        if (velocity.y < 0)
            velocity.y += gravity * fallMultiplier * Time.deltaTime;
        else
            velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);

        // ลงพื้น
        if (isGrounded && !wasGroundedLastFrame)
        {
            animator.SetTrigger("Land");
            PlayLandSound();
        }
    }

    // ===== Movement Lock Methods =====
    public void LockMovement()
    {
        isMovementLocked = true;
        Debug.Log("🔒 ล็อคการเคลื่อนที่ของผู้เล่น");
    }

    public void UnlockMovement()
    {
        isMovementLocked = false;
        Debug.Log("🔓 ปลดล็อคการเคลื่อนที่ของผู้เล่น");
    }

    public bool IsMovementLocked()
    {
        return isMovementLocked;
    }

    private void PlayJumpEffect()
    {
        if (jumpDustEffectPrefab != null)
        {
            Vector3 spawnPos = effectSpawnPoint != null ? effectSpawnPoint.position : transform.position;
            GameObject effect = Instantiate(jumpDustEffectPrefab, spawnPos, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }

    private void HandleFootstepSounds()
    {
        if (isGrounded && Mathf.Abs(currentSpeed) > 0.1f)
        {
            footstepTimer += Time.deltaTime;

            if (footstepTimer >= footstepInterval)
            {
                PlayFootstepSound();
                footstepTimer = 0f;
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    private void PlayFootstepSound()
    {
        if (footstepSounds == null || footstepSounds.Length == 0 || audioSource == null)
            return;

        AudioClip clip = footstepSounds[UnityEngine.Random.Range(0, footstepSounds.Length)];

        if (clip != null)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(clip, footstepVolume);
            else
                audioSource.PlayOneShot(clip, footstepVolume);
        }
    }

    private void PlayJumpSound()
    {
        if (jumpSound != null && audioSource != null)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(jumpSound, jumpVolume);
            else
                audioSource.PlayOneShot(jumpSound, jumpVolume);
        }
    }

    private void PlayLandSound()
    {
        if (landSound != null && audioSource != null)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(landSound, landVolume);
            else
                audioSource.PlayOneShot(landSound, landVolume);
        }
    }

    public void PlayPickupAnimation(Action onPickupComplete)
    {
        if (isPickingUp) return;
        isPickingUp = true;

        pickupCallback = onPickupComplete;
        animator.SetTrigger("Pickup");

        Invoke(nameof(CompletePickup), 0.3f);
    }

    private void CompletePickup()
    {
        isPickingUp = false;
        pickupCallback?.Invoke();
        pickupCallback = null;
    }

    private void RecomputeCurrentZone()
    {
        UseZone best = null;
        float bestDist = float.MaxValue;
        int bestPriority = int.MinValue;

        Vector3 p = transform.position;

        foreach (var z in zonesIn)
        {
            if (z == null) continue;
            var col = z.GetComponent<Collider>();
            if (col == null) continue;

            if (z.priority > bestPriority)
            {
                best = z;
                bestPriority = z.priority;
                bestDist = Vector3.SqrMagnitude(col.bounds.ClosestPoint(p) - p);
                continue;
            }
            if (z.priority < bestPriority) continue;

            float d = Vector3.SqrMagnitude(col.bounds.ClosestPoint(p) - p);
            if (d < bestDist)
            {
                best = z;
                bestDist = d;
            }
        }

        currentUseZone = best;
    }

    public void TryUseSelectedInZone()
    {
        if (currentUseZone == null)
        {
            Debug.Log("ยังไม่ได้ยืนอยู่ในโซนใช้งาน");
            return;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out UseZone zone))
        {
            zonesIn.Add(zone);
            RecomputeCurrentZone();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out UseZone zone))
        {
            zonesIn.Remove(zone);
            RecomputeCurrentZone();
        }
    }

    public void PlayCastingAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("IsCasting", true);
            animator.SetTrigger("Cast");
        }
    }

    public void StopCastingAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("IsCasting", false);
        }
    }

    public void ShowSuccessSymbol()
    {
        if (successSymbol == null)
        {
            Debug.LogWarning("⚠️ successSymbol ยังไม่ได้ assign ใน PlayerController");
            return;
        }

        successSymbol.SetActive(true);
        Debug.Log("✨ แสดง Success Symbol บนหัวผู้เล่น");

        DOTween.Kill(successSymbol);

        RectTransform rt = successSymbol.GetComponent<RectTransform>();

        Vector2 originalAnchoredPos2D = Vector2.zero;
        Vector3 originalLocalPos3D = Vector3.zero;
        Vector3 originalScale = successSymbol.transform.localScale;

        if (rt != null)
        {
            originalAnchoredPos2D = rt.anchoredPosition;
        }
        else
        {
            originalLocalPos3D = successSymbol.transform.localPosition;
        }

        CanvasGroup cg = successSymbol.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = successSymbol.AddComponent<CanvasGroup>();
        }
        cg.alpha = 0f;

        float riseAmount = 25f;

        if (rt != null)
        {
            rt.anchoredPosition = originalAnchoredPos2D - new Vector2(0f, riseAmount);
        }
        else
        {
            successSymbol.transform.localPosition = originalLocalPos3D - new Vector3(0f, riseAmount, 0f);
        }

        successSymbol.transform.localScale = originalScale;

        Sequence seq = DOTween.Sequence();

        if (rt != null)
        {
            seq.Append(
                rt.DOAnchorPos(originalAnchoredPos2D, 0.4f)
                  .SetEase(Ease.OutCubic)
            );
        }
        else
        {
            seq.Append(
                successSymbol.transform.DOLocalMove(originalLocalPos3D, 0.4f)
                    .SetEase(Ease.OutCubic)
            );
        }

        seq.Join(
            cg.DOFade(1f, 0.4f)
              .SetEase(Ease.OutCubic)
        );

        seq.Append(
            successSymbol.transform
                .DOScale(originalScale * 1.05f, 0.2f)
                .SetEase(Ease.OutQuad)
        );
        seq.Append(
            successSymbol.transform
                .DOScale(originalScale, 0.15f)
                .SetEase(Ease.InQuad)
        );

        seq.OnComplete(() =>
        {
            cg.alpha = 1f;
            successSymbol.transform.localScale = originalScale;

            if (rt != null)
            {
                rt.anchoredPosition = originalAnchoredPos2D;
            }
            else
            {
                successSymbol.transform.localPosition = originalLocalPos3D;
            }
        });

        seq.SetTarget(successSymbol);
    }

    public void HideSuccessSymbol()
    {
        if (successSymbol != null && successSymbol.activeSelf)
        {
            successSymbol.SetActive(false);
            Debug.Log("💨 ซ่อน Success Symbol บนหัวผู้เล่น");
        }
    }
}