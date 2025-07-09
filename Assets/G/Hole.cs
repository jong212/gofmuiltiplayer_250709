using UnityEngine;

public class Hole : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        var putter = other.GetComponentInParent<Putter>();
        if (putter != null)
        {
            putter.TryRegisterGoalArrival();
        }
    }
}
