#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// 一键把单机工程改造成可联机的工程。
///
/// 它会做这些事（可重复执行，不会重复添加组件）：
///  1. 把场景里的 Player 复制成 Assets/Resources/Multiplayer/Player_Multiplayer.prefab，
///     并挂上 NetworkObject / OwnerNetworkTransform / PlayerNetwork。
///  2. 给 ItemObject 与 LostCurrency 预制体补上网络组件。
///  3. 给游戏场景里所有敌人补上网络组件（服务器权威）。
///  4. 给场景里的存档点补上网络组件。
///  5. 在玩家出生位置放一个锚点 + 两个出生点。
///  6. 在 Main Menu 场景放一个 NetBootstrap，并把网络预制体登记进去。
///
/// 执行完之后用「联机自检」菜单确认没有遗漏。
/// </summary>
public static class MultiplayerSetupTool
{
    private const string ResourcesMultiplayer = "Assets/Resources/Multiplayer";
    private const string PlayerPrefabPath = ResourcesMultiplayer + "/Player_Multiplayer.prefab";

    private const string ItemObjectSourcePath = "Assets/Prefabs/ItemObject_空.prefab";
    private const string LostCurrencySourcePath = "Assets/Prefabs/LostCurrency.prefab";

    private const string MenuScenePath = "Assets/Scenes/Main Menu.unity";

    /// <summary>
    /// 工程里自带的源字体（按优先级）。用来生成 TMP 中文字体资源。
    /// 注意：这两个是「字体文件」，不是 TMP 字体资源 —— TMP 必须先烘焙成 .asset 才能用。
    /// </summary>
    private static readonly string[] CjkSourceFontPaths =
    {
        "Assets/Graphics/Font/字体/msyh.ttc",                        // 微软雅黑
        "Assets/Graphics/Font/字体/SourceHanSansSC-Regular.otf",     // 思源黑体 简体
    };

    /// <summary>生成出来的 TMP 中文字体资源路径（放在 Resources 下，运行时才能加载）。</summary>
    private const string CjkFontAssetPath = ResourcesMultiplayer + "/Fonts/Chinese SDF.asset";

    private static readonly string[] GameplayScenePaths =
    {
        "Assets/Scenes/MainScene.unity",
        "Assets/Scenes/MainPass1.unity",
    };

    // =================================================================
    // 菜单入口
    // =================================================================

    [MenuItem("Tools/联机改造/① 一键配置联机场景", false, 1)]
    public static void SetupAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("[联机改造] 已取消：请先保存当前场景。");
            return;
        }

        try
        {
            EditorUtility.DisplayProgressBar("联机改造", "检查目录...", 0.05f);
            EnsureFolders();

            EditorUtility.DisplayProgressBar("联机改造", "生成中文字体资源...", 0.10f);
            BuildChineseFontAsset();

            EditorUtility.DisplayProgressBar("联机改造", "处理道具 / 掉落预制体...", 0.15f);
            GameObject itemObjectPrefab = SetupNetworkedPrefab(ItemObjectSourcePath, root =>
            {
                EnsureComponent<NetworkObject>(root);
                EnsureComponent<NetItemObject>(root);
            });

            GameObject lostCurrencyPrefab = SetupNetworkedPrefab(LostCurrencySourcePath, root =>
            {
                EnsureComponent<NetworkObject>(root);
                EnsureComponent<WorldObjectNetwork>(root);
            });

            float step = 0.7f / Mathf.Max(1, GameplayScenePaths.Length);
            float progress = 0.25f;

            for (int i = 0; i < GameplayScenePaths.Length; i++)
            {
                string path = GameplayScenePaths[i];
                if (!System.IO.File.Exists(path))
                {
                    Debug.LogWarning("[联机改造] 找不到场景，跳过：" + path);
                    continue;
                }

                EditorUtility.DisplayProgressBar("联机改造", "处理场景 " + path, progress);
                SetupGameplayScene(path, lostCurrencyPrefab);
                progress += step;
            }

            EditorUtility.DisplayProgressBar("联机改造", "配置主菜单 / NetBootstrap...", 0.92f);
            SetupMenuScene(itemObjectPrefab, lostCurrencyPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[联机改造] 配置完成！请执行 Tools/联机改造/② 联机自检 确认没有遗漏。");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[联机改造] 配置失败：" + e);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    [MenuItem("Tools/联机改造/② 联机自检", false, 2)]
    public static void VerifySetup()
    {
        int problems = 0;

        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CjkFontAssetPath) == null)
        {
            Debug.LogWarning("[自检] 还没有生成中文字体资源，中文可能显示成方块。" +
                             "请执行 Tools/联机改造/③ 生成中文字体资源。");
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) == null)
        {
            Debug.LogError("[自检] 缺少玩家网络预制体：" + PlayerPrefabPath);
            problems++;
        }
        else
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab.GetComponent<NetworkObject>() == null)
            {
                Debug.LogError("[自检] 玩家预制体缺少 NetworkObject");
                problems++;
            }
            if (prefab.GetComponent<PlayerNetwork>() == null)
            {
                Debug.LogError("[自检] 玩家预制体缺少 PlayerNetwork");
                problems++;
            }
            if (prefab.GetComponent<OwnerNetworkTransform>() == null)
            {
                Debug.LogError("[自检] 玩家预制体缺少 OwnerNetworkTransform（位移不会同步）");
                problems++;
            }
            if (prefab.GetComponent<NetworkCharacterStats>() == null)
            {
                Debug.LogError("[自检] 玩家预制体缺少 NetworkCharacterStats（血量不会同步）");
                problems++;
            }
        }

        foreach (string path in GameplayScenePaths)
        {
            if (!System.IO.File.Exists(path)) continue;

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

            int enemies = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Enemy[] found = root.GetComponentsInChildren<Enemy>(true);
                for (int i = 0; i < found.Length; i++)
                {
                    enemies++;
                    if (found[i].GetComponent<NetworkObject>() == null ||
                        found[i].GetComponent<EnemyNetwork>() == null)
                    {
                        Debug.LogError($"[自检] {path} 里的敌人 {found[i].name} 还没配置网络组件");
                        problems++;
                    }
                }
            }

            EditorSceneManager.CloseScene(scene, true);
            Debug.Log($"[自检] {path}：共 {enemies} 个敌人。");
        }

        if (problems == 0)
        {
            Debug.Log("[自检] 全部通过 ✅ 现在可以直接在 Main Menu 里创建/加入房间了。");
        }
        else
        {
            Debug.LogError($"[自检] 发现 {problems} 个问题，请重新执行「① 一键配置联机场景」。");
        }
    }

    [MenuItem("Tools/联机改造/③ 生成中文字体资源", false, 3)]
    public static void BuildChineseFontAssetMenu()
    {
        if (BuildChineseFontAsset())
        {
            EditorUtility.DisplayDialog(
                "中文字体",
                "已生成：" + CjkFontAssetPath + "\n\n" +
                "所有中文（包括原有界面的技能名、飘字、道具描述）现在都能正常显示了。",
                "好");
        }
        else
        {
            EditorUtility.DisplayDialog(
                "中文字体",
                "没有生成新的字体资源（已存在或源字体缺失），详见 Console。\n\n" +
                "即使这里失败，运行时也会自动改用系统里的中文字体（微软雅黑等），一般也能正常显示。",
                "知道了");
        }
    }

    /// <summary>
    /// 用工程自带的源字体生成一个「动态图集」的 TMP 中文字体资源。
    ///
    /// 为什么不用现成的那两个？
    ///  · SIMSUN SDF 只烘焙了 123 个字符（标点 + 极少数汉字），用来显示中文会大面积方块；
    ///  · 我的字体.asset 是 Static 模式、8192×8192 × 25 页图集（文件 130MB），
    ///    为了一个菜单去加载它代价太大。
    /// 所以这里从源字体（msyh.ttc / 思源黑体）生成动态图集：
    /// 只占一张 1024×1024，用到哪个字才生成哪个字，两万多个汉字都能显示。
    /// </summary>
    public static bool BuildChineseFontAsset()
    {
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CjkFontAssetPath) != null)
        {
            return true;    // 已经生成过
        }

        EnsureFolder(ResourcesMultiplayer + "/Fonts");

        for (int i = 0; i < CjkSourceFontPaths.Length; i++)
        {
            string sourcePath = CjkSourceFontPaths[i];
            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (sourceFont == null)
            {
                continue;
            }

            if (TryCreateDynamicFontAsset(sourceFont, sourcePath))
            {
                return true;
            }

            Debug.LogWarning("[中文字体] 用 " + sourcePath + " 生成失败，尝试下一个源字体。");
        }

        Debug.LogWarning("[中文字体] 所有源字体都不可用，跳过。运行时会改用系统里的中文字体（微软雅黑等）。");
        return false;
    }

    private static bool TryCreateDynamicFontAsset(Font sourceFont, string sourcePath)
    {
        TMP_FontAsset fontAsset = null;

        try
        {
            fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,                          // 采样字号
                9,                           // 图集内边距
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic, // 动态图集
                true);

            if (fontAsset == null)
            {
                return false;
            }

            fontAsset.name = "Chinese SDF";

            // 先塞几个字进去，既验证字形能生成，也让图集真正被创建出来
            string missing = string.Empty;
            try
            {
                fontAsset.TryAddCharacters("联机创建房间加入断开始游戏", out missing);
            }
            catch (System.Exception)
            {
                missing = string.Empty;   // 只是验证用，抛异常不致命
            }

            if (!string.IsNullOrEmpty(missing))
            {
                Debug.LogWarning("[中文字体] " + sourcePath + " 无法生成这些字：" + missing);
                Object.DestroyImmediate(fontAsset);
                return false;
            }

            AssetDatabase.CreateAsset(fontAsset, CjkFontAssetPath);

            // atlas 贴图与材质必须作为子资源存下来，否则重新加载后字体就丢了
            if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0 &&
                fontAsset.atlasTextures[0] != null)
            {
                fontAsset.atlasTextures[0].name = fontAsset.name + " Atlas";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
            }

            if (fontAsset.material != null)
            {
                fontAsset.material.name = fontAsset.name + " Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[中文字体] 已用 " + sourcePath + " 生成动态中文字体：" + CjkFontAssetPath);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[中文字体] 用 " + sourcePath + " 生成时出错：" + e.Message);

            if (fontAsset != null)
            {
                Object.DestroyImmediate(fontAsset);
            }

            return false;
        }
    }

    [MenuItem("Tools/联机改造/④ 修复敌人网络组件（拆卸重建）", false, 4)]
    public static void RepairEnemyNetworkComponents()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("[联机修复] 已取消：请先保存当前场景。");
            return;
        }

        int repaired = 0;
        int touchedScenes = 0;

        try
        {
            foreach (string path in GameplayScenePaths)
            {
                if (!System.IO.File.Exists(path)) continue;

                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

                Enemy[] enemies = Object.FindObjectsOfType<Enemy>(true);
                for (int i = 0; i < enemies.Length; i++)
                {
                    Enemy enemy = enemies[i];
                    if (enemy == null) continue;

                    GameObject go = enemy.gameObject;

                    // 先把旧的网络组件全部拆掉（连同可能写坏的序列化数据）
                    RemoveIfPresent<EnemyNetwork>(go);
                    RemoveIfPresent<NetworkCharacterStats>(go);
                    RemoveIfPresent<NetworkTransform>(go);
                    RemoveIfPresent<OwnerNetworkTransform>(go);
                    RemoveIfPresent<NetworkObject>(go);

                    // 再按正确顺序重新挂上
                    EnsureComponent<NetworkObject>(go);

                    NetworkTransform netTransform = EnsureComponent<NetworkTransform>(go);
                    netTransform.Interpolate = true;
                    netTransform.InLocalSpace = false;
                    netTransform.SyncPositionX = true;
                    netTransform.SyncPositionY = true;
                    netTransform.SyncPositionZ = false;
                    netTransform.SyncRotAngleX = false;
                    netTransform.SyncRotAngleY = true;
                    netTransform.SyncRotAngleZ = false;
                    netTransform.SyncScaleX = false;
                    netTransform.SyncScaleY = false;
                    netTransform.SyncScaleZ = false;

                    EnsureComponent<NetworkCharacterStats>(go);
                    EnsureComponent<EnemyNetwork>(go);

                    repaired++;
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                touchedScenes++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "联机修复",
                $"已重建 {repaired} 个敌人的网络组件（{touchedScenes} 个场景）。\n\n" +
                "接下来请务必：\n" +
                "1. 关闭 Unity\n" +
                "2. 删除这个目录：Library\\PlayerDataCache\n" +
                "3. 重新打开 Unity 并重新打包\n\n" +
                "删缓存是为了让 Unity 重新序列化关卡数据，而不是照抄旧的。",
                "好");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[联机修复] 失败：" + e);
        }
    }

    private static void RemoveIfPresent<T>(GameObject go) where T : Component
    {
        T existing = go.GetComponent<T>();
        if (existing != null)
        {
            Object.DestroyImmediate(existing, true);
        }
    }

    [MenuItem("Tools/联机改造/⑤ 移除场景网络组件（打包排查用）", false, 5)]
    public static void StripNetworkComponentsForBisect()
    {
        bool ok = EditorUtility.DisplayDialog(
            "打包排查",
            "这个操作会把游戏场景里【所有联机组件】删掉，让工程回到纯单机状态，\n" +
            "用来判断「打包崩溃」到底是不是联机改造引入的。\n\n" +
            "· 排查完请重新执行「① 一键配置联机场景」恢复\n" +
            "· Resources 里的玩家网络预制体不会被删\n\n" +
            "继续吗？",
            "移除并保存", "取消");

        if (!ok) return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("[排查] 已取消：请先保存当前场景。");
            return;
        }

        int removed = 0;
        int scenes = 0;

        try
        {
            foreach (string path in GameplayScenePaths)
            {
                if (!System.IO.File.Exists(path)) continue;

                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

                // 注意删除顺序：先删 NetworkBehaviour，最后删 NetworkObject
                removed += RemoveAllOfType<EnemyNetwork>(scene);
                removed += RemoveAllOfType<NetworkCharacterStats>(scene);
                removed += RemoveAllOfType<WorldObjectNetwork>(scene);
                removed += RemoveAllOfType<NetItemObject>(scene);
                removed += RemoveAllOfType<OwnerNetworkTransform>(scene);
                removed += RemoveAllOfType<NetworkTransform>(scene);
                removed += RemoveAllOfType<NetworkObject>(scene);

                // 顺带清掉出生点标记，避免残留
                removed += RemoveAllOfType<MultiplayerSpawnPoint>(scene);
                removed += RemoveAllOfType<GameModeAnchor>(scene);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                scenes++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "打包排查",
                $"已从 {scenes} 个场景移除 {removed} 个组件。\n\n" +
                "现在去打包，运行 exe 并走「新游戏」：\n" +
                "· 不崩了 → 是联机组件导致的，重点查 NetworkTransform\n" +
                "· 还崩   → 和联机无关，是场景里本来就有的东西\n\n" +
                "排查完记得执行「① 一键配置联机场景」恢复联机。",
                "好");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[排查] 失败：" + e);
        }
    }

    private static int RemoveAllOfType<T>(Scene scene) where T : Component
    {
        int count = 0;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T[] found = root.GetComponentsInChildren<T>(true);
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] == null) continue;
                Object.DestroyImmediate(found[i], true);
                count++;
            }
        }

        return count;
    }

    [MenuItem("Tools/联机改造/帮助：怎么联机", false, 20)]
    public static void ShowHelp()
    {
        EditorUtility.DisplayDialog(
            "怎么联机",
            "1. 执行「① 一键配置联机场景」（只需一次）。\n" +
            "2. 打开 Assets/Scenes/Main Menu.unity，点 Play。\n" +
            "3. 左下角会出现联机面板：\n" +
            "   · 一台机器点「创建房间」当房主\n" +
            "   · 另一台机器填房主 IP，点「加入房间」\n" +
            "4. 不想联机时什么都别点，就是原来的单机游戏。\n\n" +
            "注意：两台机器需要同一个局域网；首次联机请允许防火墙访问。",
            "知道了");
    }

    // =================================================================
    // 目录
    // =================================================================

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder(ResourcesMultiplayer);
        EnsureFolder("Assets/Resources/Multiplayer/NetworkPrefabs");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = System.IO.Path.GetFileName(path);

        if (!AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, leaf);
    }

    // =================================================================
    // 预制体
    // =================================================================

    private delegate void PrefabEdit(GameObject root);

    /// <summary>在已有预制体上补组件并原样保存（保留原有引用关系）。</summary>
    private static GameObject SetupNetworkedPrefab(string path, PrefabEdit edit)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null)
        {
            Debug.LogWarning("[联机改造] 找不到预制体，跳过：" + path);
            return null;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            edit(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.ImportAsset(path);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    // =================================================================
    // 玩家预制体
    // =================================================================

    private static GameObject BuildPlayerPrefab(Player scenePlayer, GameObject lostCurrencyPrefab)
    {
        if (scenePlayer == null)
        {
            Debug.LogError("[联机改造] 场景里找不到 Player 组件，无法生成玩家预制体。");
            return null;
        }

        GameObject copy = Object.Instantiate(
            scenePlayer.gameObject,
            scenePlayer.transform.position,
            scenePlayer.transform.rotation);
        copy.name = "Player_Multiplayer";

        try
        {
            EnsureComponent<NetworkObject>(copy);

            OwnerNetworkTransform transform = EnsureComponent<OwnerNetworkTransform>(copy);
            transform.Interpolate = true;
            transform.InLocalSpace = false;
            transform.SyncPositionX = true;
            transform.SyncPositionY = true;
            transform.SyncPositionZ = false;
            transform.SyncRotAngleX = false;
            transform.SyncRotAngleY = true;
            transform.SyncRotAngleZ = false;
            transform.SyncScaleX = false;
            transform.SyncScaleY = false;
            transform.SyncScaleZ = false;

            PlayerNetwork playerNetwork = EnsureComponent<PlayerNetwork>(copy);

            // 血量 / 元素状态的网络同步（少了这个双方血量不会同步）
            EnsureComponent<NetworkCharacterStats>(copy);

            if (lostCurrencyPrefab != null)
            {
                SerializedObject so = new SerializedObject(playerNetwork);
                SerializedProperty prop = so.FindProperty("lostCurrencyPrefab");
                if (prop != null)
                {
                    prop.objectReferenceValue = lostCurrencyPrefab;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(copy, PlayerPrefabPath);
            Debug.Log("[联机改造] 已生成玩家网络预制体：" + PlayerPrefabPath);
            return prefab;
        }
        finally
        {
            Object.DestroyImmediate(copy);
        }
    }

    // =================================================================
    // 游戏场景
    // =================================================================

    private static void SetupGameplayScene(string scenePath, GameObject lostCurrencyPrefab)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // ---------- 玩家 ----------
        Player scenePlayer = null;
        Player[] players = Object.FindObjectsOfType<Player>(true);
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].GetComponent<NetworkObject>() == null)
            {
                scenePlayer = players[i];
                break;
            }
        }

        if (scenePlayer != null)
        {
            BuildPlayerPrefab(scenePlayer, lostCurrencyPrefab);

            Vector3 spawn = scenePlayer.transform.position;

            // 出生锚点：联机时场景里的 Player 会被禁用，靠这个锚点记住位置
            GameModeAnchor anchor = Object.FindObjectOfType<GameModeAnchor>();
            if (anchor == null)
            {
                GameObject anchorGo = new GameObject("MultiplayerSpawnAnchor");
                anchorGo.transform.position = spawn;
                anchorGo.AddComponent<GameModeAnchor>();
            }

            // 两个出生点
            MultiplayerSpawnPoint[] existing = Object.FindObjectsOfType<MultiplayerSpawnPoint>(true);
            if (existing == null || existing.Length < 2)
            {
                GameObject container = GameObject.Find("MultiplayerSpawnPoints");
                if (container == null)
                {
                    container = new GameObject("MultiplayerSpawnPoints");
                }

                for (int i = 0; i < 2; i++)
                {
                    string name = "SpawnPoint_" + (i + 1);
                    if (container.transform.Find(name) != null) continue;

                    GameObject point = new GameObject(name);
                    point.transform.SetParent(container.transform, false);
                    point.transform.position = spawn + new Vector3(i * 3f - 1.5f, 0f, 0f);

                    MultiplayerSpawnPoint marker = point.AddComponent<MultiplayerSpawnPoint>();
                    marker.order = i;
                }
            }
        }
        else
        {
            Debug.LogWarning("[联机改造] " + scenePath + " 里没有找到单机 Player。");
        }

        // ---------- 敌人 ----------
        Enemy[] enemies = Object.FindObjectsOfType<Enemy>(true);
        for (int i = 0; i < enemies.Length; i++)
        {
            ConfigureEnemy(enemies[i]);
        }

        // ---------- 存档点 ----------
        CheckPiont[] checkpoints = Object.FindObjectsOfType<CheckPiont>(true);
        for (int i = 0; i < checkpoints.Length; i++)
        {
            GameObject go = checkpoints[i].gameObject;
            EnsureComponent<NetworkObject>(go);
            EnsureComponent<WorldObjectNetwork>(go);
        }

        // ---------- 场景里的道具 ----------
        ItemObject[] itemObjects = Object.FindObjectsOfType<ItemObject>(true);
        for (int i = 0; i < itemObjects.Length; i++)
        {
            GameObject go = itemObjects[i].gameObject;
            EnsureComponent<NetworkObject>(go);
            EnsureComponent<NetItemObject>(go);
        }

        // ---------- 丢失的灵魂 ----------
        LostCurrentcyController[] currencies = Object.FindObjectsOfType<LostCurrentcyController>(true);
        for (int i = 0; i < currencies.Length; i++)
        {
            GameObject go = currencies[i].gameObject;
            EnsureComponent<NetworkObject>(go);
            EnsureComponent<WorldObjectNetwork>(go);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[联机改造] 已处理场景 {scenePath}：敌人 {enemies.Length} 个，存档点 {checkpoints.Length} 个，" +
                  $"道具 {itemObjects.Length} 个。");
    }

    private static void ConfigureEnemy(Enemy enemy)
    {
        GameObject go = enemy.gameObject;

        EnsureComponent<NetworkObject>(go);

        NetworkTransform netTransform = EnsureComponent<NetworkTransform>(go);
        netTransform.Interpolate = true;
        netTransform.InLocalSpace = false;
        netTransform.SyncPositionX = true;
        netTransform.SyncPositionY = true;
        netTransform.SyncPositionZ = false;
        netTransform.SyncRotAngleX = false;
        netTransform.SyncRotAngleY = true;
        netTransform.SyncRotAngleZ = false;
        netTransform.SyncScaleX = false;
        netTransform.SyncScaleY = false;
        netTransform.SyncScaleZ = false;

        EnsureComponent<NetworkCharacterStats>(go);
        EnsureComponent<EnemyNetwork>(go);
    }

    // =================================================================
    // 主菜单场景
    // =================================================================

    private static void SetupMenuScene(GameObject itemObjectPrefab, GameObject lostCurrencyPrefab)
    {
        if (!System.IO.File.Exists(MenuScenePath))
        {
            Debug.LogWarning("[联机改造] 找不到主菜单场景：" + MenuScenePath);
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);

        NetBootstrap bootstrap = Object.FindObjectOfType<NetBootstrap>();
        if (bootstrap == null)
        {
            GameObject go = new GameObject("NetBootstrap");
            bootstrap = go.AddComponent<NetBootstrap>();
        }

        // 把需要联网同步的预制体登记进列表
        SerializedObject so = new SerializedObject(bootstrap);
        SerializedProperty list = so.FindProperty("extraNetworkPrefabs");
        if (list != null)
        {
            List<GameObject> wanted = new List<GameObject>();
            if (itemObjectPrefab != null) wanted.Add(itemObjectPrefab);
            if (lostCurrencyPrefab != null) wanted.Add(lostCurrencyPrefab);

            list.arraySize = wanted.Count;
            for (int i = 0; i < wanted.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = wanted[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 让 NetBootstrap 指向玩家预制体
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (playerPrefab != null)
        {
            SerializedObject bootstrapSo = new SerializedObject(bootstrap);
            SerializedProperty playerProp = bootstrapSo.FindProperty("playerPrefab");
            if (playerProp != null)
            {
                playerProp.objectReferenceValue = playerPrefab;
                bootstrapSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[联机改造] 已配置主菜单场景，NetBootstrap 就绪。");
    }

    // =================================================================
    // 工具方法
    // =================================================================

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        T existing = go.GetComponent<T>();
        if (existing != null) return existing;

        return go.AddComponent<T>();
    }
}
#endif
