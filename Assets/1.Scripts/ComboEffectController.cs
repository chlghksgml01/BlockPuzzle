using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-70)]
public class ComboEffectController : MonoBehaviour, IInitializable
{
    [Header("Combo Feedback")]
    [SerializeField] private bool _enableComboFeedback = true;

    [Header("Shake Settings")]
    [SerializeField] private float _shakeDuration = 0.1f;
    [SerializeField] private float _shakeStrength = 30f;
    [SerializeField] private int _shakeVibrato = 20;
    [SerializeField] private RectTransform _shakeTarget;

    [Header("Combo Text")]
    [SerializeField] private GameObject _comboText;
    [SerializeField] private NumberDisplay _scoreDisplay;
    [SerializeField] private RectTransform _comboTextTransform;
    private Vector3 _originalScale = Vector3.one;

    [Header("Combo Text & Effect Settings")]
    [SerializeField] private float _punchStrength = 0.5f;
    [SerializeField] private float _punchDuration = 0.2f;

    [SerializeField] private float _hideDelay = 0.5f;
    [SerializeField] private float _hideDuration = 0.1f;

    [Header("Combo Score Text")]
    [SerializeField] private TMP_Text _comboBonusText;
    [SerializeField] private float _comboScoreDelay = 0.5f;
    [SerializeField] private float _comboTextRiseY = 60f;
    [SerializeField] private float _comboTextDuration = 0.8f;

    private Vector3 _startPos;

    ScoreSystem _scoreSystem;

    public void Initialize(InitializeContext context)
    {
        _scoreSystem = context.ScoreSystem;
    }

    private void Awake()
    {
        _startPos = _comboBonusText.rectTransform.anchoredPosition;

        SetComboBonusColor(0f);

        _comboTextTransform.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (_scoreSystem != null)
            _scoreSystem.OnComboScore += PlayCombo;
    }

    private void OnDisable()
    {
        if (_scoreSystem != null)
            _scoreSystem.OnComboScore -= PlayCombo;
    }

    // 테스트용
    private void Update()
    {
        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            if (!_comboTextTransform.gameObject.activeSelf)
            {
                _comboTextTransform.gameObject.SetActive(true);
            }

            PlayCombo(50, 4);
        }

        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            if (!_comboTextTransform.gameObject.activeSelf)
            {
                _comboTextTransform.gameObject.SetActive(true);
            }

            PlayCombo(50, 40);
        }
    }

    private void PlayCombo(int comboAddedScore, int comboCount)
    {
        if (!_enableComboFeedback)
            return;

        if (comboAddedScore <= 0)
            return;

        if (!_comboTextTransform.gameObject.activeSelf)
        {
            _comboTextTransform.gameObject.SetActive(true);
        }

        TriggerComboShake();
        ShowComboCount(comboCount);
        DOTween.Kill("ComboBonusTask");
        DOVirtual.DelayedCall(_comboScoreDelay, () => ShowComboBonusText(comboAddedScore)).SetId("ComboBonusTask");
    }

    private void TriggerComboShake()
    {
        _shakeTarget.DOKill(true);
        _shakeTarget.DOShakeAnchorPos(_shakeDuration, _shakeStrength, _shakeVibrato);
    }

    private void ShowComboCount(int comboCount)
    {
        _scoreDisplay.UpdateDisplay(comboCount, 0);


        if (_comboTextTransform != null)
        {
            _comboTextTransform.DOKill(true);
            _comboTextTransform.localScale = _originalScale;

            float targetX = (comboCount < 10) ? -51.2f : -102.82f;
            Vector2 currentPos = _comboTextTransform.anchoredPosition;
            _comboTextTransform.anchoredPosition = new Vector2(targetX, currentPos.y);

            Sequence comboSeq = DOTween.Sequence();
            comboSeq.Append(_comboTextTransform.DOPunchScale(Vector3.one * _punchStrength, _punchDuration, 10, 1));
            comboSeq.AppendInterval(_hideDelay);
            comboSeq.Append(_comboTextTransform.DOScale(Vector3.zero, _hideDuration).SetEase(Ease.InBack));
        }
    }

    private void ShowComboBonusText(int comboAddedScore)
    {
        _comboBonusText.DOKill(true);
        _comboBonusText.rectTransform.DOKill(true);

        _comboBonusText.text = $"+{comboAddedScore}";

        SetComboBonusColor(1f);

        _comboBonusText.rectTransform.anchoredPosition = _startPos;
        float targetY = _startPos.y + _comboTextRiseY;

        _comboBonusText.rectTransform.DOAnchorPosY(targetY, _comboTextDuration).SetEase(Ease.OutCubic);
        _comboBonusText.DOFade(0f, _comboTextDuration).SetEase(Ease.InQuad);
    }

    private void SetComboBonusColor(float alpha)
    {
        Color color = _comboBonusText.color;
        color.a = alpha;
        _comboBonusText.color = color;
    }
}
