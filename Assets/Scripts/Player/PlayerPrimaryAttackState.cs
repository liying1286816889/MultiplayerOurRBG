using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPrimaryAttackState : PlayerState
{
    public int comboCounter { get; private set; }

    private float lastTimeAttacked;
    private float comboWindow = 2;

    public PlayerPrimaryAttackState(PlayerStateMachine _stateMachine, Player _player, string animBoolName) : base(_stateMachine, _player, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        //AudioManager.instance.PlayRandomPitchSFX(2);
        //AudioManager.instance.PlaySFX(2);

        xInput = 0;
        if (comboCounter > 2||Time.time>=lastTimeAttacked+comboWindow)
        {
            comboCounter = 0;
        }
        player.anim.SetInteger("ComboCounter", comboCounter);
        player.anim.speed = player.attackSpeed;
        #region 在攻击时选择方向
        float attackDir=player.facingDir;
        if (xInput != 0)
        {
            attackDir = xInput;
        }
        

        #endregion
        player.SetVelocity(player.attackMovement[comboCounter].x  * attackDir, player.attackMovement[comboCounter].y);
        stateTimer = 0.1f;
    }

    public override void Exit()
    {
        base.Exit();
        player.StartCoroutine("BusyFor",0.1f);
        player.anim.speed = 1f;
        comboCounter++;
        lastTimeAttacked = Time.time;
    }

    public override void Update()
    {
        base.Update();
        if (stateTimer < 0)
        {
            player.SetZeroVelocity();
        }


        if (triggerCalled)
        {
            stateMachine.ChangeState(player.idleState);
        }
    }
}
