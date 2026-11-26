using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationTriggers : MonoBehaviour
{
    private Player player=>GetComponentInParent<Player>();

    private void AnimationTrigger()
    {
        player.AnimationTrigger();
    }
    private void AttackTrigger()
    {
        // 远程玩家的副本不结算伤害
        if (player == null || !player.IsLocalControlled)
        {
            return;
        }

        AudioManager.instance.PlaySFX(2);

        Collider2D[] colliders=Physics2D.OverlapCircleAll(player.attackCheck.position, player.attackCheckRadius);
        foreach(var hit in colliders)
        {
            if(hit.GetComponent<Enemy>() != null)
            {
                EnemyStats _target = hit.GetComponent<EnemyStats>();
                if (_target != null)
                {
                    player.stats.DoDamage(_target);
                }
                ItemData_Equipment weaponData = Inventory.instance.GetEquipment(EquipmentType.Weapon);

                if (weaponData != null)
                {
                    weaponData.Effect(_target.transform);
                }
            }
        }
    }

    


    private void ThrowSword()
    {
        if (player == null || !player.IsLocalControlled)
        {
            return;
        }

        if (SkillManager.instance == null)
        {
            return;
        }

        SkillManager.instance.sword.CreateSword();

    }

}
 