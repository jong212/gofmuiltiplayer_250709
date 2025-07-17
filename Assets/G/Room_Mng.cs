using Fusion;
using System.Linq;
using UnityEngine;

public class Room_Mng : NetworkBehaviour
{
    public static Room_Mng instance;

    private void Awake()
    {
        if (instance)
        {
            Debug.LogWarning("Instance already exists!");
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
    [Header("실시간 방 참여자 수 ")]
    [Networked] public int PlayerCount { get; private set; }    
    [Networked] public bool ReadyToStart { get; private set; }
 
    public override void Render()
    {
        InterfaceManager.instance.coomPlayerCount.text = PlayerCount.ToString();
     }
 
    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        PlayerCount = Runner.ActivePlayers.Count();
        // 예: 인원이 4명 이상일 때 게임 시작 조건 만족
        if( PlayerCount >=2)
        {
            ReadyToStart = true;
            Runner.SessionInfo.IsOpen = false; // 더 이상 Join 불가
            Runner.SessionInfo.IsVisible = false; // 로비·매치 리스트에서도 숨김
        }
    }
}
