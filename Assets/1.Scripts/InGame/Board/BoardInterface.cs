using System;
using System.Collections.Generic;
using UnityEngine;

public interface IBoardHandler
{
    public void UpdatePreviewFromScreen(DraggableBlock block, Vector2 anchorScreenPos, Vector2Int anchorOffset, Camera uiCam = null);

    public bool PlaceLastPreview(DraggableBlock block, Sprite blockSprite, out int placedCount);

    public void ClearDragPreview();

    public bool CanPlaceBlock { get; }
}

public interface IBoardQuery
{
    public bool TryGetCellWorldPosition(int x, int y, out Vector3 worldPos);
    event Action<IReadOnlyList<int>, IReadOnlyList<int>> OnLinesClearedDetailed;
}

public interface IBoardInfo
{
    int Width { get; }
    int Height { get; }
    float BoardCellSize { get; }
    bool CanPlaceBlock { get; }
    Color PreviewColor { get; }
}