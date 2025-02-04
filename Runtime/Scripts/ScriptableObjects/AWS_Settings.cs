#if UNITY_EDITOR

using UnityEditorInternal;

#endif

using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;


namespace AUTOMATIC_WORLD_STREAMING
{
    [CreateAssetMenu(fileName = "ASW AUTOMATIC WORLD STREAMING Settings", menuName = "AUTOMATIC WORLD STREAMING/ASW Automatic World Streaming Settings", order = 0)]
    public class AWS_Settings : ScriptableObject
    {
        #region CONSTANTS  
        private const float MEDIUM_SIZE_SPACE = 20;
        private const float LARGE_SIZE_SPACE = 40;
        private const string HEADER_SEPARATOR = "_______________________________________________________________________________________________________________________________________";
        #endregion

        
        #region FIELDS
        
        
        [field:  SerializeField, Header(HEADER_SEPARATOR), Header("Chunk Settings"), Tooltip("!For sort is used pivot point from objects.!")] 
        public Vector3 ChunkSize { get; private set; } = new Vector3(100, 100, 100);
        
        /*[field:  SerializeField, /*Header("<color=yellow>Before change this, Move All chunk in main scene</color>"),#1# Tooltip("For sort objects by layer data method.(Small, Medium, Large objects.)")]
        public bool UseStreamingBySizeObjects { get; private set; } = false;*/

        [SerializeField, Tooltip("It use for sort if <b>UseStreamingBySizeObjects</b> is false.")] 
        private string[] defaultExcludedTagsForSort = { "MainCamera","EditorOnly","Untagged" };
        
        [Space(MEDIUM_SIZE_SPACE)] [Header(HEADER_SEPARATOR)] [Header("<b>Runtime Settings")] 
        public float LoopTimeCheckDistance = 5f;
        [field: SerializeField,Tooltip("Min distance to spawn Chunk")] 
        public float MinDistanceShow{ get; private set; } = 100;
        [field: SerializeField,Tooltip("Max distance to delete Chunk")] 
        public float MaxDistanceShow{ get; private set; } = 110;

        
        #endregion
        
        
        #region EDITOR
        
        
        #region EDITOR FIELDS
        
        
        [Space(MEDIUM_SIZE_SPACE)][Header(HEADER_SEPARATOR)] 
        [Header("<color=cyan>Editor Settings")] 
        public float LoopTimeAutomaticSortEditor = 60f;
        [field: SerializeField,Tooltip("Min distance to spawn Chunk")] 
        public float MinDistanceShowEditor{ get; private set; } = 1000;
        [field: SerializeField,Tooltip("Max distance to delete Chunk")] 
        public float MaxDistanceShowEditor{ get; private set; } = 1100;
        public bool ShowChunkSquareGizmosAroundSceneCamera = true;
        public Vector3Int CellsAroundSceneCameraToShow = Vector3Int.one;
        
        [Space(LARGE_SIZE_SPACE)][Header(HEADER_SEPARATOR)] 
        //[Header("<color=yellow>For use from down features tick true -> UseStreamingBySizeObjects")]
        [Header("<color=yellow>When sort objects to chunks it will move just the last parent object from hierarchy with this settings.")]
        [Header(HEADER_SEPARATOR)] 
        [Header("Small Objects Sort Settings")]
        public string[] UnityTagsSmallObjects;
        
        [Space(MEDIUM_SIZE_SPACE)][Header(HEADER_SEPARATOR)] 
        [Header("Medium Objects Sort Settings")]
        public string[] UnityTagsMediumObjects;
        
        [Space(MEDIUM_SIZE_SPACE)][Header(HEADER_SEPARATOR)] 
        [Header("Large Objects Sort Settings")]
        public string[] UnityTagsLargeObjects;


#if UNITY_EDITOR
        public string[] AllUnityTagsForSortInSimpleMode => InternalEditorUtility.tags
            .Where(tag => !defaultExcludedTagsForSort.Contains(tag))
            .ToArray();
#endif
        
        
        #endregion


#if UNITY_EDITOR
        #region EDITOR ONLY METHODS
        
        
        private void OnValidate()
        {
            if (MinDistanceShow > MaxDistanceShow) 
                MaxDistanceShow = MinDistanceShow;
            
            if (0 > MaxDistanceShow) 
                MaxDistanceShow = 0;
            
            if (0 > MinDistanceShow) 
                MinDistanceShow = 0;
            
            
            // Editor
            if (MinDistanceShowEditor > MaxDistanceShowEditor) 
                MaxDistanceShowEditor = MinDistanceShowEditor;
            
            if (0 > MaxDistanceShowEditor) 
                MaxDistanceShowEditor = 0;
            
            if (0 > MinDistanceShowEditor) 
                MinDistanceShowEditor = 0;
        }
        
        
        
        
        [ContextMenu("Reset Array UnityTagsSmallObjects")]
        private void ResetArrayUnityTagsSmallObjects()
        {
            UnityTagsSmallObjects = InternalEditorUtility.tags
                .Where(tag => !defaultExcludedTagsForSort.Contains(tag))
                .ToArray();
        }
        
        [ContextMenu("Reset Array UnityTagsMediumObjects")]
        private void ResetArrayUnityTagsMediumObjects()
        {
            UnityTagsMediumObjects = InternalEditorUtility.tags
                .Where(tag => !defaultExcludedTagsForSort.Contains(tag))
                .ToArray();
        }
        [ContextMenu("Reset Array UnityTagsLargeObjects")]
        private void ResetArrayUnityTagsLargeObjects()
        {
            UnityTagsLargeObjects = InternalEditorUtility.tags
                .Where(tag => !defaultExcludedTagsForSort.Contains(tag))
                .ToArray();
        }

        
        #endregion

        
#endif
        #endregion
    }
}
