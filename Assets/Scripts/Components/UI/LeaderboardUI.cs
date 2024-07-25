/*using UnityEngine;
using UnityEngine.UI;

public class LeaderboardUI : MonoBehaviour
{
  public Text[] entryTexts;
  [SerializeField] private Button closeButton;
  private MainMenu mainMenu;

  private LeaderboardManager leaderboardManager;
  public static LeaderboardUI Instance { get; private set; }
  
  private void Awake()
  {
    mainMenu = FindObjectOfType<MainMenu>();
    if (mainMenu == null)
    {
      Debug.LogError("MainMenu bulunamadı! Lütfen sahnede olduğundan emin olun.");
    }

    closeButton.onClick.AddListener(Hide);
  }  
  
  private void Start()
  {
    leaderboardManager = FindObjectOfType<LeaderboardManager>();
    closeButton.onClick.AddListener(Close);
    gameObject.SetActive(false);
  }

  public void Show()
  {
    gameObject.SetActive(true);
    UpdateLeaderboard();
  }
  
  public void Hide()
  {
    gameObject.SetActive(false);
    if (mainMenu != null)
    {
      mainMenu.OnLeaderboardClosed();
    }
  }

  private void Close()
  {
    gameObject.SetActive(false);
  }

  private void UpdateLeaderboard()
  {
    var entries = leaderboardManager.GetLeaderboard();
    for (int i = 0; i < entryTexts.Length; i++)
    {
      if (i < entries.Count)
      {
        entryTexts[i].text = $"{i + 1}. {entries[i].playerName}: {entries[i].score}";
      }
      else
      {
        entryTexts[i].text = $"{i + 1}. ---";
      }
    }
  }
}*/