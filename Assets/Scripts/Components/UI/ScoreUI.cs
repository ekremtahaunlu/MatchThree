using UnityEngine;
using UnityEngine.UI;

public class ScoreUI : MonoBehaviour
{
  public Text currentScoreText;
  public Text highScoreText;

  private ScoreManager scoreManager;

  void Start()
  {
    scoreManager = FindObjectOfType<ScoreManager>();

    if (scoreManager == null)
    {
      Debug.LogError("ScoreManager bulunamadı!");
      return;
    }
    
    //ScoreManager.OnScoreChanged += UpdateCurrentScore;
    ScoreManager.OnHighScoreChanged += UpdateHighScore;
    
    UpdateCurrentScore(ScoreManager.CurrentScore);
    UpdateHighScore(ScoreManager.HighScore);
  }

  void UpdateCurrentScore(int score)
  {
    currentScoreText.text = "Score: " + score;
  }

  void UpdateHighScore(int highScore)
  {
    highScoreText.text = "High Score: " + highScore;
  }
}