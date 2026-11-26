using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[CreateAssetMenu(fileName = "Buff Effect", menuName = "Data/ItemEffect/BuffEffect")]
public class Buff_Effect : ItemEffect
{
    private PlayerStats PlayerStats;
    [SerializeField] private StatType buffType;
    [SerializeField] private int buffAmount;
    [SerializeField] private float buffDuration;

    public override void ExecuteEffect(Transform _respondPosition)
    {
        PlayerStats = PlayerManager.instance.player.GetComponent<PlayerStats>();


        PlayerStats.IncreaseStatBy(buffAmount, buffDuration,PlayerStats.GetStatsByType(buffType));
    }

    


}
