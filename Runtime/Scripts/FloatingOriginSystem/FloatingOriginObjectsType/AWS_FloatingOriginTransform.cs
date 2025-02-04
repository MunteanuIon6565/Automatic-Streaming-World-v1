using UnityEngine;

namespace AUTOMATIC_WORLD_STREAMING.FloatingOriginObjectsType
{
    public class AWS_FloatingOriginTransform : AWS_FloatingOriginObject
    {
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
    }
}