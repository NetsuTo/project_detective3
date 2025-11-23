using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuWithVolume : MonoBehaviour
{
    [Header("UI Buttons (เรียงจากบนลงล่าง)")]
    public Button[] buttons; // Start / Options / Quit เป็นต้น

    [Header("Volume Sliders")]
    public Slider sfxVolumeSlider;
    public Slider musicVolumeSlider;

    [Header("การตั้งค่าสี")]
    public Color normalButtonColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);
    public Color selectedButtonColor = new Color(1f, 1f, 1f, 1f);
    public Color normalSliderColor = Color.white;
    public Color selectedSliderColor = Color.yellow;

    private int selectedIndex = 0;
    private int totalElements; // ปุ่ม + Slider ทั้งหมด

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
        Image handleImage = slider.handleRect.GetComponent<Image>();
        if (handleImage != null)
        {
            handleImage.color = isSelected ? selectedSliderColor : normalSliderColor;
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

    // ========== ฟังก์ชันเปลี่ยน Scene (เหมือนเดิม) ==========
    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void LoadOpenGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("OpenGame");
    }

    public void LoadGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Game");
    }

    public void LoadEndGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("EndGame");
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        Debug.Log("Quit Game");
    }
}