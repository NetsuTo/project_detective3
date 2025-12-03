using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Text.RegularExpressions;

public class AutoBubbleDialog : MonoBehaviour
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
    public Font customFont; // สำหรับ UI.Text
    public TMP_FontAsset customFontTMP; // สำหรับ TextMeshPro

    [Header("การแสดงผล")]
    public float displayDuration = 3f;
    public float typingSpeed = 0.05f;
    public float detectionRange = 2.5f;
    public bool playOnce = false;

    [Header("UI")]
    public GameObject bubblePrefab;

    private GameObject bubbleInstance;
    private Text bubbleText;
    private TextMeshProUGUI bubbleTextTMP;
    private bool isShowingDialog = false;
    private int currentLineIndex = 0;
    private Canvas mainCanvas;
    private float floatTimer = 0f;
    private bool hasPlayed = false;

    void Start()
    {
        mainCanvas = FindObjectOfType<Canvas>();
        if (mainCanvas == null)
        {
            Debug.LogError("? ไม่พบ Canvas! กรุณาสร้าง Canvas (UI -> Canvas)");
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

        Debug.Log($"? AutoBubbleDialog พร้อมใช้งาน - มี {dialogLines.Length} ข้อความ");

        if (enableHighlight && highlightWords.Length > 0)
        {
            Debug.Log($"?? เปิดใช้งานการเน้นคำ: {string.Join(", ", highlightWords)}");
        }
    }

    void Update()
    {
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

                    // ? เช็คว่าผู้เล่นอยู่หน้ากล้อง
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

                    // ? เพิ่มการเช็คว่าหัวผู้เล่นสูงกว่าบับเบิ้ลหรือไม่
                    Vector3 playerHeadPos = player.transform.position + Vector3.up * 2f; // ? ปรับค่านี้ตามความสูงของตัวละคร
                    Vector3 playerHeadScreenPos = Camera.main.WorldToScreenPoint(playerHeadPos);

                    // ถ้าหัวผู้เล่นใกล้บับเบิ้ล (กระโดด) ให้ซ่อนบับเบิ้ล
                    if (playerHeadScreenPos.z > 0)
                    {
                        float headDistance = Vector2.Distance(
                            new Vector2(screenPos.x, screenPos.y),
                            new Vector2(playerHeadScreenPos.x, playerHeadScreenPos.y)
                        );

                        // ? ถ้าหัวใกล้บับเบิ้ลเกินไป ให้ซ่อน
                        if (headDistance < 200f)
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
        if (dialogLines == null || dialogLines.Length == 0)
        {
            Debug.LogError("?? ไม่มีข้อความ!");
            return;
        }

        if (playOnce && hasPlayed)
        {
            Debug.Log("?? เล่นไปแล้ว (Play Once)");
            return;
        }

        isShowingDialog = true;
        currentLineIndex = 0;
        hasPlayed = true;

        CreateBubble();

        // ? เช็คว่าสร้างบับเบิ้ลสำเร็จหรือไม่
        if (bubbleInstance == null)
        {
            Debug.LogError("?? ไม่สามารถสร้างบับเบิ้ลได้! ยกเลิกการแสดงบทสนทนา");
            isShowingDialog = false;
            return;
        }

        // ? เช็คว่ามี Text Component หรือไม่
        if (bubbleText == null && bubbleTextTMP == null)
        {
            Debug.LogError("?? ไม่พบ Text Component! ยกเลิกการแสดงบทสนทนา");
            isShowingDialog = false;
            if (bubbleInstance != null)
            {
                Destroy(bubbleInstance);
            }
            return;
        }

        StartCoroutine(TypeText(dialogLines[currentLineIndex]));
    }

    void CreateBubble()
    {
        // ? ตรวจสอบ Canvas ก่อนทำอะไร
        if (mainCanvas == null)
        {
            mainCanvas = FindObjectOfType<Canvas>();
            if (mainCanvas == null)
            {
                Debug.LogError("?? ไม่พบ Canvas! กรุณาสร้าง Canvas ในฉาก (GameObject -> UI -> Canvas)");
                return;
            }
        }

        // ? ทำลาย bubbleInstance เก่าถ้ามี
        if (bubbleInstance != null)
        {
            Destroy(bubbleInstance);
            bubbleInstance = null;
        }

        try
        {
            if (bubblePrefab != null)
            {
                // ใช้ Prefab
                bubbleInstance = Instantiate(bubblePrefab, mainCanvas.transform);

                if (bubbleInstance == null)
                {
                    Debug.LogError("?? ไม่สามารถสร้าง bubbleInstance จาก Prefab ได้!");
                    return;
                }

                // ? เพิ่ม Canvas Component ให้บับเบิ้ล
                Canvas bubbleCanvas = bubbleInstance.GetComponent<Canvas>();
                if (bubbleCanvas == null)
                {
                    bubbleCanvas = bubbleInstance.AddComponent<Canvas>();
                }

                if (bubbleCanvas != null)
                {
                    bubbleCanvas.overrideSorting = true;
                    bubbleCanvas.sortingOrder = -1;
                }

                // เพิ่ม GraphicRaycaster ถ้ายังไม่มี
                if (bubbleInstance.GetComponent<GraphicRaycaster>() == null)
                {
                    bubbleInstance.AddComponent<GraphicRaycaster>();
                }

                // หา Text Component
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
                // สร้างบับเบิ้ลใหม่ทั้งหมด
                bubbleInstance = new GameObject("SpeechBubble");

                if (bubbleInstance == null)
                {
                    Debug.LogError("?? ไม่สามารถสร้าง GameObject ได้!");
                    return;
                }

                bubbleInstance.transform.SetParent(mainCanvas.transform, false);

                // เพิ่ม Canvas Component
                Canvas bubbleCanvas = bubbleInstance.AddComponent<Canvas>();
                if (bubbleCanvas != null)
                {
                    bubbleCanvas.overrideSorting = true;
                    bubbleCanvas.sortingOrder = -1;
                }

                // เพิ่ม GraphicRaycaster
                GraphicRaycaster raycaster = bubbleInstance.AddComponent<GraphicRaycaster>();

                // เพิ่ม RectTransform
                RectTransform rectTransform = bubbleInstance.GetComponent<RectTransform>();
                if (rectTransform == null)
                {
                    rectTransform = bubbleInstance.AddComponent<RectTransform>();
                }

                if (rectTransform != null)
                {
                    rectTransform.sizeDelta = bubbleSize;
                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                }

                // เพิ่ม Background Image
                Image bg = bubbleInstance.AddComponent<Image>();
                if (bg != null)
                {
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
                }

                // สร้าง Text Object
                GameObject textObj = new GameObject("Text");
                if (textObj != null)
                {
                    textObj.transform.SetParent(bubbleInstance.transform, false);

                    RectTransform textRect = textObj.AddComponent<RectTransform>();
                    if (textRect != null)
                    {
                        textRect.anchorMin = Vector2.zero;
                        textRect.anchorMax = Vector2.one;
                        textRect.offsetMin = new Vector2(padding, padding + textVerticalOffset);
                        textRect.offsetMax = new Vector2(-padding, -padding + textVerticalOffset);
                    }

                    // สร้าง TextMeshPro หรือ Text ธรรมดา
                    if (useTextMeshPro)
                    {
                        bubbleTextTMP = textObj.AddComponent<TextMeshProUGUI>();

                        if (bubbleTextTMP != null)
                        {
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
                    }
                    else
                    {
                        bubbleText = textObj.AddComponent<Text>();

                        if (bubbleText != null)
                        {
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

                                if (defaultFont != null)
                                {
                                    bubbleText.font = defaultFont;
                                    Debug.Log("?? ใช้ฟอนต์ default (Arial)");
                                }
                            }

                            bubbleText.fontSize = fontSize;
                            bubbleText.color = textColor;
                            bubbleText.alignment = textAlignment;
                            bubbleText.horizontalOverflow = HorizontalWrapMode.Wrap;
                            bubbleText.verticalOverflow = VerticalWrapMode.Overflow;
                            bubbleText.supportRichText = true;
                        }
                    }
                }
            }

            // เปิดใช้งาน Rich Text
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

            Debug.Log($"? สร้างบับเบิ้ลสำเร็จ!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"?? เกิด Error ตอนสร้างบับเบิ้ล: {e.Message}\n{e.StackTrace}");

            // ทำลาย bubbleInstance ถ้ามีปัญหา
            if (bubbleInstance != null)
            {
                Destroy(bubbleInstance);
                bubbleInstance = null;
            }
        }
    }

    IEnumerator TypeText(string text)
    {
        if (bubbleText == null && bubbleTextTMP == null)
        {
            Debug.LogError("? ไม่มี Text Component!");
            yield break;
        }

        // ?? ใช้ Highlight กับข้อความ
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

        // แสดงข้อความทีละตัว
        string currentText = "";

        for (int i = 0; i < highlightedText.Length; i++)
        {
            currentText += highlightedText[i];

            // ข้าม Rich Text tags
            if (highlightedText[i] == '<')
            {
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

        if (bubbleInstance != null)
        {
            Destroy(bubbleInstance);
        }

        Debug.Log("? จบบทสนทนา");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isShowingDialog)
        {
            StartDialog();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // ปิดบทสนทนาถ้าเดินออกไป (ถ้าต้องการ)
            // if (isShowingDialog)
            // {
            //     EndDialog();
            // }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }

    public void ResetDialog()
    {
        hasPlayed = false;
        Debug.Log("?? รีเซ็ต AutoBubbleDialog");
    }
}