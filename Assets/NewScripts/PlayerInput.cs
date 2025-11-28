using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    private PlayerSkillManager manager;

    // ===== Input System Actions =====
    private InputAction selectPrevAction;
    private InputAction selectNextAction;
    private InputAction confirmSkillAction;

    private void Awake()
    {
        manager = GetComponent<PlayerSkillManager>();

        // สร้าง Input Actions
        SetupInputActions();

        // ?? Enable ทันทีใน Awake
        selectPrevAction?.Enable();
        selectNextAction?.Enable();
        confirmSkillAction?.Enable();
    }

    private void SetupInputActions()
    {
        // เลือก Skill ก่อนหน้า (Z)
        selectPrevAction = new InputAction("SelectPrev", binding: "<Keyboard>/z");
        selectPrevAction.performed += OnSelectPrevPerformed;

        // เลือก Skill ถัดไป (C)
        selectNextAction = new InputAction("SelectNext", binding: "<Keyboard>/c");
        selectNextAction.performed += OnSelectNextPerformed;

        // ยืนยัน Skill (T)
        confirmSkillAction = new InputAction("ConfirmSkill", binding: "<Keyboard>/t");
        confirmSkillAction.performed += OnConfirmSkillPerformed;
    }

    // ? อ่าน Input ทุกเฟรมด้วย Update() (วิธีสำรอง)
    private void Update()
    {
        // ถ้า Input System ไม่ทำงาน ให้ใช้ GetKeyDown แทน
        if (Keyboard.current == null)
        {
            // ใช้ Input Manager (Old Input System) แทน
            if (Input.GetKeyDown(KeyCode.Z))
            {
                OnSelectPrevPerformed(default);
            }
            if (Input.GetKeyDown(KeyCode.C))
            {
                OnSelectNextPerformed(default);
            }
            if (Input.GetKeyDown(KeyCode.T))
            {
                OnConfirmSkillPerformed(default);
            }
        }
        else
        {
            // ใช้ New Input System แบบอ่านทุกเฟรม
            if (Keyboard.current.zKey.wasPressedThisFrame)
            {
                OnSelectPrevPerformed(default);
            }
            if (Keyboard.current.cKey.wasPressedThisFrame)
            {
                OnSelectNextPerformed(default);
            }
            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                OnConfirmSkillPerformed(default);
            }
        }
    }

    private void OnEnable()
    {
        selectPrevAction?.Enable();
        selectNextAction?.Enable();
        confirmSkillAction?.Enable();
    }

    private void OnDisable()
    {
        selectPrevAction?.Disable();
        selectNextAction?.Disable();
        confirmSkillAction?.Disable();
    }

    // ===== Input Callbacks =====
    private void OnSelectPrevPerformed(InputAction.CallbackContext ctx)
    {
        if (manager != null)
        {
            Debug.Log("?? กด Z - เลือก Skill ก่อนหน้า");
            manager.SelectPrev();
        }
    }

    private void OnSelectNextPerformed(InputAction.CallbackContext ctx)
    {
        if (manager != null)
        {
            Debug.Log("?? กด C - เลือก Skill ถัดไป");
            manager.SelectNext();
        }
    }

    private void OnConfirmSkillPerformed(InputAction.CallbackContext ctx)
    {
        // ?? ตรวจสอบว่ามีขวดยาอยู่แล้วหรือไม่
        SkillInventory inv = FindObjectOfType<SkillInventory>();
        if (inv != null && inv.HasAnyBottle())
        {
            Debug.Log("?? ไม่สามารถเริ่ม Mix ได้ เพราะยังมีขวดใน Inventory อยู่แล้ว");
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

    private void OnDestroy()
    {
        // Cleanup Input Actions
        if (selectPrevAction != null)
        {
            selectPrevAction.performed -= OnSelectPrevPerformed;
            selectPrevAction.Dispose();
        }
        if (selectNextAction != null)
        {
            selectNextAction.performed -= OnSelectNextPerformed;
            selectNextAction.Dispose();
        }
        if (confirmSkillAction != null)
        {
            confirmSkillAction.performed -= OnConfirmSkillPerformed;
            confirmSkillAction.Dispose();
        }
    }
}