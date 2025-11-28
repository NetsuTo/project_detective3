using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;

public class SceneChangerEndGame : MonoBehaviour
{
    [Header("การตั้งค่า Scene")]
    [Tooltip("ชื่อ Scene ที่จะเปลี่ยนไป (ต้องเพิ่มใน Build Settings)")]
    public string endGameSceneName = "EndGame";

    [Header("การตรวจจับผู้เล่น")]
    public float detectionRange = 2f;
    public GameObject pressEIndicator;

    [Header("เสียง")]
    public AudioClip changeSceneSound;
    [Range(0f, 1f)] public float soundVolume = 0.8f;

    [Header("เอฟเฟค (ถ้ามี)")]
    public ParticleSystem transitionEffect;

    [Header("หน่วงเวลา")]
    public float delayBeforeChange = 1f;

    [Header("?? การเชื่อมต่อ")]
    [Tooltip("ลาก TimerController มาใส่ (หรือปล่อยว่างให้หาอัตโนมัติ)")]
    public TimerController timerController;

    [Tooltip("ลาก ExplosionController มาใส่ (หรือปล่อยว่างให้หาอัตโนมัติ)")]
    public ExplosionController explosionController;

    private bool playerInRange = false;
    private bool isChanging = false;
    private AudioSource audioSource;

    // ===== Input System Actions - รองรับ Keyboard + Gamepad =====
    private InputAction interactAction;

    void Awake()
    {
        // สร้าง Input Actions
        SetupInputActions();
        interactAction?.Enable();

        Debug.Log("? SceneChangerEndGame - Input System Ready (Keyboard + Gamepad)!");
    }

    private void SetupInputActions()
    {
        // ===== Interact (E / Button North) สำหรับจบเกม =====
        interactAction = new InputAction("End Game", type: InputActionType.Button);
        interactAction.AddBinding("<Keyboard>/e");
        interactAction.AddBinding("<Gamepad>/buttonNorth");  // Xbox: Y, PS: Triangle
        interactAction.performed += OnInteractPerformed;
    }

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        if (pressEIndicator != null)
            pressEIndicator.SetActive(false);

        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning("?? [SceneChangerEndGame] ไม่พบ Collider! กำลังสร้าง SphereCollider...");
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = detectionRange;
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning("?? [SceneChangerEndGame] Collider ต้องเปิด Is Trigger!");
            col.isTrigger = true;
        }

        if (timerController == null)
        {
            timerController = FindObjectOfType<TimerController>();
            if (timerController != null)
            {
                Debug.Log("? [SceneChangerEndGame] เจอ TimerController แล้ว!");
            }
        }

        if (explosionController == null)
        {
            explosionController = FindObjectOfType<ExplosionController>();
            if (explosionController != null)
            {
                Debug.Log("? [SceneChangerEndGame] เจอ ExplosionController แล้ว!");
            }
        }

        if (SceneUtility.GetBuildIndexByScenePath(endGameSceneName) == -1)
        {
            Debug.LogError($"? Scene '{endGameSceneName}' ไม่อยู่ใน Build Settings! กรุณาเพิ่มใน File > Build Settings");
        }

        Debug.Log($"? SceneChangerEndGame พร้อมใช้งาน - จะเปลี่ยนไป '{endGameSceneName}'");
    }

    void Update()
    {
        // ? Fallback สำหรับ Old Input System
        if (Keyboard.current == null && Gamepad.current == null)
        {
            if (playerInRange && Input.GetKeyDown(KeyCode.E) && !isChanging)
            {
                StartCoroutine(ChangeToEndGame());
            }
        }
    }

    private void OnEnable()
    {
        interactAction?.Enable();
    }

    private void OnDisable()
    {
        interactAction?.Disable();
    }

    // ===== Input Actions Callback =====
    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        if (!playerInRange || isChanging) return;

        Debug.Log("?? กด Interact (E / Y/Triangle) - จบเกม!");
        StartCoroutine(ChangeToEndGame());
    }

    IEnumerator ChangeToEndGame()
    {
        isChanging = true;

        Debug.Log($"?? กำลังเปลี่ยนไป Scene '{endGameSceneName}'...");

        // ?? หยุด Timer ทันทีที่ผู้เล่นจบ!
        if (timerController != null)
        {
            timerController.StopTimer();
            Debug.Log("?? หยุด Timer - ผู้เล่นจบเกมแล้ว!");
        }

        // ?? หยุดเอฟเฟกต์การระเบิดทั้งหมด
        if (explosionController != null)
        {
            explosionController.StopContinuousShake();
            explosionController.StopContinuousDebris();
            explosionController.StopCaveCollapseSound();
            Debug.Log("?? หยุดเอฟเฟกต์ระเบิดทั้งหมด");
        }

        if (pressEIndicator != null)
            pressEIndicator.SetActive(false);

        if (changeSceneSound != null)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(changeSceneSound, soundVolume);
            else if (audioSource != null)
                audioSource.PlayOneShot(changeSceneSound, soundVolume);

            Debug.Log("?? เล่นเสียงเปลี่ยน Scene");
        }

        if (transitionEffect != null)
        {
            ParticleSystem fx = Instantiate(transitionEffect, transform.position, Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject, 3f);
            Debug.Log("? เล่นเอฟเฟคเปลี่ยน Scene");
        }

        yield return new WaitForSeconds(delayBeforeChange);

        Debug.Log($"?? เปลี่ยนไป Scene '{endGameSceneName}' เรียบร้อย!");
        SceneManager.LoadScene(endGameSceneName);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;

            if (pressEIndicator != null && !isChanging)
                pressEIndicator.SetActive(true);

            Debug.Log("?? ผู้เล่นเข้าใกล้จุดจบ");
            Debug.Log("?? กด E / Y(Triangle) เพื่อจบเกม");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;

            if (pressEIndicator != null)
                pressEIndicator.SetActive(false);

            Debug.Log("?? ผู้เล่นออกจากจุดจบ");
        }
    }

    private void OnDestroy()
    {
        if (interactAction != null)
        {
            interactAction.performed -= OnInteractPerformed;
            interactAction.Dispose();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}