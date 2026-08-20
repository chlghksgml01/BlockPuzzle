using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 가상화 풀에서 재사용되는 레벨 노드 하나의 뷰.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public sealed class LevelNodeView : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("노드에 표시할 레벨 번호 텍스트")]
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private GameObject _hardIcon;
    [SerializeField] private Button _nodeButton;
    [SerializeField] private Image _nodeImg;
    [Tooltip("잠긴(아직 해금되지 않은) 레벨 스프라이트")]
    [SerializeField] private Sprite _defaultSprite;
    [Tooltip("완료된 레벨 스프라이트")]
    [SerializeField] private Sprite _clearSprite;
    [Tooltip("해금되었지만 아직 완료하지 않은, 다음에 플레이할 레벨(현재 위치) 스프라이트")]
    [SerializeField] private Sprite _currentSprite;
    [Tooltip("완료된 레벨의 텍스트 색상")]
    [SerializeField] private Color _clearTextColor = Color.yellow;


    /// <summary>노드 버튼 클릭 시 자신의 NodeIndex를 담아 알리는 이벤트. 풀링 재사용을 고려해 생성 시 1회만 구독할 것.</summary>
    public event Action<int> OnClicked;

    public RectTransform RectTransform { get; private set; }
    public int NodeIndex { get; private set; } = -1;

    private Color _defaultTextColor;

    private void Awake()
    {
        RectTransform = (RectTransform)transform;

        if (_levelText != null)
            _defaultTextColor = _levelText.color;
    }
    private void OnEnable()
    {
        _nodeButton.onClick.AddListener(OnNodeButtonClicked);
    }

    private void OnNodeButtonClicked()
    {
        OnClicked?.Invoke(NodeIndex);
    }

    public void Bind(int nodeIndex, Vector2 anchoredPosition, MissionData missionData, bool isCurrent, bool isUnlocked)
    {
        NodeIndex = nodeIndex;
        RectTransform.anchoredPosition = anchoredPosition;
        gameObject.name = $"LevelNode_{nodeIndex + 1}";

        bool isCompleted = isUnlocked && !isCurrent;

        if (_levelText != null)
        {
            _levelText.text = (nodeIndex + 1).ToString();
            _levelText.color = isCompleted ? _clearTextColor : _defaultTextColor;
        }

        ApplyNodeVisual(missionData, isUnlocked, isCurrent);
    }

    /// <summary>
    /// 노드 상태를 3단계(잠김 → 현재(해금됨, 미완료) → 완료)로 시각화한다.
    /// isCurrent가 우선하며, 그 다음 완료 여부, 마지막으로 잠김 상태를 적용한다.
    /// </summary>
    private void ApplyNodeVisual(MissionData missionData, bool isUnlocked, bool isCurrent)
    {
        if (_hardIcon != null)
            _hardIcon.SetActive(missionData != null && missionData.IsHard);

        if (_nodeImg == null)
            return;

        Sprite sprite;
        if (isCurrent && _currentSprite != null)
            sprite = _currentSprite;
        else if (isUnlocked)
            sprite = _clearSprite;
        else
            sprite = _defaultSprite;

        if (sprite == null)
            return;

        _nodeImg.sprite = sprite;
        _nodeImg.SetNativeSize();
    }
}
