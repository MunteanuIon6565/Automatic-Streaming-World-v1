#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace AUTOMATIC_STREAMING_WORLD
{
    public class ASW_ChunksSorter : MonoBehaviour
    {
        [FormerlySerializedAs("objectsToSort")]
        [Header("Transforms to Sort")]
        [SerializeField] 
        private List<Transform> m_objectsToSort = new List<Transform>();
        
        [SerializeField] private ASW_Settings m_aws_Settings;
        private Vector3 m_chunkSize => m_aws_Settings.ChunkSize;

        
        
        
        [ContextMenu("Sort To Chunks Small Objects")]
        private void SortToChunksSmallObjects() => SortToChunksByTags(m_aws_Settings.UnityTagsSmallObjects, "Small Objects");
        
        private void SortToChunksByTags(string[] tagsFilters, string chunkPrefixName = "")
        {
            var chunks = new Dictionary<Vector3Int, List<Transform>>();

            m_objectsToSort = GetObjectsToSortByTags(tagsFilters);

            foreach (var obj in m_objectsToSort)
            {
                if (obj == null) continue;

                Vector3Int chunkCoord = CalculateChunkCoordinate(obj.position);

                if (!chunks.ContainsKey(chunkCoord))
                    chunks[chunkCoord] = new List<Transform>();

                chunks[chunkCoord].Add(obj);
            }
            
            OrganizeObjectsInChunks(chunks, chunkPrefixName);

            MarkCurrentSceneDirty();
        }

        
        
        private List<Transform> GetObjectsToSortByTags(string[] tagsFilters)
        {
            List<Transform> objectsToSort = new List<Transform>();
            Transform[] allObjectsToSort = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            
            if (m_aws_Settings.UseStreamingBySizeObjects)
            {
                foreach (var obj in allObjectsToSort)
                {
                    if (
                        ContainOneTag(obj, tagsFilters) 
                        && !HasParentWithTag(obj, tagsFilters)
                        ) 
                        objectsToSort.Add(obj);
                }
            }
            
            return objectsToSort;
            
            

            bool HasParentWithTag(Transform obj, string[] tags)
            {
                List<Transform> allParents = new List<Transform>();


                AddAllParents(obj);
                
                foreach (var parent in allParents)
                {
                    if (ContainOneTag(parent, tags))
                        return true;
                }
                return false;
                
                
                void AddAllParents(Transform obj)
                {
                    if (obj.parent != null)
                    {
                        allParents.Add(obj.parent);
                        AddAllParents(obj.parent);
                    }
                }
            }

            
            
            bool ContainOneTag(Transform obj, string[] tags)
            {
                foreach (var tag in tags)
                {
                    if (obj.CompareTag(tag))
                        return true;
                }
                
                return false;
            }
        }

        

        private Vector3Int CalculateChunkCoordinate(Vector3 worldPosition)
        {
            return new Vector3Int(
                Mathf.FloorToInt(worldPosition.x / m_chunkSize.x),
                Mathf.FloorToInt(worldPosition.y / m_chunkSize.y),
                Mathf.FloorToInt(worldPosition.z / m_chunkSize.z)
            );
        }

        
        
        private void OrganizeObjectsInChunks(Dictionary<Vector3Int, List<Transform>> chunks, string chunkPrefixName = "")
        {
            GameObject chunkParent = null;
            
            foreach (var chunk in chunks)
            {
                Vector3 chunkCenter = new Vector3(
                    chunk.Key.x * m_chunkSize.x + m_chunkSize.x / 2,
                    chunk.Key.y * m_chunkSize.y + m_chunkSize.y / 2,
                    chunk.Key.z * m_chunkSize.z + m_chunkSize.z / 2
                );

                chunkParent = new GameObject($"{chunkPrefixName}_Chunk_{chunk.Key}")
                {
                    transform = { position = chunkCenter }
                };
                
                chunkParent.tag = "EditorOnly";

                foreach (var obj in chunk.Value)
                {
                    obj.SetParent(chunkParent.transform);
                }
            }

            foreach (var item in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (item.CompareTag("EditorOnly") && item.name.Equals(chunkParent.name)) 
                    Destroy(item);
            }
        }
        
        
        
        public static void MarkCurrentSceneDirty()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                Debug.Log($"Scene '{activeScene.name}' has been marked as dirty.");
            }
            else
            {
                Debug.LogError("Active scene is not valid!");
            }
        }
        
        

        /// <summary>
        /// Draws Gizmos to visually represent chunk boundaries for debugging in the editor.
        /// </summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            HashSet<Vector3Int> drawnChunks = new HashSet<Vector3Int>();

            foreach (var obj in m_objectsToSort)
            {
                if (obj == null) continue;

                Vector3Int chunkCoord = CalculateChunkCoordinate(obj.position);

                if (!drawnChunks.Contains(chunkCoord))
                {
                    drawnChunks.Add(chunkCoord);

                    Vector3 chunkCenter = new Vector3(
                        chunkCoord.x * m_chunkSize.x + m_chunkSize.x / 2,
                        chunkCoord.y * m_chunkSize.y + m_chunkSize.y / 2,
                        chunkCoord.z * m_chunkSize.z + m_chunkSize.z / 2
                    );

                    Gizmos.DrawWireCube(chunkCenter, m_chunkSize);
                }
            }
        }
    }
}

#endif
