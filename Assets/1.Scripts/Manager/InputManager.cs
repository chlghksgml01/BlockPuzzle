using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private SettingPanel _settingPanel;
    [SerializeField] private GameOverUI _gameOverUI;
    [SerializeField] private LeaderboardUI _leaderboardUI;
    [SerializeField] private NicknameUI _nicknameUI;

    [Header("Input Action")]
    [SerializeField] private InputActionReference _escapeActionReference;

    public static event Action _onNicknameUIClose;

    private void OnEnable()
    {
        if (_escapeActionReference != null)
        {
            _escapeActionReference.action.performed += HandleEscape;
            _escapeActionReference.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (_escapeActionReference != null)
        {
            _escapeActionReference.action.performed -= HandleEscape;
            _escapeActionReference.action.Disable();
        }
    }

    // 테스트용
#if UNITY_EDITOR
    private void Update()
    {
        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            PlayerPrefs.DeleteAll();
            LeaderboardManager.Instance.DeleteBestScore();
        }
    }
#endif


    private void HandleEscape(InputAction.CallbackContext context)
    {
        SceneName currentScene = SceneLoadManager.GetActiveScene();

        if (currentScene == SceneName.Lobby)
        {
            HandleLobbyEscape();
        }
        else if (currentScene == SceneName.InGame)
        {
            HandleInGameEscape();
        }
    }

    private void HandleLobbyEscape()
    {
        if (_nicknameUI == null || _leaderboardUI == null)
            return;
        if (_nicknameUI.gameObject.activeSelf)
        {
            _onNicknameUIClose?.Invoke();
            LeaderboardManager.Instance.GetRank();
            _nicknameUI.Close();
            PlayClickSound();
        }
        else if (_leaderboardUI.gameObject.activeSelf)
        {
            _leaderboardUI.Close();
            PlayClickSound();
        }
    }

    private void HandleInGameEscape()
    {
        if (_settingPanel == null || _gameOverUI == null)
            return;

        if (!_settingPanel.gameObject.activeSelf && !_gameOverUI.gameObject.activeSelf)
        {
            _settingPanel.Open();
            PlayClickSound();
        }
        else if (_settingPanel.gameObject.activeSelf)
        {
            _settingPanel.Close();
            PlayClickSound();
        }
    }

    private void PlayClickSound()
    {
        SoundManager.Instance.PlaySFX(SFXType.ClickUI);
    }
}