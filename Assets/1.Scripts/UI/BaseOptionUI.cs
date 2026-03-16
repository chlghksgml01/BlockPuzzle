using UnityEngine;

public class BaseOptionUI : MonoBehaviour
{
    public void Replay()
    {
        InGameManager.Instance.ResetGame();
        Close();
    }

    public void Home()
    {
        InGameManager.Instance.ResetGame();
        Close();
        SceneLoadManager.LoadScene(SceneName.Lobby);
    }

    public void Close()
    {
        Time.timeScale = 1f;
        gameObject.SetActive(false);
    }
}