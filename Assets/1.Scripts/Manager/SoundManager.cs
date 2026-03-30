using UnityEngine;

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
    private AudioSource _audioSource;
    [SerializeField] private AudioClip _gameStart;
    [SerializeField] private AudioClip _gameOver;
    [SerializeField] private AudioClip _selectBlock;
    [SerializeField] private AudioClip _placeBlock;
    [SerializeField] private AudioClip _placeFailed;
    [SerializeField] private AudioClip[] _clearLine;
    [SerializeField] private AudioClip _score;
    [SerializeField] private AudioClip _clickUI;


    protected override void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
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

    private void PlayComboSFX(int comboCount)
    {
        if (comboCount <= 0)
            return;
        int index = Mathf.Min(comboCount - 1, _clearLine.Length - 1);
        _audioSource.PlayOneShot(_clearLine[index]);
    }
}