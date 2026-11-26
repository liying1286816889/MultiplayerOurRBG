using System.Collections;
using UnityEngine;

public class Entity : MonoBehaviour
{
    [Header("碰撞信息")]
    public Transform attackCheck;
    public float attackCheckRadius=1.2f;

    [SerializeField] protected Transform groundCheck;
    [SerializeField] protected float groundCheckDistance=1;
    [SerializeField] protected Transform wallCheck;
    [SerializeField] protected float wallCheckDistance=0.8f;
    [SerializeField] protected LayerMask whatIsGround;

    #region 组件
    //动画机组件，在子对象中调用
    public Animator anim { get; private set; }
    public Rigidbody2D rb { get; private set; }
    public SpriteRenderer sr { get; private set; }
    public CharacterStats stats { get; private set; }
    public CapsuleCollider2D cd { get; private set; }

    protected bool isEnemy;


    #endregion
    [Header("攻击击退信息")]
    [SerializeField]protected Vector2 knockbackPower=new Vector2(7,12);
    [SerializeField] protected Vector2 knockbackOffset=new Vector2(.5f,2);
    [SerializeField] protected float knockbackDuration=0.7f;
    protected bool isKnocked;

    public int knockbackDir { get; private set; } = 1;
    public int facingDir { get; private set; } = 1;
    protected bool facingRight = true;

    public System.Action OnFlipped;


    protected virtual void Awake()
    {

    }
    protected virtual void Start()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
        
        stats = GetComponent<CharacterStats>();
        cd=GetComponent<CapsuleCollider2D>();

    }
    protected virtual void Update()
    {

    }

    public virtual void SlowEntityBy(float _slowPercentage,float _slowDuration)
    {

    }

    protected virtual void ReturnDefaultSpeed()
    {
        anim.speed = 1;
    }


    #region 碰撞
    //地面检测
    public virtual bool IsGroundDetected() => Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, whatIsGround);
    //墙面检测 
    public virtual bool IsWallDetected() => Physics2D.Raycast(wallCheck.position, Vector2.right * facingDir, wallCheckDistance, whatIsGround);

    protected virtual void OnDrawGizmos()
    {
        Gizmos.DrawLine(groundCheck.position, new Vector3(groundCheck.position.x, groundCheck.position.y - groundCheckDistance));
        Gizmos.DrawLine(wallCheck.position, new Vector3(wallCheck.position.x + wallCheckDistance*facingDir, wallCheck.position.y));
        Gizmos.DrawWireSphere(attackCheck.position, attackCheckRadius);
    }
    public virtual void DamageImpact()=>StartCoroutine("HitKnockback");

    public virtual void SetupKnockbackDir(Transform _damageDirection)
    {
        if (_damageDirection.position.x > transform.position.x)
        {
            knockbackDir = -1;
        }else if(_damageDirection.position.x < transform.position.x)
        {
            knockbackDir = 1; 
        }

    }
    
    public virtual void SetupKnockbackPower(Vector2 _knockbackPower)
    {
        knockbackPower = _knockbackPower;
    }

    protected virtual IEnumerator HitKnockback()
    {
        isKnocked = true;

        float xOffset = Random.Range(knockbackOffset.x, knockbackOffset.y);

        //注释这一行可以使玩家免于受击僵直
        if (knockbackPower.x > 0||knockbackPower.y>0)
        {
            rb.velocity = new Vector2((knockbackPower.x+xOffset) * -facingDir, knockbackPower.y);
        }
        yield return new WaitForSeconds(knockbackDuration);
        isKnocked = false;

        SetupZeroKnockbackPower();
    }

    protected virtual void SetupZeroKnockbackPower()
    {

    }


    #endregion

    #region 翻转
    //翻转函数
    public virtual void Flip()
    {
        facingDir *= -1;
        facingRight = !facingRight;
        transform.Rotate(0, 180, 0);
        if (OnFlipped != null)
        {
            OnFlipped();
        }
    }
    //调用翻转函数
    public virtual void FlipController(float _x)
    {
        if (_x > 0 && !facingRight)
        {
            Flip();
        }
        else if (_x < 0 && facingRight)
        {
            Flip();
        }

    }

    public virtual void SetupDefailtFacingDir(int _direction)
    {
        facingDir = _direction;
        if (facingDir == -1)
        {
            facingRight = false;
        }

    }


    #endregion
    #region 速度
    public void SetZeroVelocity()
    {
        if (isKnocked)
        {
            return;
        }
        rb.velocity = new Vector2(0, 0);
    }
    //设置运动速度的函数
    public void SetVelocity(float _xvelocity, float _yvelocity)
    {
        if (isKnocked)
        {
            return;
        }

        rb.velocity = new Vector2(_xvelocity, _yvelocity);
        FlipController(_xvelocity);
    }
    #endregion
    

    public virtual void Die()
    {

    }


    #region 联机支持
    /// <summary>
    /// 本机是否应该模拟这个角色。
    /// 单机模式下恒为 true；联机时远程玩家的副本、以及客户端上的敌人返回 false，
    /// 它们的位移与动画全部由网络同步驱动，避免“两端各算一套”。
    /// </summary>
    public virtual bool ShouldSimulate => NetGame.CanSimulate(this);
    #endregion


}
