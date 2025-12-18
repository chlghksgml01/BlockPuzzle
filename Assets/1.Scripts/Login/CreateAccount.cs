using BackEnd;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UIElements.UxmlAttributeDescription;

public class CreateAccount : LoginBase
{
    [SerializeField]
    private Image _imageID;
    [SerializeField]
    private TMP_InputField _inputFieldID;
    [SerializeField]
    private Image _imagePW;
    [SerializeField]
    private TMP_InputField _inputFieldPW;
    [SerializeField]
    private Image _imageConfirmPW;
    [SerializeField]
    private TMP_InputField _inputFieldConfirmPW;
    [SerializeField]
    private Image _imageEmail;
    [SerializeField]
    private TMP_InputField _inputFieldEmail;

    [SerializeField]
    private Button _accountCreateBtn;

    public void OnClickCreateAccount()
    {
        // 매개변수로 입력한 InputField UI의 색상과 Message 내용 초기화 
        ResetUI(_imageID, _imagePW, _imageConfirmPW, _imageEmail);

        if (IsFieldDataEmpty(_imageID, _inputFieldID.text, "ID"))
            return;
        if (IsFieldDataEmpty(_imagePW, _inputFieldPW.text, "Password"))
            return;
        if (IsFieldDataEmpty(_imageConfirmPW, _inputFieldConfirmPW.text, "Password Confirm"))
            return;
        if (IsFieldDataEmpty(_imageEmail, _inputFieldEmail.text, "Email"))
            return;

        if (!_inputFieldPW.text.Equals(_inputFieldConfirmPW.text))
        {
            GuideForIncorrectyEnteredData(_imageConfirmPW, "Passwords do not match");
            return;
        }

        if (!_inputFieldEmail.text.Contains("@"))
        {
            GuideForIncorrectyEnteredData(_imageEmail, "Invalid email format (ex. address@xx.xx)");
            return;
        }

        _accountCreateBtn.interactable = false;
        SetMessage("Creating Your Account...");

        CustomSignUp();
    }

    private void CustomSignUp()
    {
        Backend.BMember.CustomSignUp(_inputFieldID.text, _inputFieldPW.text, callback =>
        {
            _accountCreateBtn.interactable = true;

            if (callback.IsSuccess())
            {
                Backend.BMember.UpdateCustomEmail(_inputFieldEmail.text, callback =>
                {
                    if (callback.IsSuccess())
                        SetMessage($"Account successfully created! Welcome,{_inputFieldID.text}");
                });
            }

            else
            {
                string message = string.Empty;
                switch (int.Parse(callback.GetStatusCode()))
                {
                    case 409:
                        message = "This ID is already in use.";
                        break;
                    default:
                        message = callback.GetMessage();
                        break;
                }

                if (message.Contains("ID"))
                    GuideForIncorrectyEnteredData(_imageID, message);
                else
                    SetMessage(message);
            }
        });
    }
}
