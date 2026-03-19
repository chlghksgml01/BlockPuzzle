using UnityEngine;
using UnityEngine.UI;

public class NumberDisplay : MonoBehaviour
{
    [SerializeField] private GameObject _digitPrefab;
    [SerializeField] private Sprite[] _digitSprites;
    [SerializeField] private Transform _container;

    public void UpdateDisplay(int score, int skipCount = 0)
    {
        for (int i = _container.childCount - 1; i >= skipCount; i--)
        {
            Destroy(_container.GetChild(i).gameObject);
        }

        string numberStr = score.ToString();

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
}