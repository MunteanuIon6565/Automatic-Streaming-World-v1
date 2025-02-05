using UnityEngine;

namespace AUTOMATIC_WORLD_STREAMING.FloatingOriginObjectsType
{
    public class AWS_FloatingOriginRigidbody : AWS_FloatingOriginObject
    {
        [SerializeField] private Rigidbody m_rigidbody;
        
        
        
        public override void ShiftPosition(Vector3 positionToShift)
        {
            base.ShiftPosition(positionToShift);
            
            /*Vector3 velocity = m_rigidbody.linearVelocity;
            bool isKinematic = m_rigidbody.isKinematic;
            
            m_rigidbody.isKinematic = true;*/
            m_rigidbody.position += positionToShift;
            /*m_rigidbody.isKinematic = isKinematic;
            
            m_rigidbody.linearVelocity = velocity;*/
        }

        protected override void Start()
        {
            m_rigidbody.position = transform.GetOriginPos();
            
            base.Start();
        }


        private void OnValidate()
        {
            m_rigidbody ??= GetComponent<Rigidbody>();
        }
    }
}