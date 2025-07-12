using UnityEngine;

public class ResourcesManager : MonoBehaviour
{
    #region Singleton

    public static ResourcesManager instance;


    private void Awake()
    {
        if (instance && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    #endregion


}
