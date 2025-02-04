using UnityEngine;

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.IO;
using System.Linq;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine.AddressableAssets;
using Task = System.Threading.Tasks.Task;
#endif

namespace AUTOMATIC_WORLD_STREAMING
{
#if UNITY_EDITOR
    [ExecuteInEditMode]
#endif
    public class AWS_ChunksSorter : MonoBehaviour
    {
#if UNITY_EDITOR
        #region CONSTANTS
        private const string SMALL_OBJECT_NAME = "Small";
        private const string MEDIUM_OBJECT_NAME = "Medium";
        private const string LARGE_OBJECT_NAME = "Large";
        private const string MIDDLE_PART_SORT_NAME = "_Objects_Chunk_"; // it is used for delete copies of empty objects create with chunk system
        
        private const string PATH_CREATE_CHUNKS = "Assets/Plugins/AWS_Chunks/";
        private const string AddressableGroupName = "AWS_Chunks_Group";
        private const string AddressableLabelName = "AWS_Chunks_Label";
        #endregion


        
        #region FIELDS

        
        [Header("<color=yellow>Set tag <color=white>Untagged</color> on objects which don't need to sort in chunks.\\\n(Sun,Player,Camera...) You can change these tags in AWS_Settings file.</color>")]
        [SerializeField] private AWS_Settings m_aws_Settings;
        [SerializeField] private AWS_AllChunksInOneWorld m_AllChunksInOneWorld;
        private Vector3 m_chunkSize => m_aws_Settings.ChunkSize;

        
        #endregion

        
        
        #region MAIN FUNCTIONAL

        
        [ContextMenu("2.EXTRACT Chunk Objects From Scenes And Delete Scenes")]
        public void ExtractChunkObjectsFromScenesAndDeleteScenes()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("Exit from Play Mode before execute this.");
                return;
            }
            
            
            var chunkDictionary = m_AllChunksInOneWorld.RebuildListToDictionary();
            var openedScenes = new List<Scene>();

            try
            {
                EditorUtility.DisplayProgressBar("Extract chunk objects", "Prepare for extract...", 0.0f);

                int i = 0;
                foreach (var item in chunkDictionary)
                {
                    EditorUtility.DisplayProgressBar("Extract chunk objects", $"Extract chunk: {item.Key}", 
                        (float)i / chunkDictionary.Count);
                    string scenePath = AssetDatabase.GUIDToAssetPath(item.Value.SceneReference.AssetGUID);
                    var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                    openedScenes.Add(scene);

                    i++;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error: {ex.Message}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            // Move chunk objects to the current scene
            foreach (var chunk in FindObjectsByType<AWS_Chunk>(FindObjectsSortMode.None))
            {
                SceneManager.MoveGameObjectToScene(chunk.gameObject, gameObject.scene);
            }

            
            try
            {
                EditorUtility.DisplayProgressBar("Delete old chunk scenes", "Prepare for delete...", 0.0f);

                int i = 0;
                List<string> pathsList = new List<string>();
                List<string> failedPathsList = new List<string>();
                
                // Delete the opened scenes after moving objects
                foreach (var scene in openedScenes)
                {
                    string scenePath = scene.path;
                    if (scene.isLoaded)
                    {
                        EditorUtility.DisplayProgressBar("Delete old chunk scenes", $"Delete chunk scene: {scene.name}", 
                            (float)i / openedScenes.Count);
                        
                        EditorSceneManager.CloseScene(scene, true);

                        if (!string.IsNullOrEmpty(scenePath))
                        {
                            pathsList.Add(scenePath);
                        }
                    }
                    i++;
                }
                
                AssetDatabase.DeleteAssets(pathsList.ToArray(), failedPathsList);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error: {ex.Message}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            
            m_AllChunksInOneWorld.chunkEntries.Clear();
            m_AllChunksInOneWorld.RebuildListToDictionary();

            EditorUtility.SetDirty(m_AllChunksInOneWorld);
            MarkCurrentSceneDirty();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }



        [ContextMenu("1.SORT TO CHUNKS")]
        public async void SortToChunksByTagsContextMenu()
        {
            await SortToChunksByTags();
            
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            EditorSceneManager.OpenScene(EditorSceneManager.GetActiveScene().path, OpenSceneMode.Additive);
        }

        public async Task SortToChunksByTags()
        {
            List<GameObject> chunkSmallSort = null;
            List<GameObject> chunkMediumSort = null;
            List<GameObject> chunkLargeSort = null;
            
            if (!AssetDatabase.IsValidFolder(PATH_CREATE_CHUNKS))
            {
                Directory.CreateDirectory(PATH_CREATE_CHUNKS);
                AssetDatabase.Refresh();
            }

            chunkLargeSort = SortToChunksByTags(m_aws_Settings.UnityTagsLargeObjects, LARGE_OBJECT_NAME);
            chunkMediumSort = SortToChunksByTags(m_aws_Settings.UnityTagsMediumObjects, MEDIUM_OBJECT_NAME);
            chunkSmallSort = SortToChunksByTags(m_aws_Settings.UnityTagsSmallObjects, SMALL_OBJECT_NAME);

            GenerateEmptyChunkScenes(chunkLargeSort);
            GenerateEmptyChunkScenes(chunkMediumSort);
            GenerateEmptyChunkScenes(chunkSmallSort);

            MoveObjectToSortChunkMini(chunkLargeSort);
            MoveObjectToSortChunkMini(chunkMediumSort);
            MoveObjectToSortChunkMini(chunkSmallSort);

            void MoveObjectToSortChunkMini(List<GameObject> chunkSort)
            {
                if (Application.isPlaying)
                {
                    Debug.LogError("Exit from Play Mode before execute this.");
                    return;
                }

                EditorUtility.DisplayProgressBar("Save Chunk Scenes", "Prepare for save...", 0.0f);

                try
                {
                    if (chunkSort is { Count: > 0 })
                        for (int i = chunkSort.Count - 1; i >= 0; i--)
                        {
                            EditorUtility.DisplayProgressBar(
                                "Save Chunk Scenes",
                                $"Save scene for chunk: {chunkSort[i].name}",
                                -(float)i / chunkSort.Count + 1
                            );

                            MoveObjectToSceneChunk(chunkSort[i]);
                        }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error: {ex.Message}");
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            }

            MarkCurrentSceneDirty();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SaveAllChunksScenesOpened();
            
            Debug.Log("SORT TO CHUNKS Finished.");
            
            await Task.Delay(100);
        }

        
        
        private void GenerateEmptyChunkScenes(List<GameObject> chunkSort)
        {
            if (chunkSort == null || chunkSort.Count == 0) return;

            string templateScenePath = $"{PATH_CREATE_CHUNKS}__TemplateChunkScene.unity";
            if (!File.Exists(templateScenePath))
            {
                Scene templateScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                EditorSceneManager.SaveScene(templateScene, templateScenePath);
                Debug.Log($"Template scene created: {templateScenePath}");
                EditorSceneManager.CloseScene(templateScene, true);
            }

            for (int i = 0; i < chunkSort.Count; i++)
            {
                string chunkSceneName = $"{chunkSort[i].scene.name}_{chunkSort[i].name}.unity";
                string chunkScenePath = PATH_CREATE_CHUNKS + chunkSceneName;

                if (!File.Exists(chunkScenePath))
                {
                    File.Copy(templateScenePath, chunkScenePath);
                    Debug.Log($"Chunk scene created: {chunkScenePath}");
                }
            }

            AssetDatabase.Refresh();
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
            
            
            Scene currentActiveScene = EditorSceneManager.GetActiveScene();
            string currentScenePath = currentActiveScene.path;
            string currentSceneName = Path.GetFileNameWithoutExtension(currentScenePath);

            string chunkSceneName = $"{currentSceneName}_{objectToMove.name}.unity";
            string targetScenePath = PATH_CREATE_CHUNKS + chunkSceneName;
            AssetReference sceneAssetReference = default;
            Scene targetScene = default;
            
            
            if (!File.Exists(targetScenePath))
            {
                targetScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                targetScene.name = chunkSceneName;
                EditorSceneManager.SetActiveScene(currentActiveScene);
                EditorSceneManager.SaveScene(targetScene, targetScenePath);

                sceneAssetReference = AddSceneToAddressables(targetScenePath);
            }
            else
            {
                //sceneAssetReference = new AssetReference(AssetDatabase.AssetPathToGUID(targetScenePath));
                sceneAssetReference = AddSceneToAddressables(targetScenePath);

                string scenePath = AssetDatabase.GetAssetPath(sceneAssetReference.editorAsset);
                
                targetScene = EditorSceneManager.GetSceneByPath(scenePath);
                if (!targetScene.isLoaded)
                {
                    targetScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                }
            }

            
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

            Debug.Log($"Scena a fost adăugată la Addressables cu label-ul \"{AddressableLabelName}\" în grupul \"{AddressableGroupName}\".");

            return new AssetReference(guid);
        }



        private void SaveAllChunksScenesOpened()
        {
            List<Scene> scenesToUnload = new List<Scene>();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene loadedScene = SceneManager.GetSceneAt(i);
                if (loadedScene.name != gameObject.scene.name && loadedScene.isDirty)
                    scenesToUnload.Add(loadedScene);
            }
            EditorSceneManager.SaveScenes(scenesToUnload.ToArray());
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
                //chunkParent.tag = EDITOR_ONLY_TAG;

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
                if (/*item.CompareTag(EDITOR_ONLY_TAG) 
                    &&*/ item.name.Contains(MIDDLE_PART_SORT_NAME) 
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
                //EditorSceneManager.SaveScene(activeScene);
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
#endif
    }
}

