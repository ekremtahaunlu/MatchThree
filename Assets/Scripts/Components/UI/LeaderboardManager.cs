using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LeaderboardEntry
{
  public string playerName;
  public int score;
}

public class LeaderboardManager : MonoBehaviour
{
  private const string LeaderboardKey = "Leaderboard";
  private const int MaxEntries = 10;

  public static List<LeaderboardEntry> Leaderboard { get; private set; } = new List<LeaderboardEntry>();

  private void Awake()
  {
    LoadLeaderboard();
  }

  public static void AddEntry(string playerName, int score)
  {
    Leaderboard.Add(new LeaderboardEntry { playerName = playerName, score = score });
    Leaderboard.Sort((a, b) => b.score.CompareTo(a.score));

    if (Leaderboard.Count > MaxEntries)
    {
      Leaderboard.RemoveAt(Leaderboard.Count - 1);
    }

    SaveLeaderboard();
  }

  private static void SaveLeaderboard()
  {
    string json = JsonUtility.ToJson(new SerializableList<LeaderboardEntry> { Items = Leaderboard });
    PlayerPrefs.SetString(LeaderboardKey, json);
    PlayerPrefs.Save();
  }

  private void LoadLeaderboard()
  {
    if (PlayerPrefs.HasKey(LeaderboardKey))
    {
      string json = PlayerPrefs.GetString(LeaderboardKey);
      SerializableList<LeaderboardEntry> loadedList = JsonUtility.FromJson<SerializableList<LeaderboardEntry>>(json);
      Leaderboard = loadedList.Items;
    }
  }
}

[System.Serializable]
public class SerializableList<T>
{
  public List<T> Items;
}