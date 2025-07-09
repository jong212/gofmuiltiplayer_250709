using UnityEngine;

public class GameRoot : MonoBehaviour
{
    public static GameRoot Instance { get; private set; }
    public Transform bodyRootContainer;

    void Awake()
    {
        Instance = this;
    }
}