using UnityEngine;

/// <summary>需要同步给队友的技能特效类型。</summary>
public enum SkillFxKind
{
    Sword = 0,
    Blackhole = 1,
    Crystal = 2,
    Clone = 3
}

/// <summary>
/// 标记：这个对象只是"表现副本"，不参与任何伤害/判定结算。
/// 由 <see cref="NetSkillFx"/> 在远端客户端生成时挂上。
/// </summary>
public class SkillFxVisualOnly : MonoBehaviour
{
}

/// <summary>
/// 技能特效的双端可见。
///
/// 模型（信任客户端）：
/// - 施法者在自己机器上照旧本地生成并模拟技能物件（原有逻辑一行没改）；
/// - 生成完调用 <see cref="Broadcast"/>，把「什么技能 / 在哪 / 朝向 / 几个参数」发给队友；
/// - 队友本地实例化同一个预制体，并挂上 <see cref="SkillFxVisualOnly"/>：
///   外观、飞行、动画都一样，但不会结算伤害。
/// - 伤害仍然只由施法者通过 <see cref="NetCombat"/> 提交给服务器。
///
/// 这样一来队友能看见你的剑 / 黑洞 / 水晶 / 分身，而数值不会翻倍。
/// </summary>
public static class NetSkillFx
{
    /// <summary>这个对象是否只是表现副本（不该结算伤害）。</summary>
    public static bool IsVisualOnly(GameObject go)
    {
        return go != null && go.GetComponent<SkillFxVisualOnly>() != null;
    }

    public static void MarkVisualOnly(GameObject go)
    {
        if (go == null) return;
        if (go.GetComponent<SkillFxVisualOnly>() == null)
        {
            go.AddComponent<SkillFxVisualOnly>();
        }
    }

    /// <summary>
    /// 施法者调用：把刚生成的技能广播给其它客户端。
    /// 单机模式下直接返回，没有任何副作用。
    /// </summary>
    public static void Broadcast(SkillFxKind kind, Vector3 position, Vector3 dir,
        float p1 = 0f, float p2 = 0f, float p3 = 0f)
    {
        if (!NetGame.IsOnline) return;

        PlayerNetwork local = NetGame.LocalPlayer;
        if (local == null || !local.IsSpawned || !local.IsOwner) return;

        local.BroadcastSkillFx(kind, position, dir, p1, p2, p3);
    }

    /// <summary>
    /// 队友收到广播后，在本地重建一个纯表现副本。
    /// </summary>
    public static void PlayVisual(SkillFxKind kind, Player caster, Vector3 position, Vector3 dir,
        float p1, float p2, float p3)
    {
        if (caster == null) return;

        GameObject prefab = ResolvePrefab(kind);
        if (prefab == null) return;

        switch (kind)
        {
            case SkillFxKind.Sword:
                CreateSword(prefab, caster, position, dir, p1, p2, p3);
                break;

            case SkillFxKind.Blackhole:
                CreateBlackhole(prefab, position, p1, p2, p3);
                break;

            case SkillFxKind.Crystal:
                CreateCrystal(prefab, caster, position, p1, p2, p3);
                break;

            case SkillFxKind.Clone:
                CreateClone(prefab, caster, position, p1, p2);
                break;
        }
    }

    // ------------------------------------------------------------------
    // 预制体解析：直接复用技能脚本上已经配好的预制体引用
    // ------------------------------------------------------------------

    private static GameObject ResolvePrefab(SkillFxKind kind)
    {
        SkillManager manager = SkillManager.instance;
        if (manager == null) return null;

        switch (kind)
        {
            case SkillFxKind.Sword:
                return manager.sword != null ? manager.sword.FxPrefab : null;

            case SkillFxKind.Blackhole:
                return manager.blackhole != null ? manager.blackhole.FxPrefab : null;

            case SkillFxKind.Crystal:
                return manager.crystal != null ? manager.crystal.FxPrefab : null;

            case SkillFxKind.Clone:
                return manager.clone != null ? manager.clone.FxPrefab : null;
        }

        return null;
    }

    // ------------------------------------------------------------------
    // 各类特效
    // ------------------------------------------------------------------

    /// <summary>p1=重力缩放 p2=回收速度 p3=旋转持续时间(0 表示不是旋转剑)</summary>
    private static void CreateSword(GameObject prefab, Player caster, Vector3 position,
        Vector3 dir, float p1, float p2, float p3)
    {
        GameObject go = Object.Instantiate(prefab, position, Quaternion.identity);
        MarkVisualOnly(go);

        Sword_Skill_Controller controller = go.GetComponent<Sword_Skill_Controller>();
        if (controller == null) return;

        if (p3 > 0f)
        {
            // 旋转剑：队友这边也转起来
            controller.SetupSpin(true, 12f, p3, 0.25f);
        }

        controller.SetupSword(dir, p1, caster, 0f, p2, 0f);
    }

    /// <summary>p1=最大半径 p2=生长速度 p3=持续时间</summary>
    private static void CreateBlackhole(GameObject prefab, Vector3 position,
        float p1, float p2, float p3)
    {
        GameObject go = Object.Instantiate(prefab, position, Quaternion.identity);
        MarkVisualOnly(go);

        Blackhole_Skill_Controller controller = go.GetComponent<Blackhole_Skill_Controller>();
        if (controller == null) return;

        // amountOfAttacks 传 0：队友这边只在旁边看着，不重复生成分身/水晶
        controller.SetupBlackhole(p1, p2, p2, 0, 0f, p3);
    }

    /// <summary>p1=存在时间 p2=追踪速度 p3=能力位掩码(1 爆炸 / 2 追踪)</summary>
    private static void CreateCrystal(GameObject prefab, Player caster, Vector3 position,
        float p1, float p2, float p3)
    {
        GameObject go = Object.Instantiate(prefab, position, Quaternion.identity);
        MarkVisualOnly(go);

        Crystal_Skill_Controller controller = go.GetComponent<Crystal_Skill_Controller>();
        if (controller == null) return;

        int flags = Mathf.RoundToInt(p3);
        bool canExplode = (flags & 1) != 0;
        bool canMove = (flags & 2) != 0;

        controller.SetupCrystal(p1, canExplode, canMove, p2, null, caster);
    }

    /// <summary>p1=存在时间 p2=攻击倍率</summary>
    private static void CreateClone(GameObject prefab, Player caster, Vector3 position,
        float p1, float p2)
    {
        GameObject go = Object.Instantiate(prefab, position, Quaternion.identity);
        MarkVisualOnly(go);

        Clone_Skill_Controller controller = go.GetComponent<Clone_Skill_Controller>();
        if (controller == null) return;

        // 已经摆到正确位置了，偏移量传 0；队友这边不做"复制分身"判定
        controller.SetupClone(go.transform, p1, true, Vector3.zero, null, false, 0f, caster, p2);
    }
}
