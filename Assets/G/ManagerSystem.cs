using UnityEngine;

public class ManagerRoot : MonoBehaviour
{
    public static ManagerRoot Instance { get; private set; }

    public InterfaceManager UIManager{ get; private set; }
    public MatchManager MatchManager{ get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject); // 이게 더 안전합니다
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject); // ✅ 씬이 바뀌어도 살아있게 만듦

        UIManager = GetComponentInChildren<InterfaceManager>();
        MatchManager = GetComponentInChildren<MatchManager>();
        UIManager.Init();
        MatchManager.Init();
    }
}
