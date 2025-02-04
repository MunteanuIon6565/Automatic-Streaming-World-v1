using System.Collections.Generic;
using UnityEngine;



namespace AUTOMATIC_WORLD_STREAMING
{
    [RequireComponent(typeof(SphereCollider))]
    [DefaultExecutionOrder(1000)]
    public class AWS_FloatingOriginController : MonoBehaviour
    {
        public static AWS_FloatingOriginController Instance;
        public static Vector3 OriginOffset { get; private set; } = Vector3.zero;
        
        [SerializeField] private Rigidbody m_targetRigidbody;

        private List<AWS_FloatingOriginObject> m_originShiftObjects;
        private float m_threshold;



        public void Initialize()
        {
            m_originShiftObjects ??= new List<AWS_FloatingOriginObject>();
            
            Instance ??= this;
            
            var sphereCollider = GetComponent<SphereCollider>();
            m_threshold = sphereCollider.radius;
        }
        
        public void UnInitialize()
        {
            Instance = null;
            m_originShiftObjects = null;
            OriginOffset = Vector3.zero;
        }
        
        

        private void ShiftAllObjectsToOrigin(Vector3 positionToShift)
        {
            //shift player
            
            OriginOffset += positionToShift;
            
            foreach (var item in m_originShiftObjects)
            {
                item.ShiftPosition(positionToShift);
            }
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
        
        
        
        //
        
        
        
        private void Awake()
        {
            Initialize();
        }

        private void FixedUpdate()
        {
            var referencePosition = m_targetRigidbody.position;

            if (referencePosition.magnitude >= m_threshold)
            {
                ShiftAllObjectsToOrigin(-referencePosition);
            }
        }

        private void OnDestroy()
        {
            UnInitialize();
        }
    }




    public static class ExtensionsFunctionalAws
    {
        public static Vector3 GetOriginPos(this Transform transform)
        {
            return AWS_FloatingOriginController.OriginOffset + transform.position;
        }
    }
}