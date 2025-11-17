using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenuWithVolume : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pauseMenuUI;
    public Button[] buttons; // Resume, Restart, Exit
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;

    [Header("Settings")]
    public float sliderAdjustSpeed = 1f; // ความเร็วในการปรับ Slider (เปลี่ยนเป็นทีละขั้น)

    private bool isPaused = false;
    private int selectedIndex = 0;
    private int totalElements; // ปุ่ม + Slider ทั้งหมด

    void Start()
    {
        pauseMenuUI.SetActive(false);

        // คำนวณจำนวน element ทั้งหมด (ปุ่ม 3 + slider 2)
        totalElements = buttons.Length + 2;

        // ตั้งค่า Slider เริ่มต้นจาก AudioManager
        if (AudioManager.Instance != null)
        {
            masterVolumeSlider.value = AudioManager.Instance.GetMasterVolume();
            musicVolumeSlider.value = AudioManager.Instance.GetMusicVolume();
        }

        // เชื่อม Slider กับ AudioManager
        masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

        UpdateUIHighlight();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
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

        if (sliderIndex == 0)
            return masterVolumeSlider;
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
                colors.normalColor = new Color(0.6f, 0.6f, 0.6f, 1f); // สีเทา = เลือก
            }
            else
            {
                colors.normalColor = new Color(1f, 1f, 1f, 0f); // โปร่งใส
            }

            colors.highlightedColor = colors.normalColor;
            colors.selectedColor = colors.normalColor;
            buttons[i].colors = colors;
        }

        // อัพเดท Slider (เปลี่ยนสีของ Handle)
        UpdateSliderHighlight(masterVolumeSlider, selectedIndex == buttons.Length);
        UpdateSliderHighlight(musicVolumeSlider, selectedIndex == buttons.Length + 1);
    }

    void UpdateSliderHighlight(Slider slider, bool isSelected)
    {
        // เปลี่ยนสี Handle ของ Slider
        Image handleImage = slider.handleRect.GetComponent<Image>();
        if (handleImage != null)
        {
            if (isSelected)
            {
                handleImage.color = Color.yellow; // สีเหลือง = เลือก
            }
            else
            {
                handleImage.color = Color.white; // สีขาว = ปกติ
            }
        }
    }

    // ========== Slider Callbacks ==========
    void OnMasterVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(value);
        }
    }

    void OnMusicVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
        }
    }

    // ========== Pause/Resume Functions ==========
    public void PauseGame()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
        selectedIndex = 0;
        UpdateUIHighlight();
    }

    public void ResumeGame()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
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
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}