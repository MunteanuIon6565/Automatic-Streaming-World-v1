#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AUTOMATIC_STREAMING_WORLD
{
    /// <summary>
    /// This script organizes a list of objects into spatial chunks based on their global positions. 
    /// Useful for scene optimization by grouping objects within specified chunk boundaries.
    /// </summary>
    //[CreateAssetMenu(fileName = "ChunkSorterSettings", menuName = "STREAMING WORLD SYSTEM/Chunk Sorter Settings", order = 1)]
    public class ASW_ChunkSorter : MonoBehaviour
    {
        [Header("Transforms to Sort")]
        [SerializeField] 
        private List<Transform> objectsToSort = new List<Transform>();
        
        [SerializeField] private ASW_ChunksSettings m_aws_ChunksSettings;
        private Vector3 m_chunkSize => m_aws_ChunksSettings.ChunkSize;

        
        
        
        private void PopulateChildrenTransforms(Transform parent)
        {
            foreach (Transform child in parent)
            {
                if (child != parent) // Avoid adding self if called on root transform
                {
                    objectsToSort.Add(child);
                    PopulateChildrenTransforms(child);
                }
            }
        }

        
        [ContextMenu("Sort To Chunks")]
        private void SortToChunks()
        {
            var chunks = new Dictionary<Vector3Int, List<Transform>>();

            foreach (var obj in objectsToSort)
            {
                if (obj == null) continue;

                Vector3Int chunkCoord = CalculateChunkCoordinate(obj.position);

                if (!chunks.ContainsKey(chunkCoord))
                    chunks[chunkCoord] = new List<Transform>();

                chunks[chunkCoord].Add(obj);
            }
            
            OrganizeObjectsInChunks(chunks);

            MarkCurrentSceneDirty();
        }

        
        private Vector3Int CalculateChunkCoordinate(Vector3 worldPosition)
        {
            return new Vector3Int(
                Mathf.FloorToInt(worldPosition.x / m_chunkSize.x),
                Mathf.FloorToInt(worldPosition.y / m_chunkSize.y),
                Mathf.FloorToInt(worldPosition.z / m_chunkSize.z)
            );
        }

        
        private void OrganizeObjectsInChunks(Dictionary<Vector3Int, List<Transform>> chunks)
        {
            foreach (var chunk in chunks)
            {
                Vector3 chunkCenter = new Vector3(
                    chunk.Key.x * m_chunkSize.x + m_chunkSize.x / 2,
                    chunk.Key.y * m_chunkSize.y + m_chunkSize.y / 2,
                    chunk.Key.z * m_chunkSize.z + m_chunkSize.z / 2
                );

                GameObject chunkParent = new GameObject($"Chunk_{chunk.Key}")
                {
                    transform = { position = chunkCenter }
                };

                foreach (var obj in chunk.Value)
                {
                    obj.SetParent(chunkParent.transform);
                }
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

            foreach (var obj in objectsToSort)
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
