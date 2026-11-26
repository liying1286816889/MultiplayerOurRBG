using System.Collections;
using UnityEngine;

public class DeathBringerTeleportState : EnemyState
{
    private Enemy_DeathBringer enemy;
    public DeathBringerTeleportState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_DeathBringer _enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = _enemy;
    }

    public override void Enter()
    {
        base.Enter();
        enemy.stats.MakeInvinciable(true);
    }

    public override void Exit()
    {
        base.Exit();
        enemy.stats.MakeInvinciable(false);
    } 

    public override void Update()
    {
        base.Update();
        if (triggerCalled)
        {
            if (enemy.CanTeleport())
            {
                stateMachine.ChangeState(enemy.spellCastState);
            }
            else
            {
                stateMachine.ChangeState(enemy.battleState);
            }

        }

    }
}
