using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class MenuWithVolume : MonoBehaviour
{
    [Header("UI Buttons (เรียงจากบนลงล่าง)")]
    public Button[] buttons; // Start / Options / Quit เป็นต้น

    [Header("Volume Sliders")]
    public Slider sfxVolumeSlider;
    public Slider musicVolumeSlider;

    [Header("การตั้งค่าสี - ปุ่ม")]
    public Color normalButtonColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);
    public Color selectedButtonColor = new Color(1f, 1f, 1f, 1f);

    [Header("การตั้งค่าสี - Slider")]
    public Color normalSliderHandleColor = Color.white;
    public Color selectedSliderHandleColor = Color.yellow;
    public Color normalSliderFillColor = Color.white;
    public Color selectedSliderFillColor = new Color(1f, 0.9f, 0f, 1f);

    private int selectedIndex = 0;
    private int totalElements; // ปุ่ม + Slider ทั้งหมด

    // ===== Input System Actions - รองรับ Keyboard + Gamepad =====
    private InputAction navigateUpAction;
    private InputAction navigateDownAction;
    private InputAction navigateLeftAction;
    private InputAction navigateRightAction;
    private InputAction confirmAction;

    // ป้องกันการกดซ้ำ
    private bool upWasPressed = false;
    private bool downWasPressed = false;
    private bool leftWasPressed = false;
    private bool rightWasPressed = false;
    private bool confirmWasPressed = false;

    void Awake()
    {
        // สร้าง Input Actions
        SetupInputActions();

        // Enable ทุก Action
        navigateUpAction?.Enable();
        navigateDownAction?.Enable();
        navigateLeftAction?.Enable();
        navigateRightAction?.Enable();
        confirmAction?.Enable();

        Debug.Log("? MenuWithVolume - Input System Ready (Keyboard + Gamepad)!");
    }

    private void SetupInputActions()
    {
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
        confirmAction.AddBinding("<Keyboard>/return");
        confirmAction.AddBinding("<Gamepad>/buttonSouth");  // Xbox: A, PS: Cross
    }

    void Start()
    {
        // คำนวณจำนวน element ทั้งหมด
        int sliderCount = 0;
        if (sfxVolumeSlider != null) sliderCount++;
        if (musicVolumeSlider != null) sliderCount++;

        totalElements = buttons.Length + sliderCount;

        // โหลดค่า volume จาก AudioManager
        LoadVolumeValues();

        // เชื่อม Slider กับ AudioManager
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

        UpdateUIHighlight();
    }

    void Update()
    {
        HandleInput();

        // ? Fallback สำหรับ Old Input System
        if (Keyboard.current == null && Gamepad.current == null)
        {
            HandleLegacyInput();
        }
    }

    void HandleInput()
    {
        // อ่าน Input
        bool upPressed = navigateUpAction.IsPressed();
        bool downPressed = navigateDownAction.IsPressed();
        bool leftPressed = navigateLeftAction.IsPressed();
        bool rightPressed = navigateRightAction.IsPressed();
        bool confirmPressed = confirmAction.IsPressed();

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
    }

    void HandleLegacyInput()
    {
        // เลื่อนขึ้น-ลง
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            selectedIndex = (selectedIndex - 1 + totalElements) % totalElements;
            UpdateUIHighlight();
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedIndex = (selectedIndex + 1) % totalElements;
            UpdateUIHighlight();
        }

        // ถ้าเลือก Slider อยู่ ? ปรับค่าด้วยซ้าย-ขวา
        if (selectedIndex >= buttons.Length)
        {
            Slider currentSlider = GetCurrentSlider();

            if (currentSlider != null)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    currentSlider.value = Mathf.Clamp01(currentSlider.value - 0.1f);
                }

                if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    currentSlider.value = Mathf.Clamp01(currentSlider.value + 0.1f);
                }
            }
        }
        // ถ้าเลือกปุ่มอยู่ ? กด Enter เพื่อกดปุ่ม
        else
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                buttons[selectedIndex].onClick.Invoke();
            }
        }
    }

    Slider GetCurrentSlider()
    {
        int sliderIndex = selectedIndex - buttons.Length;
        int currentSlider = 0;

        if (sfxVolumeSlider != null)
        {
            if (sliderIndex == currentSlider) return sfxVolumeSlider;
            currentSlider++;
        }

        if (musicVolumeSlider != null)
        {
            if (sliderIndex == currentSlider) return musicVolumeSlider;
        }

        return null;
    }

    void UpdateUIHighlight()
    {
        // เช็คว่ากำลังเลือก Slider อยู่หรือเปล่า
        bool isSelectingSlider = selectedIndex >= buttons.Length;

        // อัพเดทปุ่ม
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;

            Image img = buttons[i].GetComponent<Image>();

            // ไฮไลท์เฉพาะตอนที่เลือกปุ่มนี้อยู่ และไม่ได้เลือก Slider
            if (i == selectedIndex && !isSelectingSlider)
            {
                img.color = selectedButtonColor;
                buttons[i].transform.localScale = Vector3.one * 1.1f;
            }
            else
            {
                img.color = normalButtonColor;
                buttons[i].transform.localScale = Vector3.one;
            }
        }

        // อัพเดท Slider
        int sliderIndexStart = buttons.Length;
        int currentSliderIndex = sliderIndexStart;

        if (sfxVolumeSlider != null)
        {
            UpdateSliderHighlight(sfxVolumeSlider, selectedIndex == currentSliderIndex);
            currentSliderIndex++;
        }

        if (musicVolumeSlider != null)
        {
            UpdateSliderHighlight(musicVolumeSlider, selectedIndex == currentSliderIndex);
        }
    }

    void UpdateSliderHighlight(Slider slider, bool isSelected)
    {
        // เปลี่ยนสี Handle
        Image handleImage = slider.handleRect.GetComponent<Image>();
        if (handleImage != null)
        {
            handleImage.color = isSelected ? selectedSliderHandleColor : normalSliderHandleColor;
        }

        // เปลี่ยนสี Fill
        Image fillImage = slider.fillRect.GetComponent<Image>();
        if (fillImage != null)
        {
            fillImage.color = isSelected ? selectedSliderFillColor : normalSliderFillColor;
        }

        // เปลี่ยนขนาด Handle เล็กน้อยเมื่อเลือก
        slider.handleRect.localScale = isSelected ? Vector3.one * 1.15f : Vector3.one;
    }

    // ========== โหลดค่า Volume จาก AudioManager ==========
    void LoadVolumeValues()
    {
        if (AudioManager.Instance != null)
        {
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = AudioManager.Instance.GetSFXVolume();

            if (musicVolumeSlider != null)
                musicVolumeSlider.value = AudioManager.Instance.GetMusicVolume();
        }
    }

    // ========== Slider Callbacks ==========
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

    private void OnEnable()
    {
        navigateUpAction?.Enable();
        navigateDownAction?.Enable();
        navigateLeftAction?.Enable();
        navigateRightAction?.Enable();
        confirmAction?.Enable();
    }

    private void OnDisable()
    {
        navigateUpAction?.Disable();
        navigateDownAction?.Disable();
        navigateLeftAction?.Disable();
        navigateRightAction?.Disable();
        confirmAction?.Disable();
    }

    private void OnDestroy()
    {
        // Cleanup Input Actions
        navigateUpAction?.Dispose();
        navigateDownAction?.Dispose();
        navigateLeftAction?.Dispose();
        navigateRightAction?.Dispose();
        confirmAction?.Dispose();
    }

    // ========== ฟังก์ชันเปลี่ยน Scene (เหมือนเดิม) ==========
    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
        Debug.Log("?? เปลี่ยนไป MainMenu");
    }

    public void LoadOpenGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("OpenGame");
        Debug.Log("?? เปลี่ยนไป OpenGame");
    }

    public void LoadGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Game");
        Debug.Log("?? เปลี่ยนไป Game");
    }

    public void LoadEndGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("EndGame");
        Debug.Log("?? เปลี่ยนไป EndGame");
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        Debug.Log("?? Quit Game");
    }
}