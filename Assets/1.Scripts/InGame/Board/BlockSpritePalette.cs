using UnityEngine;

[CreateAssetMenu(menuName = "BlockPuzzle/Block Sprite Palette", fileName = "BlockSpritePalette")]
public class BlockSpritePalette : ScriptableObject
{
    [Tooltip("보드 레이아웃 에디터에서 사용할 블록 스프라이트 목록")]
    public Sprite[] sprites = new Sprite[0];
}
