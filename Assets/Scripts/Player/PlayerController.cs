using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Jump / Gravity")]
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundMask;

    // ===== Use Zone (รองรับหลายโซน) =====
    [Header("Use Zone")]
    public UseZone currentUseZone;                 // โซนที่ใช้งานอยู่
    private readonly HashSet<UseZone> zonesIn = new HashSet<UseZone>(); // โซนทั้งหมดที่กำลังยืนอยู่

    private CharacterController controller;
    private Animator animator;
    private Vector3 velocity;
    private bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        // ✅ สแกนว่าตอน spawn ยืนคร่อมโซนใดอยู่หรือไม่
        ScanZonesAtStart();
    }

    private void ScanZonesAtStart()
    {
        zonesIn.Clear();

        var allZones = FindObjectsOfType<UseZone>(true);
        Vector3 p = transform.position;
        foreach (var z in allZones)
        {
            var col = z.GetComponent<Collider>();
            if (col == null) continue;

            // ถ้า player อยู่ใน bounds ของโซนตั้งแต่เริ่ม → เพิ่มเข้าชุด
            if (col.bounds.Contains(p))
                zonesIn.Add(z);
        }

        RecomputeCurrentZone(); // จะเป็นคนเปิด/ปิด canUseItems และ UI ให้เอง
    }

    void Update()
    {
        // Ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
            animator.SetBool("isJumping", false);
        }

        // Horizontal movement (แกน X)
        float x = Input.GetAxis("Horizontal");
        Vector3 move = new Vector3(x, 0f, 0f);
        controller.Move(move * moveSpeed * Time.deltaTime);
        animator.SetFloat("Speed", Mathf.Abs(x));

        // หันซ้าย/ขวา
        if (x > 0.05f) transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        else if (x < -0.05f) transform.rotation = Quaternion.Euler(0f, -90f, 0f);

        // Jump
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator.SetBool("isJumping", true);
        }

        // Gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // หมายเหตุ: การ "ใช้ไอเท็ม" เรียกจาก InventorySlot → player.TryUseSelectedInZone()
    }

    // ========== เลือกโซนที่ดีที่สุด เมื่อมีหลายโซน ==========
    private void RecomputeCurrentZone()
    {
        UseZone best = null;
        float bestDist = float.MaxValue;
        int bestPriority = int.MinValue;

        Vector3 p = transform.position;

        foreach (var z in zonesIn)
        {
            if (z == null) continue;
            var col = z.GetComponent<Collider>();
            if (col == null) continue;

            // เลือกตาม priority ก่อน
            if (z.priority > bestPriority)
            {
                best = z;
                bestPriority = z.priority;
                bestDist = Vector3.SqrMagnitude(col.bounds.ClosestPoint(p) - p);
                continue;
            }
            if (z.priority < bestPriority) continue;

            // priority เท่ากัน → เลือกอันที่ใกล้กว่า
            float d = Vector3.SqrMagnitude(col.bounds.ClosestPoint(p) - p);
            if (d < bestDist)
            {
                best = z;
                bestDist = d;
            }
        }

        currentUseZone = best;

        bool inAnyZone = currentUseZone != null;
    }

    // ========== ใช้ไอเท็มกับโซนปัจจุบัน ==========
    public void TryUseSelectedInZone()
    {
        if (currentUseZone == null)
        {
            Debug.Log("ยังไม่ได้ยืนอยู่ในโซนใช้งาน");
            return;
        }


        
    }

    // ========== Trigger: เข้า/ออกหลายโซนได้พร้อมกัน ==========
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out UseZone zone))
        {
            zonesIn.Add(zone);
            RecomputeCurrentZone();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out UseZone zone))
        {
            zonesIn.Remove(zone);
            RecomputeCurrentZone();
        }
    }


}
