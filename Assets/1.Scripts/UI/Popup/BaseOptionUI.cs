using UnityEngine;

public class BaseOptionUI : BasePopupUI
{
    public virtual void Replay()
    {
        InGameManager.Instance.ResetGame();
        Close();
    }

    public virtual void Home(bool isLobby)
    {
        if (InGameManager.HasInstance)
        {
            InGameManager.Instance.ResetGame();
        }
        Close();
        SceneLoadManager.LoadScene(isLobby ? SceneName.Lobby : SceneName.Level);
    }

    public override void Close()
    {
        base.Close();
        Time.timeScale = 1f;
    }
}