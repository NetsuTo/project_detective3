using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    private PlayerSkillManager manager;

    // ===== Input System Actions - รองรับ Keyboard + Gamepad =====
    private InputAction selectPrevAction;
    private InputAction selectNextAction;
    private InputAction confirmSkillAction;

    private void Awake()
    {
        manager = GetComponent<PlayerSkillManager>();

        // สร้าง Input Actions
        SetupInputActions();

        // Enable ทันทีใน Awake
        selectPrevAction?.Enable();
        selectNextAction?.Enable();
        confirmSkillAction?.Enable();

        Debug.Log("? PlayerInput Started - Keyboard + Gamepad Ready!");
    }

    private void SetupInputActions()
    {
        // ===== เลือก Skill ก่อนหน้า - Z และ Right Stick Up =====
        selectPrevAction = new InputAction("SelectPrev", type: InputActionType.Button);
        selectPrevAction.AddBinding("<Keyboard>/z");
        selectPrevAction.AddBinding("<Gamepad>/rightStick/up");  // ? เปลี่ยนเป็น Right Stick
        selectPrevAction.performed += OnSelectPrevPerformed;

        // ===== เลือก Skill ถัดไป - C และ Right Stick Down =====
        selectNextAction = new InputAction("SelectNext", type: InputActionType.Button);
        selectNextAction.AddBinding("<Keyboard>/c");
        selectNextAction.AddBinding("<Gamepad>/rightStick/down");  // ? เปลี่ยนเป็น Right Stick
        selectNextAction.performed += OnSelectNextPerformed;

        // ===== ยืนยัน Skill - T และ Button East (B/Circle) =====
        confirmSkillAction = new InputAction("ConfirmSkill", type: InputActionType.Button);
        confirmSkillAction.AddBinding("<Keyboard>/t");
        confirmSkillAction.AddBinding("<Gamepad>/buttonEast");  // B/Circle (เหมือนเดิม)
        confirmSkillAction.performed += OnConfirmSkillPerformed;
    }

    // อ่าน Input ทุกเฟรมด้วย Update() (วิธีสำรอง - สำหรับ Old Input System)
    private void Update()
    {
        // ?? Fallback: ตรวจสอบ Gamepad โดยตรง
        if (Gamepad.current != null)
        {
            // Right Stick Up (เลือกก่อนหน้า)
            if (Gamepad.current.rightStick.up.wasPressedThisFrame)
            {
                OnSelectPrevPerformed(default);
            }

            // Right Stick Down (เลือกถัดไป)
            if (Gamepad.current.rightStick.down.wasPressedThisFrame)
            {
                OnSelectNextPerformed(default);
            }

            // Button East (ยืนยัน)
            if (Gamepad.current.buttonEast.wasPressedThisFrame)
            {
                OnConfirmSkillPerformed(default);
            }
        }

        // ?? Fallback: ตรวจสอบ Keyboard โดยตรง
        if (Keyboard.current != null)
        {
            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                OnConfirmSkillPerformed(default);
            }

            if (Keyboard.current.zKey.wasPressedThisFrame)
            {
                OnSelectPrevPerformed(default);
            }

            if (Keyboard.current.cKey.wasPressedThisFrame)
            {
                OnSelectNextPerformed(default);
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
            Debug.Log("?? เลือก Skill ก่อนหน้า (Z / D-Pad Up)");
            manager.SelectPrev();
        }
    }

    private void OnSelectNextPerformed(InputAction.CallbackContext ctx)
    {
        if (manager != null)
        {
            Debug.Log("?? เลือก Skill ถัดไป (C / D-Pad Down)");
            manager.SelectNext();
        }
    }

    private void OnConfirmSkillPerformed(InputAction.CallbackContext ctx)
    {
        // ?? เช็คว่าหนังสือ Tutorial เปิดอยู่หรือไม่
        TutorialBook tutorialBook = FindObjectOfType<TutorialBook>();
        if (tutorialBook != null && tutorialBook.IsBookOpen())
        {
            Debug.Log("?? ไม่สามารถกด T ได้ - Tutorial Book เปิดอยู่!");
            return;
        }

        // ?? เช็คว่า Pause Menu เปิดอยู่หรือไม่
        PauseMenuWithVolume pauseMenu = FindObjectOfType<PauseMenuWithVolume>();
        if (pauseMenu != null && pauseMenu.IsPaused())
        {
            Debug.Log("?? ไม่สามารถกด T ได้ - Pause Menu เปิดอยู่!");
            return;
        }

        // ?? เช็คว่ามินิเกมลูกศร (ElementMiniGame) กำลังทำงานอยู่หรือไม่
        if (ElementMiniGameManager.activeMiniGame != null)
        {
            Debug.Log("?? ไม่สามารถกด T ได้ - มินิเกมลูกศรกำลังทำงานอยู่!");
            return;
        }

        // ?? เช็คว่า QTE กำลังทำงานหรือไม่ - ล็อคปุ่ม T
        SkillLetterSelector selector = GetComponent<SkillLetterSelector>();
        if (selector != null && !selector.CanPressT)
        {
            Debug.Log("?? ไม่สามารถกด T ได้ - QTE กำลังทำงานอยู่หรือตัวอักษรยังอยู่บนหัว!");
            return;
        }

        // ?? ตรวจสอบว่ามีขวดยาอยู่แล้วหรือไม่
        SkillInventory inv = FindObjectOfType<SkillInventory>();
        if (inv != null && inv.HasAnyBottle())
        {
            Debug.Log("? ไม่สามารถเริ่ม Mix ได้ เพราะยังมีขวดใน Inventory อยู่แล้ว");
            return; // หยุดการทำงาน ไม่ให้แสดงตัวอักษรบนหัว
        }

        // ? ถ้าไม่มีขวดและไม่มี QTE ให้ Confirm Skill ได้
        if (manager != null && manager.GetSkills().Count > 0)
        {
            Debug.Log("? เริ่ม Mix Skill (T / B/Circle)");
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