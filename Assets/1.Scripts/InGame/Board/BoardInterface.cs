using System;
using System.Collections.Generic;
using UnityEngine;

public interface IBoardHandler
{
    public void UpdatePreviewFromScreen(DraggableBlock block, Vector2 anchorScreenPos, Vector2Int anchorOffset, Camera uiCam = null);

    public bool PlaceLastPreview(DraggableBlock block, out int placedCount);

    public void ClearDragPreview();

    public bool CanPlaceBlock { get; }
}

public interface IBoardQuery
{
    public bool TryGetCellWorldPosition(int x, int y, out Vector3 worldPos);
    public bool TryGetCellWorldCorners(int x, int y, Vector3[] corners);
    event Action<IReadOnlyList<int>, IReadOnlyList<int>> OnLinesClearedDetailed;
    /// <summary>드래그 프리뷰 중 클리어될 행/열 목록. 비어 있으면 프리뷰 해제.</summary>
    event Action<IReadOnlyList<int>, IReadOnlyList<int>> OnLineClearPreviewChanged;
}

public interface IBoardInfo
{
    int Width { get; }
    int Height { get; }
    float BoardCellSize { get; }
    bool CanPlaceBlock { get; }
    Color PreviewColor { get; }
}