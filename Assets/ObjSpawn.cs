using Fusion;
using UnityEngine;
using static Unity.Collections.Unicode;

public class ObjSpawn : MonoBehaviour
{
    public Transform[] SpawnPoints;
    public MovingObstacle MovingObjPrefab;

    private void Start()
    {
        // StateAuthority(Host)�� ���� ����
        if (MatchManager.Instance.Runner.IsSharedModeMasterClient)
        {
            foreach (var point in SpawnPoints)
            {
                MatchManager.Instance.Runner.Spawn(
                    MovingObjPrefab,                   // ������
                    point.position,                    // ��ġ
                    point.rotation                     // ȸ��
                );
            }
        }
    }
    public void ClearMovingObstacles()
    {
        // �ùٸ� ���: ���� �ִ� ��� NetworkObject �˻�
        var allNetObjs = FindObjectsOfType<NetworkObject>();
        foreach (var netObj in allNetObjs)
        {
                    Debug.Log("aaa");
            // Ư�� ��ũ��Ʈ �پ��ִ��� üũ
            if (netObj.TryGetComponent<MovingObstacle>(out _))
            {
                if (netObj.IsValid)
                    MatchManager.Instance.Runner.Despawn(netObj);
            }
        }
    }
}
