using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneChangerEndGame : MonoBehaviour
{
    [Header("การตั้งค่า Scene")]
    [Tooltip("ชื่อ Scene ที่จะเปลี่ยนไป (ต้องเพิ่มใน Build Settings)")]
    public string endGameSceneName = "EndGame";

    [Header("การตรวจจับผู้เล่น")]
    public float detectionRange = 2f; // ระยะตรวจจับ Player
    public GameObject pressEIndicator; // UI บอกให้กด E (ถ้ามี)

    [Header("เสียง")]
    public AudioClip changeSceneSound; // เสียงตอนเปลี่ยน Scene
    [Range(0f, 1f)] public float soundVolume = 0.8f;

    [Header("เอฟเฟค (ถ้ามี)")]
    public ParticleSystem transitionEffect; // เอฟเฟคตอนเปลี่ยน Scene

    [Header("หน่วงเวลา")]
    public float delayBeforeChange = 1f; // รอกี่วินาทีก่อนเปลี่ยน Scene

    [Header("?? การเชื่อมต่อ")]
    [Tooltip("ลาก TimerController มาใส่ (หรือปล่อยว่างให้หาอัตโนมัติ)")]
    public TimerController timerController;

    [Tooltip("ลาก ExplosionController มาใส่ (หรือปล่อยว่างให้หาอัตโนมัติ)")]
    public ExplosionController explosionController;

    private bool playerInRange = false;
    private bool isChanging = false;
    private AudioSource audioSource;

    void Start()
    {
        // สร้าง AudioSource สำรอง
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        if (pressEIndicator != null)
            pressEIndicator.SetActive(false);

        // ตรวจสอบว่ามี Collider หรือไม่
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning("[SceneChangerEndGame] ไม่พบ Collider! กำลังสร้าง SphereCollider...");
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = detectionRange;
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning("[SceneChangerEndGame] Collider ต้องเปิด Is Trigger!");
            col.isTrigger = true;
        }

        // หา TimerController ถ้ายังไม่ได้ลากมาใส่
        if (timerController == null)
        {
            timerController = FindObjectOfType<TimerController>();
            if (timerController != null)
            {
                Debug.Log("[SceneChangerEndGame] เจอ TimerController แล้ว!");
            }
        }

        // หา ExplosionController ถ้ายังไม่ได้ลากมาใส่
        if (explosionController == null)
        {
            explosionController = FindObjectOfType<ExplosionController>();
            if (explosionController != null)
            {
                Debug.Log("[SceneChangerEndGame] เจอ ExplosionController แล้ว!");
            }
        }

        // ตรวจสอบว่า Scene อยู่ใน Build Settings หรือไม่
        if (SceneUtility.GetBuildIndexByScenePath(endGameSceneName) == -1)
        {
            Debug.LogError($"? Scene '{endGameSceneName}' ไม่อยู่ใน Build Settings! กรุณาเพิ่มใน File > Build Settings");
        }

        Debug.Log($"? SceneChangerEndGame พร้อมใช้งาน - จะเปลี่ยนไป '{endGameSceneName}'");
    }

    void Update()
    {
        // กด E เพื่อเปลี่ยน Scene
        if (playerInRange && Input.GetKeyDown(KeyCode.E) && !isChanging)
        {
            StartCoroutine(ChangeToEndGame());
        }
    }

    IEnumerator ChangeToEndGame()
    {
        isChanging = true;

        Debug.Log($"?? กำลังเปลี่ยนไป Scene '{endGameSceneName}'...");

        // ? หยุด Timer ทันทีที่ผู้เล่นจบ!
        if (timerController != null)
        {
            timerController.StopTimer();
            Debug.Log("? หยุด Timer - ผู้เล่นจบเกมแล้ว!");
        }

        // ? หยุดเอฟเฟกต์การระเบิดทั้งหมด
        if (explosionController != null)
        {
            explosionController.StopContinuousShake();
            explosionController.StopContinuousDebris();
            explosionController.StopCaveCollapseSound();
            Debug.Log("?? หยุดเอฟเฟกต์ระเบิดทั้งหมด");
        }

        // ซ่อน Press E
        if (pressEIndicator != null)
            pressEIndicator.SetActive(false);

        // เล่นเสียง
        if (changeSceneSound != null)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(changeSceneSound, soundVolume);
            else if (audioSource != null)
                audioSource.PlayOneShot(changeSceneSound, soundVolume);

            Debug.Log("?? เล่นเสียงเปลี่ยน Scene");
        }

        // เล่นเอฟเฟค
        if (transitionEffect != null)
        {
            ParticleSystem fx = Instantiate(transitionEffect, transform.position, Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject, 3f);
            Debug.Log("? เล่นเอฟเฟคเปลี่ยน Scene");
        }

        // รอตามที่ตั้งไว้
        yield return new WaitForSeconds(delayBeforeChange);

        // เปลี่ยน Scene
        Debug.Log($"?? เปลี่ยนไป Scene '{endGameSceneName}' เรียบร้อย!");
        SceneManager.LoadScene(endGameSceneName);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;

            if (pressEIndicator != null && !isChanging)
                pressEIndicator.SetActive(true);

            Debug.Log("?? ผู้เล่นเข้าใกล้จุดจบ - กด E เพื่อจบเกม");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;

            if (pressEIndicator != null)
                pressEIndicator.SetActive(false);

            Debug.Log("?? ผู้เล่นออกจากจุดจบ");
        }
    }

    // แสดงระยะตรวจจับใน Scene View
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}