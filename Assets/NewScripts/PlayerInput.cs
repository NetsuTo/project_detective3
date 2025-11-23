using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    private PlayerSkillManager manager;

    private void Start()
    {
        manager = GetComponent<PlayerSkillManager>();
    }

    private void Update()
    {
        // เลือก skill ก่อนหน้า
        if (Input.GetKeyDown(KeyCode.Z))
            manager.SelectPrev();

        // เลือก skill ถัดไป
        if (Input.GetKeyDown(KeyCode.C))
            manager.SelectNext();

        // ยืนยัน skill (กด T)
        if (Input.GetKeyDown(KeyCode.T))
        {
            // ? ตรวจสอบว่ามีขวดยาอยู่แล้วหรือไม่
            SkillInventory inv = FindObjectOfType<SkillInventory>();
            if (inv != null && inv.HasAnyBottle())
            {
                Debug.Log("? ไม่สามารถเริ่ม Mix ได้ เพราะยังมีขวดใน Inventory อยู่แล้ว");
                return; // ? หยุดการทำงาน ไม่ให้แสดงตัวอักษรบนหัว
            }

            // ? ถ้าไม่มีขวด ให้ Confirm Skill ได้
            if (manager != null && manager.GetSkills().Count > 0)
            {
                Debug.Log("? กด T ? เริ่ม Mix Skill");
                manager.ConfirmSkill();
            }
            else
            {
                Debug.Log("?? ไม่มี skill ให้ confirm");
            }
        }
    }
}