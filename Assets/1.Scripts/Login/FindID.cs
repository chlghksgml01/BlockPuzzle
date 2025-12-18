using BackEnd;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FindID : LoginBase
{
    [SerializeField]
    private Image _imageEmail;
    [SerializeField]
    private TMP_InputField _inputFieldEmail;

    [SerializeField]
    private Button _findIDBtn;

    public void ResetUI()
    {
        ResetUI(_imageEmail);
    }

    public void OnClickFindID()
    {
        ResetUI(_imageEmail);

        if (IsFieldDataEmpty(_imageEmail, _inputFieldEmail.text, "Email"))
            return;

        if (!_inputFieldEmail.text.Contains("@"))
        {
            GuideForIncorrectyEnteredData(_imageEmail, "Invalid email format (ex. address@xx.xx)");
            return;
        }

        _findIDBtn.interactable = false;
        SetMessage("Sending Email...");

        FindCustomID();
    }

    private void FindCustomID()
    {
        Backend.BMember.FindCustomID(_inputFieldEmail.text, callback =>
        {
            _findIDBtn.interactable = true;

            if (callback.IsSuccess())
                SetMessage($"We¡¯ve sent an email to {_inputFieldEmail.text}");

            else
            {
                string message = string.Empty;
                switch (int.Parse(callback.GetStatusCode()))
                {
                    case 404:
                        message = "No account found with this email address";
                        break;
                    case 429:
                        message = "Too many attempts. Please try again later";
                        break;
                    default:
                        message = callback.GetMessage();
                        break;
                }

                if (message.Contains("Email"))
                    GuideForIncorrectyEnteredData(_imageEmail, message);
                else
                    SetMessage(message);
            }
        });

    }
}
