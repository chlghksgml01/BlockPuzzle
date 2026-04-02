using UnityEngine;
using UnityEngine.InputSystem;

public class SettingUIController : MonoBehaviour
{
    [SerializeField] private SettingPanel _settingPanel;
    [SerializeField] private GameOverUI _GameOverUI;

    private InputSystem_Actions _inputAction;

    private void Awake()
    {
        _inputAction = new InputSystem_Actions();
        _inputAction.UI.Escape.performed += CloseToKeyboard;
    }

    private void CloseToKeyboard(InputAction.CallbackContext context)
    {
        if (!_settingPanel.gameObject.activeSelf && !_GameOverUI.gameObject.activeSelf)
        {
            _settingPanel.Open();
            SoundManager.Instance.PlaySFX(SFXType.ClickUI);
        }
        else if (_settingPanel.gameObject.activeSelf)
        {
            _settingPanel.Close();
            SoundManager.Instance.PlaySFX(SFXType.ClickUI);
        }
    }


    private void OnEnable()
    {
        _inputAction.Enable();
    }

    private void OnDisable()
    {
        _inputAction.Disable();
    }
}