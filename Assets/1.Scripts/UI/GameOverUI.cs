using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField]
    private GameObject _digitPrefab;
    [SerializeField]
    public Sprite[] _digitSprites;
    [SerializeField]
    public Transform _scoreContainer;
    [SerializeField]
    public Transform _bestScoreContainer;

    private List<GameObject> _activeDigits = new List<GameObject>();

    private void OnEnable()
    {
        UpdateDisplay(_scoreContainer, ScoreManager.Instance.CurrentScore, 0);

        UpdateDisplay(_bestScoreContainer, SaveManager.Instance.BestScore, 2);
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