using TMPro;
using UnityEngine;

public class LeaderboardItem : MonoBehaviour
{
    [SerializeField] private TMP_Text _rankText;
    [SerializeField] private TMP_Text _nicknameText;
    [SerializeField] private TMP_Text _scoreText;

    public void SetData(string rank, string nickname, string score)
    {
        _rankText.text = rank;
        _nicknameText.text = nickname;
        _scoreText.text = score;
    }
}
