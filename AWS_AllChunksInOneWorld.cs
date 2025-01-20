using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;



namespace AUTOMATIC_WORLD_STREAMING
{
    [CreateAssetMenu(fileName = "ASW All Chunks In One World",
        menuName = "AUTOMATIC WORLD STREAMING/ASW All Chunks In One World", order = 0)]
    public class AWS_AllChunksInOneWorld : ScriptableObject
    {
        [SerializeField] public List<ChunkEntry> chunkEntries = new List<ChunkEntry>();
        
        [SerializeField] public Dictionary<string, ChunkContainer> ChunkContainers = new Dictionary<string, ChunkContainer>();
        
        
        
        public Dictionary<string, ChunkContainer> RebuildListToDictionary()
        {
            ChunkContainers.Clear();
            foreach (var entry in chunkEntries)
            {
                if (!ChunkContainers.ContainsKey(entry.Key))
                {
                    ChunkContainers.Add(entry.Key, entry.Value);
                }
            }
            return ChunkContainers;
        }
        
        public List<ChunkEntry> RebuildDictionaryToList()
        {
            chunkEntries.Clear();

            foreach (var kvp in ChunkContainers)
            {
                chunkEntries.Add(new ChunkEntry
                {
                    Key = kvp.Key,
                    Value = kvp.Value
                });
            }
            return chunkEntries;
        }
    }


    [Serializable]
    public enum SizeObject
    {
        Small,
        Medium,
        Large
    }

    [Serializable]
    public class ChunkEntry
    {
        public string Key;
        public ChunkContainer Value;
    }

    [Serializable]
    public class ChunkContainer
    {
        public Vector3 WorldPosition;
        public AssetReference SceneReference;
    }
}