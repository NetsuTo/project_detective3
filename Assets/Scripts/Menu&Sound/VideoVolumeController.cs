using UnityEngine;
using UnityEngine.Video;

public class VideoVolumeController : MonoBehaviour
{
    [Header("Video Player")]
    public VideoPlayer videoPlayer;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    public float videoVolumeScale = 1f; // ปรับเสียงวิดีโอเทียบกับ Music

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

            // ตั้งค่า Output ของ AudioSource ไปที่ Music Group (ถ้ามี Audio Mixer)
            if (videoAudioSource != null && AudioManager.Instance != null && AudioManager.Instance.audioMixer != null)
            {
                var musicGroup = AudioManager.Instance.audioMixer.FindMatchingGroups("Music");
                if (musicGroup.Length > 0)
                {
                    videoAudioSource.outputAudioMixerGroup = musicGroup[0];
                }
            }
        }
    }

    void Update()
    {
        // อัพเดทเสียงวิดีโอตาม Music Volume แบบ real-time
        UpdateVideoVolume();
    }

    void UpdateVideoVolume()
    {
        if (videoAudioSource != null && AudioManager.Instance != null)
        {
            // เสียงวิดีโอ = Music Volume ? Video Volume Scale
            float musicVolume = AudioManager.Instance.GetMusicVolume();
            float finalVolume = musicVolume * videoVolumeScale;
            videoAudioSource.volume = finalVolume;

            // Debug (ลบได้ถ้าไม่ต้องการ)
            // Debug.Log($"Video Volume: Music={musicVolume:F2}, Scale={videoVolumeScale:F2}, Final={finalVolume:F2}");
        }
    }

    public void SetVideoVolumeScale(float scale)
    {
        videoVolumeScale = Mathf.Clamp01(scale);
        UpdateVideoVolume();
    }
}