using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Progress : MonoBehaviour
{
    [SerializeField]
    private Slider progressSlider;
    [SerializeField]
    private TextMeshProUGUI progressText;
    [SerializeField]
    private float progressTime = 3f;

    public void Play(UnityAction action = null)
    {
        StartCoroutine(OnProgress(action));
    }

    private IEnumerator OnProgress(UnityAction action)
    {
        float current = 0f;
        float percent = 0f;

        while (percent < 1)
        {
            current += Time.deltaTime;
            percent = current / progressTime;

            progressText.text = $"·ÎµùÁß... {progressSlider.value * 100f:0}%";
            progressSlider.value = Mathf.Lerp(0f, 1f, percent);
            yield return null;
        }

        action?.Invoke();
    }
}