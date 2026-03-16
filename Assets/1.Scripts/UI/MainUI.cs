using UnityEngine;
using UnityEngine.SceneManagement;

public class MainUI : MonoBehaviour
{
    public void StartClassicMode()
    {
        SceneLoadManager.LoadScene(SceneName.InGame);
    }
}