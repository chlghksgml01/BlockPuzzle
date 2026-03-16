public class SaveManager : Singleton<SaveManager>
{
    private int _bestScore = 8593; // 임시
    public int BestScore => _bestScore;
}