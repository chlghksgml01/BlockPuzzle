/// <summary>보석 종류.</summary>
public enum GemType
{
    Pentagon,
    Square,
    Star
}

/// <summary>보석 종류별 목표 개수.</summary>
[System.Serializable]
public struct GemTargetInfo
{
    public GemType gemType;
    public int count;
}
