#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEditor;
using System.IO;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;


namespace AUTOMATIC_WORLD_STREAMING
{
    [ExecuteInEditMode]
    public class AWS_ChunksSorter : MonoBehaviour
    {
        #region CONSTANTS
        private const string EDITOR_ONLY_TAG = "EditorOnly";
        private const string SMALL_OBJECT_NAME = "Small";
        private const string MEDIUM_OBJECT_NAME = "Medium";
        private const string LARGE_OBJECT_NAME = "Large";
        private const string SIMPLE_SORT_NAME = "All";
        private const string MIDDLE_PART_SORT_NAME = "_Objects_Chunk_"; // it is used for delete copies of empty objects create with chunk system
        
        private const string PATH_CREATE_CHUNKS = "Assets/Plugins/AWS_Chunks";
        private const string AddressableGroupName = "AWS_Chunks_Group";
        private const string AddressableLabelName = "AWS_Chunks_Label";
        /*private const float DistanceThreshold = 500f;*/
        #endregion


        #region FIELDS

        
        [FormerlySerializedAs("m_objectsToSort")] [SerializeField] 
        private List<Transform> m_objectsToSortDebug = new List<Transform>();
        
        [SerializeField] private AWS_Settings m_aws_Settings;
        private Vector3 m_chunkSize => m_aws_Settings.ChunkSize;

        
        #endregion

        
        #region TEST METHODS

        
        [ContextMenu("Sort To Chunks Small Objects")]
        private void SortToChunksSmallObjects() => SortToChunksByTags(m_aws_Settings.UnityTagsSmallObjects, SMALL_OBJECT_NAME);
        [ContextMenu("Sort To Chunks Medium Objects")]
        private void SortToChunksMediumObjects() => SortToChunksByTags(m_aws_Settings.UnityTagsMediumObjects, MEDIUM_OBJECT_NAME);
        [ContextMenu("Sort To Chunks Large Objects")]
        private void SortToChunksLargeObjects() => SortToChunksByTags(m_aws_Settings.UnityTagsLargeObjects, LARGE_OBJECT_NAME);
        
        [ContextMenu("Sort To Chunks In Simple Mode")]
        private void SortToChunksSimpleMode()
        {
            if (!m_aws_Settings.UseStreamingBySizeObjects) 
                SortToChunksByTags(m_aws_Settings.AllUnityTagsForSortInSimpleMode, SIMPLE_SORT_NAME);
            else
                Debug.LogError("Cannot sort to chunks because StreamingBySizeObjects is not disabled.");
        }

        
        #endregion

        
        #region MAIN FUNCTIONAL
        
        
        [ContextMenu("SORT TO CHUNKS")]
        public void SortToChunksByTags()
        {
            List<GameObject> chunkSimpleSort = null;
            List<GameObject> chunkSmallSort = null;
            List<GameObject> chunkMediumSort = null;
            List<GameObject> chunkLargeSort = null;
            
            if (!m_aws_Settings.UseStreamingBySizeObjects) 
                chunkSimpleSort = SortToChunksByTags(m_aws_Settings.AllUnityTagsForSortInSimpleMode, SIMPLE_SORT_NAME);
            else
            {
                chunkLargeSort = SortToChunksByTags(m_aws_Settings.UnityTagsLargeObjects, LARGE_OBJECT_NAME);
                chunkMediumSort = SortToChunksByTags(m_aws_Settings.UnityTagsMediumObjects, MEDIUM_OBJECT_NAME);
                chunkSmallSort = SortToChunksByTags(m_aws_Settings.UnityTagsSmallObjects, SMALL_OBJECT_NAME);
            }

            
            if (chunkSimpleSort is { Count: > 0 })
            {
                for (int i = chunkSimpleSort.Count - 1; i >= 0; i--)
                {
                    MoveObjectToSceneChunk(chunkSimpleSort[i]);
                }
            }
        }



        private void MoveObjectToSceneChunk(GameObject objectToMove)
        {
            if (!Application.isEditor || Application.isPlaying)
                return;

            if (objectToMove == null)
            {
                Debug.LogError("Referința la obiectul de mutat este null.");
                return;
            }

            if (!AssetDatabase.IsValidFolder(PATH_CREATE_CHUNKS))
            {
                Directory.CreateDirectory(PATH_CREATE_CHUNKS);
                AssetDatabase.Refresh();
            }
            
            
            
            /*GameObject currentObject = this.gameObject;
            float distance = Vector3.Distance(currentObject.transform.position, objectToMove.transform.position);
            */

            string currentScenePath = EditorSceneManager.GetActiveScene().path;
            string currentSceneName = Path.GetFileNameWithoutExtension(currentScenePath);


            string targetScenePath = Path.Combine(PATH_CREATE_CHUNKS, currentSceneName + ".unity");
            if (!File.Exists(targetScenePath))
            {
                var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                EditorSceneManager.SaveScene(newScene, targetScenePath);
                EditorSceneManager.CloseScene(newScene, true);

                AddSceneToAddressables(targetScenePath);
            }

            Scene targetScene = EditorSceneManager.GetSceneByPath(targetScenePath);
            if (!targetScene.isLoaded/* && distance < DistanceThreshold*/)
            {
                EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Additive);
            }

            if (objectToMove.scene.path != targetScenePath)
            {
                SceneManager.MoveGameObjectToScene(objectToMove, targetScene);
                EditorSceneManager.MarkSceneDirty(targetScene);
                EditorSceneManager.SaveScene(targetScene);
            }

            /*// Descărca scena dacă distanța este prea mare
            if (distance >= DistanceThreshold && targetScene.isLoaded)
            {
                EditorSceneManager.CloseScene(targetScene, true);
            }*/
        }
        private void AddSceneToAddressables(string scenePath)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);

            AddressableAssetGroup group = settings.FindGroup(AddressableGroupName);
            if (group == null)
            {
                group = settings.CreateGroup(AddressableGroupName, false, false, true, null, typeof(BundledAssetGroupSchema));
            }

            if (!settings.GetLabels().Contains(AddressableLabelName))
            {
                settings.AddLabel(AddressableLabelName);
            }

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(scenePath), group);
            entry.SetLabel(AddressableLabelName, true);

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            AssetDatabase.SaveAssets();
            Debug.Log($"Scena a fost adăugată la Addressables cu label-ul \"{AddressableLabelName}\" în grupul \"{AddressableGroupName}\".");
        }




        private List<GameObject> SortToChunksByTags(string[] tagsFilters, string chunkPrefixName = "")
        {
            var chunks = new Dictionary<Vector3Int, List<Transform>>();

            m_objectsToSortDebug = GetObjectsToSortByTags(tagsFilters);

            foreach (var obj in m_objectsToSortDebug)
            {
                if (obj == null) continue;

                Vector3Int chunkCoord = CalculateChunkCoordinate(obj.position);

                if (!chunks.ContainsKey(chunkCoord))
                    chunks[chunkCoord] = new List<Transform>();

                chunks[chunkCoord].Add(obj);
            }
            
            MarkCurrentSceneDirty();

            return OrganizeObjectsInChunks(chunks, chunkPrefixName);
        }

        
        
        private List<Transform> GetObjectsToSortByTags(string[] tagsFilters)
        {
            List<Transform> objectsToSort = new List<Transform>();
            Transform[] allObjectsToSort = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            
            foreach (var obj in allObjectsToSort)
            {
                if (
                    ContainOneTag(obj, tagsFilters)
                    && !HasParentWithTag(obj, tagsFilters)
                )
                    objectsToSort.Add(obj);
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

        
        
        private List<GameObject> OrganizeObjectsInChunks(Dictionary<Vector3Int, List<Transform>> chunks, string chunkPrefixName = "")
        {
            List<GameObject> chunksParentList = new List<GameObject>();
            GameObject chunkParent;

            foreach (var chunk in chunks)
            {
                Vector3 chunkCenter = new Vector3(
                    chunk.Key.x * m_chunkSize.x + m_chunkSize.x / 2,
                    chunk.Key.y * m_chunkSize.y + m_chunkSize.y / 2,
                    chunk.Key.z * m_chunkSize.z + m_chunkSize.z / 2
                );

                string chunkName = $"{chunkPrefixName}{MIDDLE_PART_SORT_NAME}{chunk.Key}";
                chunkParent = new GameObject(chunkName)
                {
                    transform = { position = chunkCenter }
                };
                chunksParentList.Add(chunkParent);
                
                chunkParent.AddComponent<AWS_Chunk>().Initialize(m_chunkSize);
                chunkParent.tag = EDITOR_ONLY_TAG;

                foreach (var obj in chunk.Value)
                {
                    obj.SetParent(chunkParent.transform);
                }
            }
            
            // remove copies of empty chunk objects
            foreach (var item in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (item.CompareTag(EDITOR_ONLY_TAG) 
                    && item.name.Contains(MIDDLE_PART_SORT_NAME) 
                    && item.childCount == 0 
                    && item.gameObject.GetComponentCount() <= 2 
                   ) 
                    DestroyImmediate(item.gameObject);
            }

            return chunksParentList;
        }
        
        
        
        private static void MarkCurrentSceneDirty()
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
        

        #endregion
        

        /// <summary>
        /// Draws Gizmos to visually represent chunk boundaries for debugging in the editor.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!m_aws_Settings.ShowChunkSquareGizmos) return;
            
            Color colorWire = Color.green;
            HashSet<Vector3Int> drawnChunks = new HashSet<Vector3Int>();

            foreach (var obj in m_objectsToSortDebug)
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
                    
                    Gizmos.color = colorWire;
                    Gizmos.DrawWireCube(chunkCenter, m_chunkSize);
                }
            }
        }
    }
}

#endif
