using Fusion;
using System.Collections;
using UnityEngine;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined, IPlayerLeft
{
	[field: SerializeField] public GameObject PlayerPrefab { get; private set; }

	public void PlayerJoined(PlayerRef player)
	{
		//InterfaceManager.instance.PrintPlayerCount(Runner.SessionInfo.PlayerCount, Runner.SessionInfo.MaxPlayers);

		if (player == Runner.LocalPlayer)
		{
			StartCoroutine(SpawnRoutine());
        }


	// Menu 씬에서는 아직 게임 매니저가 없기 때문에 대기하는게 맞고 씬이 로드 되고나서 겜매니저 마스터 클라가 스폰하면 그 때 진행됨
		IEnumerator SpawnRoutine()
		{
			Debug.Log("대기");
			yield return new WaitUntil(() => GameManager.instance != null);
			yield return new WaitForEndOfFrame();

				Debug.Log("Spawning player");

            Vector3 loc = new Vector3(0f, 0.05f, 0f);   // 지면보다 5 cm 위
            Runner.SpawnAsync(
					prefab: PlayerPrefab,
					position: loc,
					rotation: Quaternion.identity, // ⬅️ 강제로 (0,0,0) 회전
                    inputAuthority: player,
					onCompleted: (res) => { if (res.IsSpawned) { Runner.SetPlayerObject(Runner.LocalPlayer, res.Object); } }
				);
			
		}
	}

	public void PlayerLeft(PlayerRef player)
	{/*
		InterfaceManager.instance.PrintPlayerCount(Runner.SessionInfo.PlayerCount, Runner.SessionInfo.MaxPlayers);
		GameManager.instance.ReservedPlayerVisualsChanged();
		*/
	}
}