using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : CharacterStats
{
    private Player player;

    protected override void Start()
    {
        base.Start();
        player = GetComponent<Player>();
    }
    public override void TakeDamage(int _damage)
    {
        base.TakeDamage(_damage);
        player.DamageImpact();
    }
    protected override void Die()
    {
        base.Die();
        player.Die();

        if (!NetGame.IsOnline)
        {
            GameManager.instance.lostCurrencyAmount = PlayerManager.instance.currency;
            PlayerManager.instance.currency = 0;

            GetComponent<PlayerItemDrop>()?.GenerateDrop();
            return;
        }

        // 联机：钱包是每个人自己的，只有本机玩家才处理掉魂与死亡界面
        HandleLocalDeathIfOwner();
    }

    private bool localDeathHandled;

    /// <summary>联机时客户端从服务器收到“我死了”。</summary>
    protected override void OnRemoteDeath()
    {
        base.OnRemoteDeath();

        if (player != null)
        {
            player.Die();
        }

        HandleLocalDeathIfOwner();
    }

    private void HandleLocalDeathIfOwner()
    {
        if (localDeathHandled) return;

        PlayerNetwork pn = GetComponent<PlayerNetwork>();
        if (pn == null || !pn.IsOwner) return;

        localDeathHandled = true;
        pn.HandleLocalDeath();
    }

    /// <summary>复活之后重置本地的死亡标记。</summary>
    public void ResetLocalDeathFlag()
    {
        localDeathHandled = false;
    }
    protected override void DecreaseHealthBy(int _damage)
    {
        base.DecreaseHealthBy(_damage);

        if (_damage > GetMaxHealthValue() * .3f)
        {
            player.SetupKnockbackPower(new Vector2(10,6));
            player.fx.ScreenShake(player.fx.shakeHighDamage);
            int randomIndex = Random.Range(34, 36);
            AudioManager.instance.PlaySFX(randomIndex);
            player.isBleed = true;
            if (PlayerManager.instance.player.isBleed)
            {
                PlayerManager.instance.player.fx.ClearFX();
                PlayerManager.instance.player.fx.BleedFxUse(1f);
            }
            PlayerManager.instance.player.isBleed = false;
        }


        ItemData_Equipment currentArmor = Inventory.instance.GetEquipment(EquipmentType.Armor);
        if(currentArmor!=null)
        {
            currentArmor.Effect(player.transform);
        }

    }

    public override void OnEvasion()
    {
        base.OnEvasion();

        player.skill.dodge.CreateMirageOnDodge();
    }

    public void CloneDoDamage(CharacterStats _targetStats,float _multipler)
    {
        if (TargetCanAvoidAttack(_targetStats))
        {
            return;
        }
        int totalDamage = damage.GetValue() + strengths.GetValue();
        if (_multipler > 0) 
        {
            totalDamage= Mathf.RoundToInt(totalDamage*_multipler);
        }


        if (CanCrit())
        {
            totalDamage = CalculateCriticalDamage(totalDamage);
        }

        totalDamage = CheckTargeArmor(_targetStats, totalDamage);
        _targetStats.TakeDamage(totalDamage);

        //如果物理攻击包含魔法伤害
        DoMagicalDamage(_targetStats);
    }


}
