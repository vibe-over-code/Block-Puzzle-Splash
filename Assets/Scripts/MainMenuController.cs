using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    // Имя сцены, где находится ваша игра (сетка, блоки и т.д.)
    // Согласно вашему запросу, это "SampleScene"
    [SerializeField] private string gameSceneName = "SampleScene";

    public void StartGame()
    {
        Debug.Log("Запуск игры...");
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenSettings()
    {
        Debug.Log("Нажата кнопка настроек!");
        // TODO: Реализуйте логику панели настроек здесь (например, включите Canvas настроек)
        // Пока что просто выводим сообщение в консоль.
    }

    public void ExitGame()
    {
        Debug.Log("Выход из игры...");

        // Application.Quit() не работает в редакторе Unity.
        // UnityEditor.EditorApplication.isPlaying = false; останавливает режим игры в редакторе.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
