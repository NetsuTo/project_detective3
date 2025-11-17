using UnityEngine;
using UnityEngine.Video;

public class VideoVolumeController : MonoBehaviour
{
    [Header("Video Player")]
    public VideoPlayer videoPlayer;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    public float videoVolumeScale = 1f; // ปรับเสียงวิดีโอเทียบกับ Master

    private AudioSource videoAudioSource;

    void Start()
    {
        // หา AudioSource ของ VideoPlayer
        if (videoPlayer != null)
        {
            videoAudioSource = videoPlayer.GetComponent<AudioSource>();

            if (videoAudioSource == null && videoPlayer.audioOutputMode == VideoAudioOutputMode.AudioSource)
            {
                videoAudioSource = videoPlayer.GetTargetAudioSource(0);
            }

            // รอให้ AudioManager พร้อม แล้วค่อยตั้งเสียง
            if (AudioManager.Instance != null)
            {
                UpdateVideoVolume();
            }
            else
            {
                // ถ้า AudioManager ยังไม่พร้อม ลองอีกครั้งหลัง 0.1 วินาที
                Invoke(nameof(UpdateVideoVolume), 0.1f);
            }

            // ตั้งค่า Output ของ AudioSource ไปที่ Master (ถ้ามี Audio Mixer)
            if (videoAudioSource != null && AudioManager.Instance != null && AudioManager.Instance.audioMixer != null)
            {
                var masterGroup = AudioManager.Instance.audioMixer.FindMatchingGroups("Master");
                if (masterGroup.Length > 0)
                {
                    videoAudioSource.outputAudioMixerGroup = masterGroup[0];
                }
            }
        }
    }

    void Update()
    {
        // อัพเดทเสียงวิดีโอตาม Master Volume แบบ real-time
        UpdateVideoVolume();
    }

    void UpdateVideoVolume()
    {
        if (videoAudioSource != null && AudioManager.Instance != null)
        {
            // เสียงวิดีโอ = Master Volume ? Video Volume Scale
            float masterVolume = AudioManager.Instance.GetMasterVolume();
            float finalVolume = masterVolume * videoVolumeScale;

            videoAudioSource.volume = finalVolume;

            // Debug (ลบได้ถ้าไม่ต้องการ)
            // Debug.Log($"Video Volume: Master={masterVolume:F2}, Scale={videoVolumeScale:F2}, Final={finalVolume:F2}");
        }
    }

    public void SetVideoVolumeScale(float scale)
    {
        videoVolumeScale = Mathf.Clamp01(scale);
        UpdateVideoVolume();
    }
}