using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class NumberDisplay : MonoBehaviour
{
    [SerializeField] private GameObject _digitPrefab;
    [SerializeField] private Sprite[] _digitSprites;
    [SerializeField] private Transform _container;

    private Tween _scoreTween;

    public void UpdateDisplay(int value, int skipCount = 0)
    {
        if (this == null || gameObject == null)
            return;

        for (int i = _container.childCount - 1; i >= skipCount; i--)
        {
            Destroy(_container.GetChild(i).gameObject);
        }

        string numberStr = value.ToString();

        foreach (char num in numberStr)
        {
            int digitIndex = (int)char.GetNumericValue(num);

            GameObject newDigit = Instantiate(_digitPrefab, _container);
            Image digitImage = newDigit.GetComponent<Image>();

            if (digitImage != null && digitIndex < _digitSprites.Length)
            {
                digitImage.sprite = _digitSprites[digitIndex];
            }
        }
    }

    public void ScoreRollUpdate(int currentScore, int newScore, float animationDuration = 0.5f)
    {
        _scoreTween?.Kill();

        _scoreTween = DOVirtual.Int(currentScore, newScore, animationDuration, (value) =>
        {
            UpdateDisplay(value);
        })
        .SetEase(Ease.OutQuad);
    }
}