using UnityEngine;

namespace AUTOMATIC_WORLD_STREAMING.FloatingOriginObjectsType
{
    public class AWS_FloatingOriginRigidbody : AWS_FloatingOriginObject
    {
        [SerializeField] private Rigidbody m_rigidbody;
        
        
        
        public override void ShiftPosition(Vector3 positionToShift)
        {
            base.ShiftPosition(positionToShift);
            
            m_rigidbody.position += positionToShift;
        }

        protected override void Start()
        {
            base.Start();
            
            m_rigidbody.position = transform.GetOriginPos();
        }


        private void OnValidate()
        {
            m_rigidbody ??= GetComponent<Rigidbody>();
        }
    }
}