using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ThunderStrike_Control : MonoBehaviour
{
    
    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<Enemy>() != null)
        {
            Player localPlayer = PlayerManager.instance != null ? PlayerManager.instance.PlayerOrDefault : null;
            if (localPlayer == null)
            {
                return;
            }

            PlayerStats playerStats = localPlayer.GetComponent<PlayerStats>();
            EnemyStats enemyTarget = collision.GetComponent<EnemyStats>();
            playerStats.DoMagicalDamage(enemyTarget);
        }
    }

}
