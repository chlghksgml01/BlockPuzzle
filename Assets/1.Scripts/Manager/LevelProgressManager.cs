using BackEnd;
using LitJson;
using System;
using UnityEngine;

/// <summary>
/// 레벨 진행도(현재 플레이 레벨)를 로컬/서버에 동기화한다.
/// </summary>
[DefaultExecutionOrder(-100)]
public class LevelProgressManager : Singleton<LevelProgressManager>
{
    private const string PrefsKey = "CurrentLevel";
    private const string TableName = "LEVEL_PROGRESS";
    private const string CurrentLevelColumn = "CurrentLevel";
    private const int DefaultCurrentLevel = 1;

    /// <summary>진행도가 로컬 또는 서버 병합으로 변경되었을 때.</summary>
    public static event Action OnProgressChanged;

    [Header("Debug/Test")]
    [Tooltip("체크하면 실제 저장된 진행도 대신 아래 Current Level을 사용한다. 테스트 종료 후 반드시 해제할 것")]
    [SerializeField] private bool _useDebugCurrentLevel = false;

    [Tooltip("테스트용 현재 플레이 레벨 (1-base). N 입력 시 1~(N-1) 클리어, N이 현재 위치. _useDebugCurrentLevel이 체크된 경우에만 사용됨")]
    [SerializeField] private int _debugCurrentLevel = 1;

    private int _currentLevel;
    private string _userIndate = string.Empty;

    /// <summary>현재 플레이 레벨 번호 (1-base). 미진행은 1. Debug 오버라이드는 포함하지 않는다.</summary>
    public int CurrentLevel => _currentLevel;

    protected override void OnAwake()
    {
        _currentLevel = Mathf.Max(DefaultCurrentLevel, PlayerPrefs.GetInt(PrefsKey, DefaultCurrentLevel));
    }

    private void OnEnable()
    {
        GoogleLoginManager.OnLoginSucceed += SyncWithServer;
    }

    private void OnDisable()
    {
        GoogleLoginManager.OnLoginSucceed -= SyncWithServer;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 플레이 모드에서 인스펙터 Debug/Test 값을 바꿀 때 UI(ClearRoad 등)를 즉시 갱신한다.
    /// </summary>
    private void OnValidate()
    {
        if (!Application.isPlaying || !HasInstance || Instance != this)
            return;

        OnProgressChanged?.Invoke();
    }
#endif

    /// <summary>
    /// 레벨 클리어를 반영한다. levelNumber는 1-base.
    /// 클리어한 다음 레벨이 현재보다 앞설 때만 로컬 저장 후 서버에 동기화한다.
    /// </summary>
    public void NotifyLevelCleared(int levelNumber)
    {
        if (levelNumber <= 0)
            return;

        int nextLevel = levelNumber + 1;
        if (nextLevel <= _currentLevel)
            return;

        SetCurrentLevel(nextLevel, syncToServer: true);
    }

    /// <summary>
    /// 해금 판정에 쓸 currentLevel (1-base).
    /// Debug ON이면 인스펙터 Current Level, OFF면 실제 저장값.
    /// 레벨 인덱스 i는 i &lt; EffectiveCurrentLevel 이면 해금(완료 + 현재 플레이 가능).
    /// </summary>
    public int GetEffectiveCurrentLevel()
    {
        if (!_useDebugCurrentLevel)
            return _currentLevel;

        return Mathf.Max(DefaultCurrentLevel, _debugCurrentLevel);
    }

    /// <summary>인스턴스가 없으면 기본값(1). 있으면 GetEffectiveCurrentLevel.</summary>
    public static int PeekEffectiveCurrentLevel()
    {
        if (!HasInstance)
            return DefaultCurrentLevel;

        return Instance.GetEffectiveCurrentLevel();
    }

    /// <summary>levelIndex(0-base)가 해금되었는지. 인스턴스가 없으면 기본 진행도(레벨 1) 기준.</summary>
    public static bool IsLevelUnlocked(int levelIndex)
    {
        if (levelIndex < 0)
            return false;

        return levelIndex < PeekEffectiveCurrentLevel();
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

                int serverCurrentLevel = DefaultCurrentLevel;
                if (row.ContainsKey(CurrentLevelColumn) && row[CurrentLevelColumn] != null)
                    int.TryParse(row[CurrentLevelColumn].ToString(), out serverCurrentLevel);

                int merged = Mathf.Max(_currentLevel, Mathf.Max(DefaultCurrentLevel, serverCurrentLevel));
                SetCurrentLevel(merged, syncToServer: false);
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
        param.Add(CurrentLevelColumn, _currentLevel);

        BackendReturnObject bro = Backend.GameData.Insert(TableName, param);
        if (bro.IsSuccess())
        {
            _userIndate = bro.GetInDate();
            Debug.Log($"[LevelProgressManager] Initial row created. currentLevel={_currentLevel}");
        }
        else
        {
            Debug.LogError($"[LevelProgressManager] Insert failed: {bro.GetErrorCode()} - {bro.GetMessage()}");
        }
    }

    private void SetCurrentLevel(int value, bool syncToServer)
    {
        int clamped = Mathf.Max(DefaultCurrentLevel, value);
        bool changed = clamped != _currentLevel;

        _currentLevel = clamped;
        PlayerPrefs.SetInt(PrefsKey, _currentLevel);
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
        param.Add(CurrentLevelColumn, _currentLevel);

        if (string.IsNullOrEmpty(_userIndate))
        {
            Backend.GameData.Insert(TableName, param, bro =>
            {
                if (bro.IsSuccess())
                {
                    _userIndate = bro.GetInDate();
                    Debug.Log($"[LevelProgressManager] Insert succeeded. currentLevel={_currentLevel}");
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
                Debug.Log($"[LevelProgressManager] UpdateV2 succeeded. currentLevel={_currentLevel}");
            else
                Debug.LogError($"[LevelProgressManager] UpdateV2 failed: {bro.GetErrorCode()} - {bro.GetMessage()}");
        });
    }
}
