using UnityEngine;
using BackEnd;
using System;
using LitJson;

[DefaultExecutionOrder(-100)]
public class SaveManager : Singleton<SaveManager>
{
    [Header("Configurations")]
    private const string BestScoreKey = "BestScore";
    private const string TableName = "BEST_SCORE";
    private const string ScoreColumn = "bestscore";

    [Header("Runtime State")]
    private int _bestScore = 0;
    public int BestScore => _bestScore;

    private string _userIndate = string.Empty;
    public static event Action OnSyncSucceed;

    protected override void OnAwake()
    {
        _bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
    }

    private void OnEnable()
    {
        ScoreManager.OnHighScoreUpdated += UpdateBestScore;
        GoogleLoginManager.OnLoginSucceed += SyncWithServer;
    }

    private void OnDisable()
    {
        ScoreManager.OnHighScoreUpdated -= UpdateBestScore;
        GoogleLoginManager.OnLoginSucceed -= SyncWithServer;
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
                    string rawId = userData["gamerId"].ToString();
                    string shortId = rawId.Substring(0, 4);
                    string defaultNick = "Player_" + shortId;

                    Backend.BMember.CreateNickname(defaultNick, (createBro) =>
                    {
                        if (createBro.IsSuccess()) Debug.Log("Setting Nickname: " + defaultNick);
                        FetchGameData();
                    });
                }
                else
                {
                    FetchGameData();
                }
            }
        });
    }

    private void FetchGameData()
    {
        Backend.GameData.GetMyData(TableName, new Where(), (bro) =>
        {
            if (bro.IsSuccess())
            {
                if (bro.FlattenRows().Count > 0)
                {
                    _userIndate = bro.FlattenRows()[0]["inDate"].ToString();
                    int serverScore = int.Parse(bro.FlattenRows()[0][ScoreColumn].ToString());

                    if (serverScore > _bestScore)
                    {
                        _bestScore = serverScore;
                        PlayerPrefs.SetInt(BestScoreKey, _bestScore);
                    }
                }
                else
                {
                    CreateInitialServerData();
                }

                OnSyncSucceed?.Invoke();
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
        }
    }



    private void UpdateBestScore(int newScore)
    {
        _bestScore = newScore;
        PlayerPrefs.SetInt(BestScoreKey, _bestScore);

        if (Backend.IsLogin)
        {
            Param param = new Param();
            param.Add(ScoreColumn, _bestScore);

            // _userIndate가 비어있다면 새로 생성(Insert), 있다면 수정(Update)
            if (string.IsNullOrEmpty(_userIndate))
            {
                var bro = Backend.GameData.Insert(TableName, param);
                if (bro.IsSuccess()) _userIndate = bro.GetInDate();
            }
            else
            {
                // V2 업데이트 방식을 사용하여 서버 점수 갱신
                Backend.GameData.UpdateV2(TableName, _userIndate, Backend.UserInDate, param);
            }
        }
    }
}