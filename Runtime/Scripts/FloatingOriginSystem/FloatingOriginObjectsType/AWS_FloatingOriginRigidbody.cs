using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AUTOMATIC_WORLD_STREAMING.FloatingOriginObjectsType
{
    public class AWS_FloatingOriginRigidbody : AWS_FloatingOriginObject
    {
        [SerializeField] private Rigidbody m_rigidbody;
        
        
        
        public async override void ShiftPosition(Vector3 positionToShift)
        {
            base.ShiftPosition(positionToShift);
            
            Vector3 velocity = m_rigidbody.linearVelocity;
            Vector3 position = m_rigidbody.position;
            bool isKinematic = m_rigidbody.isKinematic;
            m_rigidbody.linearVelocity = Vector3.zero;
            
            m_rigidbody.isKinematic = true;
            m_rigidbody.position += positionToShift;
            
            await UniTask.WaitForFixedUpdate();
            
            m_rigidbody.linearVelocity = Vector3.zero;
            
            await UniTask.WaitForFixedUpdate();
            
            m_rigidbody.isKinematic = isKinematic;
            m_rigidbody.position = position;
            m_rigidbody.linearVelocity = velocity;
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