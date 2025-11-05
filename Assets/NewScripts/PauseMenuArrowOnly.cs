using UnityEngine;
using UnityEngine.UI;

public class PauseMenuArrowOnly : MonoBehaviour
{
    public GameObject pauseMenuUI;
    public Button[] buttons; // ใส่ปุ่ม Resume, Quit ตามลำดับ

    private bool isPaused = false;
    private int selectedIndex = 0;

    void Start()
    {
        pauseMenuUI.SetActive(false);
        UpdateButtonHighlight();
    }

    void Update()
    {
        // กด ESC เพื่อ Pause/Resume
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }

        // ถ้าอยู่ในโหมด Pause ? ควบคุมด้วยลูกศร
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
            var colors = buttons[i].colors;
            colors.normalColor = (i == selectedIndex) ? Color.yellow : Color.white;
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

    public void QuitGame()
    {
        Application.Quit();
    }
}
