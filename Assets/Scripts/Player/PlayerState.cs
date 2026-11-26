using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerState //: MonoBehaviour
{
    protected PlayerStateMachine stateMachine;
    protected Player player;
    protected Rigidbody2D rb;


    //x方向偏移量
    protected float xInput;
    //y方向偏移量
    protected float yInput;

    //动画播放
    private string animBoolName;
    protected float stateTimer;
    protected bool triggerCalled;


    public PlayerState(PlayerStateMachine _stateMachine, Player _player, string animBoolName)
    {
        this.stateMachine = _stateMachine;
        this.player = _player;
        this.animBoolName = animBoolName;
    }
    //状态函数
    public virtual void Enter()
    {
        player.anim.SetBool(animBoolName, true);
        rb=player.rb;
        triggerCalled = false;

        // 联机：告诉其它客户端现在播的是哪个状态
        player.NotifyStateEntered(animBoolName);
    }
    //更新函数
    public virtual void Update()
    {
        stateTimer -= Time.deltaTime;
        xInput = Input.GetAxisRaw("Horizontal");
        yInput=Input.GetAxisRaw("Vertical");
        player.anim.SetFloat("yVelocity", rb.velocity.y);
    }

    //退出函数
    public virtual void Exit()
    {
        player.anim.SetBool(animBoolName, false);

    }

    public virtual void AnimationFinishTrigger()
    {
        triggerCalled = true;
    }


}
