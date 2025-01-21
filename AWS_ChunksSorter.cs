#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.IO;
using System.Linq;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine.AddressableAssets;


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
        
        private const string PATH_CREATE_CHUNKS = "Assets/Plugins/AWS_Chunks/";
        private const string AddressableGroupName = "AWS_Chunks_Group";
        private const string AddressableLabelName = "AWS_Chunks_Label";
        #endregion


        
        #region FIELDS

        
        [Header("<color=yellow>Set tag <color=white>EditorOnly</color> on objects which don't need to sort in chunks.\\\n(Sun,Player,Camera...) You can change these tags in AWS_Settings file.</color>")]
        [SerializeField] private AWS_Settings m_aws_Settings;
        [SerializeField] private AWS_AllChunksInOneWorld m_AllChunksInOneWorld;
        private Vector3 m_chunkSize => m_aws_Settings.ChunkSize;

        
        #endregion

        
        
        #region MAIN FUNCTIONAL

        
        [ContextMenu("2.Extract Chunk Objects From Scenes And Delete Scenes")]
        public void ExtractChunkObjectsFromScenesAndDeleteScenes()
        {
            var chunkDictionary = m_AllChunksInOneWorld.RebuildListToDictionary();

            var openedScenes = new List<Scene>();

            // Open all scenes additively
            foreach (var item in chunkDictionary)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(item.Value.SceneReference.AssetGUID);
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                openedScenes.Add(scene);
            }

            // Move chunk objects to the current scene
            foreach (var chunk in FindObjectsByType<AWS_Chunk>(FindObjectsSortMode.None))
            {
                SceneManager.MoveGameObjectToScene(chunk.gameObject, gameObject.scene);
            }

            // Delete the opened scenes after moving objects
            foreach (var scene in openedScenes)
            {
                string scenePath = scene.path;
                if (scene.isLoaded)
                {
                    if (scene.isDirty)
                        EditorSceneManager.SaveScene(scene);

                    EditorSceneManager.CloseScene(scene, true);

                    // Delete the scene asset
                    if (!string.IsNullOrEmpty(scenePath))
                    {
                        AssetDatabase.DeleteAsset(scenePath);
                        Debug.Log($"Scena '{scenePath}' a fost ștearsă.");
                    }
                }
            }
            
            m_AllChunksInOneWorld.chunkEntries.Clear();
            m_AllChunksInOneWorld.RebuildListToDictionary();

            EditorUtility.SetDirty(m_AllChunksInOneWorld);
            MarkCurrentSceneDirty();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }



        [ContextMenu("1.SORT TO CHUNKS")]
        public void SortToChunksByTags()
        {
            /*List<GameObject> chunkSimpleSort = null;*/
            List<GameObject> chunkSmallSort = null;
            List<GameObject> chunkMediumSort = null;
            List<GameObject> chunkLargeSort = null;
            
            /*if (!m_aws_Settings.UseStreamingBySizeObjects) 
                chunkSimpleSort = SortToChunksByTags(m_aws_Settings.AllUnityTagsForSortInSimpleMode, SIMPLE_SORT_NAME);
            else
            {*/
                chunkLargeSort = SortToChunksByTags(m_aws_Settings.UnityTagsLargeObjects, LARGE_OBJECT_NAME);
                chunkMediumSort = SortToChunksByTags(m_aws_Settings.UnityTagsMediumObjects, MEDIUM_OBJECT_NAME);
                chunkSmallSort = SortToChunksByTags(m_aws_Settings.UnityTagsSmallObjects, SMALL_OBJECT_NAME);
            /*}*/

            
            /*MoveObjectToSortChunkMini(chunkSimpleSort);*/
            MoveObjectToSortChunkMini(chunkLargeSort);
            MoveObjectToSortChunkMini(chunkMediumSort);
            MoveObjectToSortChunkMini(chunkSmallSort);

            
            void MoveObjectToSortChunkMini(List<GameObject> chunkSort)
            {
                if (chunkSort is { Count: > 0 })
                    for (int i = chunkSort.Count - 1; i >= 0; i--)
                        MoveObjectToSceneChunk(chunkSort[i]);
            }
            
            MarkCurrentSceneDirty();
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
            
            
            
            string currentScenePath = EditorSceneManager.GetActiveScene().path;
            string currentSceneName = Path.GetFileNameWithoutExtension(currentScenePath);
            
            string targetScenePath = PATH_CREATE_CHUNKS + $"{currentSceneName}_{objectToMove.name}.unity";
            AssetReference sceneAssetReference = default;
            
            
            if (!File.Exists(targetScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                EditorSceneManager.SaveScene(scene, targetScenePath);
                EditorSceneManager.CloseScene(scene, true);

                sceneAssetReference = AddSceneToAddressables(targetScenePath);
            }
            else
            {
                sceneAssetReference = new AssetReference(AssetDatabase.AssetPathToGUID(targetScenePath));
            }
            
            
            Scene targetScene = EditorSceneManager
                .OpenScene(
                    AssetDatabase.GUIDToAssetPath(sceneAssetReference.AssetGUID), 
                    OpenSceneMode.Additive
                    );

            
            if (objectToMove.scene.path != targetScenePath)
            {
                var foundAWSChunkObjects = FindObjectsByType<AWS_Chunk>(FindObjectsSortMode.None);
                foundAWSChunkObjects = foundAWSChunkObjects.Where(x => x.name.Equals(objectToMove.name)).ToArray();
                
                if (foundAWSChunkObjects.Length >= 2)
                {
                    string nameObjectToMove = objectToMove.name;
                    AWS_Chunk awsChunkMain = null;
                    objectToMove.name = "--Temporary To Move In Chunk--";
                    
                    SceneManager.MoveGameObjectToScene(objectToMove, targetScene);

                    foreach (var item in foundAWSChunkObjects)
                    {
                        if (item.name.Equals(nameObjectToMove))
                            for (int i = 0; i < item.transform.childCount; i++)
                                if (item.transform.parent.Equals(objectToMove.transform)) 
                                    item.transform.GetChild(i).SetParent(objectToMove.transform);
                    }
                    
                    objectToMove.name = nameObjectToMove;
                    RemoveCopiesOfEmptyChunkObjects();
                }
                else
                {
                    SceneManager.MoveGameObjectToScene(objectToMove, targetScene);
                }
                
                
                EditorSceneManager.MarkSceneDirty(targetScene);
                EditorSceneManager.SaveScene(targetScene);
            }


            string chunkCoordinateKey = objectToMove.name;
            if (!m_AllChunksInOneWorld.ChunkContainers.ContainsKey(chunkCoordinateKey))
            {
                m_AllChunksInOneWorld.ChunkContainers.Add(chunkCoordinateKey, new ChunkContainer());
            }
            

            m_AllChunksInOneWorld.ChunkContainers[chunkCoordinateKey].WorldPosition = objectToMove.transform.position;
            
            m_AllChunksInOneWorld.ChunkContainers[chunkCoordinateKey].SceneReference = sceneAssetReference;

            m_AllChunksInOneWorld.RebuildDictionaryToList(); 
            
            
            EditorUtility.SetDirty(m_AllChunksInOneWorld);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        
        
        private AssetReference AddSceneToAddressables(string scenePath)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);

            AddressableAssetGroup group = settings.FindGroup(AddressableGroupName);
            if (group == null)
                group = settings.CreateGroup(AddressableGroupName, false, false, true, null, typeof(BundledAssetGroupSchema));

            if (!settings.GetLabels().Contains(AddressableLabelName))
                settings.AddLabel(AddressableLabelName);

            string guid = AssetDatabase.AssetPathToGUID(scenePath);
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);
            entry.SetLabel(AddressableLabelName, true);

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            AssetDatabase.SaveAssets();

            Debug.Log($"Scena a fost adăugată la Addressables cu label-ul \"{AddressableLabelName}\" în grupul \"{AddressableGroupName}\".");

            return new AssetReference(guid);
        }


        
        private List<GameObject> SortToChunksByTags(string[] tagsFilters, string chunkPrefixName = "")
        {
            var chunks = new Dictionary<Vector3Int, List<Transform>>();

            List<Transform> m_objectsToSortDebug = GetObjectsToSortByTags(tagsFilters);

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

                string chunkName = $"{chunkPrefixName}{MIDDLE_PART_SORT_NAME}{chunk.Key.ToString()}";
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
            
            RemoveCopiesOfEmptyChunkObjects();

            return chunksParentList;
        }

        
        private static void RemoveCopiesOfEmptyChunkObjects()
        {
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
        }


        private static void MarkCurrentSceneDirty()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
                Debug.Log($"Scene '{activeScene.name}' has been marked as dirty.");
            }
            else
            {
                Debug.LogError("Active scene is not valid!");
            }
        }
        

        #endregion
        
        
        #region UNITY METHODS

        

        #endregion
        

        /// <summary>
        /// Draws Gizmos to visually represent chunk boundaries for debugging in the editor.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!m_aws_Settings.ShowChunkSquareGizmosAroundSceneCamera) return;

            Color colorWire = Color.green;
            Vector3Int cellsAroundCamera;
            
            if (!m_aws_Settings) 
                cellsAroundCamera = new Vector3Int(1, 1, 1);
            else
                cellsAroundCamera = m_aws_Settings.CellsAroundSceneCameraToShow;
                

            // Obține poziția camerei din fereastra Scene
            Camera sceneCamera = Camera.current;
            if (sceneCamera == null) return;

            Vector3 cameraPosition = sceneCamera.transform.position;

            Vector3Int cameraChunk = CalculateChunkCoordinate(cameraPosition);

            // Desenează cellsAroundCameraToShow celule în fiecare direcție de la chunk-ul camerei
            for (int x = cameraChunk.x - cellsAroundCamera.x; x <= cameraChunk.x + cellsAroundCamera.x; x++)
                for (int y = cameraChunk.y - cellsAroundCamera.y; y <= cameraChunk.y + cellsAroundCamera.y; y++)
                    for (int z = cameraChunk.z - cellsAroundCamera.z; z <= cameraChunk.z + cellsAroundCamera.z; z++)
                    {
                        Vector3 chunkCenter = new Vector3(
                            x * m_chunkSize.x + m_chunkSize.x / 2,
                            y * m_chunkSize.y + m_chunkSize.y / 2,
                            z * m_chunkSize.z + m_chunkSize.z / 2
                        );

                        Gizmos.color = colorWire;
                        Gizmos.DrawWireCube(chunkCenter, m_chunkSize);
                    }
        }
    }
}

#endif
