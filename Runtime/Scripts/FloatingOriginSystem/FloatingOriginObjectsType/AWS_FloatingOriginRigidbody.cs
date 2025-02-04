using UnityEngine;

namespace AUTOMATIC_WORLD_STREAMING.FloatingOriginObjectsType
{
    public class AWS_FloatingOriginRigidbody : AWS_FloatingOriginObject
    {
        [SerializeField] private Rigidbody m_rigidbody;
        
        
        
        public override void ShiftPosition(Vector3 positionToShift)
        {
            base.ShiftPosition(positionToShift);
            
            transform.position += positionToShift;
        }

        protected override void Start()
        {
            base.Start();
            
            transform.position = transform.GetOriginPos();
        }


        private void OnValidate()
        {
            //m_rigidbody ??= GetComponent<Rigidbody>();
        }
    }
}