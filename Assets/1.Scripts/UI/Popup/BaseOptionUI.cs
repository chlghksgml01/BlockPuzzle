using UnityEngine;

public class BaseOptionUI : BasePopupUI
{
    public virtual void Replay()
    {
        InGameManager.Instance.ResetGame();
        Close();
    }

    public virtual void Home()
    {
        InGameManager.Instance.ResetGame();
        Close();
        SceneLoadManager.Instance.LoadScene(SceneName.Lobby);
        BoardManager.Instance.ActivateGrayscale(false);
    }

    public override void Close()
    {
        base.Close();
        Time.timeScale = 1f;
    }
}