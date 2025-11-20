using UnityEngine;

// BlockSlot: 블럭을 보관/생성하는 슬롯(간단 구조).
// 슬롯은 블럭 프리팹을 보관하고 필요 시 Instantiate -> Scene에 배치.
public class BlockSlot : MonoBehaviour
{
    public GameObject _blockPrefab; // DraggableBlock 프리팹
    public Transform _spawnParent;

    // 슬롯에서 블록 생성 및 초기화
    public GameObject SpawnBlock()
    {
        if (_blockPrefab == null) return null;
        var go = Instantiate(_blockPrefab, _spawnParent ? _spawnParent : transform, false);
        return go;
    }

    // 슬롯에 블록 반환 (간단 구현)
    public void ReturnBlock(GameObject block)
    {
        if (block == null) return;
        block.transform.SetParent(_spawnParent ? _spawnParent : transform, false);
        // 추가: 리셋(위치/상태) 처리 가능
    }
}
