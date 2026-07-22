using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "BlockPuzzle/Board Layout Data", fileName = "BoardLayout")]
public class BoardLayoutData : ScriptableObject
{
    [Tooltip("보드 한 변의 칸 수")]
    public int boardSize = 9;

    [Tooltip("채워진 보드 칸 목록")]
    public List<FilledCellData> filledCells = new List<FilledCellData>();
}
