using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

/// <summary>
/// ติดตามการใช้ธาตุ 3 ตัว: เมื่อใช้ครบทั้งหมดแล้วจะเรียก Event (เช่น ระเบิด)
/// Script นี้ไม่จัดการมินิเกม แค่เช็คสถานะ
/// </summary>
public class ElementTracker : MonoBehaviour
{
    [Header("?? รายชื่อธาตุที่ต้องใช้")]
    [Tooltip("ชื่อธาตุทั้ง 3 ตัว เช่น HHO, NNOO, CO2")]
    public List<string> requiredElements = new List<string> { "HHO", "NNOO", "CO2" };

    [Header("?? Event เมื่อใช้ครบทั้งหมด")]
    [Tooltip("เรียกเมื่อใช้ธาตุครบทั้ง 3 ตัว (เช่น ระเบิด, ทำลายกำแพง)")]
    public UnityEvent onAllElementsUsed;

    [Header("UI Feedback (Optional)")]
    public UnityEngine.UI.Text progressText;
    public GameObject feedbackPanel;

    // ติดตามว่าใช้ธาตุไหนไปแล้ว
    private HashSet<string> usedElements = new HashSet<string>();
    private bool isCompleted = false;

    void Start()
    {
        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        UpdateProgressUI();
    }

    /// <summary>
    /// เรียกฟังก์ชันนี้เมื่อผู้เล่นทำมินิเกมสำเร็จ
    /// ส่งชื่อธาตุเข้ามา เช่น "HHO"
    /// </summary>
    public void OnElementUsedSuccess(string elementName)
    {
        if (isCompleted)
        {
            Debug.Log("?? ใช้ธาตุครบแล้ว ไม่ต้องทำอีก");
            return;
        }

        // เช็คว่าธาตุนี้อยู่ในรายการหรือไม่
        if (!requiredElements.Contains(elementName))
        {
            Debug.Log($"? ธาตุ {elementName} ไม่ได้อยู่ในรายการที่ต้องการ");
            return;
        }

        // เช็คว่าใช้ไปแล้วหรือยัง
        if (usedElements.Contains(elementName))
        {
            Debug.Log($"?? ธาตุ {elementName} ถูกใช้ไปแล้ว");
            return;
        }

        // บันทึกว่าใช้ธาตุนี้แล้ว
        usedElements.Add(elementName);
        Debug.Log($"? บันทึกธาตุ {elementName} ({usedElements.Count}/{requiredElements.Count})");

        UpdateProgressUI();

        // เช็คว่าใช้ครบหรือยัง
        if (usedElements.Count >= requiredElements.Count)
        {
            TriggerFinalEvent();
        }
    }

    /// <summary>
    /// เช็คว่าธาตุนี้ถูกใช้ไปแล้วหรือยัง
    /// </summary>
    public bool IsElementUsed(string elementName)
    {
        return usedElements.Contains(elementName);
    }

    /// <summary>
    /// เช็คว่าใช้ครบทั้งหมดหรือยัง
    /// </summary>
    public bool IsCompleted()
    {
        return isCompleted;
    }

    /// <summary>
    /// ดูจำนวนธาตุที่ใช้ไปแล้ว
    /// </summary>
    public int GetUsedCount()
    {
        return usedElements.Count;
    }

    private void TriggerFinalEvent()
    {
        isCompleted = true;
        Debug.Log("?? ใช้ธาตุครบทั้งหมดแล้ว! เรียก Final Event");

        // เรียก Event (เช่น ระเบิด)
        onAllElementsUsed?.Invoke();

        if (progressText != null)
        {
            progressText.text = "ครบทั้ง 3 ธาตุแล้ว! ??";
            progressText.color = Color.green;
        }
    }

    private void UpdateProgressUI()
    {
        if (progressText != null)
        {
            progressText.text = $"ธาตุที่ใช้: {usedElements.Count}/{requiredElements.Count}";
        }

        if (feedbackPanel != null && usedElements.Count > 0)
        {
            feedbackPanel.SetActive(true);
        }
    }

    /// <summary>
    /// รีเซ็ตสถานะทั้งหมด (สำหรับเริ่มเกมใหม่)
    /// </summary>
    public void ResetTracker()
    {
        usedElements.Clear();
        isCompleted = false;
        UpdateProgressUI();
        Debug.Log("?? รีเซ็ต ElementTracker");
    }

    // ฟังก์ชันสำหรับ Debug
    void OnGUI()
    {
        if (Application.isEditor && Input.GetKey(KeyCode.LeftShift))
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 150));
            GUILayout.Label($"Element Tracker Debug:");
            GUILayout.Label($"Used: {usedElements.Count}/{requiredElements.Count}");
            GUILayout.Label($"Completed: {isCompleted}");

            if (GUILayout.Button("Reset Tracker"))
            {
                ResetTracker();
            }

            GUILayout.EndArea();
        }
    }
}