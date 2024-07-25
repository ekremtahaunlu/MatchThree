﻿using System;
using DG.Tweening;
using Events;
using Extensions.DoTween;
using Extensions.Unity;
using UnityEngine;
using Zenject;

namespace Components
{
	public class Tile : MonoBehaviour, ITileGrid, IPoolObj, ITweenContainerBind
	{
		public Vector2Int Coords => _coords;
		public int ID => _id;
		[SerializeField] private Vector2Int _coords;
		[SerializeField] private int _id;
		[SerializeField] private SpriteRenderer _spriteRenderer;
		[SerializeField] private Transform _transform;
		public MonoPool MyPool { get; set; }
		public ITweenContainer TweenContainer { get; set; }
		public bool ToBeDestroyed { get; set; }
		public bool IsPowerUp { get; set; }

		[Inject] private AudioEvents AudioEvents { get; set; }

		public enum TileType
		{
			Normal,
			PowerUp
		}

		public TileType type = TileType.Normal;

		private void Awake()
		{
			TweenContainer = TweenContain.Install(this);
		}

		private void OnDisable()
		{
			TweenContainer.Clear();
		}

		private void OnMouseDown()
		{
		}

		void ITileGrid.SetCoord(Vector2Int coord)
		{
			_coords = coord;
		}

		void ITileGrid.SetCoord(int x, int y)
		{
			_coords = new Vector2Int(x, y);
		}

		public void AfterCreate()
		{
		}

		public void BeforeDeSpawn()
		{
		}

		public void TweenDelayedDeSpawn(Func<bool> onComplete)
		{
		}

		public void AfterSpawn()
		{
			ToBeDestroyed = false;
			//RESET METHOD (Resurrect)
		}

		public void Teleport(Vector3 worldPos)
		{
			_transform.position = worldPos;
		}

		public void Construct(Vector2Int coords)
		{
			_coords = coords;
		}

		public Tween DoMove(Vector3 worldPos, TweenCallback onComplete = null)
		{
			/*if (moveSound != null)
			{
			    audioSource.PlayOneShot(moveSound);
			}

			//TweenContainer.AddTween = _transform.DOMove(worldPos, 1f);
			TweenContainer.AddTween = _transform.DOMove(worldPos, 0.3f).SetEase(Ease.OutQuad);

			TweenContainer.AddedTween.onComplete += onComplete;

			return TweenContainer.AddedTween;*/

			if (!IsPowerUp) AudioEvents.PlayMove?.Invoke();

			TweenContainer.AddTween = _transform.DOMove(worldPos, 0.3f).SetEase(Ease.OutQuad);

			TweenContainer.AddedTween.onComplete += onComplete;
			return TweenContainer.AddedTween;
		}

		public Sequence DoHint(Vector3 worldPos, TweenCallback onComplete = null)
		{
			_spriteRenderer.sortingOrder = EnvVar.HintSpriteLayer;

			var lastPos = _transform.position;

			TweenContainer.AddSequence = DOTween.Sequence();

			TweenContainer.AddedSeq.Append(_transform.DOMove(worldPos, 1f));
			TweenContainer.AddedSeq.Append(_transform.DOMove(lastPos, 1f));

			TweenContainer.AddedSeq.onComplete += onComplete;
			TweenContainer.AddedSeq.onComplete += delegate { _spriteRenderer.sortingOrder = EnvVar.TileSpriteLayer; };
			return TweenContainer.AddedSeq;
		}
	}

	public interface ITileGrid
	{
		void SetCoord(Vector2Int coord);
		void SetCoord(int x, int y);
	}
}