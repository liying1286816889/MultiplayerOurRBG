
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

//一个怪物需要的脚本
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(EnemyStats))]
[RequireComponent(typeof(EntityFX))]
[RequireComponent(typeof(ItemDrop))]

public class Enemy : Entity
{
    
    [SerializeField]protected LayerMask whatIsPlayer;
    [Header("眩晕信息")]
    public float stunDuration=1f;
    public Vector2 stunDirection=new Vector2(10,12);
    protected bool canBeStunned;
    [SerializeField] protected GameObject counterImage;

    [Header("敌人移动信息")]
    public float moveSpeed = 1.5f; //移动速度
    public float idleTime = 2f;
    public float moveTime = 1f;
    public float battleTime=7f;
    public float defaultSpeed = 12f;

    [Header("敌人战斗信息")]
    public float agroDistance=2f;
    public float attackDistance=2f;
    public float attackCooldown;
    public float minAttackCooldown=1;
    public float maxAttackCooldown=2;

    [HideInInspector] public float lastTimeAttacked;

    public EnemyStateMachine stateMachine { get; private set; }
    private GameObject child1;
    private GameObject child2;

    public EntityFX fx { get; private set; }
    public string lastAnimBoolName { get;private set; }


    protected override void Awake()
    {
        base.Awake();
        this.isEnemy = true;
        stateMachine = new EnemyStateMachine();

        defaultSpeed = moveSpeed;
        var sprites = counterImage.GetComponentsInChildren<SpriteRenderer>();
        if (sprites.Length >= 2)
        {
            child1 = sprites[0].gameObject;
            child2 = sprites[1].gameObject;

        }
        else
        {
            child1 = null;
            child2 = null;
        }
        child1.GetComponent<SpriteRenderer>().color = new Color(255, 0, 0, 0);
        child2.GetComponent<SpriteRenderer>().color = new Color(255, 0, 0, 0);


    }

    protected override void Start()
    {
        base.Start();
        fx = GetComponent<EntityFX>();
    }

    protected override void Update()
    {
        base.Update();

        // 联机时敌人 AI 只在服务器模拟，客户端只接收同步结果
        if (!ShouldSimulate)
        {
            return;
        }

        stateMachine.currentState.Update(); 
    }
    public virtual void AssignLastAnimName(string _animBoolName)=>lastAnimBoolName = _animBoolName;

    public override void SlowEntityBy(float _slowPercentage, float _slowDuration)
    {

        moveSpeed = moveSpeed * (1 - _slowPercentage);
        anim.speed = anim.speed * (1 - _slowPercentage);

        Invoke("ReturnDefaultSpeed", _slowDuration);
    }

    protected override void ReturnDefaultSpeed()
    {
        base.ReturnDefaultSpeed();
        moveSpeed =defaultSpeed;

    }

    public virtual void  AnimationFinishTrigger()=>stateMachine.currentState.AnimationFinishTrigger();
    public virtual void AnimationSpecialAttackTrigger()
    {

    }


    public virtual RaycastHit2D IsPlayerDetected()=>Physics2D.Raycast(wallCheck.position, Vector2.right * facingDir, 50, whatIsPlayer);


    // 渐显
    public void colorRedEyeEnhance()
    {
        if (child1 != null) StartCoroutine(FadeIn(child1));
        if (child2 != null) StartCoroutine(FadeIn(child2));
    }

    // 渐隐
    public void colorRedEyeFead()
    {
        if (child1 != null) StartCoroutine(FadeOut(child1));
        if (child2 != null) StartCoroutine(FadeOut(child2));
    }

    private IEnumerator FadeIn(GameObject obj)
    {
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;
        Color color = sr.color;
        float duration = 0.2f; // 渐变时间
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, timer / duration);
            sr.color = color;
            yield return null;
        }
        color.a = 1f;
        sr.color = color;
    }

    private IEnumerator FadeOut(GameObject obj)
    {
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;
        Color color = sr.color;
        float duration = 0.2f; // 渐变时间
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            color.a = Mathf.Lerp(1f, 0f, timer / duration);
            sr.color = color;
            yield return null;
        }
        color.a = 0f;
        sr.color = color;
    }

    public virtual void FreezeTime(bool _timeFrozen)
    {
        if (_timeFrozen)
        {
            moveSpeed = 0;
            anim.speed = 0;
        }
        else
        {
            anim.speed = 1;
            moveSpeed = defaultSpeed;
        }
    }

    public virtual void FreezeTimeFor(float _seconds)
    {
        StartCoroutine(FreezeTimerCoroutine(_seconds));
        fx.ChillFxUse(_seconds);

    }
    protected virtual IEnumerator FreezeTimerCoroutine(float _seconds)
    {
        FreezeTime(true);
        yield return new WaitForSeconds(_seconds);
        FreezeTime(false);

    }


    #region Counter Attack Window
    public virtual void OpenCounterAttackWindow()
    {
        canBeStunned = true;
        counterImage.SetActive(true);
        colorRedEyeEnhance();

    }
    public virtual void CloseCounterAttackWindow()
    {
        canBeStunned = false;
        counterImage?.SetActive(false);
        colorRedEyeFead();
        
    }
    #endregion

    public virtual bool CanBeStunned()
    {
        if (canBeStunned)
        {
            CloseCounterAttackWindow();
            return true;
        }
        return false;
    }




    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(wallCheck.position, new Vector3(transform.position.x + attackDistance*facingDir, transform.position.y));
    }
}

