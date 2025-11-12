using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AutoBubbleDialog : MonoBehaviour
{
    [Header("บทสนทนา")]
    [TextArea(2, 5)]
    public string[] dialogLines; // ข้อความที่จะแสดง

    [Header("การตั้งค่าตำแหน่ง")]
    public float bubbleHeight = 2f; // ความสูงของบับเบิ้ลเหนือหัว
    public Vector2 bubbleOffset = Vector2.zero; // เลื่อนตำแหน่งบับเบิล (X, Y)
    public float floatAmplitude = 0.3f; // ความสูงของการลอย (0 = ไม่ลอย)
    public float floatSpeed = 2f; // ความเร็วการลอย

    [Header("ขนาดและสี")]
    public Vector2 bubbleSize = new Vector2(400, 120); // ขนาดกล่องบับเบิล
    public int fontSize = 24; // ขนาดตัวอักษร
    public Color textColor = Color.black; // สีตัวอักษร
    public Color bubbleColor = new Color(1f, 1f, 1f, 0.9f); // สีพื้นหลังบับเบิล (ถ้าไม่มีรูป)
    public Sprite bubbleSprite; // รูปกรอบบับเบิล (ลากรูปมาใส่ตรงนี้)
    public float padding = 15f; // ระยะขอบข้อความ
    public TextAnchor textAlignment = TextAnchor.MiddleCenter; // ตำแหน่งข้อความ
    public float textVerticalOffset = 0f; // ขยับข้อความขึ้น(+) ลง(-) ภายในกล่อง

    [Header("การแสดงผล")]
    public float displayDuration = 3f; // ระยะเวลาแสดงแต่ละข้อความ (วินาที)
    public float typingSpeed = 0.05f; // ความเร็วพิมพ์ตัวอักษร
    public float detectionRange = 2.5f; // ระยะตรวจจับ Player
    public bool playOnce = false; // เล่นครั้งเดียวแล้วหยุด

    [Header("UI")]
    public GameObject bubblePrefab; // Prefab ของบับเบิ้ล (ถ้ามี)

    private GameObject bubbleInstance;
    private Text bubbleText;
    private bool isShowingDialog = false;
    private int currentLineIndex = 0;
    private Canvas mainCanvas;
    private float floatTimer = 0f;
    private bool hasPlayed = false; // เช็คว่าเล่นไปแล้วหรือยัง

    void Start()
    {
        // หา Canvas
        mainCanvas = FindObjectOfType<Canvas>();
        if (mainCanvas == null)
        {
            Debug.LogError("? ไม่พบ Canvas! กรุณาสร้าง Canvas (UI -> Canvas)");
        }

        // ตรวจสอบว่ามี Collider หรือไม่
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
    }

    void Update()
    {
        // อัพเดทตำแหน่งบับเบิ้ลให้ติดตัวตลอด
        if (bubbleInstance != null && Camera.main != null)
        {
            // เพิ่มการลอยขึ้นลง
            floatTimer += Time.deltaTime * floatSpeed;
            float floatOffset = Mathf.Sin(floatTimer) * floatAmplitude;

            Vector3 worldPos = transform.position + Vector3.up * (bubbleHeight + floatOffset);
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            // เช็คว่า NPC อยู่หลังกล้องหรือไม่
            bool isBehindCamera = screenPos.z < 0;

            // เช็คว่าบับเบิลจะทับ Player หรือไม่
            bool isBlockingPlayer = false;
            if (!isBehindCamera)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null && Camera.main != null)
                {
                    Vector3 playerScreenPos = Camera.main.WorldToScreenPoint(player.transform.position);

                    // ถ้า Player อยู่ระหว่างกล้องกับ NPC
                    if (playerScreenPos.z > 0 && playerScreenPos.z < screenPos.z)
                    {
                        // เช็คว่าตำแหน่งบนจอใกล้กันไหม
                        float distance = Vector2.Distance(
                            new Vector2(screenPos.x, screenPos.y),
                            new Vector2(playerScreenPos.x, playerScreenPos.y)
                        );

                        if (distance < 150f) // ถ้าใกล้เกินไป
                        {
                            isBlockingPlayer = true;
                        }
                    }
                }
            }

            if (isBehindCamera || isBlockingPlayer)
            {
                // ซ่อนบับเบิลถ้าอยู่หลังกล้องหรือทับ Player
                bubbleInstance.SetActive(false);
            }
            else
            {
                bubbleInstance.SetActive(true);

                // เพิ่ม offset
                screenPos.x += bubbleOffset.x;
                screenPos.y += bubbleOffset.y;

                bubbleInstance.transform.position = screenPos;
            }
        }
    }

    void StartDialog()
    {
        if (dialogLines.Length == 0)
        {
            Debug.LogError("? ไม่มีข้อความ!");
            return;
        }

        // ถ้าเล่นครั้งเดียวและเล่นไปแล้ว ไม่ต้องเล่นอีก
        if (playOnce && hasPlayed)
        {
            Debug.Log("?? เล่นไปแล้ว (Play Once)");
            return;
        }

        isShowingDialog = true;
        currentLineIndex = 0;
        hasPlayed = true;

        // สร้างบับเบิ้ล
        CreateBubble();

        // แสดงข้อความแรก
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
            // ใช้ Prefab ถ้ามี
            bubbleInstance = Instantiate(bubblePrefab, mainCanvas.transform);
            bubbleText = bubbleInstance.GetComponentInChildren<Text>();
        }
        else
        {
            // สร้างแบบอัตโนมัติ
            bubbleInstance = new GameObject("SpeechBubble");
            bubbleInstance.transform.SetParent(mainCanvas.transform, false);

            // เพิ่ม RectTransform
            RectTransform rectTransform = bubbleInstance.AddComponent<RectTransform>();
            rectTransform.sizeDelta = bubbleSize;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            // เพิ่มพื้นหลัง
            Image bg = bubbleInstance.AddComponent<Image>();

            // ใช้รูปที่ลากมาใส่ (ถ้ามี) ไม่งั้นใช้สี
            if (bubbleSprite != null)
            {
                bg.sprite = bubbleSprite;
                bg.type = Image.Type.Sliced; // ให้ยืดหดได้สวย
                bg.color = Color.white; // ไม่เปลี่ยนสีรูป
            }
            else
            {
                bg.color = bubbleColor; // ใช้สีธรรมดา
            }

            // สร้าง Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(bubbleInstance.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(padding, padding + textVerticalOffset);
            textRect.offsetMax = new Vector2(-padding, -padding + textVerticalOffset);

            bubbleText = textObj.AddComponent<Text>();

            // ลอง Load Font
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            bubbleText.font = font;
            bubbleText.fontSize = fontSize;
            bubbleText.color = textColor;
            bubbleText.alignment = textAlignment; // ใช้ค่าที่ตั้งได้
            bubbleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bubbleText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        Debug.Log("? สร้างบับเบิ้ลสำเร็จ");
    }

    IEnumerator TypeText(string text)
    {
        if (bubbleText == null)
        {
            Debug.LogError("? bubbleText เป็น null!");
            yield break;
        }

        bubbleText.text = "";

        // พิมพ์ทีละตัว
        foreach (char c in text)
        {
            bubbleText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        // รอก่อนไปข้อความถัดไป
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
            // แสดงข้อความถัดไป
            StartCoroutine(TypeText(dialogLines[currentLineIndex]));
        }
        else
        {
            // จบบทสนทนา
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
            // เดินผ่านแล้วเริ่มคุยทันที
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

    // แสดงระยะตรวจจับใน Scene View
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }

    // ฟังก์ชันรีเซ็ตสำหรับทดสอบ
    public void ResetDialog()
    {
        hasPlayed = false;
        Debug.Log("?? รีเซ็ต AutoBubbleDialog");
    }
}