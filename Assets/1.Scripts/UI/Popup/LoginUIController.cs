using BackEnd;
using DG.Tweening;
using LitJson;
using TMPro;
using UnityEngine;

public class LoginUIController : MonoBehaviour
{
    [Header("Leaderboard UI References")]
    [SerializeField] private GameObject _leaderboardDim;

    [Header("Nickname UI References")]
    [SerializeField] private NicknameUI _nicknameUI;
    [SerializeField] private GameObject _nicknameDim;
    [SerializeField] private TMP_Text _nicknameErrorText;
    private System.Action _hideNicknameErrorHandler;

    private void OnEnable()
    {
        LeaderboardManager.OnSetNickname += ActiveSetNickname;
        InputManager._onNicknameUIClose += () => OpenNicknameUI(false);

        if (_nicknameUI != null)
        {
            if (_hideNicknameErrorHandler == null)
                _hideNicknameErrorHandler = HideNicknameError;
            _nicknameUI.OnNicknameChanged += _hideNicknameErrorHandler;
        }
    }

    private void OnDisable()
    {
        LeaderboardManager.OnSetNickname -= ActiveSetNickname;
        InputManager._onNicknameUIClose -= () => OpenNicknameUI(false);

        if (_nicknameUI != null && _hideNicknameErrorHandler != null)
            _nicknameUI.OnNicknameChanged -= _hideNicknameErrorHandler;
        _hideNicknameErrorHandler = null;

        if (_nicknameErrorText != null)
            _nicknameErrorText.transform.DOKill();
    }

    private void HideNicknameError()
    {
        if (_nicknameErrorText != null)
            _nicknameErrorText.gameObject.SetActive(false);
    }

    private void ActiveSetNickname(JsonData userData)
    {
        OpenNicknameUI(true);

        string rawId = userData["gamerId"].ToString();
        string shortId = rawId.Substring(0, 4);
        string defaultNick = "Player_" + shortId;
        _nicknameUI.SetDefaultNickname(defaultNick);
    }

    // UI에서 호출
    public void OpenNicknameEditUI()
    {
        if (_nicknameUI == null)
            return;

        OpenNicknameUI(true);

        if (_nicknameErrorText != null)
            _nicknameErrorText.gameObject.SetActive(false);

        string currentNick = PlayerPrefs.GetString(LeaderboardManager.LocalNicknameKey, string.Empty);
        if (string.IsNullOrEmpty(currentNick))
            currentNick = "Player";

        _nicknameUI.SetDefaultNickname(currentNick);
    }

    // UI에서 호출
    public void ConfirmNicknameUI()
    {
        string nickname = _nicknameUI.GetNickname();

        if (string.IsNullOrEmpty(nickname))
            return;

        Backend.BMember.CreateNickname(nickname, (createBro) =>
        {
            if (createBro.IsSuccess())
            {
                Debug.Log("닉네임 설정 성공 : " + nickname);

                OpenNicknameUI(false);

                PlayerPrefs.SetString(LeaderboardManager.LocalNicknameKey, nickname);

                LeaderboardManager.Instance.FetchGameData();
            }
            else
            {
                HandleNicknameError(createBro.GetStatusCode());
            }
        });
    }

    private void OpenNicknameUI(bool isOpen)
    {
        if (isOpen)
        {
            _nicknameUI.Open();

            if (_leaderboardDim != null)
                _leaderboardDim.SetActive(false);
            if (_nicknameDim != null)
                _nicknameDim.SetActive(true);
        }
        else
        {
            _nicknameUI.Close();
            if (_leaderboardDim != null)
                _leaderboardDim.SetActive(true);
            if (_nicknameDim != null)
                _nicknameDim.SetActive(false);
            _nicknameErrorText.gameObject.SetActive(false);
        }
    }

    private void HandleNicknameError(string statusCode)
    {
        _nicknameErrorText.gameObject.SetActive(true);
        _nicknameErrorText.transform.DOComplete();
        _nicknameErrorText.transform.DOPunchPosition(new Vector3(10f, 0, 0), 0.5f, 20, 1f)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        switch (statusCode)
        {
            case "409":
                Debug.LogWarning("존재하는 닉네임");
                _nicknameErrorText.text = "Nickname already taken.";
                break;

            case "400":
                Debug.LogWarning("닉네임 형식이 올바르지 않음");
                _nicknameErrorText.text = "Invalid nickname format.";
                break;

            default:
                Debug.LogError("닉네임 생성 실패: " + statusCode);
                _nicknameErrorText.text = "An unexpected error occurred.";
                break;
        }
    }
}
