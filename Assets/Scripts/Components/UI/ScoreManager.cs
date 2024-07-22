using System;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
  private static int _currentScore;
  private static int _highScore;
  //public static event Action<int> OnScoreChanged;

  public void ChangeScore(int newScore)
  {
    //OnScoreChanged?.Invoke(newScore);
  }

  public static int CurrentScore 
  { 
    get => _currentScore;
    private set
    {
      if (_currentScore != value)
      {
        _currentScore = value;
        //OnScoreChanged?.Invoke(_currentScore);
      }
    }
  }

  public static int HighScore 
  { 
    get => _highScore;
    private set
    {
      if (_highScore != value)
      {
        _highScore = value;
        OnHighScoreChanged?.Invoke(_highScore);
      }
    }
  }

  public event Action<int> OnScoreChanged;
  public static event Action<int> OnHighScoreChanged;

  public static void AddScore(int score)
  {
    CurrentScore += score;
    if (CurrentScore > HighScore)
    {
      HighScore = CurrentScore;
    }
  }

  public static void ResetScore()
  {
    CurrentScore = 0;
  }

  public static void LoadHighScore()
  {
    HighScore = PlayerPrefs.GetInt("HighScore", 0);
  }

  public static void SaveHighScore()
  {
    PlayerPrefs.SetInt("HighScore", HighScore);
    PlayerPrefs.Save();
  }

  public static void ResetHighScore()
  {
    HighScore = 0;
    PlayerPrefs.SetInt("HighScore", HighScore);
    PlayerPrefs.Save();
  }
}