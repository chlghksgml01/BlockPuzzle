using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class InGameManager : Singleton<InGameManager>
{
    private IPlacementHandler _placementHandler;

    [SerializeField]
    private float _hintTimeInterval = 5f;
    private Coroutine _hintCoroutine;

    [SerializeField]
    private List<BlockSlot> _slots;

    [SerializeField]
    private GameObject _gameOverUI;

    public static event Action<int> OnBlockSettled;
    public static event Action OnResetGame;

    override protected void OnAwake()
    {
        _gameOverUI.SetActive(false);
        _placementHandler = FindFirstObjectByType<BoardManager>();
    }

    private void OnEnable()
    {
        BlockSlot.OnBlockPlaced += HandleBlockPlaced;
    }

    private void OnDisable()
    {
        BlockSlot.OnBlockPlaced -= HandleBlockPlaced;
    }

    private void HandleBlockPlaced(int blockShapeCount)
    {
        OnBlockSettled?.Invoke(blockShapeCount);

        bool hasBlocks = _slots.Any(slot => slot.HasBlock);

        if (!hasBlocks)
        {
            foreach (var slot in _slots)
            {
                slot.SetNewBlock();
            }
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
            _gameOverUI.SetActive(true);
            return;
        }
    }

    public void ResetGame()
    {
        foreach (var slot in _slots)
        {
            slot.SpawnNewBlock();
        }

        OnResetGame?.Invoke();
    }
}