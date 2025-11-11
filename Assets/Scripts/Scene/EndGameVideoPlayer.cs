using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class EndGameVideoPlayer : MonoBehaviour
{
    [Header("Video Settings")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private string mainMenuSceneName = "MainMenu"; // ชื่อซีนหน้าแรก

    [Header("Skip Settings")]
    [SerializeField] private bool allowSkip = true;
    [SerializeField] private KeyCode skipKey = KeyCode.Space;

    private bool videoEnded = false;

    void Start()
    {
        // ถ้าไม่ได้กำหนด VideoPlayer ให้หาจาก GameObject นี้
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        if (videoPlayer != null)
        {
            // ตั้งค่าให้เล่นวิดีโอทันทีที่โหลดซีน
            videoPlayer.Play();

            // ลงทะเบียน callback เมื่อวิดีโอเล่นจบ
            videoPlayer.loopPointReached += OnVideoFinished;
        }
        else
        {
            Debug.LogError("VideoPlayer not found! Please assign it in the Inspector.");
        }
    }

    void Update()
    {
        // ตรวจสอบการกดปุ่มข้ามวิดีโอ
        if (allowSkip && Input.GetKeyDown(skipKey) && !videoEnded)
        {
            SkipToMainMenu();
        }
    }

    // เรียกเมื่อวิดีโอเล่นจบ
    void OnVideoFinished(VideoPlayer vp)
    {
        videoEnded = true;
        LoadMainMenu();
    }

    // ข้ามไปหน้าแรกทันที
    void SkipToMainMenu()
    {
        Debug.Log("Skipping video...");
        videoPlayer.Stop();
        LoadMainMenu();
    }

    // โหลดซีนหน้าแรก
    void LoadMainMenu()
    {
        Debug.Log("Loading main menu...");
        SceneManager.LoadScene(mainMenuSceneName);
    }

    void OnDestroy()
    {
        // ยกเลิก callback เมื่อ destroy
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }
}