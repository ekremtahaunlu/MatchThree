using UnityEngine;
using TMPro;

public class LeaderboardUI : MonoBehaviour
{
  [SerializeField] private Transform leaderboardEntryContainer;
  [SerializeField] private GameObject leaderboardEntryPrefab;

  private void OnEnable()
  {
    UpdateLeaderboardUI();
  }

  private void UpdateLeaderboardUI()
  {
    foreach (Transform child in leaderboardEntryContainer)
    {
      Destroy(child.gameObject);
    }

    foreach (LeaderboardEntry entry in LeaderboardManager.Leaderboard)
    {
      GameObject entryObject = Instantiate(leaderboardEntryPrefab, leaderboardEntryContainer);
      TextMeshProUGUI[] texts = entryObject.GetComponentsInChildren<TextMeshProUGUI>();
      texts[0].text = entry.playerName;
      texts[1].text = entry.score.ToString();
    }
  }
}