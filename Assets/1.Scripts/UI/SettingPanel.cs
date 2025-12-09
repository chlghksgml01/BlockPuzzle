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
        BoardManager.Instance.Replay();
        Close();
    }

    public void Home()
    {
        SceneManager.LoadScene("MainUI");
    }

    public void Close()
    {
        Time.timeScale = 1f;
        gameObject.SetActive(false);
    }
}
