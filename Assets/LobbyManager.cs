using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }
    [SerializeField] public GameObject[] sprites;
    public Button GamestartBtn;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject); 
            return;
        }

        Instance = this;
        ManagerSystem.Instance.LobbySceneStepByCall("9_LobbySceneInit");
    }

    public void Step9()
    {
        InitResource();
        InitGameLogic();
    }
    public void InitResource()
    {
  
       foreach (var item in sprites)
        {
            Sprite r_sprite = AddressableMng.instance.GetSprite("lobbyui", item.name);
            Debug.Log(r_sprite.ToString());
            if (item.TryGetComponent<Image>(out var image))
            {
                image.sprite = r_sprite;
            }
            else if (item.TryGetComponent<RawImage>(out var rawImage))
            {
                rawImage.texture = r_sprite.texture; // ✅ 이렇게 수정
            }
            else
            {
                Debug.Log("NoResource");
            }

        }
    }
    public void InitGameLogic()
    {
        GamestartBtn.onClick.AddListener(ManagerSystem.Instance.MatchManager.TryConnectShared);
    }
}
