using UnityEngine;
using TMPro;

public class LeaderboardItem : MonoBehaviour
{
    [SerializeField] private TMP_Text _rankText;
    [SerializeField] private TMP_Text _IDText;
    [SerializeField] private TMP_Text _scoreText;

    public void SetData(string rank, string nickname, string score)
    {
        _rankText.text = rank;
        _IDText.text = nickname;
        _scoreText.text = score;
    }
}
