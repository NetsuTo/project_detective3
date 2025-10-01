using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    private PlayerSkillManager manager;

    private void Start()
    {
        manager = GetComponent<PlayerSkillManager>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
            manager.SelectPrev();

        if (Input.GetKeyDown(KeyCode.E))
            manager.SelectNext();

        if (Input.GetKeyDown(KeyCode.T))
            manager.ConfirmSkill();
    }
}
