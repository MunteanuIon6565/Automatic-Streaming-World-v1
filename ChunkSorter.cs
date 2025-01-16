#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AutomaticChunkSystem
{
    /// <summary>
    /// This script organizes a list of objects into spatial chunks based on their global positions. 
    /// Useful for scene optimization by grouping objects within specified chunk boundaries.
    /// </summary>
    //[CreateAssetMenu(fileName = "ChunkSorterSettings", menuName = "STREAMING WORLD SYSTEM/Chunk Sorter Settings", order = 1)]
    public class ChunkSorter : MonoBehaviour
    {
        [Header("Transforms to Sort")]
        [SerializeField] 
        private List<Transform> objectsToSort = new List<Transform>();

        [Header("Chunk Settings")] 
        [SerializeField] private Vector3 chunkSize = new Vector3(10, 10, 10);
        [SerializeField] private Vector3 maxChunkCoordinates = new Vector3(100, 100, 100);

        
        /*/// <summary>
         /// Adds all child transforms under this GameObject to the objectsToSort list.
         /// </summary>
         [ContextMenu("Add All Child Transforms")]
         private void AddAllChildTransforms()
         {
             objectsToSort.Clear();
             PopulateChildrenTransforms(transform);
         }*/

        /// <summary>
        /// Recursively populates all child transforms under a given parent transform.
        /// </summary>
        /// <param name="parent">Parent transform from which to start adding children.</param>
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

        /// <summary>
        /// Sorts all objects in objectsToSort into chunks based on their global positions, 
        /// and organizes them under newly created empty GameObjects.
        /// </summary>
        [ContextMenu("Sort To Chunks")]
        private void SortToChunks()
        {
            var chunks = new Dictionary<Vector3Int, List<Transform>>();
            
            objectsToSort.AddRange(FindObjectsOfType<Transform>());

            foreach (var obj in objectsToSort)
            {
                if (obj == null) continue;

                // Calculate chunk coordinates based on global position and chunk size
                Vector3Int chunkCoord = CalculateChunkCoordinate(obj.position);

                // Ensure chunk coordinate is within bounds
                if (IsWithinMaxChunkCoordinates(chunkCoord))
                {
                    // Add the object to the respective chunk list
                    if (!chunks.ContainsKey(chunkCoord))
                        chunks[chunkCoord] = new List<Transform>();

                    chunks[chunkCoord].Add(obj);
                }
            }

            // Create and organize empty GameObjects for each chunk
            OrganizeObjectsInChunks(chunks);
        }

        /// <summary>
        /// Calculates chunk coordinates based on global position and chunk size.
        /// </summary>
        /// <param name="worldPosition">The global position of the object.</param>
        /// <returns>A Vector3Int representing the chunk coordinates.</returns>
        private Vector3Int CalculateChunkCoordinate(Vector3 worldPosition)
        {
            return new Vector3Int(
                Mathf.FloorToInt(worldPosition.x / chunkSize.x),
                Mathf.FloorToInt(worldPosition.y / chunkSize.y),
                Mathf.FloorToInt(worldPosition.z / chunkSize.z)
            );
        }

        /// <summary>
        /// Checks if the given chunk coordinates are within the defined maxChunkCoordinates.
        /// </summary>
        /// <param name="chunkCoord">Chunk coordinates to check.</param>
        /// <returns>True if within bounds, false otherwise.</returns>
        private bool IsWithinMaxChunkCoordinates(Vector3Int chunkCoord)
        {
            return chunkCoord.x <= maxChunkCoordinates.x &&
                   chunkCoord.y <= maxChunkCoordinates.y &&
                   chunkCoord.z <= maxChunkCoordinates.z;
        }

        /// <summary>
        /// Creates an empty GameObject for each chunk and assigns the objects in the chunk to it.
        /// </summary>
        /// <param name="chunks">Dictionary containing chunk coordinates and the corresponding objects.</param>
        private void OrganizeObjectsInChunks(Dictionary<Vector3Int, List<Transform>> chunks)
        {
            foreach (var chunk in chunks)
            {
                Vector3 chunkCenter = new Vector3(
                    chunk.Key.x * chunkSize.x + chunkSize.x / 2,
                    chunk.Key.y * chunkSize.y + chunkSize.y / 2,
                    chunk.Key.z * chunkSize.z + chunkSize.z / 2
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

                if (IsWithinMaxChunkCoordinates(chunkCoord) && !drawnChunks.Contains(chunkCoord))
                {
                    drawnChunks.Add(chunkCoord);

                    Vector3 chunkCenter = new Vector3(
                        chunkCoord.x * chunkSize.x + chunkSize.x / 2,
                        chunkCoord.y * chunkSize.y + chunkSize.y / 2,
                        chunkCoord.z * chunkSize.z + chunkSize.z / 2
                    );

                    Gizmos.DrawWireCube(chunkCenter, chunkSize);
                }
            }
        }
    }
}

#endif
