using UnityEngine;
using UnityEngine.SceneManagement;

public enum SceneName { Loading = 0, Lobby = 1, InGame = 2 }

public static class SceneLoadManager
{
    public static string GetActiveScene()
    {
        return SceneManager.GetActiveScene().name;
    }

    public static void LoadScene(string sceneName = "")
    {
        if (sceneName == "")
            SceneManager.LoadScene(GetActiveScene());

        else
            SceneManager.LoadScene(sceneName);
    }

    public static void LoadScene(SceneName sceneName)
    {
        SceneManager.LoadScene(sceneName.ToString());
    }
}
