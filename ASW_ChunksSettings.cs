using UnityEngine;

namespace AUTOMATIC_STREAMING_WORLD
{
    [CreateAssetMenu(fileName = "Chunks Sorter Settings", menuName = "STREAMING WORLD SYSTEM/Chunk Sorter Settings", order = 100)]
    public class ASW_ChunksSettings : ScriptableObject
    {
        [Header("Chunk Settings")] 
        [field:  SerializeField] public Vector3 ChunkSize { get; private set; } = new Vector3(100, 100, 100);

        [Header("Runtime Settings")] 
        public float LoopTimeCheckDistance = 5f;
        
        [Header("Editor Settings")] 
        public float LoopTimeAutomaticSortEditor = 5f;
    }
}
