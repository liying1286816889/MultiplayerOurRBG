using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAirState : PlayerState
{
    public PlayerAirState(PlayerStateMachine _stateMachine, Player _player, string animBoolName) : base(_stateMachine, _player, animBoolName)
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
        if (player.IsWallDetected())
        {
            player.stateMachine.ChangeState(player.wallSlide);
            if (xInput != 0 && xInput * player.facingDir < 0)
            {
                player.stateMachine.ChangeState(player.idleState);
            }

        }
        

        if (player.IsGroundDetected())
        {
            player.stateMachine.ChangeState(player.idleState);
        }
        if(xInput != 0)
        {
            player.SetVelocity(player.moveSpeed*0.8f*xInput,rb.velocity.y);
        }
    }
}
