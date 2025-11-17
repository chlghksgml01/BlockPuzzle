using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public int _width = 9;
    public int _height = 9;

    public RectTransform _boardRoot;
    public GameObject _cellPrefab;

    private bool[,] board;

    private void Awake()
    {
        board = new bool[_width, _height];
        GenerateBoard();
    }

    private void GenerateBoard()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                GameObject cell = Instantiate(_cellPrefab, _boardRoot);
            }
        }
    }
}
