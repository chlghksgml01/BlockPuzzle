using BackEnd;
using LitJson;
using System;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class LeaderboardManager : Singleton<LeaderboardManager>, IInitializable
{
    private const string RankUuid = "019cffa2-ab30-7c69-8b25-0025d37e6deb";
    private const string BestScoreKey = "BestScore";
    public const string LocalNicknameKey = "LocalNickname";
    private const string TableName = "BEST_SCORE";
    private const string ScoreColumn = "bestscore";

    private int _bestScore = 0;
    public int BestScore => _bestScore;

    private string _userIndate = string.Empty;
    public static event Action<JsonData> OnRankDataReceived;
    public static event Action<JsonData> OnSetNickname;

    private ScoreSystem _scoreSystem;

    public void Initialize(InitializeContext context)
    {
        _scoreSystem = context.ScoreSystem;
    }

    protected override void OnAwake()
    {
        _bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
    }

    private void OnEnable()
    {
        GoogleLoginManager.OnLoginSucceed += SyncWithServer;
        _scoreSystem.OnHighScoreUpdated += UpdateBestScore;
    }

    private void OnDisable()
    {
        GoogleLoginManager.OnLoginSucceed -= SyncWithServer;
        _scoreSystem.OnHighScoreUpdated -= UpdateBestScore;
    }

    private void SyncWithServer(bool isSucceed)
    {
        if (!isSucceed)
            return;

        Backend.BMember.GetUserInfo(userBro =>
        {
            if (userBro.IsSuccess())
            {
                JsonData userData = userBro.GetReturnValuetoJSON()["row"];

                if (userData["nickname"] == null || string.IsNullOrEmpty(userData["nickname"].ToString()))
                {
                    OnSetNickname?.Invoke(userData);
                }
                else
                {
                    PlayerPrefs.SetString(LocalNicknameKey, userData["nickname"].ToString());
                    FetchGameData();
                }
            }
        });
    }

    public void FetchGameData()
    {
        Backend.GameData.GetMyData(TableName, new Where(), (bro) =>
        {
            if (bro.IsSuccess())
            {
                // 기존 유저
                if (bro.FlattenRows().Count > 0)
                {
                    _userIndate = bro.FlattenRows()[0]["inDate"].ToString();
                    int serverScore = int.Parse(bro.FlattenRows()[0][ScoreColumn].ToString());

                    _bestScore = Mathf.Max(_bestScore, serverScore);
                    PlayerPrefs.SetInt(BestScoreKey, _bestScore);

                    UpdateBestScore(_bestScore);
                }
                // 신규 유저
                else
                {
                    CreateInitialServerData();
                }
            }
        });
    }

    private void CreateInitialServerData()
    {
        Param param = new Param();
        param.Add(ScoreColumn, _bestScore);

        var bro = Backend.GameData.Insert(TableName, param);
        if (bro.IsSuccess())
        {
            _userIndate = bro.GetInDate();
            UpdateBestScore(_bestScore);
        }
    }

    public void UpdateBestScore(int newScore)
    { 
        Debug.Log("Current Score " + newScore);
        if (_bestScore > newScore)
            return;

        Debug.Log("New high score " + newScore);
        _bestScore = newScore;

        PlayerPrefs.SetInt(BestScoreKey, _bestScore);

        if (Backend.IsLogin)
        {
            Param param = new Param();
            param.Add(ScoreColumn, _bestScore);

            if (string.IsNullOrEmpty(_userIndate))
            {
                Backend.GameData.Insert(TableName, param, (bro) =>
                {
                    if (bro.IsSuccess())
                    {
                        _userIndate = bro.GetInDate();
                        UpdateLeaderboard(_bestScore);
                    }
                });
            }
            else
            {
                Backend.GameData.UpdateV2(TableName, _userIndate, Backend.UserInDate, param, (bro) =>
                {
                    if (bro.IsSuccess())
                    {
                        UpdateLeaderboard(_bestScore);
                    }
                });
            }
        }
    }

    private void UpdateLeaderboard(int score)
    {
        Param param = new Param();
        param.Add(ScoreColumn, score);

        Backend.URank.User.UpdateUserScore(RankUuid, TableName, _userIndate, param, (bro) =>
        {
            if (bro.IsSuccess())
            {
                Debug.Log($"[리더보드] 점수 갱신 성공: {score}");
            }
            else
            {
                Debug.LogError($"[리더보드] 갱신 실패: {bro.GetErrorCode()} - {bro.GetMessage()}");
            }
            GetRank();
        });
    }

    public void GetRank()
    {
        Backend.URank.User.GetRankList(RankUuid, 50, (bro) =>
        {
            JsonData rankData = new JsonData();

            if (bro.IsSuccess())
            {
                rankData = bro.FlattenRows();
                Debug.Log("Ranking lookup succeeded");
            }
            else
            {
                Debug.LogError("Ranking lookup failed : " + bro.GetErrorCode());
            }

            OnRankDataReceived?.Invoke(rankData);
        });
    }

    public void DeleteBestScore() => _bestScore = 0;
}
