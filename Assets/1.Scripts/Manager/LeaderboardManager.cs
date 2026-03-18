using BackEnd;
using LitJson;
using System;
using UnityEngine;

public class LeaderboardManager : Singleton<LeaderboardManager>
{
    private const string RankUuid = "019cffa2-ab30-7c69-8b25-0025d37e6deb";

    public static event Action<JsonData> OnRankDataReceived;

    private void OnEnable() => SaveManager.OnSyncSucceed += GetRank;
    private void OnDisable() => SaveManager.OnSyncSucceed -= GetRank;

    public void GetRank()
    {
        Backend.URank.User.GetRankList(RankUuid, 10, (bro) =>
        {
            JsonData rankData = new JsonData();

            if (bro.IsSuccess())
            {
                rankData = bro.FlattenRows();
                Debug.Log("Ranking lookup succeeded");
            }
            else
            {
                Debug.LogError("Ranking lookup failed : " + bro.GetErrorCode());
            }

            OnRankDataReceived?.Invoke(rankData);
        });
    }
}