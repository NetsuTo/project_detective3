using UnityEngine;

public class ObjectAppearWithEffect : MonoBehaviour
{
    [Header("Object ที่จะปรากฏ")]
    public GameObject objectA;

    [Header("Particle Effect ตอนปรากฏ (ไม่จำเป็นต้องใส่)")]
    public ParticleSystem appearEffect;

    [Header("เสียงตอนปรากฏ (ไม่จำเป็นต้องใส่)")]
    public AudioClip appearSound;
    [Range(0f, 1f)] public float soundVolume = 0.8f;

    [Header("Delay ก่อนปรากฏ (วินาที)")]
    public float delayBeforeAppear = 0f;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // ถ้า ObjectA ยังเปิดอยู่ให้ปิดไว้ก่อน
        if (objectA != null)
            objectA.SetActive(false);
    }

    /// <summary>
    /// เรียกใน onSuccessEvent ของมินิเกม เพื่อให้ Object ปรากฏ
    /// </summary>
    public void AppearObject()
    {
        if (objectA == null)
        {
            Debug.LogWarning("[ObjectAppearWithEffect] ยังไม่ได้กำหนด ObjectA");
            return;
        }

        Debug.Log("[ObjectAppearWithEffect] เริ่ม Event: ObjectA จะปรากฏ...");
        StartCoroutine(DoAppear());
    }

    private System.Collections.IEnumerator DoAppear()
    {
        if (delayBeforeAppear > 0)
            yield return new WaitForSeconds(delayBeforeAppear);

        // เปิด Object
        objectA.SetActive(true);

        // เล่น Effect
        if (appearEffect != null)
        {
            ParticleSystem fx = Instantiate(appearEffect, objectA.transform.position, Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject, 3f);
        }

        // เล่นเสียง
        if (appearSound != null && audioSource != null)
            audioSource.PlayOneShot(appearSound, soundVolume);

        Debug.Log("[ObjectAppearWithEffect] ObjectA ปรากฏพร้อมเอฟเฟกต์!");
    }
}
