using UnityEngine;

[CreateAssetMenu(menuName = "BlockPuzzle/Block Sprite Palette", fileName = "BlockSpritePalette")]
public class BlockSpritePalette : ScriptableObject
{
    [Tooltip("미션/레이아웃용 블록 스프라이트 (stone 등). 플레이어 스폰용 BlockSprites와 분리한다.")]
    public Sprite[] sprites = new Sprite[0];
}
