using UnityEngine;

public class SkillPickup : MonoBehaviour
{
    public string skillID; // เช่น "HHO", "NNOO" เป็นต้น

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerSkillManager manager = other.GetComponent<PlayerSkillManager>();
            if (manager != null)
            {
                if (manager.CanPickupSkill(skillID))
                {
                    manager.PickupSkill(skillID);
                    Destroy(gameObject); // ลบ skill จากพื้น
                }
            }
        }
    }
}
