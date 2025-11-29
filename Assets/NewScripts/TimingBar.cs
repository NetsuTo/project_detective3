using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class TimingBar : MonoBehaviour
{
    [Header("⭐ UI References")]
    public RectTransform pointer;     // เข็ม
    public RectTransform targetZone;  // Perfect zone
    [Tooltip("RectTransform ของ Bar พื้นหลัง (ถ้าไม่ใส่จะใช้ transform หลัก)")]
    public RectTransform barBackground;

    [Header("⭐ Settings")]
    public float speed = 200f;        // pixels/sec
    public bool loop = true;
    [Tooltip("ระยะห่างจากขอบ Bar ที่เข็มจะกลับทิศ (0 = ไปชนขอบพอดี)")]
    [Range(0f, 50f)]
    public float edgePadding = 0f;
    [Tooltip("รีเซ็ตตำแหน่งเข็มกลับกลาง Bar ทุกครั้งที่เริ่ม Timing")]
    public bool resetPositionOnStart = false;

    private float direction = 1f;
    private Action<bool> onComplete;
    private bool isActive = false;
    private bool isInitialized = false; // ⭐ เพิ่ม: เช็คว่า Initialize แล้วหรือยัง

    // ===== Input System - รองรับทั้ง Keyboard + Gamepad =====
    private InputAction mixAction;

    void Start()
    {
        // ===== สร้าง Input Action สำหรับปุ่ม F และ Gamepad =====
        mixAction = new InputAction("Mix", type: InputActionType.Button);
        mixAction.AddBinding("<Keyboard>/f");              // Keyboard: F
        mixAction.AddBinding("<Gamepad>/buttonWest");      // Xbox: X, PS: Square □
        mixAction.Enable();

        // ⭐ เริ่มต้นที่ตรงกลาง Bar (ครั้งแรกเท่านั้น)
        if (pointer != null && !isInitialized)
        {
            pointer.anchoredPosition = Vector2.zero;
            direction = 1f;
            isInitialized = true;
            Debug.Log("🎬 TimingBar เริ่มต้น - เข็มอยู่ตรงกลาง");
        }

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
        // ⭐ ป้องกันการเรียกซ้ำ
        if (isActive)
        {
            Debug.LogWarning("⚠️ TimingBar กำลังทำงานอยู่แล้ว! ไม่สามารถเรียก StartTiming() ซ้ำได้");
            return;
        }

        // ⭐ Debug: ดูว่าถูกเรียกกี่ครั้ง
        Debug.Log($"🔔 StartTiming() เรียก! ตำแหน่งปัจจุบัน: {pointer.anchoredPosition.x:F2}, ทิศทาง: {(direction > 0 ? "ขวา →" : "ซ้าย ←")}");

        onComplete = callback;
        isActive = true;

        // ⭐ ถ้าเลือกรีเซ็ตตำแหน่ง ให้รีเซ็ต ไม่งั้นให้เคลื่อนที่ต่อ
        if (resetPositionOnStart)
        {
            pointer.anchoredPosition = Vector2.zero;
            direction = 1f;
            Debug.Log("🔄 รีเซ็ตตำแหน่งเข็มกลับกลาง");
        }
        else
        {
            Debug.Log($"▶️ เข็มเคลื่อนที่ต่อจากตำแหน่ง {pointer.anchoredPosition.x:F2} ทิศทาง {(direction > 0 ? "ขวา →" : "ซ้าย ←")}");
        }

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
            // ⭐ ใช้ barBackground แทน transform
            RectTransform bar = barBackground != null ? barBackground : (RectTransform)transform;

            // ⭐ คำนวณขอบจริงๆ โดยคำนึงถึงความกว้างของเข็ม
            float pointerHalfWidth = pointer.rect.width / 2f;
            float maxX = (bar.rect.width / 2f) - edgePadding - pointerHalfWidth;
            float minX = -(bar.rect.width / 2f) + edgePadding + pointerHalfWidth;

            // เช็คขอบขวา
            if (pointer.anchoredPosition.x > maxX)
            {
                pointer.anchoredPosition = new Vector2(maxX, pointer.anchoredPosition.y);
                direction *= -1f;
                Debug.Log($"🔄 กลับทิศที่ขอบขวา: {maxX:F2} | ทิศทางใหม่: ซ้าย ←");
            }
            // เช็คขอบซ้าย
            else if (pointer.anchoredPosition.x < minX)
            {
                pointer.anchoredPosition = new Vector2(minX, pointer.anchoredPosition.y);
                direction *= -1f;
                Debug.Log($"🔄 กลับทิศที่ขอบซ้าย: {minX:F2} | ทิศทางใหม่: ขวา →");
            }
        }

        // ===== ตรวจสอบปุ่ม F หรือ Gamepad ผ่าน Input System =====
        if (mixAction.WasPressedThisFrame())
        {
            bool success = IsPointerInTarget();

            // ⚠️ หยุดการเคลื่อนที่ก่อน แต่ไม่รีเซ็ตตำแหน่ง/ทิศทาง
            isActive = false;

            if (success)
            {
                Debug.Log($"✅ Timing Perfect! ตำแหน่ง: {pointer.anchoredPosition.x:F2}, ทิศทาง: {(direction > 0 ? "ขวา →" : "ซ้าย ←")}");
            }
            else
            {
                Debug.Log($"❌ Timing Failed! ตำแหน่ง: {pointer.anchoredPosition.x:F2}");
            }

            onComplete?.Invoke(success);

            // ⭐ สำคัญ: ไม่รีเซ็ตตำแหน่งหรือทิศทาง ให้เก็บไว้สำหรับรอบถัดไป
            Debug.Log($"💾 เก็บสถานะ: ตำแหน่ง {pointer.anchoredPosition.x:F2}, ทิศทาง {(direction > 0 ? "ขวา →" : "ซ้าย ←")}");
        }
    }

    bool IsPointerInTarget()
    {
        float pointerX = pointer.anchoredPosition.x;
        float targetLeft = targetZone.anchoredPosition.x - targetZone.rect.width / 2f;
        float targetRight = targetZone.anchoredPosition.x + targetZone.rect.width / 2f;

        // 🐛 Debug ดูค่า
        Debug.Log($"🎯 Pointer: {pointerX:F2} | Target: [{targetLeft:F2}, {targetRight:F2}] | In Range: {pointerX >= targetLeft && pointerX <= targetRight}");

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
        Debug.Log("🔄 รีเซ็ตตำแหน่งเข็ม (เรียกจาก ResetPointer())");
    }
}