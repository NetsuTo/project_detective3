using UnityEngine;

public class SkillPickup : MonoBehaviour
{
    public string skillID; // เช่น "HHO", "NNOO" เป็นต้น

    [Header("Sound Effects")]
    public AudioClip pickupSound; // เสียงเก็บไอเทม
    [Range(0f, 1f)] public float pickupVolume = 0.8f;

    private bool playerInRange = false;
    private PlayerController playerController;
    private PlayerSkillManager manager;
    private AudioSource audioSource;

    void Start()
    {
        // สร้าง AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("?? กด E ขณะอยู่ในระยะ pickup");
            if (playerController != null)
            {
                Debug.Log("?? เรียก animation pickup");
                playerController.PlayPickupAnimation(() =>
                {
                    if (manager != null && manager.CanPickupSkill(skillID))
                    {
                        Debug.Log("? เก็บสกิลสำเร็จ: " + skillID);

                        // เล่นเสียงเก็บไอเทม
                        PlayPickupSound();

                        manager.PickupSkill(skillID);

                        // ทำลาย object หลังจากเสียงเล่นเสร็จ (ถ้ามีเสียง)
                        if (pickupSound != null)
                        {
                            Destroy(gameObject, pickupSound.length);
                        }
                        else
                        {
                            Destroy(gameObject);
                        }
                    }
                });
            }
        }
    }

    private void PlayPickupSound()
    {
        if (pickupSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(pickupSound, pickupVolume);
            Debug.Log("?? เล่นเสียงเก็บไอเทม");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("?? Player เข้ามาในระยะ pickup");
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
}