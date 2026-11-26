using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStats : CharacterStats
{
    private Enemy enemy ;
    private ItemDrop myDropSystem;
    public Stats soulsDropAmount;

    [Header("Level details")]
    [SerializeField]private int level = 1;

    [Header("等级成长率")]
    [SerializeField] private float levelGrowth=0.1f;
    [SerializeField] private int maxLevelGrowth = 10;
    [SerializeField] private int fixedGrowth = 3;

    protected override void Start()
    {
        soulsDropAmount.SetDefaultValue(100);
        ApplyLevelModifiers();

        base.Start();
        enemy = GetComponent<Enemy>();

        myDropSystem = GetComponent<ItemDrop>();

    }

    private void ApplyLevelModifiers()
    {
        Modify(strengths);
        Modify(vitality);
        Modify(agility);
        Modify(intelligence);
        

        Modify(damage);
        Modify(critChance);
        Modify(critPower);

        Modify(maxHealth);
        Modify(armor);
        Modify(evasion);
        Modify(magicResistance);

        Modify(fireDamage);
        Modify(iceDamage);
        Modify(lightningDamage);

        Modify(soulsDropAmount);
    }

    private void Modify(Stats _stats)
    {
        for (int i = 1; i < level; i++)
        {
            if (i >= maxLevelGrowth)
            {
                _stats.AddModifier(fixedGrowth);
                break;
            }
            float modifier = _stats.GetValue() * levelGrowth;
            _stats.AddModifier(Mathf.RoundToInt(modifier));
        }


    }


    public override void TakeDamage(int _damage)
    {
        base.TakeDamage(_damage);
        //enemy.DamageEffect();
    }
    protected override void Die()
    {
        base.Die();
        enemy.Die();

        if (!NetGame.IsOnline)
        {
            PlayerManager.instance.currency += soulsDropAmount.GetValue();
            myDropSystem.GenerateDrop();
            Destroy(gameObject, 5f);
            return;
        }

        // 联机：灵魂发给最后一击的玩家，销毁交给 EnemyNetwork。
        // 掉落物改为【本地掉落】：服务器不生成，各客户端在收到死亡同步后自己生成。
        if (NetGame.IsServer)
        {
            EnemyNetwork.AwardKillSouls(this);
        }

        // 房主模式：本机既是服务器又是客户端，也要生成自己那份掉落
        if (NetGame.IsClient)
        {
            GenerateLocalDrop();
        }
    }

    /// <summary>联机时客户端从服务器收到“这个敌人死了”。</summary>
    protected override void OnRemoteDeath()
    {
        base.OnRemoteDeath();

        if (enemy != null)
        {
            enemy.Die();
        }

        // 纯客户端：各自生成自己的掉落物
        if (!NetGame.IsServer)
        {
            GenerateLocalDrop();
        }
    }

    private void GenerateLocalDrop()
    {
        if (myDropSystem != null)
        {
            myDropSystem.GenerateDrop();
        }
    }

}
