using System;
using UnityEngine;

public class SlimeGroundState : EnemyState
{
    protected Transform player;
    protected Enemy_Slime enemy;
    public SlimeGroundState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName,Enemy_Slime _enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = _enemy;
    }

    public override void Enter()
    {
        base.Enter();
        player = EnemyTargeting.Resolve(enemyBase != null ? (Vector2)enemyBase.transform.position : Vector2.zero, player);
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        // 每帧重新解析目标：玩家可能新增/死亡/被销毁，
        // 缓存的旧 Transform 一旦失效就会抛 MissingReferenceException。
        player = EnemyTargeting.Resolve(enemyBase != null ? (Vector2)enemyBase.transform.position : Vector2.zero, player);

        if (player == null)
        {
            return;
        }
        if (enemy.IsPlayerDetected() || Vector2.Distance(enemy.transform.position, player.transform.position) <= enemy.agroDistance)
        {
            stateMachine.ChangeState(enemy.battleState);
        }

    }


}
