using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class QTEManager : MonoBehaviour
{
    [Header("UI")]
    public Transform qteParent;
    public GameObject qteSlotPrefab;
    public float slotSpacing = 60f;

    [Header("Timing Bar")]
    public GameObject timingBarPrefab;
    private TimingBar currentTimingBar;

    [Header("⭐ Animation Settings")]
    [Tooltip("เวลาที่ QTE Slot ค่อยๆ โผล่ขึ้นมา")]
    public float slotFadeInDuration = 0.3f;
    [Tooltip("ประเภทอนิเมชั่น: Fade, Scale, Both, Pop")]
    public SlotAnimationType slotAnimationType = SlotAnimationType.Both;

    public enum SlotAnimationType
    {
        None,
        Fade,
        Scale,
        Both,
        Pop
    }

    [Header("Settings")]
    public float timePerSlot = 1f;

    [Header("เสียงประกอบ QTE")]
    public AudioClip keyPressSound;
    public AudioClip keyFailSound;
    private AudioSource sfxSource;
    [Range(0f, 1f)] public float passVolume = 0.5f;
    [Range(0f, 1f)] public float failVolume = 0.5f;

    private List<KeyCode> sequence = new List<KeyCode>();
    private List<GameObject> slotUIs = new List<GameObject>();
    private int currentIndex = 0;
    private bool isActive = false;
    public LetterIconDatabase iconDB;

    void Start()
    {
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
    }

    public void StartQTE(List<KeyCode> keySequence)
    {
        if (keySequence == null || keySequence.Count == 0)
            return;

        SkillInventory inv = FindObjectOfType<SkillInventory>();
        if (inv != null && inv.HasAnyBottle())
        {
            Debug.Log("⛔ ไม่สามารถ Mix ได้ เพราะยังมีขวดใน Inventory อยู่แล้ว");
            return;
        }

        EndQTE();

        isActive = true;

        foreach (var key in keySequence)
        {
            SpawnQTESlot(key);
        }

        if (currentTimingBar == null && slotUIs.Count > 0)
        {
            SpawnTimingBar();
        }
    }

    void SpawnQTESlot(KeyCode key)
    {
        Vector2 startPos = Vector2.zero;

        if (slotUIs.Count > 0)
        {
            RectTransform lastRT = slotUIs[slotUIs.Count - 1].GetComponent<RectTransform>();
            startPos = lastRT.anchoredPosition + new Vector2(slotSpacing, 0f);
        }

        GameObject slot = Instantiate(qteSlotPrefab, qteParent);
        slot.transform.localScale = Vector3.one;

        RectTransform rt = slot.GetComponent<RectTransform>();
        rt.anchoredPosition = startPos;

        Image bg = slot.GetComponent<Image>();
        if (bg != null)
            bg.color = Color.white;

        QTESlotUI ui = slot.GetComponent<QTESlotUI>();
        if (ui != null)
        {
            ui.iconDB = iconDB;
            ui.Init(key, timePerSlot);
        }

        slot.SetActive(false);

        slotUIs.Add(slot);
        sequence.Add(key);
    }

    void SpawnTimingBar()
    {
        if (currentTimingBar != null)
        {
            Debug.LogWarning("⚠️ TimingBar มีอยู่แล้ว ไม่สร้างใหม่");
            return;
        }

        GameObject barGO = Instantiate(timingBarPrefab, qteParent.root);
        currentTimingBar = barGO.GetComponent<TimingBar>();

        // 🎲 เริ่ม TimingBar รอบแรก (จะสุ่ม Target อัตโนมัติ)
        currentTimingBar.StartTiming(OnTimingComplete);

        Debug.Log("🎯 สร้าง TimingBar ใหม่ + สุ่ม Target รอบแรก");
    }

    void OnTimingComplete(bool success)
    {
        if (!isActive) return;

        if (success)
        {
            if (keyPressSound != null)
                sfxSource.PlayOneShot(keyPressSound, passVolume);
        }
        else
        {
            if (keyFailSound != null)
                sfxSource.PlayOneShot(keyFailSound, failVolume);
        }

        if (success)
        {
            if (currentIndex < slotUIs.Count)
            {
                StartCoroutine(ShowSlotWithAnimation(slotUIs[currentIndex]));
            }

            SkillLetterSelector selector = FindObjectOfType<SkillLetterSelector>();
            if (selector != null)
            {
                selector.RemoveOneLetterUI();
            }

            currentIndex++;

            if (currentIndex >= sequence.Count)
            {
                Debug.Log("✅ All QTE Success!");

                SkillBarUI skillBar = FindObjectOfType<SkillBarUI>();
                if (skillBar != null)
                    skillBar.ConsumeSelectedSkill();

                SkillInventory inv = FindObjectOfType<SkillInventory>();
                if (inv != null)
                    inv.AddMixedSkill(new List<KeyCode>(sequence));

                PlayerController player = FindObjectOfType<PlayerController>();
                if (player != null)
                    player.ShowSuccessSymbol();

                EndQTE();
            }
            else
            {
                // 🎲🎲🎲 สำคัญ! เรียก StartTiming() ใหม่ทุกรอบ → Target จะสุ่มตำแหน่งใหม่ทุกครั้ง! 🎲🎲🎲
                if (currentTimingBar != null)
                {
                    Debug.Log($"🎲 ต่อรอบที่ {currentIndex + 1}/{sequence.Count} → สุ่ม Target ใหม่!");
                    currentTimingBar.StartTiming(OnTimingComplete);
                }
            }
        }
        else
        {
            Debug.Log("❌ QTE Failed!");

            // ⭐⭐⭐ เพิ่มส่วนนี้ - เรียก OnQTEFailed() เพื่อลบตัวอักษรบนหัว ⭐⭐⭐
            SkillLetterSelector selector = FindObjectOfType<SkillLetterSelector>();
            if (selector != null)
            {
                selector.OnQTEFailed();
                Debug.Log("✅ เรียก OnQTEFailed() สำเร็จ - ลบตัวอักษรบนหัวแล้ว");
            }
            else
            {
                Debug.LogError("❌ ไม่เจอ SkillLetterSelector!");
            }

            EndQTE();
        }
    }

    void EndQTE()
    {
        isActive = false;

        PlayerSkillManager manager = FindObjectOfType<PlayerSkillManager>();
        if (manager != null)
            manager.UnlockSelectedSkill();

        StopAllCoroutines();

        if (currentTimingBar != null)
        {
            Destroy(currentTimingBar.gameObject);
            currentTimingBar = null;
            Debug.Log("🗑️ ลบ TimingBar");
        }

        foreach (var slot in slotUIs)
        {
            if (slot != null)
                Destroy(slot);
        }

        slotUIs.Clear();
        sequence.Clear();
        currentIndex = 0;

        Debug.Log("🔚 QTE Ended → พร้อมเริ่มใหม่ถ้าไม่มีขวดใน Inventory");
    }

    private IEnumerator ShowSlotWithAnimation(GameObject slot)
    {
        if (slot == null)
        {
            Debug.LogWarning("⚠️ Slot ถูก Destroy ไปแล้วก่อนที่อนิเมชั่นจะเสร็จ");
            yield break;
        }

        slot.SetActive(true);

        if (slotAnimationType == SlotAnimationType.None)
            yield break;

        CanvasGroup canvasGroup = slot.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = slot.AddComponent<CanvasGroup>();

        RectTransform rect = slot.GetComponent<RectTransform>();
        Vector3 originalScale = rect.localScale;

        float elapsed = 0f;

        switch (slotAnimationType)
        {
            case SlotAnimationType.Fade:
                canvasGroup.alpha = 0f;
                break;

            case SlotAnimationType.Scale:
                rect.localScale = Vector3.zero;
                break;

            case SlotAnimationType.Both:
                canvasGroup.alpha = 0f;
                rect.localScale = Vector3.zero;
                break;

            case SlotAnimationType.Pop:
                rect.localScale = Vector3.zero;
                break;
        }

        while (elapsed < slotFadeInDuration)
        {
            if (slot == null || canvasGroup == null || rect == null)
            {
                Debug.LogWarning("⚠️ Slot ถูก Destroy ระหว่างอนิเมชั่น");
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = elapsed / slotFadeInDuration;

            switch (slotAnimationType)
            {
                case SlotAnimationType.Fade:
                    canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                    break;

                case SlotAnimationType.Scale:
                    rect.localScale = Vector3.Lerp(Vector3.zero, originalScale, t);
                    break;

                case SlotAnimationType.Both:
                    canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                    rect.localScale = Vector3.Lerp(Vector3.zero, originalScale, t);
                    break;

                case SlotAnimationType.Pop:
                    float elasticT = Mathf.Sin(t * Mathf.PI * 0.5f);
                    float overshoot = 1f + (Mathf.Sin(t * Mathf.PI * 2f) * 0.2f * (1f - t));
                    rect.localScale = originalScale * elasticT * overshoot;
                    break;
            }

            yield return null;
        }

        if (slot != null && canvasGroup != null && rect != null)
        {
            canvasGroup.alpha = 1f;
            rect.localScale = originalScale;
        }
    }
}