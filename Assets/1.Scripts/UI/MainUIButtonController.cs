using UnityEngine;
using UnityEngine.UI;

public class MainUIButtonController : MonoBehaviour
{
    [Header("Lobby Buttons")]
    [Tooltip("리더보드 팝업을 여는 버튼")]
    [SerializeField] private Button _leaderBoardButton;

    [Tooltip("클래식 모드로 진입하는 버튼")]
    [SerializeField] private Button _startButton;

    [Tooltip("레벨 맵으로 진입하는 버튼. 미로그인 시 구글 로그인을 먼저 수행한다")]
    [SerializeField] private Button _levelButton;

    [Header("UI References")]
    [Tooltip("로비 리더보드 팝업")]
    [SerializeField] private LeaderboardUI _leaderBoardUI;

    private bool _isWaitingLevelLogin;

    private void Start()
    {
        _leaderBoardButton.onClick.AddListener(() => _leaderBoardUI.Open());
        _startButton.onClick.AddListener(() => SceneLoadManager.LoadScene(SceneName.Classic));
        _levelButton.onClick.AddListener(OnLevelButtonClicked);
    }

    private void OnDisable()
    {
        CancelWaitingLevelLogin();
    }

    /// <summary>
    /// 레벨 맵 진입. 미로그인 상태면 구글 로그인을 먼저 수행하고, 성공 시에만 씬을 로드한다.
    /// </summary>
    private void OnLevelButtonClicked()
    {
        if (_isWaitingLevelLogin)
            return;

#if UNITY_EDITOR
        // 에디터에서는 Android 구글 로그인을 수행할 수 없어 레벨 맵으로 바로 진입한다.
        LoadLevelScene();
#else
        GoogleLoginManager loginManager = GoogleLoginManager.Instance;
        if (loginManager != null && loginManager.IsLoggedIn)
        {
            LoadLevelScene();
            return;
        }

        if (loginManager == null)
        {
            Debug.LogWarning("[MainUIButtonController] GoogleLoginManager가 없어 레벨 맵으로 바로 진입합니다.");
            LoadLevelScene();
            return;
        }

        _isWaitingLevelLogin = true;
        _levelButton.interactable = false;
        GoogleLoginManager.OnLoginSucceed += OnLevelLoginCompleted;
        loginManager.StartGoogleLogin();
#endif
    }

    private void OnLevelLoginCompleted(bool isSucceed)
    {
        CancelWaitingLevelLogin();

        if (!isSucceed)
            return;

        LoadLevelScene();
    }

    private void CancelWaitingLevelLogin()
    {
        if (!_isWaitingLevelLogin)
            return;

        GoogleLoginManager.OnLoginSucceed -= OnLevelLoginCompleted;
        _isWaitingLevelLogin = false;

        if (_levelButton != null)
            _levelButton.interactable = true;
    }

    private static void LoadLevelScene()
    {
        SceneLoadManager.LoadScene(SceneName.Level);
    }
}
