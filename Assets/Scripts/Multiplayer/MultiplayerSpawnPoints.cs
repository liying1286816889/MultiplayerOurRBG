using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 显式的出生点标记。在场景里放几个带有这个组件的空物体即可，
/// <see cref="MultiplayerSpawnPoints"/> 会按 <see cref="order"/> 顺序分配。
/// 没有放置时，会自动用「场景里原本的 Player 位置 + 间距」兜底。
/// </summary>
[DisallowMultipleComponent]
public class MultiplayerSpawnPoint : MonoBehaviour
{
    [Tooltip("越小越先被使用")]
    public int order = 0;
}

/// <summary>
/// 出生点 / 复活点解析工具。
/// </summary>
public static class MultiplayerSpawnPoints
{
    private static readonly List<MultiplayerSpawnPoint> cachedPoints = new List<MultiplayerSpawnPoint>();
    private static bool cacheValid;

    /// <summary>清除缓存（切场景后调用）。</summary>
    public static void Invalidate()
    {
        cachedPoints.Clear();
        cacheValid = false;
    }

    private static void EnsureCache()
    {
        if (cacheValid) return;

        cachedPoints.Clear();
        MultiplayerSpawnPoint[] points = Object.FindObjectsOfType<MultiplayerSpawnPoint>(true);
        if (points != null)
        {
            for (int i = 0; i < points.Length; i++)
            {
                if (points[i] != null) cachedPoints.Add(points[i]);
            }
        }

        cachedPoints.Sort((a, b) => a.order.CompareTo(b.order));
        cacheValid = true;
    }

    /// <summary>取某个客户端索引对应的出生点。</summary>
    public static Vector3 GetPosition(ulong clientId, float fallbackSpacing)
    {
        EnsureCache();

        int index = (int)clientId;

        if (cachedPoints.Count > 0)
        {
            int slot = index % cachedPoints.Count;
            return cachedPoints[slot].transform.position;
        }

        return GetFallbackPosition(index, fallbackSpacing);
    }

    /// <summary>取复活点：优先最近的已激活存档点，否则退回出生点。</summary>
    public static Vector3 GetRespawnPosition(ulong clientId, float fallbackSpacing)
    {
        Vector3 fallback = GetPosition(clientId, fallbackSpacing);

        CheckPiont[] checkpoints = Object.FindObjectsOfType<CheckPiont>();
        if (checkpoints == null || checkpoints.Length == 0) return fallback;

        CheckPiont best = null;
        float bestDistance = float.PositiveInfinity;

        Vector3 reference = fallback;
        if (PlayerManager.instance != null && PlayerManager.instance.player != null)
        {
            reference = PlayerManager.instance.player.transform.position;
        }

        for (int i = 0; i < checkpoints.Length; i++)
        {
            CheckPiont cp = checkpoints[i];
            if (cp == null || !cp.activationStatus) continue;

            float distance = Vector2.Distance(reference, cp.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = cp;
            }
        }

        return best != null ? best.transform.position : fallback;
    }

    /// <summary>
    /// 兜底出生点：用场景里名为 "Player" 的对象（联机时被禁用的那个）作为参考，
    /// 按索引向两侧排开，避免两个玩家叠在一起。
    /// </summary>
    private static Vector3 GetFallbackPosition(int index, float spacing)
    {
        GameModeAnchor anchor = GameModeAnchor.Instance;

        if (anchor != null)
        {
            float offset = (index - 0.5f) * Mathf.Max(0.5f, spacing);
            return anchor.transform.position + new Vector3(offset, 0f, 0f);
        }

        GameObject scenePlayer = GameObject.Find("Player");
        if (scenePlayer != null)
        {
            float offset = (index - 0.5f) * Mathf.Max(0.5f, spacing);
            return scenePlayer.transform.position + new Vector3(offset, 0f, 0f);
        }

        return Vector3.zero;
    }
}

/// <summary>
/// 记录「单机模式下的玩家出生位置」。
/// 因为联机时场景里原来的 Player 会被禁用，需要一个占位物体保存它的位置。
/// 由 Editor 一键配置脚本自动创建。
/// </summary>
[DisallowMultipleComponent]
public class GameModeAnchor : MonoBehaviour
{
    public static GameModeAnchor Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
