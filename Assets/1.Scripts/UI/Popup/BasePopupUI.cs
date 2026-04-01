using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class BasePopupUI : MonoBehaviour
{
    [Header("Popup Settings")]
    [SerializeField] protected CanvasGroup _canvasGroup;
    [SerializeField] protected Transform _popupTransform;

    [Header("Animation Duration")]
    [SerializeField, Range(0.1f, 2f)] private float _duration = 0.3f;

    public virtual void Open()
    {
        _canvasGroup.alpha = 0f;
        _popupTransform.localScale = Vector3.zero;

        gameObject.SetActive(true);

        _popupTransform.DOKill();
        _canvasGroup.DOKill();

        _popupTransform.DOScale(1f, _duration).SetEase(Ease.OutBack).SetUpdate(true);
        _canvasGroup.DOFade(1f, _duration * 0.6f).SetUpdate(true);
    }

    public virtual void Close()
    {
        _popupTransform.DOKill();
        _canvasGroup.DOKill();

        _popupTransform.DOScale(0f, _duration).SetEase(Ease.InBack).SetUpdate(true);

        _canvasGroup.DOFade(0f, _duration).SetUpdate(true).OnComplete(() =>
        {
            gameObject.SetActive(false);
        });
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
            SoundManager.Instance.PlaySFX(SFXType.ClickUI);
        }
    }
}