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
    [Header("�ǽð� �� ������ �� ")]
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
        // ��: �ο��� 4�� �̻��� �� ���� ���� ���� ����
        if( PlayerCount >=2)
        {
            ReadyToStart = true;
            Runner.SessionInfo.IsOpen = false; // �� �̻� Join �Ұ�
            Runner.SessionInfo.IsVisible = false; // �κ񡤸�ġ ����Ʈ������ ����
        }
    }
}
