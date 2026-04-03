using System;
using TMPro;
using UnityEngine;

public class NicknameUI : BasePopupUI
{
    [Header("Nickname UI Reference")]
    [SerializeField] private TMP_Text _defaultNicknameText;
    [SerializeField] private TMP_InputField _nicknameInput;

    public event Action OnNicknameChanged;

    private void Awake()
    {
        _nicknameInput.characterLimit = 15;
        _nicknameInput.onValueChanged.AddListener(OnNicknameInputChanged);
    }

    private void OnNicknameInputChanged(string value)
    {
        OnNicknameChanged?.Invoke();
    }

    public void SetDefaultNickname(string defaultNick)
    {
        _defaultNicknameText.text = defaultNick;
    }

    public string GetNickname()
    {
        if (!string.IsNullOrEmpty(_nicknameInput.text))
        {
            return _nicknameInput.text;
        }

        return _defaultNicknameText.text;
    }
}