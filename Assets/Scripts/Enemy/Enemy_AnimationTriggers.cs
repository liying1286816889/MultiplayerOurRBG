using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_AnimationTriggers : MonoBehaviour
{

    private Enemy enemy=>GetComponentInParent<Enemy>();

    private void AnimationTrigger()
    {
        enemy.AnimationFinishTrigger();
    }

    private void AttackTrigger()
    {
        // 动画事件在每个客户端都会触发，联机时只让服务器真正结算伤害
        if (NetGame.IsOnline && !NetGame.IsServer)
        {
            return;
        }

        Collider2D[] colliders = Physics2D.OverlapCircleAll(enemy.attackCheck.position, enemy.attackCheckRadius);
        foreach (var hit in colliders)
        {
            if (hit.GetComponent<Player>() != null)
            {
                PlayerStats _target = hit.GetComponent<PlayerStats>();
                enemy.stats.DoDamage(_target);

            }

        }
    }

    //比如精灵射手的箭矢攻击
    private void SpeicalAttackTrigger()
    {
        enemy.AnimationSpecialAttackTrigger();
    }

    private void OpenCounterWindow()=>enemy.OpenCounterAttackWindow();
    private  void CloseCounterWindow() => enemy.CloseCounterAttackWindow();
}
