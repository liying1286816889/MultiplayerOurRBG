using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 场景里的“世界物件”（道具、掉落物、存档点、丢失的灵魂）的网络身份。
///
/// 规则：
/// - 服务器是唯一能销毁 / 激活它们的人。
/// - 客户端碰到道具时，道具先进自己背包（背包是每个人自己的），
///   再请求服务器把这个世界物件删掉，其它玩家也就看不到了。
/// </summary>
[DisallowMultipleComponent]
public class WorldObjectNetwork : NetworkBehaviour
{
    /// <summary>存档点是否已激活（服务器写入，所有人可见）。</summary>
    private readonly NetworkVariable<bool> activated =
        new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public bool IsActivated => activated.Value;

    public System.Action<bool> OnActivatedChanged;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        activated.OnValueChanged += HandleActivatedChanged;

        if (IsServer)
        {
            // 把服务器上的状态广播一次
            CheckPiont checkpoint = GetComponent<CheckPiont>();
            if (checkpoint != null && checkpoint.activationStatus && !activated.Value)
            {
                activated.Value = true;
            }
        }
        else
        {
            CheckPiont checkpoint = GetComponent<CheckPiont>();
            if (checkpoint != null && activated.Value && !checkpoint.activationStatus)
            {
                checkpoint.activationStatus = true;
                checkpoint.ApplyActivatedVisual();
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        activated.OnValueChanged -= HandleActivatedChanged;
        base.OnNetworkDespawn();
    }

    private void HandleActivatedChanged(bool previous, bool current)
    {
        if (IsServer) return;

        CheckPiont checkpoint = GetComponent<CheckPiont>();
        if (checkpoint != null && current && !checkpoint.activationStatus)
        {
            checkpoint.activationStatus = true;
            checkpoint.ApplyActivatedVisual();
        }

        OnActivatedChanged?.Invoke(current);
    }

    /// <summary>服务器：激活存档点并同步给所有人。</summary>
    public void ServerActivate()
    {
        if (!IsServer) return;
        activated.Value = true;
    }

    /// <summary>客户端：请求激活存档点。</summary>
    [ServerRpc(RequireOwnership = false)]
    public void ActivateServerRpc()
    {
        if (!IsServer) return;
        if (activated.Value) return;

        activated.Value = true;

        CheckPiont checkpoint = GetComponent<CheckPiont>();
        if (checkpoint != null && !checkpoint.activationStatus)
        {
            checkpoint.activationStatus = true;
            checkpoint.ApplyActivatedVisual();
        }
    }

    /// <summary>请求服务器销毁这个物件（道具被拾取时用）。</summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestDespawnServerRpc()
    {
        if (!IsServer) return;

        NetworkObject no = GetComponent<NetworkObject>();
        if (no != null && no.IsSpawned)
        {
            no.Despawn(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}

/// <summary>世界物件的通用工具方法。</summary>
public static class NetWorld
{
    /// <summary>
    /// 让某个世界物件在所有玩家那里消失。
    /// 单机模式直接 Destroy。
    /// </summary>
    public static void Despawn(GameObject target)
    {
        if (target == null) return;

        if (!NetGame.IsOnline)
        {
            Object.Destroy(target);
            return;
        }

        WorldObjectNetwork net = target.GetComponent<WorldObjectNetwork>();
        NetworkObject no = target.GetComponent<NetworkObject>();

        if (net == null || no == null || !no.IsSpawned)
        {
            // 不是联网物件（例如本地生成的特效），直接本地删除
            Object.Destroy(target);
            return;
        }

        if (NetGame.IsServer)
        {
            no.Despawn(true);
        }
        else
        {
            net.RequestDespawnServerRpc();
        }
    }
}
