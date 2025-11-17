using UnityEngine;
using System.Collections;

public class ObjectMoveToFillHole : MonoBehaviour
{
    public GameObject objectA;
    public Transform targetHole;
    public float moveSpeed = 2f;
    public float arriveDistance = 0.05f;

    public ParticleSystem movingEffect;
    public Vector3 effectOffset = Vector3.zero;

    public ParticleSystem fillEffect;

    public AudioClip movingSound;
    [Range(0f, 1f)] public float movingSoundVolume = 0.6f;

    public AudioClip impactSound;
    [Range(0f, 1f)] public float impactSoundVolume = 0.8f;

    public Collider pathColliderToEnable;

    private bool isMoving = false;
    private ParticleSystem activeMovingEffect;

    // สำหรับเก็บ id ของเสียง loop ที่ AudioManager เล่นอยู่
    private int movingSoundID = -1;

    void Start()
    {
        if (movingEffect != null) movingEffect.Stop();
        if (fillEffect != null) fillEffect.Stop();
    }

    public void StartMoveToHole()
    {
        if (objectA == null || targetHole == null)
        {
            Debug.LogWarning("[ObjectMoveToFillHole] ยังไม่ได้กำหนด objectA หรือ targetHole");
            return;
        }

        if (isMoving) return;

        StartCoroutine(MoveObject());
    }

    private IEnumerator MoveObject()
    {
        isMoving = true;

        StartMovingEffect();
        StartMovingSound();

        while (isMoving && objectA != null)
        {
            objectA.transform.position = Vector3.MoveTowards(
                objectA.transform.position,
                targetHole.position,
                moveSpeed * Time.deltaTime
            );

            if (activeMovingEffect != null)
            {
                activeMovingEffect.transform.position = objectA.transform.position + effectOffset;
            }

            if (Vector3.Distance(objectA.transform.position, targetHole.position) <= arriveDistance)
            {
                OnArriveHole();
                yield break;
            }

            yield return null;
        }
    }

    private void StartMovingEffect()
    {
        if (movingEffect == null) return;

        Vector3 spawnPos = objectA.transform.position + effectOffset;
        activeMovingEffect = Instantiate(movingEffect, spawnPos, Quaternion.identity);
        activeMovingEffect.Play();
    }

    private void StopMovingEffect()
    {
        if (activeMovingEffect != null)
        {
            activeMovingEffect.Stop();
            Destroy(activeMovingEffect.gameObject, 2f);
            activeMovingEffect = null;
        }
    }

    private void StartMovingSound()
    {
        if (movingSound != null)
        {
            // ถ้า AudioManager คุณมีระบบ Loop
            movingSoundID = AudioManager.Instance.PlaySFXLoop(movingSound, movingSoundVolume);
        }
    }

    private void StopMovingSound()
    {
        if (movingSoundID != -1)
        {
            AudioManager.Instance.StopSFXLoop(movingSoundID);
            movingSoundID = -1;
        }
    }

    private void OnArriveHole()
    {
        isMoving = false;

        StopMovingEffect();
        StopMovingSound();

        objectA.transform.position = targetHole.position;
        objectA.transform.rotation = targetHole.rotation;

        if (fillEffect != null)
        {
            var fx = Instantiate(fillEffect, targetHole.position, Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject, 3f);
        }

        if (impactSound != null)
        {
            AudioManager.Instance.PlaySFX(impactSound, impactSoundVolume);
        }

        if (pathColliderToEnable != null)
            pathColliderToEnable.enabled = true;
    }

    void OnDestroy()
    {
        StopMovingEffect();
        StopMovingSound();
    }
}
