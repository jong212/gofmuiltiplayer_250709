using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }
    [SerializeField] public GameObject[] sprites;

    public Transform BG_UI;
    public Transform Lobby_UI;
    public Transform Room_UI;

    public Transform Room_Parent;
    public Transform Room_Addressable_Instance;

    public Button GamestartBtn;
    public Button GameCancelBtn;
    public TextMeshProUGUI playerCount;

    public LobbyPlayerinfo[] lobbyPlayerinfos;
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
    public void Step10()
    {
        StartCoroutine(NickRpc()); 
    }
    IEnumerator NickRpc()
    {
        yield return new WaitUntil(() => Room_Mng.instance != null);
        int rand = Random.Range(0, 101); // 테스트 할때만 랜덤값 해서 같이 넘김
        Room_Mng.instance.RPC_SubmitNickname(ManagerSystem.Instance.BackendCash.Nick, ManagerSystem.Instance.BackendCash.UserData.SelectedCharId);
        Room_Parent.gameObject.SetActive(true);
        Room_UI.gameObject.SetActive(true);
        BG_UI.gameObject.SetActive(false);
        Lobby_UI.gameObject.SetActive(false);
    }
    public void InitResource()
    {
       // Lobby UI Init
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
        // Lobby Room UI Init
        GameObject RoomObj = AddressableMng.instance.GetPrefab("obj", "MatchUI");
        var roomObj = Instantiate(RoomObj, Room_Parent);
        Room_Addressable_Instance = roomObj.transform;
        Room_Parent.gameObject.SetActive(false);


    }
    public void InitGameLogic()
    {
        GamestartBtn.onClick.AddListener(ManagerSystem.Instance.MatchManager.TryConnectShared);
        GameCancelBtn.onClick.AddListener(() => {MatchManager.Instance.Runner.Shutdown();});
    }

    private void Update()
    {

        RenderSettings.skybox.SetFloat("_Rotation", Time.time * 0.5f);
        UpdateLobbyUI();
    }
    void UpdateLobbyUI()
    {
 
        if (Room_Mng.instance == null || Room_Mng.instance.Nicknames.Count == 0)
            return;
        if (!Room_Mng.instance.Object )
            return;
        var nicknames = Room_Mng.instance.Nicknames;

        // 1. Nicknames 기준으로, 아직 세팅 안된 PlayerRef면 슬롯에 세팅
        foreach (var kv in nicknames)
        {
            string playerRefStr = kv.Key.ToString(); // 중요 포인트
            string nick = kv.Value.Nick.ToString();
            int charId = kv.Value.CharId;

            bool isAlreadySet = false;

            foreach (var info in lobbyPlayerinfos)
            {
                if (info.PlayerRefId == playerRefStr)
                {
                    // 세팅 되어있네? 세팅할 필요가 없군 그럼 break !
                    isAlreadySet = true;
                    break;
                }
            }

            // 세팅 안 되어있네? 빈 거 찾자
            if (!isAlreadySet)
            {
                foreach (var info in lobbyPlayerinfos)
                {
                    if (string.IsNullOrEmpty(info.PlayerRefId))
                    {
                        info.SetPlayer(playerRefStr, nick, charId); // 아래 참고
                        break;
                    }
                }
            }
        }

        // 2. 슬롯에 세팅된 PlayerRef 중에 Nicknames에 없는 애들 제거
        foreach (var info in lobbyPlayerinfos)
        {
            if (string.IsNullOrEmpty(info.PlayerRefId)) continue;

            bool existsInNicknames = false;

            foreach (var kv in nicknames)
            {
                if (info.PlayerRefId == kv.Key.ToString())
                {
                    existsInNicknames = true;
                    break;
                }
            }

            if (!existsInNicknames)
            {
                info.ClearSlot(); // 아래 참고
            }
        }
    }



}
