using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class IntroVideoPlayer : MonoBehaviour
{
    [Header("Video Settings")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private string gameSceneName = "GameScene"; // ชื่อซีนเกม

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

        Debug.Log("?? IntroVideoPlayer - Input System Ready (Keyboard + Gamepad)!");
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
            Debug.Log("?? Intro video started playing...");
        }
        else
        {
            Debug.LogError("? VideoPlayer not found! Please assign it in the Inspector.");
            // ถ้าไม่มี video player ให้ไปหน้าเกมเลย
            LoadGameScene();
        }

        // ? แสดงข้อความข้ามวิดีโอตลอด (ไม่ซ่อน)
        if (skipText != null)
        {
            skipText.SetActive(allowSkip);
        }

        // ? แสดง Progress UI ตลอด (ไม่ซ่อน)
        if (holdProgressUI != null)
        {
            holdProgressUI.SetActive(allowSkip);
        }

        // เริ่มต้นหลอดที่ 0%
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

        // ?? Fallback สำหรับ Old Input System
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
                SkipToGame();
            }
        }
        else
        {
            // ปล่อยปุ่ม ?? รีเซ็ตหลอดเท่านั้น (ไม่ซ่อน UI)
            if (isHolding)
            {
                isHolding = false;
                holdTimer = 0f;

                if (holdProgressBar != null)
                    holdProgressBar.fillAmount = 0f;

                Debug.Log("?? ปล่อยปุ่ม - รีเซ็ตหลอด");
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
        LoadGameScene();
    }

    // ข้ามไปหน้าเกมทันที
    void SkipToGame()
    {
        Debug.Log("? Skipping intro video...");

        if (videoPlayer != null)
            videoPlayer.Stop();

        LoadGameScene();
    }

    // โหลดซีนเกม
    void LoadGameScene()
    {
        if (isLoading) return; // ป้องกันโหลดซ้ำ

        isLoading = true;
        Debug.Log("?? Loading game scene...");
        SceneManager.LoadScene(gameSceneName);
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