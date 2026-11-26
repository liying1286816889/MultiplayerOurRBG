using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 战斗结算的统一出口。
///
/// 权威模型（2 人局域网合作，信任客户端）：
/// - 攻击方在自己机器上用原有公式算出伤害（保留全部暴击 / 闪避 / 元素玩法手感）。
/// - 真正的扣血一律交给服务器执行，服务器再把血量同步给所有客户端。
/// - 单机模式下这里直接调用原生 TakeDamage，行为和改造前完全一致。
///
/// 这样改造的好处：<see cref="CharacterStats"/> 里那一长串伤害公式完全不用动。
/// </summary>
public static class NetCombat
{
    /// <summary>对目标造成物理/通用伤害。</summary>
    public static void ApplyDamage(CharacterStats attacker, CharacterStats target, int damage)
    {
        if (target == null) return;
        if (target.isDead) return;

        // 单机模式：原样执行
        if (!NetGame.IsOnline)
        {
            target.TakeDamage(damage);
            return;
        }

        // 服务器：直接结算
        if (NetGame.IsServer)
        {
            target.TakeDamage(damage);
            return;
        }

        // 纯客户端：提交给服务器
        NetworkCharacterStats targetNet = target.GetComponent<NetworkCharacterStats>();
        if (targetNet == null || !targetNet.IsSpawned)
        {
            // 目标不是联网对象（例如本地生成的特效体），退化成本地结算
            target.TakeDamage(damage);
            return;
        }

        ulong attackerId = GetNetworkObjectId(attacker);
        targetNet.ApplyDamageServerRpc(damage, attackerId);
    }

    /// <summary>施加元素状态（点燃 / 冰冻 / 感电）。</summary>
    public static void ApplyAilments(CharacterStats attacker, CharacterStats target,
        bool ignite, bool chill, bool shock, int igniteDamage, int shockDamage)
    {
        if (target == null) return;
        if (target.isDead) return;

        if (!NetGame.IsOnline || NetGame.IsServer)
        {
            target.SetupIgniteDamage(igniteDamage);
            target.SetupShockStrikeDamage(shockDamage);
            target.ApplyAilments(ignite, chill, shock);
            return;
        }

        NetworkCharacterStats targetNet = target.GetComponent<NetworkCharacterStats>();
        if (targetNet == null || !targetNet.IsSpawned)
        {
            target.SetupIgniteDamage(igniteDamage);
            target.SetupShockStrikeDamage(shockDamage);
            target.ApplyAilments(ignite, chill, shock);
            return;
        }

        targetNet.ApplyAilmentsServerRpc(ignite, chill, shock, igniteDamage, shockDamage);
    }

    /// <summary>该 <see cref="CharacterStats"/> 是否应该在本机被模拟（用于避免双端重复结算）。</summary>
    public static bool ShouldSimulate(CharacterStats stats)
    {
        if (stats == null) return false;
        if (!NetGame.IsOnline) return true;

        NetworkObject no = stats.GetComponent<NetworkObject>();
        return NetGame.IsSimulationAllowed(no);
    }

    /// <summary>取一个组件所在对象的 NetworkObjectId，取不到返回 0。</summary>
    public static ulong GetNetworkObjectId(Component component)
    {
        if (component == null) return 0;

        NetworkObject no = component.GetComponent<NetworkObject>();
        if (no == null) no = component.GetComponentInParent<NetworkObject>();
        if (no == null || !no.IsSpawned) return 0;

        return no.NetworkObjectId;
    }
}
