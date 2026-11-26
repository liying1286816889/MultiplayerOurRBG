using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Archer : Enemy
{
    [Header("精灵射手特殊信息")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private float arrowSpeed;
    [SerializeField] private float arrowDamage;

    public Vector2 jumpVelocity;
    public float jumpCooldown;
    public float safeDistance;//玩家靠近触发距离
    [HideInInspector] public float lastJumpTime;
    [Header("其他碰撞检测")]
    [SerializeField] private Transform groundBehindCheck;
    [SerializeField] private Vector2 groundBehindCheckSize;
    


    #region 精灵射手状态
    public ArcherIdleState idleState { get; private set; }
    public ArcherMoveState moveState { get; private set; }
    public ArcherBattleState battleState { get; private set; }
    public ArcherAttackState attackState { get; private set; }
    public ArcherStunnedState stunnedState { get; private set; }
    public ArcherDeadState deadState { get; private set; }
    public ArcherJumpState jumpState { get; private set; }

    #endregion
    protected override void Awake()
    {
        base.Awake();
        idleState = new ArcherIdleState(this, stateMachine, "Idle", this);
        moveState = new ArcherMoveState(this, stateMachine, "Move", this);
        battleState = new ArcherBattleState(this, stateMachine, "Idle", this);
        attackState = new ArcherAttackState(this, stateMachine, "Attack", this);
        stunnedState = new ArcherStunnedState(this, stateMachine, "Stunned", this);
        deadState = new ArcherDeadState(this, stateMachine, "Idle", this);
        jumpState=new ArcherJumpState(this,stateMachine,"Jump",this);
    }
    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
    }


    public override bool CanBeStunned()
    {
        if (base.CanBeStunned())
        {
            stateMachine.ChangeState(stunnedState);
            return true;
        }
        return false;
    }
    public override void Die()
    {
        base.Die();
        stateMachine.ChangeState(deadState);
    }

    public override void AnimationSpecialAttackTrigger()
    {
        base.AnimationSpecialAttackTrigger();
        Vector3 newposition = attackCheck.position;
        

        GameObject newArrow = Instantiate(arrowPrefab, newposition, Quaternion.identity);
        GameObject newArrow1 = Instantiate(arrowPrefab, newposition + new Vector3(0, 0.5f, 0), Quaternion.identity);
        GameObject newArrow2 = Instantiate(arrowPrefab, newposition + new Vector3(0, -0.5f, 0), Quaternion.identity);


        newArrow.GetComponent<Arrow_Controller>().SetupArrow(arrowSpeed  *  facingDir,stats);
        newArrow1.GetComponent<Arrow_Controller>().SetupArrow(arrowSpeed * facingDir, stats, arrowSpeed * facingDir*Mathf.Tan(Mathf.PI/6));
        newArrow2.GetComponent<Arrow_Controller>().SetupArrow(arrowSpeed * facingDir, stats, -arrowSpeed * facingDir * Mathf.Tan(Mathf.PI / 6));
    }

    public bool GroundBehindCheck() => Physics2D.BoxCast(groundBehindCheck.position, groundBehindCheckSize, 0, Vector2.zero, 0, whatIsGround);
    public bool WallBehindCheck()=> Physics2D.Raycast(wallCheck.position, Vector2.right*-facingDir, wallCheckDistance+2, whatIsGround);

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        Gizmos.DrawWireCube(groundBehindCheck.position, groundBehindCheckSize); 
    }
}
