using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginBase : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _textMessage;

    protected void ResetUI(params Image[] images)
    {
        _textMessage.text = string.Empty;

        for (int i = 0; i < images.Length; i++)
        {
            images[i].color = Color.white;
        }
    }

    protected void SetMessage(string msg)
    {
        _textMessage.text = msg;
    }

    protected void GuideForIncorrectyEnteredData(Image image, string msg)
    {
        _textMessage.text = msg;
        image.color = Color.red;
    }

    protected bool IsFieldDataEmpty(Image image, string field, string result)
    {
        if (field.Trim().Equals(""))
        {
            GuideForIncorrectyEnteredData(image, $"\"{result}\" Please fill in the fields.");
            return true;
        }
        return false;
    }
}

