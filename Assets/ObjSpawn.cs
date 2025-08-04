using Fusion;
using UnityEngine;
using static Unity.Collections.Unicode;

public class ObjSpawn : MonoBehaviour
{
    public Transform[] SpawnPoints;
    public MovingObstacle MovingObjPrefab;

    private void Start()
    {
        // StateAuthority(Host)만 스폰 실행
        if (MatchManager.Instance.Runner.IsSharedModeMasterClient)
        {
            foreach (var point in SpawnPoints)
            {
                MatchManager.Instance.Runner.Spawn(
                    MovingObjPrefab,                   // 프리팹
                    point.position,                    // 위치
                    point.rotation                     // 회전
                );
            }
        }
    }
    public void ClearMovingObstacles()
    {
        // 올바른 방법: 씬에 있는 모든 NetworkObject 검색
        var allNetObjs = FindObjectsOfType<NetworkObject>();
        foreach (var netObj in allNetObjs)
        {
                    Debug.Log("aaa");
            // 특정 스크립트 붙어있는지 체크
            if (netObj.TryGetComponent<MovingObstacle>(out _))
            {
                if (netObj.IsValid)
                    MatchManager.Instance.Runner.Despawn(netObj);
            }
        }
    }
}
