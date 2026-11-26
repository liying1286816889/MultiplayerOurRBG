using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
public enum EquipmentType
{
    Weapon,  //武器
    Armor,   //护甲
    Amulet,  //护符
    Flask    //药水

}
[CreateAssetMenu(fileName = "New Item Data", menuName = "Data/Equipment")]
public class ItemData_Equipment : ItemData
{
    public EquipmentType equipmentType;
    [Header("独特效果")]
    [TextArea]
    public string itemEffectDescription;


    public float itemCooldown;
    public ItemEffect[] itemEffects;
    public string[] FlaskDescription;

    [Header("主要属性")]
    public int strength;
    public int agility;
    public int intelligence;
    public int vitality;


    [Header("攻击属性")]
    public int damage;
    public int critChance;
    public int critPower;



    [Header("防御属性")]
    public int health;
    public int armor;
    public int evasion;
    public int magicResistance;

    [Header("魔法属性")]
    public int fireDamage;
    public int iceDamage;
    public int lightningDamage;

    [Header("制作要求")]
    public List<InventoryItem> craftingMaterials;


    private int DescriptionLength;

    public void Effect(Transform _enemyPosition)
    {
        if (itemEffects == null || itemEffects.Length == 0)
            return;

        foreach (var item in itemEffects)
        {
            item.ExecuteEffect(_enemyPosition);
        }

    }


    public void AddModifiers()
    {
        PlayerStats playerStats = PlayerManager.instance.player.GetComponent<PlayerStats>();
        playerStats.strengths.AddModifier(strength);
        playerStats.agility.AddModifier(agility);
        playerStats.intelligence.AddModifier(intelligence);
        playerStats.vitality.AddModifier(vitality);

        playerStats.damage.AddModifier(damage);
        playerStats.critChance.AddModifier(critChance);
        playerStats.critPower.AddModifier(critPower);

        playerStats.maxHealth.AddModifier(health);
        playerStats.armor.AddModifier(armor);
        playerStats.evasion.AddModifier(evasion);
        playerStats.magicResistance.AddModifier(magicResistance);

        playerStats.fireDamage.AddModifier(fireDamage);
        playerStats.iceDamage.AddModifier(iceDamage);
        playerStats.lightningDamage.AddModifier(lightningDamage);
    }

    public void RemoveModifiers()
    {
        PlayerStats playerStats = PlayerManager.instance.player.GetComponent<PlayerStats>();
        playerStats.strengths.RemoveModifier(strength);
        playerStats.agility.RemoveModifier(agility);
        playerStats.intelligence.RemoveModifier(intelligence);
        playerStats.vitality.RemoveModifier(vitality);

        playerStats.damage.RemoveModifier(damage);
        playerStats.critChance.RemoveModifier(critChance);
        playerStats.critPower.RemoveModifier(critPower);

        playerStats.maxHealth.RemoveModifier(health);
        playerStats.armor.RemoveModifier(armor);
        playerStats.evasion.RemoveModifier(evasion);
        playerStats.magicResistance.RemoveModifier(magicResistance);

        playerStats.fireDamage.RemoveModifier(fireDamage);
        playerStats.iceDamage.RemoveModifier(iceDamage);
        playerStats.lightningDamage.RemoveModifier(lightningDamage);
    }
    public override string GetDescription()
    {
        sb.Length = 0;
        DescriptionLength = 0;
        if (this.equipmentType == EquipmentType.Flask && FlaskDescription != null && FlaskDescription.Length > 0)
        {
            for (int i = 0; i < FlaskDescription.Length; i++)
            {
                sb.Append(FlaskDescription[i]);
                sb.AppendLine();
                DescriptionLength++;
            }
        }

        AddItemDescription(strength, "Strength");
        AddItemDescription(agility, "Agility");
        AddItemDescription(intelligence, "Intelligence");
        AddItemDescription(vitality, "Vitality");

        AddItemDescription(damage, "Damage");
        AddItemDescription(critChance, "Crit Chance");
        AddItemDescription(critPower, "Crit Power");

        AddItemDescription(health, "Health");
        AddItemDescription(armor, "Armor");
        AddItemDescription(evasion, "Evasion");
        AddItemDescription(magicResistance, "Magic Resistance");

        AddItemDescription(fireDamage, "Fire Damage");
        AddItemDescription(iceDamage, "Ice Damage");
        AddItemDescription(lightningDamage, "Lightning Damage");

        if (DescriptionLength < 4)
        {
            for (int i = 0; i < 4 - DescriptionLength; i++)
            {
                sb.AppendLine();
                sb.Append("");
            }
        }
        if (!string.IsNullOrEmpty(itemEffectDescription))
        {
            sb.AppendLine();
            sb.Append("特殊效果" + itemEffectDescription);
        }


        return sb.ToString();
    }

    protected void AddItemDescription(int _value, string _name)
    {
        if (_value != 0)
        {
            if (_name.Length > 0)
            {
                sb.AppendLine();
            }
            if (_value > 0)
            {
                sb.Append(_name + " : " + _value);
            }
            DescriptionLength++;
        }
    }


}
