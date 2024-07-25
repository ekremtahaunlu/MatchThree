/*using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class LeaderboardEntry
{
  public string playerName;
  public int score;
}

public class LeaderboardManager : MonoBehaviour
{
  private const string LeaderboardKey = "Leaderboard";
  private const int MaxEntries = 10;
  
  public static LeaderboardManager Instance { get; private set; }
  
  private List<LeaderboardEntry> leaderboard;

  private void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
      DontDestroyOnLoad(gameObject);
    }
    else
    {
      Destroy(gameObject);
    }
    //LoadLeaderboard();
  }

  public void AddEntry(string playerName, int score)
  {
    leaderboard.Add(new LeaderboardEntry { playerName = playerName, score = score });
    leaderboard = leaderboard.OrderByDescending(e => e.score).Take(MaxEntries).ToList();
    SaveLeaderboard();
  }

  public List<LeaderboardEntry> GetLeaderboard()
  {
    return leaderboard;
  }

  private void LoadLeaderboard()
  {
    string json = PlayerPrefs.GetString(LeaderboardKey, "[]");
    leaderboard = JsonUtility.FromJson<List<LeaderboardEntry>>(json) ?? new List<LeaderboardEntry>();
  }

  private void SaveLeaderboard()
  {
    string json = JsonUtility.ToJson(leaderboard);
    PlayerPrefs.SetString(LeaderboardKey, json);
    PlayerPrefs.Save();
  }
}*/