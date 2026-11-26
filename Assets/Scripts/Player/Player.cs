 using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Player : Entity
{

    [Header("玩家攻击信息")]
    public Vector2[] attackMovement;
    [Header("玩家反击时间")]
    public float counterAttackDuration=0.2f; 

    public bool isBusy { get; private set; }
    [Header("玩家攻击信息")]
    public float attackSpeed = 1f;//攻击速度
    
    [Header("玩家移动信息")]
    public float moveSpeed = 12f; //移动速度
    public float jumpForce = 12f; //跳跃速度
    public float swordReturnImpact = 4f;
    private float defaultMoveSpeed;
    private float defaultJumpForce;


    [Header("玩家冲刺信息")]
    public float dashSpeed;
    public float dashDuration;
    private float defaultDashSpeed;



   

    public float dashDir { get; private set; }

    public bool isBleed;

    public SkillManager skill { get; private set; } //技能管理器
    public GameObject sword { get; private set; }
    public PlayerFX fx { get; private set; }


    #region 玩家状态
    //玩家状态机 
    public PlayerStateMachine stateMachine { get; private set; }
    //玩家移动状态机
    public PlayerMoveState moveState { get; private set; }
    //玩家闲置状态机
    public PlayerIdleState idleState { get; private set; }
    //玩家跳跃状态
    public PlayerJumpState jumpState { get; private set; }
    //玩家在空中状态
    public PlayerAirState airState { get; private set; }
    //玩家冲刺状态
    public PlayerDashState dashState { get; private set; }
    //玩家爬墙状态
    public PlayerWallSlideState wallSlide { get; private set; }
    //玩家爬墙跳跃状态
    public PlayerWallJumpState wallJump { get; private set; }
    //玩家攻击状态
    public PlayerPrimaryAttackState primaryAttack { get; private set; }
    //玩家反击状态
    public PlayerCounterAttackState counterAttack { get; private set; }
    //玩家用剑瞄准状态
    public PlayerAimSwordState aimSword { get; private set; }
    //玩家回收剑
    public PlayerCatchSwordState catchSword { get; private set; }
    //玩家释放黑洞
    public PlayerBlackholeState blackHoleState { get; private set; }
    //玩家死亡
    public PlayerDeadState deadState { get; private set; }

    #endregion

    //实例化对象
    protected override void Awake()
    {
        base.Awake();
        this.isEnemy = false;
        Net = GetComponent<PlayerNetwork>();
        stateMachine = new PlayerStateMachine();
        idleState = new PlayerIdleState(stateMachine, this, "Idle");
        moveState = new PlayerMoveState(stateMachine, this, "Move");
        jumpState = new PlayerJumpState(stateMachine, this, "Jump");
        airState  = new PlayerAirState(stateMachine, this, "Jump");
        dashState = new PlayerDashState(stateMachine, this, "Dash");
        wallSlide = new PlayerWallSlideState(stateMachine, this, "WallSlide");
        wallJump  = new PlayerWallJumpState(stateMachine, this, "Jump");
        primaryAttack = new PlayerPrimaryAttackState(stateMachine, this, "Attack");
        counterAttack = new PlayerCounterAttackState(stateMachine, this, "CounterAttack");
        aimSword = new PlayerAimSwordState(stateMachine, this, "AimSword");
        catchSword = new PlayerCatchSwordState(stateMachine, this, "CatchSword");
        blackHoleState = new PlayerBlackholeState(stateMachine, this, "Jump");
        deadState = new PlayerDeadState(stateMachine, this, "Die");
    }
    //启动函数,初始化函数
    protected override void Start()
    {
        base.Start();


        stateMachine.Initialize(idleState);
        skill = SkillManager.instance;
        fx = GetComponent<PlayerFX>();

        defaultMoveSpeed = moveSpeed;
        defaultJumpForce = jumpForce;
        defaultDashSpeed = dashSpeed;
    }

    //更新函数
    protected override void Update()
    {
        // 联机时别人控制的玩家副本不处理输入、不跑状态机
        if (!ShouldSimulate)
        {
            return;
        }

        if (Time.timeScale == 0)
        {
            return;
        }

        base.Update();
        stateMachine.currentState.Update();


        CheckForDashInput();

        if (Input.GetKeyDown(KeyCode.F)&&skill.crystal.crystalUnlocked)
        {
            skill.crystal.CanUseSkill();
        }

        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            Inventory.instance.UseFlask();
        }

        // 把动画 / 朝向等表现数据同步给其它客户端
        PushNetworkSnapshot();

    }

    public override void SlowEntityBy(float _slowPercentage, float _slowDuration)
    {
        moveSpeed = moveSpeed * (1 - _slowPercentage);
        jumpForce = jumpForce * (1 - _slowPercentage);
        dashSpeed = dashSpeed * (1 - _slowPercentage);
        anim.speed = anim.speed * (1 - _slowPercentage);

        Invoke("ReturnDefaultSpeed", _slowDuration);
    }

    protected override void ReturnDefaultSpeed()
    {
        base.ReturnDefaultSpeed();
        moveSpeed = defaultMoveSpeed;
        jumpForce = defaultJumpForce;
        dashSpeed = defaultDashSpeed;

    }


    public void AssignNewSword(GameObject _newSword)
    {
        sword = _newSword;

    }
    public void CatchTheSword()
    {
        stateMachine.ChangeState(catchSword);
        Destroy(sword);
    }

       

    public IEnumerator BusyFor(float _seconds)
    {
        isBusy = true;
        yield return new WaitForSeconds(_seconds);
        isBusy = false;
    }


    public void AnimationTrigger()=>stateMachine.currentState.AnimationFinishTrigger();


    #region 联机支持

    /// <summary>本机的 <see cref="PlayerNetwork"/>（单机时为 null）。</summary>
    public PlayerNetwork Net { get; private set; }

    /// <summary>本机是否在控制这个角色。单机模式恒为 true。</summary>
    public bool IsLocalControlled { get; private set; } = true;

    public void SetLocalControlled(bool _local)
    {
        IsLocalControlled = _local;
    }

    /// <summary>被别人控制时的初始化：去掉所有只属于“自己”的表现。</summary>
    public void SetUpAsRemoteCopy()
    {
        // 远程副本不需要本地的瞄准点、技能准星等对象
        Transform aimDotParent = transform.Find("AimDotParent");
        if (aimDotParent != null)
        {
            aimDotParent.gameObject.SetActive(false);
        }
    }

    /// <summary>状态机切状态时通知网络层，让其它客户端也能播同样的动画。</summary>
    public void NotifyStateEntered(string _animBoolName)
    {
        if (Net != null && Net.IsOwner)
        {
            Net.PushAnimState(_animBoolName);
        }
    }

    /// <summary>把动画相关的表现数据推给其它客户端。</summary>
    public void PushNetworkSnapshot()
    {
        if (Net == null || !Net.IsOwner) return;
        if (anim == null) return;

        int combo = primaryAttack != null ? primaryAttack.comboCounter : 0;
        Net.PushAnimatorSnapshot(rb != null ? rb.velocity.y : 0f, combo, anim.speed,
            anim.GetBool("SuccessfulCounterAttack"), facingDir > 0);
    }

    #endregion


    //检查有没有进入冲刺状态
    private void CheckForDashInput()
    {
        if (IsWallDetected()&&!IsGroundDetected())
            return;

        if(skill.dash.dashUnlocked==false)return;


        if (Input.GetKeyDown(KeyCode.LeftShift)&&SkillManager.instance.dash.CanUseSkill())
        {
            dashDir =Input.GetAxisRaw("Horizontal");

            if (dashDir == 0)
                dashDir = facingDir;

            stateMachine.ChangeState(dashState);
        }

    }
    public override void Die()
    {
        base.Die();
        stateMachine.ChangeState(deadState);
    }

    protected override void SetupZeroKnockbackPower()
    {
        knockbackPower = new Vector2(0, 0);

    }

}
