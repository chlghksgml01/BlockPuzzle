using UnityEngine;

public class BaseOptionUI : BasePopupUI
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

    public override void Close()
    {
        base.Close();
        Time.timeScale = 1f;
    }
}