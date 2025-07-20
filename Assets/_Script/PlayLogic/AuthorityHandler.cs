using System.Linq;
using UnityEngine;
using Fusion;

public class AuthorityHandler : NetworkBehaviour, IStateAuthorityChanged
{
	[Networked] System.Guid Guid { get; set; }

	private bool isAuthorityChanging = false;

	float timeRequested = 0;
	System.Action onAuthorized = null;
	System.Action onUnauthorized = null;

	public override void Spawned()
	{
		if (Object.HasStateAuthority) Guid = System.Guid.NewGuid();
	}

	public void RequestAuthority(System.Action onAuthorized = null, System.Action onUnauthorized = null)
	{
		timeRequested = Runner.SimulationTime;
		this.onAuthorized = onAuthorized;
		this.onUnauthorized = onUnauthorized;

        // 이 객체의 권한을 가진자가 살아있고 세션에 있음 → 권한자에게 권한을 요청하자
        // 즉, 이 객체의 권한을 가진자(Player ref)가 유효하고 세션(Runner.ActivePlayers)에 참여 중인 플레이어(p == Object.StateAuthority)일 때만 Rpc_RequestAuthority()를 호출해서 그 플레이어에게 권한을 요청합니다.
        if (!Object.StateAuthority.IsNone && Runner.ActivePlayers.FirstOrDefault(p => p == Object.StateAuthority) != default)
		{
			Rpc_RequestAuthority(Object.StateAuthority, GetHierarchyState());
		}
		else
        {// 3.4 권한자가 없거나 세션에서 나감 → 그냥 나 자신한테 권한을 주자 (매게변수로 마스터 클라의 id값 넘김 즉 나한테 달라)
            Rpc_Authorized(Runner.LocalPlayer);
		}
	}

	private System.Guid GetHierarchyState()
	{
		byte[] g1 = new byte[16];
		foreach (var authHandler in GetComponentsInChildren<AuthorityHandler>())
		{
			byte[] g2 = authHandler.Guid.ToByteArray();
			for (var i = 0; i < 16; i++)
			{
				g1[i] ^= g2[i];
			}
		}
		return new System.Guid(g1);
	}

	[Rpc(RpcSources.All, RpcTargets.All)]
	private void Rpc_RequestAuthority([RpcTarget] PlayerRef player, System.Guid expectedState, RpcInfo info = default)
	{
		
		if (Object.HasStateAuthority && !isAuthorityChanging && expectedState.Equals(GetHierarchyState()))
		{
			if (info.IsInvokeLocal)
			{
				onAuthorized?.Invoke();
				onAuthorized = null;
				onUnauthorized = null;
				timeRequested = 0;
			}//이RPC를 내가 호출한게 아닌 경우에 이 else를 탄다 즉 누가 이오브젝트의 권한이 필요하다고 요청한 것이다. 그래서 RPC를 누가 호출했는지 등의 정보를 알 수 있는 infoi를 아래에 넘긴다			
			else
			{
				isAuthorityChanging = true;
				Rpc_Authorized(info.Source);
				Log($"authorizing {info.Source} for {gameObject.name}", gameObject);
			}
		}
		else
		{
			Rpc_NotAuthorized(info.Source);
		}
	}

	[Rpc(RpcSources.All, RpcTargets.All)]
	private void Rpc_Authorized([RpcTarget] PlayerRef player)
	{
		Log("authorized...");
		AuthorityHandler[] authHandlers = GetComponentsInChildren<AuthorityHandler>();
		foreach (AuthorityHandler authHandler in authHandlers)
		{
            // 3.5 [Rpc(RpcSources.All, RpcTargets.All)]때문에 모든 클라 호출처럼 보이지만 [RpcTarget] PlayerRef player 때문에 All이 아니라 해당 클라에서만 실행 됨
            Log(authHandler.gameObject.name, gameObject);
			authHandler.Object.RequestStateAuthority();
		}
	}

	[Rpc(RpcSources.All, RpcTargets.All)]
	private void Rpc_NotAuthorized([RpcTarget] PlayerRef player)
	{
		Log("not authorized");
		onUnauthorized?.Invoke();
		onAuthorized = null;
		onUnauthorized = null;
	}

	[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
	public void Rpc_Despawn()
	{
		Runner.Despawn(Object);
	}

	private void Log(string message, Object ctx = null) => Debug.Log(string.Join('\n', message.Split('\n').Select(s => $"<color=#8080ff>{s}</color>")), ctx);

	public void StateAuthorityChanged()
	{
		if (isAuthorityChanging)
		{
			Log("authority changed.");
			isAuthorityChanging = false;
		}
	}

	public override void FixedUpdateNetwork()
	{
		if (onAuthorized != null)
		{
			if (GetComponentsInChildren<AuthorityHandler>().All(h => h.Object.HasStateAuthority))
			{
				Log("invoking authority action");
				onAuthorized();
				onAuthorized = null;
				onUnauthorized = null;
				timeRequested = 0;
			}
		}
	}

	public override void Render()
	{
		if (timeRequested > 0 && Runner.SimulationTime - timeRequested > 2)
		{
			Debug.LogWarning("timed out");
			timeRequested = 0;
			onUnauthorized?.Invoke();
			onAuthorized = null;
			onUnauthorized = null;
		}
	}
}