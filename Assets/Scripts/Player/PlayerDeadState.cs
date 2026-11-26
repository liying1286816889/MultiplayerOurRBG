using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDeadState : PlayerState
{
    public PlayerDeadState(PlayerStateMachine _stateMachine, Player _player, string animBoolName) : base(_stateMachine, _player, animBoolName)
    {
    }

    public override void AnimationFinishTrigger()
    {
        base.AnimationFinishTrigger();
    }

    public override void Enter()
    {
        base.Enter();

        // 联机时别人死亡不应该弹出我的死亡结算界面
        if (NetGame.IsOnline && !player.IsLocalControlled)
        {
            return;
        }

        GameObject canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            UI ui = canvas.GetComponent<UI>();
            if (ui != null)
            {
                ui.SwitchOnEndScreen();
            }
        }
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();
        player.SetZeroVelocity();
    }
}
