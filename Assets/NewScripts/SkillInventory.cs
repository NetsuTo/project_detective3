using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SkillInventory : MonoBehaviour
{
    public Transform bottleParent;   // จุดวาง UI ใน Canvas
    public GameObject bottlePrefab;  // Prefab icon/slot สำหรับสกิลผสมแล้ว
    public ElementMiniGameManager miniGameManager; // reference ไปยัง MiniGameManager (optional ถ้ามี global)

    private List<List<KeyCode>> storedSkills = new List<List<KeyCode>>();

    void Update()
    {
        // ตรวจว่าผู้เล่นกด R
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (storedSkills.Count > 0)
            {
                Debug.Log("กด R แล้ว! แต่การใช้สกิลจะถูกจัดการผ่าน TargetZone");
            }
            else
            {
                Debug.Log("ไม่มีสกิลในขวดให้ใช้");
            }
        }
    }

    // เพิ่มสกิลใหม่เข้าขวด
    public void AddMixedSkill(List<KeyCode> sequence)
    {
        storedSkills.Add(sequence);

        GameObject go = Instantiate(bottlePrefab, bottleParent);
        Text t = go.GetComponentInChildren<Text>();
        if (t != null)
        {
            t.text = string.Join("", sequence); // เช่น HHO
        }
    }

    // ตรวจว่ามี skill ตรงกับ seq หรือไม่
    public bool HasSkill(List<KeyCode> seq)
    {
        foreach (var s in storedSkills)
        {
            if (SequencesMatch(s, seq)) return true;
        }
        return false;
    }

    // ดึง sequence ที่ตรง
    public List<KeyCode> GetSkillSequence(List<KeyCode> seq)
    {
        foreach (var s in storedSkills)
        {
            if (SequencesMatch(s, seq)) return new List<KeyCode>(s);
        }
        return null;
    }

    // ลบสกิลที่ตรงกับ sequence
    public void ConsumeSkill(List<KeyCode> seq)
    {
        for (int i = 0; i < storedSkills.Count; i++)
        {
            if (SequencesMatch(storedSkills[i], seq))
            {
                storedSkills.RemoveAt(i);
                if (i < bottleParent.childCount)
                {
                    Destroy(bottleParent.GetChild(i).gameObject);
                }
                return;
            }
        }
    }

    // ลบสกิลแรกออก (ใช้ผิด Zone → เสียขวด)
    public void ConsumeFirstSkill()
    {
        if (storedSkills.Count > 0)
        {
            storedSkills.RemoveAt(0);
            if (bottleParent.childCount > 0)
            {
                Destroy(bottleParent.GetChild(0).gameObject);
            }
        }
    }

    // ใช้ตรวจว่าไม่มีสกิลใน inventory
    public bool IsEmpty()
    {
        return storedSkills.Count == 0;
    }

    // ===== Helper =====

    public bool HasAnyBottle()
    {
        return storedSkills.Count > 0;
    }

    private bool SequencesMatch(List<KeyCode> a, List<KeyCode> b)
    {
        if (a == null || b == null) return false;
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i]) return false;
        }
        return true;
    }

    private string SeqToString(List<KeyCode> seq)
    {
        if (seq == null) return "";
        return string.Join("", seq);
    }
}
