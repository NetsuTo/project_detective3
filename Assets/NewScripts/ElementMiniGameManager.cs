using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ElementMiniGameManager : MonoBehaviour
{
    [Header("UI ของ MiniGame ใน Zone นี้")]
    public Text displayText;                 // ถ้าไม่มีรูป ใช้ Text แสดงแทน
    public Image displayImage;               // รูปสำหรับแสดงปุ่ม
    public GameObject failSymbol;            // ไอคอน Fail
    public float failSymbolDuration = 5f;    // เวลาที่ Fail symbol โผล่แล้วหาย

    [Header("Optional: Sequence เริ่มต้น (fallback ถ้าไม่ได้ส่งจาก TargetZone)")]
    public List<KeyCode> inspectorSequence = new List<KeyCode>();

    [Header("Key → Sprite Mapping (ตั้งค่าใน Inspector)")]
    public List<KeySpritePair> keySpriteMappings = new List<KeySpritePair>();

    [Header("Events (ตั้งใน Inspector)")]
    public UnityEvent onSuccessEvent;
    public UnityEvent onFailEvent;

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
        // เตรียม dictionary สำหรับ map key -> sprite
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
    }

    void Update()
    {
        if (!isActive) return;
        if (currentSequence == null || currentSequence.Count == 0) return;

        if (Input.anyKeyDown)
        {
            if (Input.GetKeyDown(currentSequence[currentIndex]))
            {
                currentIndex++;
                UpdateDisplay();

                if (currentIndex >= currentSequence.Count)
                    Success();
            }
            else
            {
                Fail();
            }
        }
    }

    public void StartMiniGame(List<KeyCode> sequence, Action<bool> callback)
    {
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

    private void UpdateDisplay()
    {
        if (currentIndex >= currentSequence.Count)
        {
            HideDisplay();
            return;
        }

        KeyCode key = currentSequence[currentIndex];

        // ถ้ามีรูป
        if (displayImage != null && keyToSprite.ContainsKey(key) && keyToSprite[key] != null)
        {
            displayImage.sprite = keyToSprite[key];
            displayImage.gameObject.SetActive(true);
            if (displayText != null) displayText.gameObject.SetActive(false);
        }
        else
        {
            // ไม่มีรูป → ใช้ text
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

    private void Success()
    {
        isActive = false;
        HideDisplay();
        onSuccessEvent?.Invoke();
        onCompleteCallback?.Invoke(true);
        onCompleteCallback = null;
    }

    private void Fail()
    {
        isActive = false;
        HideDisplay();
        ShowFailSymbolSafe();
        onFailEvent?.Invoke();
        onCompleteCallback?.Invoke(false);
        onCompleteCallback = null;
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