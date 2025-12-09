using UnityEngine;

public class SettingUI : MonoBehaviour
{
    [SerializeField]
    private GameObject _settingPanel;

    public void OnClicked()
    {
        _settingPanel.SetActive(true);

    }
}
