using UnityEngine;
using UnityEngine.Audio;

public enum SFXType
{
    GameStart,
    GameOver,
    SelectBlock,
    PlaceBlock,
    PlaceFailed,
    ClearLine,
    Score,
    ClickUI
}

public class SoundManager : Singleton<SoundManager>
{
    public static readonly string SFXVolumeParam = "Master";
    public static readonly string SoundOnKey = "SoundOn";
    public static readonly string VibrateOnKey = "VibrateOn";

    private AudioSource _audioSource;

    [Header("SFX")]
    [SerializeField] private AudioClip _gameStart;
    [SerializeField] private AudioClip _gameOver;
    [SerializeField] private AudioClip _selectBlock;
    [SerializeField] private AudioClip _placeBlock;
    [SerializeField] private AudioClip _placeFailed;
    [SerializeField] private AudioClip[] _clearLine;
    [SerializeField] private AudioClip _score;
    [SerializeField] private AudioClip _clickUI;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer _audioMixer;

    private bool _isVibrateOn = true;

    public bool IsVibrateOn => _isVibrateOn;

    protected override void Awake()
    {
        _audioSource = GetComponent<AudioSource>();

        InitSettings();
    }

    private void InitSettings()
    {
        bool isSoundOn = PlayerPrefs.GetInt(SoundOnKey, 1) == 1;
        ApplySoundSettings(isSoundOn);

        _isVibrateOn = PlayerPrefs.GetInt(VibrateOnKey, 1) == 1;
        ApplyVibrateSettings(_isVibrateOn);
    }

    public void PlaySFX(SFXType type, int comboCount = 0)
    {
        switch (type)
        {
            case SFXType.GameStart:
                _audioSource.PlayOneShot(_gameStart);
                break;
            case SFXType.GameOver:
                _audioSource.PlayOneShot(_gameOver);
                break;
            case SFXType.SelectBlock:
                _audioSource.PlayOneShot(_selectBlock);
                break;
            case SFXType.PlaceBlock:
                _audioSource.PlayOneShot(_placeBlock);
                if (_isVibrateOn)
                    Vibrate();
                break;
            case SFXType.PlaceFailed:
                _audioSource.PlayOneShot(_placeFailed);
                break;
            case SFXType.ClearLine:
                PlayComboSFX(comboCount);
                break;
            case SFXType.Score:
                _audioSource.PlayOneShot(_score);
                break;
            case SFXType.ClickUI:
                _audioSource.PlayOneShot(_clickUI);
                break;
        }
    }

    // UI에서 호출
    public void PlayUISFX()
    {
        _audioSource.PlayOneShot(_clickUI);
    }

    private void PlayComboSFX(int comboCount)
    {
        if (comboCount <= 0)
            return;
        int index = Mathf.Min(comboCount - 1, _clearLine.Length - 1);
        _audioSource.PlayOneShot(_clearLine[index]);
    }


    public void ApplyVibrateSettings(bool isOn)
    {
        _isVibrateOn = isOn;
        PlayerPrefs.SetInt(VibrateOnKey, isOn ? 1 : 0);
    }

    public void ApplySoundSettings(bool isOn)
    {
        float volume = isOn ? 0f : -80f;
        _audioMixer.SetFloat(SFXVolumeParam, volume);

        PlayerPrefs.SetInt(SoundOnKey, isOn ? 1 : 0);
    }

    public void Vibrate(long milliseconds = 50)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
    try
    {
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");

        if (vibrator.Call<bool>("hasVibrator"))
        {
            vibrator.Call("vibrate", milliseconds);
        }
    }
    catch (System.Exception e)
    {
        Debug.LogError("Vibration Error: " + e.Message);
    }
#endif
    }
}
