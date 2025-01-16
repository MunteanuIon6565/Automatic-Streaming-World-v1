#if UNITY_EDITOR

using UnityEditorInternal;

#endif

using System;
using UnityEngine;



namespace AUTOMATIC_STREAMING_WORLD
{
    [CreateAssetMenu(fileName = "ASW Automatic Streaming World Settings", menuName = "STREAMING WORLD SYSTEM/ASW Automatic Streaming World Settings", order = 0)]
    public class ASW_Settings : ScriptableObject
    {
        [Header("Chunk Settings")] 
        [Tooltip("!For sort is used pivot point from objects.!")]
        [field:  SerializeField] 
        public Vector3 ChunkSize { get; private set; } = new Vector3(100, 100, 100);
        
        [field:  SerializeField]
        public bool UseStreamingBySizeObjects { get; private set; } = false;
        

        
        [Header("<b>Runtime Settings")] 
        public float LoopTimeCheckDistance = 5f;
        
        
#if UNITY_EDITOR
        
        [Header("<color=cyan>Editor Settings")] 
        public float LoopTimeAutomaticSortEditor = 5f;
        
        
        
        [Header("<color=yellow>For use from down features tick true -> UseStreamingBySizeObjects")]
        [Header("<color=yellow>When sort objects to chunks it will move just the last parent object from hierarchy with this settings.")]
        [Header("Small Objects Sort Settings")]
        public string[] UnityTagsSmallObjects;
        public LayerMask LayersSmallObjects;
        
        [Header("Small Objects Sort Settings")]
        public string[] UnityTagsMediumObjects;
        public LayerMask LayersMediumObjects;
        
        [Header("Small Objects Sort Settings")]
        public string[] UnityTagsLargeObjects;
        public LayerMask LayersLargeObjects;
        
#endif
        
#if UNITY_EDITOR

        
        [ContextMenu("Reset Array UnityTagsSmallObjects")]
        private void ResetArrayUnityTagsSmallObjects()
        {
            UnityTagsSmallObjects = InternalEditorUtility.tags;
        }
        
        [ContextMenu("Reset Array UnityTagsMediumObjects")]
        private void ResetArrayUnityTagsMediumObjects()
        {
            UnityTagsSmallObjects = InternalEditorUtility.tags;
        }
        [ContextMenu("Reset Array UnityTagsLargeObjects")]
        private void ResetArrayUnityTagsLargeObjects()
        {
            UnityTagsLargeObjects = InternalEditorUtility.tags;
        }


#endif
    }
}
