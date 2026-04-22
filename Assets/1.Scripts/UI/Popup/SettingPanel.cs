using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : BaseOptionUI, IInitializable
{
    [Header("Sound Settings")]
    [SerializeField] private Slider _soundSlider;

    [Header("Vibration Settings")]
    [SerializeField] private Slider _vibrateSlider;
    [SerializeField] private ScoreUI _scoreUI;

    private ScoreSystem _scoreSystem;

    public void Initialize(InitializeContext context)
    {
        _scoreSystem = context.ScoreSystem;
    }

    private void OnEnable()
    {
        Time.timeScale = 0f;

        SetSliders();
    }

    private void SetSliders()
    {
        bool isSoundOn = PlayerPrefs.GetInt(SoundManager.SoundOnKey, 1) == 1;
        _soundSlider.value = isSoundOn ? 1 : 0;

        bool isVibrateOn = SoundManager.Instance.IsVibrateOn;
        _vibrateSlider.value = isVibrateOn ? 1 : 0;
    }

    public override void Replay()
    {
        _scoreUI?.ResetScore();
        base.Replay();
        LeaderboardManager.Instance.UpdateBestScore(_scoreSystem.CurrentScore);
    }

    public override void Home()
    {
        base.Home();
        LeaderboardManager.Instance.UpdateBestScore(_scoreSystem.CurrentScore);
    }

    // UI에서 호출
    public void ToggleSound()
    {
        float nextValue = (_soundSlider.value > 0.5f) ? 0f : 1f;
        _soundSlider.value = nextValue;

        SoundManager.Instance.ApplySoundSettings(nextValue > 0.5f);
        if (nextValue > 0.5f)
            SoundManager.Instance.PlaySFX(SFXType.ClickUI);
    }

    public void ToggleVibrate()
    {
        float nextValue = (_vibrateSlider.value > 0.5f) ? 0f : 1f;
        _vibrateSlider.value = nextValue;

        SoundManager.Instance.ApplyVibrateSettings(nextValue > 0.5f);
        if (nextValue > 0.5f)
            SoundManager.Instance.Vibrate(30);
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
