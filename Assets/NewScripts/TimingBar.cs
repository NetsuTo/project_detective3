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

    [Header("🎲 Random Target Zone")]
    [Tooltip("สุ่มตำแหน่ง Target Zone ทุกครั้งที่เริ่ม Timing")]
    public bool randomizeTarget = true;
    [Tooltip("แบบสุ่ม: Discrete = จุดเฉพาะ (ซ้าย/กลาง/ขวา), Continuous = ตำแหน่งไหนก็ได้")]
    public RandomMode randomMode = RandomMode.Continuous; // ตั้งค่าเริ่มต้นเป็น Continuous
    [Tooltip("ระยะห่างจากขอบ Bar เมื่อสุ่มแบบ Continuous (ป้องกัน Target ชิดขอบเกินไป)")]
    [Range(0f, 100f)]
    public float targetEdgePadding = 50f;

    [Header("🎲 Random Target Size")]
    [Tooltip("สุ่มขนาด Target Zone ด้วย (จะไม่ใหญ่เกินขนาดเดิม)")]
    public bool randomizeSize = true;
    [Tooltip("ขนาดเล็กสุดที่สุ่มได้ (0-1) เช่น 0.5 = ครึ่งหนึ่งของขนาดเดิม")]
    [Range(0.1f, 1f)]
    public float minSizeScale = 0.5f;

    public enum RandomMode
    {
        Discrete,   // ซ้าย-กลาง-ขวา
        Continuous  // สุ่มต่อเนื่อง
    }

    private float direction = 1f;
    private Action<bool> onComplete;
    private bool isActive = false;
    private bool isInitialized = false;
    private float originalTargetWidth = 0f; // เก็บขนาดเดิมของ Target

    // ===== Input System - รองรับทั้ง Keyboard + Gamepad =====
    private InputAction mixAction;

    void Start()
    {
        // ===== สร้าง Input Action สำหรับปุ่ม F และ Gamepad =====
        mixAction = new InputAction("Mix", type: InputActionType.Button);
        mixAction.AddBinding("<Keyboard>/f");              // Keyboard: F
        mixAction.AddBinding("<Gamepad>/buttonWest");      // Xbox: X, PS: Square □
        mixAction.Enable();

        // ⭐ เก็บขนาดเดิมของ Target Zone (ใช้ rect.width เพราะรองรับทุก Anchor)
        if (targetZone != null)
        {
            originalTargetWidth = targetZone.rect.width;
            Debug.Log($"💾 เก็บขนาดเดิมของ Target: {originalTargetWidth}");
        }

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

        Debug.Log($"🔔 StartTiming() เรียก! ตำแหน่งปัจจุบัน: {pointer.anchoredPosition.x:F2}, ทิศทาง: {(direction > 0 ? "ขวา →" : "ซ้าย ←")}");

        onComplete = callback;
        isActive = true;

        // 🎲 สุ่มตำแหน่ง Target Zone (ถ้าเปิดใช้งาน)
        if (randomizeTarget)
        {
            RandomizeTargetPosition();
        }

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

    // ===== 🎲 ระบบสุ่มตำแหน่ง Target Zone =====
    void RandomizeTargetPosition()
    {
        if (targetZone == null)
        {
            Debug.LogWarning("⚠️ ไม่มี Target Zone ให้สุ่ม!");
            return;
        }

        // 🎲 สุ่มขนาด Target (ถ้าเปิดใช้งาน)
        float currentTargetWidth = targetZone.rect.width; // อ่านขนาดปัจจุบัน

        if (randomizeSize && originalTargetWidth > 0)
        {
            float randomScale = UnityEngine.Random.Range(minSizeScale, 1f);
            currentTargetWidth = originalTargetWidth * randomScale;

            // เปลี่ยนขนาด Target Zone ด้วย SetSizeWithCurrentAnchors (รองรับทุก Anchor)
            targetZone.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentTargetWidth);

            Debug.Log($"📏 สุ่มขนาด Target: {randomScale:P0} ({currentTargetWidth:F1}px จากเดิม {originalTargetWidth:F1}px)");
        }
        else if (originalTargetWidth > 0)
        {
            // รีเซ็ตกลับเป็นขนาดเดิม (ถ้าไม่สุ่ม)
            targetZone.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, originalTargetWidth);
            currentTargetWidth = originalTargetWidth;
        }

        // อ่านขนาดจริงหลังจากแก้ไข
        currentTargetWidth = targetZone.rect.width;

        RectTransform bar = barBackground != null ? barBackground : (RectTransform)transform;
        float barWidth = bar.rect.width;
        float targetHalfWidth = currentTargetWidth / 2f;
        float newX = 0f;

        if (randomMode == RandomMode.Discrete)
        {
            // แบบจุดเฉพาะ: ซ้าย (-), กลาง (0), ขวา (+)
            int randomChoice = UnityEngine.Random.Range(0, 3);
            float maxOffset = (barWidth / 2f) - targetEdgePadding - targetHalfWidth;

            switch (randomChoice)
            {
                case 0: // ซ้าย
                    newX = -maxOffset;
                    Debug.Log("🎲 สุ่มได้: ซ้าย ←");
                    break;
                case 1: // กลาง
                    newX = 0f;
                    Debug.Log("🎲 สุ่มได้: กลาง ■");
                    break;
                case 2: // ขวา
                    newX = maxOffset;
                    Debug.Log("🎲 สุ่มได้: ขวา →");
                    break;
            }
        }
        else
        {
            // แบบต่อเนื่อง: สุ่มตำแหน่งไหนก็ได้ภายใน Bar
            float minX = -(barWidth / 2f) + targetEdgePadding + targetHalfWidth;
            float maxX = (barWidth / 2f) - targetEdgePadding - targetHalfWidth;
            newX = UnityEngine.Random.Range(minX, maxX);
            Debug.Log($"🎲 สุ่มได้ (Continuous): {newX:F2}");
        }

        targetZone.anchoredPosition = new Vector2(newX, targetZone.anchoredPosition.y);
        Debug.Log($"🎯 Target Zone: Width={currentTargetWidth:F1}, Pos={newX:F2}, BarWidth={barWidth:F1}");
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

    // ฟังก์ชันเสริม: เรียกสุ่มตำแหน่ง Target จากภายนอก
    public void ManualRandomizeTarget()
    {
        RandomizeTargetPosition();
    }
}