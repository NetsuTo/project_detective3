using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SkillLetterSelector : MonoBehaviour
{
    public Transform uiAnchor;
    public GameObject letterPrefab;
    public Canvas mainCanvas;
    public float cycleSpeed = 2f;

    [Header("⭐ Animation Settings")]
    [Tooltip("ระยะเวลาที่ตัวอักษรค่อยๆ โผล่ขึ้นมา")]
    public float fadeInDuration = 0.3f;
    [Tooltip("ดีเลย์ระหว่างตัวอักษรแต่ละตัว")]
    public float letterDelay = 0.1f;
    [Tooltip("เอฟเฟกต์การโผล่: None, Fade, Scale, Both, SlideDown, Pop")]
    public AnimationType animationType = AnimationType.Both;

    public enum AnimationType
    {
        None,
        Fade,
        Scale,
        Both,
        SlideDown,
        Pop
    }

    private List<char> letters = new List<char>();
    private List<char> remaining = new List<char>();
    private List<char> originalLetters = new List<char>();

    private int currentIndex = 0;
    private float timer = 0f;
    private bool isActive = false;
    private bool qteStarted = false;
    private bool isQTERunning = false;

    private List<GameObject> letterUIs = new List<GameObject>();
    private List<SkillLetterUI> letterImages = new List<SkillLetterUI>();

    private PlayerSkillManager manager;
    public Vector3[] customOffsets;
    public float letterSpacing = 100f;

    private InputAction mixAction;

    // ⭐ Property สำหรับเช็คจากภายนอก (PlayerSkillManager จะเช็คตัวนี้)
    public bool CanPressT => !isActive && !isQTERunning;

    void Start()
    {
        manager = GetComponent<PlayerSkillManager>();

        // สร้าง Input Action สำหรับปุ่ม F และ Gamepad
        mixAction = new InputAction("Mix", type: InputActionType.Button);
        mixAction.AddBinding("<Keyboard>/f");
        mixAction.AddBinding("<Gamepad>/buttonWest");

        mixAction.Enable();

        Debug.Log("✅ SkillLetterSelector Started!");
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

    void Update()
    {
        if (!isActive) return;

        // ให้ตัวอักษรกระพริบอยู่เสมอ
        timer += Time.deltaTime;
        if (timer >= 1f / cycleSpeed && remaining.Count > 0)
        {
            timer = 0f;
            currentIndex = (currentIndex + 1) % remaining.Count;

            for (int i = 0; i < letterImages.Count; i++)
            {
                letterImages[i].UpdateLetter(remaining[currentIndex]);
            }
        }

        // เช็คว่ากด F/X และยังไม่มี QTE ทำงานอยู่
        if (mixAction.WasPressedThisFrame() && remaining.Count > 0)
        {
            if (!isQTERunning)
            {
                qteStarted = true;
                isQTERunning = true;
                StartQTE();
                Debug.Log("🎯 กดปุ่ม F/X -> เริ่ม QTE! (🔒 ล็อคปุ่ม T แล้ว)");
            }
            else
            {
                Debug.Log("⚠️ QTE กำลังทำงานอยู่ - ไม่สามารถเริ่ม QTE ใหม่ได้!");
            }
        }

        // อัปเดตตำแหน่ง UI บนหัว
        for (int i = 0; i < letterUIs.Count; i++)
        {
            Vector3 offset;

            if (customOffsets != null && i < customOffsets.Length)
            {
                offset = customOffsets[i];
            }
            else
            {
                offset = new Vector3(i * letterSpacing, 0f, 0f);
            }

            Vector3 screenPos = Camera.main.WorldToScreenPoint(uiAnchor.position) + offset;
            letterUIs[i].transform.position = screenPos;
        }
    }

    public void StartSelection(string skillID)
    {
        // 🔒 เช็ค QTEManager ว่า QTE กำลังทำงานอยู่หรือไม่
        QTEManager qte = FindObjectOfType<QTEManager>();
        if (qte != null && qte.IsQTEActive)
        {
            Debug.Log("⚠️ QTE กำลังทำงานอยู่ที่ QTEManager - ไม่สามารถกด T ได้!");
            return;
        }

        // 🔒 ถ้า QTE กำลังทำงานอยู่ → บล็อคไม่ให้กด T
        if (isQTERunning)
        {
            Debug.Log("⚠️ QTE กำลังทำงานอยู่ - ไม่สามารถกด T ได้!");
            return;
        }

        // 🔒 ถ้ายังมีตัวอักษรอยู่บนหัว → บล็อคไม่ให้กด T ซ้ำ
        if (isActive)
        {
            Debug.Log("⚠️ ตัวอักษรยังอยู่บนหัว - ต้องกด F/X เพื่อเริ่ม QTE ก่อน!");
            return;
        }

        letters = new List<char>(skillID.ToCharArray());
        remaining = new List<char>(letters);
        originalLetters = new List<char>(letters);

        // ลบ UI เดิมถ้ามี
        foreach (var ui in letterUIs)
        {
            if (ui != null) Destroy(ui);
        }
        letterUIs.Clear();
        letterImages.Clear();

        StartCoroutine(SpawnLettersWithAnimation());

        currentIndex = 0;
        isActive = true;
        qteStarted = false;
        isQTERunning = false;

        Debug.Log("🔤 เริ่ม Selection สำหรับ Skill: " + skillID + " (กด F/X เพื่อเริ่ม QTE)");
    }

    private IEnumerator SpawnLettersWithAnimation()
    {
        for (int i = 0; i < letters.Count; i++)
        {
            GameObject go = Instantiate(letterPrefab, mainCanvas.transform);
            go.transform.localScale = Vector3.one;

            // ตั้งตำแหน่งทันทีก่อนแสดงผล
            Vector3 offset;
            if (customOffsets != null && i < customOffsets.Length)
            {
                offset = customOffsets[i];
            }
            else
            {
                offset = new Vector3(i * letterSpacing, 0f, 0f);
            }
            Vector3 screenPos = Camera.main.WorldToScreenPoint(uiAnchor.position) + offset;
            go.transform.position = screenPos;

            SkillLetterUI ui = go.GetComponent<SkillLetterUI>();
            if (ui != null)
            {
                QTEManager qte = FindObjectOfType<QTEManager>();
                if (qte != null)
                    ui.iconDB = qte.iconDB;

                ui.SetLetter(letters[i]);

                letterUIs.Add(go);
                letterImages.Add(ui);

                StartCoroutine(AnimateLetter(go, i));
            }
            else
            {
                Debug.LogError("SkillLetterUI not found on letterPrefab!");
            }

            yield return new WaitForSeconds(letterDelay);
        }
    }

    private IEnumerator AnimateLetter(GameObject letter, int index)
    {
        if (letter == null) yield break;

        CanvasGroup canvasGroup = letter.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = letter.AddComponent<CanvasGroup>();

        RectTransform rect = letter.GetComponent<RectTransform>();
        Vector3 originalScale = rect.localScale;
        Vector3 originalPos = rect.localPosition;

        float elapsed = 0f;

        switch (animationType)
        {
            case AnimationType.Fade:
                canvasGroup.alpha = 0f;
                break;

            case AnimationType.Scale:
                rect.localScale = Vector3.zero;
                break;

            case AnimationType.Both:
                canvasGroup.alpha = 0f;
                rect.localScale = Vector3.zero;
                break;

            case AnimationType.SlideDown:
                canvasGroup.alpha = 0f;
                rect.localPosition = originalPos + new Vector3(0, 100f, 0);
                break;

            case AnimationType.Pop:
                rect.localScale = Vector3.zero;
                break;

            case AnimationType.None:
                yield break;
        }

        while (elapsed < fadeInDuration)
        {
            if (letter == null || canvasGroup == null || rect == null)
                yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;

            switch (animationType)
            {
                case AnimationType.Fade:
                    canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                    break;

                case AnimationType.Scale:
                    rect.localScale = Vector3.Lerp(Vector3.zero, originalScale, t);
                    break;

                case AnimationType.Both:
                    canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                    rect.localScale = Vector3.Lerp(Vector3.zero, originalScale, t);
                    break;

                case AnimationType.SlideDown:
                    canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                    rect.localPosition = Vector3.Lerp(originalPos + new Vector3(0, 100f, 0), originalPos, t);
                    break;

                case AnimationType.Pop:
                    float elasticT = Mathf.Sin(t * Mathf.PI * 0.5f);
                    float overshoot = 1f + (Mathf.Sin(t * Mathf.PI * 2f) * 0.2f * (1f - t));
                    rect.localScale = originalScale * elasticT * overshoot;
                    break;
            }

            yield return null;
        }

        if (letter != null && canvasGroup != null && rect != null)
        {
            canvasGroup.alpha = 1f;
            rect.localScale = originalScale;
            rect.localPosition = originalPos;
        }
    }

    private void StartQTE()
    {
        QTEManager qte = FindObjectOfType<QTEManager>();
        if (qte != null)
        {
            List<KeyCode> keySequence = new List<KeyCode>();
            foreach (char c in originalLetters)
            {
                if (System.Enum.TryParse(c.ToString().ToUpper(), out KeyCode code))
                {
                    keySequence.Add(code);
                }
            }

            Debug.Log("🎮 Final QTE sequence count = " + keySequence.Count);
            qte.StartQTE(keySequence);
        }
    }

    public void RemoveOneLetterUI()
    {
        if (letterUIs.Count == 0) return;

        StartCoroutine(FadeOutAndDestroy(letterUIs[0]));

        letterUIs.RemoveAt(0);
        letterImages.RemoveAt(0);

        if (letterUIs.Count == 0)
        {
            isActive = false;
            isQTERunning = false;
            Debug.Log("✅ QTE เสร็จสิ้น - ทุก Letter UI ถูกลบแล้ว (🔓 ปลดล็อคปุ่ม T)");
        }
    }

    // ⚠️ ถูกเรียกเมื่อกดผิด → ลบตัวอักษรบนหัว + ปลดล็อคปุ่ม T
    public void OnQTEFailed()
    {
        Debug.Log("🔴 OnQTEFailed() ถูกเรียก!");

        // 🛑 หยุด Coroutine ทั้งหมด
        StopAllCoroutines();

        // ❌ ลบตัวอักษรทั้งหมดบนหัวทันที
        foreach (var ui in letterUIs)
        {
            if (ui != null)
            {
                Destroy(ui);
                Debug.Log("🗑️ ลบ Letter UI");
            }
        }
        letterUIs.Clear();
        letterImages.Clear();

        // 🔄 รีเซ็ตข้อมูล
        remaining.Clear();
        letters.Clear();
        originalLetters.Clear();

        // 🔓 ปลดล็อคปุ่ม T ให้กดใหม่ได้
        isQTERunning = false;
        qteStarted = false;
        isActive = false;
        currentIndex = 0;
        timer = 0f;

        Debug.Log("❌ QTE ล้มเหลว - ลบตัวอักษรและหยุดกระพริบแล้ว (🔓 กด T ได้แล้ว)");
    }

    private IEnumerator FadeOutAndDestroy(GameObject letter)
    {
        if (letter == null) yield break;

        CanvasGroup canvasGroup = letter.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = letter.AddComponent<CanvasGroup>();

        RectTransform rect = letter.GetComponent<RectTransform>();
        Vector3 startScale = rect.localScale;

        float elapsed = 0f;
        float duration = 0.2f;

        while (elapsed < duration)
        {
            if (letter == null || canvasGroup == null || rect == null)
                yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            rect.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            yield return null;
        }

        if (letter != null)
            Destroy(letter);
    }

    public bool IsQTERunning()
    {
        return isQTERunning;
    }

    public bool IsActive()
    {
        return isActive;
    }

    public void ResetQTE()
    {
        foreach (var ui in letterUIs)
        {
            if (ui != null) Destroy(ui);
        }

        letterUIs.Clear();
        letterImages.Clear();

        isActive = false;
        qteStarted = false;
        isQTERunning = false;
        currentIndex = 0;
        timer = 0f;

        Debug.Log("🔄 รีเซ็ต SkillLetterSelector (🔓 ปลดล็อคปุ่ม T)");
    }
}