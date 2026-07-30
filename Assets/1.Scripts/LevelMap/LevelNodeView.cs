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
    [SerializeField] private Sprite _defaultSprite;
    [SerializeField] private Sprite _clearSprite;


    /// <summary>노드 버튼 클릭 시 자신의 NodeIndex를 담아 알리는 이벤트. 풀링 재사용을 고려해 생성 시 1회만 구독할 것.</summary>
    public event Action<int> OnClicked;

    public RectTransform RectTransform { get; private set; }
    public int NodeIndex { get; private set; } = -1;

    private void Awake()
    {
        RectTransform = (RectTransform)transform;
    }
    private void OnEnable()
    {
        _nodeButton.onClick.AddListener(OnNodeButtonClicked);
    }

    private void OnNodeButtonClicked()
    {
        OnClicked?.Invoke(NodeIndex);
    }

    public void Bind(int nodeIndex, Vector2 anchoredPosition, MissionData missionData)
    {
        NodeIndex = nodeIndex;
        RectTransform.anchoredPosition = anchoredPosition;
        gameObject.name = $"LevelNode_{nodeIndex + 1}";

        if (_levelText != null)
            _levelText.text = (nodeIndex + 1).ToString();

        ApplyNodeVisual(missionData);
    }

    private void ApplyNodeVisual(MissionData missionData)
    {
        if (_hardIcon != null)
            _hardIcon.SetActive(missionData != null && missionData.IsHard);

        if (_nodeImg == null)
            return;

        bool isClear = missionData != null && missionData.IsClear;
        Sprite sprite = isClear ? _clearSprite : _defaultSprite;
        if (sprite != null)
            _nodeImg.sprite = sprite;
    }
}
