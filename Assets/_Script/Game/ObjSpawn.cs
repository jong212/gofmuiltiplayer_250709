using Fusion;
using UnityEngine;

public class ObjSpawn : MonoBehaviour
{
    public Transform[] MovingSpawnPoints;
    public MovingObstacle MovingObjPrefab;

    public Transform[] PunchSpawnPoints;
    public SlidingObstacle PunchObjPrefab;
    private void Start()
    {
        // StateAuthority(Host)�� ���� ����
        if (MatchManager.Instance.Runner.IsSharedModeMasterClient)
        {
            foreach (var point in MovingSpawnPoints)
            {
                MatchManager.Instance.Runner.Spawn(
                    MovingObjPrefab,                   // ������
                    point.position,                    // ��ġ
                    point.rotation                     // ȸ��
                );
            }
            foreach (var point in PunchSpawnPoints)
            {
                MatchManager.Instance.Runner.Spawn(
                    PunchObjPrefab,                   // ������
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
            if (netObj.TryGetComponent<SlidingObstacle>(out _))
            {
                if (netObj.IsValid)
                    MatchManager.Instance.Runner.Despawn(netObj);
            }
        }
    }
}
