using UnityEngine.SceneManagement;

public enum SceneName { Loading = 0, Lobby = 1, InGame = 2 }

public static class SceneLoadManager
{
    public static SceneName GetActiveScene()
    {
        return (SceneName)System.Enum.Parse(typeof(SceneName), SceneManager.GetActiveScene().name);
    }

    public static void LoadScene(SceneName sceneName)
    {
        SceneManager.LoadScene(sceneName.ToString());
    }
}
