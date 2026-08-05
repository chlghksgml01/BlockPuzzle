using BackEnd;
using LitJson;
using System;
using UnityEngine;

/// <summary>
/// 레벨 클리어 진행도(최고 클리어 레벨)를 로컬/서버에 동기화한다.
/// </summary>
[DefaultExecutionOrder(-100)]
public class LevelProgressManager : Singleton<LevelProgressManager>
{
    private const string PrefsKey = "MaxClearedLevel";
    private const string TableName = "LEVEL_PROGRESS";
    private const string MaxClearedColumn = "maxClearedLevel";

    /// <summary>진행도가 로컬 또는 서버 병합으로 변경되었을 때.</summary>
    public static event Action OnProgressChanged;

    private int _maxClearedLevel;
    private string _userIndate = string.Empty;

    /// <summary>클리어 완료한 최고 레벨 번호 (1-base). 미클리어는 0.</summary>
    public int MaxClearedLevel => _maxClearedLevel;

    protected override void OnAwake()
    {
        _maxClearedLevel = Mathf.Max(0, PlayerPrefs.GetInt(PrefsKey, 0));
    }

    private void OnEnable()
    {
        GoogleLoginManager.OnLoginSucceed += SyncWithServer;
    }

    private void OnDisable()
    {
        GoogleLoginManager.OnLoginSucceed -= SyncWithServer;
    }

    /// <summary>
    /// 레벨 클리어를 반영한다. levelNumber는 1-base.
    /// 기존 최고보다 높을 때만 로컬 저장 후 서버에 동기화한다.
    /// </summary>
    public void NotifyLevelCleared(int levelNumber)
    {
        if (levelNumber <= 0 || levelNumber <= _maxClearedLevel)
            return;

        SetMaxClearedLevel(levelNumber, syncToServer: true);
    }

    /// <summary>
    /// maxClearedLevel 기준으로 MissionData.isClear를 재적용한다.
    /// isClear = (levelIndex &lt;= maxClearedLevel) → 완료 레벨 + 다음 플레이 가능 레벨 해금.
    /// </summary>
    /// <param name="overrideMaxClearedLevel">
    /// null이 아니면 실제 저장된 진행도 대신 이 값을 기준으로 적용한다.
    /// 에디터에서 ClearRoad 등 진행도 UI를 테스트할 때만 사용할 것.
    /// </param>
    public void ApplyToMissionTable(LevelMissionTableData table, int? overrideMaxClearedLevel = null)
    {
        if (table == null)
            return;

        int maxClearedLevel = overrideMaxClearedLevel ?? _maxClearedLevel;

        int levelCount = table.LevelCount;
        for (int i = 0; i < levelCount; i++)
        {
            MissionData mission = table.GetMission(i);
            if (mission == null)
                continue;

            mission.isClear = i <= maxClearedLevel;
        }
    }

    private void SyncWithServer(bool isSucceed)
    {
        if (!isSucceed)
            return;

        FetchGameData();
    }

    private void FetchGameData()
    {
        Backend.GameData.GetMyData(TableName, new Where(), bro =>
        {
            if (!bro.IsSuccess())
            {
                Debug.LogError($"[LevelProgressManager] GetMyData failed: {bro.GetErrorCode()} - {bro.GetMessage()}");
                return;
            }

            if (bro.FlattenRows().Count > 0)
            {
                JsonData row = bro.FlattenRows()[0];
                _userIndate = row["inDate"].ToString();

                int serverMax = 0;
                if (row.ContainsKey(MaxClearedColumn) && row[MaxClearedColumn] != null)
                    int.TryParse(row[MaxClearedColumn].ToString(), out serverMax);

                int merged = Mathf.Max(_maxClearedLevel, Mathf.Max(0, serverMax));
                SetMaxClearedLevel(merged, syncToServer: false);
                TrySyncProgressToServer();
            }
            else
            {
                CreateInitialServerData();
            }
        });
    }

    private void CreateInitialServerData()
    {
        Param param = new Param();
        param.Add(MaxClearedColumn, _maxClearedLevel);

        BackendReturnObject bro = Backend.GameData.Insert(TableName, param);
        if (bro.IsSuccess())
        {
            _userIndate = bro.GetInDate();
            Debug.Log($"[LevelProgressManager] Initial row created. maxClearedLevel={_maxClearedLevel}");
        }
        else
        {
            Debug.LogError($"[LevelProgressManager] Insert failed: {bro.GetErrorCode()} - {bro.GetMessage()}");
        }
    }

    private void SetMaxClearedLevel(int value, bool syncToServer)
    {
        int clamped = Mathf.Max(0, value);
        bool changed = clamped != _maxClearedLevel;

        _maxClearedLevel = clamped;
        PlayerPrefs.SetInt(PrefsKey, _maxClearedLevel);
        PlayerPrefs.Save();

        if (changed)
            OnProgressChanged?.Invoke();

        if (syncToServer)
            TrySyncProgressToServer();
    }

    private void TrySyncProgressToServer()
    {
        if (!Backend.IsLogin)
            return;

        Param param = new Param();
        param.Add(MaxClearedColumn, _maxClearedLevel);

        if (string.IsNullOrEmpty(_userIndate))
        {
            Backend.GameData.Insert(TableName, param, bro =>
            {
                if (bro.IsSuccess())
                {
                    _userIndate = bro.GetInDate();
                    Debug.Log($"[LevelProgressManager] Insert succeeded. maxClearedLevel={_maxClearedLevel}");
                }
                else
                {
                    Debug.LogError($"[LevelProgressManager] Insert failed: {bro.GetErrorCode()} - {bro.GetMessage()}");
                }
            });
            return;
        }

        Backend.GameData.UpdateV2(TableName, _userIndate, Backend.UserInDate, param, bro =>
        {
            if (bro.IsSuccess())
                Debug.Log($"[LevelProgressManager] UpdateV2 succeeded. maxClearedLevel={_maxClearedLevel}");
            else
                Debug.LogError($"[LevelProgressManager] UpdateV2 failed: {bro.GetErrorCode()} - {bro.GetMessage()}");
        });
    }
}
