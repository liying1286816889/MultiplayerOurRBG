using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDashState : PlayerState
{

    public PlayerDashState(PlayerStateMachine _stateMachine, Player _player, string animBoolName) : base(_stateMachine, _player, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        player.skill.dash.CloneOnDash();
        stateTimer = player.dashDuration;
        player.stats.MakeInvinciable(true);


    } 

    public override void Exit()
    {
        base.Exit();
        player.skill.dash.CloneOnArrival();
        player.SetVelocity(0, rb.velocity.y);
        player.stats.MakeInvinciable(false);
    }

    public override void Update()
    {
        base.Update();
        if (!player.IsGroundDetected()&&player.IsWallDetected())
        {
            stateMachine.ChangeState(player.wallSlide);
        }


        player.SetVelocity(player.dashSpeed * player.dashDir, 0);
        if (stateTimer<0)
            stateMachine.ChangeState(player.idleState);

        player.fx.CreateAfterImage();
    }
}
