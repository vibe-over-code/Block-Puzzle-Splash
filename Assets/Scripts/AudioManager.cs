using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sound Effects")]
    [SerializeField] private AudioClip lineClearSound;
    [SerializeField] private AudioClip comboClearSound;
    [SerializeField] private AudioClip blocksRefreshSound;
    [SerializeField] private AudioClip blockPlaceSound;
    
    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayLineClearSound(int clearedLines)
    {
        if (clearedLines <= 0) return;
        
        if (clearedLines >= 3 && comboClearSound != null)
        {
            audioSource.PlayOneShot(comboClearSound);
        }
        else if (lineClearSound != null)
        {
            audioSource.PlayOneShot(lineClearSound);
        }
    }

    public void PlayBlocksRefreshSound()
    {
        if (blocksRefreshSound != null)
        {
            audioSource.PlayOneShot(blocksRefreshSound);
        }
    }

    public void PlayBlockPlaceSound()
    {
        if (blockPlaceSound != null)
        {
            audioSource.PlayOneShot(blockPlaceSound);
        }
    }
}