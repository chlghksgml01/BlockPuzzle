using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// LevelInGame 씬의 미션 세션·진행도 총괄.
/// 수집 블록은 HUD로 비행한 뒤에만 카운트가 줄고, 그 다음에 결과 팝업이 뜬다.
/// </summary>
[DefaultExecutionOrder(-110)]
public class MissionManager : MonoBehaviour, IInitializable
{
    [Header("UI")]
    [Tooltip("미션 성공/실패 결과 팝업")]
    [SerializeField] private ResultPopupUI _resultPopup;

    [Tooltip("미션 HUD (비행 목표 아이콘 위치 제공)")]
    [SerializeField] private MissionHUD _missionHud;

    [Tooltip("수집 블록 → HUD 비행 연출")]
    [SerializeField] private MissionCollectFlyEffect _flyEffect;

    [Header("Test")]
    [Tooltip("연결 시 레벨맵 없이 LevelInGame 씬에서 바로 이 미션으로 테스트한다. 비우면 LevelSessionContext 사용.")]
    [SerializeField] private MissionData _testMissionData;

    [Tooltip("테스트 미션 HUD/결과에 표시할 레벨 번호 (1-base)")]
    [SerializeField] private int _testLevelNumber = 1;

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

    /// <summary>미션 실패(시간 초과·게임오버 등) 시.</summary>
    public static event Action OnMissionFailed;

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
    private bool _resultResolved;

    private int _pendingCollectFlyCount;
    private readonly Dictionary<GemType, int> _pendingGemFlyCounts = new Dictionary<GemType, int>();

    private Coroutine _timerCoroutine;
    private Coroutine _grassSpreadSyncCoroutine;
    private bool _subscriptionsBound;
    private bool _usingTestMission;

    /// <summary>레벨 세션이 유효하면 true (미션 에셋 누락과 무관).</summary>
    public bool IsActive => _currentMission != null && (_missionTable != null || _usingTestMission);

    /// <summary>인스펙터 테스트 미션으로 플레이 중이면 true.</summary>
    public bool IsUsingTestMission => _usingTestMission;

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

    /// <summary>Ice/Grass 미션의 남은 블록 수 (비행 중인 것 포함 표시값).</summary>
    public int RemainingCollectCount => _remainingCollectCount;

    /// <summary>Gem 미션의 종류별 남은 개수 (비행 중인 것 포함 표시값).</summary>
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

    /// <summary>결과(성공/실패)가 이미 확정되었으면 true.</summary>
    public bool IsResultResolved => _resultResolved;

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
    /// LevelSessionContext 또는 테스트 MissionData를 캐시한다.
    /// 둘 다 없으면 내부 상태를 비운다.
    /// </summary>
    public void BindFromSession()
    {
        if (LevelSessionContext.IsActive)
        {
            _usingTestMission = false;
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
        }
        else if (_testMissionData != null)
        {
            _usingTestMission = true;
            _currentLevelIndex = Mathf.Max(0, _testLevelNumber - 1);
            _missionTable = null;
            _currentMission = _testMissionData;
        }
        else
        {
            ResetState();
            return;
        }

        ClearPendingFlies();
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
        _resultResolved = false;
        ClearPendingFlies();
        _isTracking = true;

        RefreshDisplayedRemaining();

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

        if (_grassSpreadSyncCoroutine != null)
        {
            StopCoroutine(_grassSpreadSyncCoroutine);
            _grassSpreadSyncCoroutine = null;
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

    /// <summary>보드 잔여 + 비행 중인 수집을 합쳐 HUD 표시값을 만든다.</summary>
    private void RefreshDisplayedRemaining()
    {
        if (_boardManager == null || _currentMission == null)
            return;

        switch (CurrentMissionType)
        {
            case MissionType.Ice:
                _remainingCollectCount = _boardManager.CountIceCells() + _pendingCollectFlyCount;
                break;
            case MissionType.Grass:
                _remainingCollectCount = _boardManager.CountGrassCells() + _pendingCollectFlyCount;
                break;
            case MissionType.Gem:
                RefreshGemDisplayedRemaining();
                break;
        }
    }

    private void RefreshGemDisplayedRemaining()
    {
        _remainingGems.Clear();
        List<GemTargetInfo> targets = _currentMission.BuildGemTargets();
        for (int i = 0; i < targets.Count; i++)
        {
            GemTargetInfo target = targets[i];
            int boardCount = _boardManager.CountGemCells(target.gemType);
            int pending = 0;
            _pendingGemFlyCounts.TryGetValue(target.gemType, out pending);

            _remainingGems.Add(new GemTargetInfo
            {
                gemType = target.gemType,
                count = boardCount + pending
            });
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

        if (_boardManager != null)
            _boardManager.OnMissionCollectibleRemoved += HandleMissionCollectibleRemoved;

        _subscriptionsBound = true;
    }

    private void UnbindSubscriptions()
    {
        if (!_subscriptionsBound)
            return;

        InGameManager.OnBlockSettled -= HandleBlockSettled;
        if (_scoreSystem != null)
            _scoreSystem.OnScoreChanged -= HandleScoreChanged;

        if (_boardManager != null)
            _boardManager.OnMissionCollectibleRemoved -= HandleMissionCollectibleRemoved;

        _subscriptionsBound = false;
    }

    private void HandleBlockSettled(int blockShapeCount)
    {
        if (!_isTracking)
            return;

        // grass 전파로 개수가 늘어날 수 있으므로, 비행이 없을 때만 보드와 동기화
        if (CurrentMissionType == MissionType.Grass)
            ScheduleGrassSpreadSync();
    }

    private void HandleScoreChanged(int previousScore, int newScore)
    {
        if (!_isTracking || CurrentMissionType != MissionType.ScoreGoal)
            return;

        RaiseProgressChanged();
        EvaluateObjective();
    }

    private void HandleMissionCollectibleRemoved(MissionCollectInfo info)
    {
        if (!_isTracking || _resultResolved)
            return;

        if (!IsRelevantCollect(info))
            return;

        AddPendingFly(info);

        // pending을 올려 표시값은 유지한 채 비행 시작
        RefreshDisplayedRemaining();

        Vector3 targetWorld = ResolveHudTarget(info);
        if (_flyEffect != null)
        {
            _flyEffect.Play(info.Sprite, info.WorldPosition, targetWorld, () => OnCollectFlyCompleted(info));
        }
        else
        {
            OnCollectFlyCompleted(info);
        }
    }

    private bool IsRelevantCollect(MissionCollectInfo info)
    {
        switch (CurrentMissionType)
        {
            case MissionType.Ice:
                return info.CollectType == MissionType.Ice;
            case MissionType.Grass:
                return info.CollectType == MissionType.Grass;
            case MissionType.Gem:
                return info.CollectType == MissionType.Gem;
            default:
                return false;
        }
    }

    private void AddPendingFly(MissionCollectInfo info)
    {
        if (info.CollectType == MissionType.Gem)
        {
            int count = 0;
            _pendingGemFlyCounts.TryGetValue(info.GemType, out count);
            _pendingGemFlyCounts[info.GemType] = count + 1;
            return;
        }

        _pendingCollectFlyCount++;
    }

    private void RemovePendingFly(MissionCollectInfo info)
    {
        if (info.CollectType == MissionType.Gem)
        {
            if (!_pendingGemFlyCounts.TryGetValue(info.GemType, out int count))
                return;

            count--;
            if (count <= 0)
                _pendingGemFlyCounts.Remove(info.GemType);
            else
                _pendingGemFlyCounts[info.GemType] = count;
            return;
        }

        _pendingCollectFlyCount = Mathf.Max(0, _pendingCollectFlyCount - 1);
    }

    private int GetTotalPendingFlies()
    {
        int total = _pendingCollectFlyCount;
        foreach (KeyValuePair<GemType, int> pair in _pendingGemFlyCounts)
            total += pair.Value;
        return total;
    }

    private void OnCollectFlyCompleted(MissionCollectInfo info)
    {
        if (_resultResolved)
            return;

        RemovePendingFly(info);
        RefreshDisplayedRemaining();
        RaiseProgressChanged();

        if (GetTotalPendingFlies() <= 0)
            EvaluateObjective();
    }

    private Vector3 ResolveHudTarget(MissionCollectInfo info)
    {
        if (_missionHud == null)
            return info.WorldPosition + Vector3.up * 2f;

        if (info.CollectType == MissionType.Gem)
        {
            if (_missionHud.TryGetGemIconWorldPosition(info.GemType, out Vector3 gemPos))
                return gemPos;
        }
        else if (_missionHud.TryGetCollectIconWorldPosition(out Vector3 collectPos))
        {
            return collectPos;
        }

        return info.WorldPosition + Vector3.up * 2f;
    }

    private void ScheduleGrassSpreadSync()
    {
        if (_grassSpreadSyncCoroutine != null)
            StopCoroutine(_grassSpreadSyncCoroutine);

        _grassSpreadSyncCoroutine = StartCoroutine(GrassSpreadSyncCoroutine());
    }

    private IEnumerator GrassSpreadSyncCoroutine()
    {
        yield return null;

        if (!_isTracking || _pendingCollectFlyCount > 0)
        {
            _grassSpreadSyncCoroutine = null;
            yield break;
        }

        RefreshDisplayedRemaining();
        RaiseProgressChanged();
        _grassSpreadSyncCoroutine = null;
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
                FailMission();
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

        if (GetTotalPendingFlies() > 0)
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

        ResolveSuccess();
    }

    private void ResolveSuccess()
    {
        if (_resultResolved || _objectiveCompleted)
            return;

        _objectiveCompleted = true;
        _resultResolved = true;
        UnlockNextLevel();
        StopProgressTracking();
        OnObjectiveCompleted?.Invoke();
        TryShowResultPopup(success: true);
    }

    /// <summary>블록을 더 이상 배치할 수 없을 때 등 외부에서 미션 실패를 알린다.</summary>
    public void FailMission()
    {
        if (!IsActive || _resultResolved)
            return;

        _resultResolved = true;
        StopProgressTracking();
        OnMissionFailed?.Invoke();
        TryShowResultPopup(success: false);
    }

    private void TryShowResultPopup(bool success)
    {
        if (_resultPopup == null)
        {
            Debug.LogWarning("[MissionManager] ResultPopupUI가 연결되지 않았습니다.", this);
            return;
        }

        _resultPopup.ShowResult(success);
    }

    private void UnlockNextLevel()
    {
        if (_missionTable == null || _currentLevelIndex < 0)
            return;

        MissionData nextMission = _missionTable.GetMission(_currentLevelIndex + 1);
        if (nextMission == null)
            return;

        nextMission.isClear = true;
    }

    private bool AreAllGemsCleared()
    {
        if (_remainingGems.Count == 0)
            return false;

        for (int i = 0; i < _remainingGems.Count; i++)
        {
            if (_remainingGems[i].count > 0)
                return false;
        }

        return true;
    }

    private void RaiseProgressChanged()
    {
        OnProgressChanged?.Invoke();
    }

    private void RaiseTimeChanged()
    {
        OnTimeChanged?.Invoke();
    }

    private void ClearPendingFlies()
    {
        _pendingCollectFlyCount = 0;
        _pendingGemFlyCounts.Clear();
    }

    private void ResetState()
    {
        _currentLevelIndex = -1;
        _missionTable = null;
        _currentMission = null;
        _usingTestMission = false;
        _remainingCollectCount = 0;
        _remainingGems.Clear();
        _remainingTimeSeconds = 0f;
        _objectiveCompleted = false;
        _timeExpired = false;
        _resultResolved = false;
        ClearPendingFlies();
    }
}
