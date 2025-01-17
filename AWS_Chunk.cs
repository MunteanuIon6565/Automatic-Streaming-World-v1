using UnityEngine;

namespace AUTOMATIC_WORLD_STREAMING
{
    public class AWS_Chunk : MonoBehaviour
    {
        
        
        
        
        
        
        
        
#if UNITY_EDITOR
        [Header("<color=red>DON'T REMOVE THIS COMPONENT OR OBJECT!\\\n<color=yellow>(Just when this object is completely unused.)</color></color>")]
        [SerializeField] private Vector3 m_chunkSize;
        
        public void Initialize(Vector3 chunkSize)
        {
            m_chunkSize = chunkSize;
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;

            Gizmos.DrawWireCube(transform.position, m_chunkSize);
        }
        
#endif
    }
}