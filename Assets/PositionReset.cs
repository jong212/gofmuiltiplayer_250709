using UnityEngine;

public class PositionReset : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
            var putter = other.GetComponentInParent<Putter>();
            if (putter != null && putter.Object != null && putter.HasStateAuthority)
            {
                putter.TeleportTo(new Vector3(0,3f,0));
            }
    }
}
