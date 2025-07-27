using Fusion;
using System.Linq;
using Unity.Collections;
using UnityEngine;

public class Room_Mng : NetworkBehaviour
{
    public static Room_Mng instance;

    public override void Spawned()
    {
        if (instance == null)
            instance = this;
    }
    [Header("�ǽð� �� ������ �� ")]
    [Networked] public int PlayerCount { get; private set; }    
    [Networked] public bool ReadyToStart { get; private set; }


    [Networked, Capacity(4)]
    public NetworkDictionary<PlayerRef, string> Nicknames => default;

    [Networked, Capacity(4)]
    public NetworkDictionary<PlayerRef, int> CharIdxArr => default;

    public override void Render()
    {
        if(LobbyManager.Instance != null) LobbyManager.Instance.playerCount.text = PlayerCount.ToString() + " / 4";
    }
 
    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        PlayerCount = Runner.ActivePlayers.Count();
        // ��: �ο��� 4�� �̻��� �� ���� ���� ���� ����
        if(  PlayerCount >=4)
        {
            ReadyToStart = true;
            Runner.SessionInfo.IsOpen = false; // �� �̻� Join �Ұ�
            Runner.SessionInfo.IsVisible = false; // �κ񡤸�ġ ����Ʈ������ ����
        }
    }
 
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SubmitNickname(string nick, int charId, RpcInfo info = default)
    {
        if (!Nicknames.ContainsKey(info.Source))
            Nicknames.Add(info.Source, nick);
            CharIdxArr.Add(info.Source, charId);        
    }
}
