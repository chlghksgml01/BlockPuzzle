using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SceneName { Loading = 0, Lobby = 1, InGame = 2 }

public class SceneLoadManager : Singleton<SceneLoadManager>
{
    public string GetActiveScene()
    {
        return SceneManager.GetActiveScene().name;
    }

    public void LoadScene(SceneName sceneName)
    {
        SceneManager.LoadScene(sceneName.ToString());
    }
}
