using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGroundState : PlayerState
{
    public PlayerGroundState(PlayerStateMachine _stateMachine, Player _player, string animBoolName) : base(_stateMachine, _player, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
    }

    public override void Exit() 
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        if (Input.GetKeyDown(KeyCode.R)&&SkillManager.instance.blackhole.blackHoleUnlocked)
        {
            if (player.skill.blackhole.cooldown > 0)
            {
                player.fx.CreatePopUpText("ººƒ‹¿‰»¥");
            }
            


            player.stateMachine.ChangeState(player.blackHoleState);
        }

        if (Input.GetKeyDown(KeyCode.Mouse1) && HasNoSword()&&player.skill.sword.swordUnlocked)
        {
            player.rb.velocity = Vector2.zero;
            player.stateMachine.ChangeState(player.aimSword);
        }

        if (Input.GetKeyDown(KeyCode.Q) && player.skill.parry.parryUnLocked)
        {
            player.stateMachine.ChangeState(player.counterAttack);
        }

        if (!player.IsGroundDetected())
        {
            player.stateMachine.ChangeState(player.airState);
        }

        if(Input.GetKeyDown(KeyCode.J)||Input.GetKeyDown(KeyCode.Mouse0))
            stateMachine.ChangeState(player.primaryAttack);

        if (Input.GetKeyDown(KeyCode.Space)&&player.IsGroundDetected())
        {
            player.stateMachine.ChangeState(player.jumpState);
        }

    }

    private bool HasNoSword()
    {
        if(!player.sword)
        {
            return true;
        }
        player.sword.GetComponent<Sword_Skill_Controller>().ReturnSword(); 
        return false;
    }
}
