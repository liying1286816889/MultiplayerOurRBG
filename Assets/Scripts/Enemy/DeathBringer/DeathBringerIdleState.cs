using System.Collections;
using UnityEngine;


public class DeathBringerIdleState : EnemyState
{
    private Enemy_DeathBringer enemy;
    private Transform player;
    public DeathBringerIdleState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_DeathBringer _enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = _enemy;
    }

    public override void Enter()
    {
        base.Enter();
        stateTimer = enemy.idleTime;
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
        if (Vector2.Distance(player.transform.position, enemy.transform.position)<7)
        {
            enemy.bossFightBegun = true;
        }



        if(Input.GetKeyDown(KeyCode.M))
        {
            stateMachine.ChangeState(enemy.teleportState);
        }
        if (stateTimer < 0 && enemy.bossFightBegun)
        {
            stateMachine.ChangeState(enemy.battleState);
        }


    }
}