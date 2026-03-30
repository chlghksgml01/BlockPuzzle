using UnityEngine;
using UnityEngine.UI;

public class UIButtonSound : MonoBehaviour
{
    void Start()
    {
        Button button = GetComponent<Button>();
        button.onClick.AddListener(() => { SoundManager.Instance.PlayUISFX(); });
    }
}