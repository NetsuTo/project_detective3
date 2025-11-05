using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Header("UI Buttons (เรียงจากบนลงล่าง)")]
    public Button[] buttons; // ใส่ปุ่มเช่น Start / Retry / MainMenu / Quit ตามลำดับ

    private int selectedIndex = 0;

    void Start()
    {
        UpdateButtonHighlight();
    }

    void Update()
    {
        // เลือกด้วยลูกศร
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

        // กด Enter เพื่อเลือก
        if (Input.GetKeyDown(KeyCode.Return))
        {
            buttons[selectedIndex].onClick.Invoke();
        }
    }

    void UpdateButtonHighlight()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            Image img = buttons[i].GetComponent<Image>();

            if (i == selectedIndex)
            {
                img.color = new Color(1f, 0.9f, 0.3f); // สีเหลืองตอนเลือก
                buttons[i].transform.localScale = Vector3.one * 1.1f; // ขยายเล็กน้อย
            }
            else
            {
                img.color = Color.white;
                buttons[i].transform.localScale = Vector3.one;
            }
        }
    }

    // ===== ฟังก์ชันเปลี่ยน Scene =====

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
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
        Application.Quit();
        Debug.Log("Quit Game"); // สำหรับทดสอบใน Editor
    }
}
