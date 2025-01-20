using System;
using UnityEngine;

namespace AUTOMATIC_WORLD_STREAMING
{
    #if UNITY_EDITOR
    [RequireComponent(typeof(AWS_ChunksSorter))]
    #endif
    public class AWS_WorldStreamManager : MonoBehaviour
    {
        public static AWS_WorldStreamManager Instance = null;
        public static Vector3 OffsetOrigin = Vector3.zero;
        
        [field: SerializeField] public AWS_Settings m_aws_Settings { get; private set; }
        [SerializeField] private bool m_autoInitializeInAwake = true;



        public void Initialize()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(this);
            }
        }

        private void Awake()
        {
            if (m_autoInitializeInAwake) Initialize();
        }
    }
}