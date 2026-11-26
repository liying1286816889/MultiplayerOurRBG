using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class Sword_Skill_Controller : MonoBehaviour
{
    private Animator anim;
    private Rigidbody2D rb;
    private CapsuleCollider2D cd;
    private Player player;
    private bool canRotate = true;
    private bool isReturning = false;




    [Header("贯穿剑信息")]
    private int pierceAmount;

    [Header("弹射剑信息")]
    private int bounceAmount;
    private float bounceSpeed;

    private bool isBouncing;
    private List<Transform> enemyTarget;
    private int targetIndex = 0;

    [Header("旋转剑信息")]
    private float maxTravelDistance;
    private float spinDuration;
    private float spinTimer;
    private bool wasStopped;
    private bool isSpinning;
    private float hitTimer;
    private float hitCooldown = 0.2f;
    private float spinDirection;


    [Header("普通信息")]
    private float freezeTimeDuration;
    
    private float vulnerableTimeDuration;

    private float returnSpeed;


        


private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
        cd = GetComponent<CapsuleCollider2D>();
    }

    private void DestroySword()
    {
        Destroy(gameObject);
    }

    public void SetupSword(Vector2 _dir,float _gravityScale,Player _player,float _freezeTimeDuration,float _returnSpeed,float _vulnerableTimeDuration)
    {
        returnSpeed = _returnSpeed;
        freezeTimeDuration =_freezeTimeDuration;
        rb.velocity = _dir;
        rb.gravityScale = _gravityScale;
        player = _player;
        vulnerableTimeDuration = _vulnerableTimeDuration;


        if (pierceAmount <= 0)
        {
            anim.SetBool("Rotation", true);
        }

        spinDirection=Mathf.Clamp(rb.velocity.x, -1, 1);


       Invoke("DestroySword", 7f);
    }

    public void SetupBounce(bool _isBouncing, int _amountOfBounces, float _bounceSpeed) 
    {
        isBouncing = _isBouncing;
        bounceAmount = _amountOfBounces;
        bounceSpeed = _bounceSpeed;


        enemyTarget = new List<Transform>();
    }

    public void SetupPierce(int _pierceAmount)
    {
        pierceAmount = _pierceAmount;


    }
    public void SetupSpin(bool _isSpinning,float _maxTravelDistance,float _spinDuration,float _hitCooldown)
    {
        isSpinning = _isSpinning;
        maxTravelDistance = _maxTravelDistance;
        spinDuration = _spinDuration;
        hitCooldown = _hitCooldown;
    }

    
    public void ReturnSword()
    {
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        transform.Rotate(0, 0, 180f);
        Vector2 dirToPlayer = (player.transform.position - transform.position).normalized;
        transform.right = dirToPlayer;
        //rb.isKinematic = false;
        transform.parent = null;
        isReturning = true;
        
        //剑的冷却结束要从回收剑开始记录

    }
     


    private void Update()
    {
        if (canRotate)
            transform.right = rb.velocity;
        if (isReturning)
        {
            // 每帧都让剑面向玩家当前位置
            Vector2 dirToPlayer = (player.transform.position - transform.position).normalized;
            transform.right = dirToPlayer;
            transform.position = Vector2.MoveTowards(transform.position, player.transform.position, returnSpeed * Time.deltaTime);
            if (Vector2.Distance(transform.position, player.transform.position) < 1)
            {
                if (NetSkillFx.IsVisualOnly(gameObject))
                {
                    // 队友这边的复制品：回收时自己消失即可，
                    // 不要调用 CatchTheSword（那会触发队友那边的屏幕抖动和状态切换）
                    Destroy(gameObject);
                }
                else
                {
                    player.CatchTheSword();
                }            }
        }

        BounceLogic();
        SpinLogic();
    }

    private void SpinLogic()
    {
        if (isSpinning)
        {
            if (Vector2.Distance(player.transform.position, transform.position) > maxTravelDistance && !wasStopped)
            {
                StopWhenSpin();
            }

            if (wasStopped)
            {
                spinTimer -= Time.deltaTime;

                transform.position=Vector2.MoveTowards(transform.position, new Vector2(transform.position.x+spinDirection,transform.position.y), 1.5f * Time.deltaTime); 

                if (spinTimer < 0)
                {
                    isReturning = true;
                    isSpinning = false;
                }

                hitTimer -= Time.deltaTime;
                if (hitTimer < 0)
                {
                    hitTimer=hitCooldown;

                    Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 1);
                    foreach (var hit in colliders)
                    {
                        if (hit.GetComponent<Enemy>() != null)
                        {
                            SwordSkillDamage(hit.GetComponent<Enemy>());
                        }
                    }

                }

            }

        }

        
    }
    private void StopWhenSpin()
    {
        wasStopped = true;
        rb.constraints = RigidbodyConstraints2D.FreezePosition;
        spinTimer = spinDuration;
    }
    private void BounceLogic()
    {

        if (isBouncing && enemyTarget.Count > 0)
        {

            transform.position = Vector2.MoveTowards(transform.position, enemyTarget[targetIndex].position, bounceSpeed * Time.deltaTime);

            if (Vector2.Distance(transform.position, enemyTarget[targetIndex].position) < 0.3f)
            {
                SwordSkillDamage(enemyTarget[targetIndex].GetComponent<Enemy>());

                targetIndex++;
                bounceAmount--;
                if (bounceAmount <= 0)
                {
                    isBouncing = false;
                    ReturnSword();
                    return;
                }


                if (targetIndex >= enemyTarget.Count)
                {
                    targetIndex = 0;
                }

            }
        }
    }

    private void SwordSkillDamage(Enemy enemy)
    {
        EnemyStats enemyStats=enemy.GetComponent<EnemyStats>();

        player.stats.DoDamage(enemyStats);

        if (SkillManager.instance.sword.timeStopUnlocked)
        {
            enemy.FreezeTimeFor(freezeTimeDuration);
        }

        if (SkillManager.instance.sword.vulnerableUnlocked)
        {
            enemyStats.MakeVulnerableFor(vulnerableTimeDuration);
        }


        ItemData_Equipment equipedAmult = Inventory.instance.GetEquipment(EquipmentType.Amulet);
        if (equipedAmult != null)
        {
            equipedAmult.Effect(enemy.transform);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (NetSkillFx.IsVisualOnly(gameObject)) { return; }  
        if(isReturning) 
            return;
        
        //if (collision.GetComponent<Enemy>() != null)
        //{
        //    Enemy enemy = collision.GetComponent<Enemy>();

        //    player.stats.DoDamage(enemy.GetComponent<CharacterStats>());

        //    //enemy.DamageEffect();
        //    enemy.StartCoroutine("FreezeTimerFor",freezeTimeDuration);
        //}

        if (collision.GetComponent<Enemy>() != null)
        {
            Enemy enemy = collision.GetComponent<Enemy>();

            player.stats.DoDamage(enemy.GetComponent<CharacterStats>());

            // 修改：直接调用 Enemy 的公开方法来处理冻结，而不是使用字符串 StartCoroutine
            enemy.FreezeTimeFor(freezeTimeDuration);
        }



        SetupTargetsForBounce(collision);
        StuckInto(collision);
    }

    private void SetupTargetsForBounce(Collider2D collision)
    {
        if (collision.GetComponent<Enemy>() != null)
        {
            if (isBouncing && enemyTarget.Count <= 0)
            {
                Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 10f);
                foreach (var hit in colliders)
                {
                    if (hit.GetComponent<Enemy>() != null)
                    {
                        enemyTarget.Add(hit.transform);
                    }
                }
            }

        }
    }

    void StuckInto(Collider2D collision)
    {
        if(pierceAmount > 0 && collision.GetComponent<Enemy>() != null)
        {
            pierceAmount--;
            return;
        }
        if(isSpinning)
        {
            StopWhenSpin();
            return;
        }


        canRotate = false;
        cd.enabled = false;

        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        //这是飞剑击中的特效
        //GetComponentInChildren<ParticleSystem>().Play();


        if (isBouncing&&enemyTarget.Count>0)
            return;

        anim.SetBool("Rotation", false);
        transform.parent = collision.transform;
    }

}
