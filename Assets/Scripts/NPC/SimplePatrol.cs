using UnityEngine;

public class SimplePatrol : MonoBehaviour
{
    [Header("จุดเดินตรวจ")]
    public Transform[] patrolPoints;
    public float waitTimeAtPoint = 2f;

    [Header("การเดิน")]
    public float walkSpeed = 2f;
    public float rotationSpeed = 10f; // เพิ่มความเร็วหมุน
    public float stoppingDistance = 0.5f; // เพิ่มระยะหยุด

    [Header("อนิเมชั่น")]
    public Animator animator;
    public bool useSpeedParameter = true;
    public string speedParameterName = "Speed";

    [Header("?? Debug")]
    public bool showDebugLogs = true;
    public bool showGizmos = true;

    private int currentPointIndex = 0;
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private bool isPaused = false;
    private bool isRotating = false; // เพิ่ม: สถานะการหมุน

    void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            LogError("? ไม่มีจุดเดินตรวจ!");
            enabled = false;
            return;
        }

        // เริ่มต้นหันหน้าไปจุดแรก
        if (patrolPoints[0] != null)
        {
            Vector3 directionToFirst = (patrolPoints[0].position - transform.position).normalized;
            if (directionToFirst != Vector3.zero)
            {
                directionToFirst.y = 0;
                transform.rotation = Quaternion.LookRotation(directionToFirst);
            }
        }

        Log($"? พร้อมใช้งาน - มี {patrolPoints.Length} จุด");
    }

    void Update()
    {
        if (isPaused || patrolPoints.Length == 0)
        {
            UpdateAnimation(0f);
            return;
        }

        // กำลังรออยู่
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            UpdateAnimation(0f);

            if (waitTimer <= 0f)
            {
                isWaiting = false;
                isRotating = true; // เริ่มหมุนหาจุดใหม่
                currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
                Log($"?? เตรียมเดินไปจุดที่ {currentPointIndex + 1}");
            }
            return;
        }

        // กำลังหมุนหาทิศทาง
        if (isRotating)
        {
            RotateTowardsTarget();
        }
        else
        {
            // เดินไปยังจุดปัจจุบัน
            MoveTowardsTarget();
        }
    }

    void RotateTowardsTarget()
    {
        if (patrolPoints[currentPointIndex] == null) return;

        Vector3 targetPos = patrolPoints[currentPointIndex].position;
        Vector3 direction = (targetPos - transform.position).normalized;
        direction.y = 0; // ไม่หมุนตาม Y

        if (direction == Vector3.zero)
        {
            isRotating = false;
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // ตรวจสอบว่าหมุนเสร็จหรือยัง
        float angle = Quaternion.Angle(transform.rotation, targetRotation);
        if (angle < 5f) // หมุนเสร็จแล้ว
        {
            transform.rotation = targetRotation;
            isRotating = false;
            Log($"? หมุนเสร็จ เริ่มเดินไปจุดที่ {currentPointIndex + 1}");
        }

        UpdateAnimation(0f); // ไม่เดินขณะหมุน
    }

    void MoveTowardsTarget()
    {
        if (patrolPoints[currentPointIndex] == null) return;

        Vector3 targetPos = patrolPoints[currentPointIndex].position;
        Vector3 currentPos = transform.position;

        // คำนวณระยะทางแบบ 3 มิติ
        float distance = Vector3.Distance(currentPos, targetPos);

        // ถ้าถึงจุดหมายแล้ว
        if (distance <= stoppingDistance)
        {
            StartWaiting();
            return;
        }

        // คำนวณทิศทาง
        Vector3 direction = (targetPos - currentPos).normalized;

        // เดินไปหาเป้าหมาย
        transform.position += direction * walkSpeed * Time.deltaTime;

        // หมุนหน้าไปทางที่เดินขณะเดิน (ค่อยๆ หมุน)
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * 0.5f * Time.deltaTime);
        }

        // อัพเดทอนิเมชั่น
        UpdateAnimation(walkSpeed);
    }

    void StartWaiting()
    {
        if (isWaiting) return;

        isWaiting = true;
        waitTimer = waitTimeAtPoint;
        UpdateAnimation(0f);
        Log($"?? ถึงจุดที่ {currentPointIndex + 1} - รออยู่ {waitTimeAtPoint} วินาที");
    }

    void UpdateAnimation(float speed)
    {
        if (animator == null) return;

        if (useSpeedParameter)
        {
            animator.SetFloat(speedParameterName, speed);
        }
    }

    public void PausePatrol()
    {
        isPaused = true;
        UpdateAnimation(0f);
        Log("?? หยุดการเดินตรวจ");
    }

    public void ResumePatrol()
    {
        isPaused = false;
        Log("?? เดินตรวจต่อ");
    }

    void Log(string message)
    {
        if (showDebugLogs) Debug.Log($"[{gameObject.name}] {message}");
    }

    void LogError(string message)
    {
        if (showDebugLogs) Debug.LogError($"[{gameObject.name}] {message}");
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || patrolPoints == null || patrolPoints.Length == 0) return;

        // วาดจุดเดินตรวจ
        Gizmos.color = Color.cyan;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] != null)
            {
                Gizmos.DrawWireSphere(patrolPoints[i].position, 0.3f);

#if UNITY_EDITOR
                UnityEditor.Handles.Label(patrolPoints[i].position + Vector3.up * 0.5f,
                    $"จุดที่ {i + 1}");
#endif
            }
        }

        // วาดเส้นเชื่อมจุด
        Gizmos.color = Color.blue;
        for (int i = 0; i < patrolPoints.Length - 1; i++)
        {
            if (patrolPoints[i] != null && patrolPoints[i + 1] != null)
            {
                Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
            }
        }

        // วาดเส้นจากจุดสุดท้ายกลับจุดแรก
        if (patrolPoints.Length > 1 && patrolPoints[0] != null &&
            patrolPoints[patrolPoints.Length - 1] != null)
        {
            Gizmos.DrawLine(patrolPoints[patrolPoints.Length - 1].position,
                          patrolPoints[0].position);
        }

        // วาดเส้นไปยังจุดหมายปัจจุบัน (เวลา Play)
        if (Application.isPlaying && patrolPoints.Length > 0 &&
            patrolPoints[currentPointIndex] != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, patrolPoints[currentPointIndex].position);
            Gizmos.DrawWireSphere(patrolPoints[currentPointIndex].position, stoppingDistance);

            // วาดลูกศรแสดงทิศทางที่หัน
            Gizmos.color = Color.red;
            Vector3 forward = transform.forward * 1f;
            Gizmos.DrawRay(transform.position + Vector3.up, forward);
        }
    }
}