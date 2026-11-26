using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAimSwordState : PlayerState
{
    public PlayerAimSwordState(PlayerStateMachine _stateMachine, Player _player, string animBoolName) : base(_stateMachine, _player, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        player.skill.sword.DotsActive(true);

    }

    public override void Exit()
    {
        base.Exit();
        player.StartCoroutine("BusyFor", .2f);
    }

    public override void Update()
    {
        base.Update();
        rb.velocity = new Vector2(0 , rb.velocity.y);

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            player.stateMachine.ChangeState(player.idleState);
        }
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //角色方向随着剑的方向旋转
        if (player.transform.position.x > mousePosition.x && player.facingDir== 1)
        {
            player.Flip();
        }
        else if (player.transform.position.x < mousePosition.x && player.facingDir == -1)
        {
            player.Flip();
        }

    }
}
