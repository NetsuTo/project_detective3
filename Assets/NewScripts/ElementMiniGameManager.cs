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
    public float failSymbolDuration = 2f; // ลดเวลาให้เร็วขึ้น

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
    private AudioSource sfxSource;
    [Range(0f, 1f)] public float PressVolume = 0.5f;
    [Range(0f, 1f)] public float FailVolume = 0.5f;

    // หนังสือ
    [Header("Book Model Settings")]
    [Tooltip("โมเดลหนังสือที่จะเปิดตอนเริ่มมินิเกม (วางไว้ในตัวผู้เล่น)")]
    public GameObject bookModel;

    // เอฟเฟกต์สำเร็จ
    [Header("Success Effect Settings")]
    [Tooltip("Effect ที่จะเล่นเมื่อทำมินิเกมสำเร็จ (แต่ละ Zone ใส่ Effect ต่างกัน)")]
    public ParticleSystem successEffect;

    [Tooltip("ตำแหน่งมือที่จะ Spawn Effect (เช่น Hand_R หรือ Hand_L)")]
    public Transform handEffectSpawnPoint;

    [Tooltip("ดีเลย์ก่อนเล่น Effect (รอให้ Animation เล่นถึงจังหวะที่ต้องการ)")]
    public float effectDelay = 0.5f;

    [Tooltip("เสียงที่เล่นตอนปล่อยสกิลสำเร็จ")]
    public AudioClip successSkillSound;
    [Range(0f, 1f)] public float successSkillVolume = 0.8f;

    [Header("⭐ Retry Settings")]
    [Tooltip("อนุญาตให้ลองใหม่ได้เมื่อกดพลาด")]
    public bool allowRetry = true;

    [Tooltip("ระยะเวลาหน่วงก่อนเริ่มใหม่ (วินาที)")]
    public float retryDelay = 0.5f;

    private Dictionary<KeyCode, Sprite> keyToSprite = new Dictionary<KeyCode, Sprite>();
    private List<KeyCode> currentSequence = new List<KeyCode>();
    private int currentIndex = 0;
    private bool isActive = false;
    private Action<bool> onCompleteCallback = null;
    private bool isRetrying = false; // ป้องกันการกดซ้ำตอน retry

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
    }

    void Start()
    {
        if (displayText != null) displayText.gameObject.SetActive(false);
        if (displayImage != null) displayImage.gameObject.SetActive(false);
        if (failSymbol != null) failSymbol.SetActive(false);

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        if (bookModel != null)
            bookModel.SetActive(false);
    }

    void Update()
    {
        // ทำงานเฉพาะมินิเกมที่ active อยู่เท่านั้น
        if (!isActive || activeMiniGame != this || isRetrying)
            return;

        if (currentSequence == null || currentSequence.Count == 0) return;

        if (Input.anyKeyDown)
        {
            if (Input.GetKeyDown(currentSequence[currentIndex]))
            {
                if (keyPressSound != null)
                    sfxSource.PlayOneShot(keyPressSound, PressVolume);

                currentIndex++;
                UpdateDisplay();

                if (currentIndex >= currentSequence.Count)
                    Success();
            }
            else
            {
                if (keyFailSound != null)
                    sfxSource.PlayOneShot(keyFailSound, FailVolume);

                if (allowRetry)
                    Retry(); // ลองใหม่แทนที่จะ Fail
                else
                    Fail(); // Fail แบบเดิม
            }
        }
    }

    public void StartMiniGame(List<KeyCode> sequence, Action<bool> callback)
    {
        // ปิดมินิเกมเก่า (ถ้ามี)
        if (activeMiniGame != null && activeMiniGame != this)
            activeMiniGame.ForceStop();

        activeMiniGame = this;

        if (playerController != null)
        {
            playerController.HideSuccessSymbol();
            playerController.PlayCastingAnimation();
        }

        if (bookModel != null)
            bookModel.SetActive(true);

        if (sequence == null || sequence.Count == 0)
        {
            if (inspectorSequence != null && inspectorSequence.Count > 0)
                currentSequence = new List<KeyCode>(inspectorSequence);
            else
            {
                Debug.LogWarning("[MiniGame] ไม่มี Sequence ให้เล่น!");
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

        // ป้องกัน fail ทันทีจากปุ่ม R
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

        if (sfxSource != null)
            sfxSource.Stop();

        if (bookModel != null)
            bookModel.SetActive(false);

        currentIndex = 0;
        currentSequence.Clear();

        Debug.Log($"[MiniGame] ForceStop called on {name}");
    }

    private IEnumerator DelayInputActivation()
    {
        bool prev = isActive;
        isActive = false;
        yield return null; // skip frame ปุ่ม R
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

        // เล่น effect + sound ของคุณ
        StartCoroutine(PlaySuccessEffectSequence());

        if (playerController != null)
            playerController.StopCastingAnimation();

        if (bookModel != null)
            bookModel.SetActive(false);

        Debug.Log($"✅ MiniGame Success ({name})");
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

        if (successSkillSound != null && sfxSource != null)
            sfxSource.PlayOneShot(successSkillSound, successSkillVolume);
    }

    // ⭐ ฟังก์ชันใหม่: Retry แทน Fail
    private void Retry()
    {
        Debug.Log($"🔄 กดผิด! รีเซ็ตลำดับ...");

        // แสดงสัญลักษณ์ Fail ชั่วคราว
        ShowFailSymbolSafe();

        // รีเซ็ตลำดับกลับไปเริ่มต้น
        StartCoroutine(RetrySequence());
    }

    private IEnumerator RetrySequence()
    {
        isRetrying = true;

        // รอสักครู่
        yield return new WaitForSeconds(retryDelay);

        // รีเซ็ตกลับไปเริ่มต้น
        currentIndex = 0;
        isRetrying = false;

        // แสดงคีย์แรกใหม่
        UpdateDisplay();

        Debug.Log($"🔄 เริ่มใหม่! คีย์ที่ต้องกด: {currentSequence[currentIndex]}");
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

        if (bookModel != null)
            bookModel.SetActive(false);

        Debug.Log($"💥 MiniGame Failed ({name})");
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
}