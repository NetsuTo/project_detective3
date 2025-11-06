using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ElementMiniGameManager : MonoBehaviour
{
    [Header("UI ของ MiniGame ใน Zone นี้")]
    public Text displayText;
    public Image displayImage;
    public GameObject failSymbol;
    public float failSymbolDuration = 5f;

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

    // 🪶 เพิ่มส่วนหนังสือ
    [Header("Book Model Settings")]
    [Tooltip("โมเดลหนังสือที่จะเปิดตอนเริ่มมินิเกม (วางไว้ในตัวผู้เล่น)")]
    public GameObject bookModel;

    private Dictionary<KeyCode, Sprite> keyToSprite = new Dictionary<KeyCode, Sprite>();
    private List<KeyCode> currentSequence = new List<KeyCode>();
    private int currentIndex = 0;
    private bool isActive = false;
    private Action<bool> onCompleteCallback = null;

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

        // ✅ เพิ่ม AudioSource สำหรับ SFX
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        // 🪶 ปิดโมเดลหนังสือตอนเริ่ม
        if (bookModel != null)
            bookModel.SetActive(false);
    }

    void Update()
    {
        if (!isActive) return;
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

                Fail();
            }
        }
    }

    public void StartMiniGame(List<KeyCode> sequence, Action<bool> callback)
    {
        if (playerController != null)
        {
            playerController.HideSuccessSymbol();
            playerController.PlayCastingAnimation();
        }

        // 🪶 เปิดโมเดลหนังสือตอนเริ่มเล่นมินิเกม
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

        if (failSymbol != null) failSymbol.SetActive(false);
        UpdateDisplay();

        Debug.Log($"[MiniGame] StartMiniGame - seq: {SeqToString(currentSequence)}");
    }

    private void Success()
    {
        isActive = false;
        HideDisplay();
        onSuccessEvent?.Invoke();
        onCompleteCallback?.Invoke(true);
        onCompleteCallback = null;

        if (playerController != null)
            playerController.StopCastingAnimation();

        // 🪶 ปิดโมเดลหนังสือตอนจบมินิเกม
        if (bookModel != null)
            bookModel.SetActive(false);

        Debug.Log("✅ MiniGame Success Completed!");
    }

    private void Fail()
    {
        isActive = false;
        HideDisplay();
        ShowFailSymbolSafe();
        onFailEvent?.Invoke();
        onCompleteCallback?.Invoke(false);
        onCompleteCallback = null;

        // 🪶 ปิดโมเดลหนังสือตอนจบมินิเกม (แม้จะ fail)
        if (bookModel != null)
            bookModel.SetActive(false);
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
        StopAllCoroutines();
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
