using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SimpleBubbleDialog : MonoBehaviour
{
    [Header("บทสนทนา")]
    [TextArea(2, 5)]
    public string[] dialogLines;

    [Header("🔒 เงื่อนไขการปลดล็อค")]
    public TargetZone requiredZone; // ลาก TargetZone มาใส่
    public int requiredCompletedCount = 2; // ต้องใช้ไป 2 สกิล
    public bool startLocked = true; // เริ่มต้นล็อคไว้

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

    private GameObject bubbleInstance;
    private Text bubbleText;
    private bool playerInRange = false;
    private bool isShowingDialog = false;
    private int currentLineIndex = 0;
    private Canvas mainCanvas;
    private float floatTimer = 0f;
    private PlayerController playerController;
    private Animator playerAnimator;
    private bool isTyping = false;
    private bool hasSpawnedObjects = false;
    private Collider myCollider; // 🔑 เก็บ Collider ของตัวเอง
    private bool isUnlocked = false; // 🔑 สถานะปลดล็อค

    void Start()
    {
        mainCanvas = FindObjectOfType<Canvas>();
        if (mainCanvas == null)
        {
            Debug.LogError("❌ ไม่พบ Canvas! กรุณาสร้าง Canvas (UI -> Canvas)");
        }

        if (pressEIndicator != null)
            pressEIndicator.SetActive(false);

        // 🔑 หา Collider ของตัวเอง
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

        // 🔒 ล็อค Collider ตอนเริ่มต้น (ถ้าตั้งค่าไว้)
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
    }

    void Update()
    {
        // 🔑 เช็คการปลดล็อคทุกเฟรม (ถ้ายังล็อคอยู่)
        if (!isUnlocked && requiredZone != null)
        {
            if (requiredZone.GetCompletedCount() >= requiredCompletedCount)
            {
                UnlockDialog();
            }
        }

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
                StopAllCoroutines();
                CompleteCurrentText();
            }
            else
            {
                NextLine();
            }
        }

        // อัพเดทตำแหน่งบับเบิ้ลให้ติดตัวตลอด
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

    // 🔓 ปลดล็อคเห็ด
    void UnlockDialog()
    {
        isUnlocked = true;
        if (myCollider != null)
        {
            myCollider.enabled = true;
        }
        Debug.Log($"🔓 ปลดล็อคเห็ดแล้ว! (ใช้สกิลครบ {requiredCompletedCount} ตัว)");
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
            bubbleText = bubbleInstance.GetComponentInChildren<Text>();
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

            bubbleText = textObj.AddComponent<Text>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            bubbleText.font = font;
            bubbleText.fontSize = fontSize;
            bubbleText.color = textColor;
            bubbleText.alignment = textAlignment;
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

        isTyping = true;
        bubbleText.text = "";

        foreach (char c in text)
        {
            bubbleText.text += c;
            yield return new WaitForSeconds(typingSpeed);
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
            bubbleText.text = dialogLines[currentLineIndex];
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
            Debug.Log("🔓 ปลดล็อค Player ให้เดินได้แล้ว");
        }

        if (bubbleInstance != null)
        {
            Destroy(bubbleInstance);
        }

        if (playerInRange && pressEIndicator != null)
        {
            pressEIndicator.SetActive(true);
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