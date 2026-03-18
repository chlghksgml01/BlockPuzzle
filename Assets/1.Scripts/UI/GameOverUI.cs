using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : BaseOptionUI
{
    [Header("Digit")]

    [SerializeField] private GameObject _digitPrefab;
    [SerializeField] private Sprite[] _digitSprites;
    [SerializeField] private Transform _scoreContainer;
    [SerializeField] private Transform _bestScoreContainer;

    [Header("Banner")]
    [SerializeField] private Image _titleBanner;
    [SerializeField] private Sprite _defaultBanner;
    [SerializeField] private Sprite _newBestBanner;

    private void OnEnable()
    {
        UpdateDisplay(_scoreContainer, ScoreManager.Instance.CurrentScore, 0);
        UpdateDisplay(_bestScoreContainer, LeaderboardManager.Instance.BestScore, 2);
    }

    public void UpdateBanner(bool isNewBest)
    {
        if (isNewBest)
        {
            _titleBanner.sprite = _newBestBanner;
        }
        else
        {
            _titleBanner.sprite = _defaultBanner;
        }
    }

    private void UpdateDisplay(Transform container, int score, int skipCount)
    {
        for (int i = container.childCount - 1; i >= skipCount; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }

        string numberStr = score.ToString();

        foreach (char num in numberStr)
        {
            int digitIndex = int.Parse(num.ToString());

            GameObject newDigit = Instantiate(_digitPrefab, container);
            newDigit.GetComponent<Image>().sprite = _digitSprites[digitIndex];
        }
    }

}