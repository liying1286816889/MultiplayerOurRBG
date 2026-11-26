using UnityEngine;

/// <summary>
/// 敌人 AI 的“目标玩家”解析。
///
/// 改造前敌人都是直接拿 <c>PlayerManager.instance.player</c>（唯一的那个玩家）。
/// 联机之后服务器上同时存在两个玩家，敌人应该锁定最近的那一个。
/// 单机模式下行为和改造前完全一致。
/// </summary>
public static class EnemyTargeting
{
    /// <summary>
    /// 解析敌人应该盯着的目标。
    ///
    /// 重要：返回值可能为 null（比如联机时所有玩家都死了）。
    /// 调用方必须判空 —— 敌人的状态机每帧都会重新解析，
    /// 这样既不会盯着一个已经不存在的玩家，也不会在退场/切场景时炸出
    /// MissingReferenceException（Unity 的 == null 能识别"已销毁"对象）。
    /// </summary>
    /// <param name="from">敌人的位置</param>
    /// <param name="fallback">上一帧的目标；如果它已经被销毁会被自动丢弃</param>
    public static Transform Resolve(Vector2 from, Transform fallback = null)
    {
        if (NetGame.IsOnline)
        {
            // 服务器上有多个玩家时锁定最近的活人；一个活人都没有就返回 null
            Transform nearest = NetGame.FindClosestPlayer(from);
            if (nearest != null) return nearest;

            return null;
        }

        if (PlayerManager.instance != null && PlayerManager.instance.player != null)
        {
            return PlayerManager.instance.player.transform;
        }

        // fallback 若已被销毁，Unity 的 != null 会返回 false，这里就变成 null
        return fallback != null ? fallback : null;
    }

    /// <summary>目标是否还有效（活着、存在）。</summary>
    public static bool IsValid(Transform target)
    {
        if (target == null) return false;

        CharacterStats stats = target.GetComponent<CharacterStats>();
        if (stats != null && stats.isDead) return false;

        Player player = target.GetComponent<Player>();
        if (player != null) return true;

        return true;
    }
}
