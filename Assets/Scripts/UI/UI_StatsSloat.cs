using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class UI_StatsSlot : MonoBehaviour,IPointerEnterHandler, IPointerExitHandler
{
    private UI ui;

    [SerializeField] private string statName;
    [SerializeField] private StatType statType;

    [SerializeField] private TextMeshProUGUI statValueText;
    [SerializeField] private TextMeshProUGUI statNameText;

    [TextArea]
    [SerializeField] private string statsDescription;



    private void OnValidate()
    {
        gameObject.name="Stats -"+statName;

        if (statName != null)
        {
            statNameText.text = statName;

        }
    }

    void Start()
    {
        UpdateStatsValueUI();
        ui=GetComponentInParent<UI>();

    }

    public void UpdateStatsValueUI()
    {
        PlayerStats playerStats=PlayerManager.instance.player.GetComponent<PlayerStats>();
        if (playerStats != null)
        { 
            
            statValueText.text = playerStats.GetStatsByType(statType).GetValue().ToString();

            if( statType==StatType.health)
            {
                statValueText.text =playerStats.GetMaxHealthValue().ToString();
            }

            if( statType==StatType.damage)
            {
                statValueText.text =(playerStats.damage.GetValue()+playerStats.strengths.GetValue()).ToString();
            }
            
            if (statType == StatType.critChance)
            {
                statValueText.text = (playerStats.critChance.GetValue() + playerStats.agility.GetValue()).ToString();
            }

            if (statType == StatType.critPower)
            {
                statValueText.text = (playerStats.critPower.GetValue() + playerStats.strengths.GetValue()).ToString();
            }

            if (statType == StatType.evasion)
            {
                statValueText.text = (playerStats.evasion.GetValue() + playerStats.agility.GetValue()).ToString();
            }

            if (statType == StatType.magicResistance)
            {
                statValueText.text = (playerStats.magicResistance.GetValue() + playerStats.intelligence.GetValue()*3).ToString();
            }


        }

    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ui.statsToolTip.ShowStatsToolTip(statsDescription);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ui.statsToolTip.HideStatsToolTip();   

    }
}
