using System;
using UnityEngine;

public class ArcherBattleState : EnemyState
{
    private Transform player;
    private Enemy_Archer enemy;
    private int moveDir;
    private float maxDirattacktoplayer = 0.88f;
    private float minDirattacktoplayer = 0.3f;

    public ArcherBattleState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Archer _enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = _enemy;
    }

    public override void Enter()
    {
        base.Enter();
        player = EnemyTargeting.Resolve(enemyBase != null ? (Vector2)enemyBase.transform.position : Vector2.zero, player);

        if (player == null)
        {
            return;
        }

        if (player.GetComponent<PlayerStats>().isDead)
        {
            stateMachine.ChangeState(enemy.moveState);
        }

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
        double distance = Math.Abs(enemy.IsPlayerDetected().distance - enemy.attackDistance);

        if (enemy.IsPlayerDetected())
        {
            stateTimer = enemy.battleTime;

            if (enemy.IsPlayerDetected().distance < enemy.attackDistance)
            {
                if (CanJump())
                {
                    stateMachine.ChangeState(enemy.jumpState);
                }
            }

            if (distance <= maxDirattacktoplayer && distance >= minDirattacktoplayer)
            {
                if (CanAttack())
                {
                    stateMachine.ChangeState(enemy.attackState);
                    enemy.SetVelocity(0, rb.velocity.y);
                }
                else
                {
                    enemy.SetVelocity(0, rb.velocity.y);
                }
            }

        }
        else
        {
            if (stateTimer < 0 || Vector2.Distance(player.transform.position, enemy.transform.position) >= 7)
            {
                stateMachine.ChangeState(enemy.idleState);
            }
        }

        BattleStateFlipControl();

    }

    private void BattleStateFlipControl()
    {
        if (player.position.x > enemy.transform.position.x && enemy.facingDir == -1)
        {
            enemy.Flip();
        }
        else if (player.position.x < enemy.transform.position.x && enemy.facingDir == 1)
        {
            enemy.Flip();
        }
    }

    public override void Exit()
    {
        base.Exit();
    }
    private bool CanAttack()
    {
        if (Time.time >= enemy.lastTimeAttacked + enemy.attackCooldown)
        {
            enemy.attackCooldown = UnityEngine.Random.Range(enemy.minAttackCooldown, enemy.maxAttackCooldown);
            enemy.lastTimeAttacked = Time.time;
            return true;
        }
        return false;
    }

    private bool CanJump()
    {
        if (enemy.GroundBehindCheck() == false||enemy.WallBehindCheck()==true)
        {
            return false;
        }

        if (Time.time >= enemy.lastJumpTime + enemy.jumpCooldown)
        {
            enemy.lastJumpTime = Time.time;
            return true;
        }
        return false;
    }
}
