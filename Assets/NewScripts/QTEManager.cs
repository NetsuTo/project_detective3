using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QTEManager : MonoBehaviour
{
    [Header("UI")]
    public Transform qteParent;           // Panel ใน Canvas
    public GameObject qteSlotPrefab;      // Prefab QTE Slot (Text + Image)
    public float slotSpacing = 60f;

    [Header("Timing Bar")]
    public GameObject timingBarPrefab;    // Prefab TimingBar
    private TimingBar currentTimingBar;

    [Header("Settings")]
    public float timePerSlot = 1f;

    private List<KeyCode> sequence = new List<KeyCode>();
    private List<GameObject> slotUIs = new List<GameObject>();
    private int currentIndex = 0;
    private bool isActive = false;

    // 🔹 เริ่ม QTE
    public void StartQTE(List<KeyCode> keySequence)
    {
        if (keySequence == null || keySequence.Count == 0)
            return;

        // ⛔ ตรวจสอบว่ามีขวดแล้วหรือยัง
        SkillInventory inv = FindObjectOfType<SkillInventory>();
        if (inv != null && inv.HasAnyBottle())
        {
            Debug.Log("⛔ ไม่สามารถ Mix ได้ เพราะยังมีขวดใน Inventory อยู่แล้ว");
            return;
        }

        // 🔹 รีเซ็ตสถานะก่อนเริ่มใหม่ (กันค้าง)
        EndQTE();

        isActive = true;

        foreach (var key in keySequence)
        {
            SpawnQTESlot(key);
        }

        // spawn TimingBar ตัวแรก
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

        Text slotText = slot.GetComponentInChildren<Text>();
        if (slotText != null)
            slotText.text = key.ToString();

        Image img = slot.GetComponent<Image>();
        if (img != null)
            img.color = Color.white;

        slot.SetActive(false); // ซ่อน slot ก่อน
        slotUIs.Add(slot);     // Add ลง list

        sequence.Add(key);
    }

    void SpawnTimingBar()
    {
        if (currentTimingBar != null)
            Destroy(currentTimingBar.gameObject);

        GameObject barGO = Instantiate(timingBarPrefab, qteParent.root); // Spawn บน Canvas
        currentTimingBar = barGO.GetComponent<TimingBar>();
        currentTimingBar.StartTiming(OnTimingComplete);
    }

    void OnTimingComplete(bool success)
    {
        if (!isActive) return;

        if (success)
        {
            if (currentIndex < slotUIs.Count)
                slotUIs[currentIndex].SetActive(true);

            // แจ้งให้ SkillLetterSelector ลบตัวอักษรด้านบนหัว
            SkillLetterSelector selector = FindObjectOfType<SkillLetterSelector>();
            if (selector != null)
            {
                selector.RemoveOneLetterUI();
            }

            currentIndex++;

            if (currentIndex >= sequence.Count)
            {
                Debug.Log("All QTE Success!");

                // ✅ ลบสกิลจาก SkillBar
                SkillBarUI skillBar = FindObjectOfType<SkillBarUI>();
                if (skillBar != null)
                {
                    skillBar.ConsumeSelectedSkill();
                }

                // ✅ เพิ่มไปยัง SkillInventory (ขวด)
                SkillInventory inv = FindObjectOfType<SkillInventory>();
                if (inv != null)
                {
                    inv.AddMixedSkill(new List<KeyCode>(sequence));
                }

                EndQTE();
            }
            else
            {
                SpawnTimingBar();
            }
        }
        else
        {
            Debug.Log("QTE Failed!");
            EndQTE();
        }
    }

    void EndQTE()
    {
        isActive = false;

        if (currentTimingBar != null)
            Destroy(currentTimingBar.gameObject);

        currentTimingBar = null;

        // ลบ QTEslot ทุกตัวออกจากจอ
        foreach (var slot in slotUIs)
        {
            if (slot != null)
                Destroy(slot);
        }

        slotUIs.Clear();
        sequence.Clear();
        currentIndex = 0;

        Debug.Log("QTE Ended → พร้อมเริ่มใหม่ถ้าไม่มีขวดใน Inventory");
    }

}
