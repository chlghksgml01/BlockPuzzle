using TMPro;
using UnityEngine;

public class GameOverUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _scroreText;
    [SerializeField]
    private TextMeshProUGUI _bestScroreText;

    private void OnEnable()
    {
        _scroreText.text = ScoreManager.Instance.CurrentScore.ToString();
    }
}