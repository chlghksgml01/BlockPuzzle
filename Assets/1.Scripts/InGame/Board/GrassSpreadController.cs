using UnityEngine;

/// <summary>
/// Grass 미션에서만 동작한다.
/// grass 포함 줄 클리어에 실패가 연속되면, 기존 grass 인접 칸으로 전파한다.
/// </summary>
[DefaultExecutionOrder(-80)]
[RequireComponent(typeof(BoardManager))]
[RequireComponent(typeof(MissionBoardController))]
public sealed class GrassSpreadController : MonoBehaviour
{
    [Header("Spread Settings")]
    [Tooltip("grass가 포함된 줄을 클리어하지 못한 연속 횟수. 이 값에 도달하면 전파한다.")]
    [SerializeField, Min(1)] private int _missedClearsBeforeSpread = 3;

    [Tooltip("전파된 grass 등장 DoTween 시간")]
    [SerializeField, Min(0f)] private float _spreadAppearDuration = 0.28f;

    private BoardManager _boardManager;
    private MissionBoardController _missionBoardController;
    private int _missedGrassLineClears;

    private void Awake()
    {
        _boardManager = GetComponent<BoardManager>();
        _missionBoardController = GetComponent<MissionBoardController>();
    }

    private void OnEnable()
    {
        InGameManager.OnBlockSettled += HandleBlockSettled;
        InGameManager.OnResetGame += ResetMissCounter;
    }

    private void OnDisable()
    {
        InGameManager.OnBlockSettled -= HandleBlockSettled;
        InGameManager.OnResetGame -= ResetMissCounter;
    }

    /// <summary>미션 레이아웃 적용 직후 카운터를 초기화한다.</summary>
    public void ResetMissCounter()
    {
        _missedGrassLineClears = 0;
    }

    private void HandleBlockSettled(int blockShapeCount)
    {
        if (_boardManager == null || _missionBoardController == null)
            return;

        if (_missionBoardController.CurrentMissionType != MissionType.Grass)
            return;

        // BoardManager(-90)가 먼저 ProcessFullLines를 끝낸 뒤 호출된다.
        if (!_boardManager.HasAnyGrass())
        {
            _missedGrassLineClears = 0;
            return;
        }

        if (_boardManager.LastClearContainedGrass)
        {
            _missedGrassLineClears = 0;
            return;
        }

        _missedGrassLineClears++;
        if (_missedGrassLineClears < _missedClearsBeforeSpread)
            return;

        _missedGrassLineClears = 0;
        _boardManager.TrySpreadGrass(_spreadAppearDuration);
    }
}
