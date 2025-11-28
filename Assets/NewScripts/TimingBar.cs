using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class TimingBar : MonoBehaviour
{
    public RectTransform pointer;     // เข็ม
    public RectTransform targetZone;  // Perfect zone
    public float speed = 200f;        // pixels/sec
    public bool loop = true;

    private float direction = 1f;
    private Action<bool> onComplete;
    private bool isActive = false;

    // ===== Input System - รองรับทั้ง Keyboard + Gamepad =====
    private InputAction mixAction;

    void Start()
    {
        // ===== สร้าง Input Action สำหรับปุ่ม F และ Gamepad =====
        mixAction = new InputAction("Mix", type: InputActionType.Button);
        mixAction.AddBinding("<Keyboard>/f");              // Keyboard: F
        mixAction.AddBinding("<Gamepad>/buttonWest");      // Xbox: X, PS: Square □

        mixAction.Enable();

        Debug.Log("✅ TimingBar Started - F (Keyboard) / X/Square (Gamepad) Ready!");
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

    public void StartTiming(Action<bool> callback)
    {
        onComplete = callback;
        isActive = true;
        pointer.anchoredPosition = Vector2.zero; // เริ่มตรงกลาง bar
        direction = 1f; // รีเซ็ตทิศทาง

        Debug.Log("⏱️ เริ่ม Timing Bar - กด F หรือ X/Square เพื่อหยุดเข็ม");
    }

    void Update()
    {
        if (!isActive) return;

        // move pointer
        pointer.anchoredPosition += Vector2.right * speed * direction * Time.deltaTime;

        // reverse direction
        if (loop)
        {
            float halfWidth = ((RectTransform)transform).rect.width / 2f;
            if (pointer.anchoredPosition.x > halfWidth)
            {
                pointer.anchoredPosition = new Vector2(halfWidth, pointer.anchoredPosition.y);
                direction *= -1f;
            }
            else if (pointer.anchoredPosition.x < -halfWidth)
            {
                pointer.anchoredPosition = new Vector2(-halfWidth, pointer.anchoredPosition.y);
                direction *= -1f;
            }
        }

        // ===== ตรวจสอบปุ่ม F หรือ Gamepad ผ่าน Input System =====
        if (mixAction.WasPressedThisFrame())
        {
            bool success = IsPointerInTarget();

            if (success)
            {
                Debug.Log("✅ Timing Perfect!");
            }
            else
            {
                Debug.Log("❌ Timing Failed!");
            }

            onComplete?.Invoke(success);
            isActive = false;
        }
    }

    bool IsPointerInTarget()
    {
        float pointerX = pointer.anchoredPosition.x;
        float targetLeft = targetZone.anchoredPosition.x - targetZone.rect.width / 2f;
        float targetRight = targetZone.anchoredPosition.x + targetZone.rect.width / 2f;
        return pointerX >= targetLeft && pointerX <= targetRight;
    }

    // ===== ฟังก์ชันเสริม =====
    public void StopTiming()
    {
        isActive = false;
        Debug.Log("⏸️ หยุด Timing Bar");
    }

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
        Debug.Log($"⚡ เปลี่ยนความเร็วเข็มเป็น: {newSpeed}");
    }

    public bool IsTimingActive()
    {
        return isActive;
    }

    public void ResetPointer()
    {
        pointer.anchoredPosition = Vector2.zero;
        direction = 1f;
        Debug.Log("🔄 รีเซ็ตตำแหน่งเข็ม");
    }
}