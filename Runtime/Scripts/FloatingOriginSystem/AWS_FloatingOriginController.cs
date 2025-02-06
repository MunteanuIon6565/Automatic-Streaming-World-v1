using System.Collections.Generic;
using UnityEngine;


namespace AUTOMATIC_WORLD_STREAMING
{
    //[DefaultExecutionOrder(-1000)]
    public class AWS_FloatingOriginController : MonoBehaviour
    {
        public static AWS_FloatingOriginController Instance;
        public static Vector3 OriginOffset { get; private set; } = Vector3.zero;
        
        [Header("<color=yellow>Don't forget to add AWS_FloatingOriginObject to the target!")]
        [SerializeField] private Transform m_target;
        [SerializeField] private float m_threshold = 100f;

        private List<AWS_FloatingOriginObject> m_originShiftObjects;



        #region Initialization
        
        public void Initialize()
        {
            m_originShiftObjects ??= new List<AWS_FloatingOriginObject>();
            
            Instance ??= this;
            m_target ??= transform;
            if (m_target == null) Debug.LogError("No Rigidbody found!");
            
            CheckShiftWorld();
        }
        
        public void UnInitialize()
        {
            Instance = null;
            m_originShiftObjects = null;
            OriginOffset = Vector3.zero;
        }

        #endregion



        #region Main Functional
        
        private void ShiftAllObjectsToOrigin(Vector3 positionToShift)
        {
            //Physics.autoSimulation = false;
            
            OriginOffset += positionToShift;
            
            foreach (var item in m_originShiftObjects)
            {
                item.ShiftPosition(positionToShift);
            }
            
            //Physics.autoSimulation = true;
        }

        public void SubscribeObject(AWS_FloatingOriginObject obj)
        {
            if (!m_originShiftObjects.Contains(obj))
                m_originShiftObjects.Add(obj);
        }
        
        public void UnSubscribeObject(AWS_FloatingOriginObject obj)
        {
            if (m_originShiftObjects.Contains(obj))
                m_originShiftObjects.Remove(obj);
        }

        #endregion
        
        
        
        //



        #region UNITY FUNCS
        
        private void Awake()
        {
            Initialize();
        }

        private void FixedUpdate()
        {
            CheckShiftWorld();
        }

        private void CheckShiftWorld()
        {
            var referencePosition = m_target.position;

            if (referencePosition.magnitude >= m_threshold)
            {
                ShiftAllObjectsToOrigin(-referencePosition);
            }
        }

        private void OnDestroy()
        {
            UnInitialize();
        }

        #endregion



        #region EDITOR
        
        private void OnValidate()
        {
            m_target ??= transform;
        }

        private void OnDrawGizmos()
        {
            if (m_target) Gizmos.DrawWireSphere( new Vector3( 0, m_target.position.y, 0) /*m_target.position*/,m_threshold);
        }

        #endregion
    }




    public static class ExtensionsFunctionalAws
    {
        public static Vector3 GetOriginPos(this Transform transform)
        {
            return AWS_FloatingOriginController.OriginOffset + transform.position;
        }
    }
}