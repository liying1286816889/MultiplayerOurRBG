using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCatchSwordState : PlayerState
{
    private Transform sword;
    public PlayerCatchSwordState(PlayerStateMachine _stateMachine, Player _player, string animBoolName) : base(_stateMachine, _player, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        sword = player.sword.transform;
        player.fx.PlayDustFX();
        player.fx.ScreenShake(player.fx.shakeSwordImpact);

        //人方向随着剑的方向回收转动
        if (player.transform.position.x > sword.position.x && player.facingDir == 1)
        {
            player.Flip();
        }
        else if (player.transform.position.x < sword.position.x && player.facingDir == -1)
        {
            player.Flip();
        }

         rb.velocity= new  Vector2(player.swordReturnImpact *-player.facingDir, rb.velocity.y);
    }

    public override void Exit()
    {
        base.Exit();
        player.StartCoroutine("BusyFor", .2f);

    }

    public override void Update()
    {
        base.Update();
        if (triggerCalled)
        {
           stateMachine.ChangeState(player.idleState);
        }
    }
}
