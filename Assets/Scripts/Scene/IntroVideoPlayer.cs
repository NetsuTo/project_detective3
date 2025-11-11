using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class IntroVideoPlayer : MonoBehaviour
{
    [Header("Video Settings")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private string gameSceneName = "GameScene"; // ชื่อซีนเกม

    [Header("Skip Settings")]
    [SerializeField] private bool allowSkip = true;
    [SerializeField] private KeyCode skipKey = KeyCode.Space;
    [SerializeField] private KeyCode skipKey2 = KeyCode.Escape;

    [Header("UI Settings (Optional)")]
    [SerializeField] private GameObject skipText; // ข้อความแสดง "Press SPACE to skip"

    private bool videoEnded = false;
    private bool isLoading = false;

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

            Debug.Log("Intro video started playing...");
        }
        else
        {
            Debug.LogError("VideoPlayer not found! Please assign it in the Inspector.");
            // ถ้าไม่มี video player ให้ไปหน้าเกมเลย
            LoadGameScene();
        }

        // แสดงข้อความข้ามวิดีโอถ้ามี
        if (skipText != null)
        {
            skipText.SetActive(allowSkip);
        }
    }

    void Update()
    {
        // ตรวจสอบการกดปุ่มข้ามวิดีโอ
        if (allowSkip && !videoEnded && !isLoading)
        {
            if (Input.GetKeyDown(skipKey) || Input.GetKeyDown(skipKey2))
            {
                SkipToGame();
            }
        }
    }

    // เรียกเมื่อวิดีโอเล่นจบ
    void OnVideoFinished(VideoPlayer vp)
    {
        videoEnded = true;
        LoadGameScene();
    }

    // ข้ามไปหน้าเกมทันที
    void SkipToGame()
    {
        Debug.Log("Skipping intro video...");
        videoPlayer.Stop();
        LoadGameScene();
    }

    // โหลดซีนเกม
    void LoadGameScene()
    {
        if (isLoading) return; // ป้องกันโหลดซ้ำ

        isLoading = true;
        Debug.Log("Loading game scene...");
        SceneManager.LoadScene(gameSceneName);
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