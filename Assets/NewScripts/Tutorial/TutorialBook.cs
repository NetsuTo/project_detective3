using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using DG.Tweening;

[RequireComponent(typeof(AudioSource))]
public class TutorialBook : MonoBehaviour
{
    [Header("การตั้งค่าหลัก")]
    [Tooltip("ปุ่มที่ใช้เปิด/ปิดสมุด (Tab)")]
    [SerializeField]
    private KeyCode toggleKey = KeyCode.Tab;

    [Header("ส่วนประกอบ UI (ลากมาใส่)")]
    [SerializeField]
    private GameObject tutorialBookPanelObject;

    [Tooltip("ไอคอนสมุดมุมขวาบน")]
    [SerializeField]
    private GameObject bookIconUI;

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

    [Header("การตั้งค่า Animation")]
    [Tooltip("ระยะเวลา Fade In/Out ของไอคอนสมุด")]
    [SerializeField]
    private float iconFadeDuration = 0.3f;

    [Tooltip("ระยะเวลาการพลิกหน้า")]
    [SerializeField]
    private float pageFlipDuration = 0.4f;

    private int currentRightPageIndex = 0;
    private bool isBookOpen = false;
    private GameObject spawnedCover;
    private PlayerController playerController;
    private AudioSource audioSource;
    private CanvasGroup bookIconCanvasGroup;
    private bool isAnimating = false;

    // ===== Input System Actions - รองรับ Keyboard + Gamepad =====
    private InputAction toggleBookAction;
    private InputAction nextPageAction;
    private InputAction prevPageAction;

    // ป้องกันการกดซ้ำ
    private bool toggleWasPressed = false;
    private bool nextWasPressed = false;
    private bool prevWasPressed = false;

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

        // ตั้งค่า CanvasGroup สำหรับไอคอนสมุด
        if (bookIconUI != null)
        {
            bookIconCanvasGroup = bookIconUI.GetComponent<CanvasGroup>();
            if (bookIconCanvasGroup == null)
            {
                bookIconCanvasGroup = bookIconUI.AddComponent<CanvasGroup>();
            }
            bookIconCanvasGroup.alpha = 1f;
            bookIconUI.SetActive(true);
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

        // เพิ่ม CanvasGroup ให้หน้าหนังสือ
        SetupPageCanvasGroup(leftPageImage);
        SetupPageCanvasGroup(rightPageImage);

        // สร้าง Input Actions
        SetupInputActions();

        UpdatePageDisplay();

        Debug.Log("? TutorialBook Started - Keyboard + Gamepad Ready!");
    }

    private void SetupInputActions()
    {
        // ===== Toggle Book - รองรับ Tab และ Select Button =====
        toggleBookAction = new InputAction("ToggleBook", type: InputActionType.Button);
        toggleBookAction.AddBinding("<Keyboard>/tab");
        toggleBookAction.AddBinding("<Gamepad>/select");  // Select/Back button (Xbox: View, PS: Share)

        // ===== Next Page - รองรับ Right Arrow และ Shoulder Buttons =====
        nextPageAction = new InputAction("NextPage", type: InputActionType.Button);
        nextPageAction.AddBinding("<Keyboard>/rightArrow");
        nextPageAction.AddBinding("<Gamepad>/rightShoulder");  // RB/R1
        nextPageAction.AddBinding("<Gamepad>/dpad/right");

        // ===== Previous Page - รองรับ Left Arrow และ Shoulder Buttons =====
        prevPageAction = new InputAction("PrevPage", type: InputActionType.Button);
        prevPageAction.AddBinding("<Keyboard>/leftArrow");
        prevPageAction.AddBinding("<Gamepad>/leftShoulder");   // LB/L1
        prevPageAction.AddBinding("<Gamepad>/dpad/left");

        // Enable Toggle ตลอดเวลา
        toggleBookAction.Enable();
    }

    private void OnEnable()
    {
        toggleBookAction?.Enable();
    }

    private void OnDisable()
    {
        toggleBookAction?.Disable();
        nextPageAction?.Disable();
        prevPageAction?.Disable();
    }

    void Update()
    {
        // ===== อ่าน Toggle Book Input ตลอดเวลา =====
        bool togglePressed = toggleBookAction.IsPressed();

        if (togglePressed && !toggleWasPressed)
        {
            // ตรวจสอบว่า Pause Menu เปิดอยู่หรือไม่
            PauseMenuWithVolume pauseMenu = FindObjectOfType<PauseMenuWithVolume>();
            if (pauseMenu != null && pauseMenu.IsPaused())
            {
                Debug.Log("?? ไม่สามารถเปิด Tutorial Book ได้ - Pause Menu เปิดอยู่");
            }
            else
            {
                ToggleBook();
            }
        }
        toggleWasPressed = togglePressed;

        // ===== อ่าน Page Navigation Input เมื่อเปิดสมุด =====
        if (isBookOpen && !isAnimating)
        {
            bool nextPressed = nextPageAction.IsPressed();
            bool prevPressed = prevPageAction.IsPressed();

            // Next Page
            if (nextPressed && !nextWasPressed)
            {
                GoToNextPage();
            }
            nextWasPressed = nextPressed;

            // Previous Page
            if (prevPressed && !prevWasPressed)
            {
                GoToPrevPage();
            }
            prevWasPressed = prevPressed;
        }
    }

    void SetupPageCanvasGroup(Image pageImage)
    {
        if (pageImage != null)
        {
            CanvasGroup cg = pageImage.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                pageImage.gameObject.AddComponent<CanvasGroup>();
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

        // เปิด/ปิด Input Actions สำหรับพลิกหน้า
        if (isBookOpen)
        {
            nextPageAction?.Enable();
            prevPageAction?.Enable();
            Debug.Log("?? เปิดสมุด - ใช้ LB/RB หรือ ?/? พลิกหน้า");
        }
        else
        {
            nextPageAction?.Disable();
            prevPageAction?.Disable();
            Debug.Log("?? ปิดสมุด");
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

        // Animate ไอคอนสมุดมุมขวาบน
        AnimateBookIcon();

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

    // Animate Fade In/Out ไอคอนสมุด
    private void AnimateBookIcon()
    {
        if (bookIconUI == null || bookIconCanvasGroup == null) return;

        DOTween.Kill(bookIconUI);

        if (isBookOpen)
        {
            // Fade Out เมื่อเปิดสมุด
            bookIconCanvasGroup.DOFade(0f, iconFadeDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .SetTarget(bookIconUI);
        }
        else
        {
            // Fade In เมื่อปิดสมุด
            bookIconCanvasGroup.DOFade(1f, iconFadeDuration)
                .SetEase(Ease.InCubic)
                .SetUpdate(true)
                .SetTarget(bookIconUI);
        }
    }

    public void GoToNextPage()
    {
        if (currentRightPageIndex < pageSprites.Count && !isAnimating)
        {
            currentRightPageIndex += 2;
            AnimatePageFlip(true);
            PlayPageSound();
            Debug.Log($"?? หน้า {currentRightPageIndex}/{pageSprites.Count}");
        }
    }

    public void GoToPrevPage()
    {
        if (currentRightPageIndex > 0 && !isAnimating)
        {
            currentRightPageIndex -= 2;
            AnimatePageFlip(false);
            PlayPageSound();
            Debug.Log($"?? หน้า {currentRightPageIndex}/{pageSprites.Count}");
        }
    }

    // Animation พลิกหน้าหนังสือ
    private void AnimatePageFlip(bool isNext)
    {
        if (leftPageImage == null || rightPageImage == null) return;

        isAnimating = true;

        // หยุด Animation เก่า
        DOTween.Kill(leftPageImage.gameObject);
        DOTween.Kill(rightPageImage.gameObject);

        CanvasGroup leftCG = leftPageImage.GetComponent<CanvasGroup>();
        CanvasGroup rightCG = rightPageImage.GetComponent<CanvasGroup>();

        RectTransform leftRT = leftPageImage.GetComponent<RectTransform>();
        RectTransform rightRT = rightPageImage.GetComponent<RectTransform>();

        // เก็บ Rotation และ Scale เดิม
        Vector3 leftOriginalRot = leftRT.localEulerAngles;
        Vector3 rightOriginalRot = rightRT.localEulerAngles;
        Vector3 leftOriginalScale = leftRT.localScale;
        Vector3 rightOriginalScale = rightRT.localScale;

        Sequence seq = DOTween.Sequence();

        if (isNext)
        {
            // พลิกไปข้างหน้า - หน้าขวาปัดไปซ้าย
            seq.Append(rightCG.DOFade(0f, pageFlipDuration * 0.5f).SetEase(Ease.InQuad));
            seq.Join(rightRT.DOScale(new Vector3(0.95f, 0.95f, 1f), pageFlipDuration * 0.5f).SetEase(Ease.InQuad));
            seq.Join(rightRT.DOLocalRotate(new Vector3(0, -15f, 0), pageFlipDuration * 0.5f).SetEase(Ease.InQuad));

            seq.AppendCallback(() =>
            {
                UpdatePageDisplay();
                leftCG.alpha = 0f;
                rightCG.alpha = 0f;
                leftRT.localScale = new Vector3(0.95f, 0.95f, 1f);
                rightRT.localScale = new Vector3(0.95f, 0.95f, 1f);
                leftRT.localEulerAngles = new Vector3(0, 15f, 0);
                rightRT.localEulerAngles = new Vector3(0, 15f, 0);
            });

            seq.Append(leftCG.DOFade(1f, pageFlipDuration * 0.5f).SetEase(Ease.OutQuad));
            seq.Join(rightCG.DOFade(1f, pageFlipDuration * 0.5f).SetEase(Ease.OutQuad));
            seq.Join(leftRT.DOScale(leftOriginalScale, pageFlipDuration * 0.5f).SetEase(Ease.OutQuad));
            seq.Join(rightRT.DOScale(rightOriginalScale, pageFlipDuration * 0.5f).SetEase(Ease.OutQuad));
            seq.Join(leftRT.DOLocalRotate(leftOriginalRot, pageFlipDuration * 0.5f).SetEase(Ease.OutQuad));
            seq.Join(rightRT.DOLocalRotate(rightOriginalRot, pageFlipDuration * 0.5f).SetEase(Ease.OutQuad));
        }
        else
        {
            // พลิกถอยหลัง - หน้าซ้ายปัดไปขวา
            seq.Append(leftCG.DOFade(0f, pageFlipDuration * 0.5f).SetEase(Ease.InQuad));
            seq.Join(leftRT.DOScale(new Vector3(0.95f, 0.95f, 1f), pageFlipDuration * 0.5f).SetEase(Ease.InQuad));
            seq.Join(leftRT.DOLocalRotate(new Vector3(0, 15f, 0), pageFlipDuration * 0.5f).SetEase(Ease.InQuad));

            seq.AppendCallback(() =>
            {
                UpdatePageDisplay();
                leftCG.alpha = 0f;
                rightCG.alpha = 0f;
                leftRT.localScale = new Vector3(0.95f, 0.95f, 1f);
                rightRT.localScale = new Vector3(0.95f, 0.95f, 1f);
                leftRT.localEulerAngles = new Vector3(0, -15f, 0);
                rightRT.localEulerAngles = new Vector3(0, -15f, 0);
            });

            seq.Append(leftCG.DOFade(1f, pageFlipDuration * 0.5f).SetEase(Ease.OutQuad));
            seq.Join(rightCG.DOFade(1f, pageFlipDuration * 0.5f).SetEase(Ease.OutQuad));
            seq.Join(leftRT.DOScale(leftOriginalScale, pageFlipDuration * 0.5f).SetEase(Ease.OutQuad));
            seq.Join(rightRT.DOScale(rightOriginalScale, pageFlipDuration * 0.5f).SetEase(Ease.OutQuad));
            seq.Join(leftRT.DOLocalRotate(leftOriginalRot, pageFlipDuration * 0.5f).SetEase(Ease.OutQuad));
            seq.Join(rightRT.DOLocalRotate(rightOriginalRot, pageFlipDuration * 0.5f).SetEase(Ease.OutQuad));
        }

        seq.OnComplete(() =>
        {
            isAnimating = false;
            leftRT.localEulerAngles = leftOriginalRot;
            rightRT.localEulerAngles = rightOriginalRot;
            leftRT.localScale = leftOriginalScale;
            rightRT.localScale = rightOriginalScale;
            leftCG.alpha = 1f;
            rightCG.alpha = 1f;
        });

        seq.SetUpdate(true);
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
        // Cleanup Input Actions
        toggleBookAction?.Dispose();
        nextPageAction?.Dispose();
        prevPageAction?.Dispose();

        // ปลดล็อคเมื่อ Script ถูกทำลาย
        if (lockInputWhenOpen && playerController != null && isBookOpen)
        {
            playerController.UnlockMovement();
        }

        // Kill DOTween animations
        DOTween.Kill(tutorialBookPanelObject);
        if (bookIconUI != null) DOTween.Kill(bookIconUI);
        if (leftPageImage != null) DOTween.Kill(leftPageImage.gameObject);
        if (rightPageImage != null) DOTween.Kill(rightPageImage.gameObject);
    }

    // ========== Public Methods ==========
    public bool IsBookOpen()
    {
        return isBookOpen;
    }
}