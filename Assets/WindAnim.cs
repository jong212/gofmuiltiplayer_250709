using UnityEngine;

public class WindAnim : MonoBehaviour
{
    public Transform test1;

    private void Start()
    {
        LobbyManager.Instance.GetComponent<BgScroll>().RegisterWindTarget(test1);
    }
}
