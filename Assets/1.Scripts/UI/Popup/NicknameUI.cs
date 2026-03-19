using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NicknameUI : BasePopupUI
{
    [Header("Nickname UI Reference")]
    [SerializeField] private TMP_Text _defaultNicknameText;
    [SerializeField] private TMP_InputField _nicknameInput;
    [SerializeField] private Button _confirmButton;

    private void Awake()
    {
        _nicknameInput.characterLimit = 12;
    }

    public void SetDefaultNickname(string defaultNick)
    {
        _defaultNicknameText.text = defaultNick;
    }

    public string GetNickname()
    {
        if (!string.IsNullOrWhiteSpace(_nicknameInput.text))
        {
            return _nicknameInput.text;
        }

        return _defaultNicknameText.text;
    }
}