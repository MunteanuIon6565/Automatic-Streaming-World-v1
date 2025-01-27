#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor;
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace ThisProject.__Project.Scripts.Optimization
{
    public class MiniChunkStreaming : MonoBehaviour
    {
        public static Dictionary<string, MiniChunkStreaming> MiniChunksStreaming = new Dictionary<string, MiniChunkStreaming>();
        
        
        
        #region Variables    
        
        [Header("<color=yellow>This GameObject contains coordonates for spawn chunk!</color>")]
        [Header("Unique Identifier For Chunk")]
        [SerializeField] private string _idUnique = "";
        [SerializeField, Tooltip("If this object is not prefab")] private bool _isItEmptyChunk;
        [Header("Points For Calc Distance")]
        [SerializeField] private Transform _targetPoint;
        [Header("Distance Settings")]
        [SerializeField,Tooltip("Min distance to spawn Prefab")] private float _minDistanceShow;
        [SerializeField,Tooltip("Max distance to delete Prefab")] private float _maxDistanceShow;
        [Header("References")]
        [SerializeField] private AssetReferenceGameObject _chunkPrefab;
        [FormerlySerializedAs("_startDelayBetweenCheckDistance")]
        [Header("Check Distance Update Time")]
        [SerializeField, Range(0,60)] private float _startDelayCheckDistance;
        [SerializeField, Range(0.1f,60)] private float _delayBetweenCheckDistance;
        
        private bool _isInitFromConstructor;
        private bool _isStartClonePrefab;
        private GameObject _spawnedChunk;
        private GameObject _emptyPointForSpawnChunk;
        
        #endregion
        
        

        #region Public Methods

        public async void CheckDistance()
        {
            if (_isStartClonePrefab) return;
            _isStartClonePrefab = true;

            if (GetDistance <= _minDistanceShow)
            {
                if (_chunkPrefab == null)
                    Debug.LogError("_chunkPrefab is null");
                else if (_spawnedChunk == null)
                    _spawnedChunk = await Addressables.InstantiateAsync
                        (_chunkPrefab, 
                            transform.position, 
                            transform.rotation).Task;
            }
            else if (GetDistance > _maxDistanceShow)
            {
                if (_spawnedChunk) Destroy(_spawnedChunk);
            }

            _isStartClonePrefab = false;
        }

        public void Constructor(
            string _idUnique,
            Transform _targetPoint,
            float _minDistanceShow,
            float _maxDistanceShow,
            AssetReferenceGameObject _chunkPrefab,
            float _delayBetweenCheckDistance,
            float _startDelayCheckDistance,
            GameObject _spawnedChunk,
            bool isUseEditorTool = false
            )
        {
            this._idUnique = _idUnique;
            this._targetPoint = _targetPoint;
            this._minDistanceShow = _minDistanceShow;
            this._maxDistanceShow = _maxDistanceShow;
            this._chunkPrefab = _chunkPrefab;
            this._delayBetweenCheckDistance = _delayBetweenCheckDistance;
            this._startDelayCheckDistance = _startDelayCheckDistance;
            this._spawnedChunk = _spawnedChunk;

            if (!isUseEditorTool)
            {
                this._isInitFromConstructor = true;

                Initialize();
            }
        }

        #endregion
        
        
        
        #region Private Methods

        private float GetDistance => Vector3.Distance(transform.position, _targetPoint.position);

        private void CreateEmptyPointForSpawnChunk()
        {
            if (_emptyPointForSpawnChunk == null)
            {
                _emptyPointForSpawnChunk = new GameObject();
                _emptyPointForSpawnChunk.name = transform.name;
                _emptyPointForSpawnChunk.transform.position = transform.position;
                _emptyPointForSpawnChunk.transform.rotation = transform.rotation;
                
                _emptyPointForSpawnChunk.AddComponent<MiniChunkStreaming>()
                    .Constructor( 
                        _idUnique, 
                        _targetPoint, 
                        _minDistanceShow,
                        _maxDistanceShow,
                        _chunkPrefab,
                        _delayBetweenCheckDistance,
                        _startDelayCheckDistance,
                        gameObject
                        );

                this._idUnique = "";
                
                Destroy(this);
            }
        }

        private void Initialize()
        {
            if (!MiniChunksStreaming.ContainsKey(this._idUnique)) 
                MiniChunksStreaming.Add(this._idUnique, this);
            
            InvokeRepeating(
                nameof(CheckDistance),
                _startDelayCheckDistance, 
                _delayBetweenCheckDistance + Random.Range(0.0f,_delayBetweenCheckDistance * 0.2f)
            );
        }

        #endregion



        #region UNITY FUNCTIONAL

        private void Start()
        {
            if (_isItEmptyChunk) _isInitFromConstructor = true;
            
            if (_isInitFromConstructor)
            {
                Initialize();
            }
            else if (!MiniChunksStreaming.ContainsKey(_idUnique))
            {
                CreateEmptyPointForSpawnChunk();
            }
            
        }

        private void OnDestroy()
        {
            MiniChunksStreaming.Remove(this._idUnique);
        }


#if UNITY_EDITOR
        
        private void OnValidate()
        {
            if (_minDistanceShow > _maxDistanceShow) 
                _maxDistanceShow = _minDistanceShow;
            
            if (0 > _maxDistanceShow) 
                _maxDistanceShow = 0;
            
            if (0 > _minDistanceShow) 
                _minDistanceShow = 0;
            
            _targetPoint ??= Camera.main.transform;
            
            if ( IsPrefab() && string.IsNullOrEmpty(_chunkPrefab.AssetGUID) )
            {
                SetPrefabAsAddressable();
            };

            _idUnique = GenerateUId(_idUnique);
        }
        
        
        
        private bool IsPrefab()
        {
            return PrefabUtility.IsPartOfPrefabAsset(gameObject);
        }

        private void SetPrefabAsAddressable()
        {
            string assetPath = AssetDatabase.GetAssetPath(gameObject);
        
            _chunkPrefab = new AssetReferenceGameObject(AssetDatabase.AssetPathToGUID(assetPath));

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings != null && settings.FindAssetEntry(_chunkPrefab.AssetGUID) == null)
            {
                var entry = settings.CreateOrMoveEntry(_chunkPrefab.AssetGUID, settings.DefaultGroup);
                entry.address = gameObject.name;
                Debug.Log($"Prefab-ul {gameObject.name} a fost adăugat la Addressables cu adresa: {entry.address}");
            }
        }
        
        
        
        private string GenerateUniqueID(int min = 11, int max = 17)
        {
            int length = UnityEngine.Random.Range(min, max);
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            string unique_id = "";
            for (int i = 0; i < length; i++)
            {
                unique_id += chars[UnityEngine.Random.Range(0, chars.Length - 1)];
            }
            return unique_id;
        }
        private string GenerateUId(string uid)
        {
            return uid.Equals("") || uid.Equals(null) ? GenerateUniqueID() : uid;
        }
        
        [ContextMenu("Create Empty Point For Spawn Chunk")]
        private void CreateEmptyPointForSpawnChunkEditor()
        {
            _emptyPointForSpawnChunk = new GameObject();
            _emptyPointForSpawnChunk.name = transform.name;
            _emptyPointForSpawnChunk.transform.position = transform.position;
            _emptyPointForSpawnChunk.transform.rotation = transform.rotation;
                
            _emptyPointForSpawnChunk.AddComponent<MiniChunkStreaming>()
                .Constructor( 
                    _idUnique, 
                    _targetPoint, 
                    _minDistanceShow,
                    _maxDistanceShow,
                    _chunkPrefab,
                    _delayBetweenCheckDistance,
                    _startDelayCheckDistance,
                    gameObject,
                    true
                );
        }
        
#endif

        #endregion
    }
}