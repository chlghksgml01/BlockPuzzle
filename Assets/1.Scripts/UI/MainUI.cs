using UnityEngine;

public class MainUI : MonoBehaviour
{
    public void StartClassicMode()
    {
        SceneLoadManager.LoadScene(SceneName.InGame);
    }
}