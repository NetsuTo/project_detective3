using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SkillBarUI : MonoBehaviour
{
    [Header("Skill Slot Settings")]
    public Transform skillSlotParent;       // ช่องสกิลใน Canvas
    public GameObject skillSlotPrefab;      // Prefab แต่ละช่อง (มี Text/Image)

    [Header("Selection Highlight")]
    public GameObject selectionCirclePrefab; // Prefab วงกลม highlight (เช่นวงสีทอง)
    private GameObject currentSelectionCircle; // ตัววงกลมปัจจุบัน

    private List<GameObject> slots = new List<GameObject>();
    private PlayerSkillManager manager;

    private void Start()
    {
        manager = FindObjectOfType<PlayerSkillManager>();
        manager.onSkillUpdate += UpdateUI;
        UpdateUI();
    }

    private void UpdateUI()
    {
        // ถ้ายังไม่มีสกิลเลย → ซ่อน Panel
        if (manager.GetSkills().Count == 0)
        {
            skillSlotParent.gameObject.SetActive(false);
            return;
        }
        else
        {
            skillSlotParent.gameObject.SetActive(true);
        }

        // ล้างของเก่า
        foreach (var slot in slots)
            Destroy(slot);
        slots.Clear();

        // ลบวงกลมเก่าออก (ถ้ามี)
        if (currentSelectionCircle != null)
            Destroy(currentSelectionCircle);

        List<string> skills = manager.GetSkills();
        for (int i = 0; i < skills.Count; i++)
        {
            GameObject slot = Instantiate(skillSlotPrefab, skillSlotParent);
            slot.GetComponentInChildren<Text>().text = skills[i];

            // ตั้งสีพื้นเป็นขาวทุกช่อง
            Image img = slot.GetComponent<Image>();
            if (img != null) img.color = Color.white;

            // ✅ ถ้าเป็นสกิลที่เลือก — วางวงกลม highlight
            if (i == manager.GetSelectedIndex() && selectionCirclePrefab != null)
            {
                GameObject circle = Instantiate(selectionCirclePrefab, slot.transform);
                circle.transform.SetAsLastSibling(); // ให้อยู่บนสุด
                circle.transform.localPosition = Vector3.zero; // ให้อยู่ตรงกลางช่อง
                circle.transform.localScale = Vector3.one;
                currentSelectionCircle = circle;
            }

            slots.Add(slot);
        }
    }

    public void ConsumeSelectedSkill()
    {
        int selectedIndex = manager.GetSelectedIndex();
        if (selectedIndex >= 0 && selectedIndex < slots.Count)
        {
            // ลบออกจาก PlayerSkillManager ด้วย
            manager.RemoveSkillAt(selectedIndex);

            // อัปเดต UI ใหม่
            UpdateUI();
        }
    }
}
