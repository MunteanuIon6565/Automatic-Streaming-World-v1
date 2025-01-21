#if UNITY_EDITOR
            
using UnityEditor;

#endif

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
        
        
        public static AWS_WorldStreamManager Instance = null;
        public static Vector3 OffsetOrigin = Vector3.zero;
        
        [field: SerializeField] 
        public AWS_Settings m_aws_Settings { get; private set; }
        [field: SerializeField] 
        public AWS_AllChunksInOneWorld m_aws_Chunks { get; private set; }
        [SerializeField] 
        private bool m_autoInitializeInAwake = true;
        
        [SerializeField, Tooltip("By default is MainCamera")] 
        private Transform m_targetForStream;
        public Transform TargetForStream
        {
            get
            {
                if (Application.isPlaying)
                {
                    if (!m_targetForStream) 
                        m_targetForStream = Camera.main.transform;
                }
                else if (Application.isEditor && Camera.current)
                {
                    return Camera.current.transform;
                }
                
                return m_targetForStream;
            }
        }

        #endregion



        #region METHODS
        
        
        public void Initialize(Transform targetForStream = null)
        {
            if (Application.isPlaying && Instance == null)
            {
                Instance = this;
            }
            else if (Application.isPlaying)
            {
                Destroy(this);
            }

            m_aws_Chunks.RebuildListToDictionary();
            
            if (targetForStream) 
                m_targetForStream = targetForStream;
        }


        private async void CheckStreamChunks()
        {
            if (!TargetForStream) return;
            
            Dictionary<string, ChunkContainer> ChunkContainers = Application.isPlaying ? m_aws_Chunks.ChunkContainers : m_aws_Chunks.RebuildListToDictionary();
            
            // prioritate la large objects mai intai 
            foreach (var item in ChunkContainers.Values)
            {
                float distance = Vector3.Distance(TargetForStream.position, item.WorldPosition + OffsetOrigin);

                if (distance <
#if UNITY_EDITOR
                        (Application.isPlaying ? m_aws_Settings.MinDistanceShow : m_aws_Settings.MinDistanceShowEditor)
#else
                             m_aws_Settings.MinDistanceShow
#endif
                   )
                {
                    // Load chunk
                    await LoadChunk(item.SceneReference);
                }
                else if (distance > 
#if UNITY_EDITOR
                             (Application.isPlaying ? m_aws_Settings.MaxDistanceShow : m_aws_Settings.MaxDistanceShowEditor)
#else
                             m_aws_Settings.MaxDistanceShow
#endif
                        )
                {
                    // Unload chunk
                    await UnloadChunk(item.SceneReference);
                }
            }



            async Task LoadChunk(AssetReference assetReference)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    // Load scene in Editor mode
                    var scenePath = UnityEditor.AssetDatabase.GetAssetPath(assetReference.editorAsset);
                    var scene = UnityEditor.SceneManagement.EditorSceneManager.GetSceneByPath(scenePath);
                    if (!scene.isLoaded)
                    {
                        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath,
                            UnityEditor.SceneManagement.OpenSceneMode.Additive);
                    }

                    return;
                }
#endif
                // Load scene in runtime
                if (assetReference != null && assetReference.IsValid())
                {
                    await assetReference.LoadSceneAsync(UnityEngine.SceneManagement.LoadSceneMode.Additive).Task;
                }
            }
            
            

            async Task UnloadChunk(AssetReference assetReference)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    // Unload scene in Editor mode
                    var scenePath = UnityEditor.AssetDatabase.GetAssetPath(assetReference.editorAsset);
                    var scene = UnityEditor.SceneManagement.EditorSceneManager.GetSceneByPath(scenePath);
                    if (scene.isLoaded)
                    {
                        // Save the scene before unloading
                        if (scene.isDirty) UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

                        // Close the scene
                        UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
                    }

                    return;
                }
#endif
                // Unload scene in runtime
                if (assetReference != null && assetReference.IsValid())
                {
                    await Addressables.UnloadSceneAsync(assetReference.OperationHandle).Task;
                }
            }

        }



        #endregion

        
        #region UNITY METHODS


        private IEnumerator Start()
        {
            WaitForSeconds waitForSeconds = new WaitForSeconds(m_aws_Settings.LoopTimeCheckDistance);

            while (Application.isPlaying)
            {
                CheckStreamChunks();

                yield return waitForSeconds;
            }
        }

        private void Awake()
        {
            if (m_autoInitializeInAwake) Initialize();
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) EditorApplication.update += CheckStreamChunks;
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) EditorApplication.update -= CheckStreamChunks;
#endif
        }
        
        
        #endregion
    }
}