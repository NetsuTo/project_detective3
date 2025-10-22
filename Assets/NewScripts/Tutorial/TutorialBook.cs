using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class TutorialBook : MonoBehaviour
{
    [Header("การตั้งค่าหลัก")]
    [Tooltip("ปุ่มที่ใช้เปิด/ปิดสมุด")]
    [SerializeField]
    private KeyCode toggleKey = KeyCode.Tab;

    // --- ส่วนที่เพิ่มเข้ามา ---
    [Tooltip("ปุ่มที่ใช้พลิกไปหน้าถัดไป")]
    [SerializeField]
    private KeyCode nextPageKey = KeyCode.RightArrow; // ค่าเริ่มต้น: ลูกศรขวา

    [Tooltip("ปุ่มที่ใช้ย้อนกลับหน้าก่อนหน้า")]
    [SerializeField]
    private KeyCode prevPageKey = KeyCode.LeftArrow; // ค่าเริ่มต้น: ลูกศรซ้าย
    // ------------------------

    [Header("ส่วนประกอบ UI (ลากมาใส่)")]
    [SerializeField]
    private GameObject tutorialBookPanelObject;
    [SerializeField]
    private Image leftPageImage;
    [SerializeField]
    private Image rightPageImage;
    [SerializeField]
    private List<Sprite> pageSprites;
    [SerializeField]
    private Button nextButton;
    [SerializeField]
    private Button prevButton;

    [Header("การตั้งค่าเกม")]
    [SerializeField]
    private bool pauseGameWhenOpen = true;

    [Header("การตั้งค่าเสียง")]
    [SerializeField]
    private AudioClip pageTurnSound;

    private AudioSource audioSource;
    private int currentRightPageIndex = 0;
    private bool isBookOpen = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (tutorialBookPanelObject != null)
        {
            tutorialBookPanelObject.SetActive(false);
        }
        else
        {
            Debug.LogError("TutorialBook: ยังไม่ได้ลาก TutorialBookPanelObject มาใส่ในสคริปต์!");
        }

        isBookOpen = false;

        // ปุ่มคลิกยังคงทำงานเหมือนเดิม
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(GoToNextPage);
        }

        if (prevButton != null)
        {
            prevButton.onClick.AddListener(GoToPrevPage);
        }

        UpdatePageDisplay();
    }

    // ฟังก์ชันนี้ทำงานทุกเฟรม
    void Update()
    {
        // 1. ตรวจสอบการเปิด/ปิดสมุดก่อนเสมอ
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleBook();
        }

        // --- ส่วนที่แก้ไข/เพิ่มเข้ามา ---
        // 2. ถ้าสมุดเปิดอยู่ (isBookOpen == true) ให้ตรวจสอบการกดปุ่มพลิกหน้า
        if (isBookOpen)
        {
            if (Input.GetKeyDown(nextPageKey))
            {
                GoToNextPage();
            }
            else if (Input.GetKeyDown(prevPageKey)) // ใช้ else if เพื่อป้องกันการกดพร้อมกัน
            {
                GoToPrevPage();
            }
        }
        // -------------------------------
    }

    // ฟังก์ชัน ToggleBook() เหมือนเดิม (ไม่ต้องแก้)
    public void ToggleBook()
    {
        isBookOpen = !isBookOpen;

        if (tutorialBookPanelObject != null)
        {
            tutorialBookPanelObject.SetActive(isBookOpen);
        }

        if (pauseGameWhenOpen)
        {
            Time.timeScale = isBookOpen ? 0f : 1f;
        }

        if (isBookOpen)
        {
            currentRightPageIndex = 0;
            UpdatePageDisplay();
        }
    }

    // ฟังก์ชัน GoToNextPage() เหมือนเดิม (ไม่ต้องแก้)
    public void GoToNextPage()
    {
        if (currentRightPageIndex + 1 < pageSprites.Count)
        {
            currentRightPageIndex += 2;
            UpdatePageDisplay();
            PlayPageSound();
        }
    }

    // ฟังก์ชัน GoToPrevPage() เหมือนเดิม (ไม่ต้องแก้)
    public void GoToPrevPage()
    {
        if (currentRightPageIndex > 0)
        {
            currentRightPageIndex -= 2;
            UpdatePageDisplay();
            PlayPageSound();
        }
    }

    // ฟังก์ชัน PlayPageSound() เหมือนเดิม (ไม่ต้องแก้)
    private void PlayPageSound()
    {
        if (audioSource != null && pageTurnSound != null)
        {
            audioSource.PlayOneShot(pageTurnSound);
        }
    }

    // ฟังก์ชัน UpdatePageDisplay() เหมือนเดิม (ไม่ต้องแก้)
    private void UpdatePageDisplay()
    {
        if (pageSprites.Count == 0 || leftPageImage == null || rightPageImage == null)
        {
            return;
        }

        if (currentRightPageIndex == 0)
        {
            leftPageImage.gameObject.SetActive(false);
        }
        else
        {
            leftPageImage.gameObject.SetActive(true);
            leftPageImage.sprite = pageSprites[currentRightPageIndex - 1];
        }

        if (currentRightPageIndex < pageSprites.Count)
        {
            rightPageImage.gameObject.SetActive(true);
            rightPageImage.sprite = pageSprites[currentRightPageIndex];
        }
        else
        {
            rightPageImage.gameObject.SetActive(false);
        }

        if (prevButton != null)
        {
            prevButton.gameObject.SetActive(currentRightPageIndex > 0);
        }

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(currentRightPageIndex + 1 < pageSprites.Count);
        }
    }
}