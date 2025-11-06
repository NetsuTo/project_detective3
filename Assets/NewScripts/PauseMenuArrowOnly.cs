using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenuArrowOnly : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pauseMenuUI;
    public Button[] buttons; // ใส่ปุ่ม Resume, Restart, Quit ตามลำดับ

    private bool isPaused = false;
    private int selectedIndex = 0;

    void Start()
    {
        pauseMenuUI.SetActive(false);
        UpdateButtonHighlight();

        // Debug ตรวจสอบจำนวนปุ่ม (ใช้ตอนเทส)
        Debug.Log("Total buttons: " + buttons.Length);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
                Debug.Log($"Button {i}: {buttons[i].name}");
            else
                Debug.LogWarning($"Button {i} is missing!");
        }
    }

    void Update()
    {
        // กด ESC เพื่อ Pause/Resume
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }

        // ควบคุมด้วยลูกศรเฉพาะตอน Pause
        if (isPaused)
        {
            HandleArrowInput();
        }
    }

    void HandleArrowInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            selectedIndex = (selectedIndex - 1 + buttons.Length) % buttons.Length;
            UpdateButtonHighlight();
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedIndex = (selectedIndex + 1) % buttons.Length;
            UpdateButtonHighlight();
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            buttons[selectedIndex].onClick.Invoke();
        }
    }

    void UpdateButtonHighlight()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;

            var colors = buttons[i].colors;
            if (i == selectedIndex)
            {
                // สีเทาเมื่อเลือก
                colors.normalColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            }
            else
            {
                // โปร่งใสเมื่อไม่ได้เลือก
                colors.normalColor = new Color(1f, 1f, 1f, 0f);
            }

            // ปรับทุกสถานะให้เหมือนกัน
            colors.highlightedColor = colors.normalColor;
            colors.selectedColor = colors.normalColor;
            buttons[i].colors = colors;
        }
    }

    public void PauseGame()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
        selectedIndex = 0;
        UpdateButtonHighlight();
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
        Application.Quit();
    }
}
