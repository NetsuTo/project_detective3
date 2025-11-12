using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Header("UI Buttons (เรียงจากบนลงล่าง)")]
    public Button[] buttons; // ใส่ปุ่มเช่น Start / Retry / MainMenu / Quit ตามลำดับ

    [Header("การตั้งค่าสี")]
    [Tooltip("สีของปุ่มที่ไม่ได้เลือก (เทาใสๆ)")]
    public Color normalColor = new Color(0.7f, 0.7f, 0.7f, 0.5f); // เทาใสๆ

    [Tooltip("สีของปุ่มที่เลือก")]
    public Color selectedColor = new Color(1f, 1f, 1f, 1f); // ขาวทึบ

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
                img.color = selectedColor; // สีขาวทึบตอนเลือก
                buttons[i].transform.localScale = Vector3.one * 1.1f; // ขยายเล็กน้อย
            }
            else
            {
                img.color = normalColor; // สีเทาใสๆ
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