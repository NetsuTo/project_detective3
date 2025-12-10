using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class TimingBar : MonoBehaviour
{
    [Header("⭐ UI References")]
    public RectTransform pointer;
    public RectTransform targetZone;
    [Tooltip("RectTransform ของ Bar พื้นหลัง (ถ้าไม่ใส่จะใช้ transform หลัก)")]
    public RectTransform barBackground;

    [Header("⭐ Settings")]
    public float speed = 200f;
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
    public RandomMode randomMode = RandomMode.Continuous;
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
        Discrete,
        Continuous
    }

    private float direction = 1f;
    private Action<bool> onComplete;
    private bool isActive = false;
    private bool isInitialized = false;
    private float originalTargetWidth = 0f;

    // ===== ⭐ ใช้ปุ่มเดียวกับ PlayerInput - F และ B =====
    private InputAction confirmAction;

    void Start()
    {
        // ===== สร้าง Input Action สำหรับปุ่ม F และ Gamepad B =====
        confirmAction = new InputAction("Confirm", type: InputActionType.Button);
        confirmAction.AddBinding("<Keyboard>/f");
        confirmAction.AddBinding("<Gamepad>/buttonEast");  // Xbox: B, PS: Circle
        confirmAction.Enable();

        // ⭐ เก็บขนาดเดิมของ Target Zone
        if (targetZone != null)
        {
            originalTargetWidth = targetZone.rect.width;
            Debug.Log($"💾 เก็บขนาดเดิมของ Target: {originalTargetWidth}");
        }

        // ⭐ เริ่มต้นที่ตรงกลาง Bar
        if (pointer != null && !isInitialized)
        {
            pointer.anchoredPosition = Vector2.zero;
            direction = 1f;
            isInitialized = true;
            Debug.Log("🎬 TimingBar เริ่มต้น - เข็มอยู่ตรงกลาง");
        }

        Debug.Log("✅ TimingBar Started - F (Keyboard) / B/Circle (Gamepad) Ready!");
    }

    private void OnEnable()
    {
        confirmAction?.Enable();
    }

    private void OnDisable()
    {
        confirmAction?.Disable();
    }

    private void OnDestroy()
    {
        confirmAction?.Dispose();
    }

    public void StartTiming(Action<bool> callback)
    {
        // ⭐ ป้องกันการเรียกซ้ำ
        if (isActive)
        {
            Debug.LogWarning("⚠️ TimingBar กำลังทำงานอยู่แล้ว!");
            return;
        }

        Debug.Log($"🔔 StartTiming() เรียก! ตำแหน่งปัจจุบัน: {pointer.anchoredPosition.x:F2}, ทิศทาง: {(direction > 0 ? "ขวา →" : "ซ้าย ←")}");

        onComplete = callback;
        isActive = true;

        // 🎲 สุ่มตำแหน่ง Target Zone
        if (randomizeTarget)
        {
            RandomizeTargetPosition();
        }

        // ⭐ รีเซ็ตหรือเคลื่อนที่ต่อ
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

        Debug.Log("⏱️ เริ่ม Timing Bar - กด F หรือ B/Circle เพื่อหยุดเข็ม");
    }

    void Update()
    {
        if (!isActive) return;

        // เคลื่อนที่เข็ม
        pointer.anchoredPosition += Vector2.right * speed * direction * Time.deltaTime;

        // กลับทิศเมื่อชนขอบ
        if (loop)
        {
            RectTransform bar = barBackground != null ? barBackground : (RectTransform)transform;

            float pointerHalfWidth = pointer.rect.width / 2f;
            float maxX = (bar.rect.width / 2f) - edgePadding - pointerHalfWidth;
            float minX = -(bar.rect.width / 2f) + edgePadding + pointerHalfWidth;

            if (pointer.anchoredPosition.x > maxX)
            {
                pointer.anchoredPosition = new Vector2(maxX, pointer.anchoredPosition.y);
                direction *= -1f;
                Debug.Log($"🔄 กลับทิศที่ขอบขวา: {maxX:F2} | ทิศทางใหม่: ซ้าย ←");
            }
            else if (pointer.anchoredPosition.x < minX)
            {
                pointer.anchoredPosition = new Vector2(minX, pointer.anchoredPosition.y);
                direction *= -1f;
                Debug.Log($"🔄 กลับทิศที่ขอบซ้าย: {minX:F2} | ทิศทางใหม่: ขวา →");
            }
        }

        // ===== ⭐ ตรวจสอบปุ่ม F หรือ B =====
        if (confirmAction.WasPressedThisFrame())
        {
            bool success = IsPointerInTarget();

            isActive = false;

            if (success)
            {
                Debug.Log($"✅ Timing Perfect! ตำแหน่ง: {pointer.anchoredPosition.x:F2}");
            }
            else
            {
                Debug.Log($"❌ Timing Failed! ตำแหน่ง: {pointer.anchoredPosition.x:F2}");
            }

            onComplete?.Invoke(success);

            Debug.Log($"💾 เก็บสถานะ: ตำแหน่ง {pointer.anchoredPosition.x:F2}, ทิศทาง {(direction > 0 ? "ขวา →" : "ซ้าย ←")}");
        }
    }

    bool IsPointerInTarget()
    {
        float pointerX = pointer.anchoredPosition.x;
        float targetLeft = targetZone.anchoredPosition.x - targetZone.rect.width / 2f;
        float targetRight = targetZone.anchoredPosition.x + targetZone.rect.width / 2f;

        Debug.Log($"🎯 Pointer: {pointerX:F2} | Target: [{targetLeft:F2}, {targetRight:F2}] | In Range: {pointerX >= targetLeft && pointerX <= targetRight}");

        return pointerX >= targetLeft && pointerX <= targetRight;
    }

    void RandomizeTargetPosition()
    {
        if (targetZone == null) return;

        float currentTargetWidth = targetZone.rect.width;

        // 🎲 สุ่มขนาด Target
        if (randomizeSize && originalTargetWidth > 0)
        {
            float randomScale = UnityEngine.Random.Range(minSizeScale, 1f);
            currentTargetWidth = originalTargetWidth * randomScale;
            targetZone.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentTargetWidth);
            Debug.Log($"📏 สุ่มขนาด Target: {randomScale:P0} ({currentTargetWidth:F1}px)");
        }
        else if (originalTargetWidth > 0)
        {
            targetZone.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, originalTargetWidth);
            currentTargetWidth = originalTargetWidth;
        }

        currentTargetWidth = targetZone.rect.width;

        RectTransform bar = barBackground != null ? barBackground : (RectTransform)transform;
        float barWidth = bar.rect.width;
        float targetHalfWidth = currentTargetWidth / 2f;
        float newX = 0f;

        if (randomMode == RandomMode.Discrete)
        {
            int randomChoice = UnityEngine.Random.Range(0, 3);
            float maxOffset = (barWidth / 2f) - targetEdgePadding - targetHalfWidth;

            switch (randomChoice)
            {
                case 0: newX = -maxOffset; Debug.Log("🎲 สุ่มได้: ซ้าย ←"); break;
                case 1: newX = 0f; Debug.Log("🎲 สุ่มได้: กลาง ■"); break;
                case 2: newX = maxOffset; Debug.Log("🎲 สุ่มได้: ขวา →"); break;
            }
        }
        else
        {
            float minX = -(barWidth / 2f) + targetEdgePadding + targetHalfWidth;
            float maxX = (barWidth / 2f) - targetEdgePadding - targetHalfWidth;
            newX = UnityEngine.Random.Range(minX, maxX);
            Debug.Log($"🎲 สุ่มได้ (Continuous): {newX:F2}");
        }

        targetZone.anchoredPosition = new Vector2(newX, targetZone.anchoredPosition.y);
        Debug.Log($"🎯 Target Zone: Width={currentTargetWidth:F1}, Pos={newX:F2}");
    }

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

    public void ManualRandomizeTarget()
    {
        RandomizeTargetPosition();
    }
}