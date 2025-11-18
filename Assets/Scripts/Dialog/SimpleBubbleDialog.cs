using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SimpleBubbleDialog : MonoBehaviour
{
    [Header("บทสนทนา")]
    [TextArea(2, 5)]
    public string[] dialogLines; // ข้อความที่จะแสดง

    [Header("การตั้งค่าตำแหน่ง")]
    public float bubbleHeight = 2f; // ความสูงของบับเบิ้ลเหนือหัว
    public Vector2 bubbleOffset = Vector2.zero; // เลื่อนตำแหน่งบับเบิล (X, Y)
    public float floatAmplitude = 0.3f; // ความสูงของการลอย (0 = ไม่ลอย)
    public float floatSpeed = 2f; // ความเร็วการลอย
    public bool alwaysInFront = true; // แสดงหน้า Player เสมอ

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

    [Header("UI")]
    public GameObject bubblePrefab; // Prefab ของบับเบิ้ล (ถ้ามี)
    public GameObject pressEIndicator; // UI บอกให้กด E

    [Header("Object ที่จะโผล่หลังคุยเสร็จ")]
    public GameObject[] objectsToSpawn; // Object ที่จะโผล่ขึ้นมา
    public bool activateObjects = true; // เปิดใช้งาน Object (true) หรือสร้างใหม่ (false)
    public Transform spawnPoint; // ตำแหน่งที่จะโผล่ (ถ้าสร้างใหม่)
    public bool spawnOnlyOnce = true; // โผล่แค่ครั้งเดียว (true) หรือทุกครั้ง (false)

    private GameObject bubbleInstance;
    private Text bubbleText;
    private bool playerInRange = false;
    private bool isShowingDialog = false;
    private int currentLineIndex = 0;
    private Canvas mainCanvas;
    private float floatTimer = 0f;
    private PlayerController playerController;
    private Animator playerAnimator;
    private bool isTyping = false; // ตรวจสอบว่ากำลังพิมพ์อยู่หรือไม่
    private bool hasSpawnedObjects = false; // ตรวจสอบว่าโผล่ Object ไปแล้วหรือยัง

    void Start()
    {
        // หา Canvas
        mainCanvas = FindObjectOfType<Canvas>();
        if (mainCanvas == null)
        {
            Debug.LogError("❌ ไม่พบ Canvas! กรุณาสร้าง Canvas (UI -> Canvas)");
        }

        if (pressEIndicator != null)
            pressEIndicator.SetActive(false);

        // ตรวจสอบว่ามี Collider หรือไม่
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning("⚠️ ไม่พบ Collider! กำลังสร้าง SphereCollider...");
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = detectionRange;
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning("⚠️ Collider ต้องเปิด Is Trigger!");
            col.isTrigger = true;
        }

        if (dialogLines.Length == 0)
        {
            Debug.LogWarning("⚠️ ไม่มีข้อความ! กรุณาเพิ่มใน Dialog Lines");
        }

        Debug.Log($"✅ SimpleBubbleDialog พร้อมใช้งาน - มี {dialogLines.Length} ข้อความ");
    }

    void Update()
    {
        // กด E เพื่อเริ่มบทสนทนา
        if (playerInRange && Input.GetKeyDown(KeyCode.E) && !isShowingDialog)
        {
            StartDialog();
        }

        // กด Space เพื่อแสดงข้อความทั้งหมด หรือไปข้อความถัดไป
        if (isShowingDialog && Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
            {
                // ถ้ากำลังพิมพ์อยู่ -> แสดงข้อความทั้งหมดทันที
                StopAllCoroutines();
                CompleteCurrentText();
            }
            else
            {
                // ถ้าแสดงครบแล้ว -> ไปข้อความถัดไป
                NextLine();
            }
        }

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
            Debug.LogError("❌ ไม่มีข้อความ!");
            return;
        }

        isShowingDialog = true;
        currentLineIndex = 0;

        // ล็อค Player
        if (playerController != null)
        {
            playerController.enabled = false;
            Debug.Log("🔒 ล็อค Player ไม่ให้เดิน");
        }

        // หยุดอนิเมชั่น
        if (playerAnimator != null)
        {
            playerAnimator.SetFloat("Speed", 0f);
            Debug.Log("⏸️ หยุดอนิเมชั่น Player");
        }

        // ซ่อน Press E
        if (pressEIndicator != null)
            pressEIndicator.SetActive(false);

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
                Debug.LogError("❌ ไม่พบ Canvas!");
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

        Debug.Log("✅ สร้างบับเบิ้ลสำเร็จ");
    }

    IEnumerator TypeText(string text)
    {
        if (bubbleText == null)
        {
            Debug.LogError("❌ bubbleText เป็น null!");
            yield break;
        }

        isTyping = true; // เริ่มพิมพ์
        bubbleText.text = "";

        // พิมพ์ทีละตัว
        foreach (char c in text)
        {
            bubbleText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false; // พิมพ์เสร็จแล้ว

        // รอก่อนไปข้อความถัดไป
        yield return new WaitForSeconds(displayDuration);

        if (isShowingDialog)
        {
            NextLine();
        }
    }

    void CompleteCurrentText()
    {
        // แสดงข้อความทั้งหมดทันที
        if (currentLineIndex < dialogLines.Length)
        {
            bubbleText.text = dialogLines[currentLineIndex];
            isTyping = false;

            // เริ่มนับเวลารอก่อนไปข้อความถัดไป
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

        // ปลดล็อค Player
        if (playerController != null)
        {
            playerController.enabled = true;
            Debug.Log("🔓 ปลดล็อค Player ให้เดินได้แล้ว");
        }

        // ไม่ต้องรีเซ็ตอนิเมชั่น เพราะ PlayerController จะจัดการเอง

        if (bubbleInstance != null)
        {
            Destroy(bubbleInstance);
        }

        // แสดง Press E กลับมา
        if (playerInRange && pressEIndicator != null)
        {
            pressEIndicator.SetActive(true);
        }

        // ทำให้ Object โผล่ขึ้นมา
        SpawnObjects();

        Debug.Log("✅ จบบทสนทนา");
    }

    void SpawnObjects()
    {
        if (objectsToSpawn == null || objectsToSpawn.Length == 0)
        {
            return; // ไม่มี Object ที่จะโผล่
        }

        // ถ้าตั้งค่าให้โผล่แค่ครั้งเดียว และโผล่ไปแล้ว ก็ไม่ทำอะไร
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
                // เปิดใช้งาน Object ที่ซ่อนอยู่
                obj.SetActive(true);
                Debug.Log($"✨ เปิด Object: {obj.name}");
            }
            else
            {
                // สร้าง Object ใหม่
                Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;
                Quaternion spawnRot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

                GameObject newObj = Instantiate(obj, spawnPos, spawnRot);
                Debug.Log($"✨ สร้าง Object: {newObj.name}");
            }
        }

        hasSpawnedObjects = true; // บันทึกว่าโผล่ไปแล้ว
        Debug.Log("✅ Object โผล่ครบแล้ว");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;

            // เก็บ reference ของ PlayerController และ Animator
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
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;

            if (pressEIndicator != null)
                pressEIndicator.SetActive(false);

            // ปิดบทสนทนาถ้าเดินออกไป
            if (isShowingDialog)
            {
                EndDialog();
            }
        }
    }

    // แสดงระยะตรวจจับใน Scene View
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}