using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Skill : MonoBehaviour
{
    [SerializeField]public float cooldown;
    protected float cooldownTimer;

    /// <summary>
    /// 技能的使用者，永远是本地玩家。
    /// 延迟解析：联机时玩家对象是连上服务器之后才生成的。
    /// </summary>
    protected Player player
    {
        get
        {
            if (PlayerManager.instance == null) return null;
            return PlayerManager.instance.PlayerOrDefault;
        }
    }

    protected virtual void Start()
    {
        CheckUnlock();
    }

    protected virtual void Update()
    {
        cooldownTimer -= Time.deltaTime;

    }

    protected virtual void CheckUnlock()
    {

    }

    public virtual bool CanUseSkill()
    {
        if (cooldownTimer <= 0)
        {
            //使用技能
            UseSkill(); 
            cooldownTimer = cooldown;
            return true;
        }
        if (player != null && player.fx != null)
        {
        player.fx.CreatePopUpText("技能冷却");
        }
        return false;
    }

    public virtual void UseSkill()
    {
        
    }

    protected virtual Transform FindClosesEnemy(Transform _checkTransform)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(_checkTransform.position, 25);
        float closestDistance = Mathf.Infinity;
        Transform closestEnemy = null;
        foreach (var hit in colliders)
        {
            if (hit.GetComponent<Enemy>() != null)
            {
                float distanceToEnemy = Vector2.Distance(_checkTransform.position, hit.transform.position);
                if (distanceToEnemy < closestDistance)
                {
                    closestDistance = distanceToEnemy;
                    closestEnemy = hit.transform;
                }

            }

        }
        return closestEnemy; 
    } 

}
