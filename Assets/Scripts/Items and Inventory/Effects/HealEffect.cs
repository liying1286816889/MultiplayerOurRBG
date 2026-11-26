using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Heal Effect", menuName = "Data/ItemEffect/HealEffect")]
public class HealEffect : ItemEffect
{
    [Range(0f,1f)]
    [SerializeField] private float healPercen;
    [SerializeField] private int fixedHealAmount;


    public override void ExecuteEffect(Transform _respondPosition)
    {
        PlayerStats playerStats=PlayerManager.instance.player.GetComponent<PlayerStats>();
        int healAmount = 0;
        if (healPercen > 0)
        {
           healAmount = Mathf.RoundToInt(playerStats.GetMaxHealthValue() * healPercen);
        }
        else if(fixedHealAmount > 0)
        {
            healAmount = fixedHealAmount;
        }
        playerStats.IncreaseHealthBy(healAmount);


    }
}
