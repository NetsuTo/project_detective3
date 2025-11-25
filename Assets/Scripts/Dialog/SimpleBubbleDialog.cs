using UnityEngine;
using UnityEngine.UI;
using TMPro; // เพิ่ม TextMeshPro
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class SimpleBubbleDialog : MonoBehaviour
{
    [Header("บทสนทนา")]
    [TextArea(2, 5)]
    public string[] dialogLines;

    [Header("🎨 การเน้นคำ (Highlight)")]
    public bool enableHighlight = true;
    [Tooltip("คำที่ต้องการเน้น (case-insensitive)")]
    public string[] highlightWords;
    public Color highlightColor = Color.yellow;
    public bool highlightBold = true;
    [Range(0, 50)]
    public int highlightSizeIncrease = 0;
    public bool useTextMeshPro = true; // เลือกใช้ TMP หรือ Text ธรรมดา

    [Header("🔒 เงื่อนไขการปลดล็อค")]
    public TargetZone requiredZone;
    public int requiredCompletedCount = 2;
    public bool startLocked = true;

    [Header("🔄 การคุยซ้ำ")]
    [Tooltip("ติ๊กถ้าต้องการให้คุยได้แค่ครั้งเดียว / ปิดถ้าต้องการคุยได้ตลอด")]
    public bool dialogOnlyOnce = false;

    [Header("การตั้งค่าตำแหน่ง")]
    public float bubbleHeight = 2f;
    public Vector2 bubbleOffset = Vector2.zero;
    public float floatAmplitude = 0.3f;
    public float floatSpeed = 2f;
    public bool alwaysInFront = true;

    [Header("ขนาดและสี")]
    public Vector2 bubbleSize = new Vector2(400, 120);
    public int fontSize = 24;
    public Color textColor = Color.black;
    public Color bubbleColor = new Color(1f, 1f, 1f, 0.9f);
    public Sprite bubbleSprite;
    public float padding = 15f;
    public TextAnchor textAlignment = TextAnchor.MiddleCenter;
    public float textVerticalOffset = 0f;

    [Header("🔤 ฟอนต์")]
    [Tooltip("ลากฟอนต์จาก Project มาใส่ตรงนี้")]
    public Font customFont; // สำหรับ UI.Text
    public TMP_FontAsset customFontTMP; // สำหรับ TextMeshPro

    [Header("การแสดงผล")]
    public float displayDuration = 3f;
    public float typingSpeed = 0.05f;
    public float detectionRange = 2.5f;

    [Header("UI")]
    public GameObject bubblePrefab;
    public GameObject pressEIndicator;

    [Header("Object ที่จะโผล่หลังคุยเสร็จ")]
    public GameObject[] objectsToSpawn;
    public bool activateObjects = true;
    public Transform spawnPoint;
    public bool spawnOnlyOnce = true;

    [Header("🎭 เอฟเฟคที่จะเปลี่ยน")]
    [Tooltip("เอฟเฟคเก่าที่จะปิดเมื่อปลดล็อค")]
    public GameObject[] oldEffects;
    [Tooltip("เอฟเฟคใหม่ที่จะเปิดเมื่อปลดล็อค")]
    public GameObject[] newEffects;

    private GameObject bubbleInstance;
    private Text bubbleText;
    private TextMeshProUGUI bubbleTextTMP; // เพิ่ม TMP
    private bool playerInRange = false;
    private bool isShowingDialog = false;
    private int currentLineIndex = 0;
    private Canvas mainCanvas;
    private float floatTimer = 0f;
    private PlayerController playerController;
    private Animator playerAnimator;
    private bool isTyping = false;
    private bool hasSpawnedObjects = false;
    private Collider myCollider;
    private bool isUnlocked = false;
    private bool hasDialogCompleted = false; // เก็บสถานะว่าคุยไปแล้วหรือยัง

    void Start()
    {
        mainCanvas = FindObjectOfType<Canvas>();
        if (mainCanvas == null)
        {
            Debug.LogError("❌ ไม่พบ Canvas! กรุณาสร้าง Canvas (UI -> Canvas)");
        }

        if (pressEIndicator != null)
            pressEIndicator.SetActive(false);

        myCollider = GetComponent<Collider>();
        if (myCollider == null)
        {
            Debug.LogWarning("⚠️ ไม่พบ Collider! กำลังสร้าง SphereCollider...");
            myCollider = gameObject.AddComponent<SphereCollider>();
            myCollider.isTrigger = true;
            ((SphereCollider)myCollider).radius = detectionRange;
        }
        else if (!myCollider.isTrigger)
        {
            Debug.LogWarning("⚠️ Collider ต้องเปิด Is Trigger!");
            myCollider.isTrigger = true;
        }

        if (startLocked && requiredZone != null)
        {
            myCollider.enabled = false;
            isUnlocked = false;
            Debug.Log($"🔒 เห็ดถูกล็อคไว้ (ต้องใช้ {requiredCompletedCount} สกิลก่อน)");
        }
        else
        {
            isUnlocked = true;
        }

        if (dialogLines.Length == 0)
        {
            Debug.LogWarning("⚠️ ไม่มีข้อความ! กรุณาเพิ่มใน Dialog Lines");
        }

        Debug.Log($"✅ SimpleBubbleDialog พร้อมใช้งาน - มี {dialogLines.Length} ข้อความ");

        if (enableHighlight && highlightWords.Length > 0)
        {
            Debug.Log($"🎨 เปิดใช้งานการเน้นคำ: {string.Join(", ", highlightWords)}");
        }
    }

    void Update()
    {
        if (!isUnlocked && requiredZone != null)
        {
            if (requiredZone.GetCompletedCount() >= requiredCompletedCount)
            {
                UnlockDialog();
            }
        }

        // ถ้าตั้งค่าให้คุยแค่ครั้งเดียว และคุยไปแล้ว ก็ไม่ให้เริ่มใหม่
        if (playerInRange && Input.GetKeyDown(KeyCode.E) && !isShowingDialog)
        {
            if (dialogOnlyOnce && hasDialogCompleted)
            {
                Debug.Log("⏭️ บทสนทนานี้เล่นไปแล้ว (ตั้งค่าให้เล่นแค่ครั้งเดียว)");
                return;
            }
            StartDialog();
        }

        if (isShowingDialog && Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
            {
                StopAllCoroutines();
                CompleteCurrentText();
            }
            else
            {
                NextLine();
            }
        }

        if (bubbleInstance != null && Camera.main != null)
        {
            floatTimer += Time.deltaTime * floatSpeed;
            float floatOffset = Mathf.Sin(floatTimer) * floatAmplitude;

            Vector3 worldPos = transform.position + Vector3.up * (bubbleHeight + floatOffset);
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            bool isBehindCamera = screenPos.z < 0;

            bool isBlockingPlayer = false;
            if (!isBehindCamera)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null && Camera.main != null)
                {
                    Vector3 playerScreenPos = Camera.main.WorldToScreenPoint(player.transform.position);

                    if (playerScreenPos.z > 0 && playerScreenPos.z < screenPos.z)
                    {
                        float distance = Vector2.Distance(
                            new Vector2(screenPos.x, screenPos.y),
                            new Vector2(playerScreenPos.x, playerScreenPos.y)
                        );

                        if (distance < 150f)
                        {
                            isBlockingPlayer = true;
                        }
                    }
                }
            }

            if (isBehindCamera || isBlockingPlayer)
            {
                bubbleInstance.SetActive(false);
            }
            else
            {
                bubbleInstance.SetActive(true);
                screenPos.x += bubbleOffset.x;
                screenPos.y += bubbleOffset.y;
                bubbleInstance.transform.position = screenPos;
            }
        }
    }

    void UnlockDialog()
    {
        isUnlocked = true;
        if (myCollider != null)
        {
            myCollider.enabled = true;
        }

        // 🎭 เปลี่ยนเอฟเฟค
        SwitchEffects();

        Debug.Log($"🔓 ปลดล็อคเห็ดแล้ว! (ใช้สกิลครบ {requiredCompletedCount} ตัว)");
    }

    // 🎭 ฟังก์ชันเปลี่ยนเอฟเฟค
    void SwitchEffects()
    {
        // ปิดเอฟเฟคเก่า
        if (oldEffects != null && oldEffects.Length > 0)
        {
            foreach (GameObject effect in oldEffects)
            {
                if (effect != null)
                {
                    effect.SetActive(false);
                    Debug.Log($"❌ ปิดเอฟเฟคเก่า: {effect.name}");
                }
            }
        }

        // เปิดเอฟเฟคใหม่
        if (newEffects != null && newEffects.Length > 0)
        {
            foreach (GameObject effect in newEffects)
            {
                if (effect != null)
                {
                    effect.SetActive(true);
                    Debug.Log($"✨ เปิดเอฟเฟคใหม่: {effect.name}");
                }
            }
        }
    }

    // 🎨 ฟังก์ชันเน้นคำในข้อความ
    string ApplyHighlight(string text)
    {
        if (!enableHighlight || highlightWords == null || highlightWords.Length == 0)
        {
            return text;
        }

        string result = text;
        string hexColor = ColorUtility.ToHtmlStringRGB(highlightColor);

        foreach (string word in highlightWords)
        {
            if (string.IsNullOrEmpty(word)) continue;

            // หาคำที่ตรงกัน (case-insensitive)
            string pattern = $@"\b({Regex.Escape(word)})\b";

            string replacement = "";

            // ใช้ tag ที่เหมาะสมกับ TMP หรือ Text
            if (useTextMeshPro)
            {
                replacement = $"<color=#{hexColor}>";
                if (highlightBold) replacement += "<b>";
                if (highlightSizeIncrease > 0) replacement += $"<size={fontSize + highlightSizeIncrease}>";

                replacement += "$1";

                if (highlightSizeIncrease > 0) replacement += "</size>";
                if (highlightBold) replacement += "</b>";
                replacement += "</color>";
            }
            else
            {
                // สำหรับ UI.Text ธรรมดา - ใช้แค่ color กับ bold
                replacement = $"<color=#{hexColor}>";
                if (highlightBold) replacement += "<b>";
                replacement += "$1";
                if (highlightBold) replacement += "</b>";
                replacement += "</color>";
            }

            result = Regex.Replace(result, pattern, replacement, RegexOptions.IgnoreCase);
        }

        return result;
    }

    void StartDialog()
    {
        if (dialogLines.Length == 0)
        {
            Debug.LogError("❌ ไม่มีข้อความ!");
            return;
        }

        isShowingDialog = true;
        currentLineIndex = 0;

        if (playerController != null)
        {
            playerController.enabled = false;
            Debug.Log("🔒 ล็อค Player ไม่ให้เดิน");
        }

        if (playerAnimator != null)
        {
            playerAnimator.SetFloat("Speed", 0f);
            Debug.Log("⏸️ หยุดอนิเมชั่น Player");
        }

        if (pressEIndicator != null)
            pressEIndicator.SetActive(false);

        CreateBubble();
        StartCoroutine(TypeText(dialogLines[currentLineIndex]));
    }

    void CreateBubble()
    {
        if (mainCanvas == null)
        {
            mainCanvas = FindObjectOfType<Canvas>();
            if (mainCanvas == null)
            {
                Debug.LogError("❌ ไม่พบ Canvas!");
                return;
            }
        }

        if (bubblePrefab != null)
        {
            bubbleInstance = Instantiate(bubblePrefab, mainCanvas.transform);

            if (useTextMeshPro)
            {
                bubbleTextTMP = bubbleInstance.GetComponentInChildren<TextMeshProUGUI>();
                if (bubbleTextTMP == null)
                {
                    Debug.LogWarning("⚠️ ไม่พบ TextMeshPro ใน Prefab! จะใช้ UI.Text แทน");
                    bubbleText = bubbleInstance.GetComponentInChildren<Text>();
                    useTextMeshPro = false;
                }
            }
            else
            {
                bubbleText = bubbleInstance.GetComponentInChildren<Text>();
            }
        }
        else
        {
            bubbleInstance = new GameObject("SpeechBubble");
            bubbleInstance.transform.SetParent(mainCanvas.transform, false);

            RectTransform rectTransform = bubbleInstance.AddComponent<RectTransform>();
            rectTransform.sizeDelta = bubbleSize;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            Image bg = bubbleInstance.AddComponent<Image>();

            if (bubbleSprite != null)
            {
                bg.sprite = bubbleSprite;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }
            else
            {
                bg.color = bubbleColor;
            }

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(bubbleInstance.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(padding, padding + textVerticalOffset);
            textRect.offsetMax = new Vector2(-padding, -padding + textVerticalOffset);

            // สร้าง TextMeshPro หรือ Text ธรรมดา
            if (useTextMeshPro)
            {
                bubbleTextTMP = textObj.AddComponent<TextMeshProUGUI>();

                // ใช้ฟอนต์ที่กำหนด หรือใช้ default
                if (customFontTMP != null)
                {
                    bubbleTextTMP.font = customFontTMP;
                    Debug.Log($"✅ ใช้ฟอนต์ TMP: {customFontTMP.name}");
                }

                bubbleTextTMP.fontSize = fontSize;
                bubbleTextTMP.color = textColor;
                bubbleTextTMP.alignment = TextAlignmentOptions.Center;
                bubbleTextTMP.enableWordWrapping = true;
                bubbleTextTMP.overflowMode = TextOverflowModes.Overflow;
                bubbleTextTMP.richText = true; // สำคัญมาก!
            }
            else
            {
                bubbleText = textObj.AddComponent<Text>();

                // ใช้ฟอนต์ที่กำหนด หรือใช้ default
                if (customFont != null)
                {
                    bubbleText.font = customFont;
                    Debug.Log($"✅ ใช้ฟอนต์: {customFont.name}");
                }
                else
                {
                    // ใช้ฟอนต์ default
                    Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (defaultFont == null)
                    {
                        defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    }
                    bubbleText.font = defaultFont;
                    Debug.Log("⚠️ ใช้ฟอนต์ default (Arial)");
                }

                bubbleText.fontSize = fontSize;
                bubbleText.color = textColor;
                bubbleText.alignment = textAlignment;
                bubbleText.horizontalOverflow = HorizontalWrapMode.Wrap;
                bubbleText.verticalOverflow = VerticalWrapMode.Overflow;
                bubbleText.supportRichText = true; // สำคัญมาก!
            }
        }

        // 🎨 เปิดใช้งาน Rich Text (สำคัญมาก!)
        if (useTextMeshPro && bubbleTextTMP != null)
        {
            bubbleTextTMP.richText = true;
            Debug.Log("✅ เปิด Rich Text สำหรับ TextMeshPro");
        }
        else if (bubbleText != null)
        {
            bubbleText.supportRichText = true;
            Debug.Log("✅ เปิด Rich Text สำหรับ UI.Text");
        }

        Debug.Log("✅ สร้างบับเบิ้ลสำเร็จ");
    }

    IEnumerator TypeText(string text)
    {
        // เช็คว่ามี Text Component ไหม
        if (bubbleText == null && bubbleTextTMP == null)
        {
            Debug.LogError("❌ ไม่มี Text Component!");
            yield break;
        }

        isTyping = true;

        // 🎨 ใช้ Highlight กับข้อความ
        string highlightedText = ApplyHighlight(text);

        // ล้างข้อความเดิม
        if (useTextMeshPro && bubbleTextTMP != null)
        {
            bubbleTextTMP.text = "";
        }
        else if (bubbleText != null)
        {
            bubbleText.text = "";
        }

        // แสดงข้อความทีละตัว (แบบง่าย - ไม่แยก tags)
        string currentText = "";
        int visibleCharCount = 0;

        for (int i = 0; i < highlightedText.Length; i++)
        {
            currentText += highlightedText[i];

            // นับเฉพาะตัวอักษรที่มองเห็น (ไม่นับ tags)
            if (highlightedText[i] != '<')
            {
                visibleCharCount++;
            }
            else
            {
                // ข้าม tag ไปเลย
                while (i < highlightedText.Length && highlightedText[i] != '>')
                {
                    i++;
                    if (i < highlightedText.Length)
                        currentText += highlightedText[i];
                }
            }

            // อัพเดทข้อความ
            if (useTextMeshPro && bubbleTextTMP != null)
            {
                bubbleTextTMP.text = currentText;
            }
            else if (bubbleText != null)
            {
                bubbleText.text = currentText;
            }

            // รอเฉพาะตัวอักษรที่มองเห็น
            if (highlightedText[i] != '<' && highlightedText[i] != '>')
            {
                yield return new WaitForSeconds(typingSpeed);
            }
        }

        isTyping = false;
        yield return new WaitForSeconds(displayDuration);

        if (isShowingDialog)
        {
            NextLine();
        }
    }

    void CompleteCurrentText()
    {
        if (currentLineIndex < dialogLines.Length)
        {
            // 🎨 ใช้ Highlight กับข้อความเต็ม
            string fullText = ApplyHighlight(dialogLines[currentLineIndex]);

            if (useTextMeshPro && bubbleTextTMP != null)
            {
                bubbleTextTMP.text = fullText;
            }
            else if (bubbleText != null)
            {
                bubbleText.text = fullText;
            }

            isTyping = false;
            StartCoroutine(WaitAfterComplete());
        }
    }

    IEnumerator WaitAfterComplete()
    {
        yield return new WaitForSeconds(displayDuration);

        if (isShowingDialog)
        {
            NextLine();
        }
    }

    void NextLine()
    {
        StopAllCoroutines();
        currentLineIndex++;

        if (currentLineIndex < dialogLines.Length)
        {
            StartCoroutine(TypeText(dialogLines[currentLineIndex]));
        }
        else
        {
            EndDialog();
        }
    }

    void EndDialog()
    {
        isShowingDialog = false;
        hasDialogCompleted = true; // บันทึกว่าคุยไปแล้ว

        if (playerController != null)
        {
            playerController.enabled = true;
            Debug.Log("🔓 ปลดล็อค Player ให้เดินได้แล้ว");
        }

        if (bubbleInstance != null)
        {
            Destroy(bubbleInstance);
        }

        // ถ้าตั้งค่าให้คุยแค่ครั้งเดียว ก็ซ่อน Press E ตัวบ่งชี้
        if (playerInRange && pressEIndicator != null)
        {
            if (dialogOnlyOnce)
            {
                pressEIndicator.SetActive(false);
            }
            else
            {
                pressEIndicator.SetActive(true);
            }
        }

        SpawnObjects();
        Debug.Log("✅ จบบทสนทนา");
    }

    void SpawnObjects()
    {
        if (objectsToSpawn == null || objectsToSpawn.Length == 0)
        {
            return;
        }

        if (spawnOnlyOnce && hasSpawnedObjects)
        {
            Debug.Log("⏭️ Object โผล่ไปแล้ว ข้ามการโผล่ครั้งนี้");
            return;
        }

        foreach (GameObject obj in objectsToSpawn)
        {
            if (obj == null) continue;

            if (activateObjects)
            {
                obj.SetActive(true);
                Debug.Log($"✨ เปิด Object: {obj.name}");
            }
            else
            {
                Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;
                Quaternion spawnRot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

                GameObject newObj = Instantiate(obj, spawnPos, spawnRot);
                Debug.Log($"✨ สร้าง Object: {newObj.name}");
            }
        }

        hasSpawnedObjects = true;
        Debug.Log("✅ Object โผล่ครบแล้ว");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;

            if (playerController == null)
            {
                playerController = other.GetComponent<PlayerController>();
            }
            if (playerAnimator == null)
            {
                playerAnimator = other.GetComponent<Animator>();
            }

            if (pressEIndicator != null && !isShowingDialog)
            {
                // ถ้าตั้งค่าให้คุยแค่ครั้งเดียว และคุยไปแล้ว ก็ไม่แสดง Press E
                if (dialogOnlyOnce && hasDialogCompleted)
                {
                    pressEIndicator.SetActive(false);
                }
                else
                {
                    pressEIndicator.SetActive(true);
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;

            if (pressEIndicator != null)
                pressEIndicator.SetActive(false);

            if (isShowingDialog)
            {
                EndDialog();
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = isUnlocked ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}