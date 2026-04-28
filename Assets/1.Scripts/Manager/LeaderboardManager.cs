using BackEnd;
using LitJson;
using System;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class LeaderboardManager : Singleton<LeaderboardManager>, IInitializable
{
    private const string RankUuid = "019cffa2-ab30-7c69-8b25-0025d37e6deb";
    private const string GameConfigChartId = "236370";
    private const string BestScoreKey = "BestScore";
    private const string WeeklyBestScoreKey = "WeeklyBestScore";
    private const string WeeklySeasonKey = "WeeklySeasonKey";
    public const string LocalNicknameKey = "LocalNickname";
    private const string TableName = "BEST_SCORE";
    private const string ScoreColumn = "bestscore";

    private int _bestScore = 0;
    public int BestScore => _bestScore;
    private int _weeklyBestScore = 0;
    private int _weeklyResetDay = 1;
    private int _weeklyResetHour = 4;

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
        RefreshWeeklySeasonState();
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

        LoadWeeklyResetConfigFromChart();
        RefreshWeeklySeasonState();

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

                    TrySyncAllTimeBestToServer();
                    TryUpdateWeeklyLeaderboardFromCache();
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
            TryUpdateWeeklyLeaderboardFromCache();
        }
    }

    public void UpdateBestScore(int newScore)
    {
        Debug.Log("Current Score " + newScore);
        bool hasNewAllTimeBest = newScore > _bestScore;
        bool hasNewWeeklyBest = newScore > _weeklyBestScore;

        if (!hasNewAllTimeBest && !hasNewWeeklyBest)
            return;

        if (hasNewAllTimeBest)
        {
            Debug.Log("New all-time high score " + newScore);
            _bestScore = newScore;
            PlayerPrefs.SetInt(BestScoreKey, _bestScore);
        }

        if (hasNewWeeklyBest)
        {
            Debug.Log("New weekly high score " + newScore);
            _weeklyBestScore = newScore;
            PlayerPrefs.SetInt(WeeklyBestScoreKey, _weeklyBestScore);
        }

        if (!Backend.IsLogin)
            return;

        if (hasNewAllTimeBest)
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
                        if (hasNewWeeklyBest)
                            UpdateLeaderboard(_weeklyBestScore);
                    }
                });
            }
            else
            {
                Backend.GameData.UpdateV2(TableName, _userIndate, Backend.UserInDate, param, (bro) =>
                {
                    if (bro.IsSuccess())
                    {
                        if (hasNewWeeklyBest)
                            UpdateLeaderboard(_weeklyBestScore);
                    }
                });
            }
        }
        else if (hasNewWeeklyBest)
        {
            if (!string.IsNullOrEmpty(_userIndate))
                UpdateLeaderboard(_weeklyBestScore);
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

    private void RefreshWeeklySeasonState()
    {
        string currentSeason = GetCurrentSeasonKey();
        string savedSeason = PlayerPrefs.GetString(WeeklySeasonKey, string.Empty);

        if (!string.Equals(savedSeason, currentSeason, StringComparison.Ordinal))
        {
            _weeklyBestScore = 0;
            PlayerPrefs.SetInt(WeeklyBestScoreKey, 0);
            PlayerPrefs.SetString(WeeklySeasonKey, currentSeason);
        }
        else
        {
            _weeklyBestScore = PlayerPrefs.GetInt(WeeklyBestScoreKey, 0);
        }
    }

    private string GetCurrentSeasonKey()
    {
        DateTime nowKst = DateTime.UtcNow.AddHours(9);
        BackendReturnObject serverTimeBro = Backend.Utils.GetServerTime();
        if (serverTimeBro.IsSuccess())
        {
            JsonData timeJson = serverTimeBro.GetReturnValuetoJSON();
            if (timeJson != null && timeJson.ContainsKey("utcTime"))
                nowKst = DateTime.Parse(timeJson["utcTime"].ToString()).AddHours(9);
        }

        DateTime weeklyResetAnchor = GetCurrentWeekResetDateTime(nowKst);
        if (nowKst < weeklyResetAnchor)
            weeklyResetAnchor = weeklyResetAnchor.AddDays(-7);

        return weeklyResetAnchor.ToString("yyyyMMddHH");
    }

    private void TrySyncAllTimeBestToServer()
    {
        if (!Backend.IsLogin || string.IsNullOrEmpty(_userIndate))
            return;

        Param param = new Param();
        param.Add(ScoreColumn, _bestScore);
        Backend.GameData.UpdateV2(TableName, _userIndate, Backend.UserInDate, param, _ => { });
    }

    private void TryUpdateWeeklyLeaderboardFromCache()
    {
        if (!Backend.IsLogin || string.IsNullOrEmpty(_userIndate))
            return;

        if (_weeklyBestScore <= 0)
            return;

        UpdateLeaderboard(_weeklyBestScore);
    }

    private void LoadWeeklyResetConfigFromChart()
    {
        BackendReturnObject chartBro = Backend.Chart.GetChartContents(GameConfigChartId);
        if (!chartBro.IsSuccess())
            return;

        JsonData rows = chartBro.FlattenRows();
        if (rows == null || rows.Count == 0)
            return;

        JsonData row = rows[0];
        int resetDay = _weeklyResetDay;
        int resetHour = _weeklyResetHour;

        if (TryGetIntFromJson(row, "ResetDay", out int parsedResetDay) && parsedResetDay >= 1 && parsedResetDay <= 7)
            resetDay = parsedResetDay;
        if (TryGetIntFromJson(row, "ResetHour", out int parsedResetHour) && parsedResetHour >= 0 && parsedResetHour <= 23)
            resetHour = parsedResetHour;

        _weeklyResetDay = resetDay;
        _weeklyResetHour = resetHour;
    }

    private DateTime GetCurrentWeekResetDateTime(DateTime nowKst)
    {
        int today = ToResetDayNumber(nowKst.DayOfWeek); // 1~7
        int dayOffset = (today - _weeklyResetDay + 7) % 7;
        return nowKst.Date.AddDays(-dayOffset).AddHours(_weeklyResetHour);
    }

    private static int ToResetDayNumber(DayOfWeek dayOfWeek)
    {
        return dayOfWeek == DayOfWeek.Sunday ? 7 : (int)dayOfWeek;
    }

    private static bool TryGetIntFromJson(JsonData data, string key, out int value)
    {
        value = 0;
        if (data == null || string.IsNullOrEmpty(key) || !data.ContainsKey(key))
            return false;

        return int.TryParse(data[key].ToString(), out value);
    }
}
