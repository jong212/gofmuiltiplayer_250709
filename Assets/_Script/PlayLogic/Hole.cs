using UnityEngine;

public class Hole : MonoBehaviour
{
    /*    private bool _doubleTriggerFlag;
        private void OnTriggerEnter(Collider other)
        {
            if (!_doubleTriggerFlag)
            {
                var putter = other.GetComponentInParent<Putter>();
                if (putter != null && putter.Object != null && putter.HasStateAuthority)
                {
                    _doubleTriggerFlag = true;
                    putter.TryRegisterGoalArrival();
                }
            }
        }
    */

    private bool _doubleTriggerFlag;

    private void OnCollisionEnter(Collision collision)
    {
        if (_doubleTriggerFlag) return;

        var putter = collision.collider.GetComponentInParent<Putter>();
        if (putter != null && putter.Object != null && putter.HasStateAuthority)
        {
            _doubleTriggerFlag = true;
            putter.TryRegisterGoalArrival();
        }
    }
}
