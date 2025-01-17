#if UNITY_EDITOR

using UnityEditorInternal;

#endif

using System.Linq;
using UnityEngine;


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
        
        [field:  SerializeField, Tooltip("For sort objects by layer data method.(Small, Medium, Large objects.)")]
        public bool UseStreamingBySizeObjects { get; private set; } = false;

#if UNITY_EDITOR
        [SerializeField, Tooltip("It use for sort if <b>UseStreamingBySizeObjects</b> is false.")] 
        private string[] defaultExcludedTagsForSortInSimpleMode = { "MainCamera","EditorOnly" };
#endif
        
        [Space(MEDIUM_SIZE_SPACE)] [Header(HEADER_SEPARATOR)] [Header("<b>Runtime Settings")] 
        public float LoopTimeCheckDistance = 5f;

        
        #endregion
        
        
        #region EDITOR ONLY
#if UNITY_EDITOR
        
        
        #region EDITOR ONLY FIELDS
        
        
        [Space(MEDIUM_SIZE_SPACE)][Header(HEADER_SEPARATOR)] 
        [Header("<color=cyan>Editor Settings")] 
        public float LoopTimeAutomaticSortEditor = 30f;
        public bool ShowChunkSquareGizmos = true;
        
        [Space(LARGE_SIZE_SPACE)][Header(HEADER_SEPARATOR)] 
        [Header("<color=yellow>For use from down features tick true -> UseStreamingBySizeObjects")]
        [Header("<color=yellow>When sort objects to chunks it will move just the last parent object from hierarchy with this settings.")]
        [Header(HEADER_SEPARATOR)] 
        [Header("Small Objects Sort Settings")]
        public string[] UnityTagsSmallObjects;
        //public LayerMask LayersSmallObjects;
        
        [Space(MEDIUM_SIZE_SPACE)][Header(HEADER_SEPARATOR)] 
        [Header("Small Objects Sort Settings")]
        public string[] UnityTagsMediumObjects;
        //public LayerMask LayersMediumObjects;
        
        [Space(MEDIUM_SIZE_SPACE)][Header(HEADER_SEPARATOR)] 
        [Header("Small Objects Sort Settings")]
        public string[] UnityTagsLargeObjects;
        //public LayerMask LayersLargeObjects;
        
        
        public string[] AllUnityTagsForSortInSimpleMode => InternalEditorUtility.tags
            .Where(tag => !defaultExcludedTagsForSortInSimpleMode.Contains(tag))
            .ToArray();
        
        
        #endregion


        #region EDITOR ONLY METHODS
        
        
        [ContextMenu("Reset Array UnityTagsSmallObjects")]
        private void ResetArrayUnityTagsSmallObjects()
        {
            UnityTagsSmallObjects = InternalEditorUtility.tags
                .Where(tag => !defaultExcludedTagsForSortInSimpleMode.Contains(tag))
                .ToArray();
        }
        
        [ContextMenu("Reset Array UnityTagsMediumObjects")]
        private void ResetArrayUnityTagsMediumObjects()
        {
            UnityTagsMediumObjects = InternalEditorUtility.tags
                .Where(tag => !defaultExcludedTagsForSortInSimpleMode.Contains(tag))
                .ToArray();
        }
        [ContextMenu("Reset Array UnityTagsLargeObjects")]
        private void ResetArrayUnityTagsLargeObjects()
        {
            UnityTagsLargeObjects = InternalEditorUtility.tags
                .Where(tag => !defaultExcludedTagsForSortInSimpleMode.Contains(tag))
                .ToArray();
        }

        
        #endregion

        
#endif
        #endregion
    }
}
