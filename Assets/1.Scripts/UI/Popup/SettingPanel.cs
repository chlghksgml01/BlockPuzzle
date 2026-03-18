using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : BaseOptionUI
{
    [Header("Sound Settings")]
    [SerializeField] private Image _soundBG;
    [SerializeField] private GameObject _soundToggle;

    [Header("BGM Settings")]
    [SerializeField] private Image _bgmBG;
    [SerializeField] private GameObject _bgmToggle;

    private void OnEnable()
    {
        Time.timeScale = 0f;
    }

    public override void Replay()
    {
        base.Open();
        LeaderboardManager.Instance.UpdateBestScore(ScoreManager.Instance.CurrentScore);
    }

    public override void Home()
    {
        base.Home();
        LeaderboardManager.Instance.UpdateBestScore(ScoreManager.Instance.CurrentScore);
    }
}
