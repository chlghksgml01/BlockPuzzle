using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InGameManager : Singleton<InGameManager>
{
    private BoardManager _boardManager => BoardManager.Instance;
    private ScoreManager _scoreManager => ScoreManager.Instance;

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
        _scoreManager.HandleBlockPlaced(blockShapeCount);
        _boardManager.ProcessFullLines();

        bool hasBlocks = _slots.Any(slot => slot.HasBlock);

        if (hasBlocks)
        {
            IsGameOver();
        }

        else
        {
            foreach (var slot in _slots)
            {
                slot.SetNewBlock();
            }
        }
    }

    private void IsGameOver()
    {
        bool allBlocksCannotPlace = true;

        foreach (var slot in _slots)
        {
            if (!slot.HasBlock)
                continue;

            if (_boardManager.CanPlaceShape(slot.Block.Shape))
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