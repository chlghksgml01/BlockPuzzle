using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InGameManager : Singleton<InGameManager>
{
    [SerializeField]
    private float _hintTimeInterval = 5f;
    private Coroutine _hintCoroutine;

    [SerializeField]
    private List<BlockSlot> _slots;

    [SerializeField]
    private GameObject _gameOverUI;

    override protected void OnAwake()
    {
        _gameOverUI.SetActive(false);
    }

    private void OnEnable()
    {
        BlockSlot.OnBlockPlaced += HandleBlockPlaced;
    }

    private void OnDestroy()
    {
        BlockSlot.OnBlockPlaced -= HandleBlockPlaced;
    }

    private void HandleBlockPlaced(int blockShapeCount)
    {
        ScoreManager.Instance.HandleBlockPlaced(blockShapeCount);
        BoardManager.Instance.ProcessFullLines();

        bool hasBlocks = _slots.Any(slot => slot.HasBlock);

        if (!hasBlocks)
        {
            foreach (var slot in _slots)
            {
                slot.SetNewBlock();
            }
        }
    }

    public void StartHintTimeCoroutine(Vector2Int[] blockShapeOffset)
    {
        if (_hintCoroutine == null)
            _hintCoroutine = StartCoroutine(TimerCoroutine(blockShapeOffset));
    }

    IEnumerator TimerCoroutine(Vector2Int[] blockShapeOffset)
    {
        yield return new WaitForSeconds(_hintTimeInterval);

        if (BoardManager.Instance.CanPlaceShape(blockShapeOffset))
            BoardManager.Instance.ShowHint(blockShapeOffset);
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

            if (BoardManager.Instance.CanPlaceShape(slot.Block.CurrentOffsets))
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

    public void SpawnNewBlock()
    {
        foreach (var slot in _slots)
        {
            slot.SpawnNewBlock();
        }
    }
}