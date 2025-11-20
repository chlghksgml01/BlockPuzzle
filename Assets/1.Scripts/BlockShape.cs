using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/BlockShape")]
public class BlockShape : ScriptableObject
{
    public Vector2Int[] _cellOffsets;
}