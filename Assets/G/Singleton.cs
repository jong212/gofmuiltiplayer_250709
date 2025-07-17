using UnityEngine;

public class Singleton : MonoBehaviour
{
    public static Singleton Instance { get; private set; }

    public LoginWithGoogle login_Mng { get; private set; }

    private void Awake()
    {
       if (Instance != null && Instance != this)
        { 
            Destroy(this.gameObject); // 이게 더 안전합니다
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject); // ✅ 씬이 바뀌어도 살아있게 만듦

       /* login_Mng = GetComponentInChildren<LoginWithGoogle>();
        login_Mng.Init(); */
    }
}