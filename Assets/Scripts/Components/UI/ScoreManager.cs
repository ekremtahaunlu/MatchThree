using System;
using Events;
using Extensions.Unity.MonoHelper;
using UnityEngine;
using Zenject;

public class ScoreManager : EventListenerMono
{
  [Inject] private UIEvents UIEvents { get; set; }
  [Inject] private MainSceneEvents MainSceneEvents { get; set; }
  [Inject] private GridEvents GridEvents { get; set; }
  
  private int _currentScore;
  private int _highScore;
  private int _scoreMultiplier = 1;

  private int CurrentScore 
  { 
    get => _currentScore;
    set
    {
      if (_currentScore != value)
      {
        _currentScore = value;
        UIEvents.ScoreChanged?.Invoke(_currentScore);
      }
    } 
  }

  private int HighScore
  { 
    get => _highScore;
    set
    {
      if (_highScore != value)
      {
        _highScore = value;
        UIEvents.HighScoreChanged?.Invoke(_highScore);
      }
    }
  }

  private void AddScore(int score)
  {
    CurrentScore += score * _scoreMultiplier;
    //CurrentScore += score;
    if (CurrentScore > HighScore)
    {
      HighScore = CurrentScore;
    }
    
    SaveHighScore();
  }

  private void Start()
  {
    LoadHighScore();
    ResetScore();
  }

  private void ResetScore()
  {
    CurrentScore = 0;
  }

  private void LoadHighScore()
  {
    HighScore = PlayerPrefs.GetInt("HighScore", 0);
  }

  private void SaveHighScore()
  {
    PlayerPrefs.SetInt("HighScore", HighScore);
    PlayerPrefs.Save();
  }

  protected override void RegisterEvents()
  {
    MainSceneEvents.MainSceneStart += OnMainSceneStart;
    GridEvents.MatchGroupDespawn += OnMatchGroupDespawn;
    GridEvents.PowerUpDestroyScore += OnPowerUpDestroyScore;
    GridEvents.GameOver += OnGameOver;
  }

  private void OnGameOver()
  {
    SaveHighScore();
  }

  private void OnPowerUpDestroyScore(int arg0)
  {
    AddScore(arg0);
  }

  private void OnMatchGroupDespawn(int matchCount)
  {
    var scoreToAdd = matchCount * _scoreMultiplier;
    
    AddScore(scoreToAdd);
  }

  private void OnMainSceneStart()
  {
    LoadHighScore();
  }

  protected override void UnRegisterEvents()
  {
    MainSceneEvents.MainSceneStart -= OnMainSceneStart;
    GridEvents.MatchGroupDespawn -= OnMatchGroupDespawn;  
    GridEvents.PowerUpDestroyScore -= OnPowerUpDestroyScore;
    GridEvents.GameOver -= OnGameOver;
  }
}