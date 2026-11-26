using System.Collections;
using System.Threading;
using UnityEngine;

public enum StatType
{
    strength,
    agility,
    intelligence,
    vitality,
    damage,
    critChance,
    critPower,
    health,
    armor,
    evasion,
    magicResistance,
    fireDamage,
    iceDamage,
    lightningDamage
}
public class CharacterStats : MonoBehaviour
{
    private EntityFX fx;

    /// <summary>联机血量同步组件（单机时为 null）。</summary>
    private NetworkCharacterStats netStats;

    [Header("主要属性")]
    //能力每增加一点上涨1%
    public Stats strengths;//攻击
    public Stats agility;//闪避，命中
    public Stats intelligence;//魔法伤害和魔力抵抗
    public Stats vitality;
    
    
    [Header("攻击属性")]
    public Stats damage;
    public Stats critChance;//暴击率
    public Stats critPower;//暴击伤害



    [Header("防御属性")]
    public Stats maxHealth;
    public Stats armor;
    public Stats evasion;
    public Stats magicResistance;
    [Range(0.1f,1f)]
    public float vulnerablePerentage;

    [Header("魔法属性")]
    public Stats fireDamage;
    public Stats iceDamage;
    public Stats lightningDamage;

    [Header("人物状态")]
    public bool isIgnited;//持续伤害
    public bool isChilled;//减速易伤
    public bool isShocked;//眩晕，命中率下降


    public float igniteDuration = 2f;
    public float chillDuration  = 2f;
    public float shockDuration  = 2f;


    //元素持续时间
    private float ailmentIgniteDuration=3;
    private float ailmentChillDuration=4;
    private float ailmentShockDuration = 2;

    //元素行为
    private float slowPercentage = 0.2f;



    private float ignitedTimer;
    private float chilledTimer;
    private float shockedTimer;


    private float igniteDamageCoolDown=0.3f;
    private float igniteDamageTimer;
    private int igniteDamage;
    [SerializeField] private GameObject shockStrikePrefab; 
    private int shockDamage;


    [SerializeField]public int currentHealth;

    public System.Action onHealthChanged;

    public bool isDead { get; private set; }

    public bool isIncincibles { get; private set; }

    public bool isVulnerable;

    protected virtual void Start()
    {
        critPower.SetDefaultValue(150);
        
        currentHealth = GetMaxHealthValue();

        fx=GetComponent<EntityFX>();

        netStats = GetComponent<NetworkCharacterStats>();
        PushHealthToNetwork();
    }

    protected virtual void Update()
    {
        // 联机时元素（点燃/冰冻/感电）统一由服务器结算，客户端只负责表现
        if (NetGame.IsOnline && !NetGame.IsServer)
        {
            return;
        }

ignitedTimer -= Time.deltaTime;
        chilledTimer -= Time.deltaTime;
        shockedTimer -= Time.deltaTime;

        igniteDamageTimer -= Time.deltaTime;



        if (ignitedTimer < 0)
        {
            isIgnited = false;
        }
        if (chilledTimer < 0)
        {
            isChilled = false;
        }
        if (shockedTimer < 0)
        {
            isShocked = false;
        }

        if (isIgnited)
        {
            ApplyIgniteDamage();
        }

    }

    public void MakeVulnerableFor(float _duration)
    {
        StartCoroutine(vulnerableForCorutine(_duration));

    }



    private IEnumerator vulnerableForCorutine(float _duartion)
    {
        isVulnerable = true;
        yield return new WaitForSeconds(_duartion);
        isVulnerable = false;

    }


    public virtual void IncreaseStatBy(int _modifier,float _duration,Stats _statsToModify)
    {
        StartCoroutine(StatModCoroutine(_modifier, _duration, _statsToModify));

    }
    private IEnumerator StatModCoroutine(int _modifier, float _duration, Stats _statsToModify)
    {
        _statsToModify.AddModifier(_modifier);
        yield return new WaitForSeconds(_duration);
        _statsToModify.RemoveModifier(_modifier);

    }

    
    public virtual void DoDamage(CharacterStats _targetStats)
    {
        if (_targetStats.isIncincibles)
        {
            return;
        }


        if (TargetCanAvoidAttack(_targetStats))
        {
            return;
        }

        _targetStats.GetComponent<Entity>().SetupKnockbackDir(transform);

        int totalDamage = damage.GetValue() + strengths.GetValue();
        if(CanCrit())
        {
           totalDamage= CalculateCriticalDamage(totalDamage);
        }
        fx.CreateHitFX(_targetStats.transform,CanCrit()); 

        totalDamage = CheckTargeArmor(_targetStats, totalDamage);
        NetCombat.ApplyDamage(this, _targetStats, totalDamage);
        
        //如果物理攻击包含魔法伤害
        DoMagicalDamage(_targetStats);

    }

    
    #region 魔法伤害和元素

    public virtual void DoMagicalDamage(CharacterStats _targetStats)
    {
        int _fireDamage = fireDamage.GetValue();
        int _iceDamage = iceDamage.GetValue();
        int _lightningDamage = lightningDamage.GetValue();



        int totalMagicalDamage = _fireDamage + _iceDamage + _lightningDamage + intelligence.GetValue();
        totalMagicalDamage = CheckTargetResistance(_targetStats, totalMagicalDamage);
        NetCombat.ApplyDamage(this, _targetStats, totalMagicalDamage);


        if (Mathf.Max(_fireDamage, _iceDamage, _lightningDamage) <= 0)
        {
            return;
        }

        AttemptToApplyAilemens(_targetStats, _fireDamage, _iceDamage, _lightningDamage);

    }

    private  void AttemptToApplyAilemens(CharacterStats _targetStats, int _fireDamage, int _iceDamage, int _lightningDamage)
    {
        bool canApplyIgnite = _fireDamage > _iceDamage && _fireDamage > _lightningDamage;
        bool canApplyChill = _iceDamage > _fireDamage && _iceDamage > _lightningDamage;
        bool canApplyShock = _lightningDamage > _fireDamage && _lightningDamage > _iceDamage;



        while (!canApplyIgnite && !canApplyChill && !canApplyShock)
        {
            int randomNum = Random.Range(0, 3);
            switch (randomNum)
            {
                case 0:
                    if (Random.Range(0, 100) < 50 && _fireDamage > 0)
                    {
                        canApplyIgnite = true;
                        NetCombat.ApplyAilments(this, _targetStats, canApplyIgnite, canApplyChill, canApplyShock,
                            canApplyIgnite ? Mathf.RoundToInt(_fireDamage * 0.2f) : 0,
                            canApplyShock ? Mathf.RoundToInt(_lightningDamage * 0.3f) : 0);
                        return;
                    }
                    break;
                case 1:
                    if (Random.Range(0, 100) < 50 && _iceDamage > 0)
                    {
                        canApplyChill = true;
                        NetCombat.ApplyAilments(this, _targetStats, canApplyIgnite, canApplyChill, canApplyShock,
                            canApplyIgnite ? Mathf.RoundToInt(_fireDamage * 0.2f) : 0,
                            canApplyShock ? Mathf.RoundToInt(_lightningDamage * 0.3f) : 0);
                        return;
                    }
                    break;
                case 2:

                    if (Random.Range(0, 100) < 50 && _lightningDamage > 0)
                    {
                        canApplyShock = true;
                        NetCombat.ApplyAilments(this, _targetStats, canApplyIgnite, canApplyChill, canApplyShock,
                            canApplyIgnite ? Mathf.RoundToInt(_fireDamage * 0.2f) : 0,
                            canApplyShock ? Mathf.RoundToInt(_lightningDamage * 0.3f) : 0);
                        return;
                    }
                    break;
            }

        }
        int igniteDamageValue = canApplyIgnite ? Mathf.RoundToInt(_fireDamage * 0.2f) : 0;
        int shockDamageValue = canApplyShock ? Mathf.RoundToInt(_lightningDamage * 0.3f) : 0;

        NetCombat.ApplyAilments(this, _targetStats, canApplyIgnite, canApplyChill, canApplyShock,
            igniteDamageValue, shockDamageValue);
    }


    public virtual void ApplyAilments(bool _ignite, bool _chill, bool _shock)
    {
        bool canApplyIgnite = !isIgnited && !isChilled && !isShocked;
        bool canApplyChill = !isChilled && !isIgnited && !isShocked;
        bool canApplyShock = !isIgnited && !isChilled;



        if (_ignite && canApplyIgnite)
        {
            isIgnited = _ignite;
            ignitedTimer = igniteDuration;
            fx.IgniteFxUse(ailmentIgniteDuration);
        }
        if (_chill && canApplyChill)
        {
            isChilled = _chill;
            chilledTimer = chillDuration;

            GetComponent<Entity>().SlowEntityBy(slowPercentage, ailmentChillDuration);
            fx.ChillFxUse(ailmentChillDuration);
        }
        ApplyShock(_shock, canApplyShock);


    }

    private void ApplyIgniteDamage()
    {
        if (igniteDamageTimer < 0)
        {
            DecreaseHealthBy(igniteDamage);

            if (currentHealth <= 0&&!isDead )
            {
                Die();
            }
            igniteDamageTimer = igniteDamageCoolDown;
        }
    }

    private void ApplyShock(bool _shock, bool canApplyShock)
    {
        if (_shock && canApplyShock)
        {
            if (!isShocked)
            {
                shockedTimer = shockDuration;
                isShocked = _shock;

                fx.ShockFxUse(ailmentShockDuration);
            }
            else
            {
                HitNearestTargetWithShockStrike();
            }
        }
    }

    private void HitNearestTargetWithShockStrike()
    {
        //寻找最近的目标并且仅仅攻击他一个
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 25);
        GameObject entity = colliders[0].gameObject;
        if (entity)
        {
            float closestDistance = Mathf.Infinity;
            Transform closestEnemy = null;
            foreach (var hit in colliders)
            {
                if (hit.GetComponent<Enemy>() != null && Vector2.Distance(transform.position, hit.transform.position) > 1)
                {
                    float distanceToEnemy = Vector2.Distance(transform.position, hit.transform.position);
                    if (distanceToEnemy < closestDistance)
                    {
                        closestDistance = distanceToEnemy;
                        closestEnemy = hit.transform;
                    }

                }

                if (closestEnemy == null)
                {
                    closestEnemy = transform;
                }

            }
            if (closestEnemy != null)
            {
                GameObject newShockStrike = Instantiate(shockStrikePrefab, transform.position, Quaternion.identity);
                newShockStrike.GetComponent<ShockStrike_Control>().Setup(shockDamage, closestEnemy.GetComponent<CharacterStats>());

            }
        }
    }

    public virtual void SetupIgniteDamage(int _damage) => igniteDamage = _damage;

    public virtual void SetupShockStrikeDamage(int _damage) => shockDamage = _damage;

    #endregion
    public virtual void TakeDamage(int _damage)
    {
        if (isIncincibles)
        {
            return;
        }



        DecreaseHealthBy(_damage);

        GetComponent<Entity>().DamageImpact();
        fx.StartCoroutine("FlashFX");
        if (currentHealth <= 0&&!isDead)
        {
            Die();
        }

    }

    public virtual void IncreaseHealthBy(int _amount)
    {
        currentHealth += _amount;
        if (currentHealth > GetMaxHealthValue())
        {
            currentHealth = GetMaxHealthValue();
        }
        PushHealthToNetwork();
        if (onHealthChanged != null)
        {
            onHealthChanged();
        }
    }


    protected virtual void DecreaseHealthBy(int _damage)
    {
        if (isVulnerable)
        {
            _damage =Mathf.RoundToInt(_damage * (1+vulnerablePerentage));

        }

        currentHealth -= _damage;
        if (_damage > 0)
        {
            fx.CreatePopUpText(_damage.ToString());
        }

        PushHealthToNetwork();



        if (onHealthChanged != null)
        {
            onHealthChanged();
        }

    }

    protected virtual void Die()
    {
        //throw new NotImplementedException();
        isDead = true;

        if (netStats != null)
        {
            netStats.PushDead(true);
        }
    }
    public void KillEntity()=> Die();

    #region 联机支持

    private void PushHealthToNetwork()
    {
        if (netStats != null)
        {
            netStats.PushHealth(currentHealth);
        }
    }

    /// <summary>客户端收到服务器推来的血量。</summary>
    public void SetHealthFromNetwork(int _value, int _damageTaken, bool _playVisuals)
    {
        currentHealth = _value;

        if (_playVisuals && _damageTaken > 0 && fx != null)
        {
            fx.CreatePopUpText(_damageTaken.ToString());
            fx.StartCoroutine("FlashFX");
        }

        if (onHealthChanged != null)
        {
            onHealthChanged();
        }
    }

    /// <summary>客户端收到“我死了”。</summary>
    public void ApplyRemoteDeath()
    {
        if (isDead) return;

        isDead = true;
        OnRemoteDeath();
    }

    /// <summary>子类可以在这里做死亡表现。</summary>
    protected virtual void OnRemoteDeath() { }

    /// <summary>客户端播放元素表现。</summary>
    public void ApplyAilmentVisual(StatAilment _ailment, bool _on)
    {
        if (fx == null) return;

        switch (_ailment)
        {
            case StatAilment.Ignite:
                isIgnited = _on;
                if (_on)
                {
                    ignitedTimer = igniteDuration;
                    fx.IgniteFxUse(ailmentIgniteDuration);
                }
                break;

            case StatAilment.Chill:
                isChilled = _on;
                if (_on)
                {
                    chilledTimer = chillDuration;
                    fx.ChillFxUse(ailmentChillDuration);
                }
                break;

            case StatAilment.Shock:
                isShocked = _on;
                if (_on)
                {
                    shockedTimer = shockDuration;
                    fx.ShockFxUse(ailmentShockDuration);
                }
                break;
        }
    }

    /// <summary>复活：血量恢复满、清空所有元素状态。</summary>
    public void ReviveFromNetwork()
    {
        isDead = false;
        currentHealth = GetMaxHealthValue();

        isIgnited = false;
        isChilled = false;
        isShocked = false;

        PushHealthToNetwork();

        if (onHealthChanged != null)
        {
            onHealthChanged();
        }
    }

    #endregion
    
    public void MakeInvinciable(bool _invinciable)
    {
        isIncincibles=_invinciable;
    }


    #region 数值计算区域

    protected int CheckTargeArmor(CharacterStats _targetStats, int totalDamage)
    {
        if (isChilled)
        {
            totalDamage =Mathf.RoundToInt(_targetStats.armor.GetValue() *0.8f);
        }
        else
        {
            totalDamage -= _targetStats.armor.GetValue();
        }

        if (totalDamage < 0)
        {
            totalDamage = 0;
        }

        return totalDamage;
    }

    private int CheckTargetResistance(CharacterStats _targetStats, int totalMagicalDamage)
    {
        totalMagicalDamage -= _targetStats.magicResistance.GetValue() + intelligence.GetValue() * 3;
        if (totalMagicalDamage < 0)
        {
            totalMagicalDamage = 0;
        }
        return totalMagicalDamage;
    }

    public virtual void OnEvasion()
    {

    }



    protected bool TargetCanAvoidAttack(CharacterStats _targetStats)
    {
        int totalEvasion = _targetStats.evasion.GetValue() + _targetStats.agility.GetValue();
        if (isShocked)
        {
            totalEvasion += 20;
            if (totalEvasion >=100)
            {
                totalEvasion = 100;
            }
        }
        
        
        
        if (Random.Range(0, 100) < totalEvasion)
        {
            _targetStats.OnEvasion();
            return true;
        }
        return false;
    }

    protected bool CanCrit()
    {
        int totalCriticalChance = critChance.GetValue()+agility.GetValue(); 

        if (Random.Range(0, 100) < totalCriticalChance)
        {
            return true;
        }
        return false;
    }
    protected int CalculateCriticalDamage(int _damage)
    {
        float totalCritPower = (critPower.GetValue() + strengths.GetValue())  * 0.01f;
        float critDamage= _damage * totalCritPower;
        return Mathf.RoundToInt(critDamage);
    }
    public int GetMaxHealthValue()
    {
        return maxHealth.GetValue() + vitality.GetValue() * 5;
    }
    #endregion

    public Stats GetStatsByType(StatType _statsType)
    {
        switch (_statsType)
        {
            case StatType.strength:
                return strengths;
            case StatType.agility:
                return agility;
            case StatType.intelligence:
                return intelligence;
            case StatType.vitality:
                return vitality;
            case StatType.damage:
                return damage;
            case StatType.critChance:
                return critChance;
            case StatType.critPower:
                return critPower;
            case StatType.health:
                return maxHealth;
            case StatType.armor:
                return armor;
            case StatType.evasion:
                return evasion;
            case StatType.magicResistance:
                return magicResistance;
            case StatType.fireDamage:
                return fireDamage;
            case StatType.iceDamage:
                return iceDamage;
            case StatType.lightningDamage:
                return lightningDamage;
            default:
                return null;
        }
    }


}
