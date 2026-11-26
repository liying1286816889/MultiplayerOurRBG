using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;

public class PlayerIdleState : PlayerGroundState
{
    public PlayerIdleState(PlayerStateMachine _stateMachine, Player _player, string animBoolName) : base(_stateMachine, _player, animBoolName)
    {

    }

    public override void Enter()
    {
        
        base.Enter();
        player.SetZeroVelocity();
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();
        //if (xInput == player.facingDir && player.IsWallDetected())
        //    return;

        if(xInput!=0&&!player.isBusy)
            player.stateMachine.ChangeState(player.moveState);
    }
}
