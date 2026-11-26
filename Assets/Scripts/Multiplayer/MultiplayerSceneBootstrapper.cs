using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 联机会话开始时，把场景里“单机版”的对象关掉。
///
/// 原因：游戏场景（MainScene / MainPass1）里原本就摆着一个 Player，
/// 联机时玩家是由 NetworkManager 根据预制体生成的，
/// 如果不管它就会出现“场景里一个 + 网络生成一个”的双胞胎。
///
/// 单机模式下这里什么都不做，保证原版玩法完全不受影响。
/// </summary>
public static class MultiplayerSceneBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        DisableSinglePlayerObjects();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        MultiplayerSpawnPoints.Invalidate();
        DisableSinglePlayerObjects();
    }

    private static void DisableSinglePlayerObjects()
    {
        // 注意这里同时判断 SessionRequested：
        // 房主是「先在本地载入关卡 → 再 StartHost」，
        // 载入关卡那一刻 NetGame.IsOnline 还是 false，必须靠 SessionRequested 才知道要关掉场景里的玩家。
        if (!NetGame.IsOnline && !NetBootstrap.SessionRequested) return;

        // 场景里不是网络对象的 Player 一律视为“单机版的玩家”
        Player[] players = Object.FindObjectsOfType<Player>(true);
        for (int i = 0; i < players.Length; i++)
        {
            Player p = players[i];
            if (p == null) continue;
            if (p.GetComponent<NetworkObject>() != null) continue;

            p.gameObject.SetActive(false);
        }

        // 敌人交给 EnemyNetwork 自己处理；这里只记录日志方便排查
        Enemy[] enemies = Object.FindObjectsOfType<Enemy>(true);
        int notNetworked = 0;
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null && enemies[i].GetComponent<NetworkObject>() == null)
            {
                notNetworked++;
            }
        }

        if (notNetworked > 0)
        {
            Debug.LogWarning(
                $"[联机] 场景里有 {notNetworked} 个敌人还不是网络对象。" +
                "请执行菜单 Tools/联机改造/一键配置联机场景 来修复。");
        }
    }
}
