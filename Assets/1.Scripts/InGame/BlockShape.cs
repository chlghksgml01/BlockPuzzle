using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/BlockShape")]
public class BlockShape : ScriptableObject
{
    [SerializeField] private Vector2Int[] _cellOffsets;
    [SerializeField] private float _weights;

    public Vector2Int[] CellOffsets => _cellOffsets;
    public float Weights => _weights;
}