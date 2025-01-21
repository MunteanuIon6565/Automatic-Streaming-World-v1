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
            ProcessChunks();

            /*var chunkContainers = Application.isPlaying
                ? AwsChunks.ChunkContainers
                : AwsChunks.RebuildListToDictionary();

            // load scenes cu prioritate mai intai cele large dupa medii dupa mici
            foreach (var chunk in chunkContainers.Values)
            {
                float distance = Vector3.Distance(TargetForStream.position, chunk.WorldPosition + OffsetOrigin);

                if (distance < GetMinDistanceShow())
                {
                    await LoadChunk(chunk.SceneReference);
                }
                else if (distance > GetMaxDistanceShow())
                {
                    await UnloadChunk(chunk.SceneReference);
                }
            }*/



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
                if (assetReference != null /*&& assetReference.IsValid()*/) 
                {
                    try
                    {
                        await assetReference.LoadSceneAsync(UnityEngine.SceneManagement.LoadSceneMode.Additive).Task;
                    }
                    catch (Exception e)
                    {
                    }
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


            void ProcessChunks()
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

                // Estimăm o capacitate inițială rezonabilă pentru listele NativeList
                NativeList<int> chunksToLoad = new NativeList<int>(chunkCount /*/ 2*/, Allocator.TempJob);
                NativeList<int> chunksToUnload = new NativeList<int>(chunkCount /*/ 2*/, Allocator.TempJob);

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


        }

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
                EditorApplication.update += CheckStreamChunks;
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                EditorApplication.update -= CheckStreamChunks;
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
