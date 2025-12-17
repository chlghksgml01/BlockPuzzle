using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LoadingUI : MonoBehaviour
{
    [SerializeField]
    private Image LoadingBar;
    [SerializeField]
    private TextMeshProUGUI LoadingValuleText;
    [SerializeField]
    private float progressTime = 3f;

    private void Awake()
    {
        Play();
    }

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

            LoadingBar.fillAmount = Mathf.Lerp(0f, 1f, percent);
            LoadingValuleText.text = $" {LoadingBar.fillAmount * 100f:0}%";
            yield return null;
        }

        action?.Invoke();
    }
}