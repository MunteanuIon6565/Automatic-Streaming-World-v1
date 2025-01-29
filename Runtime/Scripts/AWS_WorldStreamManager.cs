#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace AUTOMATIC_WORLD_STREAMING
{
#if UNITY_EDITOR
    [RequireComponent(typeof(AWS_ChunksSorter))]
    [ExecuteAlways]
#endif
    public class AWS_WorldStreamManager : MonoBehaviour
    {
        #region FIELDS

        public static AWS_WorldStreamManager Instance { get; private set; } = null;
        public static Vector3 OffsetOrigin { get; private set; } = Vector3.zero;

        [field: SerializeField]
        public AWS_Settings AwsSettings { get; private set; }

        [field: SerializeField]
        public AWS_AllChunksInOneWorld AwsChunks { get; private set; }
 
        [SerializeField]
        private bool autoInitializeInAwake = true;

        [SerializeField, Tooltip("By default is MainCamera")]
        private Transform targetForStream;

        public Transform TargetForStream
        {
            get
            {
                if (Application.isPlaying)
                {
                    if (!targetForStream)
                        targetForStream = Camera.main?.transform;
                }
#if UNITY_EDITOR
                else if (!Application.isPlaying && Camera.current)
                {
                    return Camera.current.transform;
                }
#endif
                return targetForStream;
            }
        }

        
        #region Check Distance Job Fileds

        private Dictionary<int, string> _indexToStringMap = new Dictionary<int, string>();
        private Dictionary<string, int> _stringToIndexMap = new Dictionary<string, int>();
        
        private NativeArray<float3> _chunkPositions;
        private NativeArray<int> _chunkIndices;

        private NativeList<int> _chunksToLoad;
        private NativeList<int> _chunksToUnload;
        
        private Dictionary<string,ChunkContainer> _chunkContainers => AwsChunks.ChunkContainers;

        #endregion

        #if UNITY_EDITOR
        
        private Dictionary<int,int> _chunksRemainToUnload = new Dictionary<int,int>();
        private Dictionary<string, string> _assetPathsCache = new Dictionary<string, string>();
        private Dictionary<string, Scene> _opennedScenesCache = new Dictionary<string, Scene>();

        public string GetAssetPath(AssetReference assetReference)
        {
            string guid = assetReference.AssetGUID;

            if (!_assetPathsCache.TryGetValue(guid, out string path))
            {
                path = AssetDatabase.GUIDToAssetPath(guid);
                _assetPathsCache[guid] = path;
            }

            return path;
        }
        
        #endif
        
        #endregion

        #region METHODS

        public void Initialize(Transform customTargetForStream = null)
        {
            if (!TargetForStream || !AwsChunks || !AwsSettings)
            {
                Debug.LogError("!TargetForStream || !AwsChunks || !AwsSettings is missing!");
                return;
            }
            
            if (Instance == null)
            {
                Instance = this;
                AwsChunks.RebuildListToDictionary();
                if (customTargetForStream)
                    targetForStream = customTargetForStream;
                
                ChunkContainers();
            }
            else if (Application.isPlaying)
            {
                Destroy(this);
            }
        }
        
        private int countFrames = 0;
        private void CheckStreamChunks()
        {
            #if UNITY_EDITOR

            countFrames++;
            if (countFrames > 1000000) countFrames = 0;
            if (countFrames % 120 == 0) return;
            
            if (EditorSceneManager.GetActiveScene().name.Equals(gameObject.scene)) 
                EditorSceneManager.SetActiveScene(gameObject.scene);
            #endif
            
            if (!TargetForStream || !AwsChunks || !AwsSettings)
            {
                Debug.LogError("!TargetForStream || !AwsChunks || !AwsSettings is missing!");

                return;
            }

            // load scenes cu prioritate mai intai cele large dupa medii dupa mici
            ProcessChunksJob();
        }
        


        
        private void ProcessChunksJob()
        {
            _chunksToLoad = new NativeList<int>(_chunkContainers.Count, Allocator.TempJob);
            _chunksToUnload = new NativeList<int>(_chunkContainers.Count, Allocator.TempJob);
            
            var chunkProcessingJob = new ChunkProcessingJob
            {
                TargetPosition = TargetForStream.position,
                MinDistance = GetMinDistanceShow(),
                MaxDistance = GetMaxDistanceShow(),
                ChunkPositions = _chunkPositions,
                ChunksToLoad = _chunksToLoad.AsParallelWriter(),
                ChunksToUnload = _chunksToUnload.AsParallelWriter()
            };

            JobHandle jobHandle = chunkProcessingJob.Schedule(_chunkContainers.Count, 64);
            jobHandle.Complete();

            foreach (var loadIndex in _chunksToLoad)
            {
                string chunkKey = _indexToStringMap[loadIndex];
                LoadChunk(_chunkContainers[chunkKey].SceneReference);
            }
            
            #if UNITY_EDITOR

            if (!Application.isPlaying)
            {
                string sceneNameChunkManager = $"{gameObject.scene.name}_";
                _chunksRemainToUnload.Clear();
                
                foreach (var openedScene in _opennedScenesCache)
                {
                    if (!openedScene.Value.IsValid())
                    {
                        _opennedScenesCache.Clear();
                        continue;
                    }
                    
                    string chunkKey = openedScene.Value.name.Replace(sceneNameChunkManager,"");
                    int indexChunk = _stringToIndexMap[chunkKey];
                    _chunksRemainToUnload.Add( indexChunk, indexChunk);
                }

                foreach (var unloadIndex in _chunksToUnload)
                {
                    if (_chunksRemainToUnload.TryGetValue(unloadIndex, out _))
                    {
                        string chunkKey = _indexToStringMap[unloadIndex];
                        UnloadChunk(_chunkContainers[chunkKey].SceneReference);
                    }
                }
                
                _chunksRemainToUnload.Clear();
            }
            else
            {
                foreach (var unloadIndex in _chunksToUnload)
                {
                    string chunkKey = _indexToStringMap[unloadIndex];
                    UnloadChunk(_chunkContainers[chunkKey].SceneReference);
                }
            }
            
            #else
            
            foreach (var unloadIndex in _chunksToUnload)
            {
                string chunkKey = _indexToStringMap[unloadIndex];
                UnloadChunk(_chunkContainers[chunkKey].SceneReference);
            }
            
            #endif


            /*_chunkPositions.Dispose();
            _chunkIndices.Dispose();*/
            _chunksToLoad.Dispose();
            _chunksToUnload.Dispose();
        }

        private void ChunkContainers()
        {
            int chunkCount = _chunkContainers.Count;
            int currentIndex = 0;

            foreach (var key in _chunkContainers.Keys)
            {
                _stringToIndexMap[key] = currentIndex;
                _indexToStringMap[currentIndex] = key;
                currentIndex++;
            }

            if (_chunkPositions.IsCreated) _chunkPositions.Dispose();
            if (_chunkIndices.IsCreated) _chunkIndices.Dispose();
            
            _chunkPositions = new NativeArray<float3>(chunkCount, Allocator.Persistent);
            _chunkIndices = new NativeArray<int>(chunkCount, Allocator.Persistent);

            int index = 0;
            foreach (var kvp in _chunkContainers)
            {
                _chunkPositions[index] = (float3)kvp.Value.WorldPosition + (float3)OffsetOrigin;
                _chunkIndices[index] = _stringToIndexMap[kvp.Key];
                index++;
            }
        }


        private float GetMinDistanceShow()
        {
#if UNITY_EDITOR
            return Application.isPlaying ? AwsSettings.MinDistanceShow : AwsSettings.MinDistanceShowEditor;
#else
            return AwsSettings.MinDistanceShow;
#endif
        }

        
        private float GetMaxDistanceShow()
        {
#if UNITY_EDITOR
            return Application.isPlaying ? AwsSettings.MaxDistanceShow : AwsSettings.MaxDistanceShowEditor;
#else
            return AwsSettings.MaxDistanceShow;
#endif
        }

        
        private bool IsSceneLoaded(AssetReference sceneReference)
        {
            SceneInstance sceneInstance = default;
            string sceneName;
            
            if (sceneReference.IsValid()) 
                sceneInstance = (SceneInstance)sceneReference.OperationHandle.Result;

            sceneName = sceneInstance.Scene.name;
            
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("Scene name could not be determined from the AssetReference.");
                return false;
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene loadedScene = SceneManager.GetSceneAt(i);
                if (loadedScene.name == sceneName)
                {
                    return true;
                }
            }

            return false;
        }

        
        private void UnloadChunk(AssetReference assetReference)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (_opennedScenesCache.TryGetValue(assetReference.editorAsset.name, out var scene))
                {
                    if (scene.isLoaded)
                    {
                        if (scene.isDirty)
                        {
                            List<Scene> scenesToUnload = new List<Scene>();

                            for (int i = 0; i < SceneManager.sceneCount; i++)
                            {
                                Scene loadedScene = SceneManager.GetSceneAt(i);
                                if (loadedScene.name != gameObject.scene.name && loadedScene.isDirty)
                                    scenesToUnload.Add(loadedScene);
                            }
                            EditorSceneManager.SaveScenes(scenesToUnload.ToArray());
                        }

                        _opennedScenesCache.Remove(scene.name);
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
                return;
            }
#endif
            if (assetReference != null && assetReference.IsValid())
            {
                Addressables.UnloadSceneAsync(assetReference.OperationHandle);
                assetReference.LoadSceneAsync(LoadSceneMode.Additive);
            }
        }
        

        private async void LoadChunk(AssetReference assetReference)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (!_opennedScenesCache.TryGetValue(assetReference.editorAsset.name, out var _))
                {
                    Scene scene = EditorSceneManager.OpenScene(GetAssetPath(assetReference), OpenSceneMode.Additive);
                    _opennedScenesCache.TryAdd( scene.name, scene);
                }

                return;
            }
#endif
            if (assetReference != null && !IsSceneLoaded(assetReference) /*&& assetReference.IsValid()*/)
            {
                if (assetReference.IsValid()) 
                    assetReference.ReleaseAsset();
                
                var sceneInstance = await assetReference.LoadSceneAsync(LoadSceneMode.Additive, false).Task;
                
                //await UniTask.WaitForFixedUpdate(); // trebuie de importat UniTask ca sa asteptam un fixed update si sa activam scena in fixed update ca sa nu avem asa mare freeze de fizica

                await sceneInstance.ActivateAsync();
            }
        }



#if UNITY_EDITOR
        
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                UnloadUnusedScenes();
            }
            void UnloadUnusedScenes()
            {
                Scene activeScene = SceneManager.GetActiveScene();

                for (int i = EditorSceneManager.sceneCount - 1; i >= 0; i--)
                {
                    Scene scene = EditorSceneManager.GetSceneAt(i);
                    
                    if (scene != activeScene && scene.isLoaded)
                        EditorSceneManager.CloseScene(scene, true);
                }
            }
        }
        
        #endif
        

        #endregion

        #region UNITY METHODS

        private void Awake()
        {
            if (autoInitializeInAwake)
                Initialize();
        }

        private IEnumerator Start()
        {
            var waitForSeconds = new WaitForSeconds(AwsSettings.LoopTimeCheckDistance);
            while (Application.isPlaying)
            {
                CheckStreamChunks();
                yield return waitForSeconds;
            }
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                AwsChunks.RebuildListToDictionary();
                ChunkContainers();
                EditorApplication.update += CheckStreamChunks;
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            }
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorApplication.update -= CheckStreamChunks;
            }
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            _opennedScenesCache.Clear();
#endif
            if (_chunkPositions.IsCreated) _chunkPositions.Dispose();
            if (_chunkIndices.IsCreated) _chunkIndices.Dispose();
        }

        #endregion
        
        
        
        
        [BurstCompile]
        private struct ChunkProcessingJob : IJobParallelFor
        {
            public float3 TargetPosition;
            public float MinDistance;
            public float MaxDistance;

            [ReadOnly] public NativeArray<float3> ChunkPositions;
            [WriteOnly] public NativeList<int>.ParallelWriter ChunksToLoad;
            [WriteOnly] public NativeList<int>.ParallelWriter ChunksToUnload;

            public void Execute(int index)
            {
                float distance = math.distance(TargetPosition, ChunkPositions[index]);

                if (distance < MinDistance)
                {
                    ChunksToLoad.AddNoResize(index);
                }
                else if (distance > MaxDistance)
                {
                    ChunksToUnload.AddNoResize(index);
                }
            }
        }
        
    }
}
