using Unity.Netcode.Components;

/// <summary>
/// 由「拥有者客户端」控制的 Transform 同步。
///
/// NGO 自带的 <see cref="NetworkTransform"/> 默认是服务器权威，
/// 但本项目的玩家位移是由各自的客户端跑 Rigidbody2D 物理算出来的，
/// 所以玩家必须用拥有者权威；敌人则继续用默认的服务器权威。
///
/// 用法：把这个组件挂在玩家预制体上，替代普通 NetworkTransform。
/// </summary>
public class OwnerNetworkTransform : NetworkTransform
{
    /// <summary>返回 false 表示“拥有者客户端才是权威”。</summary>
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
