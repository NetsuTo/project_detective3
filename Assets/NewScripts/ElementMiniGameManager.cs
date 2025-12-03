using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class ElementMiniGameManager : MonoBehaviour
{
    // ระบบกลาง: ป้องกันมินิเกมชนกัน
    public static ElementMiniGameManager activeMiniGame;

    [Header("UI ของ MiniGame ใน Zone นี้")]
    public Text displayText;
    public Image displayImage;
    public GameObject failSymbol;
    public float failSymbolDuration = 2f;

    [Header("⭐ Arrow Display Settings")]
    [Tooltip("ขนาดของรูปลูกศร (1 = ขนาดปกติ, 0.5 = เล็กลง 50%)")]
    [Range(0.1f, 2f)]
    public float arrowScale = 0.6f;

    [Header("🎮 Button Press Feedback Settings")]
    [Tooltip("ระยะเวลาที่จะกดลง")]
    public float pressDuration = 0.1f;
    [Tooltip("ขนาดที่จะหดลงตอนกด (0.8 = เล็กลง 20%)")]
    [Range(0.5f, 0.95f)]
    public float pressScale = 0.85f;
    [Tooltip("ระยะเวลาที่จะเด้งกลับ")]
    public float popDuration = 0.2f;
    [Tooltip("ระยะเวลาที่ลูกศรใหม่จะ Fade In")]
    public float fadeInDuration = 0.15f;
    [Tooltip("สีที่จะแฟลช (แนะนำสีเขียว)")]
    public Color flashColor = Color.green;
    [Tooltip("ความเข้มของ Flash (0-1)")]
    [Range(0f, 1f)]
    public float flashIntensity = 0.7f;

    [Header("Optional: Sequence เริ่มต้น (fallback ถ้าไม่ได้ส่งจาก TargetZone)")]
    public List<KeyCode> inspectorSequence = new List<KeyCode>();

    [Header("Key → Sprite Mapping (ตั้งค่าใน Inspector)")]
    public List<KeySpritePair> keySpriteMappings = new List<KeySpritePair>();

    [Header("Events (ตั้งใน Inspector)")]
    public UnityEvent onSuccessEvent;
    public UnityEvent onFailEvent;

    [Header("Player References")]
    public PlayerController playerController;
    public float successSymbolDuration = 3f;

    [Header("เสียงตอนกดคีย์")]
    public AudioClip keyPressSound;
    public AudioClip keyFailSound;
    [Range(0f, 1f)] public float PressVolume = 0.5f;
    [Range(0f, 1f)] public float FailVolume = 0.5f;

    [Header("📖 Book Model Settings")]
    public GameObject bookModel;
    [Tooltip("Animator ของหนังสือ (ถ้ามี) - ถ้าไม่ใส่จะใช้ Simple Animation")]
    public Animator bookAnimator;
    [Tooltip("Animation ของหนังสือ (ใช้แทน Animator ถ้าไม่มี Animator)")]
    public Animation bookAnimation;
    [Tooltip("ชื่อ Animation Clip สำหรับเปิดหนังสือ")]
    public string openAnimationClip = "Book_Open";
    [Tooltip("ชื่อ Animation Clip สำหรับปิดหนังสือ")]
    public string closeAnimationClip = "Book_Close";
    [Tooltip("ชื่อ Trigger สำหรับ Animator (ถ้าใช้ Animator)")]
    public string openAnimationTrigger = "Open";
    [Tooltip("ชื่อ Trigger สำหรับ Animator (ถ้าใช้ Animator)")]
    public string closeAnimationTrigger = "Close";
    [Tooltip("เวลารอให้อนิเมชั่นปิดหนังสือเล่นจบ")]
    public float closeAnimationDuration = 1f;

    [Header("Success Effect Settings")]
    public ParticleSystem successEffect;
    public Transform handEffectSpawnPoint;
    public float effectDelay = 0.5f;
    [Tooltip("เสียงที่เล่นตอนปล่อยสกิลสำเร็จ")]
    public AudioClip successSkillSound;
    [Range(0f, 1f)] public float successSkillVolume = 0.8f;

    [Header("⭐ Retry Settings")]
    public bool allowRetry = true;
    public float retryDelay = 0.5f;

    private Dictionary<KeyCode, Sprite> keyToSprite = new Dictionary<KeyCode, Sprite>();
    private List<KeyCode> currentSequence = new List<KeyCode>();
    private int currentIndex = 0;
    private bool isActive = false;
    private Action<bool> onCompleteCallback = null;
    private bool isRetrying = false;
    private bool isCancelling = false; // ✅ เพิ่มตัวนี้
    private AudioSource audioSource;
    private Vector3 originalImageScale;
    private Vector3 originalImagePosition;
    private Color originalImageColor;

    // ✅ เพิ่มตัวแปรสำหรับคืนสกิล
    private TargetZone callingZone = null;

    // ===== Input System - Gamepad Support =====
    private bool[] keyWasPressed = new bool[4]; // สำหรับ Up, Down, Left, Right

    [Serializable]
    public class KeySpritePair
    {
        public KeyCode key;
        public Sprite sprite;
    }

    void Awake()
    {
        keyToSprite.Clear();
        foreach (var pair in keySpriteMappings)
        {
            if (!keyToSprite.ContainsKey(pair.key))
                keyToSprite.Add(pair.key, pair.sprite);
        }

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        if (displayImage != null)
        {
            originalImageScale = displayImage.transform.localScale;
            originalImagePosition = displayImage.transform.localPosition;
            originalImageColor = displayImage.color;
        }

        Debug.Log("✅ ElementMiniGameManager - Keyboard + Gamepad Ready!");
    }

    void Start()
    {
        if (displayText != null) displayText.gameObject.SetActive(false);
        if (displayImage != null) displayImage.gameObject.SetActive(false);
        if (failSymbol != null) failSymbol.SetActive(false);
        if (bookModel != null)
            bookModel.SetActive(false);
    }

    void Update()
    {
        if (!isActive || activeMiniGame != this || isRetrying || isCancelling) // ✅ เพิ่ม isCancelling
            return;

        // ===== 🆕 ระบบ ESC Cancel + คืนสกิล =====
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CancelAndReturnSkill();
            return;
        }

        // Gamepad: กด Select/Back เพื่อ Cancel
        if (Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame)
        {
            CancelAndReturnSkill();
            return;
        }

        // Fallback: Old Input System
        if (Keyboard.current == null && Gamepad.current == null)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelAndReturnSkill();
                return;
            }
        }

        if (currentSequence == null || currentSequence.Count == 0) return;

        // ===== ตรวจจับ Input ทั้ง Keyboard + Gamepad =====
        KeyCode pressedKey = GetPressedDirectionKey();

        if (pressedKey != KeyCode.None)
        {
            if (pressedKey == currentSequence[currentIndex])
            {
                // ✅ กดถูก
                if (keyPressSound != null)
                {
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlaySFX(keyPressSound, PressVolume);
                    else if (audioSource != null)
                        audioSource.PlayOneShot(keyPressSound, PressVolume);
                }

                // 🎮 กดลง + เด้งขึ้น + ลูกศรใหม่ Fade In
                StartCoroutine(ButtonPressEffect());

                currentIndex++;

                if (currentIndex >= currentSequence.Count)
                    Success();
            }
            else
            {
                // ❌ กดผิด
                if (keyFailSound != null)
                {
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlaySFX(keyFailSound, FailVolume);
                    else if (audioSource != null)
                        audioSource.PlayOneShot(keyFailSound, FailVolume);
                }

                if (allowRetry)
                    Retry();
                else
                    Fail();
            }
        }
    }

    /// <summary>
    /// 🆕 Cancel MiniGame + คืนสกิลให้ผู้เล่น
    /// </summary>
    private void CancelAndReturnSkill()
    {
        if (!isActive) return;

        Debug.Log("❌ Cancel MiniGame ด้วย ESC → คืนสกิล");

        // ✅ ตั้ง flag ให้ Coroutine อื่นหยุดทำงาน
        isCancelling = true;

        // ✅ บล็อค Pause Menu ทันที
        PauseMenuWithVolume.blockPauseTemporarily = true;

        // ✅ บังคับซ่อน failSymbol ก่อนหยุด Coroutine
        if (failSymbol != null)
            failSymbol.SetActive(false);

        // ✅ หยุดทุก Coroutine
        StopAllCoroutines();

        // ✅ บังคับซ่อน UI ทั้งหมดทันที
        HideDisplay();

        if (displayImage != null)
        {
            displayImage.transform.localScale = originalImageScale;
            displayImage.transform.localPosition = originalImagePosition;
            displayImage.color = originalImageColor;
            displayImage.gameObject.SetActive(false);
        }

        if (displayText != null)
        {
            displayText.gameObject.SetActive(false);
        }

        // ✅ ใช้ Coroutine เพื่อรอ frames ก่อนปลดล็อค
        StartCoroutine(CancelSequence());
    }

    private IEnumerator CancelSequence()
    {
        // ✅ คืนสกิลให้ TargetZone
        if (callingZone != null)
        {
            callingZone.ReturnLastUsedSkill();
        }

        // ❌ ไม่เรียก onFailEvent (เพราะเป็น Cancel ไม่ใช่ Fail)
        onCompleteCallback?.Invoke(false);
        onCompleteCallback = null;
        callingZone = null;

        // ปิดหนังสือ
        if (bookAnimation != null && !string.IsNullOrEmpty(closeAnimationClip))
            bookAnimation.Play(closeAnimationClip);
        else if (bookAnimator != null && !string.IsNullOrEmpty(closeAnimationTrigger))
            bookAnimator.SetTrigger(closeAnimationTrigger);

        yield return new WaitForSeconds(closeAnimationDuration);

        if (bookModel != null)
            bookModel.SetActive(false);

        if (playerController != null)
        {
            playerController.StopCastingAnimation();
            playerController.UnlockMovement();
        }

        // ✅ รอให้ ESC ถูกปล่อย (2 frames)
        yield return null;
        yield return null;

        // ✅ ตอนนี้ค่อยปิด MiniGame และปลดบล็อค Pause
        isActive = false;
        activeMiniGame = null;
        isCancelling = false; // ✅ รีเซ็ต flag
        PauseMenuWithVolume.blockPauseTemporarily = false;

        Debug.Log("📖 Cancel เสร็จแล้ว - ปลดบล็อค Pause แล้ว");
    }

    /// <summary>
    /// 🎮 เอฟเฟกต์เหมือนกดปุ่ม: กดลง → เด้งกลับ → ลูกศรใหม่ Fade In
    /// </summary>
    private IEnumerator ButtonPressEffect()
    {
        if (displayImage == null) yield break;

        // ✅ เช็คก่อนเริ่ม
        if (isCancelling) yield break;

        float elapsed = 0f;
        Vector3 startScale = displayImage.transform.localScale;
        Vector3 pressedScale = startScale * pressScale;

        // ===== ขั้นที่ 1: กดลง (Press Down) =====
        while (elapsed < pressDuration)
        {
            // ✅ เช็คทุก frame
            if (isCancelling) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / pressDuration;

            // EaseOut สำหรับการกดลง
            float smoothT = 1f - Mathf.Pow(1f - t, 3f);

            displayImage.transform.localScale = Vector3.Lerp(startScale, pressedScale, smoothT);

            // แฟลชสีเขียวตอนกด
            displayImage.color = Color.Lerp(originalImageColor, flashColor, smoothT * flashIntensity);

            yield return null;
        }

        // ===== ขั้นที่ 2: เด้งกลับ (Pop Back) =====
        elapsed = 0f;
        while (elapsed < popDuration)
        {
            // ✅ เช็คทุก frame
            if (isCancelling) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / popDuration;

            // Smooth EaseOut สำหรับการเด้ง
            float smoothT = Mathf.Sin(t * Mathf.PI * 0.5f);

            displayImage.transform.localScale = Vector3.Lerp(pressedScale, startScale, smoothT);
            displayImage.color = Color.Lerp(flashColor, originalImageColor, smoothT);

            yield return null;
        }

        // ✅ เช็คก่อนรีเซ็ต
        if (isCancelling) yield break;

        // รีเซ็ตค่า
        displayImage.transform.localScale = startScale;
        displayImage.color = originalImageColor;

        // ===== ขั้นที่ 3: ซ่อนลูกศรเดิม =====
        displayImage.gameObject.SetActive(false);

        yield return new WaitForSeconds(0.05f);

        // ✅ เช็คก่อนแสดงลูกศรใหม่
        if (isCancelling) yield break;

        // ===== ขั้นที่ 4: แสดงลูกศรใหม่ + Fade In =====
        if (currentIndex < currentSequence.Count)
        {
            UpdateDisplay();

            // เริ่มจาก Alpha = 0
            Color startColor = displayImage.color;
            startColor.a = 0f;
            displayImage.color = startColor;

            // Fade In
            elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                // ✅ เช็คทุก frame
                if (isCancelling) yield break;

                elapsed += Time.deltaTime;
                float t = elapsed / fadeInDuration;

                Color newColor = originalImageColor;
                newColor.a = Mathf.Lerp(0f, 1f, t);
                displayImage.color = newColor;

                yield return null;
            }

            // ✅ เช็คก่อนตั้งค่าสุดท้าย
            if (isCancelling) yield break;

            // ตั้งค่าสุดท้าย
            displayImage.color = originalImageColor;
        }
    }

    /// <summary>
    /// 🎮 ตรวจจับปุ่มทิศทางจากทั้ง Keyboard และ Gamepad
    /// </summary>
    private KeyCode GetPressedDirectionKey()
    {
        // ===== Keyboard Input (Arrow Keys + WASD) =====
        if (Keyboard.current != null)
        {
            // Arrow Keys
            if (Keyboard.current.upArrowKey.wasPressedThisFrame)
                return KeyCode.UpArrow;
            if (Keyboard.current.downArrowKey.wasPressedThisFrame)
                return KeyCode.DownArrow;
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
                return KeyCode.LeftArrow;
            if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
                return KeyCode.RightArrow;

            // WASD Keys
            if (Keyboard.current.wKey.wasPressedThisFrame)
                return KeyCode.UpArrow;
            if (Keyboard.current.sKey.wasPressedThisFrame)
                return KeyCode.DownArrow;
            if (Keyboard.current.aKey.wasPressedThisFrame)
                return KeyCode.LeftArrow;
            if (Keyboard.current.dKey.wasPressedThisFrame)
                return KeyCode.RightArrow;
        }

        // ===== Gamepad Input (D-Pad + Left Stick) =====
        if (Gamepad.current != null)
        {
            Vector2 dpad = Gamepad.current.dpad.ReadValue();
            Vector2 stick = Gamepad.current.leftStick.ReadValue();

            // รวมค่า D-Pad และ Left Stick
            Vector2 combined = dpad + stick;

            // ตรวจจับทิศทางแบบ Dead Zone (0.5)
            float deadZone = 0.5f;

            // Up
            if (combined.y > deadZone && !keyWasPressed[0])
            {
                keyWasPressed[0] = true;
                keyWasPressed[1] = false;
                keyWasPressed[2] = false;
                keyWasPressed[3] = false;
                return KeyCode.UpArrow;
            }
            // Down
            else if (combined.y < -deadZone && !keyWasPressed[1])
            {
                keyWasPressed[0] = false;
                keyWasPressed[1] = true;
                keyWasPressed[2] = false;
                keyWasPressed[3] = false;
                return KeyCode.DownArrow;
            }
            // Left
            else if (combined.x < -deadZone && !keyWasPressed[2])
            {
                keyWasPressed[0] = false;
                keyWasPressed[1] = false;
                keyWasPressed[2] = true;
                keyWasPressed[3] = false;
                return KeyCode.LeftArrow;
            }
            // Right
            else if (combined.x > deadZone && !keyWasPressed[3])
            {
                keyWasPressed[0] = false;
                keyWasPressed[1] = false;
                keyWasPressed[2] = false;
                keyWasPressed[3] = true;
                return KeyCode.RightArrow;
            }

            // Reset flags เมื่อไม่กดทิศทางใดๆ
            if (Mathf.Abs(combined.x) < deadZone && Mathf.Abs(combined.y) < deadZone)
            {
                keyWasPressed[0] = false;
                keyWasPressed[1] = false;
                keyWasPressed[2] = false;
                keyWasPressed[3] = false;
            }
        }

        // ===== Fallback: Old Input System =====
        if (Keyboard.current == null && Gamepad.current == null)
        {
            // Arrow Keys
            if (Input.GetKeyDown(KeyCode.UpArrow)) return KeyCode.UpArrow;
            if (Input.GetKeyDown(KeyCode.DownArrow)) return KeyCode.DownArrow;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) return KeyCode.LeftArrow;
            if (Input.GetKeyDown(KeyCode.RightArrow)) return KeyCode.RightArrow;

            // WASD Keys
            if (Input.GetKeyDown(KeyCode.W)) return KeyCode.UpArrow;
            if (Input.GetKeyDown(KeyCode.S)) return KeyCode.DownArrow;
            if (Input.GetKeyDown(KeyCode.A)) return KeyCode.LeftArrow;
            if (Input.GetKeyDown(KeyCode.D)) return KeyCode.RightArrow;
        }

        return KeyCode.None;
    }

    // ✅ แก้ไขให้รับ TargetZone เพื่อคืนสกิล
    public void StartMiniGame(List<KeyCode> sequence, Action<bool> callback, TargetZone zone = null)
    {
        if (activeMiniGame != null && activeMiniGame != this)
            activeMiniGame.ForceStop();

        activeMiniGame = this;
        callingZone = zone; // ✅ เก็บไว้เพื่อคืนสกิล
        isCancelling = false; // ✅ รีเซ็ต flag

        if (playerController != null)
        {
            playerController.LockMovement();
            playerController.HideSuccessSymbol();
            playerController.PlayCastingAnimation();
        }

        // 📖 แสดงหนังสือและเล่นอนิเมชั่นเปิด
        if (bookModel != null)
        {
            bookModel.SetActive(true);
            Debug.Log($"📖 เปิดหนังสือ: {bookModel.name}, Active = {bookModel.activeSelf}");

            if (bookAnimation != null && !string.IsNullOrEmpty(openAnimationClip))
            {
                bookAnimation.Play(openAnimationClip);
                Debug.Log($"📖 [Animation] เล่น: {openAnimationClip}");
            }
            else if (bookAnimator != null && !string.IsNullOrEmpty(openAnimationTrigger))
            {
                bookAnimator.SetTrigger(openAnimationTrigger);
                Debug.Log($"📖 [Animator] เล่นอนิเมชั่นเปิดหนังสือ: {openAnimationTrigger}");
            }
        }

        if (sequence == null || sequence.Count == 0)
        {
            if (inspectorSequence != null && inspectorSequence.Count > 0)
                currentSequence = new List<KeyCode>(inspectorSequence);
            else
            {
                Debug.LogWarning("[MiniGame] ไม่มี Sequence ให้เล่น!");
                if (playerController != null)
                    playerController.UnlockMovement();
                callback?.Invoke(false);
                return;
            }
        }
        else
        {
            currentSequence = new List<KeyCode>(sequence);
        }

        onCompleteCallback = callback;
        currentIndex = 0;
        isActive = true;
        isRetrying = false;

        // Reset input flags
        for (int i = 0; i < keyWasPressed.Length; i++)
            keyWasPressed[i] = false;

        if (failSymbol != null) failSymbol.SetActive(false);
        UpdateDisplay();
        StartCoroutine(DelayInputActivation());

        Debug.Log($"🎮 [MiniGame] เริ่มเกม - Sequence: {SeqToString(currentSequence)} (กด ESC เพื่อยกเลิก)");
    }

    public void ForceStop()
    {
        if (!isActive) return;

        isActive = false;
        isCancelling = false;
        onCompleteCallback = null;
        callingZone = null;
        HideDisplay();
        StopAllCoroutines();

        if (bookModel != null)
            bookModel.SetActive(false);

        if (playerController != null)
        {
            playerController.UnlockMovement();
            playerController.StopCastingAnimation();
        }

        currentIndex = 0;
        currentSequence.Clear();

        Debug.Log($"[MiniGame] ForceStop called on {name}");
    }

    private IEnumerator DelayInputActivation()
    {
        bool prev = isActive;
        isActive = false;
        yield return null;
        isActive = prev;
    }

    private void Success()
    {
        isActive = false;
        activeMiniGame = null;
        callingZone = null; // ✅ ไม่คืนสกิลเพราะสำเร็จแล้ว
        HideDisplay();

        onSuccessEvent?.Invoke();
        onCompleteCallback?.Invoke(true);
        onCompleteCallback = null;

        StartCoroutine(CloseBookAndFinish(true));

        Debug.Log($"✅ MiniGame Success ({name})");
    }

    private void Fail()
    {
        isActive = false;
        activeMiniGame = null;
        callingZone = null; // ✅ ไม่คืนสกิลเพราะล้มเหลว
        HideDisplay();
        ShowFailSymbolSafe();

        onFailEvent?.Invoke();
        onCompleteCallback?.Invoke(false);
        onCompleteCallback = null;

        StartCoroutine(CloseBookAndFinish(false));

        Debug.Log($"💥 MiniGame Failed ({name})");
    }

    private IEnumerator CloseBookAndFinish(bool isSuccess)
    {
        if (bookAnimation != null && !string.IsNullOrEmpty(closeAnimationClip))
        {
            bookAnimation.Play(closeAnimationClip);
            Debug.Log($"📖 [Animation] เล่น: {closeAnimationClip}");
        }
        else if (bookAnimator != null && !string.IsNullOrEmpty(closeAnimationTrigger))
        {
            bookAnimator.SetTrigger(closeAnimationTrigger);
            Debug.Log($"📖 [Animator] เล่นอนิเมชั่นปิดหนังสือ: {closeAnimationTrigger}");
        }

        yield return new WaitForSeconds(closeAnimationDuration);

        if (bookModel != null)
        {
            bookModel.SetActive(false);
            Debug.Log("📖 ซ่อนหนังสือแล้ว");
        }

        if (playerController != null)
        {
            playerController.StopCastingAnimation();
            playerController.UnlockMovement();
        }

        if (isSuccess)
        {
            StartCoroutine(PlaySuccessEffectSequence());
        }
    }

    private IEnumerator PlaySuccessEffectSequence()
    {
        yield return new WaitForSeconds(effectDelay);

        if (successEffect != null)
        {
            Vector3 spawnPos = handEffectSpawnPoint != null
                ? handEffectSpawnPoint.position
                : transform.position + Vector3.up;

            Quaternion spawnRot = handEffectSpawnPoint != null
                ? handEffectSpawnPoint.rotation
                : Quaternion.identity;

            ParticleSystem effect = Instantiate(successEffect, spawnPos, spawnRot);
            Destroy(effect.gameObject, 5f);

            Debug.Log($"🎆 Effect spawned at {spawnPos}");
        }

        if (successSkillSound != null)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(successSkillSound, successSkillVolume);
            else if (audioSource != null)
                audioSource.PlayOneShot(successSkillSound, successSkillVolume);
        }
    }

    private void Retry()
    {
        if (isCancelling) return; // ✅ ป้องกันไม่ให้เรียกตอน Cancel

        Debug.Log($"🔄 กดผิด! รีเซ็ตลำดับ...");
        ShowFailSymbolSafe();
        StartCoroutine(RetrySequence());
    }

    private IEnumerator RetrySequence()
    {
        isRetrying = true;
        yield return new WaitForSeconds(retryDelay);
        currentIndex = 0;
        isRetrying = false;

        // Reset input flags
        for (int i = 0; i < keyWasPressed.Length; i++)
            keyWasPressed[i] = false;

        UpdateDisplay();
        Debug.Log($"🔄 เริ่มใหม่! คีย์ที่ต้องกด: {currentSequence[currentIndex]}");
    }

    private void UpdateDisplay()
    {
        if (currentIndex >= currentSequence.Count)
        {
            HideDisplay();
            return;
        }

        KeyCode key = currentSequence[currentIndex];

        if (displayImage != null && keyToSprite.ContainsKey(key) && keyToSprite[key] != null)
        {
            displayImage.sprite = keyToSprite[key];
            displayImage.transform.localScale = originalImageScale * arrowScale;
            displayImage.transform.localPosition = originalImagePosition;
            displayImage.color = originalImageColor;
            displayImage.gameObject.SetActive(true);
            if (displayText != null) displayText.gameObject.SetActive(false);
        }
        else
        {
            if (displayText != null)
            {
                displayText.text = "Next: " + key.ToString();
                displayText.gameObject.SetActive(true);
            }
            if (displayImage != null) displayImage.gameObject.SetActive(false);
        }
    }

    private void HideDisplay()
    {
        if (displayText != null) displayText.gameObject.SetActive(false);
        if (displayImage != null) displayImage.gameObject.SetActive(false);
    }

    public void ShowFailSymbolSafe()
    {
        if (failSymbol == null) return;
        StopCoroutine(nameof(ShowFailSymbolCoroutine));
        StartCoroutine(ShowFailSymbolCoroutine());
    }

    private IEnumerator ShowFailSymbolCoroutine()
    {
        if (isCancelling) yield break; // ✅ เช็คก่อนเริ่ม

        failSymbol.SetActive(true);

        float elapsed = 0f;
        while (elapsed < failSymbolDuration)
        {
            if (isCancelling) // ✅ เช็คทุก frame
            {
                failSymbol.SetActive(false); // ซ่อนทันที
                yield break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        failSymbol.SetActive(false);
    }

    private string SeqToString(List<KeyCode> seq)
    {
        if (seq == null || seq.Count == 0) return "";
        return string.Join("", seq);
    }

    /// <summary>
    /// ✅ ตรวจสอบว่า MiniGame กำลังเปิดอยู่หรือไม่
    /// </summary>
    public bool IsMiniGameActive()
    {
        return isActive;
    }

    /// <summary>
    /// 🔄 คืนสกิลกลับเข้า Inventory (เรียกจาก TargetZone)
    /// </summary>
    public void RestoreSkillFromBottle(List<string> sequence)
    {
        if (sequence == null || sequence.Count == 0)
        {
            Debug.LogWarning("⚠️ Sequence ว่างเปล่า");
            return;
        }

        // แปลง string sequence เป็น KeyCode
        List<KeyCode> keySequence = new List<KeyCode>();
        foreach (string s in sequence)
        {
            if (System.Enum.TryParse(s, out KeyCode key))
            {
                keySequence.Add(key);
            }
        }

        if (keySequence.Count == 0)
        {
            Debug.LogWarning("⚠️ ไม่สามารถแปลง sequence เป็น KeyCode ได้");
            return;
        }

        // เติมกลับเข้า currentSequence
        currentSequence.Clear();
        currentSequence.AddRange(keySequence);
        currentIndex = 0;

        Debug.Log($"🔄 คืนสกิล: {string.Join("-", sequence)} | KeyCode: {SeqToString(currentSequence)}");

        // อัพเดท UI
        if (isActive)
        {
            UpdateDisplay();
        }
    }
}