using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class EndGameVideoPlayer : MonoBehaviour
{
    [Header("Video Settings")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private string mainMenuSceneName = "MainMenu"; // ชื่อซีนหน้าแรก

    [Header("Skip Settings")]
    [SerializeField] private bool allowSkip = true;
    [SerializeField] private float holdDuration = 1.5f; // กดค้างกี่วินาทีเพื่อข้าม

    [Header("UI Settings")]
    [SerializeField] private GameObject skipText; // ข้อความแสดง "Hold to skip"
    [SerializeField] private Image holdProgressBar; // Progress Bar แสดงการกดค้าง (optional)
    [SerializeField] private GameObject holdProgressUI; // UI Container ของ Progress Bar

    private bool videoEnded = false;
    private bool isLoading = false;
    private float holdTimer = 0f;
    private bool isHolding = false;

    // ===== Input System Actions - รองรับ Keyboard + Gamepad =====
    private InputAction skipAction;

    void Awake()
    {
        // สร้าง Input Actions
        SetupInputActions();
        skipAction?.Enable();

        Debug.Log("? EndGameVideoPlayer - Input System Ready (Keyboard + Gamepad)!");
    }

    private void SetupInputActions()
    {
        // ===== Skip - รองรับ Space และ Button South (A/Cross) เท่านั้น =====
        skipAction = new InputAction("Skip Video", type: InputActionType.Button);
        skipAction.AddBinding("<Keyboard>/space");
        skipAction.AddBinding("<Gamepad>/buttonSouth");  // Xbox: A, PS: Cross
    }

    void Start()
    {
        // ถ้าไม่ได้กำหนด VideoPlayer ให้หาจาก GameObject นี้
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        if (videoPlayer != null)
        {
            // ตั้งค่าให้เล่นวิดีโอทันทีที่โหลดซีน
            videoPlayer.Play();
            // ลงทะเบียน callback เมื่อวิดีโอเล่นจบ
            videoPlayer.loopPointReached += OnVideoFinished;
            Debug.Log("?? End game video started playing...");
        }
        else
        {
            Debug.LogError("? VideoPlayer not found! Please assign it in the Inspector.");
            // ถ้าไม่มี video player ให้ไปหน้าเมนูเลย
            LoadMainMenu();
        }

        // แสดงข้อความข้ามวิดีโอถ้ามี
        if (skipText != null)
        {
            skipText.SetActive(allowSkip);
        }

        // ซ่อน Progress Bar ตอนเริ่ม
        if (holdProgressUI != null)
        {
            holdProgressUI.SetActive(false);
        }

        if (holdProgressBar != null)
        {
            holdProgressBar.fillAmount = 0f;
        }
    }

    void Update()
    {
        if (!allowSkip || videoEnded || isLoading) return;

        // อ่านค่า Input (กดค้างหรือไม่)
        bool isPressed = skipAction.IsPressed();

        // ? Fallback สำหรับ Old Input System
        if (Keyboard.current == null && Gamepad.current == null)
        {
            isPressed = Input.GetKey(KeyCode.Space);
        }

        if (isPressed)
        {
            // กำลังกดค้าง
            if (!isHolding)
            {
                isHolding = true;
                if (holdProgressUI != null)
                    holdProgressUI.SetActive(true);

                Debug.Log("? กำลังกดค้างเพื่อข้าม...");
            }

            holdTimer += Time.deltaTime;

            // อัพเดท Progress Bar
            if (holdProgressBar != null)
            {
                holdProgressBar.fillAmount = Mathf.Clamp01(holdTimer / holdDuration);
            }

            // ถ้ากดค้างครบเวลาแล้ว ? ข้าม
            if (holdTimer >= holdDuration)
            {
                SkipToMainMenu();
            }
        }
        else
        {
            // ปล่อยปุ่ม ? รีเซ็ต
            if (isHolding)
            {
                isHolding = false;
                holdTimer = 0f;

                if (holdProgressUI != null)
                    holdProgressUI.SetActive(false);

                if (holdProgressBar != null)
                    holdProgressBar.fillAmount = 0f;

                Debug.Log("?? ปล่อยปุ่ม - รีเซ็ต");
            }
        }
    }

    private void OnEnable()
    {
        skipAction?.Enable();
    }

    private void OnDisable()
    {
        skipAction?.Disable();
    }

    // เรียกเมื่อวิดีโอเล่นจบ
    void OnVideoFinished(VideoPlayer vp)
    {
        videoEnded = true;
        LoadMainMenu();
    }

    // ข้ามไปหน้าเมนูทันที
    void SkipToMainMenu()
    {
        Debug.Log("? Skipping end game video...");

        if (videoPlayer != null)
            videoPlayer.Stop();

        LoadMainMenu();
    }

    // โหลดซีนหน้าเมนู
    void LoadMainMenu()
    {
        if (isLoading) return; // ป้องกันโหลดซ้ำ

        isLoading = true;
        Debug.Log("?? Loading main menu...");
        SceneManager.LoadScene(mainMenuSceneName);
    }

    void OnDestroy()
    {
        // ยกเลิก callback เมื่อ destroy
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }

        // Cleanup Input Actions
        if (skipAction != null)
        {
            skipAction.Dispose();
        }
    }
}