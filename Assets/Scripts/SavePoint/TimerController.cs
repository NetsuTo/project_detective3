using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ระบบจับเวลาแบบง่าย: หมดเวลา ? ตกลงข้างล่าง ? นับเวลาใหม่
/// </summary>
public class TimerController : MonoBehaviour
{
    [Header("? ตั้งค่าเวลา")]
    [Tooltip("เวลาที่ให้ผู้เล่นหนี (วินาที)")]
    public float timeLimit = 60f;

    [Header("?? จุดตก")]
    [Tooltip("จุดที่ผู้เล่นจะตกลงมา (ข้างล่าง)")]
    public Transform fallPosition;

    [Header("?? UI")]
    public Text timerText;
    public CanvasGroup timerCanvasGroup;
    public Image timerBackground;

    [Header("?? สี")]
    public Color normalColor = Color.white;
    public Color warningColor = Color.yellow;
    public Color dangerColor = Color.red;
    public float warningTime = 20f;
    public float dangerTime = 10f;

    [Header("?? เสียง")]
    public AudioClip tickSound;
    public float tickStartTime = 10f;
    public AudioClip timeOutSound;

    [Header("?? ตั้งค่า")]
    public float fadeDuration = 0.5f;
    public bool enableBlinking = true;
    public float blinkSpeed = 2f;
    public float restartTimerDelay = 2f;

    private float currentTime;
    private bool isTimerRunning = false;
    private AudioSource audioSource;
    private Coroutine blinkCoroutine;
    private GameObject player;
    private float lastTickTime = -1f;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("[TimerController] ?? ไม่พบ Player!");
        }

        SetTimerVisibility(false);
        if (timerCanvasGroup != null)
        {
            timerCanvasGroup.alpha = 0f;
        }

        Debug.Log($"[TimerController] ? พร้อม! เวลา: {timeLimit} วินาที");
    }

    void Update()
    {
        if (isTimerRunning)
        {
            currentTime -= Time.deltaTime;
            UpdateTimerDisplay();

            if (currentTime <= tickStartTime && currentTime > 0)
            {
                PlayTickSound();
            }

            if (currentTime <= 0)
            {
                TimeOut();
            }
        }
    }

    public void StartTimer()
    {
        if (isTimerRunning)
        {
            Debug.Log("[TimerController] ?? Timer กำลังทำงานอยู่!");
            return;
        }

        currentTime = timeLimit;
        isTimerRunning = true;
        lastTickTime = -1f;

        StartCoroutine(FadeInTimer());

        if (enableBlinking)
        {
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
            }
            blinkCoroutine = StartCoroutine(BlinkWhenDanger());
        }

        Debug.Log($"? เริ่มจับเวลา! {timeLimit} วินาที");
    }

    public void StopTimer()
    {
        if (!isTimerRunning) return;

        isTimerRunning = false;

        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        StartCoroutine(FadeOutTimer());
        Debug.Log("? หยุด Timer");
    }

    private void UpdateTimerDisplay()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(Mathf.Max(0, currentTime) / 60f);
        int seconds = Mathf.FloorToInt(Mathf.Max(0, currentTime) % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        UpdateTimerColor();
    }

    private void UpdateTimerColor()
    {
        if (timerBackground == null) return;

        if (currentTime <= dangerTime)
        {
            timerBackground.color = dangerColor;
            if (timerText != null) timerText.color = Color.white;
        }
        else if (currentTime <= warningTime)
        {
            timerBackground.color = warningColor;
            if (timerText != null) timerText.color = Color.black;
        }
        else
        {
            timerBackground.color = normalColor;
            if (timerText != null) timerText.color = Color.black;
        }
    }

    private void PlayTickSound()
    {
        int currentSecond = Mathf.FloorToInt(currentTime);

        if (tickSound != null && currentSecond != lastTickTime && currentSecond >= 0)
        {
            lastTickTime = currentSecond;

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(tickSound, 0.5f);
            else if (audioSource != null)
                audioSource.PlayOneShot(tickSound, 0.5f);
        }
    }

    private void TimeOut()
    {
        isTimerRunning = false;

        Debug.Log("? หมดเวลา! ตกลงข้างล่าง...");

        // เล่นเสียง
        if (timeOutSound != null)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(timeOutSound);
            else if (audioSource != null)
                audioSource.PlayOneShot(timeOutSound);
        }

        // ตกลงข้างล่าง
        StartCoroutine(FallAndRestart());
    }

    private IEnumerator FallAndRestart()
    {
        // Fade Out Timer
        if (timerCanvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                timerCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            timerCanvasGroup.alpha = 0f;
        }

        SetTimerVisibility(false);

        // ย้ายผู้เล่นลงข้างล่าง
        if (player != null)
        {
            CharacterController charController = player.GetComponent<CharacterController>();

            if (charController != null)
            {
                charController.enabled = false;
            }

            if (fallPosition != null)
            {
                // ใช้ตำแหน่งที่กำหนด
                player.transform.position = fallPosition.position;
                player.transform.rotation = fallPosition.rotation;
            }
            else
            {
                // ตกลงข้างล่าง (ลด Y)
                Vector3 newPos = player.transform.position;
                newPos.y -= 20f; // ตกลง 20 หน่วย
                player.transform.position = newPos;
            }

            if (charController != null)
            {
                charController.enabled = true;
            }

            Debug.Log("?? ผู้เล่นตกลงข้างล่างแล้ว");
        }

        // รอสักครู่
        yield return new WaitForSeconds(restartTimerDelay);

        // เริ่มนับเวลาใหม่
        Debug.Log("?? เริ่มนับเวลาใหม่!");
        StartTimer();
    }

    private IEnumerator FadeInTimer()
    {
        SetTimerVisibility(true);

        if (timerCanvasGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            timerCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        timerCanvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOutTimer()
    {
        if (timerCanvasGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            timerCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        timerCanvasGroup.alpha = 0f;
        SetTimerVisibility(false);
    }

    private IEnumerator BlinkWhenDanger()
    {
        while (isTimerRunning)
        {
            if (currentTime <= dangerTime && timerCanvasGroup != null)
            {
                float alpha = Mathf.PingPong(Time.time * blinkSpeed, 1f);
                timerCanvasGroup.alpha = Mathf.Lerp(0.5f, 1f, alpha);
            }

            yield return null;
        }

        if (timerCanvasGroup != null)
        {
            timerCanvasGroup.alpha = 1f;
        }
    }

    private void SetTimerVisibility(bool visible)
    {
        if (timerCanvasGroup != null)
        {
            timerCanvasGroup.gameObject.SetActive(visible);
        }
    }

    public void AddTime(float seconds)
    {
        if (!isTimerRunning) return;
        currentTime += seconds;
        Debug.Log($"? +{seconds} วินาที!");
    }

    public bool IsTimerRunning()
    {
        return isTimerRunning;
    }

    public float GetRemainingTime()
    {
        return currentTime;
    }
}