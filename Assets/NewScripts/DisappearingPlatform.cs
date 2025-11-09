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

    private Renderer platformRenderer;
    private Collider platformCollider;
    private float timer;
    private bool isVisible = true;
    private Color originalColor;
    private Material platformMaterial;

    void Start()
    {
        // เก็บ Component ต่างๆ
        platformRenderer = GetComponent<Renderer>();
        platformCollider = GetComponent<Collider>();

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
        timer -= Time.deltaTime;

        // เช็คว่าใกล้จะหายหรือยัง (สำหรับเอฟเฟกต์เตือน)
        if (isVisible && showWarning && timer <= warningTime && timer > 0)
        {
            // กระพริบเตือน
            float blinkSpeed = 10f;
            float alpha = Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed));
            Color warningColor = Color.Lerp(originalColor, Color.red, 0.3f);
            platformMaterial.color = Color.Lerp(originalColor, warningColor, alpha);
        }

        // เมื่อหมดเวลา ให้สลับสถานะ
        if (timer <= 0)
        {
            TogglePlatform();
        }
    }

    void TogglePlatform()
    {
        isVisible = !isVisible;

        if (isVisible)
        {
            // แสดงแพลตฟอร์ม
            if (platformRenderer != null)
            {
                platformRenderer.enabled = true;
                platformMaterial.color = originalColor;
            }
            if (platformCollider != null)
                platformCollider.enabled = true;

            timer = visibleTime;
        }
        else
        {
            // ซ่อนแพลตฟอร์ม
            if (platformRenderer != null)
                platformRenderer.enabled = false;
            if (platformCollider != null)
                platformCollider.enabled = false;

            timer = invisibleTime;
        }
    }

    // สำหรับ Debug ดูสถานะใน Editor
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = isVisible ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}