using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

public class TargetZone : MonoBehaviour
{
    [Header("MiniGame ของ Zone นี้")]
    public ElementMiniGameManager miniGame;

    [Header("ธาตุที่ต้องการ (แต่ละธาตุคือ 1 sequence)")]
    public List<ElementRequirement> requiredElements = new List<ElementRequirement>();

    [Header("🎯 Events เมื่อครบทุกธาตุ")]
    public UnityEvent onAllElementsCompleted;

    private bool playerInside = false;
    private SkillInventory playerInventory;
    private HashSet<int> completedElements = new HashSet<int>();

    [System.Serializable]
    public class ElementRequirement
    {
        public string elementName;
        public List<KeyCode> sequence = new List<KeyCode>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            playerInventory = other.GetComponent<SkillInventory>();
            Debug.Log($"Player entered TargetZone ({completedElements.Count}/{requiredElements.Count} completed)");
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

            int matchedIndex = -1;
            for (int i = 0; i < requiredElements.Count; i++)
            {
                if (completedElements.Contains(i)) continue;

                if (playerInventory.HasSkill(requiredElements[i].sequence))
                {
                    matchedIndex = i;
                    break;
                }
            }

            if (matchedIndex >= 0)
            {
                ElementRequirement matched = requiredElements[matchedIndex];
                Debug.Log($"🎯 พบธาตุที่ {matchedIndex + 1}: {matched.elementName}");

                playerInventory.ConsumeSkill(matched.sequence);

                miniGame.StartMiniGame(null, (success) =>
                {
                    if (success)
                    {
                        Debug.Log($"✅ ผ่านมินิเกมของ {matched.elementName}");
                        completedElements.Add(matchedIndex);

                        if (completedElements.Count >= requiredElements.Count)
                        {
                            Debug.Log("🎉 เสร็จสมบูรณ์! ครบทุกธาตุแล้ว");
                            OnAllElementsCompleted();
                        }
                        else
                        {
                            Debug.Log($"📋 เหลืออีก {requiredElements.Count - completedElements.Count} ธาตุ");
                        }
                    }
                    else
                    {
                        Debug.Log($"💥 ล้มเหลวในมินิเกมของ {matched.elementName}");
                        miniGame.ShowFailSymbolSafe();
                    }
                });
            }
            else
            {
                Debug.Log("❌ ธาตุในขวดไม่ตรงกับที่ต้องการ หรือเสร็จไปแล้ว");
                miniGame.ShowFailSymbolSafe();
                playerInventory.ConsumeFirstSkill();
            }
        }
    }

    private void OnAllElementsCompleted()
    {
        Debug.Log("🏆 Zone Complete! ครบทุกธาตุแล้ว");
        onAllElementsCompleted?.Invoke();
    }

    // 🔑 ฟังก์ชันนับจำนวนสกิลที่ใช้ไปแล้ว (สำหรับเช็คปลดล็อค)
    public int GetCompletedCount()
    {
        return completedElements.Count;
    }

    // เช็คว่าใช้สกิลครบตามจำนวนที่กำหนดหรือยัง
    public bool HasCompletedAtLeast(int count)
    {
        return completedElements.Count >= count;
    }

    public void ResetZone()
    {
        completedElements.Clear();
        Debug.Log("🔄 Reset Zone");
    }

    public bool IsCompleted()
    {
        return completedElements.Count >= requiredElements.Count;
    }

    public List<string> GetRemainingElements()
    {
        List<string> remaining = new List<string>();
        for (int i = 0; i < requiredElements.Count; i++)
        {
            if (!completedElements.Contains(i))
            {
                remaining.Add(requiredElements[i].elementName);
            }
        }
        return remaining;
    }
}