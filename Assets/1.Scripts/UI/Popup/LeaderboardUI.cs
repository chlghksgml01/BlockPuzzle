using LitJson;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardUI : BasePopupUI
{
    [Header("Leaderboard UI Reference")]
    [SerializeField] private GameObject _leaderboardItemPrefab;
    [SerializeField] private Transform _content;
    [SerializeField] private TMP_Text _loadingText;

    private List<GameObject> _instantiatedItems = new List<GameObject>();

    private void OnEnable()
    {
        LeaderboardManager.OnRankDataReceived += DrawRankList;
        GoogleLoginManager.OnLoginSucceed += LoginFailedText;
    }

    private void OnDisable()
    {
        LeaderboardManager.OnRankDataReceived -= DrawRankList;
        GoogleLoginManager.OnLoginSucceed -= LoginFailedText;
    }

    // 버튼 클릭 시 호출
    public override void Open()
    {
        base.Open();
        _loadingText.text = "Loading...";
        ClearOldItems();

        GoogleLoginManager.Instance.StartGoogleLogin();
    }

    private void DrawRankList(JsonData data)
    {
        if (data.Count == 0)
        {
            _loadingText.text = "No Data";
        }
        else
        {
            _loadingText.text = "";
            for (int i = 0; i < data.Count; i++)
            {
                Debug.Log(data[i].ToJson());
                string rank = data[i]["rank"].ToString() + ".";
                string nickname = data[i].ContainsKey("nickname") ? data[i]["nickname"].ToString() : "Player";
                string score = data[i]["score"].ToString();

                GameObject item = Instantiate(_leaderboardItemPrefab, _content);
                item.GetComponent<LeaderboardItem>().SetData(rank, nickname, score);

                _instantiatedItems.Add(item);
            }
        }
    }

    private void ClearOldItems()
    {
        foreach (var item in _instantiatedItems)
        {
            if (item != null) Destroy(item);
        }
        _instantiatedItems.Clear();
    }

    private void LoginFailedText(bool isSucceed)
    {
        if (!isSucceed)
        {
            _loadingText.text = "Login Failed";
        }
    }
}