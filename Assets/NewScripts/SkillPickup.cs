using UnityEngine;

public class SkillPickup : MonoBehaviour
{
    public string skillID; // �� "HHO", "NNOO" �繵�

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
                    Destroy(gameObject); // ź skill �ҡ���
                }
            }
        }
    }
}
