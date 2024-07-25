using Events;
using Extensions.Unity.MonoHelper;
using TMPro;
using UnityEngine;
using Zenject;

public class ScoreUI : EventListenerMono
{
  public TextMeshProUGUI currentScoreText;
  public TextMeshProUGUI highScoreText;
  
  [Inject] private UIEvents UIEvents { get; set; }
  
  private void Start()
  {
    if (currentScoreText == null || highScoreText == null) 
    {
      Debug.LogError("Text components are not assigned!");
      return;
    }
  }

  private void UpdateCurrentScore(int score)
  {
    if (currentScoreText != null)
    {
      currentScoreText.text = "Score: " + score;
    }
  }

  private void UpdateHighScore(int highScore)
  {
    if (highScoreText != null)
    {
      highScoreText.text = "High Score: " + highScore;
    }
  }

  protected override void RegisterEvents()
  {
    UIEvents.ScoreChanged += UpdateCurrentScore;
    UIEvents.HighScoreChanged += UpdateHighScore;
    
  }

  protected override void UnRegisterEvents()
  {
    UIEvents.ScoreChanged -= UpdateCurrentScore;
    UIEvents.HighScoreChanged -= UpdateHighScore;
  }
}