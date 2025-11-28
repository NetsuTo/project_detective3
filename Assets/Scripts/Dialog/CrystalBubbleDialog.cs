using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Text.RegularExpressions;

public class CrystalBubbleDialog : MonoBehaviour
{
    [Header("บทสนทนา")]
    [TextArea(2, 5)]
    public string[] dialogLines;

    [Header("?? การเน้นคำ (Highlight)")]
    public bool enableHighlight = true;
    [Tooltip("คำที่ต้องการเน้น (case-insensitive)")]
    public string[] highlightWords;
    public Color highlightColor = Color.yellow;
    public bool highlightBold = true;
    [Range(0, 50)]
    public int highlightSizeIncrease = 0;
    public bool useTextMeshPro = true;

    [Header("การตั้งค่าตำแหน่ง")]
    public float bubbleHeight = 2f;
    public Vector2 bubbleOffset = Vector2.zero;
    public float floatAmplitude = 0.3f;
    public float floatSpeed = 2f;

    [Header("ขนาดและสี")]
    public Vector2 bubbleSize = new Vector2(400, 120);
    public int fontSize = 24;
    public Color textColor = Color.black;
    public Color bubbleColor = new Color(1f, 1f, 1f, 0.9f);
    public Sprite bubbleSprite;
    public float padding = 15f;
    public TextAnchor textAlignment = TextAnchor.MiddleCenter;
    public float textVerticalOffset = 0f;

    [Header("?? ฟอนต์")]
    [Tooltip("ลากฟอนต์จาก Project มาใส่ตรงนี้")]
    public Font customFont;
    public TMP_FontAsset customFontTMP;

    [Header("การแสดงผล")]
    public float displayDuration = 3f;
    public float typingSpeed = 0.05f;
    public float detectionRange = 2.5f;

    [Header("UI")]
    public GameObject bubblePrefab;
    public GameObject pressEIndicator;

    private GameObject bubbleInstance;
    private Text bubbleText;
    private TextMeshProUGUI bubbleTextTMP;
    private bool playerInRange = false;
    private bool isShowingDialog = false;
    private int currentLineIndex = 0;
    private Canvas mainCanvas;
    private float floatTimer = 0f;
    private PlayerController playerController;
    private Animator playerAnimator;
    private bool isTyping = false;
    private SimplePatrol npcPatrol;

    // ===== Input System Actions - รองรับ Keyboard + Gamepad =====
    private InputAction interactAction;
    private InputAction continueAction;

    void Awake()
    {
        // สร้าง Input Actions
        SetupInputActions();
        interactAction?.Enable();
        continueAction?.Enable();

        Debug.Log("? CrystalBubbleDialog - Input System Ready (Keyboard + Gamepad)!");
    }

    private void SetupInputActions()
    {
        // ===== Interact (E / Button North) สำหรับเริ่มบทสนทนา =====
        interactAction = new InputAction("Interact", type: InputActionType.Button);
        interactAction.AddBinding("<Keyboard>/e");
        interactAction.AddBinding("<Gamepad>/buttonNorth");  // Xbox: Y, PS: Triangle
        interactAction.performed += OnInteractPerformed;

        // ===== Continue (Space / Button South) สำหรับข้ามข้อความ =====
        continueAction = new InputAction("Continue", type: InputActionType.Button);
        continueAction.AddBinding("<Keyboard>/space");
        continueAction.AddBinding("<Gamepad>/buttonSouth");  // Xbox: A, PS: Cross
        continueAction.performed += OnContinuePerformed;
    }

    void Start()
    {
        mainCanvas = FindObjectOfType<Canvas>();
        if (mainCanvas == null)
        {
            Debug.LogError("? ไม่พบ Canvas! กรุณาสร้าง Canvas (UI -> Canvas)");
        }

        if (pressEIndicator != null)
            pressEIndicator.SetActive(false);

        npcPatrol = GetComponent<SimplePatrol>();
        if (npcPatrol == null)
        {
            Debug.LogWarning("?? ไม่พบ SimplePatrol! Crystal จะไม่หยุดเดินตอนคุย");
        }

        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning("?? ไม่พบ Collider! กำลังสร้าง SphereCollider...");
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = detectionRange;
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning("?? Collider ต้องเปิด Is Trigger!");
            col.isTrigger = true;
        }

        if (dialogLines.Length == 0)
        {
            Debug.LogWarning("?? ไม่มีข้อความ! กรุณาเพิ่มใน Dialog Lines");
        }

        Debug.Log($"? CrystalBubbleDialog พร้อมใช้งาน - มี {dialogLines.Length} ข้อความ");

        if (enableHighlight && highlightWords.Length > 0)
        {
            Debug.Log($"?? เปิดใช้งานการเน้นคำ: {string.Join(", ", highlightWords)}");
        }
    }

    void Update()
    {
        // ? Fallback สำหรับ Old Input System
        if (Keyboard.current == null && Gamepad.current == null)
        {
            if (playerInRange && Input.GetKeyDown(KeyCode.E) && !isShowingDialog)
            {
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
        }

        // อัพเดทตำแหน่ง Bubble
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

    private void OnEnable()
    {
        interactAction?.Enable();
        continueAction?.Enable();
    }

    private void OnDisable()
    {
        interactAction?.Disable();
        continueAction?.Disable();
    }

    // ===== Input Actions Callbacks =====
    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        if (!playerInRange || isShowingDialog) return;

        Debug.Log("?? กด Interact (E / Y/Triangle) - เริ่มบทสนทนากับ Crystal");
        StartDialog();
    }

    private void OnContinuePerformed(InputAction.CallbackContext ctx)
    {
        if (!isShowingDialog) return;

        Debug.Log("? กด Continue (Space / A/Cross)");

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

    // ?? ฟังก์ชันเน้นคำในข้อความ
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

            string pattern = $@"\b({Regex.Escape(word)})\b";

            string replacement = "";

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
            Debug.LogError("? ไม่มีข้อความ!");
            return;
        }

        isShowingDialog = true;
        currentLineIndex = 0;

        if (playerController != null)
        {
            playerController.enabled = false;
            Debug.Log("?? ล็อค Player ไม่ให้เดิน");
        }

        if (playerAnimator != null)
        {
            playerAnimator.SetFloat("Speed", 0f);
            Debug.Log("?? หยุดอนิเมชั่น Player");
        }

        if (npcPatrol != null)
        {
            npcPatrol.PausePatrol();
            Debug.Log("?? หยุด Crystal NPC");
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
                Debug.LogError("? ไม่พบ Canvas!");
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
                    Debug.LogWarning("?? ไม่พบ TextMeshPro ใน Prefab! จะใช้ UI.Text แทน");
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

            if (useTextMeshPro)
            {
                bubbleTextTMP = textObj.AddComponent<TextMeshProUGUI>();

                if (customFontTMP != null)
                {
                    bubbleTextTMP.font = customFontTMP;
                    Debug.Log($"? ใช้ฟอนต์ TMP: {customFontTMP.name}");
                }

                bubbleTextTMP.fontSize = fontSize;
                bubbleTextTMP.color = textColor;
                bubbleTextTMP.alignment = TextAlignmentOptions.Center;
                bubbleTextTMP.enableWordWrapping = true;
                bubbleTextTMP.overflowMode = TextOverflowModes.Overflow;
                bubbleTextTMP.richText = true;
            }
            else
            {
                bubbleText = textObj.AddComponent<Text>();

                if (customFont != null)
                {
                    bubbleText.font = customFont;
                    Debug.Log($"? ใช้ฟอนต์: {customFont.name}");
                }
                else
                {
                    Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (defaultFont == null)
                    {
                        defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    }
                    bubbleText.font = defaultFont;
                    Debug.Log("?? ใช้ฟอนต์ default (Arial)");
                }

                bubbleText.fontSize = fontSize;
                bubbleText.color = textColor;
                bubbleText.alignment = textAlignment;
                bubbleText.horizontalOverflow = HorizontalWrapMode.Wrap;
                bubbleText.verticalOverflow = VerticalWrapMode.Overflow;
                bubbleText.supportRichText = true;
            }
        }

        if (useTextMeshPro && bubbleTextTMP != null)
        {
            bubbleTextTMP.richText = true;
            Debug.Log("? เปิด Rich Text สำหรับ TextMeshPro");
        }
        else if (bubbleText != null)
        {
            bubbleText.supportRichText = true;
            Debug.Log("? เปิด Rich Text สำหรับ UI.Text");
        }

        Debug.Log("? สร้างบับเบิ้ลสำเร็จ");
    }

    IEnumerator TypeText(string text)
    {
        if (bubbleText == null && bubbleTextTMP == null)
        {
            Debug.LogError("? ไม่มี Text Component!");
            yield break;
        }

        isTyping = true;

        string highlightedText = ApplyHighlight(text);

        if (useTextMeshPro && bubbleTextTMP != null)
        {
            bubbleTextTMP.text = "";
        }
        else if (bubbleText != null)
        {
            bubbleText.text = "";
        }

        string currentText = "";

        for (int i = 0; i < highlightedText.Length; i++)
        {
            currentText += highlightedText[i];

            if (highlightedText[i] != '<')
            {
                // ตัวอักษรปกติ
            }
            else
            {
                // ข้าม tag
                while (i < highlightedText.Length && highlightedText[i] != '>')
                {
                    i++;
                    if (i < highlightedText.Length)
                        currentText += highlightedText[i];
                }
            }

            if (useTextMeshPro && bubbleTextTMP != null)
            {
                bubbleTextTMP.text = currentText;
            }
            else if (bubbleText != null)
            {
                bubbleText.text = currentText;
            }

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

        if (playerController != null)
        {
            playerController.enabled = true;
            Debug.Log("?? ปลดล็อค Player ให้เดินได้แล้ว");
        }

        if (npcPatrol != null)
        {
            npcPatrol.ResumePatrol();
            Debug.Log("?? Crystal NPC เดินต่อ");
        }

        if (bubbleInstance != null)
        {
            Destroy(bubbleInstance);
        }

        if (playerInRange && pressEIndicator != null)
        {
            pressEIndicator.SetActive(true);
        }

        Debug.Log("? จบบทสนทนา");
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
                pressEIndicator.SetActive(true);

            Debug.Log("?? Player เข้ามาในระยะ Crystal Dialog");
            Debug.Log("?? กด E / Y(Triangle) เพื่อคุย | Space / A(Cross) เพื่อข้าม");
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

            Debug.Log("?? Player ออกจากระยะ Crystal Dialog");
        }
    }

    private void OnDestroy()
    {
        if (interactAction != null)
        {
            interactAction.performed -= OnInteractPerformed;
            interactAction.Dispose();
        }
        if (continueAction != null)
        {
            continueAction.performed -= OnContinuePerformed;
            continueAction.Dispose();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}