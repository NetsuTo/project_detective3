using UnityEngine;

public class SkillPickup : MonoBehaviour
{
    public string skillID; // เช่น "HHO", "NNOO" เป็นต้น
    private bool playerInRange = false;
    private PlayerController playerController;
    private PlayerSkillManager manager;

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("? กด E ขณะอยู่ในระยะ pickup");
            if (playerController != null)
            {
                Debug.Log("?? เรียก animation pickup");
                playerController.PlayPickupAnimation(() =>
                {
                    if (manager != null && manager.CanPickupSkill(skillID))
                    {
                        Debug.Log("?? เก็บสกิลสำเร็จ: " + skillID);
                        manager.PickupSkill(skillID);
                        Destroy(gameObject);
                    }
                });
            }
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
