using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 把 <see cref="CharacterStats"/> 的生命值 / 死亡状态 / 元素状态复制到所有客户端。
///
/// 权威模型：
/// - 服务器是唯一的数值权威（只有服务器能写 NetworkVariable）。
/// - 客户端本地只负责“表现”：掉血飘字、受击闪白、元素特效。
/// - 攻击方的伤害计算依然是本地算（合作游戏信任客户端），
///   算完通过 <see cref="NetCombat"/> 提交给服务器，由服务器真正扣血。
///
/// 这个组件是可选挂载的：单机模式下（对象没有 NetworkObject / 没有启动联机）
/// 它不会影响任何原有逻辑。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterStats))]
public class NetworkCharacterStats : NetworkBehaviour
{
    private readonly NetworkVariable<int> health =
        new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> isDead =
        new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> ignited =
        new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> chilled =
        new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> shocked =
        new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> HealthVariable => health;
    public NetworkVariable<bool> DeadVariable => isDead;

    private CharacterStats stats;

    /// <summary>服务器记录：最后一个对“我”造成伤害的对象的 NetworkObjectId（用于算击杀归属）。</summary>
    public ulong LastAttackerNetworkObjectId { get; private set; }

    /// <summary>挂在玩家身上时指向同一个对象上的 <see cref="PlayerNetwork"/>。</summary>
    public PlayerNetwork OwnerPlayer
    {
        get
        {
            if (cachedOwner == null) cachedOwner = GetComponent<PlayerNetwork>();
            return cachedOwner;
        }
    }

    private PlayerNetwork cachedOwner;

    public CharacterStats Stats
    {
        get
        {
            if (stats == null) stats = GetComponent<CharacterStats>();
            return stats;
        }
    }

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        health.OnValueChanged += OnHealthValueChanged;
        isDead.OnValueChanged += OnDeadValueChanged;
        ignited.OnValueChanged += OnIgnitedChanged;
        chilled.OnValueChanged += OnChilledChanged;
        shocked.OnValueChanged += OnShockedChanged;

        if (IsServer)
        {
            // 服务器把自己的真实数值推给所有客户端
            health.Value = Stats.currentHealth;
            isDead.Value = Stats.isDead;
            ignited.Value = Stats.isIgnited;
            chilled.Value = Stats.isChilled;
            shocked.Value = Stats.isShocked;
        }
        else
        {
            // 客户端：用服务器的值覆盖本地初始值
            ApplyHealth(health.Value, 0, false);

            if (isDead.Value)
            {
                Stats.ApplyRemoteDeath();
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        health.OnValueChanged -= OnHealthValueChanged;
        isDead.OnValueChanged -= OnDeadValueChanged;
        ignited.OnValueChanged -= OnIgnitedChanged;
        chilled.OnValueChanged -= OnChilledChanged;
        shocked.OnValueChanged -= OnShockedChanged;

        base.OnNetworkDespawn();
    }

    #region 服务器 -> 客户端

    /// <summary>服务器在血量变化后调用，把最新血量推给客户端。</summary>
    public void PushHealth(int value)
    {
        if (!IsSpawned || !IsServer) return;
        if (health.Value == value) return;
        health.Value = value;
    }

    /// <summary>服务器在死亡后调用。</summary>
    public void PushDead(bool value)
    {
        if (!IsSpawned || !IsServer) return;
        if (isDead.Value == value) return;
        isDead.Value = value;
    }

    /// <summary>服务器同步元素状态。</summary>
    public void PushAilments(bool ignite, bool chill, bool shock)
    {
        if (!IsSpawned || !IsServer) return;

        if (ignited.Value != ignite) ignited.Value = ignite;
        if (chilled.Value != chill) chilled.Value = chill;
        if (shocked.Value != shock) shocked.Value = shock;
    }

    private void OnHealthValueChanged(int previous, int current)
    {
        // 只有客户端需要从网络应用数值（服务器是权威，本身就是最新值）
        if (IsServer) return;

        int damageTaken = previous - current;
        ApplyHealth(current, damageTaken, true);
    }

    private void ApplyHealth(int value, int damageTaken, bool playVisuals)
    {
        if (Stats == null) return;

        Stats.SetHealthFromNetwork(value, damageTaken, playVisuals);
    }

    private void OnDeadValueChanged(bool previous, bool current)
    {
        if (!current) return;
        if (IsServer) return;

        Stats.ApplyRemoteDeath();
    }

    private void OnIgnitedChanged(bool previous, bool current)
    {
        if (IsServer) return;
        Stats.ApplyAilmentVisual(StatAilment.Ignite, current);
    }

    private void OnChilledChanged(bool previous, bool current)
    {
        if (IsServer) return;
        Stats.ApplyAilmentVisual(StatAilment.Chill, current);
    }

    private void OnShockedChanged(bool previous, bool current)
    {
        if (IsServer) return;
        Stats.ApplyAilmentVisual(StatAilment.Shock, current);
    }

    #endregion

    #region 客户端 -> 服务器

    /// <summary>客户端请求服务器扣血（由 <see cref="NetCombat"/> 调用）。</summary>
    [ServerRpc(RequireOwnership = false)]
    public void ApplyDamageServerRpc(int damage, ulong attackerNetworkObjectId, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        if (damage <= 0) return;
        if (Stats == null || Stats.isDead) return;

        LastAttackerNetworkObjectId = attackerNetworkObjectId;

        Transform attacker = null;
        if (attackerNetworkObjectId != 0 &&
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                attackerNetworkObjectId, out NetworkObject attackerObject))
        {
            attacker = attackerObject.transform;
        }

        if (attacker != null)
        {
            Entity entity = GetComponent<Entity>();
            if (entity != null) entity.SetupKnockbackDir(attacker);
        }

        Stats.TakeDamage(damage);
    }

    /// <summary>客户端请求服务器施加元素状态。</summary>
    [ServerRpc(RequireOwnership = false)]
    public void ApplyAilmentsServerRpc(bool ignite, bool chill, bool shock,
        int igniteDamage, int shockDamage, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        if (Stats == null || Stats.isDead) return;

        Stats.SetupIgniteDamage(igniteDamage);
        Stats.SetupShockStrikeDamage(shockDamage);
        Stats.ApplyAilments(ignite, chill, shock);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetFrozenServerRpc(bool frozen, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        Enemy enemy = GetComponent<Enemy>();
        if (enemy != null) enemy.FreezeTime(frozen);
    }
    #endregion
}

/// <summary>元素状态枚举，避免在业务脚本里散落魔法字符串。</summary>
public enum StatAilment
{
    Ignite,
    Chill,
    Shock
}
