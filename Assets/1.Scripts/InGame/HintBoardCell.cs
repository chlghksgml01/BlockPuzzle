using UnityEngine;
using UnityEngine.UI;

public class HintBoardCell : MonoBehaviour
{
    private Image _image;

    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    public void ShowHint(bool isShowHint)
    {
        if (isShowHint)
            _image.color = new Color(1f, 1f, 1f, 1f);
        else
            _image.color = new Color(1f, 1f, 1f, 0f);
    }
}