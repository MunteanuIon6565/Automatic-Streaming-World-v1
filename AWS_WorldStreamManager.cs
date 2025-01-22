#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
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
        
        
        /*private Dictionary<int, string> indexToStringMap = new Dictionary<int, string>();
        private Dictionary<string, int> stringToIndexMap = new Dictionary<string, int>();*/

        #endregion

        #region METHODS

        public void Initialize(Transform customTargetForStream = null)
        {
            if (Instance == null)
            {
                Instance = this;
                AwsChunks.RebuildListToDictionary();
                if (customTargetForStream)
                    targetForStream = customTargetForStream;
            }
            else if (Application.isPlaying)
            {
                Destroy(this);
            }
        }

        private async void CheckStreamChunks()
        {
            if (!TargetForStream || !AwsChunks || !AwsSettings)
            {
                Debug.LogError("!TargetForStream || !AwsChunks || !AwsSettings is missing!");

                return;
            }

            // load scenes cu prioritate mai intai cele large dupa medii dupa mici
            ProcessChunksJob();



            float GetMinDistanceShow()
            {
#if UNITY_EDITOR
                return Application.isPlaying ? AwsSettings.MinDistanceShow : AwsSettings.MinDistanceShowEditor;
#else
                return AwsSettings.MinDistanceShow;
#endif
            }

            float GetMaxDistanceShow()
            {
#if UNITY_EDITOR
                return Application.isPlaying ? AwsSettings.MaxDistanceShow : AwsSettings.MaxDistanceShowEditor;
#else
                return AwsSettings.MaxDistanceShow;
#endif
            }

            async Task LoadChunk(AssetReference assetReference)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    string scenePath = AssetDatabase.GetAssetPath(assetReference.editorAsset);
                    var scene = EditorSceneManager.GetSceneByPath(scenePath);
                    if (!scene.isLoaded)
                    {
                        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                    }

                    return;
                }
#endif
                if (assetReference != null && !IsSceneLoaded(assetReference) /*&& assetReference.IsValid()*/) 
                {
                    await assetReference.LoadSceneAsync(LoadSceneMode.Additive).Task;
                }
            }

            async Task UnloadChunk(AssetReference assetReference)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    string scenePath = AssetDatabase.GetAssetPath(assetReference.editorAsset);
                    var scene = EditorSceneManager.GetSceneByPath(scenePath);
                    if (scene.isLoaded)
                    {
                        if (scene.isDirty) EditorSceneManager.SaveScene(scene);
                        EditorSceneManager.CloseScene(scene, true);
                    }

                    return;
                }
#endif
                if (assetReference != null && assetReference.IsValid())
                {
                    await Addressables.UnloadSceneAsync(assetReference.OperationHandle).Task;
                }
            }


            void ProcessChunksJob()
            {
                var chunkContainers = Application.isPlaying
                    ? AwsChunks.ChunkContainers
                    : AwsChunks.RebuildListToDictionary();

                int chunkCount = chunkContainers.Count;

                // Mapare între string și int
                Dictionary<int, string> indexToStringMap = new Dictionary<int, string>(chunkCount);
                Dictionary<string, int> stringToIndexMap = new Dictionary<string, int>(chunkCount);
                int currentIndex = 0;

                foreach (var key in chunkContainers.Keys)
                {
                    stringToIndexMap[key] = currentIndex;
                    indexToStringMap[currentIndex] = key;
                    currentIndex++;
                }

                NativeArray<float3> chunkPositions = new NativeArray<float3>(chunkCount, Allocator.TempJob);
                NativeArray<int> chunkIndices = new NativeArray<int>(chunkCount, Allocator.TempJob);

                NativeList<int> chunksToLoad = new NativeList<int>(chunkCount, Allocator.TempJob);
                NativeList<int> chunksToUnload = new NativeList<int>(chunkCount, Allocator.TempJob);

                float3 targetPosition = TargetForStream.position;
                float minDistance = GetMinDistanceShow();
                float maxDistance = GetMaxDistanceShow();

                int index = 0;
                foreach (var kvp in chunkContainers)
                {
                    chunkPositions[index] = (float3)kvp.Value.WorldPosition + (float3)OffsetOrigin;
                    chunkIndices[index] = stringToIndexMap[kvp.Key];
                    index++;
                }

                var chunkProcessingJob = new ChunkProcessingJob
                {
                    TargetPosition = targetPosition,
                    MinDistance = minDistance,
                    MaxDistance = maxDistance,
                    ChunkPositions = chunkPositions,
                    ChunksToLoad = chunksToLoad.AsParallelWriter(),
                    ChunksToUnload = chunksToUnload.AsParallelWriter()
                };

                JobHandle jobHandle = chunkProcessingJob.Schedule(chunkCount, 64);
                jobHandle.Complete();

                foreach (var loadIndex in chunksToLoad)
                {
                    string chunkKey = indexToStringMap[loadIndex];
                    LoadChunk(chunkContainers[chunkKey].SceneReference);
                }

                foreach (var unloadIndex in chunksToUnload)
                {
                    string chunkKey = indexToStringMap[unloadIndex];
                    UnloadChunk(chunkContainers[chunkKey].SceneReference);
                }

                chunkPositions.Dispose();
                chunkIndices.Dispose();
                chunksToLoad.Dispose();
                chunksToUnload.Dispose();
            }
            
            
            bool IsSceneLoaded(AssetReference sceneReference)
            {
                string sceneName = GetSceneNameFromAssetReference(sceneReference);

                if (string.IsNullOrEmpty(sceneName))
                {
                    Debug.LogWarning("Scene name could not be determined from the AssetReference.");
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

            
            string GetSceneNameFromAssetReference(AssetReference sceneRef)
            {
                string assetPath = sceneRef.AssetGUID;

                string sceneName = System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(assetPath));
                return sceneName;
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
#endif
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
