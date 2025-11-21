using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

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

    // หนังสือ
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

    // เอฟเฟกต์สำเร็จ
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
    private AudioSource audioSource;
    private Vector3 originalImageScale;

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
            originalImageScale = displayImage.transform.localScale;
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
        if (!isActive || activeMiniGame != this || isRetrying)
            return;

        if (currentSequence == null || currentSequence.Count == 0) return;

        if (Input.anyKeyDown)
        {
            if (Input.GetKeyDown(currentSequence[currentIndex]))
            {
                if (keyPressSound != null)
                {
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlaySFX(keyPressSound, PressVolume);
                    else if (audioSource != null)
                        audioSource.PlayOneShot(keyPressSound, PressVolume);
                }

                currentIndex++;
                UpdateDisplay();

                if (currentIndex >= currentSequence.Count)
                    Success();
            }
            else
            {
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

    public void StartMiniGame(List<KeyCode> sequence, Action<bool> callback)
    {
        if (activeMiniGame != null && activeMiniGame != this)
            activeMiniGame.ForceStop();

        activeMiniGame = this;

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

            // ลองใช้ Animation Component ก่อน (ง่ายกว่า)
            if (bookAnimation != null && !string.IsNullOrEmpty(openAnimationClip))
            {
                bookAnimation.Play(openAnimationClip);
                Debug.Log($"📖 [Animation] เล่น: {openAnimationClip}");
            }
            // ถ้าไม่มี Animation ให้ใช้ Animator
            else if (bookAnimator != null && !string.IsNullOrEmpty(openAnimationTrigger))
            {
                bookAnimator.SetTrigger(openAnimationTrigger);
                Debug.Log($"📖 [Animator] เล่นอนิเมชั่นเปิดหนังสือ: {openAnimationTrigger}");
                Debug.Log($"📖 Animator enabled? {bookAnimator.enabled}, Has parameter? {HasParameter(bookAnimator, openAnimationTrigger)}");
            }
            else
            {
                Debug.LogWarning("📖 ไม่มี Animation/Animator หรือ Clip/Trigger! หนังสือจะแสดงแบบธรรมดา");
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

        if (failSymbol != null) failSymbol.SetActive(false);
        UpdateDisplay();

        StartCoroutine(DelayInputActivation());
        Debug.Log($"[MiniGame] StartMiniGame - seq: {SeqToString(currentSequence)}");
    }

    public void ForceStop()
    {
        if (!isActive) return;

        isActive = false;
        onCompleteCallback = null;

        HideDisplay();
        StopAllCoroutines();

        // ปิดหนังสือทันที (ไม่เล่นอนิเมชั่น)
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
        HideDisplay();

        onSuccessEvent?.Invoke();
        onCompleteCallback?.Invoke(true);
        onCompleteCallback = null;

        // 📖 ปิดหนังสือพร้อมอนิเมชั่น แล้วค่อยปลดล็อคและเล่นเอฟเฟกต์
        StartCoroutine(CloseBookAndFinish(true));

        Debug.Log($"✅ MiniGame Success ({name})");
    }

    private void Fail()
    {
        isActive = false;
        activeMiniGame = null;
        HideDisplay();
        ShowFailSymbolSafe();

        onFailEvent?.Invoke();
        onCompleteCallback?.Invoke(false);
        onCompleteCallback = null;

        // 📖 ปิดหนังสือพร้อมอนิเมชั่น แล้วค่อยปลดล็อค
        StartCoroutine(CloseBookAndFinish(false));

        Debug.Log($"💥 MiniGame Failed ({name})");
    }

    /// <summary>
    /// 📖 เล่นอนิเมชั่นปิดหนังสือ รอให้เล่นจบ แล้วค่อยซ่อนและปลดล็อคผู้เล่น
    /// </summary>
    private IEnumerator CloseBookAndFinish(bool isSuccess)
    {
        // เล่นอนิเมชั่นปิดหนังสือ
        if (bookAnimation != null && !string.IsNullOrEmpty(closeAnimationClip))
        {
            bookAnimation.Play(closeAnimationClip);
            Debug.Log($"📖 [Animation] เล่น: {closeAnimationClip}");
        }
        else if (bookAnimator != null && !string.IsNullOrEmpty(closeAnimationTrigger))
        {
            bookAnimator.SetTrigger(closeAnimationTrigger);
            Debug.Log($"📖 [Animator] เล่นอนิเมชั่นปิดหนังสือ: {closeAnimationTrigger}");
            Debug.Log($"📖 Animator State: {bookAnimator.GetCurrentAnimatorStateInfo(0).IsName("Book_Close")}");
        }
        else
        {
            Debug.LogWarning("📖 ไม่มี Animation/Animator สำหรับปิดหนังสือ");
        }

        // รอให้อนิเมชั่นเล่นจบ
        yield return new WaitForSeconds(closeAnimationDuration);

        // ซ่อนหนังสือ
        if (bookModel != null)
        {
            bookModel.SetActive(false);
            Debug.Log("📖 ซ่อนหนังสือแล้ว");
        }

        // ปลดล็อคผู้เล่น
        if (playerController != null)
        {
            playerController.StopCastingAnimation();
            playerController.UnlockMovement();
        }

        // ถ้าสำเร็จ ให้เล่นเอฟเฟกต์
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
        failSymbol.SetActive(true);
        yield return new WaitForSeconds(failSymbolDuration);
        failSymbol.SetActive(false);
    }

    private string SeqToString(List<KeyCode> seq)
    {
        if (seq == null || seq.Count == 0) return "";
        return string.Join("", seq);
    }

    // 🔍 Helper function: เช็คว่า Animator มี Parameter หรือไม่
    private bool HasParameter(Animator anim, string paramName)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }
}