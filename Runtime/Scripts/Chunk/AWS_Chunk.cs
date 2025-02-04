#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

using System.Collections.Generic;
using AUTOMATIC_WORLD_STREAMING.FloatingOriginObjectsType;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AUTOMATIC_WORLD_STREAMING
{
#if UNITY_EDITOR
    [ExecuteAlways]
#endif
    [RequireComponent(typeof(AWS_FloatingOriginTransform))]
    public class AWS_Chunk : MonoBehaviour
    {
        private void OnDestroy()
        {
/*#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);

                    if (IsOutFromChunkPos(child.position))
                    {
                        EditorSceneManager.MarkSceneDirty(child.gameObject.scene);
                        child.SetParent(null);
                        SceneManager.MoveGameObjectToScene(child.gameObject, EditorSceneManager.GetActiveScene());
                    }
                }
                SaveDirtyChunkScenes();
            }
#endif*/
        }


#if UNITY_EDITOR
        [Header("<color=red>DON'T REMOVE THIS COMPONENT OR OBJECT!\\\n<color=yellow>(1.Just when this object is completely unused.)\\\n</color><color=white>2.Collapse|Expand component to Show|Hide Square Gizmos Chunk.</color></color>")]
        [SerializeField] private Vector3 m_chunkSize;
        
        public void Initialize(Vector3 chunkSize)
        {
            m_chunkSize = chunkSize;
        }

        private void SaveDirtyChunkScenes()
        {
            List<Scene> scenesToUnload = new List<Scene>();
                            
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene loadedScene = SceneManager.GetSceneAt(i);
                if (loadedScene.name != gameObject.scene.name && loadedScene.isDirty)
                {
                    scenesToUnload.Add(loadedScene);
                }
            }
            EditorSceneManager.SaveScenes(scenesToUnload.ToArray());
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
                if (posObject >= minPosObject && posObject <= maxPosObject)
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