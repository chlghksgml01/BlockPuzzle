using UnityEngine;
using UnityEngine.InputSystem;

public class SettingUI : MonoBehaviour
{
    [SerializeField] private SettingPanel _settingPanel;
    [SerializeField] private GameOverUI _GameOverUI;

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame && !_settingPanel.isActiveAndEnabled && !_GameOverUI.isActiveAndEnabled)
        {
            _settingPanel.Open();
            SoundManager.Instance.PlaySFX(SFXType.ClickUI);
        }
    }
}