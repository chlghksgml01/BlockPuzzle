using UnityEngine;
using UnityEngine.UI;

public class BoardCell : MonoBehaviour
{
    private Image _image;
    public int _x { get; set; }
    public int _y { get; set; }

    public bool IsFilled { get; private set; }
    public bool IsPreviewFilled { get; private set; }
    public Sprite FilledSprite => _defaultSprite;

    private Sprite _defaultSprite;

    private Material _material;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _material = GetComponent<Image>().material;
    }

    public void Init(int x, int y)
    {
        _x = x;
        _y = y;
        SetFilled(false);
    }

    public void SetFilled(bool isfilled)
    {
        IsFilled = isfilled;
        IsPreviewFilled = isfilled;
    }

    public void SetPreviewFilled(bool isPreviewFilled)
    {
        IsPreviewFilled = isPreviewFilled;
    }

    public void UpdateCellVisual(bool isPreviewFilled, Sprite blockSprite = null)
    {
        if (IsFilled)
            return;

        if (isPreviewFilled)
        {
            _image.sprite = blockSprite;
            _image.color = BoardManager.Instance.PreviewColor;
        }
        else if (!isPreviewFilled)
        {
            _image.sprite = null;
            _image.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    public void PlaceBlock(Sprite blockSprite)
    {
        SetFilled(true);
        _defaultSprite = blockSprite;
        _image.sprite = _defaultSprite;
        _image.color = new Color(1f, 1f, 1f, 1f);
    }

    public void RestoreFilledState(Sprite blockSprite)
    {
        SetFilled(true);
        _defaultSprite = blockSprite;
        _image.sprite = _defaultSprite;
        _image.color = new Color(1f, 1f, 1f, 1f);
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

    public void ActivateGrayscale(bool useGrayScale, float effectDuration = 0f)
    {
#if UNITY_EDITOR
        Debug.Log("Use GrayScale " + useGrayScale);
#endif

        if (useGrayScale)
        {
            _material.SetFloat("_UseGrayscale", 1f);
            _material.SetFloat("_EffectStartTime", Time.time);
            _material.SetFloat("_EffectDuration", effectDuration);
        }
        else
        {
            _material.SetFloat("_UseGrayscale", 0f);
            _material.SetFloat("_EffectStartTime", 0f);
            _material.SetFloat("_EffectDuration", 0f);
        }
    }
}