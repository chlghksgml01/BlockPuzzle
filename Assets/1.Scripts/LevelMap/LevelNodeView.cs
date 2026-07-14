using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 레벨 노드 프리팹의 뷰 스크립트. 배치 좌표/데이터는 외부(LevelMapVirtualizer)에서 Bind로 주입받아 표시만 담당한다.
/// 오브젝트 풀에서 재사용되므로 상태는 ResetView에서 반드시 정리한다.
/// </summary>
public class LevelNodeView : MonoBehaviour
{
    [Header("References")]
    [Tooltip("레벨 번호를 표시하는 텍스트")]
    [SerializeField] private TMP_Text _levelNumberText;

    [Tooltip("해골/보상 상자 등 특수 아이콘 이미지 (Normal 타입이면 비활성화)")]
    [SerializeField] private Image _specialIconImage;

    [Tooltip("노드 클릭을 받는 버튼")]
    [SerializeField] private Button _button;

    private RectTransform _rectTransform;
    private Action<int> _onClicked;
    private int _levelIndex;

    private void Awake()
    {
        _rectTransform = (RectTransform)transform;
        _button.onClick.AddListener(HandleClicked);
    }

    public void Bind(LevelNodeLayout layout, bool isUnlocked, Action<int> onClicked)
    {
        _levelIndex = layout.LevelIndex;
        _onClicked = onClicked;

        _rectTransform.anchoredPosition = layout.Position;
        _levelNumberText.text = layout.LevelIndex.ToString();

        bool hasSpecialIcon = layout.IconType != LevelNodeIconType.Normal;
        _specialIconImage.gameObject.SetActive(hasSpecialIcon);

        _button.interactable = isUnlocked;
    }

    public void ResetView()
    {
        _onClicked = null;
    }

    private void HandleClicked()
    {
        _onClicked?.Invoke(_levelIndex);
    }
}
