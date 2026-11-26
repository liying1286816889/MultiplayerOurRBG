using System;
using System.Collections.Generic;
using Cinemachine;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 玩家对象在网络中的身份与生命周期。
///
/// 职责划分：
/// 1. 登记到 <see cref="NetGame.Players"/>，并把“本地玩家”交给原有的单例系统
///    （PlayerManager / SkillManager / Inventory / UI 都因此自动指向本地角色）。
/// 2. 所有权判定：只有 owner 客户端才跑输入 + 状态机 + 物理，
///    其它客户端上的副本只负责“跟随 + 播动画”。
/// 3. 把动画状态、朝向、货币等需要让别人看到的数据同步出去。
/// 4. 处理死亡 / 复活 / 出生点。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Player))]
public class PlayerNetwork : NetworkBehaviour
{
    [Header("同步参数")]

    [Tooltip("出生点的 X 轴间距，用于没有显式出生点时的兜底")]
    [SerializeField] private float fallbackSpawnSpacing = 3f;

    public Player Player { get; private set; }
    public PlayerStats Stats { get; private set; }

    /// <summary>本机是否在控制这个角色。</summary>
    public bool IsLocallyControlled => !IsSpawned || IsOwner;

    /// <summary>是否只是别人的“镜像”。</summary>
    public bool IsRemoteCopy => IsSpawned && !IsOwner;

    /// <summary>显示用的玩家名字（后续可以做改名）。</summary>
    public string DisplayName => $"玩家 {OwnerClientId + 1}";

    // ------------------------------------------------------------------
    // 网络变量
    // ------------------------------------------------------------------

    private readonly NetworkVariable<FixedString64Bytes> animState =
        new NetworkVariable<FixedString64Bytes>(new FixedString64Bytes("Idle"),
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private readonly NetworkVariable<float> netYVelocity =
        new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private readonly NetworkVariable<int> netComboCounter =
        new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private readonly NetworkVariable<float> netAnimSpeed =
        new NetworkVariable<float>(1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private readonly NetworkVariable<bool> netCounterSuccess =
        new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private readonly NetworkVariable<bool> netFacingRight =
        new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // 需要的动画参数名（和 Player.cs 里的状态名保持一致）
    private static readonly string[] AnimBoolNames =
    {
        "Idle", "Move", "Jump", "Dash", "WallSlide", "Attack",
        "CounterAttack", "AimSword", "CatchSword", "Die"
    };

    private static readonly HashSet<string> AnimBoolSet = new HashSet<string>(AnimBoolNames);

    // ------------------------------------------------------------------
    // 本地缓存
    // ------------------------------------------------------------------

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer[] spriteRenderers;
    private PlayerFX playerFx;
    private CinemachineVirtualCamera followCamera;

    private string appliedAnimState = string.Empty;

    #region 生命周期

    private void Awake()
    {
        Player = GetComponent<Player>();
        Stats = GetComponent<PlayerStats>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        playerFx = GetComponent<PlayerFX>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        NetGame.RegisterPlayer(this);

        if (IsServer)
        {
            Vector3 spawn = MultiplayerSpawnPoints.GetPosition(OwnerClientId, fallbackSpawnSpacing);
            transform.position = spawn;
        }

        if (IsOwner)
        {
            SetUpLocalPlayer();
        }
        else
        {
            SetUpRemoteCopy();
        }

        animState.OnValueChanged += OnAnimStateChanged;
        ApplyRemoteAnimState(animState.Value.ToString());
    }

    public override void OnNetworkDespawn()
    {
        animState.OnValueChanged -= OnAnimStateChanged;

        if (IsOwner)
        {
            MultiplayerEvents.RaiseLocalPlayerDespawned();
        }

        NetGame.UnregisterPlayer(this);

        base.OnNetworkDespawn();
    }

    /// <summary>本机控制的玩家：接上所有单例系统与相机。</summary>
    private void SetUpLocalPlayer()
    {
        // 让全部历史遗留的单例代码都指向“我”
        if (PlayerManager.instance != null)
        {
            PlayerManager.instance.player = Player;
        }

        Player.SetLocalControlled(true);

        if (rb != null)
        {
            rb.simulated = true;
        }

        BindCamera();

        MultiplayerEvents.RaiseLocalPlayerSpawned(this);
    }

    /// <summary>别人控制的玩家：关闭输入与物理，只跟随网络位置。</summary>
    private void SetUpRemoteCopy()
    {
        Player.SetLocalControlled(false);

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.simulated = false;
        }


        // 远程副本不需要枪口准星之类的本地表现对象
        Player.SetUpAsRemoteCopy();
    }

    /// <summary>把本机 Cinemachine 相机绑到本地玩家身上。</summary>
    private void BindCamera()
    {
        if (followCamera == null)
        {
            followCamera = FindVirtualCamera();
        }

        if (followCamera != null)
        {
            followCamera.Follow = transform;
        }
    }

    private CinemachineVirtualCamera FindVirtualCamera()
    {
        CinemachineVirtualCamera[] cameras = FindObjectsOfType<CinemachineVirtualCamera>(true);
        if (cameras == null || cameras.Length == 0) return null;

        // 优先找当前激活的
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null && cameras[i].isActiveAndEnabled) return cameras[i];
        }

        return cameras[0];
    }

    #endregion

    #region 远程副本表现

    private void Update()
    {
        if (IsRemoteCopy)
        {
            UpdateRemoteVisuals();
        }
    }

    private void UpdateRemoteVisuals()
    {
        // 位置与朝向都由 NetworkTransform 同步，这里不需要额外处理
    }

    private void OnAnimStateChanged(FixedString64Bytes previous, FixedString64Bytes current)
    {
        if (IsOwner) return;
        ApplyRemoteAnimState(current.ToString());
    }

    private void ApplyRemoteAnimState(string stateName)
    {
        if (anim == null) return;
        if (string.IsNullOrEmpty(stateName)) return;

        if (appliedAnimState == stateName) return;

        // 关掉旧的
        if (!string.IsNullOrEmpty(appliedAnimState) && AnimBoolSet.Contains(appliedAnimState))
        {
            anim.SetBool(appliedAnimState, false);
        }

        if (AnimBoolSet.Contains(stateName))
        {
            anim.SetBool(stateName, true);
        }

        appliedAnimState = stateName;
    }

    #endregion

    #region 由本地玩家每帧推送的表现数据

    /// <summary>状态机切状态时调用。</summary>
    public void PushAnimState(string stateName)
    {
        if (!IsSpawned || !IsOwner) return;
        if (string.IsNullOrEmpty(stateName)) return;

        FixedString64Bytes value = new FixedString64Bytes(stateName);
        if (animState.Value.Equals(value)) return;

        animState.Value = value;
    }

    public void PushAnimatorSnapshot(float yVelocity, int combo, float animSpeedValue, bool counterSuccess, bool facingRight)
    {
        if (!IsSpawned || !IsOwner) return;

        netYVelocity.Value = yVelocity;
        netComboCounter.Value = combo;
        netAnimSpeed.Value = animSpeedValue;
        netCounterSuccess.Value = counterSuccess;
        netFacingRight.Value = facingRight;
    }

    #endregion

    #region 货币同步

    /// <summary>
    /// 服务器给指定玩家发灵魂。客户端收到后加到自己的 PlayerManager.currency 上，
    /// 这样每台机器的钱包是各自独立的，互不影响。
    /// </summary>
    [ClientRpc]
    public void AwardCurrencyClientRpc(int amount)
    {
        if (!IsOwner) return;
        if (PlayerManager.instance == null) return;

        // UI_InGame.UpdataSoulsUI() 每帧轮询 currency，所以这里不需要额外刷新
        PlayerManager.instance.currency += amount;
    }

    /// <summary>服务器发送给“击杀者”的便捷封装。</summary>
    public static void ServerAwardKillReward(ulong killerClientId, int amount)
    {
        if (!NetGame.IsServer) return;
        if (amount <= 0) return;

        IReadOnlyList<PlayerNetwork> players = NetGame.Players;
        for (int i = 0; i < players.Count; i++)
        {
            PlayerNetwork p = players[i];
            if (p == null || !p.IsSpawned) continue;
            if (p.OwnerClientId != killerClientId) continue;

            p.AwardCurrencyClientRpc(amount);
            return;
        }
    }

    #endregion

    #region 技能特效广播

    public void BroadcastSkillFx(SkillFxKind kind, Vector3 position, Vector3 dir, float p1, float p2, float p3)
    {
        if (!IsSpawned || !IsOwner) return;
        SkillFxClientRpc((int)kind, position, dir, p1, p2, p3);
    }

    [ClientRpc]
    private void SkillFxClientRpc(int kind, Vector3 position, Vector3 dir, float p1, float p2, float p3)
    {
        if (IsOwner) return;
        NetSkillFx.PlayVisual((SkillFxKind)kind, Player, position, dir, p1, p2, p3);
    }

    #endregion

    #region 死亡 / 复活
    [Header("死亡掉落")]
    [Tooltip("死亡后掉在地上的灵魂，由一键配置脚本自动指向 Assets/Prefabs/LostCurrency.prefab")]
    [SerializeField] private GameObject lostCurrencyPrefab;

    private bool localDeathHandled;
    private bool awaitingRespawn;

    /// <summary>是否正在等待复活（本地玩家）。</summary>
    public bool AwaitingRespawn => awaitingRespawn;

    /// <summary>本地玩家死亡：清空自己的钱包并把灵魂丢到地上。</summary>
    public void HandleLocalDeath()
    {
        if (localDeathHandled) return;

        localDeathHandled = true;
        awaitingRespawn = true;

        int lost = PlayerManager.instance != null ? PlayerManager.instance.currency : 0;

        if (PlayerManager.instance != null)
        {
            PlayerManager.instance.currency = 0;
        }

        if (lost > 0 && IsSpawned)
        {
            SpawnLostCurrencyServerRpc(lost, transform.position);
        }

        // 死亡界面（UI.ReStartButton 在联机时会变成“原地复活”）
        UI ui = FindUI();
        if (ui != null)
        {
            ui.SwitchOnEndScreen();
        }
    }

    /// <summary>服务器在所有客户端生成一坨“掉落的灵魂”。</summary>
    [ServerRpc]
    private void SpawnLostCurrencyServerRpc(int amount, Vector3 position)
    {
        if (!IsServer) return;

        GameObject prefab = lostCurrencyPrefab;

        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>("Multiplayer/NetworkPrefabs/LostCurrency_Multiplayer");
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>("Multiplayer/LostCurrency_Multiplayer");
            }
        }

        if (prefab == null)
        {
            // 没有可用的网络预制体就退化为“直接把灵魂还给玩家”，至少不丢进度
            AwardCurrencyClientRpc(amount);
            return;
        }

        GameObject instance = Instantiate(prefab, position, Quaternion.identity);

        LostCurrentcyController controller = instance.GetComponent<LostCurrentcyController>();
        if (controller != null)
        {
            controller.currentcy = amount;
        }

        NetworkObject no = instance.GetComponent<NetworkObject>();
        if (no != null)
        {
            no.Spawn(true);
        }
    }

    /// <summary>本地玩家点击“复活”。</summary>
    public void RequestRespawn()
    {
        if (!IsSpawned) return;

        if (IsServer)
        {
            RespawnLocally();
        }
        else
        {
            RequestRespawnServerRpc();
        }
    }

    /// <summary>请求服务器把我复活到最近的存档点。</summary>
    [ServerRpc]
    public void RequestRespawnServerRpc()
    {
        if (!IsServer) return;

        Vector3 position = MultiplayerSpawnPoints.GetRespawnPosition(OwnerClientId, fallbackSpawnSpacing);

        // 服务器权威副本也要恢复血量，否则同步过去仍然是 0
        if (Stats != null)
        {
            Stats.ReviveFromNetwork();
        }

        RespawnClientRpc(position);
    }

    [ClientRpc]
    private void RespawnClientRpc(Vector3 position)
    {
        ApplyRespawn(position);
    }

    private void RespawnLocally()
    {
        Vector3 position = MultiplayerSpawnPoints.GetRespawnPosition(OwnerClientId, fallbackSpawnSpacing);
        ApplyRespawn(position);
    }

    private void ApplyRespawn(Vector3 position)
    {
        if (!IsOwner) return;

        transform.position = position;
        if (rb != null) rb.velocity = Vector2.zero;

        if (Stats != null)
        {
            Stats.ReviveFromNetwork();
            Stats.ResetLocalDeathFlag();
        }

        localDeathHandled = false;
        awaitingRespawn = false;

        UI ui = FindUI();
        if (ui != null)
        {
            ui.HideEndScreen();
        }

        // 从死亡状态回到待机
        if (Player != null && Player.stateMachine != null)
        {
            Player.stateMachine.ChangeState(Player.idleState);
        }
    }

    private static UI FindUI()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null) return null;
        return canvas.GetComponent<UI>();
    }

    #endregion
}

