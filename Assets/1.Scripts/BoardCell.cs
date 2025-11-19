using UnityEngine;
using UnityEngine.UI;

public class BoardCell : MonoBehaviour
{
    public int _x { get; set; }
    public int _y { get; set; }

    public bool IsFilled { get; private set; }

    public void Init(int x, int y)
    {
        _x = x;
        _y = y;
        SetFilled(false);
    }

    public void SetFilled(bool isfilled)
    {
        IsFilled = isfilled;
    }
}