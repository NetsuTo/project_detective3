using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    [Header("UI ที่จะควบคุม")]
    [Tooltip("ลาก GameObject ที่เป็น UI (เช่น Panel, Image, Text) มาใส่ในช่องนี้")]
    [SerializeField]
    private GameObject uiObjectToShow; // ตัวแปรสำหรับเก็บ UI ที่เราจะเปิด/ปิด

    [Header("ตั้งค่าการตรวจจับ")]
    [SerializeField]
    private string playerTag = "Player"; // Tag ของผู้เล่น

    // ฟังก์ชันนี้จะทำงานครั้งเดียวเมื่อเริ่มเกม
    private void Start()
    {
        // ตรวจสอบให้แน่ใจว่า UI ของเราปิดอยู่ตอนเริ่มเกม
        if (uiObjectToShow != null)
        {
            uiObjectToShow.SetActive(false);
        }
        else
        {
            Debug.LogWarning("ยังไม่ได้กำหนด UI Object ให้กับ Trigger นี้!", this.gameObject);
        }
    }

    // ทำงานเมื่อมี Collider อื่น 'เข้ามา' ใน Trigger
    private void OnTriggerEnter(Collider other)
    {
        // ตรวจสอบว่าเป็นผู้เล่น และเราได้ตั้งค่า UI ไว้แล้ว
        if (other.CompareTag(playerTag) && uiObjectToShow != null)
        {
            // เปิดการแสดงผล UI
            uiObjectToShow.SetActive(true);
        }
    }

    // ทำงานเมื่อ Collider อื่น 'ออกไป' จาก Trigger
    private void OnTriggerExit(Collider other)
    {
        // ตรวจสอบว่าเป็นผู้เล่น และเราได้ตั้งค่า UI ไว้แล้ว
        if (other.CompareTag(playerTag) && uiObjectToShow != null)
        {
            // ปิดการแสดงผล UI
            uiObjectToShow.SetActive(false);
        }
    }
}