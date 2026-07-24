using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class BoardCell : MonoBehaviour
{
    private const string StoneSpriteNameKeyword = "stone";
    private const string AppearTweenIdPrefix = "BoardCellAppear_";

    private Image _image;
    public int _x { get; set; }
    public int _y { get; set; }

    public bool IsFilled { get; private set; }
    public bool IsBlocked { get; private set; }
    public bool IsPreviewFilled { get; private set; }
    public bool IsOccupied => IsFilled || IsBlocked;
    public Sprite FilledSprite => _defaultSprite;

    private Sprite _defaultSprite;

    private IBoardInfo _boardInfo;

    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    public void Init(int x, int y, IBoardInfo boardInfo)
    {
        _x = x;
        _y = y;
        _boardInfo = boardInfo;
        ClearAllState();
    }

    public void SetFilled(bool isfilled)
    {
        if (IsBlocked)
            return;

        IsFilled = isfilled;
        IsPreviewFilled = isfilled;
    }

    public void SetPreviewFilled(bool isPreviewFilled)
    {
        if (IsBlocked)
            return;

        IsPreviewFilled = isPreviewFilled;
    }

    public void UpdateCellVisual(bool isPreviewFilled, Sprite blockSprite = null)
    {
        if (IsOccupied)
            return;

        if (isPreviewFilled)
        {
            _image.sprite = blockSprite;
            _image.color = _boardInfo.PreviewColor;
        }
        else
        {
            _image.sprite = null;
            _image.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    public void PlaceBlock(Sprite blockSprite)
    {
        if (IsBlocked)
            return;

        SetFilled(true);
        _defaultSprite = blockSprite;
        _image.sprite = _defaultSprite;
        _image.color = new Color(1f, 1f, 1f, 1f);
    }

    public void RestoreFilledState(Sprite blockSprite)
    {
        IsBlocked = false;
        SetFilled(true);
        _defaultSprite = blockSprite;
        _image.sprite = _defaultSprite;
        _image.color = new Color(1f, 1f, 1f, 1f);
    }

    /// <summary>레이아웃의 stone 스프라이트 칸. 블록 배치는 불가하지만, 라인 클리어 시 함께 제거된다.</summary>
    public void SetBlocked(Sprite blockSprite)
    {
        IsBlocked = true;
        IsFilled = false;
        IsPreviewFilled = false;
        _defaultSprite = blockSprite;
        _image.sprite = _defaultSprite;
        _image.color = new Color(1f, 1f, 1f, 1f);
    }

    /// <summary>미션 블록 등장 연출 (페이드 + 스케일).</summary>
    public void PlayAppearTween(float duration)
    {
        if (_image == null || !IsOccupied)
            return;

        KillAppearTween();

        Color color = _image.color;
        color.a = 0f;
        _image.color = color;
        transform.localScale = Vector3.zero;

        string tweenId = AppearTweenIdPrefix + GetInstanceID();
        Sequence sequence = DOTween.Sequence().SetId(tweenId);
        sequence.Append(_image.DOFade(1f, duration).SetEase(Ease.OutQuad));
        sequence.Join(transform.DOScale(Vector3.one, duration).SetEase(Ease.OutBack));
        sequence.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    public void ClearAllState()
    {
        KillAppearTween();

        IsBlocked = false;
        IsFilled = false;
        IsPreviewFilled = false;
        _defaultSprite = null;
        transform.localScale = Vector3.one;
        if (_image != null)
        {
            _image.sprite = null;
            _image.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    public void SetLinePreview(bool active, Sprite lineSprite = null, bool isIntroEffect = false)
    {
        if (IsPreviewFilled)
            return;

        if (active)
        {
            _image.sprite = lineSprite;
            _image.color = new Color(1f, 1f, 1f, 1f);
        }
        else
        {
            _image.sprite = _defaultSprite;

            if (isIntroEffect)
                _image.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    public static bool IsStoneSpriteName(string spriteName)
    {
        return !string.IsNullOrEmpty(spriteName) &&
               spriteName.IndexOf(StoneSpriteNameKeyword, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void KillAppearTween()
    {
        DOTween.Kill(AppearTweenIdPrefix + GetInstanceID());
        if (_image != null)
            _image.DOKill();
        transform.DOKill();
    }
}
