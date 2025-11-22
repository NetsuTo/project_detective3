using UnityEngine;

public class SimplePatrol : MonoBehaviour
{
    [Header("จุดเดินตรวจ")]
    public Transform[] patrolPoints;
    public float waitTimeAtPoint = 2f;

    [Header("การเดิน")]
    public float walkSpeed = 2f;
    public float rotationSpeed = 10f;
    public float stoppingDistance = 0.5f;

    [Header("อนิเมชั่น")]
    public Animator animator;
    public bool useSpeedParameter = true;
    public string speedParameterName = "Speed";

    [Header("เสียงฝีเท้า")]
    public AudioClip[] footstepSounds;
    public float footstepInterval = 0.5f; // ระยะเวลาระหว่างเสียงฝีเท้า
    [Range(0f, 1f)]
    public float footstepVolume = 0.7f;

    [Header("Debug")]
    public bool showDebugLogs = true;
    public bool showGizmos = true;

    private int currentPointIndex = 0;
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private bool isPaused = false;
    private bool isRotating = false;
    private float footstepTimer = 0f;

    // Audio components
    private AudioSource audioSource;

    void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // สร้าง AudioSource สำรับ fallback
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 15f;
        audioSource.volume = footstepVolume;

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            LogError("ไม่มีจุดเดินตรวจ!");
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

        Log($"พร้อมใช้งาน - มี {patrolPoints.Length} จุด");
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
                isRotating = true;
                currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
                Log($"เตรียมเดินไปจุดที่ {currentPointIndex + 1}");
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
        direction.y = 0;

        if (direction == Vector3.zero)
        {
            isRotating = false;
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        float angle = Quaternion.Angle(transform.rotation, targetRotation);
        if (angle < 5f)
        {
            transform.rotation = targetRotation;
            isRotating = false;
            Log($"หมุนเสร็จ เริ่มเดินไปจุดที่ {currentPointIndex + 1}");
        }

        UpdateAnimation(0f);
    }

    void MoveTowardsTarget()
    {
        if (patrolPoints[currentPointIndex] == null) return;

        Vector3 targetPos = patrolPoints[currentPointIndex].position;
        Vector3 currentPos = transform.position;

        float distance = Vector3.Distance(currentPos, targetPos);

        if (distance <= stoppingDistance)
        {
            StartWaiting();
            return;
        }

        Vector3 direction = (targetPos - currentPos).normalized;
        transform.position += direction * walkSpeed * Time.deltaTime;

        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * 0.5f * Time.deltaTime);
        }

        // เล่นเสียงฝีเท้าขณะเดิน
        PlayFootstepSound();

        UpdateAnimation(walkSpeed);
    }

    void PlayFootstepSound()
    {
        if (footstepSounds == null || footstepSounds.Length == 0) return;

        footstepTimer -= Time.deltaTime;
        if (footstepTimer <= 0f)
        {
            AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];

            // เล่นเสียง
            if (clip != null)
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(clip, footstepVolume);
                else if (audioSource != null)
                    audioSource.PlayOneShot(clip, footstepVolume);
            }

            footstepTimer = footstepInterval;
        }
    }

    void StartWaiting()
    {
        if (isWaiting) return;

        isWaiting = true;
        waitTimer = waitTimeAtPoint;
        footstepTimer = 0f; // รีเซ็ตเสียงฝีเท้า
        UpdateAnimation(0f);
        Log($"ถึงจุดที่ {currentPointIndex + 1} - รออยู่ {waitTimeAtPoint} วินาที");
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
        Log("หยุดการเดินตรวจ");
    }

    public void ResumePatrol()
    {
        isPaused = false;
        Log("เดินตรวจต่อ");
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

        Gizmos.color = Color.blue;
        for (int i = 0; i < patrolPoints.Length - 1; i++)
        {
            if (patrolPoints[i] != null && patrolPoints[i + 1] != null)
            {
                Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
            }
        }

        if (patrolPoints.Length > 1 && patrolPoints[0] != null &&
            patrolPoints[patrolPoints.Length - 1] != null)
        {
            Gizmos.DrawLine(patrolPoints[patrolPoints.Length - 1].position,
                          patrolPoints[0].position);
        }

        if (Application.isPlaying && patrolPoints.Length > 0 &&
            patrolPoints[currentPointIndex] != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, patrolPoints[currentPointIndex].position);
            Gizmos.DrawWireSphere(patrolPoints[currentPointIndex].position, stoppingDistance);

            Gizmos.color = Color.red;
            Vector3 forward = transform.forward * 1f;
            Gizmos.DrawRay(transform.position + Vector3.up, forward);
        }
    }
}