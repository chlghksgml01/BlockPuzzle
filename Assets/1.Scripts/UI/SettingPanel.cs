using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : MonoBehaviour
{
    [SerializeField]
    private Image _soundBG;
    [SerializeField]
    private GameObject _soundToggle;

    [SerializeField]
    private Image _bgmBG;
    [SerializeField]
    private GameObject _bgmToggle;

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
