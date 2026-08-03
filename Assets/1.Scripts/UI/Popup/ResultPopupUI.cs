using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// LevelInGame 미션 결과 팝업.
/// 성공/실패와 현재 레벨에 따라 ResultText·Level 텍스트를 갱신한 뒤 연다.
/// 씬에서 비활성으로 두어도 MissionManager가 FindObjectsInactive로 찾아 연다.
/// </summary>
public class ResultPopupUI : BasePopupUI
{
    [Header("Result Texts")]
    [Tooltip("성공/실패 결과 텍스트 (예: SUCCESS / FAIL)")]
    [SerializeField] private TextMeshProUGUI _resultText;

    [Tooltip("레벨 번호 텍스트 (예: 'Level  5')")]
    [SerializeField] private TextMeshProUGUI _levelText;

    [Header("Result Labels")]
    [Tooltip("미션 성공 시 ResultText에 표시할 문구")]
    [SerializeField] private string _successLabel = "SUCCESS";

    [Tooltip("미션 실패 시 ResultText에 표시할 문구")]
    [SerializeField] private string _failLabel = "FAIL";

    [Header("Buttons")]
    [Tooltip("같은 레벨을 다시 플레이")]
    [SerializeField] private Button _retryButton;

    [Tooltip("다음 레벨로 이동")]
    [SerializeField] private Button _nextButton;

    [Tooltip("레벨맵으로 나가기")]
    [SerializeField] private Button _quitButton;

    private bool _isShowing;
    private bool _buttonsWired;

    private void OnEnable()
    {
        WireButtons();
    }

    private void OnDisable()
    {
        UnwireButtons();
    }

    private void WireButtons()
    {
        if (_buttonsWired)
            return;

        if (_retryButton != null)
            _retryButton.onClick.AddListener(Retry);

        if (_quitButton != null)
            _quitButton.onClick.AddListener(Quit);

        _buttonsWired = true;
    }

    private void UnwireButtons()
    {
        if (!_buttonsWired)
            return;

        if (_retryButton != null)
            _retryButton.onClick.RemoveListener(Retry);

        if (_quitButton != null)
            _quitButton.onClick.RemoveListener(Quit);

        _buttonsWired = false;
    }

    /// <summary>결과 텍스트를 채우고 팝업을 연다. 비활성 오브젝트에서도 호출 가능하다.</summary>
    public void ShowResult(bool success)
    {
        if (_isShowing)
            return;

        _isShowing = true;

        if (InGameManager.HasInstance)
            InGameManager.Instance.EnableInteraction(false);

        int levelNumber = MissionManager.Instance != null
            ? MissionManager.Instance.CurrentLevelNumber
            : 0;

        if (_resultText != null)
            _resultText.text = success ? _successLabel : _failLabel;

        if (_levelText != null)
            _levelText.text = $"Level  {levelNumber}";

        SetResultButtonVisibility(success);

        // 비활성 상태에서 호출되면 Open 전에 활성화되어 OnEnable에서 버튼을 연결한다.
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        WireButtons();
        Open();
    }

    private void SetResultButtonVisibility(bool success)
    {
        if (_nextButton != null)
            _nextButton.gameObject.SetActive(success);

        if (_retryButton != null)
            _retryButton.gameObject.SetActive(!success);
    }

    /// <summary>같은 레벨을 다시 시작한다.</summary>
    public void Retry()
    {
        _isShowing = false;
        Close();

        if (!InGameManager.HasInstance)
            return;

        InGameManager.Instance.ResetGame();
        // ShowResult에서 막아 둔 슬롯 입력을 Retry 후 다시 연다.
        InGameManager.Instance.EnableInteraction(true);
    }

    /// <summary>레벨맵 씬으로 나간다.</summary>
    public void Quit()
    {
        _isShowing = false;
        Close();

        if (MissionManager.Instance != null)
            MissionManager.Instance.ClearSession();

        if (InGameManager.HasInstance)
            InGameManager.Instance.ResetGame();

        SceneLoadManager.LoadScene(SceneName.Level);
    }
}
