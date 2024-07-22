﻿using System;
using DG.Tweening;
using Extensions.DoTween;
using Extensions.Unity;
using UnityEngine;

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
        public MonoPool MyPool{get;set;}
        public ITweenContainer TweenContainer{get;set;}
        public bool ToBeDestroyed{get;set;}
        public bool IsPowerUp { get; set; }
        
        [SerializeField] private AudioClip moveSound;
        [SerializeField] private AudioClip tileMovementSound;
        private AudioSource audioSource;

        public enum TileType { Normal, PowerUp }
            public TileType type = TileType.Normal;
            public int colorIndex;

        private void Awake()
        {
            TweenContainer = TweenContain.Install(this);
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        private void OnDisable()
        {
            TweenContainer.Clear();
        }

        private void OnMouseDown() {}

        void ITileGrid.SetCoord(Vector2Int coord)
        {
            _coords = coord;
        }

        void ITileGrid.SetCoord(int x, int y)
        {
            _coords = new Vector2Int(x, y);
        }

        public void AfterCreate() {}

        public void BeforeDeSpawn()
        {
        }

        public void TweenDelayedDeSpawn(Func<bool> onComplete) {}

        public void AfterSpawn()
        {
            ToBeDestroyed = false;
            //RESET METHOD (Resurrect)
        }

        public void Teleport(Vector3 worldPos)
        {
            _transform.position = worldPos;
        }

        public void Construct(Vector2Int coords) {_coords = coords;}

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
            
            if (!IsPowerUp && tileMovementSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(tileMovementSound);
            }

            TweenContainer.AddTween = _transform.DOMove(worldPos, 0.3f).SetEase(Ease.OutQuad);

            TweenContainer.AddedTween.onComplete += onComplete;

            return TweenContainer.AddedTween;
        }

        public Sequence DoHint(Vector3 worldPos, TweenCallback onComplete = null)
        {
            _spriteRenderer.sortingOrder = EnvVar.HintSpriteLayer;
            
            Vector3 lastPos = _transform.position;
            
            TweenContainer.AddSequence = DOTween.Sequence();
            
            TweenContainer.AddedSeq.Append(_transform.DOMove(worldPos, 1f));
            TweenContainer.AddedSeq.Append(_transform.DOMove(lastPos, 1f));

            TweenContainer.AddedSeq.onComplete += onComplete;
            TweenContainer.AddedSeq.onComplete += delegate
            {
                _spriteRenderer.sortingOrder = EnvVar.TileSpriteLayer;
            };
            return TweenContainer.AddedSeq;
        }
    }

    public interface ITileGrid
    {
        void SetCoord(Vector2Int coord);
        void SetCoord(int x, int y);
    }
}