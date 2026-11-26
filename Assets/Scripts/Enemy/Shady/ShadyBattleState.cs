using System;
using UnityEngine;
namespace Assets.Scripts.Enemy.Shady
{
    public class ShadyBattleState : EnemyState
    {
        private Transform player;
        private Enemy_Shady enemy;
        private int moveDir;
        public float defaultSpeed;

        public ShadyBattleState(global::Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Shady _enemy) : base(_enemyBase, _stateMachine, _animBoolName)
        {
            this.enemy = _enemy;
        }

        public override void Enter()
        {
            base.Enter();

            defaultSpeed = enemy.moveSpeed;
            enemy.moveSpeed = enemy.battleStateMoveSpeed;

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
                    enemy.stats.KillEntity();//触发爆炸后可以获取金钱
                    enemy.stats.currentHealth = 0;
                    //怪物爆炸后会把身边的实体炸飞
                }

            }
            else
            {
                if (stateTimer < 0 || Vector2.Distance(player.transform.position, enemy.transform.position) >= 7)
                {
                    stateMachine.ChangeState(enemy.idleState);
                }
            }

            if (player.position.x > enemy.transform.position.x)
            {
                moveDir = 1;
            }
            else if (player.position.x < enemy.transform.position.x)
            {
                moveDir = -1;
            }

            enemy.SetVelocity(enemy.moveSpeed * moveDir, rb.velocity.y);

        }
        public override void Exit()
        {
            base.Exit();
            enemy.moveSpeed = defaultSpeed;
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


    }
}