using UnityEngine;
using UnityEngine.InputSystem;

public class SkillPickup : MonoBehaviour
{
    [Header("Skill Settings")]
    public string skillID;

    [Header("Sound Effects")]
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float pickupVolume = 0.8f;

    private bool playerInRange = false;
    private PlayerController playerController;
    private PlayerSkillManager manager;
    private AudioSource audioSource;

    // ===== Input System Detection =====
    private bool useNewInputSystem = false;
    private bool inputSystemChecked = false;

    // ===== Input System Actions =====
    private InputAction interactAction;

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        // ตรวจสอบว่าใช้ระบบไหน
        DetectInputSystem();

        if (useNewInputSystem)
        {
            SetupInputActions();
            interactAction?.Enable();
        }
    }

    private void DetectInputSystem()
    {
        // ตรวจสอบว่ามี Keyboard.current หรือไม่
        if (Keyboard.current != null)
        {
            useNewInputSystem = true;
            Debug.Log("?? [SkillPickup] ใช้ New Input System (Input System Package)");
        }
        else
        {
            useNewInputSystem = false;
            Debug.Log("?? [SkillPickup] ใช้ Old Input System (Input Manager)");
        }
        inputSystemChecked = true;
    }

    private void SetupInputActions()
    {
        interactAction = new InputAction("Interact", binding: "<Keyboard>/e");
        interactAction.performed += OnInteractPerformed;
    }

    void Update()
    {
        if (!playerInRange) return;

        // แสดงข้อความแนะนำครั้งเดียวตอน Player เข้ามา
        if (!inputSystemChecked)
        {
            DetectInputSystem();
        }

        // ใช้ระบบที่เหมาะสม
        if (useNewInputSystem)
        {
            // New Input System
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                Debug.Log("?? [New Input] กด E");
                OnInteractPerformed(default);
            }
        }
        else
        {
            // Old Input System
            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("?? [Old Input] กด E");
                OnInteractPerformed(default);
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

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        if (!playerInRange) return;

        Debug.Log("?? กด E ขณะอยู่ในระยะ pickup");

        if (playerController != null)
        {
            Debug.Log("?? เรียก animation pickup");
            playerController.PlayPickupAnimation(() =>
            {
                if (manager != null && manager.CanPickupSkill(skillID))
                {
                    Debug.Log("? เก็บสกิลสำเร็จ: " + skillID);
                    PlayPickupSound();
                    manager.PickupSkill(skillID);

                    if (pickupSound != null)
                    {
                        Destroy(gameObject, pickupSound.length);
                    }
                    else
                    {
                        Destroy(gameObject);
                    }
                }
                else
                {
                    Debug.Log("?? ไม่สามารถเก็บสกิลได้ หรือ Inventory เต็ม");
                }
            });
        }
    }

    private void PlayPickupSound()
    {
        if (pickupSound != null)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(pickupSound, pickupVolume);
                Debug.Log("?? เล่นเสียงเก็บไอเทมผ่าน AudioManager");
            }
            else if (audioSource != null)
            {
                audioSource.PlayOneShot(pickupSound, pickupVolume);
                Debug.Log("?? เล่นเสียงเก็บไอเทมผ่าน AudioSource");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"?? Player เข้ามาในระยะ pickup (ใช้ระบบ: {(useNewInputSystem ? "New Input" : "Old Input")})");
            playerInRange = true;
            playerController = other.GetComponent<PlayerController>();
            manager = other.GetComponent<PlayerSkillManager>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("?? Player ออกจากระยะ pickup");
            playerInRange = false;
            playerController = null;
            manager = null;
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
}