using BackEnd;
using LitJson;
using System;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class LeaderboardManager : Singleton<LeaderboardManager>, IInitializable
{
    private const string RankUuid = "019dd363-ac5b-7aa6-9309-b0cca0839e5d";
    private const string GameConfigChartId = "236370";
    private const string BestScoreKey = "BestScore";
    private const string WeeklyBestScoreKey = "WeeklyBestScore";
    private const string WeeklySeasonKey = "WeeklySeasonKey";
    public const string LocalNicknameKey = "LocalNickname";
    private const string TableName = "BEST_SCORE";
    private const string ScoreColumn = "bestscore";
    private const string WeeklyScoreColumn = "weeklyBestScore";
    private const string WeeklySeasonColumn = "weeklySeasonKey";

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
                    JsonData row = bro.FlattenRows()[0];
                    _userIndate = row["inDate"].ToString();
                    int serverScore = int.Parse(row[ScoreColumn].ToString());
                    int serverWeeklyScore = TryGetIntFromJson(row, WeeklyScoreColumn, out int parsedWeeklyScore) ? parsedWeeklyScore : 0;

                    string currentSeason = GetCurrentSeasonKey();
                    string serverWeeklySeason = TryGetStringFromJson(row, WeeklySeasonColumn, out string parsedWeeklySeason)
                        ? parsedWeeklySeason
                        : string.Empty;

                    _bestScore = Mathf.Max(_bestScore, serverScore);
                    PlayerPrefs.SetInt(BestScoreKey, _bestScore);
                    _weeklyBestScore = string.Equals(serverWeeklySeason, currentSeason, StringComparison.Ordinal)
                        ? Mathf.Max(_weeklyBestScore, serverWeeklyScore)
                        : _weeklyBestScore;
                    PlayerPrefs.SetInt(WeeklyBestScoreKey, _weeklyBestScore);
                    PlayerPrefs.SetString(WeeklySeasonKey, currentSeason);

                    TrySyncScoreDataToServer();
                    TrySyncWeeklyLeaderboard();
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
        Debug.Log("Creating initial server data with score: " + _bestScore);
        Param param = new Param();
        param.Add(ScoreColumn, _bestScore);
        param.Add(WeeklyScoreColumn, _weeklyBestScore);
        param.Add(WeeklySeasonColumn, GetCurrentSeasonKey());

        var bro = Backend.GameData.Insert(TableName, param);
        if (bro.IsSuccess())
        {
            _userIndate = bro.GetInDate();
            TrySyncWeeklyLeaderboard();
        }
    }

    public void UpdateBestScore(int newScore)
    {
        bool hasNewAllTimeBest = newScore > _bestScore;
        bool hasNewWeeklyBest = newScore > _weeklyBestScore;

        if (!hasNewAllTimeBest && !hasNewWeeklyBest)
            return;

        if (hasNewAllTimeBest)
        {
            _bestScore = newScore;
            PlayerPrefs.SetInt(BestScoreKey, _bestScore);
        }

        if (hasNewWeeklyBest)
        {
            _weeklyBestScore = newScore;
            PlayerPrefs.SetInt(WeeklyBestScoreKey, _weeklyBestScore);
        }

        if (!Backend.IsLogin)
            return;

        if (hasNewAllTimeBest)
            SyncAllTimeBestToServer(hasNewWeeklyBest);
        else
            SyncWeeklyBestToServer();
    }

    private void SyncAllTimeBestToServer(bool shouldUpdateLeaderboard)
    {
        Param param = new Param();
        param.Add(ScoreColumn, _bestScore);
        param.Add(WeeklyScoreColumn, _weeklyBestScore);
        param.Add(WeeklySeasonColumn, GetCurrentSeasonKey());

        if (string.IsNullOrEmpty(_userIndate))
        {
            Backend.GameData.Insert(TableName, param, (bro) =>
            {
                if (bro.IsSuccess())
                {
                    _userIndate = bro.GetInDate();
                    if (shouldUpdateLeaderboard)
                        UpdateLeaderboard(_weeklyBestScore);
                }
            });
        }
        else
        {
            Backend.GameData.UpdateV2(TableName, _userIndate, Backend.UserInDate, param, (bro) =>
            {
                if (bro.IsSuccess() && shouldUpdateLeaderboard)
                    UpdateLeaderboard(_weeklyBestScore);
            });
        }
    }

    private void SyncWeeklyBestToServer()
    {
        if (string.IsNullOrEmpty(_userIndate))
            return;

        Param param = new Param();
        param.Add(WeeklyScoreColumn, _weeklyBestScore);
        param.Add(WeeklySeasonColumn, GetCurrentSeasonKey());
        Backend.GameData.UpdateV2(TableName, _userIndate, Backend.UserInDate, param, (bro) =>
        {
            if (bro.IsSuccess())
                UpdateLeaderboard(_weeklyBestScore);
        });
    }

    private void UpdateLeaderboard(int score)
    {
        Debug.Log("Updating leaderboard with score: " + score);
        Param param = new Param();
        param.Add(WeeklyScoreColumn, score);

        Backend.URank.User.UpdateUserScore(RankUuid, TableName, _userIndate, param, (bro) =>
        {
            if (bro.IsSuccess())
            {
                Debug.Log($"Leaderboard updated successfully: {score}");
            }
            else
            {
                Debug.LogError($"Leaderboard update failed: {bro.GetErrorCode()} - {bro.GetMessage()}");
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

    private void TrySyncScoreDataToServer()
    {
        if (!Backend.IsLogin || string.IsNullOrEmpty(_userIndate))
            return;

        Param param = new Param();
        param.Add(ScoreColumn, _bestScore);
        param.Add(WeeklyScoreColumn, _weeklyBestScore);
        param.Add(WeeklySeasonColumn, GetCurrentSeasonKey());
        Debug.Log($"[TrySyncScoreDataToServer] UpdateV2 호출 - inDate: {_userIndate}");
        Backend.GameData.UpdateV2(TableName, _userIndate, Backend.UserInDate, param, (bro) =>
        {
            if (bro.IsSuccess())
                Debug.Log("[TrySyncScoreDataToServer] Succeeded");
            else
                Debug.LogError($"[TrySyncScoreDataToServer] Failed: {bro.GetErrorCode()} - {bro.GetMessage()}");
        });
    }

    private void TrySyncWeeklyLeaderboard()
    {
        if (!Backend.IsLogin || string.IsNullOrEmpty(_userIndate))
        {
            Debug.LogWarning("Cannot sync weekly leaderboard: user not logged in or userIndate is empty");
            return;
        }

        if (_weeklyBestScore <= 0)
        {
            Debug.Log("Skipping weekly leaderboard sync");
            GetRank();
            return;
        }

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
        int today = ToResetDayNumber(nowKst.DayOfWeek);
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

    private static bool TryGetStringFromJson(JsonData data, string key, out string value)
    {
        value = string.Empty;
        if (data == null || string.IsNullOrEmpty(key) || !data.ContainsKey(key) || data[key] == null)
            return false;

        value = data[key].ToString();
        return !string.IsNullOrEmpty(value);
    }
}
