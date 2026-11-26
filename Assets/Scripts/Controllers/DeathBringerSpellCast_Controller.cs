using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathBringerSpellCast_Controller : MonoBehaviour
{

    [SerializeField] private Transform check;
    [SerializeField] private Vector2 boxSize;
    [SerializeField] private LayerMask whatIsPlayer;

    private CharacterStats myStats;

    public void SetupSpellCast(CharacterStats _myStats)=>myStats = _myStats;
    

    private void AnimationTrigger()
    {
        Collider2D[] colliders = Physics2D.OverlapBoxAll(transform.position, boxSize, whatIsPlayer);
        foreach (var hit in colliders)
        {
            if (hit.GetComponent<Player>() != null)
            {
                hit.GetComponent<Entity>().SetupKnockbackDir(transform);
                myStats.DoDamage(hit.GetComponent<CharacterStats>());

            }
        }
    }

    private void OnDrawGizmos()=>Gizmos.DrawWireCube(check.position, boxSize);
    
    private void SelfDestroy() => Destroy(gameObject);

}
