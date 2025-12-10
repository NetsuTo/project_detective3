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

    // ===== Input System Actions - รองรับ Keyboard + Gamepad =====
    private InputAction interactAction;

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        // สร้าง Input Actions
        SetupInputActions();
        interactAction?.Enable();

        Debug.Log("?? SkillPickup Started - Keyboard + Gamepad Ready!");
    }

    private void SetupInputActions()
    {
        // ===== Interact - รองรับ E และ Button West (X/Square) =====
        interactAction = new InputAction("Interact", type: InputActionType.Button);
        interactAction.AddBinding("<Keyboard>/e");
        interactAction.AddBinding("<Gamepad>/buttonWest");  // Xbox: X, PS: Square
        interactAction.performed += OnInteractPerformed;
    }

    // ?? Update() สำหรับ Fallback (Old Input System)
    void Update()
    {
        if (!playerInRange) return;

        // Fallback สำหรับ Old Input System
        if (Keyboard.current == null && Gamepad.current == null)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("??? [Old Input] กด E");
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

        Debug.Log("? กด Interact (E / X/Square) - เริ่มเก็บสกิล");

        if (playerController != null)
        {
            Debug.Log("?? เล่น Animation Pickup");
            playerController.PlayPickupAnimation(() =>
            {
                if (manager != null && manager.CanPickupSkill(skillID))
                {
                    Debug.Log($"? เก็บสกิลสำเร็จ: {skillID}");
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
                    Debug.Log("? ไม่สามารถเก็บสกิลได้ หรือ Inventory เต็ม");
                }
            });
        }
        else
        {
            Debug.LogWarning("?? PlayerController not found!");
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
            Debug.Log($"?? Player เข้ามาในระยะ Pickup Skill [{skillID}]");
            Debug.Log("?? กด E หรือ X/Square เพื่อเก็บ");

            playerInRange = true;
            playerController = other.GetComponent<PlayerController>();
            manager = other.GetComponent<PlayerSkillManager>();

            if (playerController == null)
            {
                Debug.LogWarning("?? PlayerController not found on Player!");
            }
            if (manager == null)
            {
                Debug.LogWarning("?? PlayerSkillManager not found on Player!");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("?? Player ออกจากระยะ Pickup");
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

    // ===== Helper Methods =====
    public bool IsPlayerInRange()
    {
        return playerInRange;
    }

    public string GetSkillID()
    {
        return skillID;
    }
}