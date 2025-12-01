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

    [Header("?? ระบบปลดล็อคหน้า")]
    [Tooltip("ภาพที่จะแสดงเมื่อหน้านั้นยังล็อคอยู่")]
    [SerializeField]
    private Sprite lockedPageSprite;

    [Tooltip("เริ่มต้นปลดล็อคไว้กี่หน้า (0 = ไม่มีหน้าเลย, 1 = ปกอย่างเดียว)")]
    [SerializeField]
    private int initialUnlockedPages = 0;

    [Header("?? การแจ้งเตือนปลดล็อค")]
    [Tooltip("ระยะเวลาสั่นไอคอน")]
    [SerializeField]
    private float shakeNotificationDuration = 0.6f;

    [Tooltip("ความแรงของการสั่น")]
    [SerializeField]
    private float shakeStrength = 15f;

    [Tooltip("จำนวนครั้งที่สั่น")]
    [SerializeField]
    private int shakeVibrato = 10;

    [Tooltip("เสียงแจ้งเตือนปลดล็อค")]
    [SerializeField]
    private AudioClip unlockNotificationSound;

    [Tooltip("ระยะเวลาที่ไอคอนจะเด้งเมื่อปลดล็อค")]
    [SerializeField]
    private float bounceScale = 1.2f;

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

    // ตัวแปรสถานะ
    private int currentRightPageIndex = 0;
    private bool isBookOpen = false;
    private GameObject spawnedCover;
    private PlayerController playerController;
    private AudioSource audioSource;
    private CanvasGroup bookIconCanvasGroup;
    private bool isAnimating = false;

    // ?? ระบบปลดล็อค
    private HashSet<int> unlockedPages = new HashSet<int>();
    private int highestUnlockedPage = 0;
    private Vector2 bookIconOriginalPosition;

    // Input System Actions
    private InputAction toggleBookAction;
    private InputAction nextPageAction;
    private InputAction prevPageAction;

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
            bookIconOriginalPosition = bookIconUI.GetComponent<RectTransform>().anchoredPosition;
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

        playerController = FindObjectOfType<PlayerController>();

        SetupPageCanvasGroup(leftPageImage);
        SetupPageCanvasGroup(rightPageImage);

        SetupInputActions();

        // ?? เริ่มต้นระบบปลดล็อค
        InitializeUnlockedPages();

        UpdatePageDisplay();

        Debug.Log($"?? TutorialBook Started - {highestUnlockedPage} หน้าปลดล็อคแล้ว");
    }

    // ?? กำหนดหน้าที่ปลดล็อคตั้งแต่แรก
    private void InitializeUnlockedPages()
    {
        unlockedPages.Clear();

        for (int i = 1; i <= initialUnlockedPages; i++)
        {
            unlockedPages.Add(i);
        }

        highestUnlockedPage = initialUnlockedPages;
    }

    // ?? ปลดล็อคหน้าใหม่ (เรียกจาก Trigger หรือ Script อื่น)
    public void UnlockPage(int pageNumber)
    {
        if (pageNumber <= 0 || pageNumber > pageSprites.Count)
        {
            Debug.LogWarning($"?? ไม่สามารถปลดล็อคหน้า {pageNumber} ได้ (ไม่มีหน้านี้ในระบบ)");
            return;
        }

        if (unlockedPages.Contains(pageNumber))
        {
            Debug.Log($"?? หน้า {pageNumber} ปลดล็อคอยู่แล้ว");
            return;
        }

        unlockedPages.Add(pageNumber);

        if (pageNumber > highestUnlockedPage)
        {
            highestUnlockedPage = pageNumber;
        }

        Debug.Log($"?? ปลดล็อคหน้า {pageNumber} สำเร็จ! (รวม {unlockedPages.Count} หน้า)");

        // แสดง Notification
        PlayUnlockNotification();
    }

    // ?? แสดง Animation แจ้งเตือนปลดล็อค
    private void PlayUnlockNotification()
    {
        if (bookIconUI == null) return;

        // หยุด Animation เก่า
        DOTween.Kill(bookIconUI);

        RectTransform iconRT = bookIconUI.GetComponent<RectTransform>();
        if (iconRT == null) return;

        Vector3 originalScale = iconRT.localScale;

        Sequence notifySeq = DOTween.Sequence();

        // 1. Shake
        notifySeq.Append(iconRT.DOShakePosition(
            shakeNotificationDuration,
            strength: shakeStrength,
            vibrato: shakeVibrato,
            randomness: 90,
            snapping: false,
            fadeOut: true
        ));

        // 2. Bounce Scale
        notifySeq.Join(iconRT.DOScale(bounceScale, shakeNotificationDuration * 0.3f)
            .SetEase(Ease.OutBack));

        notifySeq.Append(iconRT.DOScale(originalScale, shakeNotificationDuration * 0.3f)
            .SetEase(Ease.InBack));

        // 3. Pulse (วนซ้ำ 2 รอบ)
        notifySeq.Append(iconRT.DOScale(originalScale * 1.1f, 0.2f).SetEase(Ease.InOutSine));
        notifySeq.Append(iconRT.DOScale(originalScale, 0.2f).SetEase(Ease.InOutSine));
        notifySeq.Append(iconRT.DOScale(originalScale * 1.1f, 0.2f).SetEase(Ease.InOutSine));
        notifySeq.Append(iconRT.DOScale(originalScale, 0.2f).SetEase(Ease.InOutSine));

        notifySeq.OnComplete(() =>
        {
            iconRT.localScale = originalScale;
        });

        notifySeq.SetUpdate(true);
        notifySeq.SetTarget(bookIconUI);

        // เล่นเสียงแจ้งเตือน
        if (unlockNotificationSound != null)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(unlockNotificationSound, 0.8f);
            }
            else if (audioSource != null)
            {
                audioSource.PlayOneShot(unlockNotificationSound, 0.8f);
            }
        }
    }

    // ?? เช็คว่าหน้านั้นปลดล็อคหรือยัง
    public bool IsPageUnlocked(int pageNumber)
    {
        return unlockedPages.Contains(pageNumber);
    }

    // ?? ดึงจำนวนหน้าที่ปลดล็อคแล้ว
    public int GetUnlockedPageCount()
    {
        return unlockedPages.Count;
    }

    private void SetupInputActions()
    {
        toggleBookAction = new InputAction("ToggleBook", type: InputActionType.Button);
        toggleBookAction.AddBinding("<Keyboard>/tab");
        toggleBookAction.AddBinding("<Gamepad>/select");

        nextPageAction = new InputAction("NextPage", type: InputActionType.Button);
        nextPageAction.AddBinding("<Keyboard>/rightArrow");
        nextPageAction.AddBinding("<Gamepad>/rightShoulder");
        nextPageAction.AddBinding("<Gamepad>/dpad/right");

        prevPageAction = new InputAction("PrevPage", type: InputActionType.Button);
        prevPageAction.AddBinding("<Keyboard>/leftArrow");
        prevPageAction.AddBinding("<Gamepad>/leftShoulder");
        prevPageAction.AddBinding("<Gamepad>/dpad/left");

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
        bool togglePressed = toggleBookAction.IsPressed();

        if (togglePressed && !toggleWasPressed)
        {
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

        if (isBookOpen && !isAnimating)
        {
            bool nextPressed = nextPageAction.IsPressed();
            bool prevPressed = prevPageAction.IsPressed();

            if (nextPressed && !nextWasPressed)
            {
                GoToNextPage();
            }
            nextWasPressed = nextPressed;

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

        // หยุดการสั่นถ้ากำลังสั่นอยู่
        DOTween.Kill(bookIconUI);
        if (bookIconUI != null)
        {
            RectTransform iconRT = bookIconUI.GetComponent<RectTransform>();
            if (iconRT != null)
            {
                iconRT.anchoredPosition = bookIconOriginalPosition;
            }
        }

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
            if (spawnedCover != null)
            {
                Destroy(spawnedCover);
                spawnedCover = null;
            }
        }
    }

    private void AnimateBookIcon()
    {
        if (bookIconUI == null || bookIconCanvasGroup == null) return;

        DOTween.Kill(bookIconUI);

        if (isBookOpen)
        {
            bookIconCanvasGroup.DOFade(0f, iconFadeDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .SetTarget(bookIconUI);
        }
        else
        {
            bookIconCanvasGroup.DOFade(1f, iconFadeDuration)
                .SetEase(Ease.InCubic)
                .SetUpdate(true)
                .SetTarget(bookIconUI);
        }
    }

    public void GoToNextPage()
    {
        // ?? จำกัดการพลิกหน้าตามที่ปลดล็อค
        int maxAllowedPage = Mathf.Min(highestUnlockedPage, pageSprites.Count);

        if (currentRightPageIndex < maxAllowedPage && !isAnimating)
        {
            currentRightPageIndex += 2;

            // ถ้าเกินหน้าที่ปลดล็อค ให้กลับไปหน้าสุดท้ายที่ปลดล็อค
            if (currentRightPageIndex > maxAllowedPage)
            {
                currentRightPageIndex = maxAllowedPage;
            }

            AnimatePageFlip(true);
            PlayPageSound();
            Debug.Log($"?? หน้า {currentRightPageIndex}/{maxAllowedPage} (ปลดล็อคแล้ว)");
        }
        else if (currentRightPageIndex >= maxAllowedPage)
        {
            Debug.Log("?? ถึงหน้าสุดท้ายที่ปลดล็อคแล้ว");
        }
    }

    public void GoToPrevPage()
    {
        if (currentRightPageIndex > 0 && !isAnimating)
        {
            currentRightPageIndex -= 2;
            AnimatePageFlip(false);
            PlayPageSound();
            Debug.Log($"?? หน้า {currentRightPageIndex}/{highestUnlockedPage}");
        }
    }

    private void AnimatePageFlip(bool isNext)
    {
        if (leftPageImage == null || rightPageImage == null) return;

        isAnimating = true;

        DOTween.Kill(leftPageImage.gameObject);
        DOTween.Kill(rightPageImage.gameObject);

        CanvasGroup leftCG = leftPageImage.GetComponent<CanvasGroup>();
        CanvasGroup rightCG = rightPageImage.GetComponent<CanvasGroup>();

        RectTransform leftRT = leftPageImage.GetComponent<RectTransform>();
        RectTransform rightRT = rightPageImage.GetComponent<RectTransform>();

        Vector3 leftOriginalRot = leftRT.localEulerAngles;
        Vector3 rightOriginalRot = rightRT.localEulerAngles;
        Vector3 leftOriginalScale = leftRT.localScale;
        Vector3 rightOriginalScale = rightRT.localScale;

        Sequence seq = DOTween.Sequence();

        if (isNext)
        {
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
            if (bookFrameObject != null)
            {
                bookFrameObject.SetActive(false);
            }

            leftPageImage.gameObject.SetActive(false);
            rightPageImage.gameObject.SetActive(false);

            if (spawnedCover == null && coverPrefab != null)
            {
                Transform parent = coverParent != null ? coverParent : tutorialBookPanelObject.transform;
                spawnedCover = Instantiate(coverPrefab, parent);
                spawnedCover.transform.SetAsFirstSibling();
            }

            if (spawnedCover != null)
            {
                spawnedCover.SetActive(true);
            }
        }
        // หน้าเนื้อหา (index > 0)
        else
        {
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

            // ?? หน้าซ้าย - แสดงล็อคหรือปลดล็อค
            if (leftIndex > 0 && leftIndex <= pageSprites.Count)
            {
                leftPageImage.gameObject.SetActive(true);

                if (IsPageUnlocked(leftIndex))
                {
                    leftPageImage.sprite = pageSprites[leftIndex - 1];
                }
                else
                {
                    leftPageImage.sprite = lockedPageSprite;
                }
            }
            else
            {
                leftPageImage.gameObject.SetActive(false);
            }

            // ?? หน้าขวา - แสดงล็อคหรือปลดล็อค
            if (rightIndex > 0 && rightIndex <= pageSprites.Count)
            {
                rightPageImage.gameObject.SetActive(true);

                if (IsPageUnlocked(rightIndex))
                {
                    rightPageImage.sprite = pageSprites[rightIndex - 1];
                }
                else
                {
                    rightPageImage.sprite = lockedPageSprite;
                }
            }
            else
            {
                rightPageImage.gameObject.SetActive(false);
            }
        }

        // ?? ปุ่ม Prev
        if (prevButton != null)
        {
            prevButton.gameObject.SetActive(currentRightPageIndex > 0);
        }

        // ?? ปุ่ม Next - แสดงถ้ายังมีหน้าที่ปลดล็อคต่อไป
        if (nextButton != null)
        {
            int maxAllowedPage = Mathf.Min(highestUnlockedPage, pageSprites.Count);
            nextButton.gameObject.SetActive(currentRightPageIndex < maxAllowedPage);
        }
    }

    void OnDestroy()
    {
        toggleBookAction?.Dispose();
        nextPageAction?.Dispose();
        prevPageAction?.Dispose();

        if (lockInputWhenOpen && playerController != null && isBookOpen)
        {
            playerController.UnlockMovement();
        }

        DOTween.Kill(tutorialBookPanelObject);
        if (bookIconUI != null) DOTween.Kill(bookIconUI);
        if (leftPageImage != null) DOTween.Kill(leftPageImage.gameObject);
        if (rightPageImage != null) DOTween.Kill(rightPageImage.gameObject);
    }

    public bool IsBookOpen()
    {
        return isBookOpen;
    }

    public int GetCurrentPage()
    {
        return currentRightPageIndex;
    }

    public bool IsViewingPage(int pageNumber)
    {
        if (!isBookOpen) return false;

        // เช็คทั้งหน้าซ้ายและหน้าขวา
        int leftPage = currentRightPageIndex - 1;
        int rightPage = currentRightPageIndex;

        return (pageNumber == leftPage || pageNumber == rightPage);
    }

    public void UnlockPageWithShake(int pageNumber)
    {
        // ปลดล็อคหน้าตามปกติ
        UnlockPage(pageNumber);

        // เริ่มสั่นไอคอน
        StartIconShake();
    }

    public void StartIconShake()
    {
        if (bookIconUI == null) return;

        // หยุด Animation เก่า
        DOTween.Kill(bookIconUI);

        RectTransform iconRT = bookIconUI.GetComponent<RectTransform>();
        if (iconRT == null) return;

        // สั่นแบบ Loop ไม่รู้จบ
        iconRT.DOShakePosition(
            duration: 999f, // สั่นนานมากๆ (จนกว่าจะหยุดด้วยตัวเอง)
            strength: 10f,
            vibrato: 20,
            randomness: 90,
            snapping: false,
            fadeOut: false
        )
        .SetLoops(-1, LoopType.Restart) // Loop ไม่รู้จบ
        .SetUpdate(true)
        .SetTarget(bookIconUI);

        Debug.Log("? เริ่มสั่นไอคอนหนังสือ UI");
    }

    public void StopIconShake()
    {
        if (bookIconUI == null) return;

        DOTween.Kill(bookIconUI);

        RectTransform iconRT = bookIconUI.GetComponent<RectTransform>();
        if (iconRT != null)
        {
            iconRT.anchoredPosition = bookIconOriginalPosition; // ใช้ตำแหน่งที่เก็บไว้
        }

        Debug.Log("?? หยุดสั่นไอคอนหนังสือ UI");
    }
}