using UnityEngine;

public class SettingUI : MonoBehaviour
{
    [SerializeField]
    private GameObject _settingPanel;

    public void OnClicked()
    {
        Time.timeScale = 0f;
        _settingPanel.SetActive(true);
    }
}
