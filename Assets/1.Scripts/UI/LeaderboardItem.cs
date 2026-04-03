using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardItem : MonoBehaviour
{
    [SerializeField] private TMP_Text _rankText;
    [SerializeField] private TMP_Text _nicknameText;
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private Image _background;

    public void SetData(string rank, string nickname, string score)
    {
        _rankText.text = rank;
        _nicknameText.text = nickname;
        _scoreText.text = score;
    }

    public void SetLocalUser(Color localUserColor, Sprite localUserSprite)
    {
        _rankText.color = localUserColor;
        _nicknameText.color = localUserColor;
        _scoreText.color = localUserColor;

        if (_background != null)
        {
            _background.sprite = localUserSprite;
            _background.SetNativeSize();
        }
    }
}
