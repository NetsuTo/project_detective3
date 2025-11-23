using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
    public Color selectedSliderHandleColor = new Color(1f, 0.8f, 0f, 1f); // สีทอง

    [Tooltip("สีของ Handle ปกติ")]
    public Color normalSliderHandleColor = Color.white;

    [Tooltip("สีของ Fill เมื่อเลือก")]
    public Color selectedSliderFillColor = new Color(1f, 0.9f, 0f, 1f); // สีเหลืองอ่อน

    [Tooltip("สีของ Fill ปกติ")]
    public Color normalSliderFillColor = Color.white;

    [Header("สีของปุ่ม")]
    [Tooltip("สีของปุ่มเมื่อเลือก")]
    public Color selectedButtonColor = new Color(0.6f, 0.6f, 0.6f, 1f); // สีเทา

    [Tooltip("สีของปุ่มปกติ")]
    public Color normalButtonColor = new Color(1f, 1f, 1f, 0f); // โปร่งใส

    private bool isPaused = false;
    private int selectedIndex = 0;
    private int totalElements;
    private PlayerController playerController;

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

        UpdateUIHighlight();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TutorialBook book = FindObjectOfType<TutorialBook>();
            if (book != null && book.IsBookOpen())
            {
                Debug.Log("?? ไม่สามารถเปิด Pause ได้ - Tutorial Book เปิดอยู่");
                return;
            }

            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }

        if (isPaused)
        {
            HandleInput();
        }
    }

    void HandleInput()
    {
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

        if (lockInputWhenPaused && playerController != null)
        {
            playerController.LockMovement();
            Debug.Log("?? Pause - ล็อคการเคลื่อนที่");
        }
    }

    public void ResumeGame()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;

        if (lockInputWhenPaused && playerController != null)
        {
            playerController.UnlockMovement();
            Debug.Log("?? Resume - ปลดล็อคการเคลื่อนที่");
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

        if (lockInputWhenPaused && playerController != null)
        {
            playerController.UnlockMovement();
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnDestroy()
    {
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