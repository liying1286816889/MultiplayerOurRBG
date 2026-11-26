using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 开局主菜单里的「联机」面板。
///
/// 完全用代码构建，不需要在场景里手工摆 UI，也不会弄乱原本的菜单预制体。
/// 只在 Main Menu 场景里自动出现（见 <see cref="AutoInject"/>）。
/// </summary>
public class MultiplayerMenuUI : MonoBehaviour
{
    [Header("外观")]
    [SerializeField] private Vector2 panelSize = new Vector2(420f, 380f);

    private NetBootstrap bootstrap;
    private GameObject panelRoot;
    private TMP_InputField addressField;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI localIpText;
    private Button hostButton;
    private Button joinButton;
    private Button leaveButton;
    private Button copyIpButton;

    private bool busy;

    #region 自动注入

    /// <summary>
    /// 进入主菜单场景时自动创建联机面板。
    /// 放在 Main Menu 场景才会生效，游戏内场景不会出现。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInject()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TryCreate(SceneManager.GetActiveScene().name);
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreate(scene.name);
    }

    private static void TryCreate(string sceneName)
    {
        if (!IsMenuScene(sceneName)) return;
        if (FindObjectOfType<MultiplayerMenuUI>() != null) return;

        GameObject go = new GameObject("MultiplayerMenuUI");
        go.AddComponent<MultiplayerMenuUI>();
    }

    private static bool IsMenuScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;

        string lower = sceneName.ToLowerInvariant();
        return lower.Contains("menu") || lower.Contains("菜单") || lower.Contains("主菜单");
    }

    #endregion

    private void Start()
    {
        if (!IsMenuScene(SceneManager.GetActiveScene().name))
        {
            enabled = false;
            return;
        }

        bootstrap = NetBootstrap.EnsureExists();
        bootstrap.StateChanged += OnStateChanged;

        BuildUI();
        RefreshButtons();

        OnStateChanged(bootstrap.State, bootstrap.LastError);
        UpdateLocalIpHint();
    }

    private void OnDestroy()
    {
        if (bootstrap != null)
        {
            bootstrap.StateChanged -= OnStateChanged;
        }
    }

    #region UI 构建

    private void BuildUI()
    {
        // 根 Canvas
        panelRoot = new GameObject("MultiplayerMenuCanvas");
        Canvas canvas = panelRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = panelRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        panelRoot.AddComponent<GraphicRaycaster>();

        EnsureEventSystem();

        // 背景板
        GameObject panel = CreateRect("Panel", panelRoot.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.anchoredPosition = new Vector2(-40f, 40f);
        panelRect.sizeDelta = new Vector2(panelSize.x, panelSize.y);

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.06f, 0.08f, 0.92f);

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 16, 16);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = true;   // 让每个元素按自身内容撑开
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;

        // 面板高度自动跟随内容，避免文字变多之后溢出到底部
        ContentSizeFitter fitter = panel.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateLabel(panel.transform, "联机 / LAN", 26f, TextAlignmentOptions.Center, new Color(0.95f, 0.85f, 0.55f));

        localIpText = CreateLabel(panel.transform, "本机 IP: ...", 15f, TextAlignmentOptions.Center, new Color(0.7f, 0.75f, 0.8f));

        copyIpButton = CreateButton(panel.transform, "复制本机 IP（发给朋友）",
            new Color(0.24f, 0.24f, 0.30f), OnCopyIpClicked);
        SetHeight(copyIpButton.gameObject, 34f);

        CreateLabel(panel.transform, "── 下面这行只有「加入的人」需要填 ──",
            13f, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.66f));

        CreateLabel(panel.transform, "房主的局域网 IP（房主自己留 127.0.0.1 就行）",
            14f, TextAlignmentOptions.Left, new Color(0.8f, 0.8f, 0.85f));
        addressField = CreateInputField(panel.transform, "127.0.0.1");

        hostButton = CreateButton(panel.transform, "创建房间（我是房主）", new Color(0.16f, 0.42f, 0.24f), OnHostClicked);
        joinButton = CreateButton(panel.transform, "加入房间", new Color(0.16f, 0.30f, 0.50f), OnJoinClicked);
        leaveButton = CreateButton(panel.transform, "断开 / 回到单机", new Color(0.42f, 0.18f, 0.18f), OnLeaveClicked);

        statusText = CreateLabel(panel.transform, "单人模式", 14f, TextAlignmentOptions.Center, new Color(0.85f, 0.85f, 0.6f));

        CreateLabel(panel.transform,
            "不点上面任何按钮 = 原版单机玩法",
            12f, TextAlignmentOptions.Center, new Color(0.55f, 0.55f, 0.6f));
    }

    private static void SetHeight(GameObject go, float height)
    {
        LayoutElement element = go.GetComponent<LayoutElement>();
        if (element == null) element = go.AddComponent<LayoutElement>();

        element.minHeight = height;
        element.preferredHeight = height;
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    /// <summary>
    /// 把 TMP 文本换成带中文字形的字体，否则中文会显示成方块。
    /// </summary>
    private static void UseChineseFont(TMP_Text text)
    {
        ChineseFontSupport.Apply(text);
    }

    private static GameObject CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static TextMeshProUGUI CreateLabel(Transform parent, string text, float size,
        TextAlignmentOptions alignment, Color color)
    {
        GameObject go = CreateRect("Label", parent);
        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.alignment = alignment;
        label.color = color;
        label.raycastTarget = false;

        UseChineseFont(label);

        // minHeight 保证至少一行；preferredHeight 留 -1 交给 TMP 自己算，
        // 这样两行以上的提示文字（比如等待好友加入）不会被裁掉。
        LayoutElement element = go.AddComponent<LayoutElement>();
        element.minHeight = size + 8f;
        element.preferredHeight = -1f;

        return label;
    }

    private static TMP_InputField CreateInputField(Transform parent, string defaultText)
    {
        GameObject go = CreateRect("AddressInput", parent);

        // 先关掉再配置：TMP_InputField 在 OnEnable 里才会创建光标（Caret），
        // 所以必须先写好 textComponent 再激活，否则输入框没有光标。
        go.SetActive(false);

        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.14f, 0.14f, 0.17f, 1f);

        TMP_InputField input = go.AddComponent<TMP_InputField>();

        GameObject viewport = CreateRect("Text Area", go.transform);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(10f, 4f);
        viewportRect.offsetMax = new Vector2(-10f, -4f);
        viewport.AddComponent<RectMask2D>();

        GameObject textGo = CreateRect("Text", viewport.transform);
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textGo.AddComponent<TextMeshProUGUI>();
        text.fontSize = 18f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Left;
        text.text = defaultText;

        UseChineseFont(text);

        input.textViewport = viewportRect;
        input.textComponent = text;
        input.targetGraphic = bg;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.text = defaultText;

        LayoutElement element = go.AddComponent<LayoutElement>();
        element.minHeight = 38f;
        element.preferredHeight = 38f;

        go.SetActive(true);

        return input;
    }

    private static Button CreateButton(Transform parent, string text, Color color, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = CreateRect("Button", parent);
        Image bg = go.AddComponent<Image>();
        bg.color = color;

        Button button = go.AddComponent<Button>();
        button.targetGraphic = bg;
        button.onClick.AddListener(onClick);

        GameObject labelGo = CreateRect("Text", go.transform);
        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelGo.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 18f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;

        UseChineseFont(label);

        LayoutElement element = go.AddComponent<LayoutElement>();
        element.minHeight = 44f;
        element.preferredHeight = 44f;

        return button;
    }

    #endregion

    #region 交互

    private void OnHostClicked()
    {
        if (busy) return;

        busy = true;
        RefreshButtons();

        // 房主不需要填地址：本机自己连 127.0.0.1，对外监听 0.0.0.0。
        // 地址框只对「加入的人」有意义。
        SetStatus("正在创建房间...");
        bootstrap.StartHost();

        StartCoroutine(ClearBusyAfter(5f));
    }

    private void OnCopyIpClicked()
    {
        string ip = NetBootstrap.GetPrimaryLocalIPv4();

        try
        {
            GUIUtility.systemCopyBuffer = ip;
            SetStatus("已复制本机 IP：" + ip + "（发给朋友，让对方填在输入框里）");
        }
        catch (System.Exception)
        {
            SetStatus("本机 IP：" + ip);
        }
    }

    private void OnJoinClicked()
    {
        if (busy) return;

        string address = addressField != null ? addressField.text.Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(address))
        {
            SetStatus("请先填写房主的 IP 地址（房主点「复制本机 IP」发给你）");
            return;
        }

        string cleanAddress;
        string error;
        if (!NetBootstrap.SanitizeAddress(address, out cleanAddress, out error))
        {
            SetStatus("IP 格式不对：" + error);
            return;
        }

        busy = true;
        RefreshButtons();

        // 把清洗后的地址写回输入框，方便玩家看到到底连的是哪个地址
        if (addressField != null && addressField.text != cleanAddress)
        {
            addressField.text = cleanAddress;
        }

        SetStatus($"正在连接 {cleanAddress} ...");
        bootstrap.StartClient(cleanAddress);

        StartCoroutine(ClearBusyAfter(8f));
    }

    private void OnLeaveClicked()
    {
        if (bootstrap != null)
        {
            bootstrap.LeaveSession();
        }

        busy = false;
        SetStatus("已回到单人模式");
        RefreshButtons();
    }

    private IEnumerator ClearBusyAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        busy = false;
        RefreshButtons();
    }

    private void OnStateChanged(NetBootstrap.SessionState state, string message)
    {
        if (panelRoot == null) return;

        switch (state)
        {
            case NetBootstrap.SessionState.Offline:
                SetStatus("单人模式");
                break;
            case NetBootstrap.SessionState.Starting:
                SetStatus("正在创建房间...");
                break;
            case NetBootstrap.SessionState.Hosting:
                SetStatus($"房间已创建 ✅\n好友填这个 IP 加入：{NetBootstrap.GetPrimaryLocalIPv4()}\n（跨网络的先看 README 第二节）");
                break;
            case NetBootstrap.SessionState.Connecting:
                SetStatus(string.IsNullOrEmpty(message) ? "正在连接..." : message);
                break;
            case NetBootstrap.SessionState.Connected:
                SetStatus("连接成功！");
                break;
            case NetBootstrap.SessionState.Failed:
                SetStatus("失败：" + message);
                busy = false;
                break;
        }

        RefreshButtons();
    }

    private void RefreshButtons()
    {
        bool online = bootstrap != null &&
                      (bootstrap.State == NetBootstrap.SessionState.Hosting ||
                       bootstrap.State == NetBootstrap.SessionState.Connected);

        if (hostButton != null) hostButton.interactable = !busy && !online;
        if (joinButton != null) joinButton.interactable = !busy && !online;
        if (leaveButton != null) leaveButton.interactable = online || busy;
        if (addressField != null) addressField.interactable = !busy && !online;
        if (copyIpButton != null) copyIpButton.interactable = true;
    }

    private void SetStatus(string text)
    {
        if (statusText != null) statusText.text = text;
    }

    private void UpdateLocalIpHint()
    {
        if (localIpText != null)
        {
            // 只显示一个 IP：之前用「/」把多网卡拼在一起，
            // 房主很容易连那一整串一起复制进地址框，导致地址非法。
            string others = NetBootstrap.GetAlternativeAddressesHint();
            localIpText.text = "本机 IP（告诉朋友）：" + NetBootstrap.GetPrimaryLocalIPv4()
                + (string.IsNullOrEmpty(others) ? string.Empty : "\n其它网卡：" + others + "（虚拟网卡通常连不通）");
        }
    }

    #endregion
}
