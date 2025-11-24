using UnityEngine;

public class ObjectAudioController : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip soundClip;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    [Header("3D Audio Settings")]
    [SerializeField] private bool use3DSound = true;
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 15f;

    [Header("Audio Source (Fallback)")]
    [SerializeField] private AudioSource audioSource;

    [Header("Advanced Settings")]
    [SerializeField] private bool loopSound = false;
    [SerializeField] private bool randomizePitch = false;
    [Range(0.5f, 1.5f)]
    [SerializeField] private float minPitch = 0.9f;
    [Range(0.5f, 1.5f)]
    [SerializeField] private float maxPitch = 1.1f;
    [SerializeField] private bool playOnStart = false;

    private void Start()
    {
        // สร้าง AudioSource ถ้ายังไม่มี
        if (audioSource == null)
        {
            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // ตั้งค่า AudioSource สำหรับ fallback
        SetupAudioSource();

        if (playOnStart && soundClip != null)
        {
            PlaySound();
        }
    }

    private void SetupAudioSource()
    {
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = use3DSound ? 1f : 0f;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.loop = loopSound;
    }

    /// <summary>
    /// เล่นเสียงผ่าน AudioManager หรือ AudioSource
    /// </summary>
    public void PlaySound()
    {
        if (soundClip == null)
        {
            Debug.LogWarning($"[{gameObject.name}] No sound clip assigned!");
            return;
        }

        // ถ้าต้องการลูป ใช้ AudioSource โดยตรง
        if (loopSound)
        {
            PlaySoundLoop();
            return;
        }

        if (AudioManager.Instance != null)
        {
            PlaySoundThroughManager();
        }
        else
        {
            PlaySoundThroughAudioSource();
        }
    }

    /// <summary>
    /// เล่นเสียงด้วย AudioClip ที่กำหนด
    /// </summary>
    public void PlaySound(AudioClip clip)
    {
        if (clip == null) return;

        if (AudioManager.Instance != null)
        {
            if (use3DSound)
            {
                AudioManager.Instance.PlaySFX3D(clip, transform.position, minDistance, maxDistance, volume);
            }
            else
            {
                AudioManager.Instance.PlaySFX(clip, volume);
            }
        }
        else if (audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    private void PlaySoundThroughManager()
    {
        if (randomizePitch)
        {
            // ใช้ Random Pitch (2D เท่านั้น)
            AudioManager.Instance.PlaySFXRandomPitch(soundClip, minPitch, maxPitch);
        }
        else if (use3DSound)
        {
            // เล่นเสียง 3D
            AudioManager.Instance.PlaySFX3D(soundClip, transform.position, minDistance, maxDistance, volume);
        }
        else
        {
            // เล่นเสียง 2D
            AudioManager.Instance.PlaySFX(soundClip, volume);
        }
    }

    private void PlaySoundThroughAudioSource()
    {
        if (audioSource == null) return;

        if (randomizePitch)
        {
            audioSource.pitch = Random.Range(minPitch, maxPitch);
        }

        audioSource.clip = soundClip;
        audioSource.volume = volume;
        audioSource.loop = loopSound;
        audioSource.Play();

        if (randomizePitch && !loopSound)
        {
            // Reset pitch หลังเล่นเสร็จ (เฉพาะเสียงไม่ลูป)
            Invoke(nameof(ResetPitch), soundClip.length);
        }
    }

    private void PlaySoundLoop()
    {
        if (audioSource == null) return;

        if (randomizePitch)
        {
            audioSource.pitch = Random.Range(minPitch, maxPitch);
        }

        audioSource.clip = soundClip;
        audioSource.volume = volume;
        audioSource.loop = true;
        audioSource.Play();
    }

    private void ResetPitch()
    {
        if (audioSource != null)
        {
            audioSource.pitch = 1f;
        }
    }

    /// <summary>
    /// เล่นเสียงที่ตำแหน่งปัจจุบันของ Object
    /// </summary>
    public void PlaySoundAtCurrentPosition()
    {
        if (soundClip == null) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFXAtPosition(soundClip, transform.position, volume);
        }
        else if (audioSource != null)
        {
            audioSource.PlayOneShot(soundClip, volume);
        }
    }

    /// <summary>
    /// เล่นเสียงแบบ Loop
    /// </summary>
    public int PlayLoopSound()
    {
        if (soundClip == null) return -1;

        if (AudioManager.Instance != null)
        {
            if (use3DSound)
            {
                return AudioManager.Instance.PlaySFXLoop3D(soundClip, transform.position, volume, minDistance, maxDistance);
            }
            else
            {
                return AudioManager.Instance.PlaySFXLoop(soundClip, volume);
            }
        }
        else if (audioSource != null)
        {
            audioSource.clip = soundClip;
            audioSource.volume = volume;
            audioSource.loop = true;
            audioSource.Play();
            return 0;
        }

        return -1;
    }

    /// <summary>
    /// หยุดเสียง Loop
    /// </summary>
    public void StopLoopSound(int loopId)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopSFXLoop(loopId);
        }
        else if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = false;
        }
    }

    /// <summary>
    /// หยุดเสียงทั้งหมด
    /// </summary>
    public void StopSound()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    /// <summary>
    /// อัพเดทตำแหน่งเสียง 3D (ใช้กับ Loop)
    /// </summary>
    public void UpdateSoundPosition(Vector3 newPosition)
    {
        if (audioSource != null)
        {
            audioSource.transform.position = newPosition;
        }
    }

    // ========== Setters ==========

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }

    public void SetMinDistance(float distance)
    {
        minDistance = distance;
        if (audioSource != null)
        {
            audioSource.minDistance = minDistance;
        }
    }

    public void SetMaxDistance(float distance)
    {
        maxDistance = distance;
        if (audioSource != null)
        {
            audioSource.maxDistance = maxDistance;
        }
    }

    public void SetSoundClip(AudioClip clip)
    {
        soundClip = clip;
    }

    public void Set3DSound(bool enable)
    {
        use3DSound = enable;
        if (audioSource != null)
        {
            audioSource.spatialBlend = use3DSound ? 1f : 0f;
        }
    }
}