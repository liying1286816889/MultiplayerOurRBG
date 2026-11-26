using UnityEngine;
using System;


public class ExplociveController : MonoBehaviour
{

    private Animator anim;
    private CharacterStats myStats;
    private float growSpeed=15;
    private float maxSize=1.5f;
    private float explosionRadius;

    private Vector2 explosionVelocity = new Vector2(10, 10);
    private bool canGrow = true;
    private void Update()
    {
        if (canGrow)
        {
            transform.localScale = Vector2.Lerp(transform.localScale, new Vector2(maxSize, maxSize), growSpeed * Time.deltaTime);
        }
        if(maxSize-transform.localScale.x < 0.5f)
        {
            canGrow = false;
            anim.SetTrigger("Explode");
        }

    }



    public void SetupExplocive(CharacterStats _myStats,float _growSpeed,float _maxSize,float _radius,Vector2 _explosionVelocity)
    {
        anim = GetComponent<Animator>();
        myStats = _myStats;
        growSpeed = _growSpeed;
        maxSize = _maxSize;
        explosionRadius = _radius;
        explosionVelocity = _explosionVelocity;
    }


    private void AnimationExplodeEvent()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (var hit in colliders)
        {
            if (hit.GetComponent<CharacterStats>() != null)
            {
                hit.GetComponent<Entity>().SetupKnockbackDir(transform);
                myStats.DoDamage(hit.GetComponent<CharacterStats>());
                StrikeEntity(hit);
            }
        }
    }

    private void StrikeEntity(Collider2D hit)
    {
        Rigidbody2D rb = hit.GetComponent<Rigidbody2D>();
        rb.velocity = Vector2.zero;
        Vector2 dirToTarget = (hit.transform.position - transform.position).normalized;
        rb.AddForce(new Vector2(dirToTarget.x*explosionVelocity.x, explosionVelocity.y), ForceMode2D.Impulse);
    }

    private void SelfDestroy() => Destroy(gameObject);
}
