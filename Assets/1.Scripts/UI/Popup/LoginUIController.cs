using BackEnd;
using DG.Tweening;
using LitJson;
using TMPro;
using UnityEngine;

public class LoginUIController : MonoBehaviour
{
    [Header("Leaderboard UI References")]
    [SerializeField] private LeaderboardUI _leaderboardUI;
    [SerializeField] private GameObject _leaderboardDim;

    [Header("Nickname UI References")]
    [SerializeField] private NicknameUI _nicknameUI;
    [SerializeField] private GameObject _nicknameDim;
    [SerializeField] private TMP_Text _nicknameErrorText;
    private System.Action _hideNicknameErrorHandler;

    private void OnEnable()
    {
        LeaderboardManager.OnSetNickname += ActiveSetNickname;
        _hideNicknameErrorHandler = HideNicknameError;
        _nicknameUI.OnNicknameChanged += _hideNicknameErrorHandler;
    }

    private void OnDisable()
    {
        LeaderboardManager.OnSetNickname -= ActiveSetNickname;
        if (_hideNicknameErrorHandler != null && _nicknameUI != null)
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
        _nicknameUI.Open();
        _leaderboardDim.SetActive(false);
        _nicknameDim.SetActive(true);

        string rawId = userData["gamerId"].ToString();
        string shortId = rawId.Substring(0, 4);
        string defaultNick = "Player_" + shortId;
        _nicknameUI.SetDefaultNickname(defaultNick);
    }

    // 버튼UI에서 호출
    public void ConfirmNicknameUI()
    {
        string nickname = _nicknameUI.GetNickname();

        // 유효성 검사 (공백 등 기본 체크)
        if (string.IsNullOrEmpty(nickname))
            return;

        Backend.BMember.CreateNickname(nickname, (createBro) =>
        {
            if (createBro.IsSuccess())
            {
                Debug.Log("닉네임 설정 성공 : " + nickname);

                _nicknameUI.Close();
                _leaderboardDim.SetActive(true);
                _nicknameDim.SetActive(false);
                _nicknameErrorText.gameObject.SetActive(false);

                LeaderboardManager.Instance.FetchGameData();
            }
            else
            {
                HandleNicknameError(createBro.GetStatusCode());
            }
        });
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