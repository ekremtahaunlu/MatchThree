using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Events;
using Extensions.DoTween;
using Extensions.System;
using Extensions.Unity;
using Settings;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Zenject;
using Sequence = DG.Tweening.Sequence;

namespace Components
{
	public partial class GridManager : SerializedMonoBehaviour, ITweenContainerBind
	{
		[Inject] private InputEvents InputEvents { get; set; }
		[Inject] private GridEvents GridEvents { get; set; }

		[BoxGroup(Order = 999)]
#if UNITY_EDITOR
		[TableMatrix(SquareCells = true, DrawElementMethod = nameof(DrawTile))]
#endif
		[OdinSerialize]
		private Tile[,] _grid;

		[SerializeField] private int _gridSizeX;
		[SerializeField] private int _gridSizeY;
		[SerializeField] private Bounds _gridBounds;
		[SerializeField] private Transform _transform;
		[SerializeField] private List<GameObject> _tileBGs = new();
		[SerializeField] private List<GameObject> _gridBorders = new();
		[SerializeField] private Transform _bGTrans;
		[SerializeField] private Transform _borderTrans;
		private Tile _selectedTile;
		private Vector3 _mouseDownPos;
		private Vector3 _mouseUpPos;
		private List<MonoPool> _tilePoolsByPrefabID;
		private MonoPool _tilePool0;
		private MonoPool _tilePool1;
		private MonoPool _tilePool2;
		private MonoPool _tilePool3;
		private Tile[,] _tilesToMove;
		[SerializeField] private List<List<Tile>> _lastMatches;
		private Tile _hintTile;
		private GridDir _hintDir;
		private Sequence _hintTween;
		private Coroutine _destroyRoutine;
		private Coroutine _hintRoutine;
		[SerializeField] private int _scoreMulti;
		private Settings _mySettings;
		public ITweenContainer TweenContainer { get; set; }
		[Inject] private ProjectSettings ProjectSettings { get; set; }
		[Inject] private AudioEvents AudioEvents { get; set; }
		
		[SerializeField] private ScoreManager _scoreManager;

		private Dictionary<int, MonoPool> _powerupPoolsByPrefabID;

		[SerializeField] private List<GameObject> _tilePowerupPrefabs;
		[SerializeField] private List<int> _powerupPrefabIDs;

		private const int horizontalPowerup = 2;
		private const int verticalBombPowerup = 4;
		private const int bombPowerup = 5;

		private bool _horizontalPowerupPresent = false;
		private bool _verticalPowerupPresent = false;
		private bool _bombPowerupPresent = false;
		
		private void Awake()
		{
			_scoreManager = FindObjectOfType<ScoreManager>();
			_mySettings = ProjectSettings.GridManagerSettings;
			_tilePoolsByPrefabID = new List<MonoPool>();
			_powerupPoolsByPrefabID = new Dictionary<int, MonoPool>();

			for (var prefabId = 0; prefabId < _mySettings.PrefabIDs.Count; prefabId++)
			{
				MonoPool tilePool = new
				(
					new MonoPoolData
					(
						_mySettings.TilePrefabs[prefabId],
						10,
						_transform
					), true
				);

				_tilePoolsByPrefabID.Add(tilePool);
			}

			for (var i = 0; i < _powerupPrefabIDs.Count; i++)
			{
				var powerupId = _mySettings.TilePrefabs.Count + i;
				var powerupPool = new MonoPool(
					new MonoPoolData(
						_tilePowerupPrefabs[i],
						5,
						_transform
					), true
				);
				_powerupPoolsByPrefabID.Add(powerupId, powerupPool);
			}

			TweenContainer = TweenContain.Install(this);
		}


		private void Start()
		{
			for (var x = 0; x < _grid.GetLength(0); x++)
			for (var y = 0; y < _grid.GetLength(1); y++)
			{
				var tile = _grid[x, y];

				SpawnTile(tile.ID, _grid.CoordsToWorld(_transform, tile.Coords), tile.Coords);
				tile.gameObject.Destroy();
			}

			IsGameOver(out _hintTile, out _hintDir);
			GridEvents.GridLoaded?.Invoke(_gridBounds);
			GridEvents.InputStart?.Invoke();

		}

		private void OnMatchGroupDespawn(int matchCount)
		{
			// var baseScore = 10;
			// var scoreToAdd = baseScore * matchCount * _scoreMulti;
			// scoreToAdd = Mathf.Min(scoreToAdd, 10000);
			// _scoreManager.AddScore(scoreToAdd);
			//
			// /*if (_scoreManager.CurrentScore == _scoreManager.HighScore)
			// {
			//     leaderboardManager.AddEntry("PlayerName", _scoreManager.HighScore);
			// }*/
		}

		private void OnEnable()
		{
			RegisterEvents();
			GridEvents.MatchGroupDespawn += OnMatchGroupDespawn;
		}

		private void OnDisable()
		{
			UnRegisterEvents();
			TweenContainer.Clear();
			GridEvents.MatchGroupDespawn -= OnMatchGroupDespawn;
		}

		private bool CanMove(Vector2Int tileMoveCoord)
		{
			return _grid.IsInsideGrid(tileMoveCoord);
		}

		private bool HasMatch(Tile fromTile, Tile toTile, out List<List<Tile>> matches)
		{
			matches = new List<List<Tile>>();
			var hasMatches = false;

			var matchesAll = _grid.GetMatchesYAll(toTile);
			matchesAll.AddRange(_grid.GetMatchesXAll(toTile));

			if (matchesAll.Count > 0) matches.Add(matchesAll);

			matchesAll = _grid.GetMatchesYAll(fromTile);
			matchesAll.AddRange(_grid.GetMatchesXAll(fromTile));

			if (matchesAll.Count > 0) matches.Add(matchesAll);

			if (matches.Count > 0) hasMatches = true;

			return hasMatches;
		}

		private bool HasAnyMatches(out List<List<Tile>> matches)
		{
			matches = new List<List<Tile>>();

			foreach (var tile in _grid)
			{
				var matchesAll = _grid.GetMatchesXAll(tile);
				matchesAll.AddRange(_grid.GetMatchesYAll(tile));

				if (matchesAll.Count > 0) matches.Add(matchesAll);
			}

			matches = matches.OrderByDescending(e => e.Count).ToList();

			for (var i = 0; i < matches.Count; i++)
			{
				var match = matches[i];

				matches[i] = match.Where(e => e.ToBeDestroyed == false).DoToAll(e => e.ToBeDestroyed = true).ToList();
			}

			const int matchIndex = 2;

			matches = matches.Where(e => e.Count > matchIndex).ToList();

			return matches.Count > 0;
		}

		private bool IsGameOver(out Tile hintTile, out GridDir hintDir)
		{
			hintDir = GridDir.Null;
			hintTile = null;

			List<Tile> matches = new();

			foreach (var fromTile in _grid)
			{
				hintTile = fromTile;

				var thisCoord = fromTile.Coords;

				var leftCoord = thisCoord + Vector2Int.left;
				var topCoord = thisCoord + Vector2Int.up;
				var rightCoord = thisCoord + Vector2Int.right;
				var botCoord = thisCoord + Vector2Int.down;

				if (_grid.IsInsideGrid(leftCoord))
				{
					var toTile = _grid.Get(leftCoord);

					_grid.Swap(fromTile, toTile);

					matches = _grid.GetMatchesX(fromTile);
					matches.AddRange(_grid.GetMatchesY(fromTile));

					_grid.Swap(toTile, fromTile);

					if (matches.Count > 0)
					{
						hintDir = GridDir.Left;
						return false;
					}
				}

				if (_grid.IsInsideGrid(topCoord))
				{
					var toTile = _grid.Get(topCoord);
					_grid.Swap(fromTile, toTile);

					matches = _grid.GetMatchesX(fromTile);
					matches.AddRange(_grid.GetMatchesY(fromTile));

					_grid.Swap(toTile, fromTile);

					if (matches.Count > 0)
					{
						hintDir = GridDir.Up;
						return false;
					}
				}

				if (_grid.IsInsideGrid(rightCoord))
				{
					var toTile = _grid.Get(rightCoord);
					_grid.Swap(fromTile, toTile);

					matches = _grid.GetMatchesX(fromTile);
					matches.AddRange(_grid.GetMatchesY(fromTile));

					_grid.Swap(toTile, fromTile);

					if (matches.Count > 0)
					{
						hintDir = GridDir.Right;
						return false;
					}
				}

				if (_grid.IsInsideGrid(botCoord))
				{
					var toTile = _grid.Get(botCoord);
					_grid.Swap(fromTile, toTile);

					matches = _grid.GetMatchesX(fromTile);
					matches.AddRange(_grid.GetMatchesY(fromTile));

					_grid.Swap(toTile, fromTile);

					if (matches.Count > 0)
					{
						hintDir = GridDir.Down;
						return false;
					}

					if (matches.Count == 0)
					{
						GridEvents.GameOver?.Invoke();
						//leaderboardManager.AddEntry("PlayerName", _scoreManager.CurrentScore);
						EndGame();
						return true;
					}

					return false;
				}
			}

			return matches.Count == 0;
		}


		private void SpawnAndAllocateTiles()
		{
			_tilesToMove = new Tile[_gridSizeX, _gridSizeY];

			for (var y = 0; y < _gridSizeY; y++)
			{
				var spawnStartY = 0;

				for (var x = 0; x < _gridSizeX; x++)
				{
					Vector2Int thisCoord = new(x, y);
					var thisTile = _grid.Get(thisCoord);

					if (thisTile) continue;

					var spawnPoint = _gridSizeY;

					for (var y1 = y; y1 <= spawnPoint; y1++)
					{
						if (y1 == spawnPoint)
						{
							if (spawnStartY == 0) spawnStartY = thisCoord.y;

							//MonoPool randomPool = _tilePoolsByPrefabID.Random();

							var newTile = SpawnRegularOrPowerupTile(new Vector2Int(x, spawnPoint), thisCoord);

							_tilesToMove[thisCoord.x, thisCoord.y] = newTile;
							break;
						}

						Vector2Int emptyCoords = new(x, y1);

						var mostTopTile = _grid.Get(emptyCoords);

						if (mostTopTile)
						{
							_grid.Set(null, mostTopTile.Coords);
							_grid.Set(mostTopTile, thisCoord);

							_tilesToMove[thisCoord.x, thisCoord.y] = mostTopTile;

							break;
						}
					}
				}
			}

			StartCoroutine(RainDownRoutine());
		}

		private Tile SpawnTile(MonoPool randomPool, Vector3 spawnWorldPos, Vector2Int spawnCoords)
		{
			var newTile = randomPool.Request<Tile>();

			newTile.Teleport(spawnWorldPos);

			_grid.Set(newTile, spawnCoords);

			return newTile;
		}

		private Tile SpawnTile(int id, Vector3 worldPos, Vector2Int coords)
		{
			return SpawnTile(_tilePoolsByPrefabID[id], worldPos, coords);
		}

		private IEnumerator RainDownRoutine()
		{
			var longestDistY = 0;
			Tween longestTween = null;

			for (var y = 0; y < _gridSizeY; y++) // TODO: Should start from first tile that we are moving
			{
				var shouldWait = false;

				for (var x = 0; x < _gridSizeX; x++)
				{
					var thisTile = _tilesToMove[x, y];

					if (thisTile == false) continue;

					var thisTween = thisTile.DoMove(_grid.CoordsToWorld(_transform, thisTile.Coords));

					shouldWait = true;

					if (longestDistY < y)
					{
						longestDistY = y;
						longestTween = thisTween;
					}
				}

				if (shouldWait) yield return new WaitForSeconds(0.1f);
			}

			if (longestTween != null)
			{
				longestTween.onComplete += delegate
				{
					if (HasAnyMatches(out _lastMatches))
					{
						StartDestroyRoutine();
					}
					else
					{
						IsGameOver(out _hintTile, out _hintDir);
						GridEvents.InputStart?.Invoke();
					}
				};
			}
			else
			{
				Debug.LogWarning("This should not have happened!");
				GridEvents.InputStart?.Invoke();
			}
		}

		private void StartDestroyRoutine()
		{
			if (_destroyRoutine != null) StopCoroutine(_destroyRoutine);

			_destroyRoutine = StartCoroutine(DestroyRoutine());
		}

		private IEnumerator DestroyRoutine()
		{
			var groupCount = _lastMatches.Count;

			foreach (var matches in _lastMatches)
			{
				IncreaseScoreMulti();
				matches.DoToAll(DespawnTile);

				// TODO: Show scoreMulti text in UI as PunchScale 

				GridEvents.MatchGroupDespawn?.Invoke(matches.Count);

				yield return new WaitForSeconds(0.1f);
			}

			SpawnAndAllocateTiles();
		}

		private void DespawnTile(Tile tile)
		{
			/*_grid.Set(null, e.Coords);
			_tilePoolsByPrefabID[e.ID].DeSpawn(e); */

			_grid.Set(null, tile.Coords);
			if (_powerupPrefabIDs.Contains(tile.ID))
			{
				if (tile.ID == _powerupPrefabIDs[0])
					_horizontalPowerupPresent = false;
				else if (tile.ID == _powerupPrefabIDs[1])
					_verticalPowerupPresent = false;
				else if (tile.ID == _powerupPrefabIDs[2]) _bombPowerupPresent = false;
				_powerupPoolsByPrefabID[tile.ID].DeSpawn(tile);
				return;
			}

			_tilePoolsByPrefabID[tile.ID].DeSpawn(tile);
		}

		private void DoTileMoveAnim(Tile fromTile, Tile toTile, TweenCallback onComplete = null)
		{
			var fromTileWorldPos = _grid.CoordsToWorld(_transform, fromTile.Coords);
			fromTile.DoMove(fromTileWorldPos);
			var toTileWorldPos = _grid.CoordsToWorld(_transform, toTile.Coords);
			toTile.DoMove(toTileWorldPos, onComplete);
		}

		private void StartHintRoutine()
		{
			if (_hintRoutine != null) StopCoroutine(_hintRoutine);

			_hintRoutine = StartCoroutine(HintRoutineUpdate());
		}

		private void StopHintRoutine()
		{
			if (_hintTile) _hintTile.Teleport(_grid.CoordsToWorld(_transform, _hintTile.Coords));

			if (_hintRoutine != null)
			{
				StopCoroutine(_hintRoutine);
				_hintRoutine = null;
			}

			_hintRoutine = StartCoroutine(HintRoutineUpdate());
		}

		private IEnumerator HintRoutineUpdate()
		{
			while (true)
			{
				yield return new WaitForSeconds(3f);
				TryShowHint();
			}
		}

		private void TryShowHint()
		{
			if (_hintTile)
			{
				Vector2Int gridMoveDir = _hintDir.ToVector();
				Vector3 gridMoveEase = gridMoveDir.ToVector3XY() * 0.66f;

				Vector3 moveCoords = _grid.CoordsToWorld(_transform, _hintTile.Coords + gridMoveDir) - gridMoveEase;

				Tween hintTween = _hintTile.DoHint(moveCoords);
			}
		}

		private void ResetScoreMulti()
		{
			SetScoreMulti(0);
		}

		private void IncreaseScoreMulti()
		{
			SetScoreMulti(Mathf.Min(_scoreMulti + 1, 5));
		}

		public int SetScoreMulti(int newMulti)
		{
			_scoreMulti = newMulti;
			GridEvents.ScoreMultiChanged?.Invoke(_scoreMulti);
			return _scoreMulti;
		}


		private Tile SpawnRegularOrPowerupTile(Vector2Int spawnPoint, Vector2Int targetCoord)
		{
			Tile newTile;
			if (ShouldSpawnPowerup(out var powerupId))
			{
				newTile = SpawnTile(_powerupPoolsByPrefabID[powerupId], _grid.CoordsToWorld(_transform, spawnPoint),
					targetCoord);
				newTile.IsPowerUp = true;
			}
			else
			{
				var randomPool = _tilePoolsByPrefabID.Random();
				newTile = SpawnTile(randomPool, _grid.CoordsToWorld(_transform, spawnPoint), targetCoord);
				newTile.IsPowerUp = false;
			}

			return newTile;
		}

		private bool ShouldSpawnPowerup(out int powerupId)
		{
			powerupId = -1;

			if (_scoreMulti == horizontalPowerup + 1 && !_horizontalPowerupPresent)
			{
				powerupId = _powerupPrefabIDs[0];
				_horizontalPowerupPresent = true;
				return true;
			}

			if (_scoreMulti == verticalBombPowerup + 1 && !_verticalPowerupPresent)
			{
				powerupId = _powerupPrefabIDs[1];
				_verticalPowerupPresent = true;
				return true;
			}

			if (_scoreMulti == bombPowerup + 1 && !_bombPowerupPresent)
			{
				Debug.LogWarning($"SCORE MULTI = {_scoreMulti}");
				powerupId = _powerupPrefabIDs[2];
				_bombPowerupPresent = true;
				return true;
			}

			return false;
		}


		private bool IsPowerupTile(Tile tile)
		{
			return _powerupPrefabIDs.Contains(tile.ID);
		}

		private void ActivatePowerup(Tile powerupTile)
		{
			var tilesToDestroy = new List<Tile>();
			var powerupPosition = _grid.CoordsToWorld(_transform, powerupTile.Coords);

			var powerupBaseScore = 50;
			var score = tilesToDestroy.Count * powerupBaseScore;
			score = Mathf.Min(score, 2000);
			GridEvents.PowerUpDestroyScore?.Invoke(score);

			if (powerupTile.ID == _powerupPrefabIDs[0])
			{
				for (var x = 0; x < _gridSizeX; x++) tilesToDestroy.Add(_grid[x, powerupTile.Coords.y]);
				_horizontalPowerupPresent = false;
				AudioEvents.PlayHorizontalPowerUp?.Invoke();
				
			}
			else if (powerupTile.ID == _powerupPrefabIDs[1])
			{
				for (var y = 0; y < _gridSizeY; y++) tilesToDestroy.Add(_grid[powerupTile.Coords.x, y]);
				_verticalPowerupPresent = false;
				AudioEvents.PlayVerticalPowerUp?.Invoke();
			}
			else if (powerupTile.ID == _powerupPrefabIDs[2])
			{
				var adjacentDirection = new Vector2Int[]
				{
					Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
					new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)
				};

				foreach (var dir in adjacentDirection)
				{
					var adjacentCoord = powerupTile.Coords + dir;
					if (_grid.IsInsideGrid(adjacentCoord)) tilesToDestroy.Add(_grid[adjacentCoord.x, adjacentCoord.y]);
				}

				_bombPowerupPresent = false;
				AudioEvents.PlayBombPowerUp?.Invoke();
			}

			powerupTile.IsPowerUp = false;

			foreach (var tile in tilesToDestroy) DespawnTile(tile);
			DespawnTile(powerupTile);

			GridEvents.MatchGroupDespawn?.Invoke(tilesToDestroy.Count);

			if (powerupTile.ID == _powerupPrefabIDs[0]) _horizontalPowerupPresent = false;
			else if (powerupTile.ID == _powerupPrefabIDs[1]) _verticalPowerupPresent = false;
			else if (powerupTile.ID == _powerupPrefabIDs[2]) _bombPowerupPresent = false;

			SetScoreMulti(1);
			score = tilesToDestroy.Count * _scoreMulti;
			GridEvents.MatchGroupDespawn?.Invoke(score);

			SpawnAndAllocateTiles();
		}

		private void EndGame()
		{
			//leaderboardManager.AddEntry("PlayerName", _scoreManager.CurrentScore);
		}

		private void RegisterEvents()
		{
			InputEvents.MouseDownGrid += OnMouseDownGrid;
			InputEvents.MouseUpGrid += OnMouseUpGrid;
			GridEvents.InputStart += OnInputStart;
			GridEvents.InputStop += OnInputStop;

			foreach (var pool in _tilePoolsByPrefabID) pool.On_InstantiateRequest += OnInstantiateRequest;

			foreach (var pair in _powerupPoolsByPrefabID) pair.Value.On_InstantiateRequest += OnInstantiateRequest;
		}

		private GameObject OnInstantiateRequest(GameObject prefabRefAsGameObject)
		{
			var newTileGo = GridEvents.InsRequest?.Invoke(prefabRefAsGameObject);
			return newTileGo;
		}

		private void OnMouseDownGrid(Tile clickedTile, Vector3 dirVector)
		{
			_selectedTile = clickedTile;
			_mouseDownPos = dirVector;
			if (_hintTween.IsActive()) _hintTween.Complete();
		}

		private void OnMouseUpGrid(Vector3 mouseUpPos)
		{
			_mouseUpPos = mouseUpPos;

			var dirVector = mouseUpPos - _mouseDownPos;

			if (_selectedTile)
			{
				if (IsPowerupTile(_selectedTile))
				{
					GridEvents.InputStop?.Invoke();

					ActivatePowerup(_selectedTile);
					return;
				}

				if (!HasAnyMatches(out _lastMatches)) ResetScoreMulti();

				var tileMoveCoord = _selectedTile.Coords + GridF.GetGridDirVector(dirVector);

				if (!CanMove(tileMoveCoord)) return;

				var toTile = _grid.Get(tileMoveCoord);

				_grid.Swap(_selectedTile, toTile);

				if (!HasAnyMatches(out _lastMatches))
				{
					GridEvents.InputStop?.Invoke();

					DoTileMoveAnim(_selectedTile, toTile,
						delegate
						{
							_grid.Swap(toTile, _selectedTile);

							DoTileMoveAnim(_selectedTile, toTile,
								delegate { GridEvents.InputStart?.Invoke(); });
						});
				}
				else
				{
					GridEvents.InputStop?.Invoke();

					DoTileMoveAnim
					(
						_selectedTile,
						toTile,
						StartDestroyRoutine
					);
				}
			}
		}

		private void OnInputStop()
		{
			StopHintRoutine();
		}

		private void OnInputStart()
		{
			StartHintRoutine();
			ResetScoreMulti();
		}

		private void UnRegisterEvents()
		{
			InputEvents.MouseDownGrid -= OnMouseDownGrid;
			InputEvents.MouseUpGrid -= OnMouseUpGrid;
			GridEvents.InputStart -= OnInputStart;
			GridEvents.InputStop -= OnInputStop;

			foreach (var pool in _tilePoolsByPrefabID) pool.On_InstantiateRequest -= OnInstantiateRequest;

			foreach (var pair in _powerupPoolsByPrefabID) pair.Value.On_InstantiateRequest -= OnInstantiateRequest;
		}

		[Serializable]
		public class Settings
		{
			public List<GameObject> TilePrefabs => _tilePrefabs;
			public List<int> PrefabIDs => _prefabIds;
			public GameObject TileBGPrefab => _tileBgPrefab;
			public GameObject BorderTopLeft => _borderTopLeft;
			public GameObject BorderTopRight => _borderTopRight;
			public GameObject BorderBotLeft => _borderBotLeft;
			public GameObject BorderBotRight => _borderBotRight;
			public GameObject BorderLeft => _borderLeft;
			public GameObject BorderRight => _borderRight;
			public GameObject BorderTop => _borderTop;
			public GameObject BorderBot => _borderBot;


			[SerializeField] private GameObject _tileBgPrefab;
			[SerializeField] private List<int> _prefabIds;
			[SerializeField] private List<GameObject> _tilePrefabs;

			[SerializeField] private GameObject _borderTopLeft;
			[SerializeField] private GameObject _borderTopRight;
			[SerializeField] private GameObject _borderBotLeft;
			[SerializeField] private GameObject _borderBotRight;
			[SerializeField] private GameObject _borderLeft;
			[SerializeField] private GameObject _borderRight;
			[SerializeField] private GameObject _borderTop;
			[SerializeField] private GameObject _borderBot;
		}
	}
}