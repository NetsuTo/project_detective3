using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenuWithVolume : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pauseMenuUI;
    public Button[] buttons; // Resume, Restart, Exit
    public Slider sfxVolumeSlider;
    public Slider musicVolumeSlider;

    [Header("Settings")]
    public float sliderAdjustSpeed = 1f;

    [Header("ล็อคการกดปุ่ม")]
    [Tooltip("ล็อคการกด Input อื่นๆ เมื่อเปิด Pause Menu")]
    [SerializeField]
    private bool lockInputWhenPaused = true;

    [Header("สีของ Slider")]
    [Tooltip("สีของ Handle เมื่อเลือก")]
    public Color selectedSliderHandleColor = new Color(1f, 0.8f, 0f, 1f);

    [Tooltip("สีของ Handle ปกติ")]
    public Color normalSliderHandleColor = Color.white;

    [Tooltip("สีของ Fill เมื่อเลือก")]
    public Color selectedSliderFillColor = new Color(1f, 0.9f, 0f, 1f);

    [Tooltip("สีของ Fill ปกติ")]
    public Color normalSliderFillColor = Color.white;

    [Header("สีของปุ่ม")]
    [Tooltip("สีของปุ่มเมื่อเลือก")]
    public Color selectedButtonColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    [Tooltip("สีของปุ่มปกติ")]
    public Color normalButtonColor = new Color(1f, 1f, 1f, 0f);

    private bool isPaused = false;
    private int selectedIndex = 0;
    private int totalElements;
    private PlayerController playerController;
    private SkillLetterSelector skillSelector; // ? เพิ่มการอ้างอิง SkillLetterSelector

    // ===== Input System Actions - รองรับ Keyboard + Gamepad =====
    private InputAction pauseAction;
    private InputAction navigateUpAction;
    private InputAction navigateDownAction;
    private InputAction navigateLeftAction;
    private InputAction navigateRightAction;
    private InputAction confirmAction;
    private InputAction cancelAction;

    // ป้องกันการกดซ้ำ
    private bool pauseWasPressed = false;
    private bool upWasPressed = false;
    private bool downWasPressed = false;
    private bool leftWasPressed = false;
    private bool rightWasPressed = false;
    private bool confirmWasPressed = false;
    private bool cancelWasPressed = false;

    void Start()
    {
        pauseMenuUI.SetActive(false);

        totalElements = buttons.Length + 2;

        if (AudioManager.Instance != null)
        {
            sfxVolumeSlider.value = AudioManager.Instance.GetSFXVolume();
            musicVolumeSlider.value = AudioManager.Instance.GetMusicVolume();
        }

        sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

        playerController = FindObjectOfType<PlayerController>();
        skillSelector = FindObjectOfType<SkillLetterSelector>(); // ? หา SkillLetterSelector

        // สร้าง Input Actions
        SetupInputActions();

        UpdateUIHighlight();

        Debug.Log("? PauseMenu Started - Keyboard + Gamepad Ready!");
    }

    private void SetupInputActions()
    {
        // ===== Pause/Unpause - รองรับ ESC และ Start Button =====
        pauseAction = new InputAction("Pause", type: InputActionType.Button);
        pauseAction.AddBinding("<Keyboard>/escape");
        pauseAction.AddBinding("<Gamepad>/start");  // Start button บนจอย

        // ===== Navigation Up - รองรับ Arrow Up และ D-Pad/Left Stick =====
        navigateUpAction = new InputAction("NavigateUp", type: InputActionType.Button);
        navigateUpAction.AddBinding("<Keyboard>/upArrow");
        navigateUpAction.AddBinding("<Gamepad>/dpad/up");
        navigateUpAction.AddBinding("<Gamepad>/leftStick/up");

        // ===== Navigation Down =====
        navigateDownAction = new InputAction("NavigateDown", type: InputActionType.Button);
        navigateDownAction.AddBinding("<Keyboard>/downArrow");
        navigateDownAction.AddBinding("<Gamepad>/dpad/down");
        navigateDownAction.AddBinding("<Gamepad>/leftStick/down");

        // ===== Navigation Left (ปรับ Slider) =====
        navigateLeftAction = new InputAction("NavigateLeft", type: InputActionType.Button);
        navigateLeftAction.AddBinding("<Keyboard>/leftArrow");
        navigateLeftAction.AddBinding("<Gamepad>/dpad/left");
        navigateLeftAction.AddBinding("<Gamepad>/leftStick/left");

        // ===== Navigation Right (ปรับ Slider) =====
        navigateRightAction = new InputAction("NavigateRight", type: InputActionType.Button);
        navigateRightAction.AddBinding("<Keyboard>/rightArrow");
        navigateRightAction.AddBinding("<Gamepad>/dpad/right");
        navigateRightAction.AddBinding("<Gamepad>/leftStick/right");

        // ===== Confirm - รองรับ Enter และ Button South (A/Cross) =====
        confirmAction = new InputAction("Confirm", type: InputActionType.Button);
        confirmAction.AddBinding("<Keyboard>/enter");
        confirmAction.AddBinding("<Gamepad>/buttonSouth");  // Xbox: A, PS: Cross

        // ===== Cancel - รองรับ ESC และ Button East (B/Circle) =====
        cancelAction = new InputAction("Cancel", type: InputActionType.Button);
        cancelAction.AddBinding("<Keyboard>/escape");
        cancelAction.AddBinding("<Gamepad>/buttonEast");  // Xbox: B, PS: Circle

        // Enable Pause ตลอดเวลา
        pauseAction.Enable();
    }

    private void OnEnable()
    {
        pauseAction?.Enable();
    }

    private void OnDisable()
    {
        pauseAction?.Disable();
        navigateUpAction?.Disable();
        navigateDownAction?.Disable();
        navigateLeftAction?.Disable();
        navigateRightAction?.Disable();
        confirmAction?.Disable();
        cancelAction?.Disable();
    }

    void Update()
    {
        // ===== อ่าน Pause Input ตลอดเวลา =====
        bool pausePressed = pauseAction.IsPressed();

        if (pausePressed && !pauseWasPressed)
        {
            // ? เช็คว่ามีสกิลอยู่หรือไม่ก่อน
            if (skillSelector != null && !skillSelector.CanOpenPause)
            {
                Debug.Log("?? มีสกิลอยู่ - กด Esc เพื่อยกเลิกสกิลก่อน (SkillLetterSelector จัดการให้แล้ว)");
                pauseWasPressed = pausePressed;
                return; // ไม่เปิด Pause Menu
            }

            // เช็คว่า Tutorial Book เปิดอยู่หรือไม่
            TutorialBook book = FindObjectOfType<TutorialBook>();
            if (book != null && book.IsBookOpen())
            {
                Debug.Log("?? ไม่สามารถเปิด Pause ได้ - Tutorial Book เปิดอยู่");
            }
            else
            {
                if (isPaused)
                    ResumeGame();
                else
                    PauseGame();
            }
        }
        pauseWasPressed = pausePressed;

        // ===== อ่าน Navigation Input เมื่อ Pause =====
        if (isPaused)
        {
            bool upPressed = navigateUpAction.IsPressed();
            bool downPressed = navigateDownAction.IsPressed();
            bool leftPressed = navigateLeftAction.IsPressed();
            bool rightPressed = navigateRightAction.IsPressed();
            bool confirmPressed = confirmAction.IsPressed();
            bool cancelPressed = cancelAction.IsPressed();

            // Navigate Up
            if (upPressed && !upWasPressed)
            {
                selectedIndex = (selectedIndex - 1 + totalElements) % totalElements;
                UpdateUIHighlight();
            }
            upWasPressed = upPressed;

            // Navigate Down
            if (downPressed && !downWasPressed)
            {
                selectedIndex = (selectedIndex + 1) % totalElements;
                UpdateUIHighlight();
            }
            downWasPressed = downPressed;

            // Navigate Left (ปรับ Slider)
            if (leftPressed && !leftWasPressed)
            {
                if (selectedIndex >= buttons.Length)
                {
                    Slider currentSlider = GetCurrentSlider();
                    if (currentSlider != null)
                    {
                        currentSlider.value = Mathf.Clamp01(currentSlider.value - 0.1f);
                    }
                }
            }
            leftWasPressed = leftPressed;

            // Navigate Right (ปรับ Slider)
            if (rightPressed && !rightWasPressed)
            {
                if (selectedIndex >= buttons.Length)
                {
                    Slider currentSlider = GetCurrentSlider();
                    if (currentSlider != null)
                    {
                        currentSlider.value = Mathf.Clamp01(currentSlider.value + 0.1f);
                    }
                }
            }
            rightWasPressed = rightPressed;

            // Confirm (กดปุ่ม)
            if (confirmPressed && !confirmWasPressed)
            {
                if (selectedIndex < buttons.Length)
                {
                    buttons[selectedIndex].onClick.Invoke();
                }
            }
            confirmWasPressed = confirmPressed;

            // Cancel (กลับ/Resume)
            if (cancelPressed && !cancelWasPressed)
            {
                ResumeGame();
            }
            cancelWasPressed = cancelPressed;
        }
    }

    Slider GetCurrentSlider()
    {
        int sliderIndex = selectedIndex - buttons.Length;

        if (sliderIndex == 0)
            return sfxVolumeSlider;
        else if (sliderIndex == 1)
            return musicVolumeSlider;

        return null;
    }

    void UpdateUIHighlight()
    {
        // อัพเดทปุ่ม
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;

            var colors = buttons[i].colors;

            if (i == selectedIndex)
            {
                colors.normalColor = selectedButtonColor;
            }
            else
            {
                colors.normalColor = normalButtonColor;
            }

            colors.highlightedColor = colors.normalColor;
            colors.selectedColor = colors.normalColor;
            buttons[i].colors = colors;
        }

        // อัพเดท Slider
        UpdateSliderHighlight(sfxVolumeSlider, selectedIndex == buttons.Length);
        UpdateSliderHighlight(musicVolumeSlider, selectedIndex == buttons.Length + 1);
    }

    void UpdateSliderHighlight(Slider slider, bool isSelected)
    {
        // เปลี่ยนสี Handle ของ Slider
        Image handleImage = slider.handleRect.GetComponent<Image>();
        if (handleImage != null)
        {
            handleImage.color = isSelected ? selectedSliderHandleColor : normalSliderHandleColor;
        }

        // เปลี่ยนสี Fill ของ Slider
        Image fillImage = slider.fillRect.GetComponent<Image>();
        if (fillImage != null)
        {
            fillImage.color = isSelected ? selectedSliderFillColor : normalSliderFillColor;
        }

        // เปลี่ยนขนาด Handle เล็กน้อยเมื่อเลือก
        if (slider.handleRect != null)
        {
            slider.handleRect.localScale = isSelected ? Vector3.one * 1.15f : Vector3.one;
        }
    }

    void OnMusicVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
        }
    }

    void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(value);
        }
    }

    public void PauseGame()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
        selectedIndex = 0;
        UpdateUIHighlight();

        // เปิด Navigation Actions
        navigateUpAction?.Enable();
        navigateDownAction?.Enable();
        navigateLeftAction?.Enable();
        navigateRightAction?.Enable();
        confirmAction?.Enable();
        cancelAction?.Enable();

        if (lockInputWhenPaused && playerController != null)
        {
            playerController.LockMovement();
            Debug.Log("?? Pause - ล็อคการเคลื่อนที่");
        }

        Debug.Log("?? เปิด Pause Menu");
    }

    public void ResumeGame()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;

        // ปิด Navigation Actions
        navigateUpAction?.Disable();
        navigateDownAction?.Disable();
        navigateLeftAction?.Disable();
        navigateRightAction?.Disable();
        confirmAction?.Disable();
        cancelAction?.Disable();

        if (lockInputWhenPaused && playerController != null)
        {
            playerController.UnlockMovement();
            Debug.Log("?? Resume - ปลดล็อคการเคลื่อนที่");
        }

        Debug.Log("?? Resume Game");
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
        Debug.Log("?? Restart Game");
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

        if (lockInputWhenPaused && playerController != null)
        {
            playerController.UnlockMovement();
        }

        Debug.Log("?? Quit Game");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnDestroy()
    {
        // Cleanup Input Actions
        pauseAction?.Dispose();
        navigateUpAction?.Dispose();
        navigateDownAction?.Dispose();
        navigateLeftAction?.Dispose();
        navigateRightAction?.Dispose();
        confirmAction?.Dispose();
        cancelAction?.Dispose();

        if (lockInputWhenPaused && playerController != null && isPaused)
        {
            playerController.UnlockMovement();
        }
    }

    public bool IsPaused()
    {
        return isPaused;
    }
}