using UnityEngine;

public class UITutorial : MonoBehaviour
{
    [SerializeField] private PlayerSkillManager playerSkillManager;
    [SerializeField] public GameObject uiWalk;
    [SerializeField] public GameObject uiPK;
    [SerializeField] public GameObject uiPKScc;
    [SerializeField] public GameObject uiPressR;

    // ใช้ bool เพื่อตรวจสอบสถานะและป้องกันไม่ให้โค้ดทำงานซ้ำซ้อน
    private bool hasPlayerMoved = false;
    private bool hasPickedUpItem = false;

    void Start()
    {
        // เริ่มเกมด้วยการแสดง uiWalk และซ่อน UI อื่นๆ ทั้งหมด
        uiWalk.SetActive(true);
        uiPK.SetActive(false);
        uiPKScc.SetActive(false);
        uiPressR.SetActive(false); // ซ่อน uiPressR ไว้ก่อนตอนเริ่ม
    }

    void Update()
    {
        // --- ขั้นตอนที่ 1: ตรวจสอบการเดิน ---
        // ตรวจสอบว่าผู้เล่นยังไม่เคยเดิน และได้กดปุ่ม A หรือ D
        if (!hasPlayerMoved && (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D)))
        {
            hasPlayerMoved = true; // ตั้งค่าว่าผู้เล่นเคยเดินแล้ว

            // ซ่อน uiWalk และแสดง uiPK
            uiWalk.SetActive(false);
            uiPK.SetActive(true);
        }

        // --- ขั้นตอนที่ 2: ตรวจสอบการเก็บไอเทม ---
        // ตรวจสอบว่าผู้เล่นเคยเดินแล้ว, ยังไม่ได้เก็บไอเทม, และเงื่อนไขการเก็บไอเทมเป็นจริง
        if (hasPlayerMoved && !hasPickedUpItem && playerSkillManager.pickUpSCC)
        {
            hasPickedUpItem = true; // ตั้งค่าว่าผู้เล่นเก็บไอเทมแล้ว

            // ซ่อน uiPK และแสดง uiPKScc
            uiPK.SetActive(false);
            uiPKScc.SetActive(true);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PressR"))
        {
            uiPressR.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("PressR"))
        {
            uiPressR.SetActive(false);
        }
    }
}