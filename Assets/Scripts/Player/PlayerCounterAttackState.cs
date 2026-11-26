using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCounterAttackState : PlayerState
{
    private bool canCloneCreated;

    public PlayerCounterAttackState(PlayerStateMachine _stateMachine, Player _player, string animBoolName) : base(_stateMachine, _player, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        canCloneCreated = true;
        stateTimer =player.counterAttackDuration;
        player.anim.SetBool("SuccessfulCounterAttack",false);
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();
        player.SetZeroVelocity();

        Collider2D[] colliders = Physics2D.OverlapCircleAll(player.attackCheck.position, player.attackCheckRadius);
        foreach (var hit in colliders)
        {
            if (hit.GetComponent<Arrow_Controller>() != null)
            {
                hit.GetComponent<Arrow_Controller>().FlipArrow();
                SuccefullCounterAttrack();
            }


            if (hit.GetComponent<Enemy>() != null)
            {
                if(hit.GetComponent<Enemy>().CanBeStunned())
                {
                    SuccefullCounterAttrack();

                    player.skill.parry.UseSkill();//用于反击恢复时

                    if (canCloneCreated)
                    {
                        canCloneCreated = false;

                        player.skill.parry.MakeMirageOnParry(hit.transform);

                    }

                }
            }

        }
        if(stateTimer<0||triggerCalled)
        {
            stateMachine.ChangeState(player.idleState);
            
        }



    }

    private void SuccefullCounterAttrack()
    {
        stateTimer = 10;
        player.anim.SetBool("SuccessfulCounterAttack", true);
    }
}
