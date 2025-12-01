using UnityEngine;

public class DisappearingPlatform : MonoBehaviour
{
    [Header("Timing Settings")]
    [Tooltip("เวลาที่แพลตฟอร์มปรากฏ (วินาที)")]
    public float visibleTime = 2f;
    [Tooltip("เวลาที่แพลตฟอร์มหายไป (วินาที)")]
    public float invisibleTime = 2f;
    [Tooltip("ดีเลย์ก่อนเริ่มวนลูป (วินาที)")]
    public float startDelay = 0f;

    [Header("Visual Settings")]
    [Tooltip("แสดงเอฟเฟกต์เตือนก่อนหายไป")]
    public bool showWarning = true;
    [Tooltip("เวลาเตือนก่อนหาย (วินาที)")]
    public float warningTime = 0.5f;
    [Tooltip("ความเร็วการกระพริบ (ยิ่งต่ำยิ่งช้า)")]
    public float blinkSpeed = 3f;

    [Header("Shrink Settings")]
    [Tooltip("เวลาที่ใช้ในการหดตัว (วินาที)")]
    public float shrinkDuration = 0.3f;
    [Tooltip("ขนาดที่หดเหลือ (0-1, เช่น 0.1 = 10%)")]
    public float shrinkScale = 0.1f;

    private Renderer platformRenderer;
    private Collider platformCollider;
    private float timer;
    private bool isVisible = true;
    private bool isShrinking = false;
    private Color originalColor;
    private Material platformMaterial;
    private Vector3 originalScale;
    private float shrinkTimer;

    void Start()
    {
        // เก็บ Component ต่างๆ
        platformRenderer = GetComponent<Renderer>();
        platformCollider = GetComponent<Collider>();

        // เก็บ Scale เดิม
        originalScale = transform.localScale;

        if (platformRenderer != null)
        {
            // สร้าง Material instance เพื่อไม่ให้กระทบ Material อื่น
            platformMaterial = platformRenderer.material;
            originalColor = platformMaterial.color;
        }

        // เริ่มต้นด้วยการปรากฏ
        timer = startDelay + visibleTime;
    }

    void Update()
    {
        // จัดการการหดตัว
        if (isShrinking)
        {
            shrinkTimer += Time.deltaTime;
            float progress = shrinkTimer / shrinkDuration;

            // ใช้ Ease Out สำหรับการหดตัวที่นุ่มนวล
            float smoothProgress = 1f - Mathf.Pow(1f - progress, 3f);
            transform.localScale = Vector3.Lerp(originalScale, originalScale * shrinkScale, smoothProgress);

            // เมื่อหดเสร็จแล้วให้หายไป
            if (progress >= 1f)
            {
                isShrinking = false;
                HidePlatform();
            }
            return;
        }

        timer -= Time.deltaTime;

        // เช็คว่าใกล้จะหายหรือยัง (สำหรับเอฟเฟกต์เตือน)
        if (isVisible && showWarning && timer <= warningTime && timer > 0)
        {
            // กระพริบเตือน (ใช้ค่า blinkSpeed ที่ปรับได้)
            float alpha = Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed));
            Color warningColor = Color.Lerp(originalColor, Color.red, 0.3f);
            platformMaterial.color = Color.Lerp(originalColor, warningColor, alpha);
        }

        // เมื่อหมดเวลา
        if (timer <= 0)
        {
            if (isVisible)
            {
                // เริ่มหดตัว
                StartShrinking();
            }
            else
            {
                // แสดงแพลตฟอร์มอีกครั้ง
                ShowPlatform();
            }
        }
    }

    void StartShrinking()
    {
        isShrinking = true;
        shrinkTimer = 0f;

        // ปิด Collider ทันทีเมื่อเริ่มหดตัว
        if (platformCollider != null)
            platformCollider.enabled = false;
    }

    void HidePlatform()
    {
        isVisible = false;

        // ซ่อนแพลตฟอร์ม
        if (platformRenderer != null)
            platformRenderer.enabled = false;

        timer = invisibleTime;
    }

    void ShowPlatform()
    {
        isVisible = true;

        // แสดงแพลตฟอร์ม
        if (platformRenderer != null)
        {
            platformRenderer.enabled = true;
            platformMaterial.color = originalColor;
        }

        if (platformCollider != null)
            platformCollider.enabled = true;

        // คืนขนาดเดิม
        transform.localScale = originalScale;

        timer = visibleTime;
    }

    // สำหรับ Debug ดูสถานะใน Editor
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = isVisible ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}