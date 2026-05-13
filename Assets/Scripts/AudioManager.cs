using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private const string MusicVolumeKey = "MusicVolume";
    private const string EffectsVolumeKey = "EffectsVolume";

    public static AudioManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private bool playMusicOnAwake;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip lineClearSound;
    [SerializeField] private AudioClip comboClearSound;
    [SerializeField] private AudioClip blocksRefreshSound;
    [SerializeField] private AudioClip blockPlaceSound;
    
    private AudioSource effectsSource;
    private AudioSource musicSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            effectsSource = gameObject.AddComponent<AudioSource>();
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;

            ApplyVolumes();

            if (playMusicOnAwake && musicClip != null)
            {
                PlayMusic(musicClip);
            }
        }
        else
        {
            Destroy(this);
        }
    }

    public static float GetMusicVolume()
    {
        return PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
    }

    public static float GetEffectsVolume()
    {
        return PlayerPrefs.GetFloat(EffectsVolumeKey, 1f);
    }

    public static void SetMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(volume));
        PlayerPrefs.Save();
        Instance?.ApplyVolumes();
    }

    public static void SetEffectsVolume(float volume)
    {
        PlayerPrefs.SetFloat(EffectsVolumeKey, Mathf.Clamp01(volume));
        PlayerPrefs.Save();
        Instance?.ApplyVolumes();
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null)
            return;

        musicSource.clip = clip;
        musicSource.Play();
    }

    private void ApplyVolumes()
    {
        if (musicSource != null)
        {
            musicSource.volume = GetMusicVolume();
        }

        if (effectsSource != null)
        {
            effectsSource.volume = GetEffectsVolume();
        }
    }

    public void PlayLineClearSound(int clearedLines)
    {
        if (clearedLines <= 0) return;
        
        if (clearedLines >= 3 && comboClearSound != null)
        {
            effectsSource.PlayOneShot(comboClearSound);
        }
        else if (lineClearSound != null)
        {
            effectsSource.PlayOneShot(lineClearSound);
        }
    }

    public void PlayBlocksRefreshSound()
    {
        if (blocksRefreshSound != null)
        {
            effectsSource.PlayOneShot(blocksRefreshSound);
        }
    }

    public void PlayBlockPlaceSound()
    {
        if (blockPlaceSound != null)
        {
            effectsSource.PlayOneShot(blockPlaceSound);
        }
    }
}
