using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class InGameManager : Singleton<InGameManager>, IInitializable
{
    [Header("References")]
    [SerializeField] private CanvasGroup _slotsCanvasGroup;
    [SerializeField] private List<BlockSlot> _slots;
    [SerializeField] private GameOverUI _gameOverUI;
    private ScoreSystem _scoreSystem;

    [Header("Settings")]
    [SerializeField] private float _hintTimeInterval = 5f;

    [Header("Game Over")]
    [SerializeField, Min(0f)] private float _gameOverDelaySeconds = 5f;
    [SerializeField, Min(0f)] private float _grayEffectDuration = 1f;

    [Header("Block")]
    [Tooltip("플레이어가 슬롯에서 놓고 배치하는 블록 스프라이트")]
    [SerializeField] private Sprite[] _blockSprites;
    [SerializeField, Range(0f, 1f)] private float _largeShapeSpawnReduceStartFillRatio = 0.5f;
    private readonly Dictionary<string, Sprite> _playerSpriteByName = new Dictionary<string, Sprite>();

    public static event Action<int> OnBlockSettled;
    public static event Action OnResetGame;

    private Coroutine _hintCoroutine;
    private Coroutine _gameOverCoroutine;
    private bool _isGameOverTriggered;
    private bool _subscriptionsBound;
    private int _previousBestScore;

    private BoardManager _boardManger;
    private MissionBoardController _missionBoardController;

    public void Initialize(InitializeContext context)
    {
        _scoreSystem = context.ScoreSystem;
        _boardManger = context.BoardManager;
        _missionBoardController = _boardManger != null
            ? _boardManger.GetComponent<MissionBoardController>()
            : null;
    }

    override protected void OnAwake()
    {
        _gameOverUI.gameObject.SetActive(false);
        BuildPlayerSpriteLookup();
    }

    private void OnEnable()
    {
        if (_scoreSystem == null || _boardManger == null)
        {
            Debug.LogError("InGameManager - Initialize must be called before OnEnable.");
            return;
        }

        _scoreSystem.Initialize(_boardManger.Width);

        BlockSlot.OnBlockPlaced += HandleBlockPlaced;
        _scoreSystem.OnHighScoreUpdated += SetNewBest;
        _subscriptionsBound = true;
    }

    private void OnDisable()
    {
        if (!_subscriptionsBound)
            return;

        BlockSlot.OnBlockPlaced -= HandleBlockPlaced;
        _scoreSystem.OnHighScoreUpdated -= SetNewBest;
        _subscriptionsBound = false;
    }

    private void Start()
    {
        _isGameOverTriggered = false;

        if (LevelSessionContext.IsActive)
        {
            StartLevelGame();
            return;
        }

        bool hasData = TryLoadGame(out bool isNewGame);

        if (!hasData || isNewGame)
        {
            if (!hasData)
                SpawnBlocksInSlots();

            EnableInteraction(false);
            _boardManger.PlayIntro(HandleIntroCompleted);
        }
        else
            EnableInteraction(true);

        ScheduleGameOverIfNeeded();
    }

    private void HandleBlockPlaced(int blockShapeCount)
    {
        OnBlockSettled?.Invoke(blockShapeCount);
        _scoreSystem.HandleBlockPlaced(blockShapeCount);

        bool hasBlocks = _slots.Any(slot => slot.HasBlock);

        if (!hasBlocks)
        {
            SpawnBlocksInSlots();
        }

        SaveGame();
        ScheduleGameOverIfNeeded();
    }

    public void StartHintCoroutine(DraggableBlock block)
    {
        if (_hintCoroutine == null)
            _hintCoroutine = StartCoroutine(TimerCoroutine(block));
    }

    public void StopHintCoroutine(DraggableBlock block, bool isPlaced)
    {
        _boardManger.ShowHint(false, block, isPlaced);

        if (_hintCoroutine != null)
        {
            StopCoroutine(_hintCoroutine);
            _hintCoroutine = null;
        }
    }

    IEnumerator TimerCoroutine(DraggableBlock block)
    {
        yield return new WaitForSeconds(_hintTimeInterval);

        if (_boardManger.CanPlaceShape(block.CurrentOffsets))
            _boardManger.ShowHint(true, block);

        _hintCoroutine = null;
    }

    private bool AreAllBlocksCannotPlace()
    {
        if (_boardManger == null)
            return true;

        int InvalidBlockCount = 0;
        foreach (BlockSlot slot in _slots)
        {
            if (!slot.HasBlock)
                continue;

            if (_boardManger.CanPlaceShape(slot.Block.CurrentOffsets))
                return false;

            InvalidBlockCount++;
        }

        Debug.Log("InvalidBlockCount : " + InvalidBlockCount);
        return true;
    }

    private void TriggerGameOverIfAllBlocksCannotPlace()
    {
        if (_isGameOverTriggered)
            return;

        if (!AreAllBlocksCannotPlace())
            return;

        _isGameOverTriggered = true;
        _previousBestScore = LeaderboardManager.Instance.BestScore;
        _scoreSystem.CheckHighScore(_previousBestScore);
        _gameOverUI.Open();
        SoundManager.Instance.PlaySFX(SFXType.Score);
        ResetGame();
    }

    public void ResetGame()
    {
        if (_gameOverCoroutine != null)
        {
            StopCoroutine(_gameOverCoroutine);
            _gameOverCoroutine = null;
        }

        if (_hintCoroutine != null)
        {
            StopCoroutine(_hintCoroutine);
            _hintCoroutine = null;
        }

        _isGameOverTriggered = false;
        SpawnBlocksInSlots();

        OnResetGame?.Invoke();
        if (LevelSessionContext.IsActive)
            ApplyLevelBoardLayout();
        _scoreSystem.ResetScore();
        SaveGame();
        ScheduleGameOverIfNeeded();
        _boardManger.ActivateGrayscale(false);
    }

    private void ScheduleGameOverIfNeeded()
    {
        if (_isGameOverTriggered)
            return;

        if (_gameOverCoroutine != null)
        {
            StopCoroutine(_gameOverCoroutine);
            _gameOverCoroutine = null;
        }

        if (!AreAllBlocksCannotPlace())
            return;

        _gameOverCoroutine = StartCoroutine(GameOverDelayCoroutine());
    }

    private IEnumerator GameOverDelayCoroutine()
    {
        Debug.Log("wait gameOverDelaySeconds");
        yield return new WaitForSeconds(_gameOverDelaySeconds);

        Debug.Log("wait grayEffectDuration");
        SoundManager.Instance.PlaySFX(SFXType.GameOver);
        _boardManger.ActivateGrayscale(true, _grayEffectDuration);
        yield return new WaitForSeconds(_grayEffectDuration + 1f);

        _gameOverCoroutine = null;

        TriggerGameOverIfAllBlocksCannotPlace();
    }

    private void SetNewBest(int newBestScore)
    {
        _gameOverUI.UpdateBanner(newBestScore > _previousBestScore);
    }

    public void SpawnBlocksInSlots()
    {
        ClearAllSlots();
        bool reduceLargeShapeSpawnRate = ShouldReduceLargeShapeSpawnRate();

        List<Sprite> spriteList = new List<Sprite>(_blockSprites);

        for (int i = 0; i < spriteList.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, spriteList.Count);
            Sprite temp = spriteList[i];
            spriteList[i] = spriteList[randomIndex];
            spriteList[randomIndex] = temp;
        }

        for (int i = 0; i < _slots.Count; i++)
        {
            if (i < spriteList.Count)
            {
                _slots[i].SpawnNewBlock(spriteList[i], reduceLargeShapeSpawnRate);
            }
        }
    }

    private bool ShouldReduceLargeShapeSpawnRate()
    {
        if (_boardManger == null)
            return false;

        int totalCells = _boardManger.Width * _boardManger.Height;
        if (totalCells <= 0)
            return false;

        int filledCellCount = _boardManger.ExportFilledCells().Count;
        float filledRatio = (float)filledCellCount / totalCells;
        return filledRatio >= _largeShapeSpawnReduceStartFillRatio;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SaveGame();
    }

    protected override void OnApplicationQuit()
    {
        SaveGame();
    }

    private void BuildPlayerSpriteLookup()
    {
        _playerSpriteByName.Clear();
        if (_blockSprites == null)
            return;

        for (int i = 0; i < _blockSprites.Length; i++)
        {
            Sprite sprite = _blockSprites[i];
            if (sprite == null)
                continue;

            if (!_playerSpriteByName.ContainsKey(sprite.name))
                _playerSpriteByName.Add(sprite.name, sprite);
        }
    }

    /// <summary>슬롯/보드 저장 복원용. 플레이어 스폰 스프라이트만 사용.</summary>
    private Sprite ResolvePlayerSprite(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName))
            return null;

        if (_playerSpriteByName.TryGetValue(spriteName, out Sprite sprite))
            return sprite;

        return null;
    }

    private void ClearAllSlots()
    {
        for (int i = 0; i < _slots.Count; i++)
            _slots[i].ClearSlotBlock();
    }

    private void SaveGame()
    {
        if (LevelSessionContext.IsActive)
            return;

        BoardManager board = _boardManger;
        if (board == null || _scoreSystem == null)
            return;

        InGameSaveData data = new InGameSaveData();
        data.boardSize = board.BoardSize;

        _scoreSystem.ExportState(out data.score, out data.currentPlaceCount, out data.currentComboCount);

        data.filledCells = board.ExportFilledCells();

        for (int i = 0; i < _slots.Count; i++)
        {
            BlockSlot slot = _slots[i];
            SlotBlockData slotData = new SlotBlockData();
            slotData.hasBlock = slot.HasBlock && slot.Block != null;

            if (slotData.hasBlock)
            {
                slotData.spriteName = slot.Block.BlockSprite != null ? slot.Block.BlockSprite.name : string.Empty;
                Vector2Int[] offsets = slot.Block.CurrentOffsets;
                if (offsets != null)
                {
                    for (int k = 0; k < offsets.Length; k++)
                        slotData.offsets.Add(new Vector2IntData(offsets[k]));
                }
            }

            data.slots.Add(slotData);
        }

        InGameSaveStorage.Save(data);
    }

    private bool TryLoadGame(out bool isNewGame)
    {
        isNewGame = true;
        if (!InGameSaveStorage.TryLoad(out InGameSaveData data))
            return false;

        if (data == null)
            return false;

        BoardManager board = _boardManger;
        if (board == null || _scoreSystem == null)
            return false;

        if (data.boardSize > 0 && data.boardSize != board.BoardSize)
        {
            Debug.LogWarning($"저장된 보드 크기({data.boardSize})와 현재 설정({board.BoardSize})이 달라 저장 데이터를 무시합니다.");
            InGameSaveStorage.Clear();
            return false;
        }

        ClearAllSlots();
        board.RestoreFilledCells(data.filledCells, ResolvePlayerSprite);

        if (data.slots != null && data.slots.Count > 0)
        {
            int count = Mathf.Min(data.slots.Count, _slots.Count);
            for (int i = 0; i < count; i++)
            {
                SlotBlockData sd = data.slots[i];
                if (sd == null || !sd.hasBlock)
                    continue;

                Sprite sprite = ResolvePlayerSprite(sd.spriteName);
                if (sprite == null)
                    continue;

                List<Vector2IntData> offsetData = sd.offsets;
                if (offsetData == null || offsetData.Count == 0)
                    continue;

                Vector2Int[] offsets = new Vector2Int[offsetData.Count];
                for (int k = 0; k < offsetData.Count; k++)
                    offsets[k] = offsetData[k].ToVector2Int();

                _slots[i].SpawnSavedBlock(sprite, offsets);
            }
        }

        isNewGame = (data.score == 0);

        _scoreSystem.RestoreState(data.score, data.currentPlaceCount, data.currentComboCount);
        return true;
    }

    public void EnableInteraction(bool isEnable)
    {
        _slotsCanvasGroup.blocksRaycasts = isEnable;
    }

    private void StartLevelGame()
    {
        SpawnBlocksInSlots();
        EnableInteraction(false);
        _boardManger.PlayIntro(HandleIntroCompleted);
        ScheduleGameOverIfNeeded();
    }

    private void HandleIntroCompleted()
    {
        if (LevelSessionContext.IsActive)
            ApplyLevelBoardLayout();

        EnableInteraction(true);
        ScheduleGameOverIfNeeded();
    }

    private void ApplyLevelBoardLayout()
    {
        if (_missionBoardController == null)
        {
            Debug.LogWarning("[InGameManager] MissionBoardController가 없습니다. BoardManager에 컴포넌트를 추가하세요.");
            return;
        }

        _missionBoardController.ApplyMissionLayout();
    }
}