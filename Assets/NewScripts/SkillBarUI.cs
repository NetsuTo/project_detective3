using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SkillBarUI : MonoBehaviour
{
    [Header("Skill Slot Settings")]
    public Transform skillSlotParent;
    public GameObject skillSlotPrefab;

    [Header("Selection Highlight")]
    public GameObject selectionCirclePrefab;
    private GameObject currentSelectionCircle;

    private List<GameObject> slots = new List<GameObject>();
    private PlayerSkillManager manager;
    private SkillDatabase skillDB;

    private void Start()
    {
        manager = FindObjectOfType<PlayerSkillManager>();
        skillDB = FindObjectOfType<SkillDatabase>(); // ✅ หา SkillDatabase ใน Scene

        if (manager != null)
            manager.onSkillUpdate += UpdateUI;

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (manager == null) return;

        if (manager.GetSkills().Count == 0)
        {
            skillSlotParent.gameObject.SetActive(false);
            return;
        }
        else
        {
            skillSlotParent.gameObject.SetActive(true);
        }

        foreach (var slot in slots) Destroy(slot);
        slots.Clear();

        if (currentSelectionCircle != null) Destroy(currentSelectionCircle);

        List<string> skills = manager.GetSkills();
        for (int i = 0; i < skills.Count; i++)
        {
            GameObject slot = Instantiate(skillSlotPrefab, skillSlotParent);
            SkillData data = (skillDB != null) ? skillDB.GetSkill(skills[i]) : null;
            Image icon = slot.GetComponentInChildren<Image>();

            if (data != null)
            {
                slot.GetComponentInChildren<Text>().text = data.displayName;
                if (icon != null) icon.sprite = data.icon;
            }
            else
            {
                slot.GetComponentInChildren<Text>().text = skills[i];
                if (icon != null) icon.sprite = null;
            }

            if (i == manager.GetSelectedIndex() && selectionCirclePrefab != null)
            {
                GameObject circle = Instantiate(selectionCirclePrefab, slot.transform);
                circle.transform.SetAsLastSibling();
                circle.transform.localPosition = Vector3.zero;
                circle.transform.localScale = Vector3.one;
                currentSelectionCircle = circle;
            }

            slots.Add(slot);
        }
    }

    public void ConsumeSelectedSkill()
    {
        if (manager == null) return;

        int selectedIndex = manager.GetLockedSkillIndex();
        if (selectedIndex == -1)
            selectedIndex = manager.GetSelectedIndex();

        if (selectedIndex >= 0 && selectedIndex < slots.Count)
        {
            manager.RemoveSkillAt(selectedIndex);
            UpdateUI();
        }
    }

}
