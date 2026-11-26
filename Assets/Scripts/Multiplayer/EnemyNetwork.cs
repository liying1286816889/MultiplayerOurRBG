using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 敌人的网络身份。
///
/// 权威模型：敌人完全由服务器模拟（AI、寻路、攻击判定都在服务器跑），
/// 客户端只接收位置 / 动画 / 血量，这样两个玩家看到的是同一批怪、同一份血量。
///
/// 动画同步采用“状态名复制”：服务器把当前状态的 Animator bool 名字推给客户端，
/// 客户端照着开关同名 bool。之所以不用 NetworkAnimator，是因为它默认服务器权威，
/// 而且需要额外配置；这里自己控制更稳、也更好排查。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Enemy))]
public class EnemyNetwork : NetworkBehaviour
{
    [Header("死亡后延迟移除")]
    [SerializeField] private float despawnDelay = 5f;

    public Enemy Enemy { get; private set; }
    public EnemyStats Stats { get; private set; }

    private readonly NetworkVariable<FixedString64Bytes> animState =
        new NetworkVariable<FixedString64Bytes>(new FixedString64Bytes("Idle"),
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> animSpeed =
        new NetworkVariable<float>(1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> yVelocity =
        new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkCharacterStats netStats;
    private Animator anim;

    private bool deathHandled;
    private bool despawnScheduled;
    private string appliedStateName = string.Empty;

    private void Awake()
    {
        Enemy = GetComponent<Enemy>();
        Stats = GetComponent<EnemyStats>();
        netStats = GetComponent<NetworkCharacterStats>();
        anim = GetComponentInChildren<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        deathHandled = false;
        despawnScheduled = false;

        if (netStats != null)
        {
            netStats.HealthVariable.OnValueChanged += OnHealthChanged;
        }

        animState.OnValueChanged += OnAnimStateChanged;

        if (!IsServer)
        {
            SetUpRemoteCopy();
            ApplyAnimState(animState.Value.ToString());
        }
    }

    public override void OnNetworkDespawn()
    {
        if (netStats != null)
        {
            netStats.HealthVariable.OnValueChanged -= OnHealthChanged;
        }

        animState.OnValueChanged -= OnAnimStateChanged;

        base.OnNetworkDespawn();
    }

    /// <summary>客户端上的敌人：关掉本地 AI 与物理，只跟随网络同步。</summary>
    private void SetUpRemoteCopy()
    {
        // Collider2D 必须保留：本地玩家攻击时要用 Physics2D 检测到它。
        // 只关掉刚体模拟，位置由 NetworkTransform 驱动。
        if (Enemy.rb != null)
        {
            Enemy.rb.velocity = Vector2.zero;

            // 同样不能 simulated = false：那样客户端的攻击检测（OverlapCircleAll）
            // 就找不到这个敌人，玩家打了没反应。
            // Kinematic 保证它仍参与物理查询，位置交给 NetworkTransform。
            Enemy.rb.bodyType = RigidbodyType2D.Kinematic;
            Enemy.rb.simulated = true;
        }
    }

    /// <summary>服务器每帧把动画状态推给客户端。</summary>
    private void Update()
    {
        if (!IsSpawned || !IsServer) return;
        if (anim == null || Enemy.stateMachine == null) return;

        EnemyState current = Enemy.stateMachine.currentState;
        if (current != null)
        {
            string name = current.AnimBoolName;

            if (!string.IsNullOrEmpty(name) && name != animState.Value.ToString())
            {
                animState.Value = new FixedString64Bytes(name);
            }
        }

        if (!Mathf.Approximately(animSpeed.Value, anim.speed))
        {
            animSpeed.Value = anim.speed;
        }

        float vy = Enemy.rb != null ? Enemy.rb.velocity.y : 0f;
        if (Mathf.Abs(yVelocity.Value - vy) > 0.01f)
        {
            yVelocity.Value = vy;
        }
    }

    private void OnAnimStateChanged(FixedString64Bytes previous, FixedString64Bytes current)
    {
        if (IsServer) return;
        ApplyAnimState(current.ToString());
    }

    private void ApplyAnimState(string stateName)
    {
        if (anim == null || string.IsNullOrEmpty(stateName)) return;
        if (appliedStateName == stateName) return;

        if (!string.IsNullOrEmpty(appliedStateName))
        {
            anim.SetBool(appliedStateName, false);
        }

        anim.SetBool(stateName, true);
        anim.speed = animSpeed.Value;
        anim.SetFloat("yVelocity", yVelocity.Value);

        appliedStateName = stateName;
    }

    private void LateUpdate()
    {
        if (!IsSpawned || IsServer) return;
        if (anim == null) return;

        // 兜底：speed / 竖直速度只需要跟随
        anim.speed = animSpeed.Value;
        anim.SetFloat("yVelocity", yVelocity.Value);
    }

    private void OnHealthChanged(int previous, int current)
    {
        if (current > 0) return;
        if (deathHandled) return;

        deathHandled = true;

        if (!IsServer) return;

        AwardKillSouls(Stats);
        ScheduleDespawn();
    }

    /// <summary>服务器：把灵魂发给最后一击的玩家。</summary>
    public static void AwardKillSouls(EnemyStats stats)
    {
        if (stats == null) return;

        int souls = stats.soulsDropAmount != null ? stats.soulsDropAmount.GetValue() : 0;
        if (souls <= 0) return;

        ulong killerClientId = ResolveKillerClientId(stats);
        PlayerNetwork.ServerAwardKillReward(killerClientId, souls);
    }

    private static ulong ResolveKillerClientId(EnemyStats stats)
    {
        NetworkCharacterStats targetNet = stats.GetComponent<NetworkCharacterStats>();
        if (targetNet == null) return 0;

        ulong attackerObjectId = targetNet.LastAttackerNetworkObjectId;
        if (attackerObjectId == 0) return 0;

        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null) return 0;

        if (!nm.SpawnManager.SpawnedObjects.TryGetValue(attackerObjectId, out NetworkObject attacker))
        {
            return 0;
        }

        if (!attacker.IsPlayerObject) return 0;

        return attacker.OwnerClientId;
    }

    private void ScheduleDespawn()
    {
        if (despawnScheduled) return;
        despawnScheduled = true;

        StartCoroutine(DespawnAfterDelay());
    }

    private IEnumerator DespawnAfterDelay()
    {
        yield return new WaitForSeconds(despawnDelay);

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
