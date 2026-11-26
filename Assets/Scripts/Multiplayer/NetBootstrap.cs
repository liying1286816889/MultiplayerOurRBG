using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 联机会话的生命周期管理：创建 Host / 连接 Client / 断开 / 场景切换。
///
/// 说明：
/// - 这个组件会自动创建（或接管）NetworkManager，并把它设为 DontDestroyOnLoad，
///   因此可以放心地放在「Main Menu」场景里。
/// - 如果场景里已经有 NetworkManager，它会直接拿来用，不会重复创建。
/// - 单机模式（从未调用 StartHost / StartClient）下本组件完全不参与游戏逻辑。
/// </summary>
[DisallowMultipleComponent]
public class NetBootstrap : MonoBehaviour
{
    public static NetBootstrap Instance { get; private set; }

    public enum SessionState
    {
        Offline,
        Starting,
        Hosting,
        Connecting,
        Connected,
        Failed
    }

    [Header("连接设置")]
    [Tooltip("默认端口，Host 和 Client 必须一致")]
    [SerializeField] private ushort port = 7777;
    [Tooltip("默认连接地址，局域网里填房主的 IPv4 地址")]
    [SerializeField] private string defaultAddress = "127.0.0.1";
    [Tooltip("最多允许的玩家数量（本项目按 2 人合作设计）")]
    [SerializeField] private int maxPlayers = 2;

    [Header("场景")]
    [Tooltip("进入联机后由服务器加载的游戏场景名")]
    [SerializeField] private string gameplaySceneName = "MainScene";

    [Header("玩家预制体")]
    [Tooltip("留空时会自动从 Resources/Multiplayer/Player_Multiplayer 加载")]
    [SerializeField] private GameObject playerPrefab;

    [Header("额外网络预制体")]
    [SerializeField] private List<GameObject> extraNetworkPrefabs = new List<GameObject>();

    [Header("自测：命令行自动开服 / 进服")]
    [Tooltip("打包后可以用命令行参数直接开局，方便一台机器双开测试\n" +
             "  MyRBG-test.exe -host            当作房主\n" +
             "  MyRBG-test.exe -join 127.0.0.1  当作客户端\n" +
             "  MyRBG-test.exe -host -port 8000 换端口\n" +
             "在编辑器里运行时这两个参数会被忽略（编辑器只有一个实例）。")]
    [SerializeField] private bool autoStartFromCommandLine = true;

    [Tooltip("客户端连不上时自动重试的次数（这样先开房主还是先开客户端都无所谓）")]
    [SerializeField] private int clientRetryCount = 12;

    [Tooltip("每次重试的间隔（秒）")]
    [SerializeField] private float clientRetryInterval = 2.5f;

    public SessionState State { get; private set; } = SessionState.Offline;
    public string LastError { get; private set; } = string.Empty;
    public ushort Port => port;
    public string Address { get; private set; }
    public NetworkManager Manager => NetworkManager.Singleton;

    /// <summary>状态变化回调（UI 用）。</summary>
    public event Action<SessionState, string> StateChanged;


    #region 生命周期

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureNetworkManager();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            NetGame.Reset();
        }
    }

    private void Start()
    {
        if (autoStartFromCommandLine)
        {
            TryAutoStartFromCommandLine();
        }
    }

    #region 命令行自动开局（自测用）

    /// <summary>
    /// 解析命令行参数，支持：
    ///   -host              直接当房主
    ///   -join &lt;ip&gt;         直接当客户端连过去
    ///   -port &lt;n&gt;          改端口
    /// 只在打包出来的程序里生效；编辑器里没有意义（一个工程只能开一个编辑器）。
    /// </summary>
    private void TryAutoStartFromCommandLine()
    {
        if (Application.isEditor) return;

        string[] args;
        try
        {
            args = System.Environment.GetCommandLineArgs();
        }
        catch (System.Exception)
        {
            return;
        }

        if (args == null || args.Length == 0) return;

        bool wantHost = false;
        bool wantServer = false;
        string joinAddress = null;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (string.IsNullOrEmpty(arg)) continue;

            switch (arg.ToLowerInvariant())
            {
                case "-host":
                case "--host":
                    wantHost = true;
                    break;

                case "-server":
                case "--server":
                    wantServer = true;
                    break;

                case "-join":
                case "--join":
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                    {
                        joinAddress = args[i + 1];
                    }
                    else
                    {
                        joinAddress = "127.0.0.1";
                    }
                    break;

                case "-port":
                case "--port":
                    if (i + 1 < args.Length)
                    {
                        ushort parsed;
                        if (ushort.TryParse(args[i + 1], out parsed) && parsed > 0)
                        {
                            port = parsed;
                        }
                    }
                    break;
            }
        }

        if (wantServer)
        {
            Debug.Log("[NetBootstrap] 命令行 -server：启动专用服务端");
            StartDedicatedServer();
            return;
        }

        if (wantHost)
        {
            Debug.Log("[NetBootstrap] 命令行 -host：自动创建房间");
            StartHost();
        }
        else if (!string.IsNullOrEmpty(joinAddress))
        {
            Debug.Log("[NetBootstrap] 命令行 -join " + joinAddress + "：自动加入房间");
            StartCoroutine(JoinWithRetry(joinAddress));
        }
        else
        {
            // 没有命令行参数时，尝试读 exe 旁边的 server.txt 自动进服
            TryAutoJoinFromConfigFile();
        }
    }

    /// <summary>
    /// 读 exe 同目录下的 server.txt（内容形如 1.2.3.4 或 1.2.3.4:7777），自动连上去。
    /// 这样把「exe + server.txt」打包发给朋友，他双击就直接进游戏，不用填任何东西。
    /// </summary>
    private void TryAutoJoinFromConfigFile()
    {
        string path = GetServerConfigPath();
        if (string.IsNullOrEmpty(path)) return;
        if (!System.IO.File.Exists(path)) return;

        try
        {
            string text = System.IO.File.ReadAllText(path).Trim();
            if (string.IsNullOrEmpty(text)) return;

            int colon = text.IndexOf(':');
            if (colon > 0)
            {
                ushort parsedPort;
                if (ushort.TryParse(text.Substring(colon + 1).Trim(), out parsedPort) && parsedPort > 0)
                {
                    port = parsedPort;
                }
                text = text.Substring(0, colon).Trim();
            }

            Debug.Log("[NetBootstrap] 从 server.txt 读到服务器地址：" + text + ":" + port);
            StartCoroutine(JoinWithRetry(text));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[NetBootstrap] 读取 server.txt 失败：" + e.Message);
        }
    }

    private static string GetServerConfigPath()
    {
        try
        {
            string exeDir = System.IO.Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(exeDir)) return null;
            return System.IO.Path.Combine(exeDir, "server.txt");
        }
        catch (System.Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// 客户端带重试的加入：先开客户端、后开房主也能连上。
    /// </summary>
    private IEnumerator JoinWithRetry(string address)
    {
        int attempts = Mathf.Max(1, clientRetryCount);

        for (int i = 0; i < attempts; i++)
        {
            if (State == SessionState.Connected || State == SessionState.Hosting) yield break;

            // StartClient 内部会把状态设成 Connecting，所以这里不能先设，
            // 否则 StartClient 会因为 "正在连接中" 直接返回 false。
            Debug.Log($"[NetBootstrap] 尝试连接 {address}:{port}（第 {i + 1}/{attempts} 次）");
            StartClient(address);

            // 给传输层一点时间
            float waited = 0f;
            float timeout = Mathf.Max(1f, clientRetryInterval);
            while (waited < timeout)
            {
                if (State == SessionState.Connected) yield break;
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            // 连不上就把 NetworkManager 关干净再重试
            NetworkManager nm = NetworkManager.Singleton;
            if (nm != null && nm.IsListening)
            {
                nm.Shutdown();
            }

            SessionRequested = false;
            yield return null;
        }

        Debug.LogError($"[NetBootstrap] 连不上 {address}:{port}，重试 {attempts} 次都失败了。" +
                       "请确认房主已经点了创建房间，并且防火墙放行了 UDP " + port);
        SetState(SessionState.Failed, $"连不上 {address}:{port}");
    }

    #endregion

    /// <summary>确保场景里存在一个可用的 NetworkManager。</summary>
    private NetworkManager EnsureNetworkManager()
    {
        NetworkManager nm = NetworkManager.Singleton;

        if (nm == null)
        {
            GameObject go = new GameObject("NetworkManager");
            nm = go.AddComponent<NetworkManager>();

            // NGO 的 NetworkManager 需要一个传输层组件才能工作
            UnityTransport transport = go.AddComponent<UnityTransport>();
            transport.SetConnectionData("127.0.0.1", port);

            nm.NetworkConfig = new NetworkConfig();
            nm.NetworkConfig.NetworkTransport = transport;
            nm.NetworkConfig.EnableSceneManagement = true;
            nm.NetworkConfig.ConnectionApproval = false;
            nm.NetworkConfig.ForceSamePrefabs = false;
            nm.NetworkConfig.RecycleNetworkIds = true;
        }

        // 一定要跨场景保留，否则载入关卡时 NetworkManager 会被一起销毁
        DontDestroyOnLoad(nm.gameObject);

        if (nm.NetworkConfig == null)
        {
            nm.NetworkConfig = new NetworkConfig();
        }

        EnsureTransport(nm);
        EnsurePlayerPrefab(nm);
        RegisterExtraPrefabs(nm);
        HookCallbacks(nm);

        return nm;
    }

    private void EnsureTransport(NetworkManager nm)
    {
        if (nm.NetworkConfig.NetworkTransport != null) return;

        UnityTransport transport = nm.GetComponent<UnityTransport>();
        if (transport == null)
        {
            transport = nm.gameObject.AddComponent<UnityTransport>();
        }

        transport.SetConnectionData("127.0.0.1", port);
        nm.NetworkConfig.NetworkTransport = transport;
    }

    private void EnsurePlayerPrefab(NetworkManager nm)
    {
        if (nm.NetworkConfig.PlayerPrefab == null)
        {
            GameObject loaded = playerPrefab != null
                ? playerPrefab
                : Resources.Load<GameObject>("Multiplayer/Player_Multiplayer");

            if (loaded != null)
            {
                nm.NetworkConfig.PlayerPrefab = loaded;
            }
            else
            {
                Debug.LogError(
                    "[NetBootstrap] 没有找到玩家预制体。请执行菜单 Tools/联机改造/一键配置联机场景，" +
                    "或手动把玩家预制体拖到 NetBootstrap.playerPrefab 上。");
            }
        }
    }

    private void RegisterExtraPrefabs(NetworkManager nm)
    {
        List<GameObject> candidates = new List<GameObject>(extraNetworkPrefabs);

        GameObject[] fromResources = Resources.LoadAll<GameObject>("Multiplayer/NetworkPrefabs");
        if (fromResources != null)
        {
            candidates.AddRange(fromResources);
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            GameObject prefab = candidates[i];
            if (prefab == null) continue;
            if (prefab.GetComponent<NetworkObject>() == null) continue;
            if (nm.NetworkConfig.Prefabs.Contains(prefab)) continue;

            nm.AddNetworkPrefab(prefab);
        }
    }

    private bool callbacksHooked;

    private void HookCallbacks(NetworkManager nm)
    {
        if (callbacksHooked) return;
        callbacksHooked = true;

        nm.OnServerStarted += HandleServerStarted;
        nm.OnClientConnectedCallback += HandleClientConnected;
        nm.OnClientDisconnectCallback += HandleClientDisconnect;
        nm.OnTransportFailure += HandleTransportFailure;
    }

    private void UnhookCallbacks(NetworkManager nm)
    {
        if (!callbacksHooked || nm == null) return;
        callbacksHooked = false;

        nm.OnServerStarted -= HandleServerStarted;
        nm.OnClientConnectedCallback -= HandleClientConnected;
        nm.OnClientDisconnectCallback -= HandleClientDisconnect;
        nm.OnTransportFailure -= HandleTransportFailure;
    }

    #endregion

    #region 回调

    private void HandleServerStarted()
    {
        SetState(SessionState.Hosting, "房间已创建");
        // 场景在 StartHost 之前就已经载入好了，这里不需要再切场景。
        // （如果在这里切，房主的角色会先被生成在菜单场景里，切场景时被销毁 —— 所以顺序很关键）
    }

    private void HandleClientConnected(ulong clientId)
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null) return;

        if (nm.IsServer)
        {
            // 人数上限保护
            int connected = nm.ConnectedClientsIds.Count;
            if (connected > maxPlayers && clientId != NetworkManager.ServerClientId)
            {
                Debug.LogWarning($"[NetBootstrap] 房间已满（上限 {maxPlayers}），踢出客户端 {clientId}");
                StartCoroutine(KickClientNextFrame(clientId));
            }
        }
        else if (clientId == nm.LocalClientId)
        {
            SetState(SessionState.Connected, "已连接到房主");
        }
    }

    private IEnumerator KickClientNextFrame(ulong clientId)
    {
        yield return null;
        NetworkManager nm = NetworkManager.Singleton;
        if (nm != null && nm.IsServer)
        {
            nm.DisconnectClient(clientId, "房间已满");
        }
    }

    private void HandleClientDisconnect(ulong clientId)
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null) return;

        if (!nm.IsServer && clientId == nm.LocalClientId)
        {
            SetState(SessionState.Failed, "与服务器的连接已断开");
            CleanupSession();
        }
        else if (nm.IsServer && clientId != NetworkManager.ServerClientId)
        {
            Debug.Log($"[NetBootstrap] 客户端 {clientId} 已离开");
        }
    }

    private void HandleTransportFailure()
    {
        SetState(SessionState.Failed, "网络传输层错误（端口可能被占用）");
    }

    #endregion

    #region 对外 API

    /// <summary>
    /// 是否已经请求过联机会话（从点了按钮开始，到断开为止）。
    /// 场景引导脚本靠它判断要不要关掉「单机版玩家」，
    /// 因为房主是先在本地载入关卡、之后才 StartHost 的。
    /// </summary>
    public static bool SessionRequested { get; private set; }

    /// <summary>创建房间并作为房主开始游戏。</summary>
    public bool StartHost(string address = null)
    {
        if (State == SessionState.Hosting || State == SessionState.Connected) return false;
        if (State == SessionState.Starting) return false;

        NetworkManager nm = EnsureNetworkManager();
        if (nm == null || nm.NetworkConfig.PlayerPrefab == null)
        {
            Fail("无法启动房主：NetworkManager 或玩家预制体缺失");
            return false;
        }

        // 房主根本不需要填地址：
        //  · 自己这个本地客户端永远连 127.0.0.1
        //  · 对外监听固定用 0.0.0.0（所有网卡），朋友用你的局域网 IP 连进来
        // 之前把地址框里的内容拿去绑定，填了「192.168.100.130/172.26.0.1」这种就必然失败。
        Address = "127.0.0.1";

        UnityTransport transport = nm.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
        }

        SessionRequested = true;
        SetState(SessionState.Starting, "正在创建房间...");

        // 关键顺序：必须先把游戏场景载入，再 StartHost。
        // 反过来的话 NGO 会把房主的角色生成在当前（菜单）场景里，切场景时被销毁。
        if (ShouldPreloadGameplayScene())
        {
            StartCoroutine(PreloadSceneThenStartHost());
        }
        else
        {
            DoStartHost();
        }

        return true;
    }

    private bool ShouldPreloadGameplayScene()
    {
        if (string.IsNullOrEmpty(gameplaySceneName)) return false;
        return SceneManager.GetActiveScene().name != gameplaySceneName;
    }

    private IEnumerator PreloadSceneThenStartHost()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(gameplaySceneName, LoadSceneMode.Single);
        if (op != null)
        {
            while (!op.isDone)
            {
                yield return null;
            }
        }

        // 再等一帧，确保场景里的 PlayerManager / GameManager / UI 都完成 Awake + Start
        yield return null;

        DoStartHost();
    }

    /// <summary>
    /// 以「专用服务端」身份启动：不生成自己的玩家，只跑游戏逻辑。
    /// 用命令行 -server 触发；配合 -batchmode -nographics 就是一个无窗口服务器。
    /// </summary>
    public bool StartDedicatedServer()
    {
        if (State == SessionState.Hosting || State == SessionState.Connected) return false;
        if (State == SessionState.Starting) return false;

        NetworkManager nm = EnsureNetworkManager();
        if (nm == null || nm.NetworkConfig.PlayerPrefab == null)
        {
            Fail("无法启动专用服务端：NetworkManager 或玩家预制体缺失");
            return false;
        }

        UnityTransport transport = nm.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
        }

        SessionRequested = true;
        SetState(SessionState.Starting, "正在启动专用服务端...");

        if (ShouldPreloadGameplayScene())
        {
            StartCoroutine(PreloadSceneThenStartServer());
        }
        else
        {
            DoStartServer();
        }

        return true;
    }

    private IEnumerator PreloadSceneThenStartServer()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(gameplaySceneName, LoadSceneMode.Single);
        if (op != null)
        {
            while (!op.isDone)
            {
                yield return null;
            }
        }

        yield return null;
        DoStartServer();
    }

    private void DoStartServer()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Fail("NetworkManager 丢失，无法启动服务端");
            return;
        }

        bool ok = nm.StartServer();
        if (!ok)
        {
            Fail("专用服务端启动失败，端口 " + port + " 可能被占用");
            return;
        }

        Debug.Log("[NetBootstrap] 专用服务端已启动，监听 0.0.0.0:" + port + "，等待玩家连接...");
    }

    private void DoStartHost()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Fail("NetworkManager 丢失，无法创建房间");
            return;
        }

        bool ok = nm.StartHost();
        if (!ok)
        {
            Fail("创建房间失败，请检查端口 " + port + " 是否被占用");
        }
    }

    /// <summary>以客户端身份加入房主。</summary>
    public bool StartClient(string address)
    {
        if (State == SessionState.Hosting || State == SessionState.Connected) return false;
        if (State == SessionState.Starting || State == SessionState.Connecting) return false;

        NetworkManager nm = EnsureNetworkManager();
        if (nm == null)
        {
            Fail("无法加入游戏：NetworkManager 缺失");
            return false;
        }

        string rawAddress = string.IsNullOrWhiteSpace(address) ? defaultAddress : address;

        string cleanAddress;
        string error;
        if (!SanitizeAddress(rawAddress, out cleanAddress, out error))
        {
            Fail("房主 IP 格式不对：" + error);
            return false;
        }

        Address = cleanAddress;

        // 客户端也应该有玩家预制体引用
        if (nm.NetworkConfig.PlayerPrefab == null)
        {
            nm.NetworkConfig.PlayerPrefab = Resources.Load<GameObject>("Multiplayer/Player_Multiplayer");
        }

        UnityTransport transport = nm.GetComponent<UnityTransport>();
        if (transport != null)
        {
            // 客户端：连到房主地址；本地监听用 0.0.0.0 交给系统分配端口
            transport.SetConnectionData(Address, port, "0.0.0.0");
        }

        SessionRequested = true;
        SetState(SessionState.Connecting, $"正在连接 {Address}:{port} ...");

        bool ok = nm.StartClient();
        if (!ok)
        {
            Fail("连接失败，请检查地址与端口");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 清洗用户输入的地址。
    /// 常见的粘贴错误：把面板上「本机 IP: 192.168.1.5 / 10.0.0.3」整行贴进来、
    /// 或者多带了空格、协议前缀。这里统一取出第一个 IPv4 地址。
    /// </summary>
    public static bool SanitizeAddress(string raw, out string address, out string error)
    {
        address = string.Empty;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(raw))
        {
            error = "地址是空的";
            return false;
        }

        string text = raw.Trim();

        // 去掉 "本机 IP" / "IP:" 之类的说明文字
        int colon = text.IndexOf(':');
        if (colon >= 0 && colon < text.Length - 1 && text.IndexOf('.', colon) > colon)
        {
            string afterColon = text.Substring(colon + 1).Trim();
            // 只有当冒号后面像地址时才采用，避免把 "192.168.1.5:7777" 截断错
            if (System.Net.IPAddress.TryParse(afterColon.Split('/', ' ', ',')[0], out _))
            {
                text = afterColon;
            }
        }

        // 去掉多网卡拼接：「192.168.1.5 / 10.0.0.3」→ 取第一个
        string[] pieces = text.Split(new[] { '/', ',', ';', ' ', '\t', '|', '，', '、' },
            System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < pieces.Length; i++)
        {
            string candidate = pieces[i].Trim();

            // 去掉可能带的端口，例如 192.168.1.5:7777
            int portColon = candidate.IndexOf(':');
            if (portColon > 0)
            {
                candidate = candidate.Substring(0, portColon);
            }

            if (System.Net.IPAddress.TryParse(candidate, out System.Net.IPAddress parsed) &&
                parsed.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                address = candidate;
                return true;
            }
        }

        error = $"「{raw}」里没有找到合法的 IPv4 地址（应该形如 192.168.1.5）";
        return false;
    }

    /// <summary>断开并回到单机状态。</summary>
    public void LeaveSession()
    {
        CleanupSession();

        // 回到主菜单
        if (SceneManager.GetActiveScene().name != "Main Menu")
        {
            SceneManager.LoadScene("Main Menu");
        }
    }

    private void CleanupSession()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening)
        {
            nm.Shutdown();
        }

        SessionRequested = false;
        NetGame.Reset();
        SetState(SessionState.Offline, string.Empty);
    }


    private void Fail(string message)
    {
        LastError = message;
        Debug.LogError("[NetBootstrap] " + message);
        SetState(SessionState.Failed, message);

        SessionRequested = false;

        NetworkManager nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening)
        {
            nm.Shutdown();
        }

        // 房主是「先载入关卡 → 再开服」，如果开服失败，玩家会卡在一个
        // 没有角色的关卡里（单机 Player 已经被禁用了）。这里退回主菜单，让玩家能改地址重试。
        Scene active = SceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(active.name) && active.name != MenuSceneName)
        {
            SceneManager.LoadScene(MenuSceneName);
        }
    }

    private void SetState(SessionState newState, string message)
    {
        State = newState;
        StateChanged?.Invoke(newState, message);
    }

    #endregion

    /// <summary>场景里没有 NetBootstrap 时，由菜单 UI 自动创建一个。</summary>
    public static NetBootstrap EnsureExists()
    {
        if (Instance != null) return Instance;

        GameObject go = new GameObject("NetBootstrap");
        return go.AddComponent<NetBootstrap>();
    }

    private const string MenuSceneName = "Main Menu";

    /// <summary>
    /// 取一个最适合告诉朋友的局域网 IP。
    ///
    /// 关键改进：以前用 DNS 枚举，会把 VirtualBox / VMware / Hyper-V 的虚拟网卡也算进来
    /// （例如 192.168.56.1 就是 VirtualBox Host-Only 的默认地址），
    /// 那种地址别人根本连不通。现在改成按网卡枚举，并对虚拟网卡大幅降权。
    /// </summary>
    public static string GetPrimaryLocalIPv4()
    {
        List<LocalAddress> all = GetLocalAddresses();
        if (all.Count == 0) return "127.0.0.1";

        LocalAddress best = null;
        int bestScore = int.MinValue;

        for (int i = 0; i < all.Count; i++)
        {
            LocalAddress entry = all[i];
            int score = ScoreAddress(entry.Address);
            if (entry.IsVirtual) score -= 300;

            if (score > bestScore)
            {
                bestScore = score;
                best = entry;
            }
        }

        return best != null ? best.Address : all[0].Address;
    }

    /// <summary>本机的一块网卡地址。</summary>
    public class LocalAddress
    {
        public string Address;
        public bool IsVirtual;
    }

    private static readonly string[] VirtualAdapterKeywords =
    {
        "virtual", "vmware", "virtualbox", "hyper-v", "vethernet", "tap-", "tun",
        "tailscale", "zerotier", "wintun", "wireguard", "npcap", "loopback",
        "bluetooth", "hamachi", "radmin", "wsl", "docker", "vpn", "teredo", "isatap",
        "loopback", "host-only",
    };

    private static bool IsVirtualAdapter(System.Net.NetworkInformation.NetworkInterface nic)
    {
        string text = ((nic.Name ?? string.Empty) + " " + (nic.Description ?? string.Empty)).ToLowerInvariant();

        for (int i = 0; i < VirtualAdapterKeywords.Length; i++)
        {
            if (text.Contains(VirtualAdapterKeywords[i])) return true;
        }

        return false;
    }

    /// <summary>枚举本机所有 IPv4（按网卡，附带"是否虚拟网卡"标记）。</summary>
    public static List<LocalAddress> GetLocalAddresses()
    {
        List<LocalAddress> result = new List<LocalAddress>();

        try
        {
            System.Net.NetworkInformation.NetworkInterface[] nics =
                System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();

            for (int i = 0; i < nics.Length; i++)
            {
                System.Net.NetworkInformation.NetworkInterface nic = nics[i];
                if (nic == null) continue;
                if (nic.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;
                if (nic.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback) continue;

                bool virtualNic = IsVirtualAdapter(nic);

                System.Net.NetworkInformation.UnicastIPAddressInformationCollection unicast =
                    nic.GetIPProperties().UnicastAddresses;

                for (int j = 0; j < unicast.Count; j++)
                {
                    System.Net.IPAddress ip = unicast[j].Address;
                    if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) continue;

                    string text = ip.ToString();
                    if (string.IsNullOrEmpty(text)) continue;
                    if (text.StartsWith("127.") || text.StartsWith("169.254.")) continue;

                    bool duplicate = false;
                    for (int k = 0; k < result.Count; k++)
                    {
                        if (result[k].Address == text) { duplicate = true; break; }
                    }
                    if (duplicate) continue;

                    LocalAddress entry = new LocalAddress();
                    entry.Address = text;
                    entry.IsVirtual = virtualNic;
                    result.Add(entry);
                }
            }
        }
        catch (Exception)
        {
            // 枚举失败就走下面的兜底
        }

        if (result.Count == 0)
        {
            // 兜底：老办法
            try
            {
                System.Net.IPAddress[] addresses =
                    System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName());

                for (int i = 0; i < addresses.Length; i++)
                {
                    if (addresses[i].AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) continue;

                    string text = addresses[i].ToString();
                    if (string.IsNullOrEmpty(text) || text.StartsWith("127.")) continue;

                    LocalAddress entry = new LocalAddress();
                    entry.Address = text;
                    entry.IsVirtual = false;
                    result.Add(entry);
                }
            }
            catch (Exception) { }
        }

        return result;
    }

    private static int ScoreAddress(string ip)
    {
        if (ip.StartsWith("127.")) return -1000;
        if (ip.StartsWith("169.254.")) return -500;
        if (ip.StartsWith("192.168.")) return 100;
        if (ip.StartsWith("10.")) return 80;

        if (ip.StartsWith("172."))
        {
            int second;
            string[] parts = ip.Split('.');
            if (parts.Length > 1 && int.TryParse(parts[1], out second) && second >= 16 && second <= 31)
            {
                return 60;
            }
        }

        return 10;
    }

    /// <summary>本机所有 IPv4（虚拟网卡会标注出来），仅用于日志与排错。</summary>
    public static string GetLocalIPv4List()
    {
        List<LocalAddress> all = GetLocalAddresses();
        if (all.Count == 0) return "127.0.0.1";

        List<string> parts = new List<string>();
        for (int i = 0; i < all.Count; i++)
        {
            parts.Add(all[i].IsVirtual ? all[i].Address + "(虚拟)" : all[i].Address);
        }

        return string.Join(" / ", parts);
    }

    /// <summary>除主 IP 之外的其它地址（面板上做提示用）。</summary>
    public static string GetAlternativeAddressesHint()
    {
        List<LocalAddress> all = GetLocalAddresses();
        string primary = GetPrimaryLocalIPv4();

        List<string> others = new List<string>();
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].Address == primary) continue;
            others.Add(all[i].IsVirtual ? all[i].Address + "(虚拟)" : all[i].Address);
        }

        return others.Count > 0 ? string.Join(", ", others) : string.Empty;
    }

    /// <summary>把一个 IPv4 的前三段取出来，用来判断两台机器是否在同一网段。</summary>
    public static string GetSubnetPrefix(string ip)
    {
        if (string.IsNullOrEmpty(ip)) return string.Empty;
        string[] parts = ip.Split('.');
        if (parts.Length < 4) return ip;
        return parts[0] + "." + parts[1] + "." + parts[2];
    }
}
