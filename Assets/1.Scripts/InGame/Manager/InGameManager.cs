using System.Collections.Generic;
using UnityEngine;

public class InGameManager : Singleton<InGameManager>
{
    private BoardManager _boardManager => BoardManager.Instance;
    private ScoreManager _scoreManager => ScoreManager.Instance;

    [SerializeField] private List<BlockSlot> _slots;

    private void OnEnable()
    {
        BlockSlot.OnBlockPlaced += HandleBlockPlaced;

    }

    private void OnDestroy()
    {
        BlockSlot.OnBlockPlaced -= HandleBlockPlaced;
    }

    private void HandleBlockPlaced(DraggableBlock block)
    {
        _scoreManager.HandleBlockPlaced(block.Shape._cellOffsets.Length);
        _boardManager.ProcessFullLines();

        foreach (var slot in _slots)
        {
            if (slot.Block == block)
                continue;

            if (!_boardManager.CanPlaceShape(slot.Block.Shape))
            {
                Debug.Log("게임 오버");
                return;
            }
        }
    }
}