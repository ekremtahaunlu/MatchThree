using System;
using UnityEngine;
using UnityEngine.Events;

namespace Events
{
  public class GridEvents
  {
    public UnityAction<Bounds> GridLoaded;
    public UnityAction InputStart;
    public UnityAction InputStop;
    public UnityAction<int> MatchGroupDespawn;
    public UnityAction<int> ScoreMultiChanged;
    public UnityAction<int> PowerUpDestroyScore;
    public UnityAction GameOver;
    /// <summary>
    /// Enter prefab to get instance, for zenject Container.IntantiatePrefab method to inject prefabs that are instantiated
    /// </summary>
    public Func<GameObject, GameObject> InsRequest;
  }
}