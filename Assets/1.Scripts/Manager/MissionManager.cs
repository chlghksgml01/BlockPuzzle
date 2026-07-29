using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// LevelInGame 씬의 미션 세션·진행도 총괄.
/// LevelSessionContext에서 전달된 레벨/미션을 캐시하고, 남은 목표/시간을 추적한다.
/// UI는 MissionHUD가 이벤트를 구독해 표시한다.
/// </summary>
[DefaultExecutionOrder(-110)]
public class MissionManager : MonoBehaviour, IInitializable
{
    private const float StageClearSyncDelaySeconds = 0.45f;

    /// <summary>씬 내 단일 인스턴스. 없으면 null.</summary>
    public static MissionManager Instance { get; private set; }

    /// <summary>레벨/미션이 바인딩되었을 때.</summary>
    public static event Action OnMissionBound;

    /// <summary>세션이 해제되었을 때.</summary>
    public static event Action OnMissionCleared;

    /// <summary>남은 목표/점수 등 진행도가 바뀌었을 때.</summary>
    public static event Action OnProgressChanged;

    /// <summary>남은 시간이 바뀌었을 때 (표시용).</summary>
    public static event Action OnTimeChanged;

    /// <summary>목표를 모두 달성했을 때.</summary>
    public static event Action OnObjectiveCompleted;

    /// <summary>제한 시간 소진 시.</summary>
    public static event Action OnTimeExpired;

    private int _currentLevelIndex = -1;
    private LevelMissionTableData _missionTable;
    private MissionData _currentMission;

    private BoardManager _boardManager;
    private ScoreSystem _scoreSystem;

    private int _remainingCollectCount;
    private readonly List<GemTargetInfo> _remainingGems = new List<GemTargetInfo>(3);
    private float _remainingTimeSeconds;
    private bool _isTracking;
    private bool _objectiveCompleted;
    private bool _timeExpired;

    private Coroutine _timerCoroutine;
    private Coroutine _boardSyncCoroutine;
    private bool _subscriptionsBound;

    /// <summary>레벨 세션이 유효하면 true (미션 에셋 누락과 무관).</summary>
    public bool IsActive => _currentLevelIndex >= 0 && _missionTable != null;

    /// <summary>현재 레벨 인덱스 (0-base). 비활성 시 -1.</summary>
    public int CurrentLevelIndex => _currentLevelIndex;

    /// <summary>현재 레벨 번호 (1-base). 비활성 시 0.</summary>
    public int CurrentLevelNumber => IsActive ? _currentLevelIndex + 1 : 0;

    /// <summary>현재 미션 데이터. 비활성 시 null.</summary>
    public MissionData CurrentMission => _currentMission;

    /// <summary>현재 미션 종류. 비활성 시 None.</summary>
    public MissionType CurrentMissionType =>
        _currentMission != null ? _currentMission.MissionType : MissionType.None;

    /// <summary>레벨 미션 테이블. 비활성 시 null.</summary>
    public LevelMissionTableData MissionTable => _missionTable;

    /// <summary>Ice/Grass 미션의 남은 블록 수.</summary>
    public int RemainingCollectCount => _remainingCollectCount;

    /// <summary>Gem 미션의 종류별 남은 개수.</summary>
    public IReadOnlyList<GemTargetInfo> RemainingGems => _remainingGems;

    /// <summary>ScoreGoal 미션의 남은 시간(초).</summary>
    public float RemainingTimeSeconds => _remainingTimeSeconds;

    /// <summary>ScoreGoal 목표 점수.</summary>
    public int TargetScore => _currentMission != null ? _currentMission.TargetScore : 0;

    /// <summary>현재 점수 (ScoreSystem).</summary>
    public int CurrentScore => _scoreSystem != null ? _scoreSystem.CurrentScore : 0;

    /// <summary>제한 시간이 소진되었으면 true.</summary>
    public bool IsTimeExpired => _timeExpired;

    /// <summary>목표를 달성했으면 true.</summary>
    public bool IsObjectiveCompleted => _objectiveCompleted;

    public void Initialize(InitializeContext context)
    {
        _scoreSystem = context.ScoreSystem;
        _boardManager = context.BoardManager;

        if (!IsActive)
            BindFromSession();

        TryBindSubscriptions();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BindFromSession();
    }

    private void OnEnable()
    {
        TryBindSubscriptions();
    }

    private void OnDisable()
    {
        UnbindSubscriptions();
        StopProgressTracking();
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        Instance = null;
    }

    /// <summary>
    /// LevelSessionContext의 선택 레벨/테이블을 캐시한다.
    /// 컨텍스트가 비활성이면 내부 상태를 비운다.
    /// </summary>
    public void BindFromSession()
    {
        if (!LevelSessionContext.IsActive)
        {
            ResetState();
            return;
        }

        _currentLevelIndex = LevelSessionContext.SelectedLevelIndex;
        _missionTable = LevelSessionContext.GetMissionTable();
        _currentMission = _missionTable != null
            ? _missionTable.GetMission(_currentLevelIndex)
            : null;

        if (_currentMission == null)
        {
            Debug.LogWarning(
                $"[MissionManager] 레벨 {_currentLevelIndex}에 MissionData가 없습니다.",
                this);
        }

        ResetProgressValuesFromMissionData();
        OnMissionBound?.Invoke();
        RaiseProgressChanged();
    }

    /// <summary>인트로/레이아웃 적용 후 호출. 보드 기준으로 진행도를 맞추고 타이머를 시작한다.</summary>
    public void BeginProgressTracking()
    {
        if (!IsActive || _currentMission == null)
            return;

        StopProgressTracking();
        _objectiveCompleted = false;
        _timeExpired = false;
        _isTracking = true;

        SyncProgressFromBoard();

        if (CurrentMissionType == MissionType.ScoreGoal)
        {
            _remainingTimeSeconds = Mathf.Max(0f, _currentMission.TimeLimitSeconds);
            RaiseTimeChanged();
            _timerCoroutine = StartCoroutine(TimerCoroutine());
        }

        TryBindSubscriptions();
        RaiseProgressChanged();
        EvaluateObjective();
    }

    /// <summary>진행 추적·타이머를 중단한다.</summary>
    public void StopProgressTracking()
    {
        _isTracking = false;

        if (_timerCoroutine != null)
        {
            StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;
        }

        if (_boardSyncCoroutine != null)
        {
            StopCoroutine(_boardSyncCoroutine);
            _boardSyncCoroutine = null;
        }
    }

    /// <summary>런타임 세션과 LevelSessionContext를 함께 해제한다.</summary>
    public void ClearSession()
    {
        StopProgressTracking();
        ResetState();
        LevelSessionContext.Clear();
        OnMissionCleared?.Invoke();
        RaiseProgressChanged();
    }

    /// <summary>보드 점유 상태를 읽어 Ice/Grass/Gem 남은 수를 갱신한다.</summary>
    public void SyncProgressFromBoard()
    {
        if (_boardManager == null || _currentMission == null)
            return;

        switch (CurrentMissionType)
        {
            case MissionType.Ice:
                _remainingCollectCount = _boardManager.CountIceCells();
                break;
            case MissionType.Grass:
                _remainingCollectCount = _boardManager.CountGrassCells();
                break;
            case MissionType.Gem:
                SyncGemRemainingFromBoard();
                break;
        }
    }

    private void SyncGemRemainingFromBoard()
    {
        _remainingGems.Clear();
        List<GemTargetInfo> targets = _currentMission.BuildGemTargets();
        for (int i = 0; i < targets.Count; i++)
        {
            GemTargetInfo target = targets[i];
            _remainingGems.Add(new GemTargetInfo
            {
                gemType = target.gemType,
                count = _boardManager.CountGemCells(target.gemType)
            });
        }
    }

    private void ResetProgressValuesFromMissionData()
    {
        _remainingCollectCount = 0;
        _remainingGems.Clear();
        _remainingTimeSeconds = 0f;
        _objectiveCompleted = false;
        _timeExpired = false;

        if (_currentMission == null)
            return;

        switch (_currentMission.MissionType)
        {
            case MissionType.Ice:
                _remainingCollectCount = _currentMission.CountIceCells();
                break;
            case MissionType.Grass:
                _remainingCollectCount = _currentMission.CountGrassCells();
                break;
            case MissionType.Gem:
                _remainingGems.AddRange(_currentMission.BuildGemTargets());
                break;
            case MissionType.ScoreGoal:
                _remainingTimeSeconds = Mathf.Max(0f, _currentMission.TimeLimitSeconds);
                break;
        }
    }

    private void TryBindSubscriptions()
    {
        if (_subscriptionsBound)
            return;

        if (_scoreSystem == null)
            return;

        InGameManager.OnBlockSettled += HandleBlockSettled;
        _scoreSystem.OnScoreChanged += HandleScoreChanged;
        _subscriptionsBound = true;
    }

    private void UnbindSubscriptions()
    {
        if (!_subscriptionsBound)
            return;

        InGameManager.OnBlockSettled -= HandleBlockSettled;
        if (_scoreSystem != null)
            _scoreSystem.OnScoreChanged -= HandleScoreChanged;

        _subscriptionsBound = false;
    }

    private void HandleBlockSettled(int blockShapeCount)
    {
        if (!_isTracking)
            return;

        if (CurrentMissionType == MissionType.Ice ||
            CurrentMissionType == MissionType.Grass ||
            CurrentMissionType == MissionType.Gem)
        {
            ScheduleBoardProgressSync();
        }
    }

    private void HandleScoreChanged(int previousScore, int newScore)
    {
        if (!_isTracking || CurrentMissionType != MissionType.ScoreGoal)
            return;

        RaiseProgressChanged();
        EvaluateObjective();
    }

    private void ScheduleBoardProgressSync()
    {
        if (_boardSyncCoroutine != null)
            StopCoroutine(_boardSyncCoroutine);

        _boardSyncCoroutine = StartCoroutine(BoardProgressSyncCoroutine());
    }

    private IEnumerator BoardProgressSyncCoroutine()
    {
        // 같은 프레임의 라인클리어·grass 전파 핸들러 이후 반영
        yield return null;
        SyncProgressFromBoard();
        RaiseProgressChanged();
        EvaluateObjective();

        // ice/grass 단계 제거 DOTween 이후 재동기화
        yield return new WaitForSeconds(StageClearSyncDelaySeconds);
        SyncProgressFromBoard();
        RaiseProgressChanged();
        EvaluateObjective();

        _boardSyncCoroutine = null;
    }

    private IEnumerator TimerCoroutine()
    {
        while (_isTracking && _remainingTimeSeconds > 0f)
        {
            yield return null;
            _remainingTimeSeconds -= Time.deltaTime;
            if (_remainingTimeSeconds < 0f)
                _remainingTimeSeconds = 0f;

            RaiseTimeChanged();

            if (_remainingTimeSeconds <= 0f)
            {
                _timeExpired = true;
                OnTimeExpired?.Invoke();
                break;
            }
        }

        _timerCoroutine = null;
    }

    private void EvaluateObjective()
    {
        if (!_isTracking || _objectiveCompleted || _currentMission == null)
            return;

        bool completed = false;
        switch (CurrentMissionType)
        {
            case MissionType.Ice:
            case MissionType.Grass:
                completed = _remainingCollectCount <= 0;
                break;
            case MissionType.Gem:
                completed = AreAllGemsCleared();
                break;
            case MissionType.ScoreGoal:
                completed = CurrentScore >= TargetScore;
                break;
        }

        if (!completed)
            return;

        _objectiveCompleted = true;
        OnObjectiveCompleted?.Invoke();
    }

    private bool AreAllGemsCleared()
    {
        for (int i = 0; i < _remainingGems.Count; i++)
        {
            if (_remainingGems[i].count > 0)
                return false;
        }

        return _remainingGems.Count > 0;
    }

    private void RaiseProgressChanged()
    {
        OnProgressChanged?.Invoke();
    }

    private void RaiseTimeChanged()
    {
        OnTimeChanged?.Invoke();
    }

    private void ResetState()
    {
        _currentLevelIndex = -1;
        _missionTable = null;
        _currentMission = null;
        _remainingCollectCount = 0;
        _remainingGems.Clear();
        _remainingTimeSeconds = 0f;
        _objectiveCompleted = false;
        _timeExpired = false;
    }
}
