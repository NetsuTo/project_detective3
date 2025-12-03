using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

public class TargetZone : MonoBehaviour
{
    [Header("MiniGame ของ Zone นี้")]
    public ElementMiniGameManager miniGame;

    [Header("ธาตุที่ต้องการ (แต่ละธาตุคือ 1 sequence)")]
    public List<ElementRequirement> requiredElements = new List<ElementRequirement>();

    [Header("🎯 Events เมื่อครบทุกธาตุ")]
    public UnityEvent onAllElementsCompleted;

    private bool playerInside = false;
    private SkillInventory playerInventory;
    private HashSet<int> completedElements = new HashSet<int>();

    // ✅ เพิ่มตัวแปรเก็บสกิลชั่วคราว
    private List<string> lastUsedSkill = null;

    // ===== Input System Actions - รองรับ Keyboard + Gamepad =====
    private InputAction useSkillAction;

    [System.Serializable]
    public class ElementRequirement
    {
        [Tooltip("ชื่อธาตุ เช่น 'Water (H2O)'")]
        public string elementName;

        [Tooltip("ลำดับธาตุ เช่น H, H, O (ใช้ตัวอักษร)")]
        public List<string> sequence = new List<string>();
    }

    void Awake()
    {
        // สร้าง Input Action สำหรับใช้สกิล
        SetupInputActions();
    }

    private void SetupInputActions()
    {
        // ===== ใช้สกิล - รองรับ R และ Right Trigger =====
        useSkillAction = new InputAction("UseSkill", type: InputActionType.Button);
        useSkillAction.AddBinding("<Keyboard>/r");
        useSkillAction.AddBinding("<Gamepad>/rightTrigger");  // RT/R2
        useSkillAction.AddBinding("<Gamepad>/rightShoulder"); // RB/R1 (สำรอง)

        useSkillAction.performed += OnUseSkillPerformed;
        useSkillAction.Enable();

        Debug.Log("✅ TargetZone Input System Ready!");
    }

    private void OnEnable()
    {
        useSkillAction?.Enable();
    }

    private void OnDisable()
    {
        useSkillAction?.Disable();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            playerInventory = other.GetComponent<SkillInventory>();
            Debug.Log($"🎯 เข้าสู่ TargetZone ({completedElements.Count}/{requiredElements.Count} สมบูรณ์)");

            if (requiredElements.Count > 0)
            {
                Debug.Log($"💡 กด R หรือ RT/R2 เพื่อใช้สกิล | กด ESC เพื่อยกเลิก");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            playerInventory = null;
            Debug.Log("👋 ออกจาก TargetZone");
        }
    }

    // ===== Update() สำหรับ Fallback (Old Input System) =====
    private void Update()
    {
        if (!playerInside) return;

        // Fallback สำหรับ Old Input System
        if (Keyboard.current == null && Gamepad.current == null)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                OnUseSkillPerformed(default);
            }
        }
    }

    // ===== Input Callback =====
    private void OnUseSkillPerformed(InputAction.CallbackContext ctx)
    {
        if (!playerInside) return;

        if (playerInventory == null || playerInventory.IsEmpty())
        {
            Debug.Log("❌ ไม่มีสกิลในขวดให้ใช้");
            return;
        }

        int matchedIndex = -1;
        for (int i = 0; i < requiredElements.Count; i++)
        {
            if (completedElements.Contains(i)) continue;

            if (playerInventory.HasSkill(requiredElements[i].sequence))
            {
                matchedIndex = i;
                break;
            }
        }

        if (matchedIndex >= 0)
        {
            ElementRequirement matched = requiredElements[matchedIndex];
            Debug.Log($"🎯 พบธาตุที่ {matchedIndex + 1}: {matched.elementName} ({string.Join("-", matched.sequence)})");

            // ✅ เก็บสกิลไว้ก่อนใช้ (สำหรับคืนตอน Cancel)
            lastUsedSkill = new List<string>(matched.sequence);

            playerInventory.ConsumeSkill(matched.sequence);

            // ✅ ส่ง 'this' (TargetZone) ไปด้วยเพื่อให้คืนสกิลได้
            miniGame.StartMiniGame(null, (success) =>
            {
                if (success)
                {
                    Debug.Log($"✅ ผ่านมินิเกมของ {matched.elementName}");
                    completedElements.Add(matchedIndex);
                    lastUsedSkill = null; // ✅ สำเร็จแล้ว ไม่ต้องคืน

                    if (completedElements.Count >= requiredElements.Count)
                    {
                        Debug.Log("🎉 เสร็จสมบูรณ์! ครบทุกธาตุแล้ว");
                        OnAllElementsCompleted();
                    }
                    else
                    {
                        Debug.Log($"📋 เหลืออีก {requiredElements.Count - completedElements.Count} ธาตุ");
                    }
                }
                else
                {
                    Debug.Log($"💥 ล้มเหลวในมินิเกมของ {matched.elementName}");
                    miniGame.ShowFailSymbolSafe();
                    lastUsedSkill = null; // ❌ ล้มเหลว ไม่คืนสกิล
                }
            }, this); // ✅ ส่ง TargetZone ไปด้วย
        }
        else
        {
            Debug.Log("❌ ธาตุในขวดไม่ตรงกับที่ต้องการ หรือเสร็จไปแล้ว");
            miniGame.ShowFailSymbolSafe();
            playerInventory.ConsumeFirstSkill();
        }
    }

    /// <summary>
    /// ✅ คืนสกิลที่ใช้ล่าสุด (เรียกจาก MiniGame เมื่อกด ESC)
    /// </summary>
    public void ReturnLastUsedSkill()
    {
        if (lastUsedSkill != null && playerInventory != null)
        {
            playerInventory.AddSkill(lastUsedSkill);
            Debug.Log($"🔄 คืนสกิล: {string.Join("-", lastUsedSkill)}");
            lastUsedSkill = null;
        }
        else
        {
            Debug.LogWarning("⚠️ ไม่มีสกิลที่จะคืน (lastUsedSkill = null)");
        }
    }

    private void OnAllElementsCompleted()
    {
        Debug.Log("🏆 Zone Complete! ครบทุกธาตุแล้ว");
        onAllElementsCompleted?.Invoke();
    }

    // 🔑 ฟังก์ชันนับจำนวนสกิลที่ใช้ไปแล้ว (สำหรับเช็คปลดล็อค)
    public int GetCompletedCount()
    {
        return completedElements.Count;
    }

    // เช็คว่าใช้สกิลครบตามจำนวนที่กำหนดหรือยัง
    public bool HasCompletedAtLeast(int count)
    {
        return completedElements.Count >= count;
    }

    public void ResetZone()
    {
        completedElements.Clear();
        lastUsedSkill = null;
        Debug.Log("🔄 Reset Zone");
    }

    public bool IsCompleted()
    {
        return completedElements.Count >= requiredElements.Count;
    }

    public List<string> GetRemainingElements()
    {
        List<string> remaining = new List<string>();
        for (int i = 0; i < requiredElements.Count; i++)
        {
            if (!completedElements.Contains(i))
            {
                remaining.Add(requiredElements[i].elementName);
            }
        }
        return remaining;
    }

    // ===== Helper Methods =====
    public bool IsPlayerInside()
    {
        return playerInside;
    }

    public int GetTotalRequiredElements()
    {
        return requiredElements.Count;
    }

    public float GetCompletionPercentage()
    {
        if (requiredElements.Count == 0) return 0f;
        return (float)completedElements.Count / requiredElements.Count * 100f;
    }

    void OnDestroy()
    {
        // Cleanup Input Action
        if (useSkillAction != null)
        {
            useSkillAction.performed -= OnUseSkillPerformed;
            useSkillAction.Dispose();
        }
    }
}