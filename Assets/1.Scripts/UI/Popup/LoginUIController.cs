using BackEnd;
using LitJson;
using UnityEngine;

public class LoginUIController : MonoBehaviour
{
    [Header("Leaderboard UI References")]
    [SerializeField] private LeaderboardUI _leaderboardUI;
    [SerializeField] private GameObject _leaderboardDim;

    [Header("Nickname UI References")]
    [SerializeField] private NicknameUI _nicknameUI;
    [SerializeField] private GameObject _nicknameDim;

    private void OnEnable()
    {
        LeaderboardManager.OnSetNickname += ActiveSetNickname;
    }

    private void OnDisable()
    {
        LeaderboardManager.OnSetNickname -= ActiveSetNickname;
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

    public void ConfirmNicknameUI()
    {
        string nickname = _nicknameUI.GetNickname();

        Backend.BMember.CreateNickname(nickname, (createBro) =>
        {
            if (createBro.IsSuccess()) Debug.Log("Setting Nickname: " + nickname);
            LeaderboardManager.Instance.FetchGameData();
        });

        _nicknameUI.Close();
        _leaderboardDim.SetActive(true);
        _nicknameDim.SetActive(false);
    }
}