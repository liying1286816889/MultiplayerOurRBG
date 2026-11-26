using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Mathematics;
using UnityEngine;
using UnityEngineInternal;

public class Clone_Skill_Controller : MonoBehaviour
{
    private Player player;
    private SpriteRenderer sr;
    private Animator anim;
    [SerializeField] private float colorLoosingSpeed;
    private float cloneTimer;
    private float attackMultiplier;
    [SerializeField] private Transform attackCheck;
    [SerializeField] private float attackCheckRadius=1.08f;
    private Transform closestEnemy;
    private bool canDuplicateClone;
    private int facingDir = 1;

    private float chanceToDuplicate;

    private void Awake()
    {
        sr=GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        cloneTimer -= Time.deltaTime;

        if (cloneTimer < 0)
        {
            sr.color = new Color(1,1,1,sr.color.a-(Time.deltaTime*colorLoosingSpeed));
            
        }
        if (sr.color.a <= 0)
        {
            Destroy(gameObject);
        }

    }

    public void SetupClone(Transform _newTransform,float _cloneDuration,bool _canAttack,Vector3 _offset,Transform _closesEnemy,bool _canDuplicateClone, float _chanceToDuplicate,Player _player,float _attackMultiplier)
    {
        if (_canAttack)
        {
            anim.SetInteger("AttackNumber",UnityEngine.Random.Range(1,4));
        }
        attackMultiplier = _attackMultiplier;
        player=_player;
        transform.position = _newTransform.position+_offset;
        cloneTimer = _cloneDuration;

        
        closestEnemy = _closesEnemy;
        canDuplicateClone = _canDuplicateClone;
        chanceToDuplicate=_chanceToDuplicate;
        FaceClosestTarge();

    }

    private void AnimationTrigger()
    {
        cloneTimer = -0.1f;
    }
    private void AttackTrigger()
    {
        if (NetSkillFx.IsVisualOnly(gameObject)) { return; }
        Collider2D[] colliders = Physics2D.OverlapCircleAll(attackCheck.position, attackCheckRadius); 
        foreach (var hit in colliders)
        {
            if (hit.GetComponent<Enemy>() != null)
            {

                hit.GetComponent<Entity>().SetupKnockbackDir(transform);
                PlayerStats playerStats=player.GetComponent<PlayerStats>();
                EnemyStats enemyStats=hit.GetComponent<EnemyStats>();
                playerStats.CloneDoDamage(enemyStats, attackMultiplier);
                if (player.skill.clone.canApplyOnHitEffect)
                {
                    ItemData_Equipment weaponData = Inventory.instance.GetEquipment(EquipmentType.Weapon);

                    if (weaponData != null)
                    {
                        weaponData.Effect(hit .transform);
                    }
                }



                if (canDuplicateClone)
                {
                    if(UnityEngine.Random.Range(0,100)<chanceToDuplicate)
                    {
                        //GameObject newTransform = new GameObject();
                        //newTransform.transform.position = new Vector2(hit.transform.position.x, PlayerManager.instance.player.transform.position.y);

                        SkillManager.instance.clone.CreateClone(hit.transform, new Vector3(.5f*facingDir,0));
                    }
                }


            }
        }
    }

    private void FaceClosestTarge()
    {
        
        if (closestEnemy != null)
        {
            if(transform.position.x > closestEnemy.position.x)
            {
                facingDir=-1;
                transform.Rotate(0, 180, 0);
            }
            

        }

    }


}
