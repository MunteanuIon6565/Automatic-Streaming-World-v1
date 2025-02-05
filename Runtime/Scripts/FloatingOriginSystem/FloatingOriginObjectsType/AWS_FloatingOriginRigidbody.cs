using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AUTOMATIC_WORLD_STREAMING.FloatingOriginObjectsType
{
    public class AWS_FloatingOriginRigidbody : AWS_FloatingOriginObject
    {
        [SerializeField] private Rigidbody[] m_rigidbodies;
        
        
        
        public async override void ShiftPosition(Vector3 positionToShift)
        {
            base.ShiftPosition(positionToShift);

            foreach (Rigidbody rigidbody in m_rigidbodies)
            {
                Vector3 velocity = rigidbody.linearVelocity;
                //bool isKinematic = rigidbody.isKinematic;
                
                //rigidbody.isKinematic = true;
                rigidbody.position += positionToShift;
                //Vector3 position = rigidbody.position;
                
                rigidbody.linearVelocity = Vector3.zero;
                //rigidbody.isKinematic = isKinematic;
                //rigidbody.position = position;
                
                //rigidbody.linearVelocity = Vector3.zero;
                rigidbody.linearVelocity = velocity;
            }
            
        }

        protected override void Start()
        {
            foreach (Rigidbody rigidbody in m_rigidbodies)
                rigidbody.position = transform.GetOriginPos();
            
            base.Start();
        }


        private void OnValidate()
        {
            m_rigidbodies ??= GetComponentsInChildren<Rigidbody>();
        }
    }
}