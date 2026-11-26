using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crystal_Skill_Controller : MonoBehaviour
{
    private Player player;
    private Animator anim=>GetComponent<Animator>();
    private CircleCollider2D cd => GetComponent<CircleCollider2D>();

    private float crystalExistTimer;
    private bool canExplode;
    private bool canMove;
    private bool canMoveToEnemy;
    private float moveSpeed;

    private bool canGrow;
    private float growSpeed=5;

    private Transform closestTarget;
    [SerializeField] private LayerMask whatIsEnemy;

    public void SetupCrystal( float _crystalDuration,bool _canExplod,bool _canMove,float _moveSpeed,Transform _closestTarget,Player _player)
    {
        player = _player;
        crystalExistTimer = _crystalDuration;
        canExplode = _canExplod;
        canMove = _canMove;
        moveSpeed = _moveSpeed;
        closestTarget = _closestTarget;
    }
    public void ChooseRandomEnemy()
    {
        float radius=SkillManager.instance.blackhole.GetBlackholeRadious();

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, radius,whatIsEnemy);

        if ((colliders.Length>0))
        {
            closestTarget = colliders[Random.Range(0, colliders.Length)].transform;
        }
        else
        {

        }


    }




    private void Update()
    {
        crystalExistTimer -= Time.deltaTime;
        if (crystalExistTimer < 0)
        {
            FinishCrystal();
        }

        if (canMove)
        {

            if (closestTarget == null) 
            {
                return;
            }


            transform.position=Vector2.MoveTowards(transform.position, closestTarget.position, moveSpeed * Time.deltaTime);
            if (Vector2.Distance(transform.position, closestTarget.position) < 1f)
            {
                FinishCrystal();
                canMove = false;
            }
             
        }
        

        if (canGrow)
        {
            transform.localScale = Vector2.Lerp(transform.localScale, new Vector2(3,3), Time.deltaTime * growSpeed);
        }


    }
    private void AnimationExplodeEvent()
    {
        if (NetSkillFx.IsVisualOnly(gameObject)) { return; }
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position,cd.radius ); 
        foreach (var hit in colliders)
        {
            if (hit.GetComponent<Enemy>() != null)
            {
                hit.GetComponent<Entity>().SetupKnockbackDir(transform);
                player.stats.DoMagicalDamage(hit.GetComponent<CharacterStats>());

                ItemData_Equipment equipedAmult=Inventory.instance.GetEquipment(EquipmentType.Amulet);
                if (equipedAmult != null)
                {
                    equipedAmult.Effect(hit.transform);
                }

            }
        }
    }


    public void FinishCrystal()
    {
        if (canExplode)
        {
            canGrow = true;
            anim.SetTrigger("Explode");
        }
        else
        {
            SelfDestroy();
        }
    }

    public void SelfDestroy() => Destroy(gameObject);

}
