using UnityEngine;
using UnityEngine.SceneManagement;
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
        BoardManager.Instance.ResetBoard();
        Close();
    }

    public void Home()
    {
        BoardManager.Instance.ResetBoard();
        Close();
        SceneManager.LoadScene("MainUI");
    }

    public void Close()
    {
        Time.timeScale = 1f;
        gameObject.SetActive(false);
    }
}
