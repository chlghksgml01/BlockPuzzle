using UnityEngine;

public class BlockBodyTile : MonoBehaviour
{
    private IBlockTileHandler _handler;

    private void Awake()
    {
        _handler = GetComponentInParent<IBlockTileHandler>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Cell"))
        {
            _handler.OnTileEnterCell();
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Cell"))
        {
            _handler.OnTileStayCell();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Cell"))
        {
            _handler.OnTileExitCell();
        }
    }
}