using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float acceleration = 10f; // ✅ เพิ่มความลื่นในการเปลี่ยนทิศทาง

    [Header("Jump / Gravity")]
    public float jumpForce = 5f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public float fallMultiplier = 2.5f; // ✅ ตกเร็วขึ้นแบบเกม platformer ทั่วไป

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundMask;

    // ===== Use Zone (รองรับหลายโซน) =====
    [Header("Use Zone")]
    public UseZone currentUseZone;                 // โซนที่ใช้งานอยู่
    private readonly HashSet<UseZone> zonesIn = new HashSet<UseZone>(); // โซนทั้งหมดที่กำลังยืนอยู่

    // ====== Success Symbol Management ======
    [Header("Success Symbol")]
    public GameObject successSymbol; // ลาก GameObject ไอคอนเหนือหัวมาวางใน Inspector

    private CharacterController controller;
    private Animator animator;
    private Vector3 velocity;
    private float currentSpeed;
    private bool isGrounded;
    private bool wasGroundedLastFrame;
    private bool isPickingUp = false;
    private Action pickupCallback; // ✅ เก็บ callback ชั่วคราวไว้ใช้ทีหลัง

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
        // --- ตรวจสอบพื้น ---
        wasGroundedLastFrame = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f; // รีเซ็ตแรงโน้มถ่วงเล็กน้อยเมื่อแตะพื้น

        // --- การเคลื่อนที่แนวนอน (ใช้แค่ปุ่ม A / D) ---
        float x = 0f;

        if (Input.GetKey(KeyCode.A))
            x = -1f;
        else if (Input.GetKey(KeyCode.D))
            x = 1f;

        float targetSpeed = x * moveSpeed;
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * acceleration);

        Vector3 move = new Vector3(currentSpeed, 0f, 0f);
        controller.Move(move * Time.deltaTime);

        animator.SetFloat("Speed", Mathf.Abs(currentSpeed));

        // --- หันตัว ---
        if (x > 0.05f) transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        else if (x < -0.05f) transform.rotation = Quaternion.Euler(0f, -90f, 0f);

        // --- กระโดด ---
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity) * jumpForce;
            animator.SetTrigger("Jump");
        }

        // --- แรงโน้มถ่วง + ตกไวขึ้น ---
        if (velocity.y < 0)
            velocity.y += gravity * fallMultiplier * Time.deltaTime;
        else
            velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);

        // --- ลงพื้นนุ่มนวล ---
        if (isGrounded && !wasGroundedLastFrame)
        {
            animator.SetTrigger("Land");
        }
    }

    public void PlayPickupAnimation(Action onPickupComplete)
    {
        if (isPickingUp) return;
        isPickingUp = true;

        pickupCallback = onPickupComplete;
        animator.SetTrigger("Pickup");

        // รอเวลาเท่าความยาว animation
        Invoke(nameof(CompletePickup), 1f);
    }

    private void CompletePickup()
    {
        isPickingUp = false;
        pickupCallback?.Invoke();
        pickupCallback = null;
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

    public void PlayCastingAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("IsCasting", true);
            animator.SetTrigger("Cast"); // Trigger สั้นเพื่อเข้า animation
        }
    }

    public void StopCastingAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("IsCasting", false);
        }
    }

    public void ShowSuccessSymbol()
    {
        if (successSymbol != null)
        {
            successSymbol.SetActive(true);
            Debug.Log("✨ แสดง Success Symbol บนหัวผู้เล่น");
        }
        else
        {
            Debug.LogWarning("⚠️ successSymbol ยังไม่ได้ assign ใน PlayerController");
        }
    }

    public void HideSuccessSymbol()
    {
        if (successSymbol != null && successSymbol.activeSelf)
        {
            successSymbol.SetActive(false);
            Debug.Log("💨 ซ่อน Success Symbol บนหัวผู้เล่น");
        }
    }

}
