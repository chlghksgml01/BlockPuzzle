using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 미션 레이아웃/팔레트 적용 전담. 보드 코어(배치·프리뷰·클리어)는 BoardManager가 담당한다.
/// </summary>
[DefaultExecutionOrder(-95)]
[RequireComponent(typeof(BoardManager))]
public sealed class MissionBoardController : MonoBehaviour
{
    [Header("Mission Sprites")]
    [Tooltip("미션 레이아웃/stone 등용 스프라이트 팔레트. Mission Maker와 동일한 에셋을 연결.")]
    [SerializeField] private BlockSpritePalette _missionSpritePalette;

    [Header("Appear Tween")]
    [Tooltip("미션 블록 등장 애니메이션 시간")]
    [SerializeField, Min(0f)] private float _appearDuration = 0.28f;

    private BoardManager _boardManager;
    private readonly Dictionary<string, Sprite> _spriteByName = new Dictionary<string, Sprite>();

    /// <summary>현재 레벨 세션의 미션 종류. 비레벨이면 None.</summary>
    public MissionType CurrentMissionType { get; private set; } = MissionType.None;

    /// <summary>현재 미션 데이터 참조. 비레벨이면 null.</summary>
    public MissionData CurrentMission { get; private set; }

    private void Awake()
    {
        _boardManager = GetComponent<BoardManager>();
        BuildSpriteLookup();
        _boardManager.SetMissionSpriteResolver(ResolveSprite);
        CacheCurrentMission();

        if (LevelSessionContext.IsActive)
            PrepareFromSelectedMission();
    }

    /// <summary>BoardManager.GenerateBoard 전에 호출되어 보드 크기를 레이아웃에 맞춘다.</summary>
    public void PrepareFromSelectedMission()
    {
        if (_boardManager == null)
            return;

        CacheCurrentMission();

        if (CurrentMission == null)
            return;

        _boardManager.PrepareBoardSizeFromLayout(CurrentMission);
    }

    /// <summary>선택된 미션의 MissionData를 보드에 적용하고 DoTween 등장 연출을 재생한다.</summary>
    public void ApplyMissionLayout()
    {
        if (_boardManager == null)
            return;

        CacheCurrentMission();

        if (CurrentMission == null)
        {
            Debug.LogWarning("[MissionBoardController] 선택된 레벨에 MissionData가 없습니다.", this);
            return;
        }

        if (_missionSpritePalette == null)
            Debug.LogWarning("[MissionBoardController] Mission Sprite Palette가 없습니다. 레이아웃 스프라이트를 찾지 못할 수 있습니다.", this);

        _boardManager.ApplyBoardLayout(CurrentMission, ResolveSprite);
        _boardManager.PlayOccupiedCellsAppear(_appearDuration);

        GrassSpreadController grassSpread = GetComponent<GrassSpreadController>();
        if (grassSpread != null)
            grassSpread.ResetMissCounter();
    }

    private void CacheCurrentMission()
    {
        CurrentMission = LevelSessionContext.GetSelectedMission();
        CurrentMissionType = CurrentMission != null ? CurrentMission.MissionType : MissionType.None;
    }

    private void BuildSpriteLookup()
    {
        _spriteByName.Clear();
        if (_missionSpritePalette == null || _missionSpritePalette.sprites == null)
            return;

        Sprite[] sprites = _missionSpritePalette.sprites;
        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite sprite = sprites[i];
            if (sprite == null)
                continue;

            if (!_spriteByName.ContainsKey(sprite.name))
                _spriteByName.Add(sprite.name, sprite);
        }
    }

    private Sprite ResolveSprite(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName))
            return null;

        if (_spriteByName.TryGetValue(spriteName, out Sprite sprite))
            return sprite;

        // ice02 → ice02_0 처럼 접두어가 같은 스프라이트 허용
        foreach (KeyValuePair<string, Sprite> pair in _spriteByName)
        {
            if (pair.Key.StartsWith(spriteName, System.StringComparison.OrdinalIgnoreCase))
                return pair.Value;
        }

        return null;
    }
}
