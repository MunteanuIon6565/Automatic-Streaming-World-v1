using System;
using UnityEngine;

namespace AUTOMATIC_WORLD_STREAMING
{
    #if UNITY_EDITOR
    [RequireComponent(typeof(AWS_ChunksSorter))]
    #endif
    public class AWS_WorldStreamManager : MonoBehaviour
    {
        
        #region FIELDS
        
        
        public static AWS_WorldStreamManager Instance = null;
        public static Vector3 OffsetOrigin = Vector3.zero;
        
        [field: SerializeField] 
        public AWS_Settings m_aws_Settings { get; private set; }
        [field: SerializeField] 
        public AWS_AllChunksInOneWorld m_aws_Chunks { get; private set; }
        [SerializeField] 
        private bool m_autoInitializeInAwake = true;
        
        private Transform m_targetForStream;
        [SerializeField, Tooltip("By default is MainCamera")] 
        public Transform TargetForStream
        {
            get
            {
                if (!m_targetForStream) 
                    m_targetForStream = Camera.main.transform;
                
                return m_targetForStream;
            }
        }

        #endregion



        #region METHODS
        
        
        public void Initialize(Transform targetForStream = null)
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(this);
            }
            
            if (targetForStream) 
                m_targetForStream = targetForStream;
        }
        
        
        
        
        
        #endregion

        
        #region UNITY METHODS
        
        
        private void Awake()
        {
            if (m_autoInitializeInAwake) Initialize();
        }
        
        
        #endregion
    }
}