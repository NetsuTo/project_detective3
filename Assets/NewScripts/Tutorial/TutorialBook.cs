using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

[RequireComponent(typeof(AudioSource))]
public class TutorialBook : MonoBehaviour
{
    [Header("การตั้งค่าหลัก")]
    [Tooltip("ปุ่มที่ใช้เปิด/ปิดสมุด")]
    [SerializeField]
    private KeyCode toggleKey = KeyCode.Tab;

    [Tooltip("ปุ่มที่ใช้พลิกไปหน้าถัดไป")]
    [SerializeField]
    private KeyCode nextPageKey = KeyCode.RightArrow;

    [Tooltip("ปุ่มที่ใช้ย้อนกลับหน้าก่อนหน้า")]
    [SerializeField]
    private KeyCode prevPageKey = KeyCode.LeftArrow;

    [Header("ส่วนประกอบ UI (ลากมาใส่)")]
    [SerializeField]
    private GameObject tutorialBookPanelObject;
    [SerializeField]
    private Image leftPageImage;
    [SerializeField]
    private Image rightPageImage;

    [Tooltip("Prefab ปกสมุด (จะ Spawn ตอนเปิดสมุด)")]
    [SerializeField]
    private GameObject coverPrefab;

    [Tooltip("ตำแหน่งที่จะวางปก (ถ้าไม่ใส่จะวางที่ TutorialBookPanel)")]
    [SerializeField]
    private Transform coverParent;

    [Tooltip("หน้าเนื้อหาภายในสมุด (ไม่รวมปก)")]
    [SerializeField]
    private List<Sprite> pageSprites;

    [SerializeField]
    private Button nextButton;
    [SerializeField]
    private Button prevButton;

    [Tooltip("กรอบสมุดที่จะแสดงเมื่อไม่ใช่หน้าปก")]
    [SerializeField]
    private GameObject bookFrameObject;

    [Header("การตั้งค่าเกม")]
    [SerializeField]
    private bool pauseGameWhenOpen = true;

    [Header("การตั้งค่าเสียง")]
    [SerializeField]
    private AudioClip pageTurnSound;

    [Range(0f, 1f)]
    [SerializeField]
    private float pageTurnVolume = 0.7f;

    [Header("ล็อคการกดปุ่ม")]
    [Tooltip("ล็อคการกด Input อื่นๆ เมื่อเปิดสมุด (ยกเว้นปุ่มปิดสมุด)")]
    [SerializeField]
    private bool lockInputWhenOpen = true;

    private int currentRightPageIndex = 0;
    private bool isBookOpen = false;
    private GameObject spawnedCover;
    private PlayerController playerController;
    private AudioSource audioSource;

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

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(GoToNextPage);
        }

        if (prevButton != null)
        {
            prevButton.onClick.AddListener(GoToPrevPage);
        }

        // ค้นหา PlayerController ในฉาก
        playerController = FindObjectOfType<PlayerController>();

        UpdatePageDisplay();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            // ตรวจสอบว่า Pause Menu เปิดอยู่หรือไม่
            PauseMenuWithVolume pauseMenu = FindObjectOfType<PauseMenuWithVolume>();
            if (pauseMenu != null && pauseMenu.IsPaused())
            {
                Debug.Log("?? ไม่สามารถเปิด Tutorial Book ได้ - Pause Menu เปิดอยู่");
                return;
            }

            ToggleBook();
        }

        if (isBookOpen)
        {
            if (Input.GetKeyDown(nextPageKey))
            {
                GoToNextPage();
            }
            else if (Input.GetKeyDown(prevPageKey))
            {
                GoToPrevPage();
            }
        }
    }

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

        // ล็อค/ปลดล็อคการเคลื่อนที่ของผู้เล่น
        if (lockInputWhenOpen && playerController != null)
        {
            if (isBookOpen)
            {
                playerController.LockMovement();
                Debug.Log("?? เปิดสมุด - ล็อคการเคลื่อนที่");
            }
            else
            {
                playerController.UnlockMovement();
                Debug.Log("?? ปิดสมุด - ปลดล็อคการเคลื่อนที่");
            }
        }

        if (isBookOpen)
        {
            currentRightPageIndex = 0;
            UpdatePageDisplay();

            CanvasGroup cg = tutorialBookPanelObject.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = tutorialBookPanelObject.AddComponent<CanvasGroup>();

            RectTransform rt = tutorialBookPanelObject.GetComponent<RectTransform>();

            cg.alpha = 0f;
            Vector2 startPos = rt.anchoredPosition;
            rt.anchoredPosition = startPos - new Vector2(0, 40f);

            DOTween.Kill(tutorialBookPanelObject);

            Sequence seq = DOTween.Sequence();
            seq.Append(rt.DOAnchorPos(startPos, 0.5f).SetEase(Ease.OutCubic));
            seq.Join(cg.DOFade(1f, 0.5f).SetEase(Ease.OutCubic));
            seq.OnComplete(() =>
            {
                rt.anchoredPosition = startPos;
                cg.alpha = 1f;
            });

            seq.SetUpdate(true);
            seq.SetTarget(tutorialBookPanelObject);
        }
        else
        {
            // ปิดสมุด - ทำลายปกที่ spawn ไว้
            if (spawnedCover != null)
            {
                Destroy(spawnedCover);
                spawnedCover = null;
            }
        }
    }

    public void GoToNextPage()
    {
        if (currentRightPageIndex < pageSprites.Count)
        {
            currentRightPageIndex += 2;
            UpdatePageDisplay();
            PlayPageSound();
        }
    }

    public void GoToPrevPage()
    {
        if (currentRightPageIndex > 0)
        {
            currentRightPageIndex -= 2;
            UpdatePageDisplay();
            PlayPageSound();
        }
    }

    private void PlayPageSound()
    {
        if (pageTurnSound != null)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(pageTurnSound, pageTurnVolume);
            }
            else if (audioSource != null)
            {
                audioSource.PlayOneShot(pageTurnSound, pageTurnVolume);
            }
        }
    }

    private void UpdatePageDisplay()
    {
        if (leftPageImage == null || rightPageImage == null)
        {
            return;
        }

        // หน้าปก (index 0)
        if (currentRightPageIndex == 0)
        {
            // ซ่อนกรอบและหน้าเนื้อหา
            if (bookFrameObject != null)
            {
                bookFrameObject.SetActive(false);
            }

            leftPageImage.gameObject.SetActive(false);
            rightPageImage.gameObject.SetActive(false);

            // แสดงปก Prefab
            if (spawnedCover == null && coverPrefab != null)
            {
                Transform parent = coverParent != null ? coverParent : tutorialBookPanelObject.transform;
                spawnedCover = Instantiate(coverPrefab, parent);
                spawnedCover.transform.SetAsFirstSibling(); // วางไว้ด้านหลังสุด
            }

            if (spawnedCover != null)
            {
                spawnedCover.SetActive(true);
            }
        }
        // หน้าเนื้อหา (index > 0)
        else
        {
            // ซ่อนปก แสดงกรอบ
            if (spawnedCover != null)
            {
                spawnedCover.SetActive(false);
            }

            if (bookFrameObject != null)
            {
                bookFrameObject.SetActive(true);
            }

            int leftIndex = currentRightPageIndex - 1;
            int rightIndex = currentRightPageIndex;

            // หน้าซ้าย
            if (leftIndex > 0 && leftIndex <= pageSprites.Count)
            {
                leftPageImage.gameObject.SetActive(true);
                leftPageImage.sprite = pageSprites[leftIndex - 1];
            }
            else
            {
                leftPageImage.gameObject.SetActive(false);
            }

            // หน้าขวา
            if (rightIndex > 0 && rightIndex <= pageSprites.Count)
            {
                rightPageImage.gameObject.SetActive(true);
                rightPageImage.sprite = pageSprites[rightIndex - 1];
            }
            else
            {
                rightPageImage.gameObject.SetActive(false);
            }
        }

        // แสดง/ซ่อนปุ่ม
        if (prevButton != null)
        {
            prevButton.gameObject.SetActive(currentRightPageIndex > 0);
        }

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(currentRightPageIndex < pageSprites.Count);
        }
    }

    void OnDestroy()
    {
        // ปลดล็อคเมื่อ Script ถูกทำลาย
        if (lockInputWhenOpen && playerController != null && isBookOpen)
        {
            playerController.UnlockMovement();
        }
    }

    // ========== Public Methods ==========
    public bool IsBookOpen()
    {
        return isBookOpen;
    }
}