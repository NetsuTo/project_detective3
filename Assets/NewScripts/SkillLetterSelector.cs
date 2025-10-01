using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillLetterSelector : MonoBehaviour
{
    public Transform uiAnchor;       // จุดบนหัว Player
    public GameObject letterPrefab;  // Prefab ของ Text UI
    public Canvas mainCanvas;        // Canvas หลัก
    public float cycleSpeed = 2f;    // ความเร็วหมุนตัวอักษร

    private List<char> letters = new List<char>();
    private List<char> remaining = new List<char>();        // สำหรับ cycle
    private List<char> originalLetters = new List<char>();  // เก็บ skillID เดิม

    private int currentIndex = 0;
    private float timer = 0f;
    private bool isActive = false;
    private bool qteStarted = false;

    private List<GameObject> letterUIs = new List<GameObject>();
    private List<Text> letterTexts = new List<Text>();

    private PlayerSkillManager manager;

    void Start()
    {
        manager = GetComponent<PlayerSkillManager>();
    }

    void Update()
    {
        if (!isActive) return;

        // หมุนตัวอักษรไปเรื่อย ๆ
        timer += Time.deltaTime;
        if (timer >= 1f / cycleSpeed && remaining.Count > 0)
        {
            timer = 0f;
            currentIndex = (currentIndex + 1) % remaining.Count;

            for (int i = 0; i < letterTexts.Count; i++)
            {
                letterTexts[i].text = remaining[currentIndex].ToString();
            }
        }

        // เริ่ม QTE ทันทีหลังจากกด O ครั้งแรก
        if (Input.GetKeyDown(KeyCode.O) && remaining.Count > 0)
        {
            if (!qteStarted)
            {
                qteStarted = true;
                StartQTE();
            }
        }

        // อัปเดตตำแหน่ง UI เหนือหัว player
        for (int i = 0; i < letterUIs.Count; i++)
        {
            Vector3 offset = new Vector3(i * 50f, 0f, 0f);
            Vector3 screenPos = Camera.main.WorldToScreenPoint(uiAnchor.position) + offset;
            letterUIs[i].transform.position = screenPos;
        }
    }

    public void StartSelection(string skillID)
    {
        letters = new List<char>(skillID.ToCharArray());
        remaining = new List<char>(letters);        // ใช้สำหรับ cycle อย่างเดียว
        originalLetters = new List<char>(letters);  // เก็บ skillID ที่แท้จริง

        foreach (var ui in letterUIs) Destroy(ui);
        letterUIs.Clear();
        letterTexts.Clear();

        for (int i = 0; i < letters.Count; i++)
        {
            GameObject go = Instantiate(letterPrefab, mainCanvas.transform);
            go.transform.localScale = Vector3.one;

            Text textComp = go.GetComponentInChildren<Text>();
            if (textComp != null)
            {
                textComp.text = letters[i].ToString();
                letterUIs.Add(go);
                letterTexts.Add(textComp);
            }
        }

        currentIndex = 0;
        isActive = true;
        qteStarted = false;
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

            Debug.Log("Final QTE sequence count = " + keySequence.Count);
            qte.StartQTE(keySequence);
        }
    }

    // ✅ ลบ Letter UI ทีละตัวเมื่อกด Space ถูกจังหวะ
    public void RemoveOneLetterUI()
    {
        if (letterUIs.Count == 0) return;

        Destroy(letterUIs[0]);
        letterUIs.RemoveAt(0);
        letterTexts.RemoveAt(0);

        if (letterUIs.Count == 0)
        {
            isActive = false; // หยุด cycle เมื่อไม่มีเหลือแล้วจริง ๆ
        }
    }
}
