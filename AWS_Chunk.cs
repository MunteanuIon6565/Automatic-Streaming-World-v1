using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AUTOMATIC_WORLD_STREAMING
{
    [ExecuteAlways]
    public class AWS_Chunk : MonoBehaviour
    {
        private void OnDestroy()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);

                    if (IsOutFromChunkPos(child.position))
                    {
                        SceneManager.MoveGameObjectToScene(child.gameObject, EditorSceneManager.GetActiveScene());
                    }
                }
            }
#endif
            
        }


#if UNITY_EDITOR
        [Header("<color=red>DON'T REMOVE THIS COMPONENT OR OBJECT!\\\n<color=yellow>(1.Just when this object is completely unused.)\\\n</color><color=white>2.Collapse|Expand component to Show|Hide Square Gizmos Chunk.</color></color>")]
        [SerializeField] private Vector3 m_chunkSize;
        
        public void Initialize(Vector3 chunkSize)
        {
            m_chunkSize = chunkSize;
        }

        private bool IsOutFromChunkPos(Vector3 chunkPos)
        {
            float minPosX = transform.position.x - m_chunkSize.x / 2;
            float maxPosX = transform.position.x + m_chunkSize.x / 2;
            
            float minPosY = transform.position.y - m_chunkSize.y / 2;
            float maxPosY = transform.position.y + m_chunkSize.y / 2;
            
            float minPosZ = transform.position.z - m_chunkSize.z / 2;
            float maxPosZ = transform.position.z + m_chunkSize.z / 2;
            

            if (
                IsChunkPositionOut(chunkPos.x, minPosX, maxPosX) 
                || IsChunkPositionOut(chunkPos.y, minPosY, maxPosY)
                || IsChunkPositionOut(chunkPos.z, minPosZ, maxPosZ)
                )
                return true;
            
            
            return false;
            
            

            bool IsChunkPositionOut(float posObject, float minPosObject, float maxPosObject)
            {
                if (posObject > minPosObject && posObject < maxPosObject)
                    return false;
                
                return true;
            }
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