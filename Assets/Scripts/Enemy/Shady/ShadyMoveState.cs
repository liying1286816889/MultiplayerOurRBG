using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Enemy.Shady
{
    public class ShadyMoveState : ShadyGroundState
    {
        public ShadyMoveState(global::Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Shady _enemy) : base(_enemyBase, _stateMachine, _animBoolName, _enemy)
        {
        }

        public override void Enter()
        {
            base.Enter();
            stateTimer = enemy.moveTime;
        }

        public override void Exit()
        {
            base.Exit();
        }

        public override void Update()
        {
            base.Update();
            enemy.SetVelocity(enemy.moveSpeed * enemy.facingDir, rb.velocity.y);

            if (enemy.IsWallDetected() || !enemy.IsGroundDetected() || stateTimer < 0)
            {
                enemy.Flip();
                stateMachine.ChangeState(enemy.idleState);
            }

        }

    }
}