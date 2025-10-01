using UnityEngine;
using System.Collections.Generic;

public class TargetZone : MonoBehaviour
{
    [Header("MiniGame ของ Zone นี้ (Drag Component ที่เป็นลูกมาใส่)")]
    public ElementMiniGameManager miniGame;   // ใส่จาก Inspector

    [Header("Sequence ที่ต้องการจากขวด (เช่น HHO, O2, HOH)")]
    public List<KeyCode> requiredSequence = new List<KeyCode>();

    private bool playerInside = false;
    private SkillInventory playerInventory;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            playerInventory = other.GetComponent<SkillInventory>();
            Debug.Log("Player entered TargetZone (need: " + SeqToString(requiredSequence) + ")");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            playerInventory = null;
            Debug.Log("Player left TargetZone");
        }
    }

    private void Update()
    {
        if (!playerInside) return;

        if (Input.GetKeyDown(KeyCode.R))
        {
            if (playerInventory == null || playerInventory.IsEmpty())
            {
                Debug.Log("❌ ไม่มีสกิลในขวดให้ใช้");
                return;
            }

            if (playerInventory.HasSkill(requiredSequence))
            {
                Debug.Log("🎯 พบสกิลตรงกับ Zone, เริ่ม MiniGame (ใช้ inspectorSequence)");

                // ลบขวดออกไปก่อนเริ่มมินิเกม
                playerInventory.ConsumeSkill(requiredSequence);

                // ✅ เรียก MiniGame โดยไม่ส่ง sequence → จะใช้ inspectorSequence ของ Zone
                miniGame.StartMiniGame(null, (success) =>
                {
                    if (success)
                    {
                        Debug.Log("🎉 Success MiniGame in Zone: " + SeqToString(requiredSequence));
                        // trigger event ของ Zone ได้ที่นี่
                    }
                    else
                    {
                        Debug.Log("💥 Fail MiniGame in Zone: " + SeqToString(requiredSequence));
                        miniGame.ShowFailSymbolSafe();
                    }
                });
            }
            else
            {
                Debug.Log("❌ ไม่มี skill ที่ถูกต้องสำหรับ Zone นี้");
                miniGame.ShowFailSymbolSafe();
                playerInventory.ConsumeFirstSkill(); // ขวดที่กดผิดก็หาย
            }
        }
    }

    private string SeqToString(List<KeyCode> seq)
    {
        if (seq == null) return "";
        return string.Join("", seq);
    }
}
