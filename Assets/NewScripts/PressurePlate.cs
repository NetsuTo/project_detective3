using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

/// <summary>
/// จุดกด/จุดเหยียบที่เมื่อมีผู้เล่นหรือวัตถุเหยียบจะเปิดใช้งานแพล็ตฟอร์ม
/// รองรับหลายแพล็ตฟอร์มพร้อมกัน
/// </summary>
public class PressurePlate : MonoBehaviour
{
    [Header("Platform Settings")]
    [Tooltip("ใช้สำหรับแพล็ตฟอร์มเดียว (ถ้าไม่ใช้ Multiple Platforms)")]
    [SerializeField] private GameObject targetPlatform; // แพล็ตฟอร์มเดียว

    [Tooltip("ใช้สำหรับหลายแพล็ตฟอร์ม - ลากใส่ได้เท่าที่ต้องการ")]
    [SerializeField] private GameObject[] targetPlatforms; // หลายแพล็ตฟอร์ม

    [SerializeField] private bool requireMultipleObjects = false;
    [SerializeField] private int requiredObjectCount = 1;

    [Header("Activation Settings")]
    [SerializeField] private bool stayActive = false;
    [SerializeField] private float activationDelay = 0f;
    [SerializeField] private float deactivationDelay = 0f;
    [SerializeField] private bool activateSequentially = false; // เปิดทีละแพล็ตฟอร์มตามลำดับ
    [SerializeField] private float sequentialDelay = 0.2f; // ดีเลย์ระหว่างแพล็ตฟอร์ม

    [Header("Animation Settings")]
    [SerializeField] private AnimationType animationType = AnimationType.FadeInOut;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private Vector3 platformSpawnOffset = Vector3.down * 2f;

    [Header("Visual Feedback")]
    [SerializeField] private Color inactiveColor = Color.red;
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private GameObject activationEffect;

    [Header("Audio")]
    [SerializeField] private AudioClip activationSound;
    [SerializeField] private AudioClip deactivationSound;

    [SerializeField] private Animator animator;
    [SerializeField] private bool isPressed = false;

    private Renderer plateRenderer;
    private AudioSource audioSource;
    private Material plateMaterial;
    private int objectsOnPlate = 0;
    private bool isActivated = false;
    private List<Coroutine> currentAnimations = new List<Coroutine>();

    private Dictionary<GameObject, Vector3> platformOriginalPositions = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, Quaternion> platformOriginalRotations = new Dictionary<GameObject, Quaternion>();
    private Dictionary<GameObject, Vector3> platformOriginalScales = new Dictionary<GameObject, Vector3>();

    public enum AnimationType
    {
        FadeInOut,
        SlideUp,
        PopIn,
        Instant
    }

    private void Start()
    {
        plateRenderer = GetComponent<Renderer>();
        audioSource = GetComponent<AudioSource>();

        if (plateRenderer != null)
        {
            plateMaterial = plateRenderer.material;
            plateMaterial.color = inactiveColor;
        }

        // เก็บตำแหน่งเดิมของแพล็ตฟอร์มทั้งหมด
        InitializePlatforms();
    }

    private void InitializePlatforms()
    {
        List<GameObject> allPlatforms = GetAllPlatforms();

        foreach (GameObject platform in allPlatforms)
        {
            if (platform != null)
            {
                platformOriginalPositions[platform] = platform.transform.position;
                platformOriginalRotations[platform] = platform.transform.rotation;
                platformOriginalScales[platform] = platform.transform.localScale;

                // ซ่อนแพล็ตฟอร์มตอนเริ่มเกม
                HidePlatformImmediate(platform);
            }
        }
    }

    private List<GameObject> GetAllPlatforms()
    {
        List<GameObject> platforms = new List<GameObject>();

        // เพิ่มแพล็ตฟอร์มเดียว (ถ้ามี)
        if (targetPlatform != null)
        {
            platforms.Add(targetPlatform);
        }

        // เพิ่มหลายแพล็ตฟอร์ม (ถ้ามี)
        if (targetPlatforms != null && targetPlatforms.Length > 0)
        {
            platforms.AddRange(targetPlatforms.Where(p => p != null));
        }

        return platforms;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsValidObject(other.gameObject))
        {
            objectsOnPlate++;
            CheckActivation();
            animator.SetTrigger("Push");
            if (other.CompareTag("Box"))
            {
                isPressed = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsValidObject(other.gameObject))
        {
            objectsOnPlate--;
            objectsOnPlate = Mathf.Max(0, objectsOnPlate);

            if (!stayActive)
            {
                CheckActivation();

                if (other.CompareTag("Player") && isPressed == false)
                {
                    animator.SetTrigger("Out");
                }
            }
        }
    }

    private bool IsValidObject(GameObject obj)
    {
        return obj.CompareTag("Player") || obj.CompareTag("Box") || obj.CompareTag("PressureObject");
    }

    private void CheckActivation()
    {
        bool shouldActivate = objectsOnPlate >= requiredObjectCount;

        if (shouldActivate && !isActivated)
        {
            StartCoroutine(ActivatePlatforms());
        }
        else if (!shouldActivate && isActivated && !stayActive)
        {
            StartCoroutine(DeactivatePlatforms());
        }
    }

    private IEnumerator ActivatePlatforms()
    {
        if (activationDelay > 0)
        {
            yield return new WaitForSeconds(activationDelay);
        }

        isActivated = true;

        // เปลี่ยนสีของแผ่นกด
        if (plateMaterial != null)
        {
            plateMaterial.color = activeColor;
        }

        // เล่นเสียง
        PlaySound(activationSound);

        // เล่น Particle Effect
        if (activationEffect != null)
        {
            Instantiate(activationEffect, transform.position, Quaternion.identity);
        }

        // แสดงแพล็ตฟอร์มทั้งหมด
        List<GameObject> platforms = GetAllPlatforms();

        // หยุด animation เก่าทั้งหมด
        StopAllCurrentAnimations();

        if (activateSequentially)
        {
            // เปิดทีละแพล็ตฟอร์มตามลำดับ
            for (int i = 0; i < platforms.Count; i++)
            {
                if (platforms[i] != null)
                {
                    Coroutine anim = StartCoroutine(ShowPlatform(platforms[i]));
                    currentAnimations.Add(anim);

                    if (i < platforms.Count - 1)
                    {
                        yield return new WaitForSeconds(sequentialDelay);
                    }
                }
            }
        }
        else
        {
            // เปิดพร้อมกันทั้งหมด
            foreach (GameObject platform in platforms)
            {
                if (platform != null)
                {
                    Coroutine anim = StartCoroutine(ShowPlatform(platform));
                    currentAnimations.Add(anim);
                }
            }
        }
    }

    private IEnumerator DeactivatePlatforms()
    {
        if (deactivationDelay > 0)
        {
            yield return new WaitForSeconds(deactivationDelay);
        }

        isActivated = false;

        // เปลี่ยนสีของแผ่นกด
        if (plateMaterial != null)
        {
            plateMaterial.color = inactiveColor;
        }

        // เล่นเสียง
        PlaySound(deactivationSound);

        // ซ่อนแพล็ตฟอร์มทั้งหมด
        List<GameObject> platforms = GetAllPlatforms();

        // หยุด animation เก่าทั้งหมด
        StopAllCurrentAnimations();

        if (activateSequentially)
        {
            // ปิดทีละแพล็ตฟอร์มตามลำดับย้อนกลับ
            for (int i = platforms.Count - 1; i >= 0; i--)
            {
                if (platforms[i] != null)
                {
                    Coroutine anim = StartCoroutine(HidePlatform(platforms[i]));
                    currentAnimations.Add(anim);

                    if (i > 0)
                    {
                        yield return new WaitForSeconds(sequentialDelay);
                    }
                }
            }
        }
        else
        {
            // ปิดพร้อมกันทั้งหมด
            foreach (GameObject platform in platforms)
            {
                if (platform != null)
                {
                    Coroutine anim = StartCoroutine(HidePlatform(platform));
                    currentAnimations.Add(anim);
                }
            }
        }
    }

    private void StopAllCurrentAnimations()
    {
        foreach (Coroutine anim in currentAnimations)
        {
            if (anim != null)
            {
                StopCoroutine(anim);
            }
        }
        currentAnimations.Clear();
    }

    private IEnumerator ShowPlatform(GameObject platform)
    {
        platform.SetActive(true);

        switch (animationType)
        {
            case AnimationType.FadeInOut:
                yield return StartCoroutine(FadePlatform(platform, 0f, 1f));
                break;

            case AnimationType.SlideUp:
                yield return StartCoroutine(SlidePlatform(platform, true));
                break;

            case AnimationType.PopIn:
                yield return StartCoroutine(PopPlatform(platform, true));
                break;

            case AnimationType.Instant:
                break;
        }
    }

    private IEnumerator HidePlatform(GameObject platform)
    {
        switch (animationType)
        {
            case AnimationType.FadeInOut:
                yield return StartCoroutine(FadePlatform(platform, 1f, 0f));
                break;

            case AnimationType.SlideUp:
                yield return StartCoroutine(SlidePlatform(platform, false));
                break;

            case AnimationType.PopIn:
                yield return StartCoroutine(PopPlatform(platform, false));
                break;

            case AnimationType.Instant:
                break;
        }

        platform.SetActive(false);
    }

    private void HidePlatformImmediate(GameObject platform)
    {
        if (animationType == AnimationType.SlideUp)
        {
            platform.transform.position = platformOriginalPositions[platform] + platformSpawnOffset;
        }
        else if (animationType == AnimationType.PopIn)
        {
            platform.transform.localScale = Vector3.zero;
        }

        platform.SetActive(false);
    }

    // Animation Coroutines
    private IEnumerator FadePlatform(GameObject platform, float startAlpha, float endAlpha)
    {
        Renderer[] renderers = platform.GetComponentsInChildren<Renderer>();
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / animationDuration);

            foreach (Renderer r in renderers)
            {
                if (r.material.HasProperty("_Color"))
                {
                    Color color = r.material.color;
                    color.a = alpha;
                    r.material.color = color;
                }
            }

            yield return null;
        }

        // ตั้งค่า alpha สุดท้าย
        foreach (Renderer r in renderers)
        {
            if (r.material.HasProperty("_Color"))
            {
                Color color = r.material.color;
                color.a = endAlpha;
                r.material.color = color;
            }
        }
    }

    private IEnumerator SlidePlatform(GameObject platform, bool show)
    {
        Vector3 startPos = show ? platformOriginalPositions[platform] + platformSpawnOffset : platformOriginalPositions[platform];
        Vector3 endPos = show ? platformOriginalPositions[platform] : platformOriginalPositions[platform] + platformSpawnOffset;

        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            // Ease Out Cubic
            t = 1f - Mathf.Pow(1f - t, 3f);

            platform.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        platform.transform.position = endPos;
    }

    private IEnumerator PopPlatform(GameObject platform, bool show)
    {
        Vector3 startScale = show ? Vector3.zero : platformOriginalScales[platform];
        Vector3 endScale = show ? platformOriginalScales[platform] : Vector3.zero;

        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            // Elastic ease สำหรับ pop effect
            if (show)
            {
                t = 1f - Mathf.Pow(1f - t, 3f);
                float overshoot = Mathf.Sin(t * Mathf.PI);
                t = t + overshoot * 0.1f;
            }

            platform.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        platform.transform.localScale = endScale;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // Debug Gizmos
    private void OnDrawGizmos()
    {
        List<GameObject> platforms = GetAllPlatforms();

        foreach (GameObject platform in platforms)
        {
            if (platform != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, platform.transform.position);

                // แสดงหมายเลขลำดับ
#if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    platform.transform.position + Vector3.up * 0.5f,
                    $"Platform {platforms.IndexOf(platform) + 1}"
                );
#endif
            }
        }
    }
}