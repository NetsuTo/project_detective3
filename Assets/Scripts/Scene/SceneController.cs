using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem;

public class SceneController : MonoBehaviour
{
    [Header("ลาก TextMeshPro ที่พิมพ์ 'Skip' ไว้แล้วมาใส่ตรงนี้")]
    public TextMeshProUGUI skipText; // ลาก TextMeshPro Object ที่มีข้อความ Skip มาใส่ตรงนี้

    [Header("การตั้งค่า")]
    public string targetSceneName = "MenuScene";

    [Header("ตั้งค่าการกระพริบ")]
    public float blinkSpeed = 1f; // ความเร็วในการกระพริบ
    public float minAlpha = 0.2f; // ความโปร่งใสต่ำสุด
    public float maxAlpha = 1f;   // ความโปร่งใสสูงสุด

    void Update()
    {
        // ทำให้ข้อความ Skip กระพริบ
        if (skipText != null)
        {
            float alpha = Mathf.Lerp(minAlpha, maxAlpha,
                (Mathf.Sin(Time.time * blinkSpeed) + 1f) / 2f);
            Color color = skipText.color;
            color.a = alpha;
            skipText.color = color;
        }

        // ตรวจสอบการกด Space
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            LoadScene();
        }
    }

    void LoadScene()
    {
        Time.timeScale = 1f; // รีเซ็ต Time Scale
        SceneManager.LoadScene(targetSceneName);
    }
}