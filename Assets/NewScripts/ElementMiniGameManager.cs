// ElementMiniGameManager.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ElementMiniGameManager : MonoBehaviour
{
    [Header("UI ของ MiniGame ใน Zone นี้")]
    public Text displayText;                 // แสดง key ปัจจุบัน (UI Text)
    public GameObject failSymbol;            // ไอคอน/UI แสดง Fail
    public float failSymbolDuration = 5f;    // เวลาที่ Fail symbol โผล่แล้วหาย

    [Header("Optional: ถ้า StartMiniGame ถูกเรียกด้วย null, จะ fallback มาใช้ inspectorSequence")]
    public List<KeyCode> inspectorSequence = new List<KeyCode>();

    [Header("Events (ตั้งใน Inspector)")]
    public UnityEvent onSuccessEvent;        // เรียกตอนมินิเกมสำเร็จ
    public UnityEvent onFailEvent;           // เรียกตอนมินิเกมล้มเหลว (นอกเหนือจาก FailSymbol)

    // internal
    private List<KeyCode> currentSequence = new List<KeyCode>();
    private int currentIndex = 0;
    private bool isActive = false;
    private Action<bool> onCompleteCallback = null;

    void Start()
    {
        if (displayText != null) displayText.gameObject.SetActive(false);
        if (failSymbol != null) failSymbol.SetActive(false);
    }

    void Update()
    {
        if (!isActive) return;
        if (currentSequence == null || currentSequence.Count == 0) return;

        // ตรวจ input: ถ้ามี key กด ให้เช็คว่าตรงกับ key ปัจจุบันหรือไม่
        if (Input.anyKeyDown)
        {
            // ถ้ key ถูกต้อง
            if (Input.GetKeyDown(currentSequence[currentIndex]))
            {
                currentIndex++;
                UpdateDisplay();

                if (currentIndex >= currentSequence.Count)
                    Success();
            }
            else
            {
                // key ผิด
                Fail();
            }
        }
    }

    /// <summary>
    /// เริ่มมินิเกมโดยใช้ sequence ที่ส่งเข้ามา (ถ้า sequence เป็น null หรือว่าง จะ fallback ไปที่ inspectorSequence ถ้ามี)
    /// callback ถูกเรียกเมื่อมินิเกมจบ (true=success, false=fail)
    /// </summary>
    public void StartMiniGame(List<KeyCode> sequence, Action<bool> callback)
    {
        // ถ้า sequence ที่เรียกว่าง ให้ fallback ไปใช้ inspectorSequence (ถ้ามี)
        if (sequence == null || sequence.Count == 0)
        {
            if (inspectorSequence != null && inspectorSequence.Count > 0)
            {
                currentSequence = new List<KeyCode>(inspectorSequence);
            }
            else
            {
                Debug.LogWarning("[ElementMiniGameManager] StartMiniGame called with empty sequence AND no inspectorSequence to fallback.");
                callback?.Invoke(false);
                return;
            }
        }
        else
        {
            currentSequence = new List<KeyCode>(sequence);
        }

        onCompleteCallback = callback;
        currentIndex = 0;
        isActive = true;

        if (displayText != null)
        {
            displayText.gameObject.SetActive(true);
            UpdateDisplay();
        }

        if (failSymbol != null)
            failSymbol.SetActive(false);

        Debug.Log($"[ElementMiniGameManager:{gameObject.name}] StartMiniGame - seq: {SeqToString(currentSequence)}");
    }

    private void UpdateDisplay()
    {
        if (displayText == null) return;

        if (currentIndex < currentSequence.Count)
            displayText.text = "Next: " + KeyToSymbol(currentSequence[currentIndex]);
        else
            displayText.text = "Done!";
    }

    private void Success()
    {
        isActive = false;
        if (displayText != null) displayText.gameObject.SetActive(false);

        Debug.Log("[ElementMiniGameManager] MiniGame Success in " + gameObject.name);

        // เรียก UnityEvent ที่ตั้งไว้ใน Inspector
        try { onSuccessEvent?.Invoke(); } catch (Exception ex) { Debug.LogWarning("onSuccessEvent invoke failed: " + ex); }

        // callback ให้ caller (เช่น TargetZone / SkillInventory)
        onCompleteCallback?.Invoke(true);

        // reset callback (ป้องกันการเรียกซ้ำ)
        onCompleteCallback = null;
    }

    private void Fail()
    {
        isActive = false;
        if (displayText != null) displayText.gameObject.SetActive(false);

        Debug.Log("[ElementMiniGameManager] MiniGame Fail in " + gameObject.name);

        // แสดง Fail symbol และ hide อัตโนมัติ
        ShowFailSymbolSafe();

        // เรียก UnityEvent สำหรับ Fail (optional)
        try { onFailEvent?.Invoke(); } catch (Exception ex) { Debug.LogWarning("onFailEvent invoke failed: " + ex); }

        onCompleteCallback?.Invoke(false);
        onCompleteCallback = null;
    }

    /// <summary>
    /// แสดง Fail symbol (ปลอดภัย เรียกซ้ำได้) — จะซ่อนอัตโนมัติหลัง failSymbolDuration
    /// </summary>
    public void ShowFailSymbolSafe()
    {
        if (failSymbol == null) return;
        StopAllCoroutines();
        StartCoroutine(ShowFailSymbolCoroutine());
    }

    private IEnumerator ShowFailSymbolCoroutine()
    {
        failSymbol.SetActive(true);
        yield return new WaitForSeconds(failSymbolDuration);
        failSymbol.SetActive(false);
    }

    // แปลง KeyCode เป็นตัวอักษร/สัญลักษณ์สำหรับแสดง (customize ได้)
    private string KeyToSymbol(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.UpArrow: return "↑";
            case KeyCode.DownArrow: return "↓";
            case KeyCode.LeftArrow: return "←";
            case KeyCode.RightArrow: return "→";
            case KeyCode.Space: return "Space";
            default: return key.ToString(); // e.g. A, H, O ...
        }
    }

    private string SeqToString(List<KeyCode> seq)
    {
        if (seq == null || seq.Count == 0) return "";
        return string.Join("", seq);
    }
}
