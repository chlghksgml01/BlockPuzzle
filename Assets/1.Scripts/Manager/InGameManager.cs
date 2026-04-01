using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class InGameManager : Singleton<InGameManager>, IInitializable
{
    [Header("References")]
    [SerializeField] private List<BlockSlot> _slots;
    [SerializeField] private GameOverUI _gameOverUI;
    private ScoreSystem _scoreSystem;
    private IPlacementHandler _placementHandler;

    [Header("Settings")]
    [SerializeField] private float _hintTimeInterval = 5f;

    [Header("Game Over")]
    [SerializeField, Min(0f)] private float _gameOverDelaySeconds = 5f;
    [SerializeField, Min(0f)] private float _grayEffectDuration = 1f;

    [Header("Block")]
    [SerializeField] private Sprite[] _blockSprites;
    private Dictionary<string, Sprite> _spriteByName = new Dictionary<string, Sprite>();

    public static event Action<int> OnBlockSettled;
    public static event Action OnResetGame;

    private Coroutine _hintCoroutine;
    private Coroutine _gameOverCoroutine;
    private bool _isGameOverTriggered;

    public void OnInitialize(InitializeContext context)
    {
        _scoreSystem = context.ScoreSystem;
    }

    override protected void OnAwake()
    {
        _gameOverUI.gameObject.SetActive(false);
        _placementHandler = FindFirstObjectByType<BoardManager>();
        BuildSpriteLookup();
    }

    private void OnEnable()
    {
        _scoreSystem.Initialize(BoardManager.Instance.Width);

        BlockSlot.OnBlockPlaced += HandleBlockPlaced;
        _scoreSystem.OnHighScoreUpdated += SetNewBest;
    }

    private void OnDisable()
    {
        BlockSlot.OnBlockPlaced -= HandleBlockPlaced;
        _scoreSystem.OnHighScoreUpdated -= SetNewBest;
    }

    private void Start()
    {
        _isGameOverTriggered = false;

        bool hasData = TryLoadGame(out bool isNewGame);

        if (!hasData || isNewGame)
        {
            if (!hasData)
                SpawnBlocksInSlots();

            BoardManager.Instance.PlayIntro();
        }

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
        _placementHandler.ShowHint(false, block, isPlaced);

        if (_hintCoroutine != null)
        {
            StopCoroutine(_hintCoroutine);
            _hintCoroutine = null;
        }
    }

    IEnumerator TimerCoroutine(DraggableBlock block)
    {
        yield return new WaitForSeconds(_hintTimeInterval);

        if (_placementHandler.CanPlaceShape(block.CurrentOffsets))
            _placementHandler.ShowHint(true, block);

        _hintCoroutine = null;
    }

    private bool AreAllBlocksCannotPlace()
    {
        if (_placementHandler == null)
            return true;

        int InvalidBlockCount = 0;
        foreach (BlockSlot slot in _slots)
        {
            if (!slot.HasBlock)
                continue;

            if (_placementHandler.CanPlaceShape(slot.Block.CurrentOffsets))
                return false;

            InvalidBlockCount++;
        }

#if UNITY_EDITOR
        Debug.Log("놓을 수 없는 블럭 개수 : " + InvalidBlockCount);
#endif
        return true;
    }

    private void TriggerGameOverIfAllBlocksCannotPlace()
    {
        if (_isGameOverTriggered)
            return;

        if (!AreAllBlocksCannotPlace())
            return;

        _isGameOverTriggered = true;
#if UNITY_EDITOR
        Debug.Log("게임 오버");
#endif
        _scoreSystem.CheckHighScore(LeaderboardManager.Instance.BestScore);
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

        _isGameOverTriggered = false;
        SpawnBlocksInSlots();

        OnResetGame?.Invoke();
        _scoreSystem.ResetScore();
        SaveGame();
        ScheduleGameOverIfNeeded();
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
#if UNITY_EDITOR
        Debug.Log("wait gameOverDelaySeconds");
#endif
        yield return new WaitForSeconds(_gameOverDelaySeconds);

#if UNITY_EDITOR
        Debug.Log("wait grayEffectDuration");
#endif
        SoundManager.Instance.PlaySFX(SFXType.GameOver);
        BoardManager.Instance.ActivateGrayscale(true, _grayEffectDuration);
        yield return new WaitForSeconds(_grayEffectDuration + 1f);

        _gameOverCoroutine = null;

        TriggerGameOverIfAllBlocksCannotPlace();
    }

    private void SetNewBest(int newBestScore)
    {
        _gameOverUI.UpdateBanner(newBestScore != -1);
    }

    public void SpawnBlocksInSlots()
    {
        ClearAllSlots();

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
                _slots[i].SpawnNewBlock(spriteList[i]);
            }
        }
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

    private void BuildSpriteLookup()
    {
        _spriteByName.Clear();
        if (_blockSprites == null)
            return;

        for (int i = 0; i < _blockSprites.Length; i++)
        {
            Sprite sprite = _blockSprites[i];
            if (sprite == null)
                continue;

            if (!_spriteByName.ContainsKey(sprite.name))
                _spriteByName.Add(sprite.name, sprite);
        }
    }

    private Sprite ResolveSprite(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName))
            return null;

        if (_spriteByName.TryGetValue(spriteName, out Sprite sprite))
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
        BoardManager board = BoardManager.Instance;
        if (board == null || _scoreSystem == null)
            return;

        InGameSaveData data = new InGameSaveData();

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

        BoardManager board = BoardManager.Instance;
        if (board == null || _scoreSystem == null)
            return false;

        ClearAllSlots();
        board.RestoreFilledCells(data.filledCells, ResolveSprite);

        if (data.slots != null && data.slots.Count > 0)
        {
            int count = Mathf.Min(data.slots.Count, _slots.Count);
            for (int i = 0; i < count; i++)
            {
                SlotBlockData sd = data.slots[i];
                if (sd == null || !sd.hasBlock)
                    continue;

                Sprite sprite = ResolveSprite(sd.spriteName);
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
}