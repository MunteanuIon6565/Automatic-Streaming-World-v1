using UnityEngine;



namespace AUTOMATIC_WORLD_STREAMING
{
    public abstract class AWS_FloatingOriginObject : MonoBehaviour
    {
        public virtual void ShiftPosition(Vector3 positionToShift)
        {
            
        }
        
        protected virtual void Start()
        {
            AWS_FloatingOriginController.Instance?.SubscribeObject(this);
        }

        protected virtual void OnDestroy()
        {
            AWS_FloatingOriginController.Instance?.UnSubscribeObject(this);
        }
    }
}