using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider effectsVolumeSlider;

    private void Awake()
    {
        InitializeSettings();
    }

    public void StartGame()
    {
        Debug.Log("Start game");
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void ExitGame()
    {
        Debug.Log("Exit game");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void InitializeSettings()
    {
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.SetValueWithoutNotify(AudioManager.GetMusicVolume());
            musicVolumeSlider.onValueChanged.AddListener(AudioManager.SetMusicVolume);
        }

        if (effectsVolumeSlider != null)
        {
            effectsVolumeSlider.SetValueWithoutNotify(AudioManager.GetEffectsVolume());
            effectsVolumeSlider.onValueChanged.AddListener(AudioManager.SetEffectsVolume);
        }

        CloseSettings();
    }
}
