using UnityEngine;

namespace AUTOMATIC_WORLD_STREAMING
{
    public class AWS_Chunk : MonoBehaviour
    {
        
        
        
        
        
        
        
        
#if UNITY_EDITOR
        [Header("<color=red>DON'T REMOVE THIS COMPONENT OR OBJECT!\\\n<color=yellow>(1.Just when this object is completely unused.)\\\n</color><color=white>2.Collapse|Expand component to Show|Hide Square Gizmos Chunk.</color></color>")]
        [SerializeField] private Vector3 m_chunkSize;
        
        public void Initialize(Vector3 chunkSize)
        {
            m_chunkSize = chunkSize;
        }
        
        private void OnDrawGizmos()
        {
            if (!UnityEditorInternal.InternalEditorUtility.GetIsInspectorExpanded(this)) return;
            
            Gizmos.color = Color.yellow;

            Gizmos.DrawWireCube(transform.position, m_chunkSize);
        }
        
#endif
    }
}