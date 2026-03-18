using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[DefaultExecutionOrder(-99)]
public class InGameManager : Singleton<InGameManager>
{
    [Header("References")]
    [SerializeField] private List<BlockSlot> _slots;
    [SerializeField] private GameOverUI _gameOverUI;
    private IPlacementHandler _placementHandler;

    [Header("Settings")]
    [SerializeField] private float _hintTimeInterval = 5f;

    [Header("Block")]
    [SerializeField] private Sprite[] _blockSprites;

    public static event Action<int> OnBlockSettled;
    public static event Action OnResetGame;
    public static event Action OnGameOver;

    private Coroutine _hintCoroutine;

    override protected void OnAwake()
    {
        _gameOverUI.gameObject.SetActive(false);
        _placementHandler = FindFirstObjectByType<BoardManager>();
    }

    private void OnEnable()
    {
        BlockSlot.OnBlockPlaced += HandleBlockPlaced;
        ScoreManager.OnHighScoreUpdated += SetNewBest;
    }

    private void OnDisable()
    {
        BlockSlot.OnBlockPlaced -= HandleBlockPlaced;
        ScoreManager.OnHighScoreUpdated -= SetNewBest;
    }

    private void Start()
    {
        SpawnBlocksInSlots();
    }

    private void HandleBlockPlaced(int blockShapeCount)
    {
        OnBlockSettled?.Invoke(blockShapeCount);

        bool hasBlocks = _slots.Any(slot => slot.HasBlock);

        if (!hasBlocks)
        {
            SpawnBlocksInSlots();
        }
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
        else
            IsGameOver();

        _hintCoroutine = null;
    }

    private void IsGameOver()
    {
        bool allBlocksCannotPlace = true;

        foreach (var slot in _slots)
        {
            if (!slot.HasBlock)
                continue;

            if (_placementHandler.CanPlaceShape(slot.Block.CurrentOffsets))
            {
                allBlocksCannotPlace = false;
                break;
            }
        }

        if (allBlocksCannotPlace)
        {
            Debug.Log("게임 오버");
            OnGameOver?.Invoke();
            _gameOverUI.Open();
            return;
        }
    }

    public void ResetGame()
    {
        SpawnBlocksInSlots();

        OnResetGame?.Invoke();
    }

    private void SetNewBest(int newBestScore)
    {
        _gameOverUI.UpdateBanner(newBestScore != -1);
    }

    public void SpawnBlocksInSlots()
    {
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
}