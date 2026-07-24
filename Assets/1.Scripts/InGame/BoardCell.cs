using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class BoardCell : MonoBehaviour
{
    private const string StoneSpriteNameKeyword = "stone";
    private const string IceSpriteNameKeyword = "ice";
    private const string GrassSpriteNameKeyword = "grass";
    private const string PentagonSpriteNameKeyword = "pentagon";
    private const string SquareSpriteNameKeyword = "square";
    private const string StarSpriteNameKeyword = "star";
    private const string AppearTweenIdPrefix = "BoardCellAppear_";
    private const string StageTweenIdPrefix = "BoardCellStage_";

    /// <summary>ice/grass 01~03 중 최대 단계. 이 값을 넘기면 셀이 제거된다.</summary>
    public const int MaxStagedBlockStage = 3;

    private const float StageDownSquashDuration = 0.08f;
    private const float StageDownPopDuration = 0.14f;
    private const float StageClearDuration = 0.15f;

    private Image _image;
    public int _x { get; set; }
    public int _y { get; set; }

    public bool IsFilled { get; private set; }
    public bool IsBlocked { get; private set; }
    public bool IsPreviewFilled { get; private set; }
    public bool IsOccupied => IsFilled || IsBlocked;
    public bool IsIce => IsIceSpriteName(FilledSprite != null ? FilledSprite.name : null);
    public bool IsGrass => IsGrassSpriteName(FilledSprite != null ? FilledSprite.name : null);
    public bool IsStagedMissionBlock => TryGetStagedBlockInfo(FilledSprite != null ? FilledSprite.name : null, out _, out _);
    /// <summary>레이아웃/미션용 특수 블록 (stone, ice, grass).</summary>
    public bool IsMissionBlock => IsBlocked || IsIce || IsGrass;
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

    /// <summary>ice/grass 단계 스프라이트만 교체한다. 점유 상태는 유지.</summary>
    public void SetStageSprite(Sprite stageSprite)
    {
        if (stageSprite == null)
            return;

        IsBlocked = false;
        IsFilled = true;
        IsPreviewFilled = true;
        _defaultSprite = stageSprite;
        _image.sprite = _defaultSprite;
        _image.color = new Color(1f, 1f, 1f, 1f);
        transform.localScale = Vector3.one;
    }

    /// <summary>
    /// ice/grass 데미지를 한 단계씩 적용하며 DOTween 연출을 재생한다.
    /// max 단계 초과 시 제거 연출 후 클리어.
    /// </summary>
    public bool TryPlayStagedDamage(int damage, System.Func<string, Sprite> spriteResolver)
    {
        if (damage <= 0 || spriteResolver == null)
            return false;

        string currentName = FilledSprite != null ? FilledSprite.name : null;
        if (!TryGetStagedBlockInfo(currentName, out string keyword, out int currentStage))
            return false;

        KillStageTween();
        transform.localScale = Vector3.one;

        string tweenId = StageTweenIdPrefix + GetInstanceID();
        Sequence sequence = DOTween.Sequence().SetId(tweenId);

        int targetStage = currentStage + damage;
        for (int nextStage = currentStage + 1; nextStage <= targetStage; nextStage++)
        {
            if (nextStage > MaxStagedBlockStage)
            {
                sequence.Append(transform.DOScale(Vector3.zero, StageClearDuration).SetEase(Ease.InBack));
                sequence.AppendCallback(ClearOccupiedStateKeepTween);
                break;
            }

            string nextSpriteName = GetStagedSpriteName(keyword, nextStage);
            Sprite nextSprite = spriteResolver.Invoke(nextSpriteName);
            if (nextSprite == null)
            {
                Debug.LogWarning($"[BoardCell] 단계 스프라이트를 찾지 못해 제거합니다: {nextSpriteName}", this);
                sequence.Append(transform.DOScale(Vector3.zero, StageClearDuration).SetEase(Ease.InBack));
                sequence.AppendCallback(ClearOccupiedStateKeepTween);
                break;
            }

            Sprite capturedSprite = nextSprite;
            sequence.Append(transform.DOScale(0.82f, StageDownSquashDuration).SetEase(Ease.InQuad));
            sequence.AppendCallback(() => SetStageSprite(capturedSprite));
            sequence.Append(transform.DOScale(Vector3.one, StageDownPopDuration).SetEase(Ease.OutBack));
        }

        sequence.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        return true;
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
        KillStageTween();
        ClearOccupiedStateKeepTween();
        transform.localScale = Vector3.one;
    }

    public void SetLinePreview(bool active, Sprite lineSprite = null, bool isIntroEffect = false)
    {
        // 드래그 프리뷰 칸, ice/grass는 라인 클리어 프리뷰로 스프라이트를 덮지 않는다.
        if (IsPreviewFilled || IsIce || IsGrass)
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

    public static bool IsIceSpriteName(string spriteName)
    {
        return !string.IsNullOrEmpty(spriteName) &&
               spriteName.IndexOf(IceSpriteNameKeyword, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static bool IsGrassSpriteName(string spriteName)
    {
        return !string.IsNullOrEmpty(spriteName) &&
               spriteName.IndexOf(GrassSpriteNameKeyword, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static bool IsGemSpriteName(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName))
            return false;

        return spriteName.IndexOf(PentagonSpriteNameKeyword, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               spriteName.IndexOf(SquareSpriteNameKeyword, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               spriteName.IndexOf(StarSpriteNameKeyword, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>ice01 / ice01_0 / grass02 등에서 키워드와 단계를 파싱한다.</summary>
    public static bool TryGetStagedBlockInfo(string spriteName, out string keyword, out int stage)
    {
        keyword = null;
        stage = 0;
        if (string.IsNullOrEmpty(spriteName))
            return false;

        if (IsIceSpriteName(spriteName))
            keyword = IceSpriteNameKeyword;
        else if (IsGrassSpriteName(spriteName))
            keyword = GrassSpriteNameKeyword;
        else
            return false;

        int keywordIndex = spriteName.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase);
        if (keywordIndex < 0)
            return false;

        int numStart = keywordIndex + keyword.Length;
        while (numStart < spriteName.Length && !char.IsDigit(spriteName[numStart]))
            numStart++;

        if (numStart >= spriteName.Length)
            return false;

        int numEnd = numStart;
        while (numEnd < spriteName.Length && char.IsDigit(spriteName[numEnd]))
            numEnd++;

        return int.TryParse(spriteName.Substring(numStart, numEnd - numStart), out stage) && stage > 0;
    }

    public static string GetStagedSpriteName(string keyword, int stage)
    {
        return keyword + stage.ToString("D2");
    }

    private void ClearOccupiedStateKeepTween()
    {
        IsBlocked = false;
        IsFilled = false;
        IsPreviewFilled = false;
        _defaultSprite = null;
        if (_image != null)
        {
            _image.sprite = null;
            _image.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    private void KillAppearTween()
    {
        DOTween.Kill(AppearTweenIdPrefix + GetInstanceID());
        if (_image != null)
            _image.DOKill();
    }

    private void KillStageTween()
    {
        DOTween.Kill(StageTweenIdPrefix + GetInstanceID());
        transform.DOKill();
    }
}
