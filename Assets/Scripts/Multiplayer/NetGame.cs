using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 联机状态的统一入口（唯一直接访问 Unity.Netcode.NetworkManager 的地方之一）。
///
/// 设计原则：
/// 1) 整个工程改造后依然可以在“单机模式”下运行。只要没有启动联机，
///    <see cref="IsOnline"/> 就永远是 false，所有业务脚本都会走改造前的原逻辑。
/// 2) 每台客户端只负责“自己的玩家 + 自己的 UI / 背包 / 技能 / 存档”，
///    因此原有的各种单例（PlayerManager / Inventory / SkillManager / UI ...）
///    可以继续保留单例形态，只需要把它们绑定到“本地玩家”即可。
/// </summary>
public static class NetGame
{
    /// <summary>联机是否已经启动（Host 或 Client 正在监听）。</summary>
    public static bool IsOnline
    {
        get
        {
            NetworkManager nm = NetworkManager.Singleton;
            return nm != null && nm.IsListening;
        }
    }

    /// <summary>本机是否是服务器（Host 也算服务器）。</summary>
    public static bool IsServer => IsOnline && NetworkManager.Singleton.IsServer;

    /// <summary>本机是否是客户端。</summary>
    public static bool IsClient => IsOnline && NetworkManager.Singleton.IsClient;

    /// <summary>
    /// 是否是「专用服务端」：只跑服务器逻辑，自己没有本地玩家、也不显示画面。
    /// 等价于"是服务器但不是客户端"，Host 模式不算。
    /// </summary>
    public static bool IsDedicatedServer => IsServer && !IsClient;

    /// <summary>本机是否是 Host（既是服务器又是客户端）。</summary>
    public static bool IsHost => IsServer && IsClient;

    /// <summary>是否是“纯客户端”（只连服务器，不跑服务器逻辑）。</summary>
    public static bool IsPureClient => IsClient && !IsServer;

    // ------------------------------------------------------------------
    // 本地玩家
    // ------------------------------------------------------------------

    private static PlayerNetwork localPlayer;

    /// <summary>本机控制的那一个玩家。联机未启动时返回 null（此时用场景里原本的 Player）。</summary>
    public static PlayerNetwork LocalPlayer
    {
        get
        {
            if (localPlayer == null && IsOnline)
            {
                // 兜底：如果因为时序问题没有被登记，就重新扫一遍
                RefreshLocalPlayer();
            }
            return localPlayer;
        }
    }

    /// <summary>本机玩家的 <see cref="Player"/> 组件。联机未启动时返回 null。</summary>
    public static Player LocalPlayerEntity => LocalPlayer != null ? LocalPlayer.Player : null;

    /// <summary>当前联机中是否存在本地玩家。</summary>
    public static bool HasLocalPlayer => LocalPlayer != null;

    // ------------------------------------------------------------------
    // 所有玩家
    // ------------------------------------------------------------------

    private static readonly List<PlayerNetwork> allPlayers = new List<PlayerNetwork>();

    /// <summary>当前场景里的所有玩家（服务器上包含全部玩家）。</summary>
    public static IReadOnlyList<PlayerNetwork> Players => allPlayers;

    public static int PlayerCount => allPlayers.Count;

    public static void RegisterPlayer(PlayerNetwork player)
    {
        if (player == null) return;

        if (!allPlayers.Contains(player))
        {
            allPlayers.Add(player);
            allPlayers.Sort(CompareByOwner);
        }

        if (player.IsOwner)
        {
            localPlayer = player;
        }

        MultiplayerEvents.RaisePlayerListChanged();
    }

    public static void UnregisterPlayer(PlayerNetwork player)
    {
        if (player == null) return;

        allPlayers.Remove(player);

        if (localPlayer == player)
        {
            localPlayer = null;
            RefreshLocalPlayer();
        }

        MultiplayerEvents.RaisePlayerListChanged();
    }

    /// <summary>联机结束时清空所有缓存（回到单机模式）。</summary>
    public static void Reset()
    {
        allPlayers.Clear();
        localPlayer = null;
        MultiplayerEvents.RaisePlayerListChanged();
    }

    private static void RefreshLocalPlayer()
    {
        localPlayer = null;
        for (int i = 0; i < allPlayers.Count; i++)
        {
            PlayerNetwork p = allPlayers[i];
            if (p != null && p.IsSpawned && p.IsOwner)
            {
                localPlayer = p;
                break;
            }
        }
    }

    private static int CompareByOwner(PlayerNetwork a, PlayerNetwork b)
    {
        if (a == null) return 1;
        if (b == null) return -1;
        return a.OwnerClientId.CompareTo(b.OwnerClientId);
    }

    // ------------------------------------------------------------------
    // 便捷查询
    // ------------------------------------------------------------------

    /// <summary>是否为本地玩家所控制的对象。联机未启动时恒为 true（单机里所有角色都归本机管）。</summary>
    public static bool IsLocalAuthority(NetworkBehaviour behaviour)
    {
        if (!IsOnline) return true;
        if (behaviour == null) return true;

        if (!behaviour.IsSpawned) return true;

        return behaviour.IsOwner;
    }

    /// <summary>
    /// 等价于 IsLocalAuthority，但对象是普通 MonoBehaviour。
    /// 用于没有 NetworkObject 的场景内对象（如 Enemy / 道具）。
    /// </summary>
    public static bool IsSimulationAllowed(NetworkObject no)
    {
        if (!IsOnline) return true;       // 单机：全部允许
        if (no == null) return true;
        if (!no.IsSpawned) return true;

        // 敌人和世界物体一律由服务器模拟
        if (!no.IsPlayerObject) return IsServer;

        // 玩家：只有拥有者模拟自己的角色
        return no.IsOwner;
    }

    /// <summary>
    /// 任意组件所属对象本机是否应该模拟。
    /// 供 <c>Entity.ShouldSimulate</c> 使用，这样业务脚本不需要 using Unity.Netcode。
    /// </summary>
    public static bool CanSimulate(Component component)
    {
        if (!IsOnline) return true;
        if (component == null) return true;

        NetworkObject no = component.GetComponent<NetworkObject>();
        if (no == null) return true;      // 不是联网对象（特效、触发器等）

        return IsSimulationAllowed(no);
    }

    /// <summary>查找距离某点最近的存活玩家（服务器端 AI 用）。</summary>
    public static Transform FindClosestPlayer(Vector2 position, System.Predicate<PlayerNetwork> filter = null)
    {
        Transform best = null;
        float bestDistance = float.PositiveInfinity;

        for (int i = 0; i < allPlayers.Count; i++)
        {
            PlayerNetwork p = allPlayers[i];
            if (p == null || p.Player == null) continue;
            if (filter != null && !filter(p)) continue;

            CharacterStats stats = p.Player.stats;
            if (stats != null && stats.isDead) continue;

            float distance = Vector2.Distance(position, p.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = p.transform;
            }
        }

        return best;
    }

    /// <summary>
    /// 单机模式下的“玩家变换”：联机时返回本地玩家，单机时返回场景里的 Player。
    /// 供敌人 AI / 音频距离衰减等需要“玩家位置”的地方使用。
    /// </summary>
    public static Transform GetReferencePlayerTransform()
    {
        if (IsOnline)
        {
            PlayerNetwork local = LocalPlayer;
            if (local != null && local.Player != null) return local.Player.transform;
        }

        if (PlayerManager.instance != null && PlayerManager.instance.player != null)
        {
            return PlayerManager.instance.player.transform;
        }

        return null;
    }
}

/// <summary>
/// 极简的联机事件中转，避免业务脚本直接依赖 Netcode 的回调签名。
/// </summary>
public static class MultiplayerEvents
{
    public static event System.Action PlayerListChanged;
    public static event System.Action<PlayerNetwork> LocalPlayerSpawned;
    public static event System.Action LocalPlayerDespawned;

    public static void RaisePlayerListChanged()
    {
        PlayerListChanged?.Invoke();
    }

    public static void RaiseLocalPlayerSpawned(PlayerNetwork player)
    {
        LocalPlayerSpawned?.Invoke(player);
    }

    public static void RaiseLocalPlayerDespawned()
    {
        LocalPlayerDespawned?.Invoke();
    }
}
