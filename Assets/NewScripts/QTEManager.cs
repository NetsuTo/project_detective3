using System;
using System.Collections.Generic;
using Unity.VisualScripting;
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
    public LetterIconDatabase iconDB;   // ใส่ ScriptableObject A-Z ใน Inspector

    void Start()
    {
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
    }

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

        // คำนวณตำแหน่ง
        if (slotUIs.Count > 0)
        {
            RectTransform lastRT = slotUIs[slotUIs.Count - 1].GetComponent<RectTransform>();
            startPos = lastRT.anchoredPosition + new Vector2(slotSpacing, 0f);
        }

        // สร้าง slot
        GameObject slot = Instantiate(qteSlotPrefab, qteParent);
        slot.transform.localScale = Vector3.one;

        RectTransform rt = slot.GetComponent<RectTransform>();
        rt.anchoredPosition = startPos;

        // เซ็ตสีพื้นหลังของ slot
        Image bg = slot.GetComponent<Image>();
        if (bg != null)
            bg.color = Color.white;

        // เซ็ตภาพตัวอักษรผ่าน QTESlotUI
        QTESlotUI ui = slot.GetComponent<QTESlotUI>();
        if (ui != null)
        {
            ui.iconDB = iconDB;       // ส่งฐานข้อมูลรูปตัวอักษร
            ui.Init(key, timePerSlot); // เซ็ต sprite + timer
        }

        slot.SetActive(false); // ซ่อนก่อนจนกว่าจะกดถูกจังหวะ

        slotUIs.Add(slot);
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

        // ✅ เล่นเสียงตามผล
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
                slotUIs[currentIndex].SetActive(true);

            SkillLetterSelector selector = FindObjectOfType<SkillLetterSelector>();
            if (selector != null)
            {
                selector.RemoveOneLetterUI();
            }

            currentIndex++;

            if (currentIndex >= sequence.Count)
            {
                Debug.Log("All QTE Success!");

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

        PlayerSkillManager manager = FindObjectOfType<PlayerSkillManager>();
        if (manager != null)
            manager.UnlockSelectedSkill(); // ✅ ปลดล็อกเมื่อจบ QTE

        if (currentTimingBar != null)
            Destroy(currentTimingBar.gameObject);

        currentTimingBar = null;

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
