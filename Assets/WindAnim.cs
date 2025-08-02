using UnityEngine;

public class WindAnim : MonoBehaviour
{
    public Transform[] test1;

    private void Start()
    {
        foreach(Transform t in test1)
        {
            LobbyManager.Instance.GetComponent<BgScroll>().RegisterWindTarget(t);
        }
    }
}
