using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InGameSaveData
{
    public int boardSize;
    public int score;
    public int currentPlaceCount;
    public int currentComboCount;
    public List<FilledCellData> filledCells = new List<FilledCellData>();
    public List<SlotBlockData> slots = new List<SlotBlockData>();
}

[Serializable]
public class FilledCellData
{
    public int x;
    public int y;
    public string spriteName;
}

[Serializable]
public class SlotBlockData
{
    public bool hasBlock;
    public string spriteName;
    public List<Vector2IntData> offsets = new List<Vector2IntData>();
}

[Serializable]
public class Vector2IntData
{
    public int x;
    public int y;

    public Vector2IntData() { }
    public Vector2IntData(Vector2Int v)
    {
        x = v.x;
        y = v.y;
    }

    public Vector2Int ToVector2Int() => new Vector2Int(x, y);
}

public static class InGameSaveStorage
{
    private const string SaveKey = "InGameSaveDataS";

    public static void Save(InGameSaveData data)
    {
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    public static bool TryLoad(out InGameSaveData data)
    {
        data = null;
        if (!PlayerPrefs.HasKey(SaveKey))
            return false;

        string json = PlayerPrefs.GetString(SaveKey, string.Empty);
        if (string.IsNullOrEmpty(json))
            return false;

        data = JsonUtility.FromJson<InGameSaveData>(json);
        return data != null;
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }
}

