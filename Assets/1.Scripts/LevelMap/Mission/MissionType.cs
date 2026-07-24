/// <summary>
/// 레벨 미션 종류. 한 미션에 Ice/Grass가 섞이지 않는다.
/// </summary>
public enum MissionType
{
    /// <summary>레벨 세션이 아니거나 미션 미정.</summary>
    None = 0,
    ScoreGoal,
    Ice,
    Grass,
    Gem
}
