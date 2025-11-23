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
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("3D Sound Default Settings")]
    public float default3DMinDistance = 1f;
    public float default3DMaxDistance = 15f;

    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";

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
        for (int i = 0; i < sfxPool.Count; i++)
        {
            int index = (currentPoolIndex + i) % sfxPool.Count;
            if (!sfxPool[index].isPlaying)
            {
                currentPoolIndex = (index + 1) % sfxPool.Count;
                return sfxPool[index];
            }
        }

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
        source.spatialBlend = 0f;
        source.clip = clip;
        source.Play();
    }

    // เล่นเสียงพร้อมปรับ volume (2D)
    public void PlaySFX(AudioClip clip, float volumeScale)
    {
        if (clip == null) return;

        AudioSource source = GetAvailableSFXSource();
        source.spatialBlend = 0f;
        source.PlayOneShot(clip, volumeScale);
    }

    // เล่นเสียง 3D จาก Transform ที่กำหนด (เสียงจะติดกับ Transform)
    public void PlaySFXFromTransform(AudioClip clip, Transform sourceTransform, float volumeScale = 1f,
                                      float minDistance = -1f, float maxDistance = -1f)
    {
        if (clip == null || sourceTransform == null) return;

        AudioSource source = GetAvailableSFXSource();
        source.transform.position = sourceTransform.position;
        source.spatialBlend = 1f;
        source.minDistance = minDistance > 0 ? minDistance : default3DMinDistance;
        source.maxDistance = maxDistance > 0 ? maxDistance : default3DMaxDistance;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.PlayOneShot(clip, volumeScale);
    }

    // เล่นเสียงแบบ 3D ที่ตำแหน่งที่กำหนด
    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volumeScale = 1f)
    {
        if (clip == null) return;

        AudioSource source = GetAvailableSFXSource();
        source.transform.position = position;
        source.spatialBlend = 1f;
        source.minDistance = default3DMinDistance;
        source.maxDistance = default3DMaxDistance;
        source.rolloffMode = AudioRolloffMode.Linear;
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
        source.rolloffMode = AudioRolloffMode.Linear;
        source.PlayOneShot(clip, volumeScale);
    }

    // เล่นเสียงแบบสุ่ม pitch
    public void PlaySFXRandomPitch(AudioClip clip, float minPitch = 0.9f, float maxPitch = 1.1f)
    {
        if (clip == null) return;

        AudioSource source = GetAvailableSFXSource();
        source.spatialBlend = 0f;
        source.pitch = Random.Range(minPitch, maxPitch);
        source.clip = clip;
        source.Play();

        StartCoroutine(ResetPitchAfterPlay(source, clip.length));
    }

    private System.Collections.IEnumerator ResetPitchAfterPlay(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        source.pitch = 1f;
    }

    // ========== SFX Loop Functions ==========

    public int PlaySFXLoop(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return -1;

        AudioSource source = GetAvailableSFXSource();
        source.spatialBlend = 0f;
        source.clip = clip;
        source.volume = volumeScale;
        source.loop = true;
        source.Play();

        int id = nextLoopID++;
        loopingSFX[id] = source;

        return id;
    }

    public void StopSFXLoop(int id)
    {
        if (loopingSFX.ContainsKey(id))
        {
            AudioSource source = loopingSFX[id];
            if (source != null)
            {
                source.loop = false;
                source.Stop();
                source.volume = 1f;
            }
            loopingSFX.Remove(id);
        }
    }

    public int PlaySFXLoop3D(AudioClip clip, Vector3 position, float volumeScale = 1f, float minDistance = 1f, float maxDistance = 500f)
    {
        if (clip == null) return -1;

        AudioSource source = GetAvailableSFXSource();
        source.transform.position = position;
        source.spatialBlend = 1f;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.clip = clip;
        source.volume = volumeScale;
        source.loop = true;
        source.Play();

        int id = nextLoopID++;
        loopingSFX[id] = source;

        return id;
    }

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

    public void StopAllSFX()
    {
        foreach (AudioSource source in sfxPool)
        {
            source.Stop();
        }
        StopAllSFXLoops();
    }

    // ========== Volume Controls ==========

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
        musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 1f);
        sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
    }

    public void ResetToDefault()
    {
        SetMusicVolume(1f);
        SetSFXVolume(1f);
    }
}