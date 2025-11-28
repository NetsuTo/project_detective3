using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SkillLetterSelector : MonoBehaviour
{
    public Transform uiAnchor;       // จุดบนหัว Player
    public GameObject letterPrefab;  // Prefab ของ Image UI (SkillLetterUI)
    public Canvas mainCanvas;        // Canvas หลัก
    public float cycleSpeed = 2f;    // ความเร็วหมุนตัวอักษร

    private List<char> letters = new List<char>();
    private List<char> remaining = new List<char>();
    private List<char> originalLetters = new List<char>();

    private int currentIndex = 0;
    private float timer = 0f;
    private bool isActive = false;
    private bool qteStarted = false;

    private List<GameObject> letterUIs = new List<GameObject>();
    private List<SkillLetterUI> letterImages = new List<SkillLetterUI>(); // ใช้รูปภาพแทน Text

    private PlayerSkillManager manager;
    public Vector3[] customOffsets;   // ใส่ใน Inspector
    public float letterSpacing = 100f; // fallback ถ้า customOffsets ไม่พอ

    // ===== Input System - รองรับทั้ง Keyboard + Gamepad =====
    private InputAction mixAction;

    void Start()
    {
        manager = GetComponent<PlayerSkillManager>();

        // ===== สร้าง Input Action สำหรับปุ่ม F และ Gamepad =====
        mixAction = new InputAction("Mix", type: InputActionType.Button);
        mixAction.AddBinding("<Keyboard>/f");              // Keyboard: F
        mixAction.AddBinding("<Gamepad>/buttonWest");      // Xbox: X, PS: Square □

        mixAction.Enable();

        Debug.Log("✅ SkillLetterSelector Started - F (Keyboard) / X/Square (Gamepad) Ready!");
    }

    private void OnEnable()
    {
        mixAction?.Enable();
    }

    private void OnDisable()
    {
        mixAction?.Disable();
    }

    private void OnDestroy()
    {
        mixAction?.Dispose();
    }

    void Update()
    {
        if (!isActive) return;

        // หมุนสลับรูปตัวอักษรเรื่อย ๆ
        timer += Time.deltaTime;
        if (timer >= 1f / cycleSpeed && remaining.Count > 0)
        {
            timer = 0f;
            currentIndex = (currentIndex + 1) % remaining.Count;

            for (int i = 0; i < letterImages.Count; i++)
            {
                letterImages[i].UpdateLetter(remaining[currentIndex]);
            }
        }

        // ===== เริ่ม QTE เมื่อกด F หรือปุ่ม Gamepad =====
        if (mixAction.WasPressedThisFrame() && remaining.Count > 0)
        {
            if (!qteStarted)
            {
                qteStarted = true;
                StartQTE();
                Debug.Log("🎯 กดปุ่ม Mix -> เริ่ม QTE!");
            }
        }

        // อัปเดตตำแหน่ง UI เหนือหัว player
        for (int i = 0; i < letterUIs.Count; i++)
        {
            Vector3 offset;

            // ถ้ามี custom offset สำหรับตัวนี้ → ใช้เลย
            if (customOffsets != null && i < customOffsets.Length)
            {
                offset = customOffsets[i];
            }
            else
            {
                // ถ้าไม่มีก็ fallback ใช้ spacing เดิม
                offset = new Vector3(i * letterSpacing, 0f, 0f);
            }

            Vector3 screenPos = Camera.main.WorldToScreenPoint(uiAnchor.position) + offset;
            letterUIs[i].transform.position = screenPos;
        }
    }

    public void StartSelection(string skillID)
    {
        letters = new List<char>(skillID.ToCharArray());
        remaining = new List<char>(letters);
        originalLetters = new List<char>(letters);

        // ลบ UI เดิม
        foreach (var ui in letterUIs) Destroy(ui);
        letterUIs.Clear();
        letterImages.Clear();

        // สร้าง LetterUI จาก prefab
        for (int i = 0; i < letters.Count; i++)
        {
            GameObject go = Instantiate(letterPrefab, mainCanvas.transform);
            go.transform.localScale = Vector3.one;

            SkillLetterUI ui = go.GetComponent<SkillLetterUI>();
            if (ui != null)
            {
                // ดึง database จาก QTEManager
                QTEManager qte = FindObjectOfType<QTEManager>();
                if (qte != null)
                    ui.iconDB = qte.iconDB;

                ui.SetLetter(letters[i]);

                letterUIs.Add(go);
                letterImages.Add(ui);
            }
            else
            {
                Debug.LogError("SkillLetterUI not found on letterPrefab!");
            }
        }

        currentIndex = 0;
        isActive = true;
        qteStarted = false;

        Debug.Log("🔤 เริ่ม Selection สำหรับ Skill: " + skillID);
    }

    private void StartQTE()
    {
        QTEManager qte = FindObjectOfType<QTEManager>();
        if (qte != null)
        {
            List<KeyCode> keySequence = new List<KeyCode>();
            foreach (char c in originalLetters)
            {
                if (System.Enum.TryParse(c.ToString().ToUpper(), out KeyCode code))
                {
                    keySequence.Add(code);
                }
            }

            Debug.Log("🎮 Final QTE sequence count = " + keySequence.Count);
            qte.StartQTE(keySequence);
        }
    }

    // ลบ Letter UI ทีละตัวเมื่อ QTE สำเร็จ
    public void RemoveOneLetterUI()
    {
        if (letterUIs.Count == 0) return;

        Destroy(letterUIs[0]);
        letterUIs.RemoveAt(0);
        letterImages.RemoveAt(0);

        if (letterUIs.Count == 0)
        {
            isActive = false;
            Debug.Log("✅ QTE เสร็จสิ้น - ทุก Letter UI ถูกลบแล้ว");
        }
    }

    // ===== ฟังก์ชันเสริม: รีเซ็ต QTE =====
    public void ResetQTE()
    {
        // ลบ UI ทั้งหมด
        foreach (var ui in letterUIs)
        {
            if (ui != null) Destroy(ui);
        }

        letterUIs.Clear();
        letterImages.Clear();

        isActive = false;
        qteStarted = false;
        currentIndex = 0;
        timer = 0f;

        Debug.Log("🔄 รีเซ็ต SkillLetterSelector");
    }
}