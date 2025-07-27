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

    public Transform MatchPopup;
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
        MatchPopup.gameObject.SetActive(true);
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
        GameCancelBtn.onClick.AddListener(() => {MatchManager.Instance.Runner.Shutdown();});
    }

    private void Update()
    {
        if (Room_Mng.instance == null) return;

        var nickDict = Room_Mng.instance.Nicknames;
        var charDict = Room_Mng.instance.CharIdxArr;
        var activePlayers = MatchManager.Instance.Runner.ActivePlayers;

        List<(int name, int charId)> activePlayerData = new();

        List<PlayerRef> toRemove = new();

        foreach (var kvp in nickDict)
        {
            if (!activePlayers.Contains(kvp.Key))
            {
                toRemove.Add(kvp.Key);
            }
            else if (charDict.TryGet(kvp.Key, out var charId))
            {
                activePlayerData.Add((kvp.Value.GetHashCode(), charId)); // or convert name to int
            }
        }

        foreach (var player in toRemove)
        {
            nickDict.Remove(player);
            charDict.Remove(player);
        }

        UpdateLobbyUI(activePlayerData);
    }
    void UpdateLobbyUI(List<(int name, int charId)> activeData)
    {
        // [1] activeData 기준으로 슬롯 세팅
        foreach (var data in activeData)
        {
            // 이미 세팅되어 있으면 스킵
            bool exists = lobbyPlayerinfos.Any(slot => slot.Name == data.name);
            if (exists) continue;

            // 없으면 빈 슬롯에 세팅
            var emptySlot = lobbyPlayerinfos.FirstOrDefault(slot => slot.Name == 0);
            if (emptySlot != null)
            {
                emptySlot.Name = data.name;
                emptySlot.CharId = data.charId;
                // emptySlot.ApplyUI(...); // 필요하면 UI 갱신
            }
            else
            {
                Debug.LogWarning("❌ 빈 슬롯 없음! activeData가 슬롯 수보다 많음");
            }
        }

        // [2] 남아있는 슬롯 중 activeData에 없는 애들 → 초기화
        foreach (var slot in lobbyPlayerinfos)
        {
            if (slot.Name == 0) continue; // 이미 빈 슬롯이면 무시

            bool stillActive = activeData.Any(data => data.name == slot.Name);
            if (!stillActive)
            {
                slot.Settings(); // 초기화 (Name, CharId = 0 등)
            }
        }
    }
}
