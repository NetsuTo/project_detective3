using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer")]
    public AudioMixer audioMixer;
    public AudioMixerGroup sfxMixerGroup;

    [Header("Audio Sources")]
    public AudioSource musicSource;

    [Header("SFX Pool Settings")]
    [SerializeField] private int poolSize = 10;
    private List<AudioSource> sfxPool = new List<AudioSource>();
    private int currentPoolIndex = 0;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";

    // เพิ่มส่วนสำหรับจัดการ Loop SFX
    private Dictionary<int, AudioSource> loopingSFX = new Dictionary<int, AudioSource>();
    private int nextLoopID = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadVolumeSettings();
            InitializeSFXPool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (musicSource != null)
            musicSource.outputAudioMixerGroup = audioMixer.FindMatchingGroups("Music")[0];

        SetMasterVolume(masterVolume);
        SetMusicVolume(musicVolume);
        SetSFXVolume(sfxVolume);
    }

    // ========== SFX Pool ==========
    private void InitializeSFXPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject sfxObject = new GameObject($"SFX_AudioSource_{i}");
            sfxObject.transform.SetParent(transform);

            AudioSource audioSource = sfxObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.outputAudioMixerGroup = sfxMixerGroup;

            sfxPool.Add(audioSource);
        }
    }

    private AudioSource GetAvailableSFXSource()
    {
        // หา AudioSource ที่ว่าง
        for (int i = 0; i < sfxPool.Count; i++)
        {
            int index = (currentPoolIndex + i) % sfxPool.Count;
            if (!sfxPool[index].isPlaying)
            {
                currentPoolIndex = (index + 1) % sfxPool.Count;
                return sfxPool[index];
            }
        }

        // ถ้าไม่มีที่ว่าง ใช้ตัวถัดไปแทน (จะตัดเสียงเก่า)
        AudioSource source = sfxPool[currentPoolIndex];
        currentPoolIndex = (currentPoolIndex + 1) % sfxPool.Count;
        return source;
    }

    // ========== Play SFX Functions ==========

    // เล่นเสียงแบบง่าย (2D)
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;

        AudioSource source = GetAvailableSFXSource();
        source.spatialBlend = 0f; // 2D sound
        source.clip = clip;
        source.Play();
    }

    // เล่นเสียงพร้อมปรับ volume
    public void PlaySFX(AudioClip clip, float volumeScale)
    {
        if (clip == null) return;

        AudioSource source = GetAvailableSFXSource();
        source.spatialBlend = 0f;
        source.PlayOneShot(clip, volumeScale);
    }

    // เล่นเสียงแบบ 3D ที่ตำแหน่งที่กำหนด
    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volumeScale = 1f)
    {
        if (clip == null) return;

        AudioSource source = GetAvailableSFXSource();
        source.transform.position = position;
        source.spatialBlend = 1f; // 3D sound
        source.PlayOneShot(clip, volumeScale);
    }

    // เล่นเสียงแบบ 3D พร้อมการตั้งค่าเพิ่มเติม
    public void PlaySFX3D(AudioClip clip, Vector3 position, float minDistance = 1f, float maxDistance = 500f, float volumeScale = 1f)
    {
        if (clip == null) return;

        AudioSource source = GetAvailableSFXSource();
        source.transform.position = position;
        source.spatialBlend = 1f;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.PlayOneShot(clip, volumeScale);
    }

    // เล่นเสียงแบบสุ่ม pitch (ทำให้เสียงไม่ซ้ำซาก)
    public void PlaySFXRandomPitch(AudioClip clip, float minPitch = 0.9f, float maxPitch = 1.1f)
    {
        if (clip == null) return;

        AudioSource source = GetAvailableSFXSource();
        source.spatialBlend = 0f;
        source.pitch = Random.Range(minPitch, maxPitch);
        source.clip = clip;
        source.Play();

        // คืน pitch กลับเป็นปกติหลังเล่นเสร็จ
        StartCoroutine(ResetPitchAfterPlay(source, clip.length));
    }

    private System.Collections.IEnumerator ResetPitchAfterPlay(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        source.pitch = 1f;
    }

    // ========== SFX Loop Functions (ใหม่) ==========

    /// <summary>
    /// เล่นเสียง SFX แบบ Loop และคืนค่า ID สำหรับหยุดเสียงภายหลัง
    /// </summary>
    public int PlaySFXLoop(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return -1;

        AudioSource source = GetAvailableSFXSource();
        source.spatialBlend = 0f; // 2D sound
        source.clip = clip;
        source.volume = volumeScale;
        source.loop = true;
        source.Play();

        int id = nextLoopID++;
        loopingSFX[id] = source;

        return id;
    }

    /// <summary>
    /// หยุดเสียง Loop ตาม ID ที่ได้จาก PlaySFXLoop
    /// </summary>
    public void StopSFXLoop(int id)
    {
        if (loopingSFX.ContainsKey(id))
        {
            AudioSource source = loopingSFX[id];
            if (source != null)
            {
                source.loop = false;
                source.Stop();
                source.volume = 1f; // คืนค่า volume เป็นปกติ
            }
            loopingSFX.Remove(id);
        }
    }

    /// <summary>
    /// เล่นเสียง Loop แบบ 3D ที่ตำแหน่งที่กำหนด
    /// </summary>
    public int PlaySFXLoop3D(AudioClip clip, Vector3 position, float volumeScale = 1f, float minDistance = 1f, float maxDistance = 500f)
    {
        if (clip == null) return -1;

        AudioSource source = GetAvailableSFXSource();
        source.transform.position = position;
        source.spatialBlend = 1f; // 3D sound
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.clip = clip;
        source.volume = volumeScale;
        source.loop = true;
        source.Play();

        int id = nextLoopID++;
        loopingSFX[id] = source;

        return id;
    }

    /// <summary>
    /// หยุดเสียง Loop ทั้งหมด
    /// </summary>
    public void StopAllSFXLoops()
    {
        foreach (var kvp in loopingSFX)
        {
            if (kvp.Value != null)
            {
                kvp.Value.loop = false;
                kvp.Value.Stop();
                kvp.Value.volume = 1f;
            }
        }
        loopingSFX.Clear();
    }

    // หยุดเสียง SFX ทั้งหมด (รวม Loop ด้วย)
    public void StopAllSFX()
    {
        foreach (AudioSource source in sfxPool)
        {
            source.Stop();
        }
        StopAllSFXLoops();
    }

    // ========== SFX Volume ==========
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        float dB = VolumeToDecibels(sfxVolume);
        audioMixer.SetFloat("SFXVolume", dB);
        SaveVolume(SFX_VOLUME_KEY, sfxVolume);
    }

    public float GetSFXVolume()
    {
        return sfxVolume;
    }

    // ========== Master Volume ==========
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        float dB = VolumeToDecibels(masterVolume);
        audioMixer.SetFloat("MasterVolume", dB);
        SaveVolume(MASTER_VOLUME_KEY, masterVolume);
    }

    public float GetMasterVolume()
    {
        return masterVolume;
    }

    // ========== Music Volume ==========
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        float dB = VolumeToDecibels(musicVolume);
        audioMixer.SetFloat("MusicVolume", dB);
        SaveVolume(MUSIC_VOLUME_KEY, musicVolume);
    }

    public float GetMusicVolume()
    {
        return musicVolume;
    }

    // ========== Helper Functions ==========
    private float VolumeToDecibels(float volume)
    {
        if (volume <= 0f)
            return -80f;
        return Mathf.Log10(volume) * 20f;
    }

    private void SaveVolume(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
    }

    private void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
        musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 1f);
        sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
    }

    public void ResetToDefault()
    {
        SetMasterVolume(1f);
        SetMusicVolume(1f);
        SetSFXVolume(1f);
    }
}