using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// 中文字体支持。
///
/// 背景：工程使用 TMP，但 TMP 的默认字体是 LiberationSans SDF（只有拉丁字形），
/// 而且 TMP Settings 的 fallback 列表是空的 —— 所以所有中文都会显示成 □□□。
///
/// 这里做两件事：
/// 1. 准备一个带中文字形的 TMP 字体资源（动态图集：用到哪个字才生成哪个字）。
///    优先用工程里烘焙好的资源；没有就用系统里的中文字体在运行时生成。
/// 2. 把它挂到 TMP 默认字体的 fallback 表上，
///    这样连原有界面的中文（技能名 / 飘字 / 道具描述 / "你死了"）也会一起被修好。
/// </summary>
public static class ChineseFontSupport
{
    /// <summary>工程内烘焙好的中文字体资源所在目录（Resources 相对路径）。</summary>
    private const string ResourcesFolder = "Multiplayer/Fonts";

    /// <summary>按优先级尝试的系统字体名。</summary>
    private static readonly string[] PreferredOsFonts =
    {
        "Microsoft YaHei UI",
        "Microsoft YaHei",
        "微软雅黑",
        "SimHei",
        "黑体",
        "SimSun",
        "宋体",
        "Noto Sans CJK SC",
        "Noto Sans SC",
        "Source Han Sans SC",
        "Source Han Sans CN",
        "思源黑体",
        "PingFang SC",
        "Hiragino Sans GB",
        "WenQuanYi Micro Hei",
        "Arial Unicode MS",
    };

    /// <summary>测试用汉字，用来判断某个字体资源到底有没有中文字形。</summary>
    private const char ProbeCharacter = '联';

    private static TMP_FontAsset chineseFontAsset;
    private static bool initialized;
    private static bool fallbackInstalled;

    /// <summary>可用的中文字体资源；极端情况下（系统一个中文字体都没有）为 null。</summary>
    public static TMP_FontAsset ChineseFont
    {
        get
        {
            EnsureInitialized();
            return chineseFontAsset;
        }
    }

    // ------------------------------------------------------------------
    // 启动时自动安装
    // ------------------------------------------------------------------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInstall()
    {
        EnsureInitialized();
        InstallGlobalFallback();
    }

    /// <summary>把中文字体挂到 TMP 默认字体的 fallback 上（只做一次）。</summary>
    public static void InstallGlobalFallback()
    {
        if (fallbackInstalled) return;

        EnsureInitialized();

        if (chineseFontAsset == null)
        {
            Debug.LogWarning("[中文字体] 没有找到可用的中文字体，中文可能显示为方块。" +
                             "请执行菜单 Tools/联机改造/③ 生成中文字体资源。");
            return;
        }

        fallbackInstalled = true;

        // 1) 挂到 TMP 默认字体的 fallback 链上 —— 这一条能修好工程里所有已有界面的中文
        TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont != null && defaultFont != chineseFontAsset)
        {
            AddFallback(defaultFont, chineseFontAsset);
        }

        // 2) TMP 全局 fallback 列表
        List<TMP_FontAsset> global = TMP_Settings.fallbackFontAssets;
        if (global != null && !global.Contains(chineseFontAsset))
        {
            global.Add(chineseFontAsset);
        }

        Debug.Log("[中文字体] 已安装中文 fallback：" + chineseFontAsset.name);
    }

    private static void AddFallback(TMP_FontAsset owner, TMP_FontAsset fallback)
    {
        if (owner == null || fallback == null) return;

        List<TMP_FontAsset> table = owner.fallbackFontAssetTable;
        if (table == null)
        {
            table = new List<TMP_FontAsset>();
            owner.fallbackFontAssetTable = table;
        }

        if (!table.Contains(fallback))
        {
            table.Add(fallback);
        }
    }

    /// <summary>把某个 TMP 文本直接换成中文字体（自己创建的 UI 用这个最稳）。</summary>
    public static void Apply(TMP_Text text)
    {
        if (text == null) return;

        EnsureInitialized();
        if (chineseFontAsset == null) return;

        if (text.font != chineseFontAsset)
        {
            text.font = chineseFontAsset;
        }
    }

    // ------------------------------------------------------------------
    // 资源准备
    // ------------------------------------------------------------------

    private static void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;

        chineseFontAsset = LoadBakedFontAsset();
        if (chineseFontAsset != null) return;

        chineseFontAsset = CreateFromSystemFont();
    }

    /// <summary>优先用工程里烘焙好的（打包出去也能用）。</summary>
    private static TMP_FontAsset LoadBakedFontAsset()
    {
        TMP_FontAsset found = SearchFontAssets(ResourcesFolder);
        if (found != null) return found;

        return SearchFontAssets(string.Empty);
    }

    private static TMP_FontAsset SearchFontAssets(string folder)
    {
        TMP_FontAsset[] assets;
        try
        {
            assets = Resources.LoadAll<TMP_FontAsset>(folder);
        }
        catch (System.Exception)
        {
            return null;
        }

        if (assets == null) return null;

        for (int i = 0; i < assets.Length; i++)
        {
            TMP_FontAsset candidate = assets[i];
            if (candidate == null) continue;
            if (HasChineseGlyph(candidate)) return candidate;
        }

        return null;
    }

    /// <summary>退而求其次：用系统字体在运行时生成一个动态中文字体资源。</summary>
    private static TMP_FontAsset CreateFromSystemFont()
    {
        string[] installed;
        try
        {
            installed = UnityEngine.Font.GetOSInstalledFontNames();
        }
        catch (System.Exception)
        {
            installed = null;
        }

        string chosen = PickOsFontName(installed);
        if (string.IsNullOrEmpty(chosen)) return null;

        try
        {
            UnityEngine.Font osFont = UnityEngine.Font.CreateDynamicFontFromOSFont(chosen, 48);
            if (osFont == null) return null;

            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(
                osFont,
                90,                          // 采样字号
                9,                           // 图集内边距
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,  // 动态图集：用到哪个字才生成哪个字
                true);

            if (asset == null) return null;

            asset.name = "Chinese Runtime (" + chosen + ")";
            Debug.Log($"[中文字体] 已用系统字体「{chosen}」生成动态中文字体资源。");
            return asset;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[中文字体] 用系统字体生成中文字体失败：" + e.Message);
            return null;
        }
    }

    private static string PickOsFontName(string[] installed)
    {
        if (installed == null || installed.Length == 0)
        {
            // 拿不到列表就直接硬试最常见的那个
            return "Microsoft YaHei";
        }

        for (int i = 0; i < PreferredOsFonts.Length; i++)
        {
            string wanted = PreferredOsFonts[i];
            for (int j = 0; j < installed.Length; j++)
            {
                if (string.Equals(installed[j], wanted, System.StringComparison.OrdinalIgnoreCase))
                {
                    return installed[j];
                }
            }
        }

        // 再退一步：名字里带 yahei / simhei / cjk / heiti 之类的
        for (int j = 0; j < installed.Length; j++)
        {
            string name = installed[j];
            if (string.IsNullOrEmpty(name)) continue;

            string lower = name.ToLowerInvariant();
            if (lower.Contains("yahei") || lower.Contains("simhei") || lower.Contains("simsun") ||
                lower.Contains("cjk") || lower.Contains("heiti") || lower.Contains("song") ||
                lower.Contains("kai") || lower.Contains("hei"))
            {
                return name;
            }
        }

        return null;
    }

    private static bool HasChineseGlyph(TMP_FontAsset asset)
    {
        if (asset == null) return false;

        try
        {
            if (asset.HasCharacter(ProbeCharacter)) return true;

            // 动态字体资源的图集里暂时还没有这个字，但实际是能生成出来的
            return asset.atlasPopulationMode == AtlasPopulationMode.Dynamic;
        }
        catch (System.Exception)
        {
            return false;
        }
    }
}
