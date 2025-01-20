using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace AUTOMATIC_WORLD_STREAMING
{
    [CreateAssetMenu(fileName = "ASW All Chunks In One World",
        menuName = "AUTOMATIC WORLD STREAMING/ASW All Chunks In One World", order = 0)]
    public class AWS_AllChunksInOneWorld : ScriptableObject
    {
        public List<ChunkContainer> ChunkContainers = new List<ChunkContainer>();
    }
    
    [System.Serializable]
    public class ChunkContainer
    {
        public Vector3 WorldPosition;
        public AssetReference SceneReference;
    }
}